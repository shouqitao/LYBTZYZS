<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Resources (Desktop)

## Purpose
XAML resource dictionaries and string resources for the WPF desktop application. Contains shared styles, themes, control templates, and localized string tables used across all modules and the shell.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| Dictionaries/ | XAML resource dictionaries — styles, colors, control templates, themes |
| Strings/ | Localized string resource files (.resx) — UI text, labels, messages |

## For AI Agents

### Working In This Directory
- XAML resource dictionaries are merged at the `App.xaml` level or via `MergedDictionaries` in module views.
- When adding a new shared style or template, place it in `Dictionaries/`.
- String resources follow standard .resx localization conventions.

### Common Patterns
- Resource dictionaries use `ResourceDictionary` with `x:Key` for individual resources
- String resources accessed via `{x:Static}` bindings or code-behind resource lookups

## Dependencies

### Internal
- Referenced by Shell and all Desktop Modules via XAML `MergedDictionaries`

<!-- MANUAL: -->
