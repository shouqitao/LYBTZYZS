<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Shared.Components

## Purpose

Shared UI component contracts and validation logic for herb-related views across the Desktop client. Defines the `IHerbItem` and `IHerbItemEditable` interfaces that prescription and formula herb list items implement, plus a generic `HerbValidatorBase<T>` base class that extracts shared herb validation logic (duplicate detection, dosage range, required fields) used by both Prescription and Formula modules.

## Key Files

| File | Description |
|------|-------------|
| `IHerbItem.cs` | Base herb item interface (HerbId, HerbName, Unit, Dosage, UnitPrice) |
| `IHerbItemEditable.cs` | Extended interface adding herb selection/filtering (AllHerbs, FilteredHerbs, SelectedHerb) |
| `HerbValidatorBase.cs` | Generic abstract base class for herb list validation (duplicates, dosage, required fields); includes `ValidationResult` class |
| `LYBT.Shared.Components.csproj` | Project file; net8.0; references LYBT.Shared.Models |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| *(none)* | All files are in the root directory |

## For AI Agents

### Working In This Directory

- This project contains only interfaces and abstract base classes -- no concrete implementations.
- `IHerbItem` is the minimal contract; `IHerbItemEditable` extends it with herb selection UI support.
- `HerbValidatorBase<TItem>` is used by both Prescription and Formula validation to avoid code duplication (Issue #1153).
- The `ValidationResult` class defined in `HerbValidatorBase.cs` is a simple error/warning accumulator, distinct from FluentValidation's `ValidationResult`.

### Testing Requirements

- No dedicated test project. Validation logic is tested indirectly through Prescription and Formula module tests.
- To test changes, run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~Formula"` or `--filter "FullyQualifiedName~Prescription"`

### Common Patterns

- **Generic constraint** -- `HerbValidatorBase<TItem> where TItem : IHerbItem` enables reuse across different herb item types.
- **Interface segregation** -- `IHerbItem` for read-only scenarios, `IHerbItemEditable` for editable UI scenarios.
- **HerbListDto** -- `IHerbItemEditable` uses `LYBT.Shared.Models.Contracts.Herbs.HerbListDto` for the herb selection dropdown.

## Dependencies

### Internal

- `LYBT.Shared.Models` -- HerbListDto (used by IHerbItemEditable)

### External

- *(none)* -- No external NuGet packages

<!-- MANUAL: -->
