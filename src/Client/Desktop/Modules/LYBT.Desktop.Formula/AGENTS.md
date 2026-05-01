# LYBT.Desktop.Formula - Desktop Formula Module

**Purpose**: Desktop UI module for formula (验方) management with Master-Detail composition pattern.

## Structure

```
LYBT.Desktop.Formula/
├── CommandHandlers/     # IFormulaCommandHandler (dead code)
├── Controls/            # FormulaEditControl, FormulaMasterDetailControl
├── Interfaces/          # IFormulaRepository, IFormulaService, IFormulaSearchProvider
├── Mappers/             # FormulaDetailModelMapper, FormulaHerbItemMapper, FormulaMapper
├── Models/              # FormulaDetailModel, FormulaItem, FormulaHerbItem
├── Repositories/        # FormulaRepository (DataSource abstraction)
├── Services/            # FormulaService, FormulaSearchProvider, FormulaValidator
├── ViewModels/          # FormulaMasterDetailViewModel, FormulaHerbItemViewModel
└── FormulaModule.cs     # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `FormulaModule.cs` | Depends on HerbsModule |
| ViewModel logic | `ViewModels/FormulaMasterDetailViewModel.cs` | MasterDetailViewModelBase derivative |
| Repository (Local/Remote) | `Repositories/FormulaRepository.cs` | DataSource abstraction layer |
| Cross-module search | `Services/FormulaSearchProvider.cs` | IFormulaSearchProvider for MedicalCase |
| Mapperly mappers | `Mappers/` | FormulaDetailModelMapper, FormulaHerbItemMapper |

## CONVENTIONS

- **ViewModel base** — `MasterDetailViewModelBase<ListDto, DetailModel>` (V2 composition pattern)
- **Control reuse** — FormulaMasterDetailControl embedded in Admin + Clinical role workspaces
- **DataSource abstraction** — Repository delegates to IFormulaDataSource (Local/Remote)
- **Mapperly** — Compile-time mapping, zero runtime overhead
- **IsShared/IsPersonal inversion** — FormulaMapper must manually map `IsShared = !IsPersonal`

## ANTI-PATTERNS

- **FormulaCommandHandler dead code** — Registered nowhere, no external consumers
- **FormulaValidator unused** — Registered in DI but not injected into ViewModel
- **Cross-module references** — MUST NOT reference other Desktop modules directly
- **Herbs集合手动映射** — FormulaDetailModelMapper cannot auto-map ObservableCollection
