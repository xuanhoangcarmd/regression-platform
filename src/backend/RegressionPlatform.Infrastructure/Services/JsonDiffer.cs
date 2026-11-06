using System.Text.Json;
using System.Text.Json.Nodes;

namespace RegressionPlatform.Infrastructure.Services;

/// <summary>
/// Produces field-level differences between two canonical JSON bodies as a JSON array of
/// <c>{ path, changeType, old, new }</c> entries (changeType: ValueChanged / FieldAdded /
/// FieldRemoved / TypeChanged, plus OrderChanged in strict-order mode). Used to populate
/// <c>CompareDetail.DiffJson</c>. Default mode ignores order (keyed arrays matched by identity);
/// strict-order mode reports a changed object-key order and diffs arrays by position (LCS) only.
/// </summary>
internal static class JsonDiffer
{
    /// <summary>Safety cap so a wholesale change can't produce an unbounded diff payload.</summary>
    private const int MaxEntries = 1000;

    /// <summary>
    /// Returns the diff as a JSON array string, or null when there are no differences.
    /// <paramref name="strictOrder"/>: object-key order and array element order are significant.
    /// </summary>
    public static string? Diff(string? jsonA, string? jsonB, bool strictOrder = false)
    {
        var entries = new JsonArray();
        var a = TryParse(jsonA);
        var b = TryParse(jsonB);

        if (a is null && b is null)
        {
            // One or both are non-JSON / empty: compare as raw text.
            if (!string.Equals(jsonA ?? string.Empty, jsonB ?? string.Empty, StringComparison.Ordinal))
            {
                entries.Add(Entry("$", "ValueChanged", ToValue(jsonA), ToValue(jsonB)));
            }
        }
        else
        {
            // Root presence = a parseable body on that side. (A literal "null" body parses to a
            // C# null node and is treated as "no body", which is the desired behaviour here.)
            DiffNode("$", a, a is not null, b, b is not null, entries, strictOrder);
        }

        return entries.Count == 0 ? null : entries.ToJsonString();
    }

    /// <summary>
    /// DISPLAY-only helper for the side-by-side viewer: reorders keyed arrays inside
    /// <paramref name="jsonB"/> to follow <paramref name="jsonA"/>'s element order, using the SAME
    /// identity matching as the diff (so what the engine pairs, the viewer shows opposite each other;
    /// unmatched B items keep their relative order at the end). Recursive; arrays without a usable
    /// identity are left untouched. Never affects hashes, classification, or the stored diff.
    /// </summary>
    public static string AlignArraysForDisplay(string jsonA, string jsonB)
    {
        var a = TryParse(jsonA);
        var b = TryParse(jsonB);
        if (a is null || b is null)
        {
            return jsonB;
        }
        AlignNode(a, b);
        return b.ToJsonString();
    }

    /// <summary>Walks both trees in parallel and aligns every array B shares with A.</summary>
    private static void AlignNode(JsonNode? a, JsonNode? b)
    {
        if (a is JsonObject objA && b is JsonObject objB)
        {
            foreach (var property in objA)
            {
                if (objB.TryGetPropertyValue(property.Key, out var childB))
                {
                    AlignNode(property.Value, childB);
                }
            }
        }
        else if (a is JsonArray arrA && b is JsonArray arrB)
        {
            AlignArray(arrA, arrB);
        }
    }

    /// <summary>Reorders <paramref name="b"/> to A's identity order, then recurses into the pairs.</summary>
    private static void AlignArray(JsonArray a, JsonArray b)
    {
        var keys = FindMatchKeys(a, b);
        if (keys is null)
        {
            // No identity → match on whole elements, the same pairing the diff uses, so a reordered
            // array still lines up in the viewer. Failing that, keep order and recurse pairwise so
            // nested keyed arrays get aligned.
            if (!AlignArrayByEquality(a, b))
            {
                var count = Math.Min(a.Count, b.Count);
                for (var i = 0; i < count; i++)
                {
                    AlignNode(a[i], b[i]);
                }
            }
            return;
        }

        // Detach B's items (a JsonNode can only live under one parent), map them by identity.
        var bItems = b.ToList();
        b.Clear();
        var byKey = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var item in bItems)
        {
            byKey[CompositeKeyOf(item, keys)!] = item;
        }

