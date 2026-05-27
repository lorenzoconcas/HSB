HSB 0.0.20 Plan

Title: HSB 0.0.20 — True Streaming Architecture

Status

Implemented in the current repository state as the architectural basis for the 0.0.20 development milestone.

Purpose

This document started as the kickoff plan for 0.0.20.
The goal is to move HSB from bounded buffering and temp-file-backed stability work into a real streaming architecture.

Relationship with 0.0.19

Version 0.0.19 established the safe baseline:

* multipart stabilization
* bounded request buffering
* upload temp-file spooling
* HTTP limit enforcement
* websocket heartbeat
* upload backpressure

Version 0.0.20 should build on that baseline, not discard it blindly.

Current Recommended Strategy

* keep 0.0.19 behavior as stable fallback
* add streaming architecture progressively
* validate each new streaming layer against the 0.0.19 baseline
* avoid replacing all request handling paths in one step

Scope

In scope for 0.0.20:

* full streaming request parser
* streaming uploads end-to-end
* streaming response improvements
* chunked transfer support
* stronger backpressure system
* zero-copy evaluation where useful

Out of scope for 0.0.20:

* global performance tuning as the main focus
* ArrayPool / GC / allocation-wide optimization as primary work
* rate limiting / DOS layer
* observability platform work
* packaging / public stabilization

Primary Goals

1. Request Streaming

Replace the current "read full header, then either read full body in memory or spool full multipart body to temp" flow with a request-body stream model.

Target outcome:

* request body consumed incrementally
* no mandatory full-body buffering before handler access
* multipart parser can read sections as they arrive
* chunked request bodies become possible

2. Upload Streaming End-to-End

Uploads should flow directly from socket to parser to destination stream with bounded buffering.

Target outcome:

* multipart file sections streamed to destination/temp file as bytes arrive
* no full multipart body temp file required as an intermediate step
* upload cancellation works during active transfer
* backpressure propagates naturally from destination writes

3. Streaming Response Improvements

Current chunked response helpers exist but are basic.
0.0.20 should make response streaming more robust and more aligned with the new request streaming model.

Target outcome:

* safer response streaming lifecycle
* better cancellation and disconnect handling
* consistent write/backpressure behavior
* reusable abstractions for streaming responses

4. Chunked Transfer Support

Request-side chunked transfer is intentionally unsupported in 0.0.19.
0.0.20 is the correct place to add it.

Target outcome:

* decode HTTP chunked request bodies
* integrate with request body stream abstraction
* support chunked uploads without requiring experimental side paths

5. Better Backpressure

The 0.0.19 upload semaphore is a coarse protection.
0.0.20 should add a deeper model.

Target outcome:

* bounded per-connection buffers
* parser-level backpressure
* upload-write pacing based on destination speed
* cleaner saturation behavior under mixed load

Non-Goals

The following should not become the center of 0.0.20:

* broad API redesign
* middleware pipeline work
* public beta API stabilization
* full production-hardening of every protocol edge case
* platform-specific micro-optimization

Proposed Architecture

1. Transport Layer

Keep the current socket / SSL / manual TLS transport handling, but expose a unified async read/write abstraction.

Suggested internal concept:

* `ITransportConnection`

Responsibilities:

* async read
* async write
* timeout control
* remote endpoint info
* clean shutdown

2. HTTP Request Reader

Introduce a streaming request reader that separates:

* header parsing
* transfer decoding
* body stream exposure

Suggested internal concept:

* `HttpRequestReader`

Responsibilities:

* incrementally parse request line and headers
* enforce header limits
* decide body mode:
  * fixed-length
  * chunked
  * no body
* expose a bounded body stream abstraction

3. Request Body Stream

The main architectural shift should be an internal request body stream rather than `byte[] RawBody` as the primary model.

Suggested internal concept:

* `RequestBodyStream`

Responsibilities:

* bounded async read
* cancellation-aware reads
* body-size accounting
* transfer decoding awareness

4. Multipart Section Reader

Build a streaming multipart reader on top of the request body stream.

Suggested internal concept:

* `StreamingMultipartReader`

Responsibilities:

* parse boundaries incrementally
* expose sections one by one
* stream file section content directly to destination
* avoid full request-body temp file dependency

