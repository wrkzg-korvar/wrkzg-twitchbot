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
- [How to Add a Chat Game](#how-to-add-a-chat-game)
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

# Install frontend dependencies
cd src/Wrkzg.Frontend && npm install && cd ../..

# Restore .NET packages
dotnet restore

# Verify everything builds
dotnet build
```

### Twitch App for Local Development

You need your own Twitch Developer Application to test OAuth and API calls:

1. Go to [dev.twitch.tv/console](https://dev.twitch.tv/console)
2. Register a new application — redirect URI: `http://localhost:5000/auth/callback`
3. Create `src/Wrkzg.Api/appsettings.Development.json` (this file is gitignored):

```json
{
  "Twitch": {
    "ClientId": "your_client_id_here"
  },
  "Bot": {
    "Port": 5000
  }
}
```

### Running in Development Mode

```bash
# Terminal 1: Start the .NET backend
dotnet run --project src/Wrkzg.Host

# Terminal 2: Start the frontend dev server (Vite with hot reload)
cd src/Wrkzg.Frontend && npm run dev
```

The Photino window opens automatically. The React frontend at `localhost:5173` proxies API calls to `localhost:5000`.

---

## Project Structure

```
wrkzg/
├── src/
│   ├── Wrkzg.Host/           Executable — entry point, Photino window, DI bootstrap
│   ├── Wrkzg.Api/            Class Library (Web SDK) — REST endpoints, SignalR hubs
│   ├── Wrkzg.Core/           Class Library — business logic, interfaces, domain models
│   ├── Wrkzg.Infrastructure/ Class Library — EF Core, SQLite, Twitch clients
│   ├── Wrkzg.Updater/        Executable — standalone auto-update process
│   └── Wrkzg.Frontend/       React + TypeScript SPA (not a .NET project)
├── tests/
│   ├── Wrkzg.Core.Tests/     Unit tests for Core
│   └── Wrkzg.Api.Tests/      Integration tests for API endpoints
└── docs/                     This documentation
```

---

## Architecture Rules

These rules are enforced during code review. Please read them carefully.

### Dependency Direction

Dependencies only point **inward**. The Core layer has no knowledge of outer layers:

```
Host → Api → Core ← Infrastructure
```

- `Wrkzg.Core` must **never** reference `Wrkzg.Infrastructure`, `Wrkzg.Api`, or `Wrkzg.Host`
- `Wrkzg.Infrastructure` must **never** reference `Wrkzg.Api` or `Wrkzg.Host`
- `Wrkzg.Api` must **never** reference `Wrkzg.Host`

### What Goes Where

| Layer | Belongs here | Does NOT belong here |
|---|---|---|
| `Core` | Interfaces, domain models, business logic, chat games | EF Core, HttpClient, Twitch SDK, ASP.NET types |
| `Infrastructure` | DbContext, repositories, Twitch clients, secure storage | Business logic, API controllers |
| `Api` | Controllers, SignalR hubs, DTOs, validators, middleware | Database queries, Twitch API calls |
| `Host` | DI bootstrap, Photino window, app startup, tray icon | Business logic, database access |

### Interface Rule

Every service in `Core` that has a concrete implementation in `Infrastructure` **must** have a corresponding interface in `Core/Interfaces/`. No concrete infrastructure types may appear in Core.

---

## Development Workflow

We use a simple branch strategy:

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
# Run all tests
dotnet test

# Check build is clean
dotnet build

# Format check (Rider/VS does this automatically, or use CLI)
dotnet format --verify-no-changes
```

---

## Coding Conventions

The project uses `.editorconfig` in the repository root. Your IDE will pick this up automatically. Key rules:

- **File-scoped namespaces** — `namespace Wrkzg.Core.Services;` not `namespace Wrkzg.Core.Services { }`
- **Private fields** — `_camelCase` with underscore prefix
- **Async methods** — always end with `Async`, always accept a `CancellationToken ct = default`
- **Explicit usings** — `ImplicitUsings` is disabled; all `using` statements must be written explicitly
- **Braces always** — even for single-line `if`/`for` bodies
- **No `var`** unless the type is obvious from the right-hand side (e.g. `var user = new User()`)

### Example: Correct Service Implementation

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Services;

public class ExampleService : IExampleService
{
    private readonly IUserRepository _users;
    private readonly ILogger<ExampleService> _logger;

    public ExampleService(IUserRepository users, ILogger<ExampleService> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(string twitchId, CancellationToken ct = default)
    {
        var user = await _users.GetByTwitchIdAsync(twitchId, ct);

        if (user is null)
        {
            _logger.LogDebug("User {TwitchId} not found", twitchId);
        }

        return user;
    }
}
```

---

## Commit Messages

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>

[optional body]

[optional footer]
```

**Types:**

| Type | When to use |
|---|---|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or updating tests |
| `chore` | Build process, dependency updates, tooling |
| `perf` | Performance improvement |

**Scope** is the project or area affected: `core`, `api`, `host`, `infrastructure`, `frontend`, `updater`, `docs`.

**Examples:**

```
feat(core): add DuelGame chat game implementation
fix(infrastructure): handle expired refresh token on startup
docs: update CONTRIBUTING setup instructions
chore(deps): bump TwitchLib.Api to 3.10.1
test(core): add unit tests for CommandProcessor cooldown logic
```

---

## Pull Request Process

1. Open your PR against the `develop` branch (not `main`)
2. Fill out the PR template
3. Ensure CI passes (build + tests)
4. Link the related issue if one exists
5. Request a review — maintainers aim to respond within a few days
6. Address review comments, then re-request review

PRs that break the architecture rules (see above) will be asked to restructure before merging.

---

## How to Add a Chat Game

Chat games live in `Wrkzg.Core/ChatGames/`. They are auto-discovered via assembly scan — no manual registration needed.

**Step 1:** Create a new class in `Wrkzg.Core/ChatGames/`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.ChatGames;

/// <summary>
/// Example chat game — replace with your implementation.
/// </summary>
public class MyGame : IChatGame
{
    public string Name => "MyGame";
    public string TriggerCommand => "!mygame";
    public string Description => "A short description shown in the dashboard.";
    public bool IsActive => false; // return true when a round is in progress

    public Task<bool> StartAsync(ChatMessage initiator, CancellationToken ct = default)
    {
        // Called when the trigger command is used
        return Task.FromResult(true);
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> HandleMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        // Called for every chat message while the game is active
        return Task.FromResult(false);
    }
}
```

**Step 2:** Add unit tests in `Wrkzg.Core.Tests/ChatGames/MyGameTests.cs`.

**Step 3:** That's it. The game is automatically registered in DI and shown in the dashboard.

---

## How to Add an API Endpoint

API endpoints live in `Wrkzg.Api/Endpoints/` as static extension methods.

**Step 1:** Create `Wrkzg.Api/Endpoints/MyFeatureEndpoints.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
