# Feature Specification: Expected Run Generation

**Feature Directory**: `specs/011-expected-run-generation`
**Created**: 2026-11-06
**Status**: Withdrawn — implemented, then rolled back on 2026-11-06 (did not meet the requirement; see `database/20261106_RollbackExpectedFeatures.sql`). The Expected Responses editor is unaffected.

> Focus on **WHAT** and **WHY**, not HOW. Written for stakeholders, not implementers.

---

## Summary

A tester turns a trusted run of the previous sprint into an **Expected Run** for the new sprint by
applying a **Change Spec** — a reviewed list of structured changes (add / remove / rename / set a
field, optionally only when a condition holds) and **matchers** (what a value *should* look like
when its exact value cannot be known in advance) written from the sprint's tickets. Comparing the
real new-sprint run against the Expected Run then leaves only **unexpected** differences, which
answers the question today's compare cannot: *is this difference intended or a regression?*

---

## User Scenarios & Testing *(mandatory)*

### Primary User Flow

1. On the Runs page the tester opens a **Completed** run of the previous sprint (the *source run*)
   and chooses **Generate Expected Run**.
2. The tester creates a Change Spec for the sprint: enters the ticket key(s) and adds one or more
   operations using a guided form (template *Schema change* or *Conditional change*) or by editing
   the spec text directly. Each operation names the API(s) it applies to, the field path, the
   action, the value or matcher, and an optional condition.
3. The tester runs **Preview**: for every operation the system shows how many of the source run's
   requests it touches, a few before/after samples, and a warning for operations that touch no
   request.
4. The tester adjusts the spec until the preview looks right, then marks the spec **Approved**.
5. The tester chooses **Generate**. The system creates a new run (status Completed, type
   *Generated*) whose responses are the source run's responses with the spec applied; each
   touched response records which operation(s) changed it.
6. The tester executes the real new-sprint run as usual, then creates a compare with the
   Expected Run as Run A (baseline) and the real run as Run B.
7. In the compare results each request is classified: **Unchanged** (behaves as the tickets
   require), **Changed** (an unexpected difference — a regression candidate), or **Expectation
   failed** (a value exists where the spec said it should, but does not satisfy its matcher). The
   diff viewer shows which ticket/operation produced each expected value.
8. After review the Expected Run can be used as the source run for the next sprint.

### Acceptance Scenarios

| # | Given | When | Then |
|---|-------|------|------|
| 1 | A Completed source run with 1,500 responses and an approved spec that adds `ToolMilStatus` before `ToolLEDStatus` on API *GetMostLikelyFixForVehicle* with matcher `oneOf [ON, OFF, UNKNOWN, null]` | the tester generates the Expected Run | a new Generated run exists with 1,500 responses; every *GetMostLikelyFixForVehicle* response contains `ToolMilStatus` immediately before `ToolLEDStatus`; responses of other APIs are byte-identical to the source |
| 2 | The Expected Run from #1 and a real run where every `ToolMilStatus` is `ON` or `OFF` and nothing else differs | the two are compared (Expected Run as Run A) | every request is **Unchanged** |
| 3 | Same as #2 but one real response returns `ToolMilStatus = "BLINK"` | the two are compared | that request is **Expectation failed** with the field path, the matcher, and the actual value shown; all others are Unchanged |
| 4 | Same as #2 but one real response also lost the field `ToolLEDStatus` | the two are compared | that request is **Changed** (regression candidate) and the removed field is listed |
| 5 | A spec with operation *rename `FixParts[*].PartName` → `PartTerminologyName`* on three APIs | the tester previews | the preview shows, per API, the number of requests whose responses contain `FixParts` and a before/after sample; an API with no `FixParts` in any response shows a "touches 0 requests" warning |
| 6 | A conditional operation *set `LEDStatus = YELLOW` when `DTCs` has 2 or more items* | the Expected Run is generated | only responses with ≥ 2 DTCs are changed; the others keep their original `LEDStatus` |
| 7 | A spec in **Draft** status | the tester chooses Generate | generation is refused with a message that the spec must be approved first |
| 8 | A source run that is Running or Pending | the tester opens Generate Expected Run | the action is unavailable with a message that only finished runs can be used |
| 9 | A Generated run | the tester tries to Start, Stop, or Re-run a request on it | the actions are unavailable (a Generated run is never executed against an environment) |
| 10 | A Generated run used as Run A in a report export (Test Results / Live Data Result) | the report is exported | the Expected Result column holds the generated (expected) response and the sheet marks the run as Generated |

### Edge Cases

- **Operation touches no request**: allowed, shown as a warning in preview and recorded in the
  generated run's summary (ticket may target an API not present in the source run).
- **Conflicting operations** (two operations set the same path on the same request): the later
  operation in the spec wins; the preview flags the overlap.
- **Rename to an existing field name**: the existing field is overwritten; the preview flags it.
- **Insert-before/after an anchor field that is missing** in a given response: the field is
  appended at the end of the object for that response and the preview counts it separately.
