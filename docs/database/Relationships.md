# Purpose

Define foreign key relationships. Reflects the **actual** `RegressionDB` schema.

---

# Foreign Keys

| Child table | FK column | References | Constraint |
|-------------|-----------|------------|------------|
| ApiRequest | `ApiId` | Api(Id) | `FK_ApiRequest_Api` |
| RunResponse | `RunId` | Run(Id) | `FK_RunResponse_Run` |
| RunResponse | `RequestId` | ApiRequest(Id) | `FK_RunResponse_Request` |
| CompareResult | `RunAId` | Run(Id) | `FK_Compare_RunA` |
| CompareResult | `RunBId` | Run(Id) | `FK_Compare_RunB` |
| CompareDetail | `CompareResultId` | CompareResult(Id) | `FK_CompareDetail_Result` |
| CompareDetail | `RequestId` | ApiRequest(Id) | `FK_CompareDetail_Request` |

No foreign keys are declared on `CompareJob` (`RunAId`, `RunBId`, `CompareResultId` are
logical references only) or on `CompareIgnoreRule`.

---

# Diagram

```text
Api ──ApiId──> ApiRequest ──RequestId──┬──> RunResponse
                                        └──> CompareDetail
Run ──RunId──> RunResponse
Run ──RunAId / RunBId──> CompareResult ──CompareResultId──> CompareDetail
```
