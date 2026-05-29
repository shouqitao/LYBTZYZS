<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.LocalWebAPI

## Purpose

Embedded ASP.NET Core WebAPI that runs inside the Desktop client process for local/offline mode. Provides a full REST API surface (auth, patients, herbs, formulas, medical cases, registrations, users) backed by SQL Server LocalDB, mirroring the remote server API. The Desktop client connects to this local API via the same HTTP client used for remote mode, enabling the dual-mode architecture (ADR-0002).

## Key Files

| File | Description |
|------|-------------|
| `LocalWebApiProgram.cs` | Static entry point; creates WebApplicationBuilder, configures DbContext, JWT auth, and controllers; exposes `RunAsync()` for desktop host |
| `Program.cs` | Standalone entry point for development/testing on port 5290 |
| `LYBT.LocalWebAPI.csproj` | Project file; ASP.NET Core SDK, SQL Server, JWT; InternalsVisibleTo LYBT.Tests.Desktop |
| `appsettings.json` | Local configuration (connection string, JWT settings) |
| `Auth/LocalJwtConfig.cs` | JWT bearer authentication configuration for local mode |
| `Data/LocalWebApiDbContext.cs` | DbContext for local mode using SQL Server |
| `Data/LocalWebApiSeedData.cs` | Seed data initialization (users, default data) |
| `Mappers/LocalApiMapper.cs` | Entity-to-DTO mapping for local API responses |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Auth/` | JWT configuration for local mode authentication |
| `Controllers/` | 10 API controllers mirroring remote server endpoints |
| `Data/` | DbContext and seed data for local SQL Server database |
| `Mappers/` | Entity-to-DTO mapping (Riok.Mapperly) |
| `Repositories/` | 6 Http*Repository implementations for local data access |
| `Properties/` | Launch settings |

## For AI Agents

### Working In This Directory

- This project serves as the local-mode server embedded in the Desktop client. Controllers mirror the remote WebAPI endpoints.
- Controllers use `LocalWebApiDbContext` directly (no service/repository layer for simplicity), unlike the server which uses 3-layer architecture.
- The `LocalWebApiProgram` class is designed to be called from the Desktop host process, not just as a standalone app.
- `InternalsVisibleTo` grants access to `LYBT.Tests.Desktop` for integration testing.
- When adding new endpoints, ensure both this local API and the remote `LYBT.WebAPI` expose the same contract.

### Testing Requirements

- Tested via `LYBT.Tests.Desktop` which uses the local WebAPI for full-stack testing.
- Run standalone with `dotnet run --project src/Client/Desktop/LocalWebAPI` for manual testing on port 5290.
- Uses real SQL Server (LocalDB), not EF InMemory provider.

### Common Patterns

- **Direct DbContext injection** -- Controllers inject `LocalWebApiDbContext` directly (simplified for local mode).
- **Seed data** -- `LocalWebApiSeedData.SeedAsync()` initializes default users and reference data on first run.
- **Same DTOs** -- Uses `LYBT.Shared.Models` DTOs to maintain API contract parity with remote server.
- **JWT auth** -- Same JWT bearer scheme as remote server, configured via `LocalJwtConfig`.

## Dependencies

### Internal

- `LYBT.Shared.Models` -- DTOs and contracts (API contract parity)
- `LYBT.Desktop.Contracts` -- Interface definitions
- `LYBT.Shared.Logging` -- Logging abstraction
- `LYBT.Entities` -- Server domain entities
- `LYBT.Infrastructure` -- Server DbContext and base infrastructure
- `LYBT.Shared.Utilities` -- Utility classes (password hashing, etc.)

### External

- `Microsoft.EntityFrameworkCore.SqlServer` -- SQL Server provider
- `Microsoft.EntityFrameworkCore` -- EF Core ORM
- `System.IdentityModel.Tokens.Jwt` -- JWT token handling
- `Microsoft.AspNetCore.Authentication.JwtBearer` -- JWT bearer auth

<!-- MANUAL: -->
