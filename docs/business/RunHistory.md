# Purpose

Stores historical execution results.

---

# Stored Data

For every request execution:

- Request Id
- Response Body
- Response Hash
- Status Code
- Duration
- Executed Date

---

# Rules

- History must never be modified.
- Run results are immutable.
- New executions create new records.
