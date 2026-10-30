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

Ollama is used to create embeddings. Entity Framework and PGvector integration
are planned and are not used by the current API.

# REST API

`GET /v1/info` returns HTTP 200 with an empty body when the application handles
the request. This is a liveness check, not a readiness or dependency health
check; it does not check PostgreSQL or Ollama.

The endpoint has no authentication. Configure access and TLS for the deployment
as appropriate. Without endpoint configuration, Kestrel listens at
`http://localhost:5000`; use `--urls` or `ASPNETCORE_URLS` to configure the
address.

### Draft ingestion

`POST /v1/ingest` accepts a JSON object with a nonempty `text` string:

```sh
curl -X POST http://127.0.0.1:5000/v1/ingest \
  -H 'Content-Type: application/json' \
  -d '{"text":"A document to ingest."}'
```

For example, the response might be:

```json
{"embeddingSize":768,"embeddingCount":1}
```

`embeddingSize` is the number of dimensions in each vector, and
`embeddingCount` is the number of vectors created. The dimension depends on
the configured model. This draft creates embeddings but does not store or
return the text or vectors.

The client splits text into ordered chunks of at most 2,048 Unicode scalar values,
preferring whitespace boundaries without discarding text. It uses Ollama's
native embedding API with truncation disabled. If a chunk exceeds the model's
token context, the client splits it further and retries. This is a basic
character-based splitter, not a semantic or tokenizer-aware RAG chunker.

The endpoint returns HTTP 400 for invalid or blank text, HTTP 502 for an Ollama
failure or invalid embedding response, and HTTP 504 when an Ollama request
times out. A successful response means that every chunk was embedded.

There is no application-defined total text limit and the endpoint disables
Kestrel's request-body size limit. The request JSON is nevertheless buffered
in memory, and runtime, memory, network, and proxy limits still apply. The
endpoint has no authentication and can consume substantial resources; restrict
it to trusted callers and configure access controls, TLS, and infrastructure
limits appropriate to the deployment.

### Ollama configuration

The following configuration keys can be supplied through normal ASP.NET Core
configuration, including environment variables:

| Key | Environment variable | Default |
| --- | --- | --- |
| `Ollama:BaseUrl` | `Ollama__BaseUrl` | `http://ollama:11434` |
| `Ollama:Model` | `Ollama__Model` | `embeddinggemma` |
| `Ollama:TimeoutSeconds` | `Ollama__TimeoutSeconds` | `100` |

The base URL must be an HTTP or HTTPS origin without a path, query, or
credentials. The timeout applies to each upstream embedding request, not the
entire ingestion request. Ollama must be reachable and the selected model must
be installed before invoking the ingestion endpoint.

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
tests. The HTTP integration tests require the application and Ollama to be
reachable with the selected model installed. The suite also includes
deterministic client and endpoint tests using a simulated embedding service,
including a Kestrel request above its default body-size limit. Stop the
application with Ctrl+C when finished.

# Project structure
- /App folder contains the main .NET application project
- /App/Controllers contains REST API controllers
- /App/Clients contains the Ollama embedding client
- /Common contains library project for code, that is used by all projects inside this solution.
- /Tests contains the MSTest project, including HTTP integration tests.
- /ADR contains architecture decision records.

# Devcontainer
This project is primarily developed in a development container. Its images and docker compose contain all tools and services required for development. Devcontainer uses Github copilot sandbox features.

The devcontainer starts PostgreSQL and Ollama. Ollama pulls the `embeddinggemma` model during initial creation and stores it in a named volume, so normal stops and restarts do not require downloading it again. Removing the `ollama-data` volume causes the model to be downloaded again. The Ollama API is available to the app at `http://ollama:11434`; it is not published to the host.