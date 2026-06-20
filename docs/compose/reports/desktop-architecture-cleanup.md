---
feature: Desktop Infrastructure Architecture Cleanup
status: delivered
specs:
  - docs/compose/specs/2026-06-20-desktop-architecture-cleanup-design.md
plans:
  - docs/compose/plans/2026-06-20-desktop-architecture-cleanup.md
branch: master
commits: a5f30bac8..2d9014b06
---

# Desktop Infrastructure Architecture Cleanup — Final Report

## What Was Built

Restructured the WPF Desktop client's Core project layout to reduce Infrastructure from 18,663 lines (48% of Core) to 9,436 lines (49% reduction). Created two new projects (`LYBT.Desktop.Controls` for WPF presentation assets, `LYBT.Desktop.Shared` for non-interface shared types), merged `LYBT.Desktop.Models` into `Infrastructure` and `LYBT.Desktop.Utilities` into `Foundation`, and extracted `CommandResult` + `BreadcrumbItem` from `Contracts` into `Shared`. The dependency graph remains a clean DAG with no cycles.

## Architecture

### New Project Layout

```
Contracts (4.3K) ← Foundation (7.9K) ← Infrastructure (9.4K) ← Controls (8.7K)
                         ↑                                         ↑
                      Shared (0.3K) ←─────────────────────────────┘
```

| Project | LOC | Role | Changed? |
|---------|-----|------|----------|
| Contracts | 4,316 | Interfaces + Shared DTOs (via dependency) | +Shared ref, -CommandResult, -BreadcrumbItem |
| Foundation | ~7,900 | HTTP, security, config, ExcelHelper | +NPOI, +ExcelHelper |
| Infrastructure | 9,436 | ViewModel bases, navigation, services, behaviors | -Controls, -Themes, -Converters, +ViewModel bases |
| Controls | ~8,700 | WPF controls, themes, converters, helpers | NEW |
| Shared | ~300 | CommandResult, BreadcrumbItem | NEW |
| Models | DELETED | — | Merged into Infrastructure |
| Utilities | DELETED | — | Merged into Foundation |

### Dependency Rules

- `Controls → Contracts + Shared + Foundation` (no Infrastructure dependency — prevents cycles)
- `Infrastructure → Controls` (for ToastControl, BaseDetailContainer in ViewModels)
- `Modules → Infrastructure + Controls` (ViewModel bases + WPF controls)
- `Contracts → Shared` (backward-compatible transitive dependency)

### What Controls Contains

| Directory | Contents | Moved From |
|-----------|----------|------------|
| Controls/ | 29 .cs + 21 .xaml (MasterDetailControlBase, BreadcrumbBar, Sidebar, etc.) | Infrastructure/Controls/ |
| Converters/ | 17 .cs + 1 .xaml (all IValueConverter implementations) | Infrastructure/Converters/ |
| Themes/ | 15 .xaml (DesignSystem, PreviewStyles, PanelStyles, etc.) | Infrastructure/Themes/ |
| Navigation/ | 3 .xaml + code-behind (BreadcrumbControl, NavigationHistoryPanel, NavigationSuggestionsPanel) | Infrastructure/Navigation/Controls/ |
| Helpers/ | ResponsiveLayoutHelper, BindingProxy | Infrastructure/Helpers/ |
| Models/ | DuplicateDosageStrategy, SuggestionType | Infrastructure/Models/ |

### Design Decisions

- **Controls does NOT reference Infrastructure.** The original plan had a temporary Controls→Infrastructure reference, but this created a cycle risk. Instead, 3 types were moved from Infrastructure to Controls (ResponsiveLayoutHelper, BindingProxy, DuplicateDosageStrategy, SuggestionType) and `SystemConstants.ApplicationVersion` was inlined. This preserves the clean DAG.
- **ViewModels stay in Infrastructure.** Navigation ViewModels (BreadcrumbControlViewModel, etc.) reference Infrastructure types (INavigationCoordinator, IEnhancedNavigationService). Moving them to Controls would require Infrastructure interfaces — impractical. Only XAML views moved to Controls.
- **Shared targets `net8.0` not `net8.0-windows`.** CommandResult and BreadcrumbItem have no WPF dependency. Targeting `net8.0` makes Shared compatible with Contracts (which also targets `net8.0`).
- **Contracts→Shared backward compatibility.** Contracts references Shared so existing code that uses `CommandResult` via Contracts namespace continues to work transitively. New code should use `LYBT.Desktop.Shared.Results` directly.

## Verification

| Criterion | Result |
|-----------|--------|
| `dotnet build LYBTZYZS.sln` | 0 errors, 48 warnings |
| Architecture tests | Pass (2 intentionally skipped) |
| Infrastructure LOC | 9,436 (< 10,000 target) |
| Controls independent build | 0 errors |
| Models directory exists | False |
| Utilities directory exists | False |
| No circular dependencies | Confirmed (DAG verified by CodeGraph) |

Desktop tests: 673 pass / 175 fail — pre-existing failures (NSubstitute proxy issues, WPF STA threading, controller integration tests needing DB). Baseline-controlled: same failures at clean HEAD.

## Journey Log

- [pivot] Original plan had Controls→Infrastructure temporary reference. CodeGraph analysis revealed this would create a cycle. Pivoted to moving types (ResponsiveLayoutHelper, BindingProxy, SuggestionType, DuplicateDosageStrategy) from Infrastructure to Controls instead.
- [lesson] XAML `clr-namespace:...;assembly=` references must match the actual assembly name. Batch-replacing `assembly=LYBT.Desktop.Infrastructure` to `assembly=LYBT.Desktop.Controls` broke Behaviors references (still in Infrastructure). Required selective revert.
- [lesson] Namespace collision: introducing `LYBT.Desktop.Shared` caused `Shared.Models.Enums.Gender` to resolve to `LYBT.Desktop.Shared.Models.Enums` instead of `LYBT.Shared.Models.Enums`. Fixed by adding explicit `using LYBT.Shared.Models.Enums` and removing the shorthand.
- [dead end] Tried to keep Controls→Infrastructure for Constants/Helpers/Models types. Reality: 3 types were enough to move (ResponsiveLayoutHelper, BindingProxy, SuggestionType, DuplicateDosageStrategy) + 1 inline (SystemConstants.ApplicationVersion). Clean separation achieved.
- [lesson] CommunityToolkit.Mvvm source generators cache the namespace. After moving BreadcrumbItem, the generated `MainWindowViewModel.g.cs` still referenced `LYBT.Desktop.Contracts.Models.BreadcrumbItem`. Required obj/ clean + explicit fully-qualified reference update in source file.

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/specs/2026-06-20-desktop-architecture-cleanup-design.md` | Design spec | §S1-S10 covering problem, architecture, migration plan |
| `docs/compose/plans/2026-06-20-desktop-architecture-cleanup.md` | Implementation plan | 13 tasks, reviewed and fixed (circular dependency, grep patterns) |
| `docs/compose/plans/2026-06-19-wpf-architecture-audit.md` | Pre-cleanup audit | Archived — drove the decision to do Option A (Infrastructure split) |
