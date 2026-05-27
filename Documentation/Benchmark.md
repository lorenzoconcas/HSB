HSB 0.0.19 — Upload Stability & HTTP Hardening

Current Stable Version: v0.0.19

Overview

This release is intentionally limited to server stability work:

* HTTP parser hardening
* multipart upload memory safety
* upload concurrency backpressure
* socket disconnect resilience
* WebSocket idle cleanup and heartbeat

Advanced upload streaming is not part of this release.

Postponed to 0.0.20+

* `/upload-resource-stream`
* NDJSON upload
* experimental chunked upload parsing
* realtime upload parsing pipelines

Implemented Hardening in 0.0.19

HTTP

* centralized request limits through `Configuration.Http`
* maximum body size
* maximum header count
* maximum header size
* request-line size validation
* header/body timeout enforcement
* early rejection for duplicate non-repeatable headers
* explicit rejection of chunked request bodies

Uploads

* classic `multipart/form-data` only
* request body buffering split into:
  * bounded header buffering in RAM
  * file-backed multipart body storage on disk
* temp-file-backed multipart file parts
* maximum file size enforcement
* maximum form-field size enforcement
* upload timeout enforcement
* automatic cleanup of request-scoped temp files
* bounded concurrent upload gate with `429 Too Many Requests`

Responses

* `Response.SendFile(...)` now streams from disk
* reduced whole-file allocations during file responses
* response header merging no longer duplicates single-value headers
* HTTP status line now emits a valid reason phrase

WebSocket

* heartbeat ping/pong during idle periods
* bounded pending frame buffer growth
* quieter handling of expected disconnects
* structured lifecycle logging

Structured Logging

* `[UPLOAD][START]`
* `[UPLOAD][PROGRESS]`
* `[UPLOAD][DONE]`
* `[UPLOAD][ERROR]`
* `[WS][CONNECT]`
* `[WS][DISCONNECT]`
* `[WS][ERROR]`

Validation Completed in Repository

Automated runtime tests currently verify:

* WebSocket frame parsing and route registration
* configuration parsing for `http` and `upload` limits
* invalidation of duplicate `Content-Length` headers
* classic multipart parsing for file + field payloads
* rejection of invalid multipart MIME values

Validation Still Required Outside This Workspace

The repository update does not include full heavy-load benchmark execution. The following matrix still needs to be run on a real target host after deployment of 0.0.19:

Upload Tests

* single upload: 1GB
* single upload: 2GB
* concurrent uploads: 10
* concurrent uploads: 50

Mixed Stress

* persistent WebSocket clients during uploads
* uploads plus parallel API traffic
* repeated abrupt client disconnects

Long-Running

* 1h+ soak test
* continuous upload loop
* persistent WebSocket connections

Metrics to Capture

* RAM usage
* CPU usage
* upload throughput
* websocket latency
* active connections
* peak allocations

Expected 0.0.19 Outcome

* no full-request multipart buffering in RAM
* bounded upload concurrency
* bounded parser memory growth
* cleaner disconnect handling
* reduced `Broken pipe` log noise
* no experimental upload streaming paths in active use

Known Limitations

* Full before/after benchmark numbers are pending rerun after these fixes.
* Advanced upload streaming remains deferred.
* HTTP request chunked transfer encoding remains intentionally unsupported in this release.
