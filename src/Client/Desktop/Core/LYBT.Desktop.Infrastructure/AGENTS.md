# LYBT.Desktop.Infrastructure - WPF Infrastructure Layer

**Purpose**: WPF services, controls, converters, themes, and infrastructure utilities for the Desktop client.

## Structure

```
LYBT.Desktop.Infrastructure/
├── Controls/          # 42 custom WPF controls (largest subdir)
├── Converters/        # 22 IValueConverter implementations
├── Services/          # 21 WPF service implementations
├── Themes/            # 15 theme resource dictionaries
├── Extensions/        # Task, Configuration extension methods
├── Configuration/     # Configuration helpers
└── Behaviors/         # Attached behaviors
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Custom controls | `Controls/` | HerbSelector, PrescriptionGrid, custom TextBoxes |
| Value converters | `Converters/` | XAML binding converters |
| Dialog service | `Services/DialogService.cs` | Unified dialog implementation |
| Navigation service | `Services/NavigationService.cs` | Prism region navigation |
| Theme resources | `Themes/` | Generic.xaml, color brushes |

## CONVENTIONS

- All custom controls inherit from appropriate WPF base (Control, UserControl, ContentControl)
- Converters implement `IValueConverter` or `IMultiValueConverter`
- Services registered via DI extensions in `Shell/Extensions/`
- Theme resources use `{DynamicResource}` for runtime switching

## ANTI-PATTERNS

- **Code-behind in Views** — Business logic belongs in ViewModel, not XAML.cs
- **Hardcoded colors** — Use theme resources from `Themes/`
- **Direct control instantiation** — Use DI or Prism navigation
