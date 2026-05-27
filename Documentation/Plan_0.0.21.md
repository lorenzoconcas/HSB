HSB 0.0.21 Plan

Title: HSB 0.0.21 — Memory & Performance

Status

Implemented in the current repository state as the optimization basis for the 0.0.21 development milestone.

Purpose

Version 0.0.21 should take the streaming architecture introduced in 0.0.20 and make it cheaper under load.
The main goal is not to add new protocol features, but to reduce allocations, lower GC pressure, and improve throughput under concurrency.

Relationship with 0.0.20

Version 0.0.20 established the modern request pipeline:

* unified transport abstraction
* streaming request reader
* stream-based multipart parsing
* chunked request-body decoding
* removal of the old duplicated request-body path

Version 0.0.21 should optimize that architecture, not replace it.

Current Recommended Strategy

* keep the 0.0.20 streaming model as the baseline
* optimize only after measuring hot paths
* prefer bounded reuse over speculative complexity
* keep protocol behavior stable while improving internals

Scope

In scope for 0.0.21:

* ArrayPool integration
* shared buffer reuse
* GC pressure reduction
* allocation profiling
* high-concurrency optimization
* faster routing
* better socket scheduling

Out of scope for 0.0.21:

* new public end-user features
* middleware API redesign
* security-layer work such as rate limiting and DOS protections
* observability platform work
* packaging and ecosystem stabilization

Primary Goals

1. Allocation Reduction

The new streaming pipeline still allocates in several hot paths:

* request header accumulation
* chunked decoder temporary buffers
* multipart parser temporary arrays
* response stream chunk copies
* log/message string churn

Target outcome:

* fewer request-scoped byte-array allocations
* reduced transient large object heap pressure
* less repeated `ToArray()` / temporary copy behavior in hot paths

2. ArrayPool Adoption

Poolable buffers should become the default for frequently reused byte arrays.

Target outcome:

* request readers rent reusable buffers
* chunked parsing uses pooled scratch buffers
* response streaming reuses shared send buffers where safe
* multipart field/file copy loops avoid repeated fresh buffer allocations

3. GC Pressure Reduction

The goal is not “zero allocations”; the goal is to stop avoidable churn from becoming throughput loss.

Target outcome:

* lower Gen0/Gen1 churn during steady traffic
* fewer full collections under upload pressure
* fewer per-request temporary strings and byte arrays

4. High-Concurrency Throughput

Now that the request path is streaming-capable, 0.0.21 should improve behavior under mixed concurrency.

Target outcome:

* better fairness across concurrent uploads and normal API traffic
* reduced contention in shared server structures
* less throughput collapse under many simultaneous connections

5. Faster Routing

Routing should be cheaper per request, especially on large route sets.

Target outcome:

* less repeated route scanning
* lower string-slicing overhead
* cleaner path matching for minimal routes and controller routes

6. Better Socket Scheduling

Transport work should avoid unnecessary thread-pool churn and reduce noisy scheduling patterns.

Target outcome:

* fewer blocking bridges in hot I/O paths
* clearer async scheduling for request/response flow
* less avoidable contention during mixed read/write pressure

Non-Goals

The following should not become the center of 0.0.21:

* broad protocol expansion
* experimental websocket features
* advanced security controls
* public API freeze
* platform-specific tuning as the primary theme

Optimization Targets

1. HTTP Request Reader

Likely opportunities:

* pooled header read buffers
* reduced `List<byte>` growth churn
* fewer request-head string splits
* cheaper duplicate-header validation

2. Chunked Request Decoder

Likely opportunities:

* pooled pending buffers
* less temporary line-buffer copying
* reduced intermediate array creation while consuming chunk metadata

3. Multipart Parsing

Likely opportunities:

* pooled section copy buffers
* bounded field accumulation reuse
* fewer temp allocations during file-part creation

4. Response Streaming

Likely opportunities:

* reusable send buffers
* less copy-on-partial-write behavior
* unified write path for fixed-length and chunked response streaming

5. Routing

