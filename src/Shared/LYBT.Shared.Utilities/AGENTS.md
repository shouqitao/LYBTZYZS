<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Shared.Utilities

## Purpose

Shared utility classes used by both Desktop client and WebAPI server. Provides password hashing and policy validation (BCrypt), Chinese Pinyin conversion for search/filter functionality, DI extension methods, and general-purpose helpers. Referenced by both server modules and desktop infrastructure.

## Key Files

| File | Description |
|------|-------------|
| `LYBT.Shared.Utilities.csproj` | Project file; net8.0; BCrypt.Net-Next, pinyin4net, MS Extensions packages |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Security/` | `PasswordHelper.cs` (22KB) -- BCrypt hashing, password policy validation, complexity checks; `IPasswordService.cs` + `BcryptPasswordService.cs` -- DI-friendly password service; `PasswordPolicyValidator.cs` (10KB) -- configurable password policy rules |
| `Text/` | `PinYinHelper.cs` (3KB) -- Chinese character to Pinyin conversion for search filtering |
| `Extensions/ServiceCollection/` | DI registration extension methods |

## For AI Agents

### Working In This Directory

- `PasswordHelper` is a large static utility class (22KB). When modifying, ensure backward compatibility with both server (direct static calls) and desktop (via `IPasswordService`).
- `IPasswordService` / `BcryptPasswordService` wraps `PasswordHelper` for DI injection. Prefer the interface in new code.
- `PinYinHelper` converts Chinese characters to Pinyin initials for herb/patient name filtering in search UIs.
- `PasswordPolicyValidator` supports configurable rules: min length, uppercase, lowercase, digits, special chars, common password check.

### Testing Requirements

- Unit tests: `tests/LYBT.Tests.Server.Unit/Utilities/PasswordHelperTests.cs` (27KB comprehensive tests)
- Also tested indirectly through auth tests in Server and Desktop test projects.
- Run: `dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~PasswordHelper"`

### Common Patterns

- **Static utility + DI wrapper** -- `PasswordHelper` (static) + `IPasswordService`/`BcryptPasswordService` (DI) dual pattern.
- **BCrypt work factor 12** -- Password hashing uses BCrypt with cost factor 12.
- **Pinyin initials** -- `PinYinHelper.GetPinyinInitials()` returns first letter of each character's Pinyin for search.

## Dependencies

### Internal

- `LYBT.Shared.Models` -- DTOs and contracts (referenced for password-related models)

### External

- `BCrypt.Net-Next` -- Password hashing
- `hyjiacan.pinyin4net` -- Chinese Pinyin conversion
- `Microsoft.Extensions.Configuration.Abstractions` -- IConfiguration access
- `Microsoft.Extensions.Configuration.Binder` -- Configuration binding
- `Microsoft.Extensions.DependencyInjection.Abstractions` -- DI extensions
- `Microsoft.Extensions.Hosting.Abstractions` -- IHost extensions
- `Microsoft.Extensions.Caching.Memory` -- In-memory caching
- `Microsoft.Extensions.Logging.Abstractions` -- Logging
- `System.Text.Json` -- JSON serialization
- `System.ComponentModel.Annotations` -- Data annotations

<!-- MANUAL: -->
