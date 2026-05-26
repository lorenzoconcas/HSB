HSB — Benchmark & Stability Report

Current Stable Version: v0.0.18

Upcoming Release: v0.0.19

⸻

Overview

A large set of stress, stability and robustness tests were executed against the HSB infrastructure, focusing on:

* WebSocket handling
* file uploads
* streaming uploads
* HTTP/header limits
* high load behavior
* server resiliency
* memory pressure
* socket robustness

The current stable release is:

HSB v0.0.18

The upcoming:

HSB v0.0.19

will introduce:

* file upload streaming
* networking improvements
* better I/O handling
* NDJSON streaming support

⸻

Testing Goals

The tests were designed to validate:

Area	Goal
HTTP Core	Parser and routing stability
WebSocket	Concurrent connection handling
Uploads	Payload robustness
Streaming	Continuous chunk processing
Networking	Abrupt disconnect handling
Security	Header abuse & flooding
Memory	Stability under pressure

⸻

1. WebSocket Storm Test

Description

Simulation of:

* many simultaneous WebSocket clients
* rapid reconnect cycles
* concurrent data transmission
* clients disconnecting unexpectedly

⸻

Results

Metric	Result
Concurrent connections	~75+
Server stability	OK
Server crashes	None
Disconnect handling	Partial
Socket errors	Present
Obvious memory leaks	Not detected

⸻

Observed Errors

System.Net.Sockets.SocketException (32): Broken pipe

and:

operation cancelled

⸻

Analysis

These errors mainly occur when:

* clients disconnect before writes complete
* the server writes to already closed sockets
* async operations are cancelled during disconnect/shutdown

The server:

* does not crash
* continues accepting new clients
* keeps internal state consistent

⸻

Status

Component	Status
WebSocket accept loop	OK
Concurrent clients	OK
Disconnect handling	Needs improvement
Exception noise	High

⸻

2. HTTP Header Abuse Test

Goal

Validate protections against:

* oversized headers
* header flooding
* abusive requests

⸻

Initial Errors

Closing connection: header size limit exceeded
Closing connection: too many headers

⸻

Implemented Fixes

The following protections were added:

Protection	Status
Header size limit	Implemented
Header count limit	Implemented
Early connection termination	Implemented

⸻

Benchmark Results

Scenario	Before	After
Oversized headers	Memory pressure	Connection rejected
Header flooding	Potential abuse	Blocked
Server stability	Risk of degradation	Stable

⸻

3. Upload & Routing Test

Scenario

Testing invalid endpoints and malformed upload requests.

⸻

Result

POST '/upload' 404 (Resource not found)

Correct behavior.

⸻

Routing Benchmark

Test	Result
Invalid route	Correct 404
Server crash	None
Dangerous fallback behavior	None

⸻

4. Heavy k6 Stress Test

Scenario

Extreme stress test using k6 with:

* high connection counts
* aggressive payloads
* concurrent traffic bursts

⸻

Results

Metric	Result
RAM usage	~20GB
Host machine stability	Unstable
HSB crash	No
System saturation	Yes
Existing limits sufficient	No

⸻

Analysis

The test highlighted several missing protections.

⸻

Current Limitations

Area	Problem
Rate limiting	Missing
Payload limits	Partial
Connection throttling	Missing
Memory guardrails	Missing
Backpressure handling	Limited

⸻

File Streaming Status (v0.0.19)

The upcoming release introduces:

POST /upload-resource-stream

with:

application/x-ndjson

support.

⸻

Streaming Goals

Feature	Status
Upload streaming	In development
NDJSON event streaming	Implemented
Progress events	Implemented
Chunked transfer	Implemented
Full robustness	Not yet

⸻

Remaining Streaming Issues

Problem	Status
Backpressure handling	Needs improvement
Slow clients	Not fully handled
Long-running timeouts	Needs tuning
Stream cleanup	Partial
Retry strategies	Missing

⸻

General Benchmark Summary

Area	Status	Notes
HTTP parser	Good	Stable
Routing	Good	No major issues
WebSocket	Good	Disconnect noise remains
Classic uploads	Good	Stable
Streaming uploads	Beta	Needs stabilization
Header protections	Good	Implemented
Memory handling	Acceptable	Can improve
Stress resilience	Medium	Missing guardrails

⸻

Remaining Work

High Priority

Networking

* graceful disconnect handling
* better socket cleanup
* improved cancellation handling

⸻

Security & Protections

* rate limiting
* anti-flood protections
* payload caps
* configurable upload limits

⸻

Streaming

* streaming pipeline stabilization
* slow-client handling
* smarter buffering
* timeout improvements

⸻

Medium Priority

Observability

The following systems are still missing:

Feature	Status
Runtime metrics	Missing
Memory metrics	Missing
Connection dashboard	Missing
Request tracing	Missing

⸻

Logging Improvements

Improvement	Status
Structured logs	Partial
Noise reduction	Needed
Error classification	Missing

⸻

Low Priority

Performance Tuning

* buffer pooling
* allocation reduction
* parser optimizations
* WebSocket tuning

⸻

Current Overall Status

HSB v0.0.18

Considered:

Area	Status
Usable	Yes
Stable	Reasonably
Stress-tested	Yes
Production-ready	Partially
Hardened	Not fully

⸻

Target for v0.0.19

The v0.0.19 release focuses mainly on:

Feature	Status
File streaming	Main feature
NDJSON streaming	Yes
Improved upload handling	Yes
Better resiliency	In progress
Socket stabilization	In progress

⸻

Conclusions

HSB demonstrated:

* good overall resiliency
* a promising architecture
* no critical crashes during realistic stress tests
* solid concurrent connection handling

The main weaknesses currently involve:

* networking edge cases
* anti-abuse protections
* streaming stability under extreme load
* aggressive memory pressure scenarios

v0.0.19 will primarily focus on file streaming support and continued networking stabilization.