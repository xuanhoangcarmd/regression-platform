using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// Implements the response-storage pipeline (docs/business/ResponseStorage.md), minus masking and
/// encryption (planned): apply ignore rules → canonical JSON → SHA-256 hash (from canonical, before
/// any encryption). For non-JSON bodies the raw text is used as-is.
/// </summary>
public static class ResponseCanonicalizer
{
    public sealed record CanonicalResult(string Canonical, string Hash);

    /// <summary>A TrimAfter rule: string value(s) at <paramref name="JsonPath"/> are cut at the first <paramref name="Delimiter"/>.</summary>
    public sealed record ValueTrim(string JsonPath, string Delimiter);

    /// <summary>Applies <paramref name="ignorePaths"/> then returns canonical text + its SHA-256 (hex).</summary>
    /// <paramref name="trims"/> (TrimAfter rules) cut matching string values before hashing.
    public static CanonicalResult Process(string body, IReadOnlyList<string> ignorePaths, IReadOnlyList<ValueTrim>? trims = null)
    {
        var node = TryParse(body);
        string canonical;
        if (node is null)
        {
            // Non-JSON (HTML/plain text/…): hash the raw body verbatim.
            canonical = body ?? string.Empty;
        }
        else
        {
            foreach (var path in ignorePaths)
            {
                RemovePath(node, path);
            }
            ApplyTrims(node, trims);
            canonical = Canonicalize(node);
        }

        return new CanonicalResult(canonical, Sha256Hex(canonical));
    }

    /// <summary>
    /// Like <see cref="Process"/> but keeps the ORIGINAL object-key order (compact, no whitespace):
    /// applies <paramref name="ignorePaths"/> then returns the text + its SHA-256. Used to store the
    /// response as returned and for strict-order compares. Non-JSON bodies pass through verbatim.
    /// </summary>
    public static CanonicalResult ProcessOrdered(string body, IReadOnlyList<string> ignorePaths, IReadOnlyList<ValueTrim>? trims = null)
    {
        var node = TryParse(body);
        string text;
        if (node is null)
        {
            text = body ?? string.Empty;
        }
        else
        {
            foreach (var path in ignorePaths)
            {
                RemovePath(node, path);
            }
            ApplyTrims(node, trims);
            text = Serialize(node, sortKeys: false);
        }

        return new CanonicalResult(text, Sha256Hex(text));
    }

    /// <summary>
    /// Lifts a subtree to the response root, so an envelope-wrapped body can be compared against an
    /// un-wrapped one. For each path in <paramref name="extractPaths"/> that resolves to an object or
    /// array, that node becomes the new root (applied in order — later paths resolve against the new
    /// root). Non-JSON bodies, empty path lists, and paths that don't resolve leave the body unchanged
    /// (so this is a no-op wherever no extract-root rule applies).
    /// </summary>
    public static string ExtractRoot(string body, IReadOnlyList<string> extractPaths)
    {
        if (extractPaths.Count == 0 || string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        var root = TryParse(body);
        if (root is null)
        {
            // Non-JSON (HTML/plain text/…): nothing to lift.
            return body;
        }

        var current = root;
        foreach (var path in extractPaths)
        {
            var resolved = ResolvePath(current, path);
            // Only lift a structural node (object/array) — never a primitive/null.
            if (resolved is JsonObject or JsonArray)
            {
                current = resolved;
            }
        }

        // Serialize the (possibly new) root. ToJsonString serializes the node in place without
        // detaching it from its parent, so no cloning is required.
        return ReferenceEquals(current, root) ? body : current.ToJsonString();
    }

    /// <summary>Navigates a dotted/indexed path (e.g. <c>$.data.result[0]</c>) and returns the node it
    /// points to, or null if any segment is missing. Wildcards are not supported for extract-root.</summary>
    private static JsonNode? ResolvePath(JsonNode? node, string path)
    {
        var current = node;
        foreach (var token in ParsePath(path))
        {
            if (current is null)
            {
                return null;
            }

            switch (token.Kind)
            {
                case TokenKind.Property when current is JsonObject obj:
                    current = obj.TryGetPropertyValue(token.Name, out var child) ? child : null;
                    break;
                case TokenKind.Index when current is JsonArray array && token.Index >= 0 && token.Index < array.Count:
                    current = array[token.Index];
                    break;
                default:
                    return null; // wrong shape or wildcard — not resolvable for extract-root
            }
        }

        return current;
    }

    private static JsonNode? TryParse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }
        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ----- Canonical JSON: object keys sorted (ordinal), no whitespace, values preserved. -----

    private static string Canonicalize(JsonNode? node) => Serialize(node, sortKeys: true);

