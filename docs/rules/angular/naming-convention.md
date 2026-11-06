# Purpose

Ensure consistent, predictable naming across the Angular application so files, classes, and
symbols are easy to locate and map to their role.

# Rules

- **Files**: kebab-case with role suffix (e.g. `compare-list-page.component.ts`, `compare-api.service.ts`, `run-history.model.ts`).
- **Directories**: kebab-case (e.g. `request-pool`, `run-history`, `compare-detail`).
- **Components**: PascalCase, suffix `Component` (e.g. `CompareListPageComponent`, `CompareSummaryCardComponent`).
- **Services**: PascalCase, suffix `Service` (e.g. `CompareService`, `ProjectApiService`, `NotificationService`).
- **Models / DTOs**: suffix `Model`, `Request`, or `Response` (e.g. `ProjectModel`, `CreateRunRequest`, `CompareSummaryResponse`).
- **Interfaces**: prefix `I` (e.g. `IProject`, `IPagedResult`, `ICompareSummary`).
- **Enums**: suffix `Enum` (e.g. `RunStatusEnum`, `CompareResultEnum`).
- **Constants**: suffix `Constants` (e.g. `ApiConstants`, `RouteConstants`, `StorageConstants`).
- **Stores**: suffix `Store` (e.g. `CompareStore`, `RunStore`).
- Use descriptive names; avoid abbreviations and generic names (e.g. `DataService`, `BaseComponent`).

# AI Generation Requirements

When generating Angular code, the AI **must**:

- Apply the correct suffix/prefix per symbol kind and use kebab-case file and directory names.
- Choose descriptive, role-revealing names.

The AI **must never**:

- Use abbreviations or generic names like `DataService`/`BaseComponent`.
- Mix casing styles or omit the required suffix/prefix conventions.
