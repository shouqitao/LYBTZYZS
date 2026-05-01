# LYBT.Module.Formula - Server Formula Module

**Purpose**: Server-side formula (验方) management with traditional 3-layer architecture.

## Structure

```
LYBT.Module.Formula/
├── Interfaces/          # IFormulaService, IFormulaImportExportService, IFormulaRepository
├── Services/            # FormulaService, FormulaImportExportService
├── Repositories/        # FormulaRepository
├── Mapping/             # FormulaMapper (Mapperly)
└── FormulaModule.cs     # Module registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| CRUD operations | `Services/FormulaService.cs` | Standard 3-layer |
| Import/export | `Services/FormulaImportExportService.cs` | SRP-split from FormulaService |
| Repository queries | `Repositories/FormulaRepository.cs` | Include Herbs, IgnoreQueryFilters |
| Mapperly mapping | `Mapping/FormulaMapper.cs` | Indication↔Indications MapProperty |

## CONVENTIONS

- **Traditional 3-layer** — Controller→Service→Repository, not CQRS
- **Herbs collection** — UpdateAsync uses Clear()+Add() full replace (Design Decision 002)
- **Cross-module** — Uses IHerbCrossModuleService for herb queries
- **Mapperly** — Compile-time mapping, MapProperty for Indication↔Indications

## ANTI-PATTERNS

- **FindAsync with soft-delete** — Use IgnoreQueryFilters() + FirstOrDefaultAsync for deleted records
- **Mapper for Herbs** — FormulaHerbItem needs manual construction (OriginalHerbName, IsValidated)
- **BatchUpdateStatusAsync** — Per-item UpdateAsync may trigger multiple SaveChanges
