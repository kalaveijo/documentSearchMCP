# 0001: Controller-based REST API host

## Status

Accepted

## Context

The application needs a REST API host and an initial endpoint that indicates
whether it can handle HTTP requests. It previously ran as a console application
and did not host HTTP endpoints.

## Decision

Use the ASP.NET Core Web SDK and `WebApplication` to host a controller-based API.
Register controllers with `AddControllers` and map them with `MapControllers`.
Keep controllers in `App/Controllers/`.

Expose `GET /v1/info` through an API controller. A successful response is HTTP
200 with an empty body. This is a liveness signal only: it does not check
PostgreSQL, Ollama, or any other dependency and does not indicate readiness.

Use ASP.NET Core's listener defaults and configuration rather than hard-coding
an address. The endpoint does not require authentication. The application does
not force HTTPS redirection; deployment-specific access controls and TLS are
separate decisions.

The existing MSTest project sends HTTP requests to a separately started
application. This exercises the real host without an additional test package,
but requires the application to be running before the test suite is executed.

## Alternatives

- **Minimal APIs:** Suitable for small APIs, but controllers were selected to
  organize this and future API endpoints consistently.
- **ASP.NET Core health-check middleware:** Useful for registered dependency
  checks and readiness, but unnecessary for the requested liveness-only response.
  Dependency health checks can be considered separately when those dependencies
  are implemented.

## Consequences

- The application remains running to serve requests instead of exiting after
  printing a greeting.
- A 200 response confirms only that this request was handled; it does not
  guarantee that other application features or dependencies are healthy.
- Deployments must deliberately configure listening addresses, authentication,
  and TLS as appropriate. The unauthenticated endpoint should be exposed only
  where intended.
- Integration tests depend on an externally running, compatible test instance.

## References

- [Create web APIs with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0)
- [Tutorial: Create a controller-based web API](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0)
- [Configure Kestrel endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
