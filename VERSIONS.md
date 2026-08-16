# HSB Versions
This file documents the evolution of the HSB framework. Recent versions are described in detail to support migrations and maintenance; historical versions are summarized from the information available in the repository, examples, and previous roadmap.
HSB is still pre-1.0: until a stable release, APIs may change. The "Breaking changes" and "Migration notes" sections should always be checked before upgrading.
## 0.0.22
Current version indicated in `HSB/Properties/AssemblyInfo.cs`.
### Focus
- Release dedicated to the first real security/runtime hardening layer on top of the modern streaming server.
- Introduces a middleware pipeline, request hardening controls, advanced throttling, and stronger authentication/runtime protections while preserving the current routing APIs.
### Added
- HTTP `QUERY` support following RFC 10008 across request parsing, minimal routes, controller attributes, and OpenAPI 3.2 output.
- `Configuration.Query(...)`, `[Query(...)]`, and `HttpMethod.Query`.
- Request middleware pipeline through `Configuration.Use(...)`.
- Request-scoped middleware context with:
  - `Request`
  - `Response`
  - `Configuration`
  - per-request `Items`
- New `Security` configuration group with:
  - response security headers
  - request validation rules
  - token-bucket rate limiting
- Per-response header mutation APIs on `Response`:
  - `SetHeader(...)`
  - `RemoveHeader(...)`
  - `TryGetHeader(...)`
- Request-scoped item storage on `Request`.
- Request authentication context support through `AuthContext` and request helpers for retrieving the authenticated principal.
- Structured authentication registrations with identity metadata for:
  - bearer tokens
  - API keys
  - basic users
- WebSocket hardening options for:
  - allowed origins
  - required origin header
  - allowed subprotocols
  - per-IP connection caps
### Changed
- QUERY requests now require `Content-Type` and retain their request body for route handlers.
- Generated OpenAPI documents now use OpenAPI 3.2 so QUERY operations can be represented natively.
- HTTP requests now execute through a compiled middleware pipeline before route dispatch.
- Minimal API delegates and controller handlers can now complete asynchronously when they return `Task` or `ValueTask`.
- Request parsing can now reject invalid host/path/query/cookie patterns earlier when `Security.Validation` is enabled.
- Built-in rate limiting can now emit `Retry-After` and `X-RateLimit-*` headers.
- WebSocket admission can now enforce stricter origin/subprotocol rules before handshake completion.
- The authentication module now:
  - stores authenticated identity data on the request
  - supports role-based authorization through `[RequireAuth(Roles = ...)]`
  - writes proper `401` vs `403` responses
  - emits `WWW-Authenticate` challenges when configured