- **Source responses that failed** (no body / error): copied as-is; operations are not applied;
  they compare as *Failed* exactly as today.
- **Source run has edited (expected) responses**: the tester chooses whether the generation
  starts from the original captured bodies or from the edited ones (default: edited when present).
- **Large source run** (≥ 10,000 responses): generation completes without the user keeping the
  page open; progress and completion are visible on the Runs page like a normal run.
- **Spec edited after generation**: already-generated runs are unaffected; the tester can
  generate again, producing a new Generated run.
- **Matcher on a value that is absent** in the real run: reported as **Changed** (field removed),
  not as Expectation failed.
- **Deleting a source run** that has Generated runs: refused while Generated runs reference it.
- **Order-sensitive compare** (strict order) against a Generated run: supported; insert-before/
  after operations define the expected key order.

---

## Functional Requirements *(mandatory)*

**Change Spec**

- **FR-1**: Users MUST be able to create, edit, approve, and archive a Change Spec within a project.
  A spec has a title, one or more ticket keys, a status (Draft, Approved, Archived), and an
  ordered list of operations.
- **FR-2**: Each operation MUST specify: the API(s) it applies to (one, several, or all), a field
  path (supporting nested fields and "every element of an array"), an action, and the ticket key
  it comes from. Supported actions: *set value*, *add field*, *remove field*, *rename field*,
  *insert field before/after an anchor field*, *map value* (old → new table), *replace text
  prefix*.
- **FR-3**: An operation MAY carry a **condition** that limits it to responses where a response
  field satisfies a comparison (equals, one of, count ≥ / ≤, regex, exists) or where a request
  input field satisfies such a comparison.
- **FR-4**: Where an exact value cannot be known, an *add* / *set* / *insert* operation MUST accept
  a **matcher** instead of a literal value. Supported matchers: *equals*, *one of*, *type is*,
  *not empty*, *regex*, *numeric range*, *any value*.
- **FR-5**: The system MUST validate a spec on save: unknown action, malformed path, matcher
  without a type, or an API not in the project is rejected with a field-level message.
- **FR-6**: Only an **Approved** spec can be used to generate; approving records who approved and
  when **(Planned — no user identity yet; the timestamp is recorded, the user is not)**.

**Preview**

- **FR-7**: Users MUST be able to preview an approved or draft spec against a chosen source run
  before generating. The preview MUST show, per operation: requests touched (count and percentage
  per API), up to three before/after samples, and warnings (zero matches, overlapping operations,
  missing anchor, overwrite of an existing field).
- **FR-8**: The preview MUST NOT create or modify any run.

**Generation**

- **FR-9**: Users MUST be able to generate an Expected Run from a source run that is Completed or
  Failed, in the same project, using an approved spec. Pending, Queued, Running, and Cancelled runs
  cannot be used.
- **FR-10**: The generated run MUST be a regular run of type **Generated**, status Completed, with
  the same dataset and the same set of requests as the source run, a title defaulting to
  "<source title> — Expected (<ticket keys>)", and a reference to both the source run and the spec.
- **FR-11**: Every response of the source run MUST appear in the generated run. Responses not
  touched by any operation MUST be identical to the source; touched responses MUST reflect every
  applicable operation in spec order. Status code, duration, and error of the source response are
  carried over unchanged.
- **FR-12**: The generated run MUST record, per response, which operation(s) touched it, and MUST
  store each matcher-based expectation (path, matcher, originating operation) so the compare can
  evaluate it.
- **FR-13**: Users MUST be able to choose whether generation starts from the source run's original
  captured responses or from its edited (expected) responses where they exist.
- **FR-14**: Generation of a large source run MUST run in the background with visible progress and
  MUST be cancellable; a cancelled generation leaves no partial run behind.
- **FR-15**: A Generated run MUST NOT be startable, stoppable, or re-runnable; it MUST be deletable
  (unless a compare or report references it, as for any run) and usable as either side of a
  compare, as a source run for a later generation, and in the Expected Responses editor.

**Compare integration**

- **FR-16**: When a compare's Run A is a Generated run, the compare MUST evaluate the run's stored
  expectations: for each expectation whose path exists on Run B, a value that **satisfies** the
  matcher counts as equal; a value that **does not** satisfy it yields the per-request result
  **Expectation failed**, reported with the path, the matcher, and the actual value.
- **FR-17**: All other differences MUST be classified exactly as today (Unchanged / Changed /
  Added / Removed / Failed), and existing compare options (ignore rules, cut-at rules, extract-root
  rules, ignore/strict order, original/expected baseline) MUST keep working unchanged.
- **FR-18**: The compare summary MUST show the count of *Expectation failed* requests, and the
  per-request list MUST be filterable by it.
- **FR-19**: In the diff viewer, a field set by the spec MUST show the ticket key and operation
  that produced it; a failed expectation MUST show the matcher next to the actual value.
- **FR-20**: Compares, reports, and exports that do not involve a Generated run MUST be unaffected.

**Reporting**

