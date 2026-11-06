# Purpose

Prevent lost updates and corruption under concurrent regression runs and worker jobs.

# Rules

- Use optimistic concurrency for mutable tables via a `RowVersion ROWVERSION` (timestamp) column; check it on update.
- Append-only tables (`ApiRequest`, run results) avoid update conflicts by inserting new rows instead of updating.
- Keep transactions short; wrap only the writes that must succeed together.
- Use the appropriate isolation level (default `READ COMMITTED`); enable `READ_COMMITTED_SNAPSHOT` to reduce reader/writer blocking.
- Acquire locks/resources in a consistent order to avoid deadlocks; retry transient deadlock/timeout errors with backoff.
- Make worker/background operations idempotent so retries don't double-process a run or comparison.

# AI Generation Requirements

When generating data-mutation code, the AI **must**:

- Add and check a `RowVersion` column for optimistic concurrency on mutable tables.
- Keep transactions minimal and make worker operations idempotent.

The AI **must never**:

- Update append-only tables instead of inserting new rows.
- Hold long-running transactions or ignore concurrency conflicts.
