# CLAUDE.md

This file provides guidance for AI assistants working on the Stikl codebase.

## Project Overview

Stikl is a .NET 10 web application for plant sharing/exchange. Users can list plants they want or have, find matches, and chat with other users. The app uses HTMX for dynamic UI updates and event sourcing for domain state management.

**Tech Stack**: ASP.NET Core 10 (Minimal APIs) + PostgreSQL + HTMX + Strongbars templates

## Repository Structure

```
stikl/
├── app/                    # .NET application source
│   ├── Stikl.Web/         # Main web application
│   │   ├── Program.cs     # App bootstrap, DI registration, middleware
│   │   ├── Model/         # Domain models and value objects
│   │   ├── DataAccess/    # Repository classes (UserSource, UserEventWriter, ChatStore, ChatBroker)
│   │   ├── Routes/        # Minimal API endpoint handlers
│   │   ├── Data/          # External API clients (LocationIQ, Perenual)
│   │   ├── Templates/     # Strongbars HTML templates (compiled to C#)
│   │   │   ├── Pages/     # Full-page templates
│   │   │   └── Components/ # Reusable component templates
│   │   └── wwwroot/static/ # CSS files
│   ├── Stikl.Tests/       # NUnit test project
│   │   └── Integration/   # Integration tests using Testcontainers
│   ├── Stikl.slnx         # Solution file
│   ├── global.json        # Pins .NET SDK to 10.0.101
│   └── dotnet-tools.json  # CSharpier formatter tool
├── infra/                  # NixOS infrastructure-as-code
│   ├── main.nix           # Firewall, ACME, Nginx reverse proxy
│   ├── app.nix            # PostgreSQL, Podman container config
│   └── telemetry.nix      # Grafana, Prometheus, Loki observability stack
├── .github/workflows/      # GitHub Actions CI/CD
│   ├── ci.yml             # Format check + unit + integration tests
│   └── publish-app-image.yml # Docker image build and push to ghcr.io
└── taskfile.yml           # Task runner (go-task) for common operations
```

## Development Commands

Use `task` (go-task) from the repository root:

```bash
task test        # Run all tests
task app:test    # Run app tests only
task format      # Format all code with CSharpier
task lint        # Check code formatting
task deploy      # Full deploy: pull + infra deploy + update app
```

Running tests directly with dotnet:
```bash
# From app/ directory
dotnet test --filter "Category!=Integration"   # Unit tests only
dotnet test --filter "Category=Integration"    # Integration tests only (requires Docker)
dotnet test                                     # All tests
```

Format check:
```bash
cd app && dotnet csharpier --check .
cd app && dotnet csharpier .    # Auto-format
```

## Architecture

### Event Sourcing

The core domain uses event sourcing. User state is never stored directly — instead, a sequence of events is persisted and replayed to reconstruct the current state.

- **Write model**: `stikl.user_event` table stores all domain events
- **Read model**: `stikl.readmodel_user` is a denormalized cache of user state for fast queries
- **Event types** (in `Model/`): `UserCreated`, `WantPlant`, `UnwantPlant`, `HasPlant`, `NoLongerHasPlant`
- **`UserSource`**: Rebuilds `User` aggregate from events, falling back to replaying when read model is stale

All `UserEventPayload` subtypes are registered for JSON polymorphism. When adding a new event type, register it with `[JsonDerivedType]` on `UserEventPayload`.

### Minimal APIs with Router Classes

Endpoints are organized into static router classes with a `Map()` method:

```csharp
public static class PlantRouter
{
    public static void Map(RouteGroupBuilder app) { ... }
}
```

All routers are registered in `Program.cs`. Each `Map()` method uses route groups for authorization and path prefixes.

### Strongbars Templates

HTML templates in `Templates/` are compiled to C# classes at build time via the Strongbars source generator. Templates are type-safe and rendered to strings — no reflection at runtime.

- Page templates → `Stikl.Web.Templates.Pages` namespace
- Component templates → `Stikl.Web.Templates.Components` namespace
- Templates receive strongly-typed model objects as parameters

### HTTP Result Types

Custom `IResult` implementations in `Routes/`:

| Type | Usage |
|------|-------|
| `PageResult` | Full page HTML with layout |
| `PartialResult` | HTMX partial content swap |
| `ModalResult` | Modal dialog content |
| `ComponentResult` | Single component |
| `RedirectResult` | HTTP redirect |
| `ServerSentEventResult` | SSE streaming (used for chat) |

### Real-time Chat

Chat uses PostgreSQL LISTEN/NOTIFY for pub-sub:
- `ChatBroker` is a hosted background service managing subscriptions
- `ChatStore` handles chat persistence and read state
- `ServerSentEventResult` streams events to the browser

## Code Conventions

### Formatting

CSharpier is enforced in CI. Always run `task format` before committing. CI will fail on improperly formatted code.

### C# Style

- **Nullable enabled** — handle nullability explicitly; compiler warnings are errors
- **Implicit usings enabled** — no need to import system namespaces manually
- **Records** for immutable data structures (domain models, value objects, events)
- **Value objects** for domain primitives: `Username`, `Email`, `SpeciesId`, `ChatId`
- **Immutable collections**: prefer `ImmutableArray<T>` and `ImmutableHashSet<T>` for domain state
- Global using alias: `ILogger = Serilog.ILogger` (defined in `Program.cs`)

### Database

- Schema prefix: `stikl.*` for all tables
- Migrations are plain SQL files in `app/sql/`
- Use `NpgsqlParam` helpers for parameterized queries
- Integration tests spin up a PostgreSQL 16-alpine container via Testcontainers and truncate tables in `[SetUp]`

### Authentication

- Email-based OTP (one-time password) login
- Cookie-based sessions (20-minute idle timeout, sliding expiration)
- Claims-based authorization
- CSRF protection via `X-CSRF-TOKEN` header

### External APIs

- **LocationIQ** — geolocation (`LocationIQClient.cs`)
- **Perenual** — plant species database (`PerenualApiScraper.cs`)
- HTTP clients use Flurl with snake_case JSON serialization

## CI/CD

### CI Pipeline (`.github/workflows/ci.yml`)

Runs on PRs and pushes to `main` (only when `app/` or `.github/` changes):
1. **format job** — `dotnet csharpier --check .` (fails if unformatted)
2. **test job** — runs unit tests, then integration tests separately

### Publish Pipeline (`.github/workflows/publish-app-image.yml`)

Triggered manually or on `main` pushes:
- Builds multi-platform Docker image
- Publishes to `ghcr.io/c0dk/stikl:main`

## Deployment

Infrastructure is NixOS-based (`infra/`):
- PostgreSQL runs on the host
- ASP.NET app runs in a Podman container on port 8080
- Nginx reverse proxy handles TLS (Let's Encrypt) and forwards to app
- Observability: Grafana + Prometheus + Loki + Promtail

## Known Issues / TODOs

Several TODOs exist in the codebase worth being aware of:
- Error middleware not yet implemented
- CSRF error handling incomplete
- User caching not implemented (rebuilds from events on each request)
- Remember Me functionality not implemented
- Access denied path handling incomplete
- HTMX redirect quirks with auth flows need attention

## Testing Guidelines

- Unit tests go in `Stikl.Tests/` (no special category)
- Integration tests go in `Stikl.Tests/Integration/` and must be tagged `[Category("Integration")]`
- Integration tests use a shared PostgreSQL container (set up once per test run)
- Each integration test truncates relevant tables in `[SetUp]` for isolation
- Use `UserFactory.cs` helper to create test users
