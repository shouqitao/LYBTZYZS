# LYBT.Desktop.Herbs - Desktop Herbs Module

**Purpose**: Desktop UI module for herb (中药材) management with pinyin search and batch import.

## Structure

```
LYBT.Desktop.Herbs/
├── Controls/            # HerbMasterDetailControl, HerbEditControl, HerbViewControl
├── Interfaces/          # IHerbRepository
├── Mappers/             # HerbMapper (Mapperly)
├── Models/              # HerbDetailModel
├── Repositories/        # HerbRepository (DataSource abstraction)
├── Services/            # HerbSearchProvider
├── ViewModels/          # HerbMasterDetailViewModel
└── HerbsModule.cs       # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `HerbsModule.cs` | Depends on AuthenticationModule |
| ViewModel logic | `ViewModels/HerbMasterDetailViewModel.cs` | MasterDetailViewModelBase derivative |
| Pinyin search | `Services/HerbSearchProvider.cs` | IHerbSearchProvider for Formula/MedicalCase |
| Three-control split | `Controls/` | MasterDetail + Edit + View controls |
| Repository (Local/Remote) | `Repositories/HerbRepository.cs` | DataSource abstraction layer |

## CONVENTIONS

- **ViewModel base** — `MasterDetailViewModelBase<ListDto, DetailModel>` (V2 composition pattern)
- **Control reuse** — HerbMasterDetailControl embedded in Admin + Clinical role workspaces
- **Three-control separation** — HerbMasterDetailControl + HerbEditControl + HerbViewControl
- **DataSource abstraction** — Repository delegates to IHerbDataSource (Local/Remote)
- **Packaging methods** — CreateWithResultAsync returns (success, data, error) tuple

## ANTI-PATTERNS

- **Name auto-generates PinYinCode** — HerbDetailModel.Name setter triggers PinYinHelper; Clone() bypasses via private fields
- **Import/Export Remote-only** — BatchImportAsync/ExportTemplateAsync return null in Local mode
- **Cross-module references** — MUST NOT reference other Desktop modules directly
