# BackofficeDemo

`BackofficeDemo` is the full showcase example for HSB `0.0.22`.
It is intentionally shaped like a small real internal business application, not a minimal API sample.

## What it demonstrates

* HSB controllers plus a couple of minimal routes
* `Use(...)` middleware in a realistic request pipeline
* `Security.Headers`, `Security.Validation`, and `Security.RateLimit`
* bearer authentication with roles and `[RequireAuth]`
* product and order uploads
* WebSocket live notifications
* OpenAPI and Swagger
* static frontend hosting from HSB
* a realistic backend/frontend split inside one example project

## Functional scope

The app simulates a small order and inventory backoffice:

* dashboard with business metrics
* product catalog management
* customer management
* order management
* inventory adjustments
* audit/activity feed
* live notifications over WebSocket

## Tech stack

### Backend

* HSB `0.0.22`
* controller-first API structure
* custom middleware chain
* in-memory seeded data

### Frontend

* Vue 3
* TypeScript
* Vite
* Vue Router
* Pinia
* Tailwind CSS
* `lucide-vue-next`

The UI uses local Tailwind-based primitives with a `shadcn`-style feel.
I kept it package-light so the example stays easy to clone and run.

## Demo users

* `admin` / `admin123`
* `manager` / `manager123`
* `operator` / `operator123`

## Run the example

### 1. Build the frontend

```bash
cd Examples/BackofficeDemo/frontend
npm install
npm run build
```

### 2. Run the backend

```bash
cd Examples/BackofficeDemo
dotnet run
```

The app listens on:

* `http://localhost:5098`

Useful endpoints:

* app: `http://localhost:5098/`
* Swagger: `http://localhost:5098/swagger/index.html`
* OpenAPI JSON: `http://localhost:5098/openapi.json`
* health: `http://localhost:5098/api/health`

## Project layout

```text
Examples/BackofficeDemo/
  BackofficeDemo.csproj
  Program.cs
  appsettings.example.json
  Backend/
    Controllers/
    Contracts/
    Infrastructure/
    Middleware/
    Models/
    Services/
    WebSockets/
  frontend/
    package.json
    vite.config.ts
    tailwind.config.ts
    src/
```

## Runtime notes

`Program.cs` configures:

* static hosting for `frontend/dist`
* global CORS for the demo
* OpenAPI/Swagger
* security headers
* request validation
* global rate limiting
* custom middleware for request id, login throttling, logging, and timing
* WebSocket endpoint at `/ws/notifications`

## Current implementation choices

* persistence is in-memory and seeded at startup
* uploads are stored under the built frontend static tree for demo convenience
* router history uses hash mode so the app works cleanly with plain static hosting

That keeps the example realistic without forcing extra infrastructure such as a database or SPA rewrite handling.
