<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Shared.Primitives

## Purpose

The lowest-level shared library in the LYBT ecosystem. Defines foundational error codes, error categories, error messages, and validation constants used across the entire solution. Zero internal dependencies -- referenced by `LYBT.Shared.Models` and `LYBT.Shared.ExceptionHandling` as the base layer for the unified error handling system (consolidate-exception-handling).

## Key Files

| File | Description |
|------|-------------|
| `LYBT.Shared.Primitives.csproj` | Project file; net8.0; only dependency is System.ComponentModel.Annotations |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ErrorCodes/` | `ErrorCode.cs` (21KB enum), `ErrorCategory.cs` (enum), `ErrorCodeExtensions.cs` (21KB), `ErrorMessages.cs` (21KB) -- centralized error code definitions |
| `Validation/` | `ValidationConstants.cs` (7KB) -- unified validation rule constants (max lengths, regex patterns, ranges) |

## For AI Agents

### Working In This Directory

- This is the foundation layer with ZERO internal dependencies. Do NOT add references to other LYBT projects.
- `ErrorCode` is a large enum (~21KB) covering all error codes across the system. When adding new errors, follow the existing category grouping.
- `ErrorCategory` enum groups errors: General, Validation, Authentication, Authorization, Resource, Business, Concurrency, System.
- `ValidationConstants` centralizes all field length limits, regex patterns, and numeric ranges. Use these constants instead of hardcoding values.
- `ErrorMessages` maps ErrorCode values to human-readable Chinese error messages.

### Testing Requirements

- No dedicated test project. Error codes and constants are tested indirectly through service and controller tests.
- To verify no build breakage: `dotnet build src/Shared/LYBT.Shared.Primitives/`

### Common Patterns

- **Enum-based error codes** -- `ErrorCode` enum with `ErrorCategory` grouping and `ErrorMessages` lookup.
- **ErrorCodeExtensions** -- Provides conversion methods between ErrorCode and HTTP status codes.
- **Centralized constants** -- `ValidationConstants` prevents scattered magic numbers across the codebase.
- **Description attributes** -- `ErrorCategory` uses `[Description]` for display purposes.

## Dependencies

### Internal

- *(none)* -- This is the lowest-level project with zero internal dependencies

### External

- `System.ComponentModel.Annotations` -- `[Description]` attributes and validation annotations

<!-- MANUAL: -->
