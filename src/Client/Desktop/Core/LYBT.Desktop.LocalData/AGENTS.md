<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.LocalData

## Purpose
Local data storage layer implementing the dual-mode architecture's local mode. Uses SQL Server LocalDB with EF Core to provide an embedded database that runs alongside the desktop application, eliminating the need for a separate server. Contains `LocalDbContext` (mirrors the server's `AppDbContext` entity set), entity-to-DTO mappers (Mapperly), `DatabaseInitializer` for idempotent schema creation and seed data, `LocalAuthService` for local authentication with BCrypt, `LocalDbBackupService` for backup/restore, and `SyncService` for bidirectional synchronization with the remote server. This project is the client-side counterpart to the server's `LYBT.Infrastructure` -- they share the same entity definitions from `LYBT.Entities`.

## Key Files
| File | Description |
|------|-------------|
| `Context/LocalDbContext.cs` | EF Core DbContext for SQL Server LocalDB; mirrors server entity set |
| `Initialization/DatabaseInitializer.cs` | Thread-safe, idempotent DB creation + seed data with performance monitoring |
| `Initialization/SeedData.cs` | Default data seeding (admin user, reference data) |
| `Services/LocalAuthService.cs` | Local authentication using BCrypt password hashing |
| `Services/LocalDbBackupService.cs` | Database backup and restore operations |
| `Services/SyncService.cs` | Bidirectional sync between local and remote databases |
| `Helpers/ChecksumHelper.cs` | Data integrity checksums for sync conflict detection |
| `Mappers/LocalPatientMapper.cs` | Mapperly entity-to-DTO mapper for Patient |
| `Mappers/LocalMedicalCaseMapper.cs` | Mapperly mapper for MedicalCase (aggregate root) |
| `Mappers/LocalFormulaMapper.cs` | Mapperly mapper for Formula |
| `Mappers/LocalHerbMapper.cs` | Mapperly mapper for Herb |
| `Mappers/LocalUserMapper.cs` | Mapperly mapper for User |
| `Mappers/LocalRegistrationMapper.cs` | Mapperly mapper for Registration |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Context/` | EF Core DbContext definition |
| `Helpers/` | Utility classes (ChecksumHelper for sync) |
| `Initialization/` | Database creation and seed data |
| `Mappers/` | Mapperly compile-time entity-to-DTO mappers |
| `Services/` | Local auth, backup, and sync services |

## For AI Agents

### Working In This Directory
- This project references `LYBT.Entities` directly (server entities) -- it is one of the few client projects allowed to reference server core
- All mappers use `Riok.Mapperly` (compile-time source generation) -- edit the `*Mapper.cs` files, not generated code
- `DatabaseInitializer` uses double-checked locking with `SemaphoreSlim` -- thread-safe and idempotent
- `LocalDbContext` connects to SQL Server LocalDB (not SQLite despite the "LocalData" name) -- connection string is configured via `LYBT.Shared.Configuration`
- SyncService handles bidirectional sync -- be careful with conflict resolution logic
- This module does NOT use Prism modules (no `IModule`) -- services are registered by the Shell or Infrastructure layer

### Testing Requirements
- Tests in `LYBT.Tests.Desktop` use real LocalDB + Respawn for clean state per test
- DatabaseInitializer tests should verify idempotency (calling EnsureInitializedAsync twice)
- Mapper tests should verify round-trip correctness (entity -> DTO -> entity)
- Auth tests should verify BCrypt hash/verify cycle

### Common Patterns
- Repository pattern: services return DTOs, not entities
- Mapperly `[Mapper]` attributes for compile-time mapping generation
- `IDatabaseInitializer` interface (from Contracts) for testability
- `IPerformanceMonitor` optional injection for timing database operations
- Factory pattern for DbContext: `Func<LocalDbContext>` injected instead of DbContext directly

## Dependencies

### Internal
- `LYBT.Entities` -- Server-side domain entities (shared entity definitions)
- `LYBT.Desktop.Contracts` -- Interface definitions (IDatabaseInitializer, IViewModelServices)
- `LYBT.Shared.Models` -- DTOs and contracts
- `LYBT.Shared.Utilities` -- Shared utility classes
- `LYBT.Shared.Validators` -- FluentValidation rules
- `LYBT.Shared.Configuration` -- Configuration models (connection strings)

### External
- `Microsoft.EntityFrameworkCore.SqlServer` -- EF Core SQL Server provider (LocalDB)
- `Microsoft.EntityFrameworkCore.Design` -- EF Core tooling
- `BCrypt.Net-Next` -- Password hashing
- `Riok.Mapperly` -- Compile-time object mapping
- `Microsoft.Extensions.Logging.Abstractions` -- Logging
- `Microsoft.Extensions.Options` -- Options pattern

<!-- MANUAL: -->
