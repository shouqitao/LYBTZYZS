# LYBT.Module.Herbs - Server Herbs Module

**Purpose**: Server-side herb (中药材) management with Record-Only mode (no inventory).

## Structure

```
LYBT.Module.Herbs/
├── Interfaces/          # IHerbService, IHerbRepository
├── Services/            # HerbService
├── Repositories/        # HerbRepository
├── Mapping/             # HerbMapper (Mapperly)
└── HerbsModule.cs       # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| CRUD + import/export | `Services/HerbService.cs` | FluentValidation + pinyin generation |
| Reference checking | `Services/HerbService.cs` | CheckReferenceAsync (cross-aggregate query) |
| Pinyin search | `Repositories/HerbRepository.cs` | GetByNameOrPinyinAsync |
| Excel import | `Services/HerbService.cs` | EPPlus, DuplicateStrategy |

## CONVENTIONS

- **Record-Only** — Manages herb profiles only, no inventory
- **Pinyin search** — PinyinAbbreviation for fast lookup ("dg" → "当归")
- **Reference check** — DeleteAsync forces CheckReferenceAsync before deletion
- **Cross-module** — IHerbCrossModuleService exposes read-only queries to Formula/MedicalCase

## ANTI-PATTERNS

- **Direct AppDbContext** — CheckReferenceAsync bypasses Repository for cross-aggregate queries
- **Two import paths** — ImportFromExcelAsync (Server-side) vs BatchImportAsync (Client-side DTO)
- **FindAsync with soft-delete** — Use IgnoreQueryFilters() for Restore operations
