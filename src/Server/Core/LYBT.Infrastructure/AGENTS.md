# LYBT.Infrastructure - EF Core Data Layer

**Purpose**: AppDbContext, BaseRepository, EF configurations, DI for repositories.

## Structure

```
LYBT.Infrastructure/
├── Data/
│   ├── AppDbContext.cs          # EF Core DbContext (SQL Server dual-mode: Remote + LocalDB)
│   └── Configurations/          # 17 IEntityTypeConfiguration implementations
└── DependencyInjection/
    └── RepositoryServiceCollectionExtensions.cs  # DI wiring
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| DbContext | `Data/AppDbContext.cs` | SQL Server (Remote + LocalDB) |
| Entity configs | `Data/Configurations/` | Fluent API configurations per entity |
| Repository DI | `DependencyInjection/RepositoryServiceCollectionExtensions.cs` | Register all repositories |
| BaseRepository | `Data/BaseRepository.cs` | 21 public methods, generic CRUD |

## CONVENTIONS

- **Provider-agnostic** — DbContext works with SQL Server (remote) or SQL Server LocalDB (local)
- **Soft delete** — Global query filter `IsDeleted` on entities; use `IgnoreQueryFilters()` for soft-deleted records
- **Repository pattern** — `BaseRepository<T>` implements `IRepository<T>`; service layer injects repository interfaces
- **EntityTypeConfiguration** — Separate config classes per entity in `Data/Configurations/`

## ANTI-PATTERNS

- **FindAsync on soft-deleted** — Applies `IsDeleted` filter when not in ChangeTracker; use `IgnoreQueryFilters()`
- **Service injecting DbContext** — Architecture test `P10_Services_Should_Not_Directly_Inject_AppDbContext` enforces Repository-only
- **Raw SQL without parameterization** — Security risk; use EF Core LINQ or parameterized SQL