- The TokenAuthentication example now demonstrates a real configured auth flow instead of dynamically minting a placeholder token inside the login endpoint.
### Fixed
- Fixed `[Options(...)]` incorrectly registering a POST route.
- Invalid parsed requests now preserve their specific 4xx status and reason in the server response.
- Fixed minimal-route async handlers previously being invoked without awaiting completion.
- Fixed authentication behavior so endpoint protection is no longer a simple boolean example with no authenticated request context.
- Fixed authorization responses so insufficient roles now produce `403 Forbidden` instead of generic unauthorized behavior.
### Security
- Optional hardening headers now cover common response safety controls such as `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and optional HSTS.
- Optional request validation can now enforce host allow-lists and stricter request-target hygiene.
- Optional per-IP throttling can now reject abusive request rates before route execution.
- Basic credential checks now use fixed-time comparison.
- WebSocket handshake validation can now reject unknown origins, missing origins, unsupported subprotocols, and excessive per-IP fan-out.
### Validation
- Verified builds:
  - `HSB/HSB.csproj`
  - `Examples/HelloWorld/HelloWorld.csproj`
  - `Examples/StreamingResponse/StreamingResponse.csproj`
  - `Examples/TokenAuthentication/TokenAuthentication.csproj`
  - `Runner/Runner.csproj`
### Known limitations
- Middleware is append-only in the current API: there is no built-in remove/disable handle once registered.
- Legacy module interceptors remain part of the runtime for backward compatibility and still coexist with the new middleware pipeline.
- Endpoint-aware middleware metadata is not yet exposed as a dedicated post-routing middleware stage.
## 0.0.21
Previous version.
### Focus
- Release dedicated to memory discipline and internal performance cleanup on top of the 0.0.20 streaming architecture.
- No new end-user features were introduced in this release.
### Added
- Shared pooled byte-buffer helper for internal hot paths.
- Route dispatch indexing by HTTP method.
- Route metadata caching for parameterized routes.
### Changed
- The HTTP request reader now uses pooled buffers for header accumulation and read scratch space.
- The chunked request decoder now reuses internal buffers instead of allocating fresh arrays while parsing.
- Multipart parsing now uses pooled buffers for file-copy and field-accumulation loops.
- Manual TLS transport read/write bridges now avoid unnecessary per-call array allocations where possible.
- Fixed-length request-body reads now reuse the direct-length fast path instead of always building through `MemoryStream`.
- Response file/stream sending now reuses pooled buffers and avoids extra per-chunk copy allocations.
- Route matching now avoids per-request LINQ pipelines and regex construction for parameterized path checks.
### Fixed
- Reduced allocation churn in request, upload, chunked, and response streaming paths.
- Reduced avoidable copies in chunked output and streamed file responses.
- Reduced route-dispatch overhead for larger controller/minimal-route sets.
### Known limitations
- The repository sandbox still does not provide full benchmark and soak-test evidence for the optimization claims.
- Response transport internals still rely on the existing `Response` write path rather than the shared request transport abstraction.
## 0.0.20
Previous version.
### Focus
- Release dedicated to the first true streaming architecture pass.
- The server now uses a single modern HTTP request pipeline instead of maintaining the previous duplicated body-reading flow.
- Chunked request-body handling is now supported in the modern parser, including multipart uploads.
### Added
- Unified transport abstraction for plain sockets, `SslStream`, and the manual TLS handler.
- Streaming HTTP request reader with a dedicated request-body stream abstraction.
- Chunked request-body decoder with bounded size enforcement and trailer consumption.
- Multipart parsing entrypoints that accept a generic `Stream`, enabling parser reuse beyond temp-file-backed request bodies.
- Regression tests for:
  - buffered body prefix reads
  - chunked body reads
  - chunked multipart reads
  - rejection of conflicting `Content-Length` + `Transfer-Encoding: chunked`
### Changed
- Multipart uploads on the modern request path are now parsed directly from the incoming body stream instead of first spooling the full request body to an intermediate temp file.
- `Request` can now accept a pre-built `MultiPartFormData` instance injected by the server pipeline.
- HTTP request parsing now treats `Content-Length` as optional when `Transfer-Encoding: chunked` is present.
- Legacy duplicated request-reading helpers in `Server` were removed in favor of the unified reader/transport pipeline.
### Fixed
- Fixed truncated fixed-length body handling so disconnects during body reads are not silently accepted as complete requests.
- Fixed request-reader validation for duplicate non-repeatable headers and conflicting transfer semantics.
- Improved cleanup for partially parsed multipart uploads when parsing fails mid-stream.
### Known limitations
- Request-side backpressure remains coarse-grained and still relies on the upload semaphore rather than a deeper streaming scheduler.
- Response streaming already supports chunked output, but response transport internals are not yet fully migrated to the shared transport abstraction.
- Heavy external stress/benchmark runs still need to be rerun outside the repository sandbox.
## 0.0.19
Previous version.
### Focus
- Release dedicated to HTTP/WebSocket stability and classic multipart upload hardening.
- No new end-user features were introduced in this release.
- Advanced upload streaming, NDJSON upload, and experimental chunked upload parsing are postponed to 0.0.20+.
### Added
- Centralized `Http` limits:
  - max body size
  - max headers
  - max header size
  - request line size
  - header/body/read timeouts
- Centralized `Upload` limits:
  - max concurrent uploads
  - temp path
  - max file size
  - form field size
  - upload timeout
- Structured upload and WebSocket lifecycle logging:
  - `[UPLOAD][START]`
  - `[UPLOAD][PROGRESS]`
  - `[UPLOAD][DONE]`
  - `[UPLOAD][ERROR]`
  - `[WS][CONNECT]`
  - `[WS][DISCONNECT]`
  - `[WS][ERROR]`
### Changed
- Multipart parsing now relies on bounded request buffering and temp-file-backed file parts instead of full in-memory payload duplication.
- File responses and `Response.SendFile(...)` now stream from disk instead of loading the whole file into RAM.
- HTTP request reading now separates bounded header parsing from body handling and enforces early rejection for invalid or oversized requests.
- Request parsing rejects duplicate non-repeatable headers such as `Content-Length`, `Connection`, `Upgrade`, and WebSocket handshake headers.
- Upload concurrency now uses server-side backpressure instead of unbounded parallel body buffering.
- WebSocket idle handling now includes heartbeat ping/pong and bounded pending-buffer checks.
### Fixed
- Reduced `Broken pipe` / disconnect noise on HTTP writes and WebSocket frame sends.
- Fixed the configuration parser bug where `RequestMaxSize` was incorrectly read from `Port`.
- Fixed duplicated header emission in HTTP responses and corrected the HTTP status line reason phrase.
- Improved cleanup of temp upload artifacts and request-scoped multipart resources.
### Security
- Invalid or abusive headers are rejected earlier with bounded parser limits.
- Chunked request bodies and postponed experimental upload modes are rejected instead of entering unstable paths.
- Multipart file MIME validation can now reject malformed values before route execution.
### Validation
- Runtime regression tests cover WebSocket framing, configuration parsing, duplicate-header rejection, classic multipart parsing, and invalid MIME rejection.
- Heavy 1GB/2GB, 10/50 concurrent, and 1h+ soak benchmarks still need to be rerun outside the repository sandbox after this code update.
## 0.0.18
Previous version.
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
- The core project target is `net1## 0.0`; consumers of the library should align SDK/runtime versions.
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

