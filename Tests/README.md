# Tests

From the repository root, [build and start the application](../README.md#run),
then run the tests in another terminal:

```sh
dotnet test --project Tests/Tests.csproj --no-build
```

The full suite requires the running API and a reachable Ollama instance with
the configured model installed. The HTTP integration tests use
`http://127.0.0.1:5000` by default; set `DOCUMENT_SEARCH_TEST_BASE_URL` to
another HTTP or HTTPS origin to test a different instance. Client and host
tests also cover behavior using fakes without these external services.
