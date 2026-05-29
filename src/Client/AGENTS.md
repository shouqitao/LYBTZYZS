<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Client

## Purpose
Container for the WPF desktop client application. Currently contains only the Desktop project tree, which implements the full Prism.DryIoc MVVM architecture for the TCM clinic management system. The client supports dual-mode operation (remote HTTP API or local embedded SQLite) and references `Shared.Models` for DTOs.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| [Desktop/](Desktop/AGENTS.md) | WPF Desktop application — core libraries, business modules, role workspaces, shell entry point, and XAML resources |

## For AI Agents

### Working In This Directory
- This is a structural container; all code lives under `Desktop/`.
- The client targets `net8.0-windows` and requires Windows to build and test.
- Cross-tier rule: Client references `Shared.Models` for DTOs but MUST NOT reference Server projects.

### Testing Requirements
- Desktop tests: `dotnet test tests/LYBT.Tests.Desktop/` (~760 tests, SQLite InMemory + real Repository)

## Dependencies

### Internal
- [Shared/](../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Validators`, `LYBT.Shared.Components`

### External
- .NET 8 SDK (Windows)
- Prism.DryIoc
- WPF

<!-- MANUAL: -->
