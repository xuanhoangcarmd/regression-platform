# Purpose

Define a partitioning strategy for the largest table, `RunResponse`.

---

# Strategy

Partition `RunResponse` by:

- `CreatedDate`

or

- `RunId`

---

# Benefits

- Faster compare
- Faster cleanup
- Better archive strategy

See `DataVolumeEstimation.md` for the scale that motivates partitioning.
