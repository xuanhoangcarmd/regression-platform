import { Injectable, signal } from '@angular/core';

import { StorageConstants } from '@core/constants/storage.const';

import { ILiveDataContextRule } from '../models/live-data-context.model';

/** One parsed path segment: an object key or an array index (`'*'` = any index, only in patterns). */
type PathSegment = { key: string } | { index: number | '*' };

/** Which response a context value is read from, per the diff entry's change type. */
type ResponseSide = 'old' | 'new';

/**
 * Context-field rules for the Live Data Result export, saved per browser in localStorage, plus the
 * matching / lookup logic that turns a changed path like `$.Dtcs[3].Definition` into `DTC: C0750`.
 */
@Injectable({ providedIn: 'root' })
export class LiveDataContextService {
  private readonly segmentPattern = /\.([^.[\]]+)|\[(\d+|\*)\]/g;

  /** The saved rules (read once from localStorage). */
  readonly rules = signal<ILiveDataContextRule[]>(this.load());

  /** Saves the rules (blank targets / fields dropped) to localStorage and the {@link rules} signal. */
  save(rules: ILiveDataContextRule[]): ILiveDataContextRule[] {
    const cleaned = rules
      .map((rule) => ({
        target: rule.target.trim(),
        fields: rule.fields.map((field) => field.trim()).filter(Boolean),
      }))
      .filter((rule) => rule.target && rule.fields.length);
    this.rules.set(cleaned);
    try {
      localStorage.setItem(StorageConstants.liveDataContextRules, JSON.stringify(cleaned));
    } catch {
      // localStorage may be unavailable (private mode) — the rules still apply to this export.
    }
    return cleaned;
  }

  /**
   * The export column names for the rules' fields — one per distinct label (the field's last key,
   * e.g. `DTC` / `DTCCode`), in rule order. Fields sharing a label share one column.
   */
  columnLabels(rules: readonly ILiveDataContextRule[]): string[] {
    const labels = new Set<string>();
    for (const rule of rules) {
      for (const field of rule.fields) {
        const parsed = this.parse(field);
        if (parsed) {
          labels.add(this.label(parsed, field));
        }
      }
    }
    return [...labels];
  }

  /**
   * The context values for one diff entry, keyed by column label (see {@link columnLabels}); empty when
   * no rule matches. Values come from the new response for changed/added entries and from the old one
   * for removed entries (the diff path indexes that side), falling back to the other side. Several
   * different values for one label are joined with ` · `.
   */
  resolve(
    rules: readonly ILiveDataContextRule[],
    diffPath: string,
    changeType: string,
    oldResponse: unknown,
    newResponse: unknown,
  ): Map<string, string> {
    const values = new Map<string, string[]>();
    const path = this.parse(diffPath);
    if (!path) {
      return new Map();
    }
    const side: ResponseSide = changeType === 'FieldRemoved' ? 'old' : 'new';
    const [first, second] = side === 'old' ? [oldResponse, newResponse] : [newResponse, oldResponse];

    for (const rule of rules) {
      const pattern = this.parse(rule.target);
      const match = pattern ? this.match(pattern, path) : null;
      if (!pattern || !match) {
        continue;
      }
      for (const field of rule.fields) {
        const fieldPath = this.resolveField(field, pattern, match);
        if (!fieldPath) {
          continue;
        }
        let value = this.read(first, fieldPath);
        if (value === undefined) {
          value = this.read(second, fieldPath);
        }
        const label = this.label(fieldPath, field);
        const text = value === undefined ? '(n/a)' : this.text(value);
        const list = values.get(label) ?? [];
        if (!list.includes(text)) {
          list.push(text);
        }
        values.set(label, list);
      }
    }
    return new Map([...values].map(([label, list]) => [label, list.join(' · ')]));
  }

  /** Parses `$.A.B[0].C` (leading `$` optional) into segments; null when it isn't a path. */
  private parse(path: string): PathSegment[] | null {
    let text = path.trim();
    if (text.startsWith('$')) {
      text = text.slice(1);
    }
    if (text && !text.startsWith('.') && !text.startsWith('[')) {
      text = `.${text}`;
    }
    const segments: PathSegment[] = [];
    let consumed = 0;
    for (const m of text.matchAll(this.segmentPattern)) {
      if (m.index !== consumed) {
        return null;
      }
      consumed += m[0].length;
      if (m[1] !== undefined) {
        segments.push({ key: m[1] });
      } else {
        segments.push({ index: m[2] === '*' ? '*' : Number(m[2]) });
      }
    }
    return consumed === text.length ? segments : null;
  }

