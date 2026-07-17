# LYBT.Desktop.MedicalCase - Desktop MedicalCase Module

**Purpose**: Desktop UI module for medical case management (DDD aggregate root).

## Structure

```
LYBT.Desktop.MedicalCase/
├── Controls/            # Custom controls (prescription grid, herb selector, etc.)
├── Dialogs/             # Dialog windows (confirm, reason input, etc.)
├── Extensions/          # PrescriptionImportExtensions
├── Interfaces/          # Module-level interfaces
├── Mappers/             # Entity-to-model mappers
├── Models/              # Module-specific models
├── Repositories/        # MedicalCase repository
├── Services/            # Module-specific services
├── ViewModels/          # MedicalCase, Consultation, Prescription ViewModels
├── Views/               # XAML views for medical case UI
└── MedicalCaseModule.cs # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `MedicalCaseModule.cs` | Prism IModule implementation |
| ViewModels | `ViewModels/` | MasterDetailViewModelBase derivatives |
| Prescription import | `Extensions/PrescriptionImportExtensions.cs` | Import logic |

## CONVENTIONS

- **ViewModel base** — `MasterDetailViewModelBase<TListDto, TDetailModel>`
- **Object mapping** — Riok.Mapperly (compile-time; AutoMapper is forbidden)
- **Navigation** — Prism Region-based between modules
- **Data access** — `IMedicalCaseDataManager` for aggregate operations

## ANTI-PATTERNS

- **Direct repository access in ViewModel** — Use DataManager for aggregates
- **Cross-module references** — MUST NOT reference other Desktop modules
- **HasPrescription not set** — Mapper must explicitly set computed `HasPrescription` property
