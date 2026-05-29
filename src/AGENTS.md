<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# src

## Purpose
Top-level container for all application source code. Organizes the LYBTZYZS solution into three tiers: Server (ASP.NET Core WebAPI), Client (WPF Desktop), and Shared (cross-tier libraries). Follows strict unidirectional dependency rules: Server and Client may reference Shared, but never each other.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| [Server/](Server/AGENTS.md) | ASP.NET Core WebAPI backend — entities, infrastructure, business modules, and API entry point |
| [Client/](Client/AGENTS.md) | WPF/Prism.DryIoc desktop client — core libraries, business modules, role workspaces, and shell |
| [Shared/](Shared/AGENTS.md) | Cross-tier shared libraries — DTOs, validators, configuration, exception handling, utilities |
| Tools/ | Build and development tooling (not part of the main solution) |

## For AI Agents

### Working In This Directory
- This is a structural container only; no source files live here directly.
- Navigate into `Server/`, `Client/`, or `Shared/` for actual code.
- Cross-tier dependency rule: Server and Client reference `Shared.Models` for DTOs but NEVER reference each other.

### Testing Requirements
- Server tests: `dotnet test tests/LYBT.Tests.Server/`
- Desktop tests: `dotnet test tests/LYBT.Tests.Desktop/`
- Architecture tests: `dotnet test tests/LYBT.Tests.Architecture/`

## Dependencies

### Internal
- References `LYBTZYZS.sln` at repository root

### External
- .NET 8 SDK

<!-- MANUAL: -->
