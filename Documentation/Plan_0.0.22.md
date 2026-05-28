HSB 0.0.22 Plan

Title: HSB 0.0.22 — Security Layer & Runtime Hardening

Status

Implemented in the current repository state as the release-closing basis for the 0.0.22 development milestone.

Purpose

Version 0.0.22 should make HSB meaningfully more production-ready without breaking the current routing model.
The primary goal is to introduce a real request middleware pipeline and opt-in runtime hardening controls while preserving the lightweight architecture established in 0.0.20 and optimized in 0.0.21.

Relationship with 0.0.21

Version 0.0.21 focused on allocation pressure and hot-path cleanup.
Version 0.0.22 builds on that work by adding security and request-governance features directly on the modern pipeline instead of reintroducing the older duplicated request architecture.

Current Strategy

* keep controller and minimal-route APIs stable
* keep new hardening features optional and configurable
* preserve low-allocation request handling where possible
* improve security posture before adding broader ecosystem features

Implemented Scope

In scope for 0.0.22:

* request middleware pipeline
* request-scoped middleware context
* response security headers
* request validation guards
* rate limiting
* stronger WebSocket handshake/runtime admission controls
* stronger authentication module behavior
* async handler correctness for minimal routes and controllers

Out of scope for 0.0.22:

* removal of legacy module interceptors
* endpoint-metadata-aware post-routing middleware stage
* auth token issuance framework
* external identity provider integrations
* benchmark-grade load validation outside the repository sandbox

Delivered Features

1. Middleware Pipeline

Delivered:

* `Configuration.Use(...)`
* request middleware delegates with `next()`
* per-request `RequestContext`
* compiled middleware chain reused across requests

Target outcome achieved:

* request cross-cutting logic can now be expressed in a modern async pipeline style
* routing APIs remain unchanged

2. Security Configuration Layer

Delivered:

* `Security.Headers`
* `Security.Validation`
* `Security.RateLimit`

Target outcome achieved:

* hardening behavior is opt-in and centralized
* security features do not force routing API changes

3. Request Validation

Delivered:

* optional host header enforcement
* optional host allow-listing
* optional path/query/cookie limits
* optional rejection of suspicious request targets

Target outcome achieved:

* malformed or unsafe requests can now be rejected earlier in request parsing

4. Rate Limiting

Delivered:

* per-IP token-bucket limiter
* optional `Retry-After`
* optional `X-RateLimit-*` headers
* optional application to WebSocket handshake requests

Target outcome achieved:

* abusive request bursts can now be throttled before handler execution

5. WebSocket Hardening

Delivered:

* per-IP WebSocket connection caps
* optional required `Origin`
* optional origin allow-list
* optional allowed subprotocol list

Target outcome achieved:

* WebSocket admission is stricter before the protocol switch completes

6. Authentication Hardening

Delivered:

* request-scoped authenticated context
* structured bearer/basic/API-key identity registrations
* role-aware authorization via `[RequireAuth(Roles = ...)]`
* proper `401 Unauthorized` vs `403 Forbidden`
* optional `WWW-Authenticate` challenge headers

Target outcome achieved:

* authentication is no longer only an illustrative sample module

7. Async Handler Correctness

Delivered:

* minimal-route delegates returning `Task` are awaited
* controller methods returning `Task`/`ValueTask` are awaited

Target outcome achieved:

* async handler completion is now aligned with middleware and request lifecycle expectations

Known Remaining Gaps

1. Middleware Lifecycle Management

Current limitation:

* middleware can be appended but not removed or toggled by handle once registered

2. Legacy/Modern Coexistence

Current limitation:

* legacy module interceptors still coexist with the middleware pipeline

3. Post-Routing Middleware Metadata

Current limitation:

* middleware does not yet receive resolved endpoint metadata such as `MethodInfo`

Validation Performed

Build validation completed for:

* `HSB/HSB.csproj`
* `Examples/HelloWorld/HelloWorld.csproj`
* `Examples/StreamingResponse/StreamingResponse.csproj`
* `Examples/TokenAuthentication/TokenAuthentication.csproj`
* `Runner/Runner.csproj`

Definition of Done

0.0.22 should be considered complete when:

* middleware is available without breaking route mapping APIs
* security features are configurable and optional
* async handlers participate correctly in request execution
* authentication behaves as a real runtime module rather than a placeholder example
* WebSocket and HTTP admission are stronger than in 0.0.21
* release notes and documentation reflect the delivered behavior

Recommended Working Label

`Security & Runtime Hardening Release`
