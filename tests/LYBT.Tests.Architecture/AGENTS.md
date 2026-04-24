# LYBT.Tests.Architecture - Architecture Guard Tests

**Purpose**: Architecture constraint enforcement tests.

## Structure

```
LYBT.Tests.Architecture/
├── ServerArchTests.cs       # Server architecture constraints
├── ClientArchTests.cs       # Client architecture constraints
├── AntiMockRuleTests.cs     # Forbids mocking in server tests
└── SharedArchTests.cs       # Shared library constraints
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Server architecture | `ServerArchTests.cs` | `P10_Services_Should_Not_Directly_Inject_AppDbContext` |
| Anti-mock rules | `AntiMockRuleTests.cs` | Forbids NSubstitute/Moq in server tests |
| Client architecture | `ClientArchTests.cs` | Desktop module isolation rules |

## CONVENTIONS

- **NetArchTest** — Architecture testing framework
- **P10 rule** — Services MUST NOT directly inject AppDbContext
- **AntiMockRule** — Server tests MUST NOT use mocking frameworks
- **Module isolation** — Server/Desktop modules MUST NOT reference each other

## ANTI-PATTERNS

- **Disabling architecture tests** — These are guardrails, not optional
- **Ignoring test failures** — Architecture violations must be fixed, not suppressed
