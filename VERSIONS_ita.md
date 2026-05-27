# HSB Versions
This file documents the evolution of the HSB framework. Recent versions are described in detail to support migrations and maintenance; historical versions are summarized from the information available in the repository, examples, and previous roadmap.
HSB is still pre-1.0: until a stable release, APIs may change. The "Breaking changes" and "Migration notes" sections should always be checked before upgrading.
## 0.0.18
Current version indicated in `HSB/Properties/AssemblyInfo.cs`.
### Breaking changes
- Completely removed the servlet-style API:
  - `Servlet`
  - `[Binding]`
  - `AssociateFile`
  - inheritance-based routing with overridden `GET()`, `POST()`, etc.
- Examples and the Runner were migrated to controllers or direct routes on `Configuration`.
- The public WebSocket model now remains endpoint-based only through `WebSocketConnection`, `config.WebSocket(...)`, and `[Ws]`.
### Migration notes
- Use `[Controller]` with `[Get]`, `[Post]`, `[Route]`, etc. for structured routes.
- Use `config.Get(...)`, `config.Post(...)`, etc. for minimal routes.
- Use `Response.SendHtmlFile(...)` inside a route instead of `AssociateFile`.
## 0.0.17
Previous version.
### Added
- New modern controller system with attributes:
  - `[Controller("/path")]`
  - `[Get]`, `[Post]`, `[Put]`, `[Delete]`, `[Patch]`, `[Head]`, `[Options]`
  - `[Route("/path", HttpMethod.X)]`
- Support for parameterized routes with `:name` segments.
- `Request` and `Response` injection into controller methods.
- `Request` and `Response` injection into controller fields.
- Support for typed parameters through `[NamedParameter]`.
- Minimal API/Express-style routing through:
  - `config.Get(...)`
  - `config.Post(...)`
  - `config.Put(...)`
  - `config.Delete(...)`
  - `config.Patch(...)`
  - `config.Options(...)`
  - `config.WebSocket(...)`
- New WebSocket model with `WebSocketConnection`.
- WebSocket controller endpoints through `[Ws("/path")]`.
- WebSocket lifecycle:
  - `OnOpen`
  - `OnMessage`
  - `OnClose`
  - `OnError`
- Synchronous/asynchronous WebSocket send for text and binary.
- WebSocket broadcast per endpoint:
  - `Broadcast`
  - `BroadcastAsync`
  - `BroadcastExceptSelf`
  - `BroadcastExceptSelfAsync`
- Header and query string snapshots in WebSocket connections.
- Chunked HTTP streaming:
  - `Response.InitStream(...)`
  - `Response.AddStreamChunk(...)`
  - `Response.EndStream()`
- Examples and stress endpoints for NDJSON streaming.
- Stress tests with realistic endpoints in `Experiments/StressTest`.
### Changed
- `Servlet` is now deprecated with the `[Obsolete]` attribute.
- Modern routing is resolved before static file fallback.
- Minimal API routes are handled uniformly with controller routes.
- The HTTP parser is more defensive against malformed request lines, query strings, headers, cookies, auth, and bodies.
- HTTP headers are handled with a case-insensitive dictionary.
- Parameters and cookies are handled with case-insensitive dictionaries.
- `Response` creates snapshots of global headers/cookies during header generation.
- The server sets receive/send timeouts on accepted sockets.
- The server reads headers until the `\r\n\r\n` delimiter before constructing `Request`.
- The WebSocket runtime uses locks/semaphores for handler registration and frame sending.
- WebSocket connection handling uses concurrent structures.
- TLS/SSL has been consolidated into `SslConfiguration`, with support for both native and experimental HSB handlers.
### Fixed
- Improved resilience against malformed requests.
- Reduced crash risk caused by invalid headers/cookies/formats.
- Improved handling of slow or incomplete connections.
- Improved stability under connection storm scenarios.
- Improved thread safety for WebSocket broadcasting and frame sending.
- Improved security of Basic/Bearer auth parsing.
- Improved socket shutdown handling in TLS, WebSocket, and invalid request paths.
### Security
- Anti-Slowloris hardening through timeouts and header limits.
- Maximum header size limit.
- Maximum request line length limit.
- Maximum number of headers limit.
- Early connection shutdown for requests that do not complete headers.
- Global CORS support.
- IP allowlist/banlist support through the `Filter` module.
- TLS 1.0 and TLS 1.1 deprecated through `DeprecatedTLSVersionException`.
### Performance
- Recently observed tests:
  - around 18k req/s on a minimal endpoint;
  - around 8k req/s on medium-sized JSON;
  - improved concurrency stress stability;
  - improved Slowloris resilience;
  - improved connection storm stability.
