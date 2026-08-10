# LYBT.Desktop.Controls - Desktop Shared Controls

**Purpose**: Shared WPF control library (DataGridToolbar, MasterDetailLayout, HerbList, converters, themes).

## Structure

```
LYBT.Desktop.Controls/
├── Controls/           # Custom controls (HerbList, HerbItem, BreadcrumbBar, StatusBadge, etc.)
├── Converters/         # IValueConverter implementations (Cvt.BoolToVis, etc.)
├── Themes/             # Resource dictionaries (dark/light palettes)
├── Helpers/            # Utility helpers
└── ViewModels/         # Control-level ViewModels (HerbListControlViewModel, etc.)
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Herb list control | `Controls/HerbList/` | HerbListControl + HerbListControlViewModel (IEnumerable DP contract, B1 2026-08-10) |
| Herb item control | `Controls/HerbItem/` | HerbItemControl + HerbItemControlViewModel (implements IHerbItemEditable) |
| DataGridToolbar | `Controls/DataGridToolbar/` | Shared toolbar w/ built-in Create/Refresh |
| MasterDetail layout | `Controls/MasterDetail/` | MasterDetailLayout + MasterDetailControlBase |
| Converters | `Converters/` | Cvt static class (BoolToVis, inverse, etc.) |
| Theme resources | `Themes/` | MDIX palette + dark overrides |

## CONVENTIONS

- **External contract** — HerbListControl.HerbItems DP is `IEnumerable` (accepts DTO or IHerbItemEditable; B1 de-coupled)
- **Internal adapter** — Controls internally use `PrescriptionItemDto` as exchange shape (LoadFromDto/ToDto); UI-edit path stays in module Models
- **Object mapping** — Riok.Mapperly (HerbItemMapper VM→DTO)
- **No shared VM base** — Control VMs derive from `ObservableObject` directly; DataContext set via `new` on LayoutRoot (avoids UserControl DataContext coupling)
- **XAML** — MDIX built-in styles + spacing tokens; no custom ControlTemplates

## ANTI-PATTERNS

- **Direct DTO binding in module Views** — Use Model/VM; DTO only at control internal adapter
- **Cross-module references** — MUST NOT reference Desktop business modules
- **Hardcoded colors** — Use theme resources from `Themes/`
