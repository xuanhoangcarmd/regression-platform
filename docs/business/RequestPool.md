# Purpose

Stores reusable API requests.

---

# Business Rules

Each request belongs to:

- Project
- API Definition

Each request has:

- Title
- Request Body
- Headers
- Query Parameters

---

# Versioning

- Requests are immutable.
- When a request changes, create a new request record.
- Do not overwrite history.

---

# Usage

Requests are selected when executing Regression Runs.
