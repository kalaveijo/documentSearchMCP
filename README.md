# Overview and features
This project is an MCP tool and a REST API with following features:
- Semantically search documentation
- Serve documents
- Ingest, chunk and store documents

# Technologies
- .NET 10
- Entity Framework
- PGvector
- Ollama with embeddinggemma model

Entity Framework, PGvector, and Ollama integration are planned and are not used
by the current API.

# REST API

`GET /v1/info` returns HTTP 200 with an empty body when the application handles
the request. This is a liveness check, not a readiness or dependency health
check; it does not check PostgreSQL or Ollama.

The endpoint has no authentication. Configure access and TLS for the deployment
as appropriate. Without endpoint configuration, Kestrel listens at
`http://localhost:5000`; use `--urls` or `ASPNETCORE_URLS` to configure the
address.

## Run and test

From the repository root, restore and build the solution:

```sh
dotnet restore documentSearchMCP.slnx
dotnet build documentSearchMCP.slnx --no-restore
```

In the first terminal, start the application and wait for it to listen:

```sh
dotnet run --project App/App.csproj --no-build --no-launch-profile -- --urls http://127.0.0.1:5000
```

In a second terminal, run the tests against the running application:

```sh
dotnet test Tests/Tests.csproj --no-build
```

The tests default to `http://127.0.0.1:5000`. To use another test instance, set
`DOCUMENT_SEARCH_TEST_BASE_URL` to its HTTP or HTTPS origin before running the
tests. The test run fails if the application is not reachable. Stop the
application with Ctrl+C when finished.

# Project structure
- /App folder contains the main .NET application project
- /App/Controllers contains REST API controllers
- /Common contains library project for code, that is used by all projects inside this solution.
- /Tests contains the MSTest project, including HTTP integration tests.
- /ADR contains architecture decision records.

# Devcontainer
This project is primarily developed in a development container. Its images and docker compose contain all tools and services required for development. Devcontainer uses Github copilot sandbox features.

The devcontainer starts PostgreSQL and Ollama. Ollama pulls the `embeddinggemma` model during initial creation and stores it in a named volume, so normal stops and restarts do not require downloading it again. Removing the `ollama-data` volume causes the model to be downloaded again. The Ollama API is available to the app at `http://ollama:11434`; it is not published to the host.