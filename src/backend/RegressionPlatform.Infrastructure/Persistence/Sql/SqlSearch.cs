using System.Text;

namespace RegressionPlatform.Infrastructure.Persistence.Sql;

/// <summary>
/// Builds safe SQL <c>LIKE</c> patterns from user input. Wildcard metacharacters (<c>%</c>, <c>_</c>,
/// <c>[</c>, and the escape char itself) are escaped so they match literally — otherwise text such as
/// run-response titles wrapped in <c>[...]</c> is read as a character class and matches everything.
/// The generated pattern must be used with the matching SQL <c>ESCAPE '\'</c> clause.
/// </summary>
internal static class SqlSearch
{
    /// <summary>
    /// A "contains" pattern (<c>%term%</c>) with wildcards escaped, or null when the term is
    /// null/whitespace (so callers can skip filtering via <c>@Search IS NULL</c>).
    /// </summary>
    public static string? Contains(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        return $"%{Escape(term.Trim())}%";
    }

    /// <summary>
    /// A LIKE pattern for the given match <paramref name="mode"/> (case-insensitive):
    /// <c>equals</c> → exact, <c>startsWith</c> → <c>term%</c>, <c>endsWith</c> → <c>%term</c>,
    /// anything else (incl. null) → <c>contains</c> (<c>%term%</c>). Null when the term is blank.
    /// </summary>
    public static string? Pattern(string? term, string? mode)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        var escaped = Escape(term.Trim());
        return mode?.Trim().ToLowerInvariant() switch
        {
            "equals" => escaped,
            "startswith" => $"{escaped}%",
            "endswith" => $"%{escaped}",
            _ => $"%{escaped}%",
        };
    }

    /// <summary>A LIKE wildcard for one DTC-code character: any single char that is NOT a dash or a
    /// space, so shapes ignore free-text placeholders like "No code" / "Available".</summary>
    private const string CodeChar = "[^- ]";

    /// <summary>
    /// A shape-only LIKE pattern from a DTC-format shape (e.g. <c>"xx-xxx"</c>): every <c>x</c> becomes
    /// <c>[^- ]</c> (any single code char — not a dash or space) and each <c>-</c> stays literal. So
    /// <c>"xxxx"</c> matches exactly four dash/space-free chars. Null when the shape is blank.
    /// </summary>
    public static string? FormatShape(string? shape)
    {
        if (string.IsNullOrWhiteSpace(shape))
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var c in shape.Trim())
        {
            sb.Append(c == '-' ? "-" : CodeChar);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Overlays a DTC <paramref name="term"/> onto a format <paramref name="shape"/> so the term acts as a
    /// prefix and the shape fixes the overall length: <c>shape "xxxx" + term "P0" → "P0[^- ][^- ]"</c>
    /// (matches <c>P0xx</c>). Each <c>x</c> slot takes the next non-dash term char (escaped) or, once the
    /// term is exhausted, <c>[^- ]</c>; each <c>-</c> stays literal (consuming a matching dash in the term).
    /// Null when either input is blank.
    /// </summary>
    public static string? FormatOverlay(string? shape, string? term)
    {
        if (string.IsNullOrWhiteSpace(shape) || string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        var value = term.Trim();
        var sb = new StringBuilder();
        var vi = 0;
        foreach (var c in shape.Trim())
        {
            if (c == '-')
            {
                sb.Append('-');
                if (vi < value.Length && value[vi] == '-')
                {
                    vi++;
                }
            }
            else if (vi < value.Length && value[vi] != '-')
            {
                sb.Append(EscapeChar(value[vi]));
                vi++;
            }
            else
            {
                sb.Append(CodeChar);
            }
        }
        return sb.ToString();
    }

    /// <summary>Escapes LIKE metacharacters in a whole term (escape char first, then wildcards).</summary>
    private static string Escape(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");

    /// <summary>Escapes a single character for use as a literal inside a LIKE pattern.</summary>
    private static string EscapeChar(char c) =>
        c is '\\' or '%' or '_' or '[' ? $"\\{c}" : c.ToString();
}
