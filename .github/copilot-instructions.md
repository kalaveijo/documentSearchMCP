# Copilot Cloud Agent Guide

Trust this guide for routine work and search only when it is incomplete or contradicted by the change you are making. This is a small, early-stage .NET solution for an MCP tool and REST API intended to semantically search, serve, ingest, chunk, and store documentation. The README names Entity Framework, PGvector, and Ollama (`embeddinggemma`) as intended technologies, but none are implemented or referenced by a project yet. Do not assume database, embedding, MCP, or HTTP behavior exists until the relevant change adds it. Modify copilot-instructions.md only when necessary to reflect changes in the project structure or workflow.

## Toolchain and workflow

- Work from the repository root. All projects target `net10.0`; use a .NET 10 SDK. `global.json` selects the `Microsoft.Testing.Platform` test runner but does **not** pin an SDK version.
- No Node, Python, .NET workload, environment variable, database, or Ollama setup is required for the current restore, build, test, run, or publish workflows.
- Always restore after cloning or changing project/package references, then use the no-restore validation commands below:

  ```powershell
  dotnet restore documentSearchMCP.slnx
  dotnet build documentSearchMCP.slnx --no-restore
  dotnet test Tests/Tests.csproj --no-restore
  ```

The test command runs one MSTest test through Microsoft.Testing.Platform. A clean-first `dotnet test Tests/Tests.csproj` also restored and built transitively, so it is a valid concise check when a prior restore is unavailable.

- Run the current console app after a build with `dotnet run --project App/App.csproj --no-build`; it intentionally prints `Hello, World!` and exits. It is not an HTTP service yet.
- Publish with `dotnet publish App/App.csproj --no-restore`. It produces the default Release output under `App/bin/Release/net10.0/publish/`.
- `dotnet format documentSearchMCP.slnx --verify-no-changes --no-restore` completed successfully. It is the only available formatting-style check; there is no `.editorconfig`, StyleCop, analyzer configuration, or separate lint command.
- VS Code defines `build`, `publish`, and `watch` tasks in `.vscode/tasks.json`. The equivalent watch command is `dotnet watch run --project App/App.csproj`; it was verified to start, print `Hello, World!`, and wait for file changes. Stop it with Ctrl+C.
- `dotnet clean documentSearchMCP.slnx` is safe for returning to a clean build-output state.

## Layout and current architecture

- `documentSearchMCP.slnx` contains three projects: `App/App.csproj`, `Common/Common.csproj`, and `Tests/Tests.csproj`.
- `App/` is the executable and references `Common/`. `App/Program.cs` is the top-level entry point and currently contains only `Console.WriteLine("Hello, World!");`.
- `Common/` is the shared `net10.0` class library. `Common/Class1.cs` is an empty starter class.
- `Tests/` references both App and Common. `Tests/Tests.csproj` uses `MSTest.Sdk/4.0.2`; `Test1.cs` has one empty, passing `[TestMethod]`; `MSTestSettings.cs` enables method-level parallelization. Add behavior tests here with normal MSTest attributes.
- `README.md` is the concise product intent and documents the three project folders. Root also contains `global.json` and a standard Visual Studio `.gitignore`.
- `.vscode/launch.json` builds then launches `App/bin/Debug/net10.0/App.dll`. Keep that target in sync if the App target framework or output changes.
- `.devcontainer/` supplies the preferred development environment: .NET 10 on Ubuntu Noble plus a `pgvector/pgvector:pg18-trixie` PostgreSQL service (`postgres`/`postgres`, database `postgres`). The app joins the database network; no ports are forwarded and no post-create command runs. Database use is not currently required by application code or tests. `docker compose -f .devcontainer/docker-compose.yml config` succeeds, but Docker warns that the Compose `version` attribute is obsolete.

## Change and validation expectations

- There are no checked-in GitHub Actions, other CI pipelines, scripts, pre-commit hooks, or custom validation configuration. Replicate the restore/build/test commands above before proposing a change; additionally run the app or publish command when touching the executable or packaging.
- Keep project references directional: App references Common; Tests reference App and Common. Add NuGet packages explicitly to the owning `.csproj` when implementing a technology only mentioned in the README.
- Build artifacts (`bin/`, `obj/`), test results, and local `.env` files are ignored. Do not commit them.
- The existing test suite is a scaffold, so a passing test run establishes toolchain health rather than meaningful feature coverage. Add focused tests with new behavior.