        // Matched items in A's order first, then B-only items in their original relative order.
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var itemA in a)
        {
            var identity = CompositeKeyOf(itemA, keys)!;
            if (byKey.TryGetValue(identity, out var match))
            {
                used.Add(identity);
                b.Add(match);
                AlignNode(itemA, match);
            }
        }
        foreach (var item in bItems)
        {
            if (!used.Contains(CompositeKeyOf(item, keys)!))
            {
                b.Add(item);
            }
        }
    }

    /// <summary>
    /// Reorders <paramref name="b"/> so elements EXACTLY equal to A's appear in A's order, with the
    /// rest kept behind them in their original relative order. Mirrors <c>TryDiffArrayByEquality</c>,
    /// so the viewer shows opposite each other what the diff paired. False when nothing is shared.
    /// </summary>
    private static bool AlignArrayByEquality(JsonArray a, JsonArray b)
    {
        if (a.Count == 0 || b.Count == 0 || a.Count > MaxArrayLcs || b.Count > MaxArrayLcs)
        {
            return false;
        }

        var takenB = new bool[b.Count];
        var order = new List<int>(b.Count);
        foreach (var itemA in a)
        {
            var j = IndexOfEqual(b, takenB, itemA);
            if (j >= 0)
            {
                takenB[j] = true;
                order.Add(j);
            }
        }
        if (order.Count == 0)
        {
            return false;
        }
        for (var j = 0; j < b.Count; j++)
        {
            if (!takenB[j])
            {
                order.Add(j);
            }
        }

        // Detach first — a JsonNode can only live under one parent.
        var bItems = b.ToList();
        b.Clear();
        foreach (var j in order)
        {
            b.Add(bItems[j]);
        }
        return true;
    }

    /// <summary>
    /// Diffs one node position. <paramref name="aPresent"/>/<paramref name="bPresent"/> say whether the
    /// key/element EXISTS on that side; the node may still be C# null, which represents a JSON
    /// <c>null</c> VALUE. The two meanings must stay distinct — e.g. a field added with value null
    /// (absent on A, present-but-null on B) is a real <c>FieldAdded</c>, not "unchanged".
    /// </summary>
    private static void DiffNode(string path, JsonNode? a, bool aPresent, JsonNode? b, bool bPresent, JsonArray entries, bool strict)
    {
        if (entries.Count >= MaxEntries)
        {
            return;
        }

        if (!aPresent && !bPresent)
        {
            return;
        }
        if (!aPresent)
        {
            entries.Add(Entry(path, "FieldAdded", null, b));
            return;
        }
        if (!bPresent)
        {
            entries.Add(Entry(path, "FieldRemoved", a, null));
            return;
        }

        // Both keys present. A C# null here means a JSON null VALUE.
        if (a is null && b is null)
        {
            return;
        }
        if (a is null || b is null)
        {
            // null value ↔ non-null value.
            entries.Add(Entry(path, "ValueChanged", a, b));
            return;
        }

        if (a.GetValueKind() != b.GetValueKind())
        {
            entries.Add(Entry(path, "TypeChanged", a, b));
            return;
        }

        switch (a)
        {
            case JsonObject objA when b is JsonObject objB:
                if (strict)
                {
                    // Strict order: the keys both sides share must appear in the same order.
                    var orderA = objA.Select(p => p.Key).Where(objB.ContainsKey).ToList();
                    var orderB = objB.Select(p => p.Key).Where(objA.ContainsKey).ToList();
                    if (!orderA.SequenceEqual(orderB, StringComparer.Ordinal))
                    {
                        Add(entries, Entry(path, "OrderChanged", KeyList(orderA), KeyList(orderB)));
                    }
                }
                foreach (var key in objA.Select(p => p.Key).Concat(objB.Select(p => p.Key)).Distinct(StringComparer.Ordinal))
                {
                    var aHas = objA.TryGetPropertyValue(key, out var childA);
                    var bHas = objB.TryGetPropertyValue(key, out var childB);
                    DiffNode($"{path}.{key}", childA, aHas, childB, bHas, entries, strict);
                }
                break;

            case JsonArray arrA when b is JsonArray arrB:
                DiffArray(path, arrA, arrB, entries, strict);
                break;

            default:
                if (!JsonNode.DeepEquals(a, b))
                {
                    entries.Add(Entry(path, "ValueChanged", a, b));
                }
                break;
        }
    }

    /// <summary>
    /// Above this length (either side) an array is diffed positionally instead of by LCS, to avoid the
    /// O(n·m) alignment cost/allocation. Typical response arrays are far smaller than this.
    /// </summary>
    private const int MaxArrayLcs = 500;

    /// <summary>
    /// Diffs two arrays. Keyed object arrays (every element has a unique identity field such as
    /// itemname/name/id/code) are matched BY KEY, so an item is always compared against the same item
    /// regardless of position — reordering produces no field diffs, and a value change reports the
    /// right item's transition. Otherwise falls back to an LCS alignment (equality via DeepEquals):
    /// a middle insertion/removal shows as a single Added/Removed; within a run of unmatched
    /// elements, deletions/insertions are paired positionally and recursed; leftovers become
    /// Removed / Added. Very large arrays fall back to positional diff.
    /// </summary>
    private static void DiffArray(string path, JsonArray a, JsonArray b, JsonArray entries, bool strict)
    {
        // Strict order: no identity matching — a moved item is a real difference (LCS shows it).
        if (!strict && TryDiffArrayByKey(path, a, b, entries))
        {
            return;
        }

        // No usable identity field (e.g. Errors, where Code and even Code+CodeType repeat across
        // modules). Pair the elements that are EXACTLY equal before falling back to LCS, so a pure
        // reorder reports nothing instead of a block of Removed followed by the same block Added.
        if (!strict && TryDiffArrayByEquality(path, a, b, entries))
        {
            return;
        }

        int n = a.Count, m = b.Count;
        if (n > MaxArrayLcs || m > MaxArrayLcs)
        {
            DiffArrayByIndex(path, a, b, entries, strict);
            return;
        }

        // lcs[i, j] = length of the LCS of a[i..] and b[j..].
        var lcs = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                lcs[i, j] = ItemEquals(a[i], b[j], strict)
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var dels = new List<int>(); // A-indices with no match in the current unmatched run
        var ins = new List<int>();  // B-indices with no match in the current unmatched run

        // Resolve the pending unmatched run: pair del↔ins positionally (edited in place → recurse),
        // then emit leftover Removed / Added.
        void FlushBlock()
        {
            var paired = Math.Min(dels.Count, ins.Count);
            for (var k = 0; k < paired; k++)
            {
                DiffNode($"{path}[{ins[k]}]", a[dels[k]], true, b[ins[k]], true, entries, strict);
            }
            for (var k = paired; k < dels.Count; k++)
            {
                Add(entries, Entry($"{path}[{dels[k]}]", "FieldRemoved", a[dels[k]], null));
            }
            for (var k = paired; k < ins.Count; k++)
            {
                Add(entries, Entry($"{path}[{ins[k]}]", "FieldAdded", null, b[ins[k]]));
            }
            dels.Clear();
            ins.Clear();
        }

        int x = 0, y = 0;
        while (x < n && y < m)
        {
            if (entries.Count >= MaxEntries)
            {
                return;
            }
            if (ItemEquals(a[x], b[y], strict))
            {
                FlushBlock(); // a matched anchor closes the current unmatched run
                x++;
                y++;
            }
            else if (lcs[x + 1, y] >= lcs[x, y + 1])
            {
                dels.Add(x++);
            }
            else
            {
                ins.Add(y++);
            }
        }
        while (x < n)
        {
            dels.Add(x++);
        }
        while (y < m)
        {
            ins.Add(y++);
        }
        FlushBlock();
    }

    /// <summary>
    /// Identity-field name SUFFIXES for keyed-array matching, in priority order (used as a tie-break).
    /// A property qualifies when its name equals or ends with one of these (case-insensitive), so
    /// domain keys like <c>DTCCode</c> or <c>ACESPartTypeID</c> are recognized too.
    /// </summary>
    private static readonly string[] MatchKeySuffixes = ["itemname", "name", "id", "code", "key"];

    /// <summary>
    /// Key-based array diff: when both arrays consist of objects sharing a unique identity (a single
    /// field, or a composite like DTCCode + CodeType), elements are paired by that identity's value
    /// (not by position). Matched pairs are recursed at the NEW side's index; items only in A →
    /// FieldRemoved; only in B → FieldAdded. Returns false when no suitable identity exists (the
    /// caller then falls back to the LCS diff).
    /// </summary>
    private static bool TryDiffArrayByKey(string path, JsonArray a, JsonArray b, JsonArray entries)
    {
        var keys = FindMatchKeys(a, b);
        if (keys is null)
        {
            return false;
        }

        // Identities are unique per FindMatchKeys, so this maps each B element by its identity.
        var bIndexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var j = 0; j < b.Count; j++)
        {
            bIndexByKey[CompositeKeyOf(b[j], keys)!] = j;
        }

        var matchedB = new HashSet<int>();
        for (var i = 0; i < a.Count; i++)
        {
            if (entries.Count >= MaxEntries)
            {
                return true;
            }
            var value = CompositeKeyOf(a[i], keys)!;
            if (bIndexByKey.TryGetValue(value, out var j))
            {
                matchedB.Add(j);
                DiffNode($"{path}[{j}]", a[i], true, b[j], true, entries, strict: false);
            }
            else
            {
                Add(entries, Entry($"{path}[{i}]", "FieldRemoved", a[i], null));
            }
        }
        for (var j = 0; j < b.Count; j++)
        {
            if (!matchedB.Contains(j))
            {
                Add(entries, Entry($"{path}[{j}]", "FieldAdded", null, b[j]));
            }
        }
        return true;
    }

    /// <summary>
    /// Order-insensitive diff for arrays with no usable identity field: every element that has an
    /// EXACTLY equal counterpart on the other side is paired off (duplicates included, one for one),
    /// and only the leftovers are reported. A pure reorder therefore yields no entries at all.
    ///
    /// Leftovers can no longer be equal to each other — an equal pair would have been matched — so
    /// they are resolved exactly as the LCS resolves a fully unmatched run: paired positionally and
    /// recursed (an item edited in place), then the remainder reported Removed / Added.
    ///
    /// Returns false when the two sides share no element at all, leaving the LCS to do its job.
    /// </summary>
    private static bool TryDiffArrayByEquality(string path, JsonArray a, JsonArray b, JsonArray entries)
    {
        if (a.Count == 0 || b.Count == 0 || a.Count > MaxArrayLcs || b.Count > MaxArrayLcs)
        {
            return false;
        }

        var takenB = new bool[b.Count];
        var leftA = new List<int>();
        var matched = 0;

        for (var i = 0; i < a.Count; i++)
        {
            var j = IndexOfEqual(b, takenB, a[i]);
            if (j < 0)
            {
                leftA.Add(i);
                continue;
            }
            takenB[j] = true;
            matched++;
        }

        if (matched == 0)
        {
            return false; // nothing in common — the LCS alignment is the better description
        }

        var leftB = new List<int>();
        for (var j = 0; j < b.Count; j++)
        {
            if (!takenB[j])
            {
                leftB.Add(j);
            }
        }

        var paired = Math.Min(leftA.Count, leftB.Count);
        for (var k = 0; k < paired; k++)
        {
            DiffNode($"{path}[{leftB[k]}]", a[leftA[k]], true, b[leftB[k]], true, entries, strict: false);
        }
        for (var k = paired; k < leftA.Count; k++)
        {
            Add(entries, Entry($"{path}[{leftA[k]}]", "FieldRemoved", a[leftA[k]], null));
        }
        for (var k = paired; k < leftB.Count; k++)
        {
            Add(entries, Entry($"{path}[{leftB[k]}]", "FieldAdded", null, b[leftB[k]]));
        }
        return true;
    }

    /// <summary>First index of an unclaimed element of <paramref name="b"/> equal to <paramref name="item"/>, or -1.</summary>
    private static int IndexOfEqual(JsonArray b, bool[] taken, JsonNode? item)
    {
        for (var j = 0; j < b.Count; j++)
        {
            if (!taken[j] && JsonNode.DeepEquals(b[j], item))
            {
                return j;
            }
        }
        return -1;
    }

    /// <summary>
    /// The identity to match the two arrays on (one field, or a two-field composite), or null.
    /// Requires: both arrays non-empty and every element an object.
    ///
    /// Pass 1 — single field: candidates are the first element's properties whose name ends with one
    /// of <see cref="MatchKeySuffixes"/>, holding a NON-EMPTY primitive value, UNIQUE within each
    /// array, on ALL elements of both sides. Among them, the one whose values OVERLAP the most across
    /// the two arrays wins (ties → suffix priority) — a churning GUID (zero overlap) is never picked
    /// over a stable domain key like <c>DTCCode</c>.
    ///
    /// Pass 2 — composite: when no single field qualifies (e.g. DTCCodes repeats the same DTCCode per
    /// CodeType), each candidate is paired with every other primitive property of the element; the
    /// composite must be unique per array and overlap across the two — yielding e.g.
    /// (DTCCode, CodeType) as the identity.
    /// </summary>
    private static string[]? FindMatchKeys(JsonArray a, JsonArray b)
    {
        if (a.Count == 0 || b.Count == 0)
        {
            return null;
        }
        foreach (var item in a.Concat(b))
        {
            if (item is not JsonObject)
            {
                return null;
            }
        }

        var first = (JsonObject)a[0]!;
        string[]? bestKeys = null;
        var bestOverlap = 0;
        var bestPriority = int.MaxValue;

        void Consider(string[] keys, int priority)
        {
            if (!TryGetKeyValues(a, keys, out var valuesA) || !TryGetKeyValues(b, keys, out var valuesB))
            {
                return;
            }
            var overlap = valuesA.Count(valuesB.Contains);
            if (overlap == 0)
            {
                return; // no shared identities → this key can't pair the two sides
            }
            if (overlap > bestOverlap || (overlap == bestOverlap && priority < bestPriority))
            {
                bestKeys = keys;
                bestOverlap = overlap;
                bestPriority = priority;
            }
        }

        // Pass 1: a single identity field.
        foreach (var property in first)
        {
            var priority = SuffixPriority(property.Key);
            if (priority >= 0)
            {
                Consider([property.Key], priority);
            }
        }
        if (bestKeys is not null)
        {
            return bestKeys;
        }

        // Pass 2: identity candidate + one discriminating primitive (e.g. DTCCode + CodeType).
        foreach (var property in first)
        {
            var priority = SuffixPriority(property.Key);
            if (priority < 0)
            {
                continue;
            }
            foreach (var other in first)
            {
                if (!string.Equals(other.Key, property.Key, StringComparison.Ordinal) && other.Value is JsonValue)
                {
                    Consider([property.Key, other.Key], priority);
                }
            }
        }
        return bestKeys;
    }

    /// <summary>Priority index of the first suffix the property name ends with, or -1 (not a key candidate).</summary>
    private static int SuffixPriority(string propertyName)
    {
        for (var i = 0; i < MatchKeySuffixes.Length; i++)
        {
            if (propertyName.EndsWith(MatchKeySuffixes[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Collects every element's identity for <paramref name="keys"/>. False when any element misses a
    /// key, holds a non-primitive value, the PRIMARY key (first) is empty, or an identity repeats
    /// (ambiguous).
    /// </summary>
    private static bool TryGetKeyValues(JsonArray array, string[] keys, out HashSet<string> values)
    {
        values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in array)
        {
            var composite = CompositeKeyOf(item, keys);
            if (composite is null)
            {
                return false;
            }
            if (!values.Add(composite))
            {
                return false; // duplicate identity → ambiguous, don't key-match
            }
        }
        return true;
    }

    /// <summary>Separator between the parts of a composite identity (never appears in field text).</summary>
    private const char CompositeSeparator = '\u001F';

    /// <summary>
    /// An element's identity as canonical text — the keys' values joined. Null when any key is missing
    /// or non-primitive, or when the PRIMARY key's value is empty/null (an empty identity can't match).
    /// </summary>
    private static string? CompositeKeyOf(JsonNode? item, string[] keys)
    {
        if (item is not JsonObject obj)
        {
            return null;
        }
        var parts = new string[keys.Length];
        for (var k = 0; k < keys.Length; k++)
        {
            if (!obj.TryGetPropertyValue(keys[k], out var value) || value is not JsonValue primitive)
            {
                return null;
            }
            var text = primitive.ToJsonString();
            if (k == 0 && text is "\"\"" or "null")
            {
                return null;
            }
            parts[k] = text;
        }
        return string.Join(CompositeSeparator, parts);
    }

    /// <summary>Positional (index-by-index) array diff — the fallback for very large arrays.</summary>
    private static void DiffArrayByIndex(string path, JsonArray a, JsonArray b, JsonArray entries, bool strict)
    {
        var max = Math.Max(a.Count, b.Count);
        for (var i = 0; i < max; i++)
        {
            var aHas = i < a.Count;
            var bHas = i < b.Count;
            DiffNode($"{path}[{i}]", aHas ? a[i] : null, aHas, bHas ? b[i] : null, bHas, entries, strict);
        }
    }

    /// <summary>
    /// Array-element equality for the LCS. JsonNode.DeepEquals ignores object-key order; in strict
    /// mode two items are equal only when their key order matches too (compared via their text).
    /// </summary>
    private static bool ItemEquals(JsonNode? a, JsonNode? b, bool strict) =>
        strict
            ? string.Equals(a?.ToJsonString(), b?.ToJsonString(), StringComparison.Ordinal)
            : JsonNode.DeepEquals(a, b);

    private static JsonArray KeyList(IEnumerable<string> keys) => new(keys.Select(key => (JsonNode?)JsonValue.Create(key)).ToArray());

    /// <summary>Adds an entry unless the safety cap has been reached.</summary>
    private static void Add(JsonArray entries, JsonObject entry)
    {
        if (entries.Count < MaxEntries)
        {
            entries.Add(entry);
        }
    }

    private static JsonObject Entry(string path, string changeType, JsonNode? oldValue, JsonNode? newValue) =>
        new()
        {
            ["path"] = path,
            ["changeType"] = changeType,
            ["old"] = oldValue?.DeepClone(),
            ["new"] = newValue?.DeepClone(),
        };

    private static JsonNode? ToValue(string? text) => text is null ? null : JsonValue.Create(text);

    private static JsonNode? TryParse(string? body)
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
}
