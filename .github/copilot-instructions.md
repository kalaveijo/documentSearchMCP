# Copilot Cloud Agent Guide

Trust this guide for routine work and search only when it is incomplete or contradicted by the change you are making. This is a small, early-stage .NET solution for an MCP tool and REST API intended to semantically search, serve, ingest, chunk, and store documentation. The REST API currently provides only a liveness endpoint. The README names Entity Framework, PGvector, and Ollama (`embeddinggemma`) as intended technologies, but none are implemented or referenced by a project yet. Do not assume database, embedding, or MCP behavior exists until the relevant change adds it. Modify copilot-instructions.md only when necessary to reflect changes in the project structure or workflow.

## Toolchain and workflow

- Work from the repository root. All projects target `net10.0`; use a .NET 10 SDK. `global.json` selects the `Microsoft.Testing.Platform` test runner but does **not** pin an SDK version.
- No Node, Python, .NET workload, database, or manual Ollama setup is required for the current restore, build, run, test, or publish workflows. Integration tests require the application to be running separately.
- Always restore after cloning or changing project/package references, then build:

  ```powershell
  dotnet restore documentSearchMCP.slnx
  dotnet build documentSearchMCP.slnx --no-restore
  ```

- Start the API with `dotnet run --project App/App.csproj --no-build --no-launch-profile -- --urls http://127.0.0.1:5000`. In another terminal, run `dotnet test Tests/Tests.csproj --no-build` through Microsoft.Testing.Platform. The tests fail if the API is unavailable. Set `DOCUMENT_SEARCH_TEST_BASE_URL` to an HTTP or HTTPS origin to target a different test instance.

- Publish with `dotnet publish App/App.csproj --no-restore`. It produces the default Release output under `App/bin/Release/net10.0/publish/`.
- Run `dotnet format documentSearchMCP.slnx --verify-no-changes --no-restore` for the available formatting-style check; there is no `.editorconfig`, StyleCop, analyzer configuration, or separate lint command.
- VS Code defines `build`, `publish`, and `watch` tasks in `.vscode/tasks.json`. The equivalent watch command is `dotnet watch run --project App/App.csproj`. Stop it with Ctrl+C.
- `dotnet clean documentSearchMCP.slnx` is safe for returning to a clean build-output state.

## Layout and current architecture

- `documentSearchMCP.slnx` contains three projects: `App/App.csproj`, `Common/Common.csproj`, and `Tests/Tests.csproj`.
- `App/` is the ASP.NET Core executable and references `Common/`. `App/Program.cs` initializes the controller host; `App/Controllers/` contains the API controllers. `GET /v1/info` returns an empty 200 response as a liveness signal, not a dependency health check.
- `Common/` is the shared `net10.0` class library. `Common/Class1.cs` is an empty starter class.
- `Tests/` references both App and Common. `Tests/Tests.csproj` uses `MSTest.Sdk/4.0.2`; `MSTestSettings.cs` enables method-level parallelization. `InfoEndpointTests.cs` tests the separately running API. Add behavior tests here with normal MSTest attributes.
- `README.md` describes the project and run/test workflow. `ADR/` contains architecture decision records. Root also contains `global.json` and a standard Visual Studio `.gitignore`.
- `.vscode/launch.json` builds then launches `App/bin/Debug/net10.0/App.dll`. Keep that target in sync if the App target framework or output changes.
- `.devcontainer/` supplies the preferred development environment: .NET 10 on Ubuntu Noble, PostgreSQL with pgvector, and CPU-only Ollama. Ollama's `embeddinggemma` model is pulled by a one-shot Compose service and retained in the `ollama-data` named volume. The app shares the database network namespace and can reach Ollama at `http://ollama:11434`; no ports are forwarded and no post-create command runs. Database and Ollama are not required by the current application code or tests. The Compose `version` attribute is obsolete and may produce a warning.

## Change and validation expectations

- There are no checked-in GitHub Actions, other CI pipelines, scripts, pre-commit hooks, or custom validation configuration. Replicate the restore/build/test commands above before proposing a change; additionally run the app or publish command when touching the executable or packaging.
- Keep project references directional: App references Common; Tests reference App and Common. Add NuGet packages explicitly to the owning `.csproj` when implementing a technology only mentioned in the README.
- Build artifacts (`bin/`, `obj/`), test results, and local `.env` files are ignored. Do not commit them.
- The endpoint tests cover the HTTP contract, not external dependencies. Add focused tests with new behavior.
