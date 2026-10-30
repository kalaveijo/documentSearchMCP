# Document Search MCP

This .NET 10 project provides a REST API for liveness checks and draft document
ingestion. Ingestion creates Ollama embeddings without storing them. MCP,
semantic search, document serving, and persistence are planned.

## Run

Install the .NET 10 SDK, then run from the repository root:

```sh
dotnet restore documentSearchMCP.slnx
dotnet build documentSearchMCP.slnx --no-restore
dotnet run --project App/App.csproj --no-build --no-launch-profile -- --urls http://127.0.0.1:5000
```

In another terminal, check that the application is listening:

```sh
curl -i http://127.0.0.1:5000/v1/info
```

Ollama is not required to start the application, but ingestion requires a
running Ollama instance and its model. See the [API guide](App/README.md) for
configuration, including use outside the devcontainer.

## Devcontainer

Open the repository in its devcontainer to provision Ollama and the
`embeddinggemma` model. The model persists in the `ollama-data` volume. The
container also starts PostgreSQL, which the current application does not use.

## Documentation

- [API](App/README.md)
- [Tests](Tests/README.md)
- [Architecture decision records](ADR/)