Likely opportunities:

* precomputed route metadata
* lower-cost route prefix checks
* less repeated normalization per request

6. Logging and Formatting

Likely opportunities:

* reduce hot-path string interpolation churn
* avoid expensive formatting unless the log path is enabled

Suggested Phase Plan

Phase 1: Measure First

Deliverables:

* identify top allocation hot paths in request, multipart, response, and routing
* capture before-state metrics for throughput, allocation rate, and GC activity
* document hot-path priorities

Exit criteria:

* clear shortlist of real bottlenecks exists
* optimization order is based on measurements, not guesswork

Phase 2: Shared Buffer Foundations

Deliverables:

* introduce safe internal pooled-buffer helpers
* define rent/return ownership rules
* integrate first in the request reader and chunked decoder

Exit criteria:

* pooled buffers are used in the hottest byte-oriented paths
* no ownership leaks or double-return risks are introduced

Phase 3: Multipart and Response Reuse

Deliverables:

* multipart copy loops use pooled buffers
* response stream sends use reusable buffers where beneficial
* cleanup paths are validated under disconnects and exceptions

Exit criteria:

* upload and response streaming no longer allocate fresh large buffers per request path by default

Phase 4: Routing and Shared-State Optimization

Deliverables:

* reduce route scanning overhead
* simplify hot-path path matching
* lower contention in shared collections where useful

Exit criteria:

* request dispatch cost improves measurably on moderate and larger route sets

Phase 5: Socket Scheduling Cleanup

Deliverables:

* reduce avoidable `Task.Run(...)` bridges in hot transport paths where safe
* improve async flow consistency
* revalidate disconnect handling and throughput

Exit criteria:

* no throughput regressions from scheduling changes
* transport remains stable across plain, SSL, and manual TLS paths

Phase 6: Benchmark and Tighten

Deliverables:

* compare before/after allocation and throughput results
* keep only optimizations that show measurable value
* document residual hotspots for later roadmap stages

Exit criteria:

* 0.0.21 improvements are measurable and documented

Major Risks

1. Premature Complexity

Pooling can easily make code harder to reason about if introduced too broadly.

Mitigation:

* optimize only hot paths
* hide pooling behind small internal helpers
* keep ownership explicit

2. Buffer Lifetime Bugs

Reuse can introduce use-after-return or stale-data bugs.

Mitigation:

* define strict ownership boundaries
* add targeted regression tests around disposal and disconnects
* prefer small, obvious pooling scopes over global reuse magic

3. Manual TLS Scheduling Regressions

The manual TLS path may need special handling because it still bridges some synchronous operations.

Mitigation:

* measure plain/SSL/manual TLS separately
* optimize the plain and `SslStream` paths first if needed
* keep manual TLS correctness ahead of raw speed

4. False Wins

Some “optimizations” may improve microbenchmarks but not real mixed-load behavior.

Mitigation:

* validate changes under concurrency
* compare throughput and allocations together
* do not keep changes that only add complexity

Required Validation for 0.0.21

Allocation Validation

* allocation rate before vs after
* Gen0/Gen1/Gen2 behavior under steady load
* peak working set during mixed upload/API traffic

Concurrency Validation

* 10 and 50 concurrent uploads
* mixed websocket + upload + API traffic
* many small requests with route-heavy dispatch

Performance Validation

* request throughput before vs after
* upload throughput before vs after
* route dispatch overhead before vs after
* latency distribution under concurrency

Correctness Validation

* no pooled-buffer lifetime bugs
* multipart cleanup still works
* chunked request decoding still works
* disconnect handling still remains quiet and correct

Definition of Done

0.0.21 should be considered complete when:

* hot request/response/upload paths allocate measurably less than 0.0.20
* pooled buffer usage exists in the main byte-processing paths
* GC pressure is lower under mixed load
* routing overhead is measurably improved or at least clearly characterized
* no protocol regressions are introduced while optimizing internals
* before/after results are documented with concrete measurements

Recommended Working Label

`Optimization Development Release`