  /**
   * Matches a diff path against a target pattern and returns the indexes taken by each `[*]`.
   * The diff path may also stop at a `[*]` item of the pattern — a whole array item added/removed
   * (e.g. `$.Dtcs[3]` for target `$.Dtcs[*].Definition`) still gets its context.
   */
  private match(pattern: PathSegment[], path: PathSegment[]): number[] | null {
    if (path.length > pattern.length) {
      return null;
    }
    if (path.length < pattern.length) {
      const last = pattern[path.length - 1];
      if (!last || !('index' in last) || last.index !== '*') {
        return null;
      }
    }
    const captured: number[] = [];
    for (let i = 0; i < path.length; i++) {
      const p = pattern[i];
      const s = path[i];
      if ('key' in p) {
        if (!('key' in s) || p.key.toLowerCase() !== s.key.toLowerCase()) {
          return null;
        }
      } else {
        if (!('index' in s) || typeof s.index !== 'number') {
          return null;
        }
        if (p.index === '*') {
          captured.push(s.index);
        } else if (p.index !== s.index) {
          return null;
        }
      }
    }
    return captured;
  }

  /**
   * The concrete path of a context field: absolute (`$…`) fields get the captured indexes in place of
   * their `[*]`s; relative fields are appended to the target's last `[*]` item (or its parent object).
   */
  private resolveField(field: string, pattern: PathSegment[], captured: number[]): PathSegment[] | null {
    const absolute = field.trim().startsWith('$');
    const parsed = this.parse(field);
    if (!parsed) {
      return null;
    }
    let base: PathSegment[] = [];
    if (!absolute) {
      const lastStar = pattern.map((s) => 'index' in s && s.index === '*').lastIndexOf(true);
      base = lastStar >= 0 ? pattern.slice(0, lastStar + 1) : pattern.slice(0, -1);
    }
    let next = 0;
    const resolved: PathSegment[] = [];
    for (const segment of [...base, ...parsed]) {
      if ('index' in segment && segment.index === '*') {
        if (next >= captured.length) {
          return null;
        }
        resolved.push({ index: captured[next++] });
      } else {
        resolved.push(segment);
      }
    }
    return resolved;
  }

  /** Reads a value at a concrete path (keys case-insensitive as a fallback); undefined when missing. */
  private read(root: unknown, path: PathSegment[]): unknown {
    let current: unknown = root;
    for (const segment of path) {
      if (current === null || typeof current !== 'object') {
        return undefined;
      }
      if ('index' in segment) {
        if (!Array.isArray(current) || typeof segment.index !== 'number') {
          return undefined;
        }
        current = current[segment.index];
      } else {
        const record = current as Record<string, unknown>;
        if (segment.key in record) {
          current = record[segment.key];
        } else {
          const key = Object.keys(record).find((k) => k.toLowerCase() === segment.key.toLowerCase());
          current = key === undefined ? undefined : record[key];
        }
      }
      if (current === undefined) {
        return undefined;
      }
    }
    return current;
  }

  /** Label for a context value: the field's last key (`DTC`), else the field as typed. */
  private label(path: PathSegment[], field: string): string {
    const lastKey = [...path].reverse().find((s): s is { key: string } => 'key' in s);
    return lastKey?.key ?? field.trim();
  }

  private text(value: unknown): string {
    if (value === null) {
      return 'null';
    }
    return typeof value === 'string' ? value : JSON.stringify(value);
  }

  private load(): ILiveDataContextRule[] {
    try {
      const raw = localStorage.getItem(StorageConstants.liveDataContextRules);
      const parsed: unknown = raw ? JSON.parse(raw) : [];
      if (!Array.isArray(parsed)) {
        return [];
      }
      return parsed
        .filter((r): r is ILiveDataContextRule => typeof r?.target === 'string' && Array.isArray(r?.fields))
        .map((r) => ({ target: r.target, fields: r.fields.filter((f): f is string => typeof f === 'string') }));
    } catch {
      return [];
    }
  }
}
