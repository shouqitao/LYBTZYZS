<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Services (Server)

## Purpose
Contains the ASP.NET Core WebAPI entry point project. Configures middleware, dependency injection, authentication, CORS, Swagger, and registers all server modules. This is the host application that ties together all backend modules.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.WebAPI/ | ASP.NET Core WebAPI host — `Program.cs`, middleware pipeline, module registration, launch configuration |

## For AI Agents

### Working In This Directory
- `Program.cs` is the entry point — configures DI, middleware, auth, CORS, and registers all modules.
- When adding a new module, register it in the DI container in `Program.cs`.
- API routes follow `/api/{module}/` convention.
- Run locally: `dotnet run --project src/Server/Services/LYBT.WebAPI`

### Testing Requirements
- Integration tests in `tests/LYBT.Tests.Server/` test against this WebAPI host.
- Use `scripts/run-webapi.ps1` to start the server for manual testing.

### Common Patterns
- **Startup**: Minimal API hosting model with `WebApplication.CreateBuilder`
- **Module registration**: Each module registers its own services via extension methods

## Dependencies

### Internal
- [Modules/](../Modules/AGENTS.md) — All business modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Sync)
- [Core/](../Core/AGENTS.md) — `LYBT.Infrastructure` (DbContext, repositories)

### External
- ASP.NET Core 8
- Swashbuckle (Swagger/OpenAPI)
- Microsoft.AspNetCore.Authentication.JwtBearer

<!-- MANUAL: -->
