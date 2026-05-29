<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Module.Sync

## Purpose

Server-side data synchronization module enabling bidirectional sync between local (Desktop/SQLite) and remote (Server/SQL Server) modes. Supports sync for Herb, Patient, Formula, and MedicalCase entities using checksum-based conflict detection and cross-module service interfaces for data access.

## Key Files

| File | Description |
|------|-------------|
| `SyncModule.cs` | Static extension methods `AddSyncModule()` and `UseSyncModule()` for DI and middleware registration |
| `LYBT.Module.Sync.csproj` | Project file; net8.0, EF Core; InternalsVisibleTo LYBT.Module.Sync.Tests |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Interfaces/` | `ISyncRepository`, `ISyncService` -- contract definitions for sync operations |
| `Services/` | `SyncService` (21KB) -- core sync logic; `ChecksumHelper` -- checksum generation for conflict detection |
| `Repositories/` | `SyncRepository` -- data access for sync operations |

## For AI Agents

### Working In This Directory

- Sync supports 4 entity types: Herb, Patient, Formula, MedicalCase (defined in `SupportedTypes`).
- Uses cross-module services (`IHerbCrossModuleService`, `IPatientCrossModuleService`) for data access -- do NOT directly reference other modules.
- `ChecksumHelper` generates checksums for detecting conflicts between local and remote data.
- The `UseSyncModule()` middleware extension currently has no special middleware; reserved for future use.
- Sync DTOs are defined in `LYBT.Shared.Models/Contracts/Sync/`.

### Testing Requirements

- Module has its own test project: `LYBT.Module.Sync.Tests` (InternalsVisibleTo configured).
- Also covered by server integration tests: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Sync"`
- Tests use real SQL Server + Respawn, zero mock.

### Common Patterns

- **Cross-module services** -- Uses `IHerbCrossModuleService` and `IPatientCrossModuleService` for cross-module data access (interface segregation, no direct module references).
- **Checksum-based conflict detection** -- `ChecksumHelper` generates entity checksums for optimistic concurrency.
- **Scoped DI** -- Repository and Service registered as Scoped lifetime.
- **JSON serialization** -- Uses `System.Text.Json` with camelCase naming policy for sync payloads.

## Dependencies

### Internal

- `LYBT.Infrastructure` -- BaseRepository, DbContext, cross-module service interfaces
- `LYBT.Entities` -- Herb, Patient, Formula, MedicalCase entities
- `LYBT.Shared.Models` -- Sync DTOs and contracts
- Cross-module: `IHerbCrossModuleService`, `IPatientCrossModuleService` (from Infrastructure.Services.CrossModule)

### External

- `Microsoft.EntityFrameworkCore` -- EF Core ORM

<!-- MANUAL: -->