    /// <summary>Compact JSON; <paramref name="sortKeys"/> = canonical (ordinal key order) vs original order.</summary>
    private static string Serialize(JsonNode? node, bool sortKeys)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteNode(writer, node, sortKeys);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteNode(Utf8JsonWriter writer, JsonNode? node, bool sortKeys)
    {
        switch (node)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonObject obj:
                writer.WriteStartObject();
                IEnumerable<KeyValuePair<string, JsonNode?>> properties = sortKeys ? obj.OrderBy(p => p.Key, StringComparer.Ordinal) : obj;
                foreach (var property in properties)
                {
                    writer.WritePropertyName(property.Key);
                    WriteNode(writer, property.Value, sortKeys);
                }
                writer.WriteEndObject();
                break;
            case JsonArray array:
                writer.WriteStartArray();
                foreach (var item in array)
                {
                    WriteNode(writer, item, sortKeys);
                }
                writer.WriteEndArray();
                break;
            default:
                // JsonValue — write the underlying primitive (number/string/bool) verbatim.
                node.WriteTo(writer);
                break;
        }
    }

    // ----- Ignore-path removal: supports $.prop, nested .prop, [index], and [*] wildcard. -----

    private enum TokenKind
    {
        Property,
        Index,
        Wildcard,
    }

    private readonly record struct PathToken(TokenKind Kind, string Name, int Index);

    private static void RemovePath(JsonNode root, string path)
    {
        var tokens = ParsePath(path);
        if (tokens.Count > 0)
        {
            RemoveRecursive(root, tokens, 0);
        }
    }

    private static void RemoveRecursive(JsonNode? node, IReadOnlyList<PathToken> tokens, int index)
    {
        if (node is null)
        {
            return;
        }

        var token = tokens[index];
        var isLast = index == tokens.Count - 1;

        switch (token.Kind)
        {
            case TokenKind.Property when node is JsonObject obj:
                if (isLast)
                {
                    obj.Remove(token.Name);
                }
                else if (obj.TryGetPropertyValue(token.Name, out var child))
                {
                    RemoveRecursive(child, tokens, index + 1);
                }
                break;

            case TokenKind.Index when node is JsonArray array && token.Index >= 0 && token.Index < array.Count:
                if (isLast)
                {
                    array.RemoveAt(token.Index);
                }
                else
                {
                    RemoveRecursive(array[token.Index], tokens, index + 1);
                }
                break;

            case TokenKind.Wildcard when node is JsonArray array && !isLast:
                // Snapshot: descend into each element (last-token removal happens inside each).
                foreach (var item in array.ToList())
                {
                    RemoveRecursive(item, tokens, index + 1);
                }
                break;
        }
    }

    // ----- TrimAfter: cut string values at a delimiter ($.prop, nested, [index], [*]) -----

    private static void ApplyTrims(JsonNode root, IReadOnlyList<ValueTrim>? trims)
    {
        if (trims is null)
        {
            return;
        }
        foreach (var trim in trims)
        {
            var tokens = ParsePath(trim.JsonPath);
            if (tokens.Count > 0 && !string.IsNullOrEmpty(trim.Delimiter))
            {
                TrimRecursive(root, tokens, 0, trim.Delimiter);
            }
        }
    }

    private static void TrimRecursive(JsonNode? node, IReadOnlyList<PathToken> tokens, int index, string delimiter)
    {
        if (node is null)
        {
            return;
        }

        var token = tokens[index];
        var isLast = index == tokens.Count - 1;

        switch (token.Kind)
        {
            case TokenKind.Property when node is JsonObject obj && obj.TryGetPropertyValue(token.Name, out var child):
                if (isLast)
                {
                    if (TrimmedValue(child, delimiter) is { } cut)
                    {
                        obj[token.Name] = cut;
                    }
                }
                else
                {
                    TrimRecursive(child, tokens, index + 1, delimiter);
                }
                break;

            case TokenKind.Index when node is JsonArray array && token.Index >= 0 && token.Index < array.Count:
                TrimElement(array, token.Index, tokens, index, isLast, delimiter);
                break;

            case TokenKind.Wildcard when node is JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    TrimElement(array, i, tokens, index, isLast, delimiter);
                }
                break;
        }
    }

    private static void TrimElement(JsonArray array, int i, IReadOnlyList<PathToken> tokens, int index, bool isLast, string delimiter)
    {
        if (!isLast)
        {
            TrimRecursive(array[i], tokens, index + 1, delimiter);
        }
        else if (TrimmedValue(array[i], delimiter) is { } cut)
        {
            array[i] = cut;
        }
    }

    /// <summary>The string cut before the first delimiter, or null when it isn't a string / has no delimiter.</summary>
    private static string? TrimmedValue(JsonNode? node, string delimiter)
    {
        if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.String)
        {
            return null;
        }
        var text = value.GetValue<string>();
        var at = text.IndexOf(delimiter, StringComparison.Ordinal);
        return at < 0 ? null : text[..at];
    }

    private static List<PathToken> ParsePath(string path)
    {
        var tokens = new List<PathToken>();
        if (string.IsNullOrWhiteSpace(path))
        {
            return tokens;
        }

        var trimmed = path.Trim();
        var i = 0;
        if (trimmed.StartsWith('$'))
        {
            i = 1;
        }

        var buffer = new StringBuilder();
        void FlushProperty()
        {
            if (buffer.Length > 0)
            {
                tokens.Add(new PathToken(TokenKind.Property, buffer.ToString(), 0));
                buffer.Clear();
            }
        }

        while (i < trimmed.Length)
        {
            var c = trimmed[i];
            if (c == '.')
            {
                FlushProperty();
                i++;
            }
            else if (c == '[')
            {
                FlushProperty();
                var close = trimmed.IndexOf(']', i);
                if (close < 0)
                {
                    break; // malformed — stop parsing this path
                }
                var inner = trimmed[(i + 1)..close].Trim();
                if (inner == "*")
                {
                    tokens.Add(new PathToken(TokenKind.Wildcard, string.Empty, 0));
                }
                else if (int.TryParse(inner, out var idx))
                {
                    tokens.Add(new PathToken(TokenKind.Index, string.Empty, idx));
                }
                i = close + 1;
            }
            else
            {
                buffer.Append(c);
                i++;
            }
        }

        FlushProperty();
        return tokens;
    }

    private static string Sha256Hex(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
