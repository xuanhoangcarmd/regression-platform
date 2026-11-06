# Purpose

Help estimate the scale of the system so design and indexing decisions account for volume.

---

# Estimation

| Factor | Value |
|--------|-------|
| APIs (`Api`) | 50 |
| Requests per API (`ApiRequest`) | 10,000 |
| Run frequency (`Run`) | 1 sprint run / week |
| Responses per sprint (`RunResponse`) | ≈ 500,000 |
| Responses per year | ≈ 26 million |

---

# Implication

- `RunResponse` is the largest and fastest-growing table.
- Design for read-heavy access, hash-based comparison, partitioning, and archival (see `PartitionStrategy.md`).
