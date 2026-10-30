# Copilot Cloud Agent Guide

Trust this guide for routine work and search only when it is incomplete or contradicted by the change you are making. This is a small, early-stage .NET solution for an MCP tool and REST API intended to semantically search, serve, ingest, chunk, and store documentation. The REST API provides a liveness endpoint and a draft ingestion endpoint that creates Ollama embeddings without storing them. Entity Framework, PGvector, and MCP behavior are not implemented. Keep this guide concise and update it when project structure, workflows, or repository-wide documentation conventions change.

## Toolchain and workflow

- Work from the repository root. All projects target `net10.0`; use a .NET 10 SDK. `global.json` selects the `Microsoft.Testing.Platform` test runner but does **not** pin an SDK version.
- No Node, Python, .NET workload, or database is required for the current restore, build, run, test, or publish workflows. HTTP integration tests require the application and Ollama to be running, with the configured embedding model installed. The devcontainer provisions Ollama and its model.
- Always restore after cloning or changing project/package references, then build:

  ```powershell
  dotnet restore documentSearchMCP.slnx
  dotnet build documentSearchMCP.slnx --no-restore
  ```

- Start the API with `dotnet run --project App/App.csproj --no-build --no-launch-profile -- --urls http://127.0.0.1:5000`. In another terminal, run `dotnet test --project Tests/Tests.csproj --no-build` through Microsoft.Testing.Platform. HTTP integration tests fail if the API or Ollama is unavailable. Set `DOCUMENT_SEARCH_TEST_BASE_URL` to an HTTP or HTTPS origin to target a different test instance. See the [API guide](../App/README.md) for Ollama configuration.

- Publish with `dotnet publish App/App.csproj --no-restore`. It produces the default Release output under `App/bin/Release/net10.0/publish/`.
- Run `dotnet format documentSearchMCP.slnx --verify-no-changes --no-restore` for the available formatting-style check; there is no `.editorconfig`, StyleCop, analyzer configuration, or separate lint command.
- VS Code defines `build`, `publish`, and `watch` tasks in `.vscode/tasks.json`. The equivalent watch command is `dotnet watch run --project App/App.csproj`. Stop it with Ctrl+C.
- `dotnet clean documentSearchMCP.slnx` is safe for returning to a clean build-output state.

## Layout and current architecture

- `documentSearchMCP.slnx` contains three projects: `App/App.csproj`, `Common/Common.csproj`, and `Tests/Tests.csproj`.
- `App/` is the ASP.NET Core executable and references `Common/`. `App/Program.cs` initializes the controller host; `App/Controllers/` contains the API controllers and `App/Clients/` contains the native Ollama embedding client. `GET /v1/info` returns an empty 200 response as a liveness signal, not a dependency health check. `POST /v1/ingest` returns vector dimensions and chunk count without persisting data.
- `Common/` is the shared `net10.0` class library. `Common/Class1.cs` is an empty starter class.
- `Tests/` references both App and Common. `Tests/Tests.csproj` uses `MSTest.Sdk/4.5.2` and ASP.NET Core integration-testing tools; `MSTestSettings.cs` enables method-level parallelization. The HTTP integration tests use a separately running API and real Ollama; client and Kestrel host tests use fakes. Add behavior tests here with normal MSTest attributes.
- The [root README](../README.md) contains the application quickstart; [App/README.md](../App/README.md) covers API usage and configuration, and [Tests/README.md](../Tests/README.md) covers testing. `ADR/` contains architecture decision records. Root also contains `global.json` and a standard Visual Studio `.gitignore`.
- `.vscode/launch.json` builds then launches `App/bin/Debug/net10.0/App.dll`. Keep that target in sync if the App target framework or output changes.
- `.devcontainer/` supplies the preferred development environment: .NET 10 on Ubuntu Noble, PostgreSQL with pgvector, and CPU-only Ollama. Ollama's `embeddinggemma` model is pulled by a one-shot Compose service and retained in the `ollama-data` named volume. The app can reach Ollama at `http://ollama:11434`; no ports are configured for forwarding, and the post-create command installs and updates the local `dotnet-ef` tool. The database is not required by the current application code or tests. The Compose `version` attribute is obsolete and may produce a warning.

## Documentation

- Keep all documentation concise and relevant. READMEs should be short, task-oriented, and focused on essentials.
- Put application startup in the root README, API usage and configuration in the API guide, and test prerequisites and commands in the test guide. Link to details rather than duplicating them.
- Retain essential prerequisites, operational limitations, and safety warnings when condensing documentation.
- ADRs may be more detailed when explaining context, decisions, alternatives, and consequences.

## Change and validation expectations

- There are no checked-in GitHub Actions, other CI pipelines, scripts, pre-commit hooks, or custom validation configuration. Replicate the restore/build/test commands above before proposing a change; additionally run the app or publish command when touching the executable or packaging.
- Keep project references directional: App references Common; Tests reference App and Common. Add NuGet packages explicitly to the owning `.csproj` when implementing a technology only mentioned in the README.
- Build artifacts (`bin/`, `obj/`), test results, and local `.env` files are ignored. Do not commit them.
- The ingestion HTTP integration tests exercise the real Ollama dependency; deterministic client and host tests cover edge cases using fakes. Add focused tests with new behavior.
