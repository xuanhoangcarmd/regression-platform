/**
 * Small JSON helpers for the Expected Response editor: a simple JSONPath subset
 * (`$.a.b[0].c`, `$.Items[*].Value`, `$['odd key']`), immutable set/remove, a flattened tree for
 * rendering, and a leaf-level diff against the original body.
 */

export type JsonSegment = string | number | '*';

export type JsonNodeKind = 'object' | 'array' | 'value';

/** One rendered line of the tree editor. */
export interface IJsonTreeRow {
  path: string;
  segments: JsonSegment[];
  depth: number;
  key: string;
  kind: JsonNodeKind;
  value: unknown;
  childCount: number;
}

export type JsonChange = 'changed' | 'added';

/** Leaf-level diff of an edited body against the original. */
export interface IJsonDiff {
  /** path → change, for leaves present in the edited body. */
  changes: Map<string, JsonChange>;
  /** Leaves of the original that no longer exist (path → original value). */
  removed: { path: string; value: unknown }[];
  /** path → original value, for changed leaves. */
  originalValues: Map<string, unknown>;
}

/** Parses `$.a.b[0]['x y'][*]` into segments. Throws on malformed input. */
export function parseJsonPath(path: string): JsonSegment[] {
  const text = path.trim();
  const segments: JsonSegment[] = [];
  let i = text.startsWith('$') ? 1 : 0;
  while (i < text.length) {
    const ch = text[i];
    if (ch === '.') {
      i++;
      const start = i;
      while (i < text.length && text[i] !== '.' && text[i] !== '[') {
        i++;
      }
      const key = text.slice(start, i);
      if (!key) {
        throw new Error(`Empty key at position ${start}.`);
      }
      segments.push(key === '*' ? '*' : key);
    } else if (ch === '[') {
      const end = text.indexOf(']', i);
      if (end < 0) {
        throw new Error('Missing "]".');
      }
      const inner = text.slice(i + 1, end).trim();
      if (inner === '*') {
        segments.push('*');
      } else if (/^\d+$/.test(inner)) {
        segments.push(Number(inner));
      } else if (/^'.*'$|^".*"$/.test(inner)) {
        segments.push(inner.slice(1, -1));
      } else {
        throw new Error(`Invalid index "[${inner}]".`);
      }
      i = end + 1;
    } else if (segments.length === 0) {
      // Allow a bare leading key: "DataItems[0].Value".
      const start = i;
      while (i < text.length && text[i] !== '.' && text[i] !== '[') {
        i++;
      }
      segments.push(text.slice(start, i));
    } else {
      throw new Error(`Unexpected "${ch}" at position ${i}.`);
    }
  }
  if (!segments.length) {
    throw new Error('Path is empty.');
  }
  return segments;
}

/** Segments → display path (`$.a.b[0]`; keys with special characters as `['…']`). */
export function formatJsonPath(segments: JsonSegment[]): string {
  return (
    '$' +
    segments
      .map((segment) =>
        typeof segment === 'number'
          ? `[${segment}]`
          : segment === '*'
            ? '[*]'
            : /^[A-Za-z_$][\w$]*$/.test(segment)
              ? `.${segment}`
              : `['${segment}']`,
      )
      .join('')
  );
}

/** Typed text → JSON value: valid JSON literal (number, true, null, "x", {…}) else a plain string. */
export function parseLiteral(text: string): unknown {
  const trimmed = text.trim();
  if (trimmed === '') {
    return '';
  }
  try {
    return JSON.parse(trimmed);
  } catch {
    return text;
  }
}

/** Value → editable text (strings unquoted, everything else as JSON). */
export function literalText(value: unknown): string {
  return typeof value === 'string' ? value : JSON.stringify(value);
}

function isContainer(value: unknown): value is Record<string, unknown> | unknown[] {
  return typeof value === 'object' && value !== null;
}

/** Flattens a JSON value into tree rows (pre-order). Collapsed paths hide their descendants. */
export function flattenJson(root: unknown, collapsed: ReadonlySet<string> = new Set()): IJsonTreeRow[] {
  const rows: IJsonTreeRow[] = [];
  const walk = (value: unknown, segments: JsonSegment[], key: string, depth: number): void => {
    const path = formatJsonPath(segments);
    if (Array.isArray(value)) {
      rows.push({ path, segments, depth, key, kind: 'array', value, childCount: value.length });
      if (!collapsed.has(path)) {
        value.forEach((child, index) => walk(child, [...segments, index], String(index), depth + 1));
      }
    } else if (isContainer(value)) {
      const entries = Object.entries(value);
      rows.push({ path, segments, depth, key, kind: 'object', value, childCount: entries.length });
      if (!collapsed.has(path)) {
        entries.forEach(([childKey, child]) => walk(child, [...segments, childKey], childKey, depth + 1));
      }
    } else {
      rows.push({ path, segments, depth, key, kind: 'value', value, childCount: 0 });
    }
  };
  walk(root, [], '$', 0);
  return rows;
}

