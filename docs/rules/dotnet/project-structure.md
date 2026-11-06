# Purpose

Define the backend project layout and feature-based organization of the Application layer.

# Rules

- Backend projects live under `src/`: `RegressionPlatform.Api`, `.Application`, `.Domain`, `.Infrastructure`, `.Compare`, `.Worker`, `.AI`, `.Shared`.
- The Application layer is organized by feature, each with `Commands/` and `Queries/` folders (e.g. `Features/Project/`, `Features/ApiDefinition/`).

# AI Generation Requirements

When generating code, the AI **must**:

- Place new code in the correct project per its responsibility.
- Organize Application code by feature with `Commands/` and `Queries/` subfolders.

The AI **must never**:

- Add cross-layer code to the wrong project or flatten features into shared folders.
