<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Modules (Desktop)

## Purpose
Business modules for the WPF desktop client. Each module encapsulates a domain area (Auth, Patients, Herbs, Formula, MedicalCase, Registration, Sync, Users) and follows the Prism module pattern with `IModule` registration, region-based navigation, and ViewModels inheriting from `UnifiedViewModelBase`. Modules are strictly isolated — cross-module references are forbidden.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Desktop.Auth/ | Authentication and login module |
| LYBT.Desktop.Patients/ | Patient management — list, detail, search, CRUD |
| LYBT.Desktop.Herbs/ | Herb (TCM medicine) catalog management |
| LYBT.Desktop.Formula/ | Formula (empirical recipe) management |
| LYBT.Desktop.MedicalCase/ | Medical case (consultation + prescription) — the primary clinical workflow |
| LYBT.Desktop.Registration/ | Patient registration and appointment scheduling |
| LYBT.Desktop.Sync/ | Data synchronization between local and remote modes |
| LYBT.Desktop.Users/ | User account management |

## For AI Agents

### Working In This Directory
- Each module is a self-contained Prism `IModule` — registered in `{Domain}Module.cs`.
- Modules MUST NOT reference each other. Cross-module communication goes through shared services or Prism `IEventAggregator`.
- All ViewModels inherit from `UnifiedViewModelBase` (single entity) or `UnifiedListViewModelBase<T>` (list/grid).
- Navigation uses Prism region-based navigation: `_regionManager.RequestNavigate("MainRegion", nameof(SomeView))`.
- Data access: inject `I{Entity}Repository` for CRUD, `I{Entity}DataManager` for aggregate roots (e.g., `IMedicalCaseDataManager`).

### Common Patterns
- **Module registration**: `public class {Domain}Module : IModule { void RegisterTypes(IContainerRegistry) {...} }`
- **ViewModel lifecycle**: `OnNavigatedTo` / `OnNavigatedFrom` for Prism navigation awareness
- **List pattern**: `UnifiedListViewModelBase<T>` with built-in paging, filtering, selection
- **Aggregate pattern**: MedicalCase module uses `IMedicalCaseDataManager` to orchestrate Consultation + Prescription

## Dependencies

### Internal
- [Core/](../Core/AGENTS.md) — `LYBT.Desktop.Contracts`, `LYBT.Desktop.Foundation`, `LYBT.Desktop.Infrastructure`, `LYBT.Desktop.Models`
- [Shared/](../../../../Shared/AGENTS.md) — `LYBT.Shared.Models` (DTOs), `LYBT.Shared.Validators`

### External
- Prism.DryIoc (MVVM, DI, navigation)
- AutoMapper / Riok.Mapperly (object mapping)

<!-- MANUAL: -->
