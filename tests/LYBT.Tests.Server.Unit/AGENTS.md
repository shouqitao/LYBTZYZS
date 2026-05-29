<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Tests.Server.Unit

## Purpose

Server-side unit tests using NSubstitute for mocking. Tests controllers, entities, validators, and utility classes in isolation without database or HTTP dependencies. Complements the integration-first `LYBT.Tests.Server` project by covering pure logic, edge cases, and validation rules that don't need real infrastructure.

## Key Files

| File | Description |
|------|-------------|
| `LYBT.Tests.Server.Unit.csproj` | Project file; net8.0; xUnit, FluentAssertions, NSubstitute, Bogus |
| `Usings.cs` | Global usings: xUnit, FluentAssertions, NSubstitute |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Controllers/` | Controller unit tests: MedicalCasesController (4 test classes, ~65KB), covering audit, print, processing, and CRUD |
| `Entities/` | Entity model tests: MedicalCaseModelTests, FormulaModelTests, PatientModelTests, PrescriptionModelTests |
| `Validators/` | FluentValidation validator tests organized by domain: Auth, Consultation, Formula, Herbs, MedicalCase, Patients, Prescriptions, Users |
| `Utilities/` | Utility class tests: PasswordHelperTests (27KB) |
| `Shared/` | Shared library tests: Auth, Configuration, ExceptionHandling, Logging |

## For AI Agents

### Working In This Directory

- This project USES NSubstitute (unlike `LYBT.Tests.Server` which bans mocks). This is intentional for pure unit tests.
- Global usings are defined in `Usings.cs`: `Xunit`, `FluentAssertions`, `NSubstitute`.
- Tests cover server-side code: controllers, entities, validators, and shared utilities.
- References server projects: WebAPI, MedicalCase module, Entities, Infrastructure, Shared.Models, Shared.Utilities, Shared.Validators, Shared.ExceptionHandling.
- Uses `Bogus` library for generating realistic test data.

### Testing Requirements

- Run all unit tests: `dotnet test tests/LYBT.Tests.Server.Unit/`
- Run specific domain: `dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~MedicalCase"`
- Run validators: `dotnet test tests/LYBT.Tests.Server.Unit/ --filter "FullyQualifiedName~Validator"`
- Run ~fast (no DB): `dotnet test tests/LYBT.Tests.Server.Unit/` (all tests are fast, no infrastructure)

### Common Patterns

- **NSubstitute mocking** -- Controllers are tested with mocked services (ISubstitute for I{Entity}Service).
- **FluentAssertions** -- All assertions use `.Should()` syntax.
- **Bogus data generation** -- Test data created with Bogus Faker for realistic values.
- **Validator testing** -- Each domain has dedicated validator tests covering valid/invalid inputs, edge cases, and error messages.
- **Entity model testing** -- Tests verify domain behavior on MedicalCaseModel (the sole DDD aggregate root) and other entities.

## Dependencies

### Internal

- `LYBT.WebAPI` -- Controllers under test
- `LYBT.Module.MedicalCase` -- MedicalCase module (CQRS command handlers)
- `LYBT.Entities` -- Domain entities
- `LYBT.Infrastructure` -- DbContext and base infrastructure
- `LYBT.Shared.Models` -- DTOs
- `LYBT.Shared.Utilities` -- Utility classes (PasswordHelper)
- `LYBT.Shared.Validators` -- FluentValidation validators
- `LYBT.Shared.ExceptionHandling` -- Exception types

### External

- `Microsoft.NET.Test.Sdk` -- Test SDK
- `xunit` -- Test framework
- `xunit.runner.visualstudio` -- VS test runner
- `FluentAssertions` -- Assertion library
- `NSubstitute` -- Mocking framework (allowed here, banned in LYBT.Tests.Server)
- `Bogus` -- Test data generation

<!-- MANUAL: -->