- Results are indicative and do not replace reproducible benchmarks with fixed environment, tooling, and configuration.
### Deprecated
- `Servlet` is still available but no longer recommended.
- The legacy servlet-style WebSocket model should be replaced with `WebSocketConnection`.
- Previous documentation based on `ProcessGet()`/`ProcessPOST()` is outdated: the real legacy API used `GET()`, `POST()`, etc.
### Breaking changes
- New controllers do not inherit from `Servlet`.
- New code should use controller attributes instead of `[Binding]`.
- Old servlet-style WebSocket examples no longer represent the preferred model.
- The core project target is `net10.0`; consumers of the library should align SDK/runtime versions.
### Migration notes
#### Servlet -> Controller
Before:
```csharp
[Binding("/hello")]
public class HelloServlet : Servlet
{
    public HelloServlet(Request req, Response res) : base(req, res)
    {
    }
    public override void GET()
    {
        res.Send("hello");
    }
}

After:

[Controller("/hello")]
public class HelloController
{
    [Get("/")]
    private void Get(Response res)
    {
        res.Send("hello");
    }
}

Legacy WebSocket -> WebSocketConnection

Before: WebSocket endpoints modeled as legacy servlet/classes.

After:

config.WebSocket("/ws", socket =>
{
    socket.OnMessage(msg => socket.Send(msg.Text));
});

or:

[Controller("/realtime")]
public class RealtimeController
{
    [Ws("/chat")]
    private void Chat(WebSocketConnection socket)
    {
        socket.OnMessage(msg => socket.BroadcastExceptSelf(msg.Text));
    }
}
```
## 0.0.16

Intermediate release/refactor reconstructed from repository changes.

### Added

* Initial modern routing APIs inspired by minimal APIs/Express.
* Internal Configuration events for route registration.
* Improvements to request-level modules.
* Preparation for the new WebSocket router.

### Changed

* Rationalized route resolution in Server.
* Greater separation between delegate mapping, controllers, and static fallback.
* Improved Request/Response lifecycle.

### Fixed

* Fixes for global header and global cookie handling.
* Fixes for routes and base parameter injection.

0.0.15

Intermediate release/refactor reconstructed from current features.

### Added

* Initial foundations of the attribute-based Controller system.
* Dedicated HTTP attributes for controllers.
* Support for custom routes through [Route].
* Initial OpenAPI integrations through attributes.

### Changed

* Gradual migration from the servlet model to controllers.
* Initial examples in Examples/ControllerExample.

### Deprecated

* Conceptual deprecation of the Servlet model for new projects began.

0.0.14

### Added

* Improvements to the HTTP parser.
* Better handling of body, form data, multipart, and uploads.
* Improvements to the session/cookie lifecycle.

### Changed

* Request parsing became more tolerant of malformed input.
* Infrastructure became more prepared for modern request handlers.

### Fixed

* Fixes for invalid requests.
* Fixes for malformed cookies and abnormal headers.

0.0.13

### Added

* SSL/TLS improvements.
* Extended support for .p12/.pkcs12 certificates.
* Preparation for local certificate debugging.

### Changed

* SslConfiguration became the central point for HTTPS configuration.
* Consolidated HTTP -> HTTPS redirect handling.

### Security

* Obsolete TLS versions considered deprecated.
* Support for revocation checks and client certificate settings.

0.0.12

### Added

* Networking and socket handling refactor.
* Initial structured protections against slow connections.
* Improved server concurrency.

### Changed

* More controlled request reading.
* Preparation for future anti-Slowloris limits.

### Fixed

* Improved stability under load.
* Reduced race conditions in the request/response path.

0.0.11

Release available in Releases/ as HSB_v0.0.11_ALPHA.

### Added

* Initial SSL/TLS support.
* SslConfiguration.
* Dual-port HTTP/HTTPS mode.
* Optional redirect for insecure requests.

### Changed

* TLS integration into the connection accept cycle.

### Known limitations

* The old roadmap indicated the custom TLS implementation was still a work in progress.

0.0.10

### Release available in Releases/ as HSB_v0.0.10.

### Added

* Initial WebSocket support.
* Legacy WebSocket examples.

### Changed

* Extended the server beyond simple HTTP request/response.

Deprecated later

* The original WebSocket model has been replaced by the WebSocketConnection system.

0.0.9

### Added

* Form data.
* File uploads.
* Multipart components.

### Changed

* Request gained broader functionality for body parsing and uploads.

0.0.8

### Added

* HTTP authentication.
* Basic/Bearer auth support and authentication components.
* Authentication examples.

0.0.7

### Changed

* Refactor/cleanup planned in the historical roadmap.

### Notes

* The previous roadmap indicated that this version could potentially be skipped.

0.0.6

### Added

* HTTP session implementation.
* Cookie/session token handling.

0.0.5 / 0.0.5 RC

### Added

* Improved debugging.
* Initial development and diagnostic utilities.

0.0.1 - 0.0.4

### Added

* Initial HTTP server versions.
* Servlet-based routing.
* Initial Request and Response.
* Static files and default pages.
* Base structure of the HSB library.

Versioning roadmap

### Next areas to stabilize

* Controllers as the primary public API surface.
* Complete migration of legacy examples.
* Automated tests for parser, timeouts, WebSocket, and streaming.
* Reproducible benchmarks with scripts and declared environments.
* Consistent manual documentation in Documentation/ aligned with README and VERSIONS.md; docs/ remains generated Doxygen output.
* Compatibility and target framework policies.

### Towards 1.0

Before a 1.0 release, the project should define:

* stable public APIs;
* deprecation policies;
* supported runtime matrix;
* automated test suite;
* reproducible benchmarks;
* complete documentation for controllers, WebSocket, streaming, TLS, and modules.

