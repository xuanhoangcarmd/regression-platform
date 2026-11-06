/** An API a rule applies to. */
export interface IIgnoreRuleApiRef {
  id: number;
  name: string;
}

/**
 * How a rule affects comparison:
 * - `Ignore`: drop the field at JsonPath before comparing (default).
 * - `ExtractRoot`: lift the subtree at JsonPath to the response root before comparing
 *   (e.g. unwrap an envelope like `$.data` so a wrapped response compares to an un-wrapped one).
 * - `TrimAfter`: compare a string value only up to the first `delimiter`
 *   (e.g. `?` so a signed URL compares without its per-call `Expires` / `Signature` query).
 */
export type IgnoreRuleType = 'Ignore' | 'ExtractRoot' | 'TrimAfter';

/** A compare rule (mirrors the CompareIgnoreRule table + its API links). */
export interface IIgnoreRule {
  id: number;
  jsonPath: string;
  description: string | null;
  enabled: boolean;
  ruleType: IgnoreRuleType;
  /** TrimAfter only: the value is compared up to (not including) this text. */
  delimiter: string | null;
  /** APIs the rule applies to. Empty = applies to all APIs. */
  apis: IIgnoreRuleApiRef[];
}

/** Create/update payload for a compare rule. */
export interface IIgnoreRuleWrite {
  jsonPath: string;
  description: string | null;
  enabled: boolean;
  ruleType: IgnoreRuleType;
  /** TrimAfter only (required there); null for the other types. */
  delimiter: string | null;
  /** APIs to apply to. Empty = all APIs. */
  apiIds: number[];
}