- **FR-21**: Report exports (Test Results, Live Data Result, Full API) MUST treat *Expectation
  failed* as a failed result and include the matcher text in the field-change rows; the run title
  and the Generated flag MUST be visible in the exported header or run information.

---

## Key Entities *(include if data is involved)*

| Entity | Maps to table | Notes |
|--------|---------------|-------|
| Change Spec | **(Planned)** `ChangeSpec` | Project-scoped; title, ticket keys, status (Draft / Approved / Archived), ordered operations (structured), approved timestamp. No `CreatedBy`/`ApprovedBy` until user identity exists. |
| Change Spec operation | **(Planned)** part of `ChangeSpec` | Stored with the spec; identified by its position; carries API scope, path, action, value or matcher, condition, ticket key. |
| Expected Run (Generated run) | `Run` | `RunType = Generated`; **(Planned)** `SourceRunId` → `Run`, **(Planned)** `ChangeSpecId` → `ChangeSpec`; same `DatasetId` as the source; `Status = Completed`. |
| Generated response | `RunResponse` | One per source response; same `RequestId`; body = source body after operations; status/duration/error copied; hashes recomputed. **(Planned)** record of touching operation(s) per response. |
| Expectation (matcher) | **(Planned)** `RunExpectation` | Belongs to a Generated run; `RequestId` (or API scope), field path, matcher, originating operation / ticket key. Evaluated by compare when the run is Run A. |
| Source run | `Run` | Any Completed/Failed run of the project; its edited responses (`RunResponseOverride`) may be used as the starting bodies. |
| Compare of an Expected Run | `CompareJob`, `CompareResult`, `CompareDetail` | `CompareDetail.Result` gains the value **Expectation failed**; `CompareResult` gains its count **(Planned column)**. |
| Report | `Report` | Unchanged structure; exports read the new result value. |

---

## Success Criteria *(mandatory)*

- **SC-1**: For a sprint with schema-type and conditional tickets (e.g. add a field with allowed
  values; rename fields inside an array; change a value under a condition), a tester can express
  the tickets as a spec, preview, and generate an Expected Run in under 30 minutes without
  editing any response by hand.
- **SC-2**: Comparing a real run against an Expected Run reports **zero** Changed / Expectation
  failed requests when the real run implements the tickets exactly, and reports **every**
  deliberately injected deviation (wrong value, missing field, extra field) as Changed or
  Expectation failed — verified on a seeded test set.
- **SC-3**: Generating an Expected Run from a 10,000-response source run completes in under 2
  minutes, and a compare against it is not slower than a compare between two captured runs of
  the same size.
- **SC-4**: Responses not touched by any operation are byte-identical to the source run's
  responses (100% of untouched responses, verified by hash).
- **SC-5**: Preview counts match the actual number of responses changed by generation (±0).
- **SC-6**: All existing compares, reports, and exports that do not use a Generated run produce
  identical results before and after this feature (regression suite passes unchanged).

---

## Scope

**In scope**
- Change Spec authoring (form templates *Schema change* and *Conditional change*, plus direct
  text editing), validation, approval, archive.
- Preview against a source run.
- Background generation of an Expected Run with per-response provenance and stored matchers.
- Compare integration: matcher evaluation, *Expectation failed* result, ticket/operation
  attribution in the diff viewer, summary count and filter.
- Report/export handling of the new result value.
- Guard rails for Generated runs (not executable; deletion rules).

**Out of scope (later phases)**
- Reading tickets from Jira or drafting a spec with AI (Phase 3).
- *Data refresh* template (allowed-change paths with thresholds) and *Localization* template
  (Phase 2).
- Creating new requests from ticket reproduction steps or changing request parameters (Phase 4).
- Project-wide invariants applied to every compare (e.g. "no 5xx", response-time ceilings).
- Lookup-table matchers (e.g. ACES id → part name).

---

## Assumptions

- A Generated run belongs to the same project and dataset as its source run; cross-project or
  cross-dataset generation is not needed.
- "Previous sprint run" is simply any finished run the tester picks; the system does not model
  sprints.
- Matchers are evaluated only when the Generated run is **Run A** (the baseline). Using a
  Generated run as Run B is allowed but behaves like a normal captured run.
- Key order produced by *insert before/after* matters only under strict-order compares; under
  ignore-order compares it is informational.
- Ticket keys are free text (e.g. `IABB-419`); the system does not validate them against Jira in
  this phase.
- The default starting bodies for generation are the source run's edited responses where they
  exist, otherwise the captured ones.
- Secrets that may appear in ticket text (API keys in sample requests) are never stored in a spec;
  the spec holds only paths, values, matchers, and ticket keys.

---

## Dependencies

- Existing run execution and storage (`Run`, `RunResponse`), including run types and the
  Expected Responses editor (`RunResponseOverride`).
- Existing compare pipeline and options (`CompareJob` baseline mode and order mode,
  `CompareIgnoreRule` kinds Ignore / ExtractRoot / TrimAfter).
- Report exports (Test Results, Live Data Result, Full API sheets).
- Schema changes listed as **(Planned)** above must be applied before the feature is usable.
