# LYBT.Desktop.MedicalCase - Desktop MedicalCase Module

**Purpose**: Desktop UI module for medical case management (DDD aggregate root).

## Structure

```
LYBT.Desktop.MedicalCase/
├── ViewModels/          # MedicalCase, Consultation, Prescription ViewModels
├── Views/               # XAML views for medical case UI
├── Services/            # Module-specific services
├── Extensions/          # PrescriptionImportExtensions
└── MedicalCaseModule.cs # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `MedicalCaseModule.cs` | Prism IModule implementation |
| ViewModels | `ViewModels/` | UnifiedViewModelBase derivatives |
| Prescription import | `Extensions/PrescriptionImportExtensions.cs` | Import logic |

## CONVENTIONS

- **ViewModel base** — `UnifiedViewModelBase` / `UnifiedListViewModelBase<T>`
- **Object mapping** — Riok.Mapperly (compile-time) + AutoMapper (runtime fallback)
- **Navigation** — Prism Region-based between modules
- **Data access** — `IMedicalCaseDataManager` for aggregate operations

## ANTI-PATTERNS

- **Direct repository access in ViewModel** — Use DataManager for aggregates
- **Cross-module references** — MUST NOT reference other Desktop modules
- **HasPrescription not set** — Mapper must explicitly set computed `HasPrescription` property