## 0.0.15

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

## 0.0.14

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

## 0.0.13

Added

* SSL/TLS improvements.
* Extended support for .p12/.pkcs12 certificates.
* Preparation for local certificate debugging.

Changed

* SslConfiguration became the central point for HTTPS configuration.
* Consolidated HTTP -> HTTPS redirect handling.

Security

* Obsolete TLS versions considered deprecated.
* Support for revocation checks and client certificate settings.

## 0.0.12

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

## 0.0.11

Release available in Releases/ as HSB_v## 0.0.11_ALPHA.

Added

* Initial SSL/TLS support.
* SslConfiguration.
* Dual-port HTTP/HTTPS mode.
* Optional redirect for insecure requests.

Changed

* TLS integration into the connection accept cycle.

Known limitations

* The old roadmap indicated the custom TLS implementation was still a work in progress.

## 0.0.10

Release available in Releases/ as HSB_v## 0.0.10.

### Added

* Initial WebSocket support.
* Legacy WebSocket examples.

### Changed

* Extended the server beyond simple HTTP request/response.

### Deprecated later

* The original WebSocket model has been replaced by the WebSocketConnection system.

## 0.0.9

### Added

* Form data.
* File uploads.
* Multipart components.

### Changed

* Request gained broader functionality for body parsing and uploads.

## 0.0.8

### Added

* HTTP authentication.
* Basic/Bearer auth support and authentication components.
* Authentication examples.

## 0.0.7

### Changed

* Refactor/cleanup planned in the historical roadmap.

### Notes

* The previous roadmap indicated that this version could potentially be skipped.

## 0.0.6

### Added

* HTTP session implementation.
* Cookie/session token handling.

## 0.0.5 / ## 0.0.5 RC

### Added

* Improved debugging.
* Initial development and diagnostic utilities.

## 0.0.1 - ## 0.0.4

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

Towards 1.0

Before a 1.0 release, the project should define:

* stable public APIs;
* deprecation policies;
* supported runtime matrix;
* automated test suite;
* reproducible benchmarks;
* complete documentation for controllers, WebSocket, streaming, TLS, and modules.
