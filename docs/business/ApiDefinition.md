# Purpose

Defines APIs available for regression testing.

---

# API Definition

An API Definition contains:

- Name
- Route
- Method
- Description
- Project

Examples:

- GET /fixes
- POST /compare
- GET /diagnostic-report

---

# Rules

- API Definitions do not store test data — test data belongs to the Request Pool.
- API Definitions are reusable.
- Multiple Requests may reference the same API.
