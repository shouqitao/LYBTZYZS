# LYBT.WebAPI - ASP.NET Core WebAPI Entry Point

**Purpose**: API gateway, middleware stack, controller layer, Serilog bootstrap.

## Structure

```
LYBT.WebAPI/
├── Program.cs               # Minimal hosting, two-phase Serilog
├── Controllers/             # 13 API controllers
├── Middleware/              # CorrelationId, ClaimsNormalization, SecurityHeaders
├── Extensions/              # DI, middleware, initialization extensions
└── Filters/                 # API logging filter
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| API entry | `Program.cs` | WebApplication.CreateBuilder, .env loading, Windows service support |
| Controllers | `Controllers/` | REST endpoints, 13 controllers |
| Middleware | `Middleware/` | Request pipeline: CorrelationId → ClaimsNormalization → SecurityHeaders |
| Middleware config | `Extensions/UnifiedMiddlewareConfiguration.cs` | Middleware ordering |
| App init | `Extensions/UnifiedApplicationInitialization.cs` | Initialization flow |
| DI registrations | `Extensions/ApiServiceCollectionExtensions.cs` | API-specific services |
| Serilog SQL | `Extensions/SerilogMSSqlServerExtensions.cs` | SQL Server sink |

## CONVENTIONS

- **Minimal hosting** — ASP.NET Core 8 `WebApplication` pattern (no Startup.cs)
- **Two-phase Serilog** — Bootstrap logger for startup exceptions, then final logger
- **Environment-aware** — .env loading, Test path bypasses heavy logging
- **Module registration** — `AddXModule` extension methods per server module
- **Health checks** — Built-in health check endpoint

## ANTI-PATTERNS

- **Service injecting DbContext** — Must use Repository interface (architecture test enforced)
- **Cross-module references** — Server modules MUST NOT reference each other
- **Direct entity exposure** — Always use DTOs from `Shared.Models`