5. Upload Destination Layer

Separate "how multipart bytes arrive" from "where file bytes go".

Suggested internal concepts:

* `IUploadSink`
* `TempFileUploadSink`
* future custom sinks

Responsibilities:

* receive bytes incrementally
* report written size
* support cancellation
* finalize or abort cleanly

6. Response Streaming Layer

Unify current chunked response helpers into a cleaner streaming writer abstraction.

Suggested internal concept:

* `HttpResponseStreamWriter`

Responsibilities:

* write headers once
* support fixed-length or chunked response mode
* propagate disconnect/cancellation cleanly

Suggested Phase Plan

Phase 1: Internal Stream Foundations

Deliverables:

* unified internal transport abstraction
* request header reader split from body consumption
* internal request body stream abstraction

Exit criteria:

* existing non-upload request handling still works
* existing 0.0.19 validation does not regress

Phase 2: Fixed-Length Streaming Bodies

Deliverables:

* handlers can consume request body incrementally
* multipart no longer depends on full-body temp spool
* classic fixed-length uploads stream directly

Exit criteria:

* large multipart uploads work with bounded memory
* temp files are used only for file content destinations, not entire request-body fallback

Phase 3: Streaming Multipart Reader

Deliverables:

* multipart boundaries parsed incrementally
* file sections streamed directly to upload sink
* form fields still bounded with size limits

Exit criteria:

* multipart parser handles large files without full-body staging
* cancellation and disconnect cleanup are reliable

Phase 4: Chunked Transfer Support

Deliverables:

* request chunk decoder
* integration with request body stream
* chunked uploads supported through normal request pipeline

Exit criteria:

* chunked request bodies pass protocol validation
* malformed chunked requests are rejected safely

Phase 5: Response Streaming Cleanup

Deliverables:

* cleaner response streaming writer
* robust chunked response lifecycle
* improved disconnect handling on streaming responses

Exit criteria:

* no noisy disconnect loops
* consistent behavior across normal and streaming responses

Phase 6: Zero-Copy Evaluation

Deliverables:

* identify copies that still matter
* benchmark whether zero-copy paths are worth complexity
* implement only targeted wins

Exit criteria:

* decision documented
* no speculative complexity without measured benefit

Major Risks

1. Protocol Complexity

Streaming HTTP parsing is significantly more complex than the bounded-buffer 0.0.19 model.
The parser may become harder to reason about if too much is changed at once.

Mitigation:

* separate components cleanly
* ship in phases
* keep 0.0.19 fallback while iterating

2. Multipart Boundary Bugs

Incremental boundary parsing is a common source of subtle bugs.

Mitigation:

* add targeted multipart regression tests early
* fuzz multipart parsing in later roadmap stages

3. Backpressure Regressions

Streaming can still leak memory if buffers are not bounded at each layer.

Mitigation:

* define explicit buffer ownership and max sizes
* require bounds at transport, parser, multipart, and sink layers

4. Disconnect / Cancellation Edge Cases

Streaming increases the number of partial-progress states.

Mitigation:

* every stream/sink path must be cancellation-aware
* temp-file abort cleanup must be guaranteed

5. Over-Engineering Zero-Copy

Zero-copy can easily consume time without clear benefit.

Mitigation:

* measure first
* implement only where proven useful

Required Validation for 0.0.20

Protocol Validation

* fixed-length upload streaming
* chunked request decoding
* malformed chunked request rejection
* multipart streaming correctness
* disconnect during section write

Stress Validation

* 1GB and 2GB uploads through streaming path
* 10 and 50 concurrent streaming uploads
* mixed websocket + upload pressure
* slow destination writes
* abrupt client disconnect storms

Memory Validation

* bounded parser buffers
* no full-body fallback reintroduced accidentally
* stable RAM under long-running mixed load

Definition of Done

0.0.20 should be considered complete when:

* request body streaming exists as a first-class internal path
* multipart uploads no longer require full request-body staging
* chunked request bodies are supported safely
* response streaming is cleaner and more robust than 0.0.19
* bounded backpressure exists beyond coarse upload concurrency limits
* large upload stress does not regress memory stability

Recommended Working Label

`Architectural Development Release`
