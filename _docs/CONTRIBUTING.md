# Contributing to Wrkzg

Thank you for your interest in contributing! This document covers everything you need to get started.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Architecture Rules](#architecture-rules)
- [Development Workflow](#development-workflow)
- [Coding Conventions](#coding-conventions)
- [Commit Messages](#commit-messages)
- [Pull Request Process](#pull-request-process)
- [How to Add a System Command](#how-to-add-a-system-command)
- [How to Add an API Endpoint](#how-to-add-an-api-endpoint)

---

## Code of Conduct

Be respectful. We're all here to build something useful.
Constructive feedback is welcome; personal attacks are not.

---

## Getting Started

### Prerequisites

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 10.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Node.js | 22 LTS | [nodejs.org](https://nodejs.org/) |
| Git | any | [git-scm.com](https://git-scm.com/) |
| IDE | any | Rider, Visual Studio 2022, or VS Code with C# Dev Kit |

### Setup

```bash
# Fork the repo on GitHub, then:
git clone https://github.com/YOUR_USERNAME/wrkzg-twitchbot.git
cd wrkzg-twitchbot

# Install frontend dependencies and build
cd src/Wrkzg.Frontend && npm install && npm run build && cd ../..

# Restore .NET packages
dotnet restore

# Verify everything builds
dotnet build
```

### Twitch App for Local Development

You need your own Twitch Developer Application to test OAuth and API calls:

1. Go to [dev.twitch.tv/console](https://dev.twitch.tv/console)
2. Register a new application:
    - Name: `Wrkzg-Dev`
    - OAuth Redirect URL: `http://localhost:5050/auth/callback`
    - Category: `Chat Bot`
    - Client Type: `Confidential`
3. Click "New Secret" to generate a Client Secret
4. Create `src/Wrkzg.Api/appsettings.Development.json` (this file is gitignored):

```json
{
  "Twitch": {
    "ClientId": "your_client_id_here",
    "ClientSecret": "your_client_secret_here"
  },
  "Bot": {
    "Port": 5050
  }
}
```

> **Note:** In production, credentials are stored in the OS keychain via the Setup Wizard. The `appsettings.Development.json` fallback is only for contributors during development.

### Running in Development Mode

**Option A — With Vite Hot Reload (recommended for frontend work):**

```bash
# Terminal 1: Start the .NET backend
dotnet run --project src/Wrkzg.Host

# Terminal 2: Start the frontend dev server
cd src/Wrkzg.Frontend && npm run dev
```

Photino detects the Vite Dev Server on `:5173` and opens it automatically. Changes to React components are reflected instantly via Hot Module Replacement.

**Option B — Without Vite (backend work only):**

```bash
# Build frontend once
cd src/Wrkzg.Frontend && npm run build && cd ../..

# Run the app (serves built static files)
dotnet run --project src/Wrkzg.Host
```

Photino opens the built SPA from `Wrkzg.Api/wwwroot/`. The console shows `[Photino] Using Kestrel at http://localhost:5050`.

### Theme Development

The app supports Light and Dark themes. When working on UI components, test both themes:
1. Toggle via the Sun/Moon button in the sidebar
2. All colors must use CSS custom properties (`var(--color-*)`)
3. Never use hardcoded Tailwind color classes for themed elements
4. Twitch-specific UI uses `var(--color-twitch)` instead of `var(--color-brand)`

---

## Project Structure

```
wrkzg/
├── src/
│   ├── Wrkzg.Host/               Entry point, Photino window, DI bootstrap
│   ├── Wrkzg.Api/                REST endpoints, SignalR hubs, wwwroot/
│   ├── Wrkzg.Core/               Business logic, interfaces, domain models
│   ├── Wrkzg.Infrastructure/     EF Core, SQLite, Twitch clients, secure storage
│   ├── Wrkzg.Updater/            Standalone auto-update process
│   └── Wrkzg.Frontend/           React + TypeScript SPA (not in .sln)
├── tests/
│   ├── Wrkzg.Core.Tests/         Unit tests for Core
│   └── Wrkzg.Api.Tests/          Integration tests for API endpoints
├── _docs/                        This documentation
├── Directory.Build.props          Shared build settings
├── Directory.Packages.props       Central NuGet version management
└── wrkzg.sln
```

---

## Architecture Rules

These rules are enforced during code review. Please read them carefully.

### Dependency Direction

Dependencies only point **inward**:

```
Host → Api → Core ← Infrastructure
```

- `Wrkzg.Core` must **never** reference Infrastructure, Api, or Host
- `Wrkzg.Infrastructure` must **never** reference Api or Host
- `Wrkzg.Api` must **never** reference Host

### What Goes Where

| Layer | Belongs here | Does NOT belong here |
|---|---|---|
| `Core` | Interfaces, domain models, business logic, chat games, effect types | EF Core, HttpClient, Twitch SDK, ASP.NET types |
| `Infrastructure` | DbContext, repositories, Twitch clients, secure storage | Business logic, API endpoints |
| `Api` | Endpoints, SignalR hubs, DTOs, validators | Database queries, Twitch API calls |
| `Host` | DI bootstrap, Photino window, app startup | Business logic, database access |

### Interface Rule

Every service in `Core` that has a concrete implementation in `Infrastructure` must have a corresponding interface in `Core/Interfaces/`. No concrete infrastructure types may appear in Core.

### Scoped-in-Singleton Pattern

Singleton services that need scoped dependencies (like repositories or DbContext) must receive `IServiceScopeFactory` and create scopes internally. Never inject a scoped service directly into a singleton.

---

## Development Workflow

```
main        → stable, tagged releases only
develop     → integration branch for new features
feat/name   → feature branches (branch from develop)
fix/issue   → bugfix branches (branch from develop or main)
```

### Starting a Feature

```bash
git checkout develop
git pull origin develop
git checkout -b feat/your-feature-name
```

### Before Opening a PR

```bash
dotnet test
dotnet build
dotnet format --verify-no-changes
```

---

## Coding Conventions

The project uses `.editorconfig` in the repository root. Key rules:

- **File-scoped namespaces** — `namespace Wrkzg.Core.Services;`
- **Private fields** — `_camelCase` with underscore prefix
- **Async methods** — always suffix with `Async`, always accept `CancellationToken ct = default`
- **Explicit usings** — `ImplicitUsings` is disabled; all `using` statements must be written
- **Braces always** — even for single-line `if`/`for` bodies
- **No `var`** unless type is obvious from RHS (e.g. `var user = new User()`)
- **TreatWarningsAsErrors** is enabled globally

---

## Commit Messages

[Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`
Scopes: `core`, `api`, `host`, `infrastructure`, `frontend`, `updater`, `docs`

Examples:
```
feat(core): add DuelGame chat game implementation
fix(infrastructure): handle expired refresh token on startup
docs: update CONTRIBUTING setup instructions
```

---

## Pull Request Process

1. Open PR against `develop` (not `main`)
2. Ensure CI passes (build + tests)
3. Link related issue if one exists
4. Request review — maintainers aim to respond within a few days

---

## How to Add a System Command

System commands live in `Wrkzg.Core/SystemCommands/`. They are auto-discovered via assembly scan — no manual registration needed.

**Step 1:** Create a new class implementing `ISystemCommand`:

```csharp
namespace Wrkzg.Core.SystemCommands;

public class MyCommand : ISystemCommand
{
    public string Trigger => "!mycommand";
    public string[] Aliases => [];
    public string Description => "A short description for the dashboard.";
    public PermissionLevel RequiredPermission => PermissionLevel.Everyone;

    public async Task<string?> ExecuteAsync(
        ChatMessage message,
        IServiceProvider services,
        CancellationToken ct = default)
    {
        return $"Hello, {message.DisplayName}!";
    }
}
```

**Step 2:** Add unit tests in `Wrkzg.Core.Tests/SystemCommands/MyCommandTests.cs`.

**Step 3:** That's it. The command is automatically registered in DI and shows up in the dashboard under System Commands (with enable/disable toggle and custom response override).

---

## How to Add an API Endpoint

**Step 1:** Create `Wrkzg.Api/Endpoints/MyFeatureEndpoints.cs`:

```csharp
namespace Wrkzg.Api.Endpoints;

public static class MyFeatureEndpoints
{
    public static void MapMyFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/myfeature").WithTags("MyFeature");

        group.MapGet("/", async (IMyFeatureService service) =>
            Results.Ok(await service.GetAllAsync()));
    }
}
```

**Step 2:** Register in `Wrkzg.Host/Program.cs`:

```csharp
app.MapMyFeatureEndpoints();
```

**Step 3:** Add integration tests in `Wrkzg.Api.Tests/`.

---

## How to Add an Effect Type

Effect types are the building blocks of the Effect System (Automations). They live in `Wrkzg.Core/Effects/EffectTypes/` and are auto-registered in DI.

**Step 1:** Create a new class implementing `IEffectType`:

```csharp
namespace Wrkzg.Core.Effects.EffectTypes;

public class MyCustomEffect : IEffectType
{
    public string Id => "my_custom_effect";
    public string DisplayName => "My Custom Effect";
    public string[] ParameterKeys => new[] { "message" };

    public async Task ExecuteAsync(
        EffectExecutionContext context,
        CancellationToken ct = default)
    {
        string message = context.ResolveVariables(
            context.GetParameter("message"));

        // Your effect logic here
    }
}
```

**Step 2:** Register it in `Wrkzg.Core/DependencyInjection.cs`:

```csharp
services.AddSingleton<IEffectType, MyCustomEffect>();
```

**Step 3:** The effect is now available in the Automations editor under its `DisplayName`. Users configure it using the `ParameterKeys` you defined.

**Available context methods:**
- `context.GetParameter("key")` — get a parameter from the effect's JSON config
- `context.ResolveVariables("Hello {user}")` — replace `{user}`, trigger data, and custom variables
- `context.Trigger.Username` — the user who triggered the automation
- `context.Trigger.GetData("key")` — get event-specific data

Similarly, you can add new **Trigger Types** (`ITriggerType` in `Effects/Triggers/`) and **Condition Types** (`IConditionType` in `Effects/Conditions/`).

### How to Add an Integration

Integrations (Discord, OBS, etc.) store credentials in the OS keychain via `ISecureStorage.SaveSecretAsync()` / `LoadSecretAsync()`. Never store secrets in the Settings database.

```csharp
// Save a secret
await secureStorage.SaveSecretAsync("my_integration_api_key", apiKey, ct);

// Load a secret
string? apiKey = await secureStorage.LoadSecretAsync("my_integration_api_key", ct);
```

This uses DPAPI on Windows and Keychain on macOS — the same mechanism as Twitch OAuth tokens.
