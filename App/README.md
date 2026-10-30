# REST API

[Start the application](../README.md#run) before calling the API.

- `GET /v1/info` returns an empty HTTP 200 response as a liveness check; it does
  not check Ollama or other dependencies.
- `POST /v1/ingest` accepts a nonblank `text` string and creates embeddings:

  ```sh
  curl -X POST http://127.0.0.1:5000/v1/ingest \
    -H 'Content-Type: application/json' \
    -d '{"text":"A document to ingest."}'
  ```

  Example response: `{"embeddingSize":768,"embeddingCount":1}`.
  `embeddingSize` is the vector dimension, which depends on the model;
  `embeddingCount` is the number of vectors created. The text and vectors are
  neither returned nor stored.

  Invalid input returns HTTP 400, an Ollama failure or invalid embedding
  response returns HTTP 502, and an Ollama request timeout returns HTTP 504.

The endpoints have no authentication. Ingestion has no application-defined
total text limit, disables Kestrel's request-body size limit, and buffers the
request JSON in memory. Restrict access to trusted callers and configure TLS
and infrastructure resource limits appropriate to the deployment.

## Ollama configuration

Ingestion requires Ollama to be reachable with the selected model installed
(for example, run `ollama pull embeddinggemma`). Configure these options through
ASP.NET Core configuration or environment variables:

| Key | Environment variable | Default |
| --- | --- | --- |
| `Ollama:BaseUrl` | `Ollama__BaseUrl` | `http://ollama:11434` |
| `Ollama:Model` | `Ollama__Model` | `embeddinggemma` |
| `Ollama:TimeoutSeconds` | `Ollama__TimeoutSeconds` | `100` |

The default address targets the devcontainer service. For Ollama running
locally outside the devcontainer, set `Ollama__BaseUrl` before starting the app:

```sh
export Ollama__BaseUrl=http://127.0.0.1:11434
```

The base URL must be an HTTP or HTTPS origin without a path, query, fragment,
or credentials. The timeout applies to each upstream embedding request.
