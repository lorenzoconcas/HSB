HSB 0.0.19 Release Gate

Title: HSB 0.0.19 — Upload Stability & HTTP Hardening

Purpose

This checklist is the release gate for 0.0.19.
The release can be considered complete only when all required stability and validation items are closed.

Scope Lock

The following areas are in scope for 0.0.19:

* multipart stability
* temp-file-backed upload handling
* HTTP hardening
* WebSocket heartbeat and cleanup
* better stress resistance
* structured upload logging
* request validation

The following are explicitly out of scope for 0.0.19 and must remain postponed:

* `/upload-resource-stream`
* NDJSON upload
* experimental chunk upload
* realtime upload parsing
* advanced upload streaming pipelines

Current Status Summary

Code status: mostly complete

* multipart upload is bounded and temp-file-backed
* HTTP limits are centralized
* request validation is stricter
* file responses no longer require full file buffering
* upload concurrency is bounded
* websocket heartbeat exists
* upload and websocket structured logs exist

Validation status: incomplete

* local build/test validation completed
* heavy stress validation not yet completed
* before/after benchmark numbers not yet collected

Release Decision Right Now

Do not mark 0.0.19 as fully closed yet.
Current state is best described as:

* code complete for scope
* not yet performance-validated for release sign-off

Release Gate Checklist

1. Scope Integrity

- [x] No new end-user features added.
- [x] Advanced upload streaming remains deferred.
- [x] Only classic `multipart/form-data` remains supported for uploads.

2. Multipart Stability

- [x] Multipart body is no longer parsed through full in-memory duplication.
- [x] Uploaded file parts are backed by temp files.
- [x] Temp file cleanup exists for request-scoped multipart resources.
- [x] Max file size limit exists.
- [x] Max form-field size limit exists.
- [x] Invalid multipart MIME detection exists.
- [x] Multipart boundary is required and validated.
- [ ] Verify no temp-file leak after repeated failed uploads.

3. HTTP Hardening

- [x] Max body size is centralized.
- [x] Max header count is centralized.
- [x] Max header size is centralized.
- [x] Request-line size limit exists.
- [x] Header read timeout exists.
- [x] Body read timeout exists.
- [x] Duplicate non-repeatable headers are rejected.
- [x] Malformed headers are rejected early.
- [x] Chunked request bodies are rejected instead of entering unstable paths.
- [ ] Validate behavior under malformed keep-alive/request pipelining abuse.

4. Response and Socket Safety

- [x] `Broken pipe` and expected disconnects are handled without hard crash.
- [x] File responses stream from disk instead of loading full files in RAM.
- [x] HTTP response header building avoids duplicate single-value headers.
- [x] HTTP status line now uses a valid reason phrase.
- [ ] Verify no noisy repeated logs under mass disconnect storm.

5. Upload Backpressure

- [x] Concurrent upload limit exists.
- [x] Saturation returns controlled error (`429 Too Many Requests`).
- [x] Server no longer accepts unbounded parallel multipart body buffering.
- [ ] Validate fair recovery after upload queue pressure subsides.

6. WebSocket Stability

- [x] WebSocket idle timeout exists.
- [x] WebSocket heartbeat ping/pong exists.
- [x] WebSocket close/error paths handle expected disconnects more quietly.
- [x] Pending buffer growth is bounded.
- [x] Connect/disconnect/error structured logging exists.
- [ ] Validate websocket latency during parallel large uploads.
- [ ] Validate no orphaned async work after repeated reconnect storms.

7. Logging and Observability

- [x] Upload lifecycle logging exists.
- [x] WebSocket lifecycle logging exists.
- [x] Error logs are more structured than before.
- [ ] Confirm log volume remains acceptable during stress tests.
- [ ] Confirm no infinite warning loop on bad clients.

8. Configuration

- [x] `http` configuration block exists.
- [x] `upload` configuration block exists.
- [x] Size strings like `2GB` / `64KB` are parsed.
- [x] Legacy `RequestMaxSize` bug has been fixed.
- [ ] Add an end-to-end sample config snippet to public configuration docs if desired.

9. Local Validation Completed

- [x] `dotnet build --no-restore HSB/HSB.csproj`
- [x] `dotnet build --no-restore Runner/Runner.csproj`
- [x] `dotnet build --no-restore Experiments/StressTest/StressTest.csproj`
- [x] `dotnet run --no-build --project Tests/HSB.WebSocketRuntimeTests/HSB.WebSocketRuntimeTests.csproj`

10. Required Release Validation Still Pending

These items are mandatory before final 0.0.19 sign-off:

- [ ] Single upload test: 1GB
- [ ] Single upload test: 2GB
- [ ] Concurrent upload test: 10 simultaneous uploads
- [ ] Concurrent upload test: 50 simultaneous uploads
- [ ] Mixed stress test: websocket clients + uploads + parallel API traffic
- [ ] Disconnect storm test during uploads
- [ ] Long-running soak test: 1h+
- [ ] Temp directory cleanup verification after soak test

11. Required Benchmark Data Still Pending

Collect before final release closeout:

- [ ] RAM usage before vs after
- [ ] CPU usage before vs after
- [ ] upload throughput before vs after
- [ ] websocket latency before vs after
- [ ] active connection behavior before vs after
- [ ] peak allocation behavior before vs after

12. Ship Criteria

0.0.19 can be marked complete only when all of the following are true:

* all pending items in sections 10 and 11 are completed
* no crash is observed during heavy upload stress
* RAM remains bounded and stable during concurrent uploads
* websocket behavior remains acceptable during upload pressure
* temp files are cleaned up after success, failure, and disconnect scenarios

Recommended Final Labels

Use one of these states while closing the release:

* `Code Complete` -> code merged, heavy validation pending
* `Release Candidate` -> heavy validation running or mostly complete
* `Released` -> all gates passed

Current Recommended Label

`Code Complete`
