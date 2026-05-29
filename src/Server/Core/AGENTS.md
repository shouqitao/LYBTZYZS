<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Core (Server)

## Purpose
Core libraries for the ASP.NET Core backend. Contains domain entities (anemic model, except MedicalCaseModel which is a DDD aggregate root) and EF Core infrastructure (DbContext, BaseRepository, entity configurations). These libraries form the foundation that all server modules depend on.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Entities/ | Domain entities — POCOs for all business objects (Patient, Herb, Formula, MedicalCase, Consultation, Prescription, User, etc.) |
| LYBT.Infrastructure/ | EF Core infrastructure — `AppDbContext`, `BaseRepository<T>`, entity type configurations, migrations |
| Documentation/ | Internal architecture documentation |

## For AI Agents

### Working In This Directory
- Entities are anemic models (properties + getters/setters only) EXCEPT `MedicalCaseModel` which is a rich DDD aggregate root with domain methods (`Complete()`, `SaveAsDraft()`, `SoftDelete()`, `UpdateConsultation()`).
- `BaseRepository<T>` provides 21 public methods for CRUD and querying.
- `FindAsync` applies global query filters (`IsDeleted`) when entity is not in ChangeTracker — use `IgnoreQueryFilters()` for soft-deleted records.
- `MedicalCase.HasPrescription` is a computed property depending on `PrescriptionId.HasValue` — Mapper must set it explicitly.

### Common Patterns
- **Entity configuration**: Fluent API via `IEntityTypeConfiguration<T>` in Infrastructure
- **Soft delete**: `IsDeleted` flag + global query filter on most entities
- **Repository**: `BaseRepository<T>` implements `IRepository<T>` with standard CRUD + paging

## Dependencies

### Internal
- [Shared/](../../Shared/AGENTS.md) — `LYBT.Shared.Primitives`, `LYBT.Shared.Models`

### External
- Entity Framework Core 8
- Microsoft.EntityFrameworkCore.SqlServer

<!-- MANUAL: -->
