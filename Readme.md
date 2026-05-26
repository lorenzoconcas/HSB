# HSB

![HSB banner](banner.png)

[![.NET](https://img.shields.io/badge/.NET-net10.0-512BD4)](https://dotnet.microsoft.com/)
[![Version](https://img.shields.io/badge/version-0.0.18-blue)](./VERSIONS.md)
[![License](https://img.shields.io/badge/license-see%20LICENSE.md-lightgrey)](./LICENSE.md)


HSB, historically short for HttpServerBoxed, is an HTTP and WebSocket web/server framework written in C#. It started as a study project focused on understanding how a web server works, but the current architecture has evolved into a lightweight framework for HTTP APIs, WebSocket communication, streaming, and embedded servers.

The project is still pre-1.0: APIs may change between releases and even between individual commits. For stable projects, it is recommended to use the published artifacts in Releases/ and read VERSIONS.md￼ before upgrading.

Introduction

HSB currently provides:

Area	Current support
HTTP server	TCP sockets, routing, static files, embedded resources
Modern routing	Attribute-based controllers and minimal API/Express-style mappings
Controllers	[Controller], [Get], [Post], [Route], route parameters, Request and Response injection
WebSocket	Modern endpoints via Configuration.WebSocket(...) and [Ws] inside controllers
Streaming	Chunked responses with InitStream, AddStreamChunk, EndStream
Security	Header/request limits, timeouts, anti-Slowloris hardening, CORS, IP filters, TLS
Middleware/hooks	Global modules, request interceptors, request handler interceptors, service modules
TLS	SslConfiguration, .p12/.pkcs12 certificates, native .NET TLS and experimental HSB handler
OpenAPI	Attributes and dedicated handlers for API documentation

Philosophy

HSB aims to stay small, explicit, and close to the protocol. The framework does not hide the HTTP lifecycle: it exposes Request, Response, sockets, headers, cookies, sessions, TLS, and WebSocket directly.

The modern design focuses on three ways of building applications:

* Attribute-based controllers for APIs organized by functional area.
* Minimal API style with config.Get(...), config.Post(...), config.WebSocket(...) for small endpoints or prototypes.
* Modules for cross-cutting hooks such as authentication, filtering, and pre/post-routing logic.

The old Servlet and [Binding] model has been removed: routes are now defined through controllers or direct mappings on Configuration.

Installation

Clone the repository and build the solution:

git clone https://github.com/lorenzoconcas/HSB.git
cd HSB
dotnet build HSB.sln

The core project is HSB/HSB.csproj and currently targets net10.0.

To get started quickly you can use:

* Examples/HelloWorld
* Examples/ControllerExample
* Examples/StreamingResponse
* Experiments/StressTest
* Runner to test many features together

Quick Start

Minimal example using Express/minimal API-style mappings:

using HSB;
Configuration config = new()
{
Port = 8080
};
config.Get("/", (Request req, Response res) =>
{
res.Send("Hello from HSB");
});
config.Get("/json", (Request req, Response res) =>
{
res.Json(new
{
message = "ok",
timestamp = DateTime.UtcNow
});
});
new Server(config).Start();

Run:

dotnet run --project Examples/HelloWorld/HelloWorld.csproj

Routing

HSB registers routes from two sources:

1. Direct mappings on Configuration, such as config.Get(...).
2. Controllers marked with [Controller].

The actual resolution order inside the server is:

Express/minimal mapping -> Controller -> static files/embedded resources -> 404

Minimal API style

config.Get("/health", (Request req, Response res) =>
{
res.Json(new
{
status = "ok",
uptime = Environment.TickCount64
});
});
config.Post("/auth/login", (Request req, Response res) =>
{
if (!req.Body.Contains("admin"))
{
res.Json(new { error = "invalid_credentials" }, 401);
return;
}
res.Json(new
{
accessToken = Guid.NewGuid(),
expiresIn = 3600
});
});

Helpers are available for Get, Post, Put, Delete, Patch, Head, Options, Trace, and Connect.

Route parameters

Inside controllers you can use :name route segments. Values are copied into req.Parameters.

[Controller("/users")]
public class UserController
{
[Get("/:id")]
private void GetById(Request req, Response res)
{
res.Json(new
{
id = req.Parameters["id"]
});
}
}

You can also request typed parameters through [NamedParameter]:

using HSB.Components.Attributes;
using HSB.Components.Controller;
[Controller("/users")]
public class UserController
{
private Response res = null!;
[Get("/:id")]
private void GetById([NamedParameter("id", true)] int id)
{
res.Json(new { id });
}
}

Controllers

The new controller system is the recommended way to build HSB applications. A controller is a normal C# class marked with [Controller("/base-path")]; it does not need to inherit from Servlet.

using HSB;
using HSB.Components.Controller;
[Controller("/api")]
public class UserController
{
[Get("/users")]
public void GetUsers(Request req, Response res)
{
res.Send("ok");
}
[Post("/users")]
private void CreateUser(Request req, Response res)
{
res.Json(new
{
created = true,
body = req.Body
});
}
}

Available attributes

Attribute	Usage
[Controller("/path")]	Declares the controller prefix
[Get("/path")]	HTTP GET route
[Post("/path")]	HTTP POST route
[Put("/path")]	HTTP PUT route
[Delete("/path")]	HTTP DELETE route
[Patch("/path")]	HTTP PATCH route
[Head("/path")]	HTTP HEAD route
[Options("/path")]	HTTP OPTIONS route
[Route("/path", HttpMethod.X)]	Route with explicit method
[Ws("/path")]	WebSocket endpoint inside the controller

Request and Response injection

HSB can inject Request and Response:

* as method parameters;
* as public, private, or protected controller fields;
* through fields when the method uses parameters annotated with [NamedParameter].

[Controller("/reports")]
public class ReportController
{
private Response res = null!;
[Get("/:year")]
private void GetReport([NamedParameter("year", true)] int year)
{
res.Json(new { year });
}
}

Differences compared to Servlet

Legacy Servlet	Modern Controller
Inherits from Servlet	Plain C# class
Routes via [Binding]	Routes via [Controller] and HTTP attributes
Override GET(), POST(), etc.	Free methods marked with [Get], [Post], etc.
One handler per class/route	Multiple routes per controller
Legacy servlet-style WebSocket	[Ws] or config.WebSocket(...)

WebSocket

The modern WebSocket model uses WebSocketConnection. Each connection exposes lifecycle hooks, text/binary sending, state, request context, and endpoint-level broadcasting.

Controller style

using HSB;
using HSB.Components.Controller;
using HSB.Components.WebSockets;
[Controller("/realtime")]
public class ChatController
{
[Ws("/chat")]
public void Chat(WebSocketConnection socket)
{
socket.OnOpen(() =>
{
socket.Send("connected");
});
socket.OnMessage(msg =>
{
socket.Send(msg.Text);
});
socket.OnClose(() =>
{
Terminal.Info("WebSocket closed");
});
}
}

The final route in this example is /realtime/chat.

Minimal API style

config.WebSocket("/ws", socket =>
{
socket.OnMessage(msg =>
{
socket.Send(msg.Text);
});
});

Async support

config.WebSocket("/events", async socket =>
{
await Task.Yield();
socket.OnOpen(async () =>
{
await socket.SendAsync("connected");
});
socket.OnMessage(async msg =>
{
await socket.SendAsync("received:" + msg.Text);
});
});

Lifecycle

Hook	When it runs
OnOpen	After the HTTP 101 handshake and connection opening
OnMessage	For every received text/binary frame
OnClose	When the connection closes
OnError	When the runtime intercepts a connection error

Broadcast

config.WebSocket("/chat", socket =>
{
socket.OnMessage(msg =>
{
socket.BroadcastExceptSelf(msg.Text);
});
});

Broadcast(...) sends to every connection on the same route; BroadcastExceptSelf(...) excludes the current connection.

Streaming

HSB supports chunked HTTP responses through Response.

using System.Text.Json;
config.Get("/stream", async (Request req, Response res) =>
{
await res.InitStream("application/x-ndjson");
for (int i = 0; i < 10; i++)
{
string payload = JsonSerializer.Serialize(new
{
type = "event",
index = i,
timestamp = DateTime.UtcNow
});
await res.AddStreamChunk(payload + "\n");
await Task.Delay(250);
}
await res.AddStreamChunk(JsonSerializer.Serialize(new { type = "done" }) + "\n");
await res.EndStream();
});

application/x-ndjson is useful when you want to send one JSON object per line without waiting for the entire processing to finish. The client can progressively read each chunk and update UI, logs, or dashboards in real time.

Use streaming for:

* lightweight server-side events;
* real-time logs;
* progressive exports;
* long-running jobs with incremental state;
* integrations consuming NDJSON.

Request/Response lifecycle

The HTTP request lifecycle is:

1. Server accepts the TCP connection and sets timeouts.
2. Headers are read until \r\n\r\n, respecting size and count limits.
3. Request parses the request line, query string, headers, cookies, auth, session, body, and uploads.
4. RequestInterceptor modules can accept or reject the request.
5. The HTTP or WebSocket route is resolved.
6. RequestHandlerInterceptor modules can intervene before the handler.
7. The handler writes to Response.
8. The connection is closed or kept alive by the runtime when needed, such as for WebSocket or streaming.

Response includes helpers for:

* Send(...) for text or bytes;
* Json(...) and SendJson(...);
* SendFile(...);
* SendHtmlFile(...) and SendHtmlContent(...);
* Redirect(...);
* SendCode(...);
* InitStream(...), AddStreamChunk(...), EndStream(...).

Middleware and modules

HSB uses a module system annotated with [Module] and [ModuleInvokeMethod]. Modules can act as:

Module type	Typical usage
Global	Global setup
Service	Services started with the server
RequestInterceptor	Filters before route resolution
RequestHandlerInterceptor	Hooks before the resolved handler

Examples inside the project:

* HSB/Modules/Filter.cs for IP allowlist/banlist.
* HSB/Modules/Authentication.cs for [RequireAuth], Bearer, Basic, API keys, and custom validation.
* HSB/Modules/OpenApi.cs for documentation integration.

Authentication example:

using HSB.Modules;
AuthenticationSettings.Instance.EnableBearer = true;
AuthenticationManager.Instance.AddBearerToken("dev-token");
[Controller("/admin")]
public class AdminController
{
[Get("/stats")]
[RequireAuth(AuthType.Bearer)]
private void Stats(Response res)
{
res.Json(new { status = "protected" });
}
}

SSL/TLS

TLS configuration goes through SslConfiguration.

using HSB;
using HSB.Constants.TLS;
SslConfiguration ssl = new("certificate.p12", "password")
{
PortMode = SSL_PORT_MODE.DUAL_PORT,
SslPort = 8443,
UpgradeUnsecureRequests = true,
SslHandler = SslHandler.Native
};
Configuration config = new()
{
Port = 8080,
SslSettings = ssl
};
new Server(config).Start();

Features:

* .p12 or .pkcs12 certificates;
* dual-port HTTP/HTTPS or single-port mode;
* automatic redirect of insecure requests when UpgradeUnsecureRequests is enabled;
* native .NET TLS as the recommended path;
* experimental HSB TLS handler for development and research;
* TLS 1.0 and TLS 1.1 are deprecated: using obsolete versions throws exceptions.

For local development you can use UseDebugCertificate, which creates/loads a temporary certificate if OpenSSL is available.

Performance

HSB includes a stress test project in Experiments/StressTest with realistic endpoints: healthcheck, fake login, medium JSON, large payload, slow endpoint, NDJSON streaming, and WebSocket broadcast.

Observed results from recent project tests:

Scenario	Approximate result
Minimal endpoint	around 18k req/s
Medium JSON	around 8k req/s
Concurrency stress	stable in recent tests
Slowloris resilience	slow connections closed through timeouts/limits
Connection storm	improved stability with timeouts and concurrent handling

These numbers are indicative and depend on machine, .NET runtime, operating system, socket configuration, payload, logging, and benchmarking tools. They are not a production throughput guarantee.

Hardening and security

Recent refactors mainly strengthened the networking and parsing pipeline:

* socket receive/send timeouts;
* closing connections that do not complete headers in time;
* header size limits;
* request line length limits;
* maximum header count;
* more defensive parsing of request lines, query strings, headers, cookies, auth, and bodies;
* case-insensitive dictionaries for headers, parameters, and cookies;
* snapshots of global headers/cookies while building responses;
* locks/semaphores in WebSocket connections for handlers and frame sending;
* concurrent WebSocket connection management through ConcurrentDictionary;
* global and per-response CORS support;
* IP allowlist/banlist through the Filter module;
* optional HTTPS redirect.

Anti-Slowloris protection is based on timeouts, header limits, and requiring header completion before parsing.

Real-world examples

JSON API

[Controller("/api")]
public class ApiController
{
[Get("/status")]
private void Status(Response res)
{
res.Json(new
{
status = "ok",
version = typeof(Server).Assembly.GetName().Version?.ToString()
});
}
}

Upload/form data

Request exposes body, parameters, cookies, authentication, and form/upload helpers. See Examples/FormHandling and Runner/Servlets/FileUpload.cs for complete examples.

Static files

Configuration config = new()
{
Port = 8080,
StaticFolderPath = "./www"
};

If a route is not resolved through mappings/controllers/servlets, HSB tries to serve static files from the configured folder.

WebSocket chat

config.WebSocket("/chat", socket =>
{
socket.OnOpen(() => socket.Broadcast("new client connected"));
socket.OnMessage(msg => socket.BroadcastExceptSelf(msg.Text));
socket.OnClose(() => socket.Broadcast("client disconnected"));
});

Project structure

Path	Description
HSB/	Core library: server, request/response, routing, controllers, WebSocket, TLS, modules
Runner/	Development application used to test HSB in action
Examples/	Small examples focused on specific APIs
Experiments/	Prototypes and stress tests for advanced features
Documentation/	Manual/authored project documentation
docs/	Doxygen-generated output, not intended as the primary documentation source
Launcher/	Tool for launching HSB projects through a dedicated entry point
Bootstrapper/	Utilities/templates to make HSB directly executable
Template/	Visual Studio template
Tests/	Tests and experimental assets
Releases/	Archive of previous releases

Removed APIs

Servlet

Servlet, [Binding], and servlet-style routing have been removed. Use controllers or direct mappings instead.

Modern equivalent using controllers:

[Controller("/legacy")]
public class ExampleController
{
[Get("/")]
private void Get(Response res)
{
res.Send("ok");
}
}

Servlet-style WebSocket

The old inheritable/class-based WebSocket approach using [Binding] has been removed. Use:

* config.WebSocket("/path", socket => { ... });
* [Ws("/path")] inside a controller.

Servlet -> Controller migration

1. Replace [Binding("/path")] with [Controller("/path")].
2. Remove inheritance from Servlet.
3. Transform GET(), POST(), etc. into methods annotated with [Get], [Post], etc.
4. Receive Request and Response as parameters or fields.
5. Group related routes inside the same controller when they belong to the same functional area.
6. Replace legacy WebSocket with [Ws] or config.WebSocket.

Why use HSB

HSB is suitable when you want:

* an embedded and controllable C# server;
* lightweight APIs without a full web stack;
* to experiment with low-level HTTP, sockets, TLS, or WebSocket;
* fast prototypes with modern routing;
* NDJSON streaming or WebSocket without heavy dependencies;
* an educational but now structured framework.

For enterprise services with a complete ecosystem, managed hosting, standardized middleware, and long-term support, ASP.NET Core remains the primary choice.

Roadmap

Recommended priorities for the next iterations:

* fully stabilize the controller system;
* consolidate modern WebSocket and broadcasting support;
* make benchmarks reproducible with scripts and documented environments;
* strengthen automated tests for parsing, timeouts, streaming, and WebSocket;
* improve OpenAPI documentation;
* clarify compatibility and target framework support for each release;
* reach a stable API surface ahead of a future 1.0 release.

Resources

* Project Webpage￼
* Versions and migrations￼
* WebSocket Documentation￼
* SSL Documentation￼
* Response Documentation￼