/** Immutable set at an exact path (no wildcards). Creates the final key on an object. */
export function setAt(root: unknown, segments: JsonSegment[], value: unknown): unknown {
  if (!segments.length) {
    return value;
  }
  const [head, ...rest] = segments;
  if (Array.isArray(root) && typeof head === 'number') {
    const copy = [...root];
    copy[head] = setAt(copy[head], rest, value);
    return copy;
  }
  if (isContainer(root) && !Array.isArray(root) && typeof head === 'string') {
    return { ...root, [head]: setAt(root[head], rest, value) };
  }
  throw new Error(`Cannot set ${formatJsonPath(segments)}.`);
}

/** Immutable remove at an exact path (object key or array element). */
export function removeAt(root: unknown, segments: JsonSegment[]): unknown {
  if (!segments.length) {
    return root;
  }
  const [head, ...rest] = segments;
  if (Array.isArray(root) && typeof head === 'number') {
    return rest.length ? root.map((item, i) => (i === head ? removeAt(item, rest) : item)) : root.filter((_, i) => i !== head);
  }
  if (isContainer(root) && !Array.isArray(root) && typeof head === 'string' && head in root) {
    if (!rest.length) {
      const { [head]: _removed, ...others } = root;
      return others;
    }
    return { ...root, [head]: removeAt(root[head], rest) };
  }
  return root;
}

/** Expands wildcards into the concrete paths that exist in the value. */
export function resolvePaths(root: unknown, segments: JsonSegment[], allowMissingLeaf: boolean): JsonSegment[][] {
  const results: JsonSegment[][] = [];
  const walk = (value: unknown, index: number, acc: JsonSegment[]): void => {
    if (index === segments.length) {
      results.push(acc);
      return;
    }
    const segment = segments[index];
    const isLast = index === segments.length - 1;
    if (segment === '*') {
      if (Array.isArray(value)) {
        value.forEach((child, i) => walk(child, index + 1, [...acc, i]));
      } else if (isContainer(value)) {
        Object.entries(value).forEach(([key, child]) => walk(child, index + 1, [...acc, key]));
      }
      return;
    }
    if (Array.isArray(value) && typeof segment === 'number') {
      if (segment < value.length) {
        walk(value[segment], index + 1, [...acc, segment]);
      }
      return;
    }
    if (isContainer(value) && !Array.isArray(value) && typeof segment === 'string') {
      if (segment in value || (isLast && allowMissingLeaf)) {
        walk(value[segment], index + 1, [...acc, segment]);
      }
    }
  };
  walk(root, 0, []);
  return results;
}

/**
 * Applies a bulk action at a (wildcard) path. `set` updates existing leaves and creates the final
 * key on existing objects; `remove` deletes matches. Returns the new value + how many places changed.
 */
export function applyBulkAction(
  root: unknown,
  path: string,
  action: 'set' | 'remove',
  value: unknown,
): { result: unknown; count: number } {
  const segments = parseJsonPath(path);
  const targets = resolvePaths(root, segments, action === 'set');
  let result = root;
  if (action === 'set') {
    for (const target of targets) {
      result = setAt(result, target, value);
    }
  } else {
    // Remove deepest / highest array index first so earlier indexes stay valid.
    const ordered = [...targets].sort((a, b) => compareSegmentsDesc(a, b));
    for (const target of ordered) {
      result = removeAt(result, target);
    }
  }
  return { result, count: targets.length };
}

function compareSegmentsDesc(a: JsonSegment[], b: JsonSegment[]): number {
  for (let i = 0; i < Math.min(a.length, b.length); i++) {
    if (a[i] !== b[i]) {
      return typeof a[i] === 'number' && typeof b[i] === 'number' ? (b[i] as number) - (a[i] as number) : 0;
    }
  }
  return b.length - a.length;
}

function leaves(root: unknown): Map<string, unknown> {
  const map = new Map<string, unknown>();
  for (const row of flattenJson(root)) {
    // Empty containers count as leaves so adding/removing them shows up.
    if (row.kind === 'value' || row.childCount === 0) {
      map.set(row.path, row.value);
    }
  }
  return map;
}

/** Leaf-level diff of the edited body against the original. */
export function diffJson(original: unknown, edited: unknown): IJsonDiff {
  const before = leaves(original);
  const after = leaves(edited);
  const changes = new Map<string, JsonChange>();
  const originalValues = new Map<string, unknown>();
  for (const [path, value] of after) {
    if (!before.has(path)) {
      changes.set(path, 'added');
    } else if (JSON.stringify(before.get(path)) !== JSON.stringify(value)) {
      changes.set(path, 'changed');
      originalValues.set(path, before.get(path));
    }
  }
  const removed = [...before].filter(([path]) => !after.has(path)).map(([path, value]) => ({ path, value }));
  return { changes, removed, originalValues };
}

/** Parses JSON text; returns null value + message when invalid. */
export function tryParseJson(text: string): { value: unknown; error: string | null } {
  try {
    return { value: JSON.parse(text), error: null };
  } catch (error) {
    return { value: null, error: error instanceof Error ? error.message : 'Invalid JSON.' };
  }
}
