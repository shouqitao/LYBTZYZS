<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Shared

## Purpose
Cross-tier shared libraries used by both the Server and Desktop client. Contains DTOs/contracts, FluentValidation rules, shared UI components, configuration models, base types/primitives, unified exception handling, logging abstraction, and utility classes. These libraries are the ONLY permitted cross-tier dependency.

## Key Files
| File | Description |
|------|-------------|
| GlobalUsings.cs | Global using directives for shared projects |
| README.md | Shared libraries overview |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Shared.Models/ | DTOs, contracts, and request/response models — the primary cross-tier data contract |
| LYBT.Shared.Validators/ | FluentValidation rules shared between Server and Client |
| LYBT.Shared.Components/ | Shared UI components (used by Desktop client) |
| LYBT.Shared.Configuration/ | Shared configuration models and options |
| LYBT.Shared.Primitives/ | Base types, constants, enums used across all tiers |
| LYBT.Shared.ExceptionHandling/ | Unified exception types and error handling |
| LYBT.Shared.Logging/ | Logging abstraction layer |
| LYBT.Shared.Utilities/ | Utility classes and extension methods |

## For AI Agents

### Working In This Directory
- `LYBT.Shared.Models` is the most critical project — it defines all DTOs that both Server and Client depend on.
- When adding a new DTO, place it in `Shared.Models` with appropriate namespace grouping.
- Validators in `Shared.Validators` use FluentValidation and are shared between Server-side validation and Client-side pre-validation.
- Keep these libraries dependency-free from Server or Client specific concerns.

### Common Patterns
- **DTO pattern**: Plain data classes with properties, no behavior
- **FluentValidation**: `AbstractValidator<T>` subclasses for shared validation rules
- **Primitives**: Base classes, constants, and enums that all tiers need

## Dependencies

### Internal
- None — these are leaf dependencies with no internal project references

### External
- FluentValidation
- System.Text.Json / Newtonsoft.Json

<!-- MANUAL: -->
