# LYBT.Shared.Validators - Shared Validators

**Purpose**: FluentValidation validators organized by business module, shared between Server and Desktop.

## Structure

```
LYBT.Shared.Validators/
├── Auth/                # Login validators
├── BusinessRules/       # Cross-cutting business rule validators
├── Consultation/        # Consultation input validators
├── Formula/             # Formula input validators
├── Herbs/               # Herb input validators
├── MedicalCase/         # MedicalCase input validators
├── Patients/            # Patient input validators
├── Prescriptions/       # Prescription input validators
└── Users/               # User input validators
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Auth validation | `Auth/` | LoginRequest, Token validators |
| Business rules | `BusinessRules/` | Cross-module validation rules |
| Module validators | `{Module}/` | FluentValidation for InputDto |

## CONVENTIONS

- **FluentValidation** — All validators use FluentValidation syntax
- **Module organization** — Validators grouped by business module (mirrors Contracts)
- **Shared** — Referenced by both Server (Service layer) and Desktop (ViewModel layer)
- **Assembly scanning** — Server registers all validators via assembly scanning

## ANTI-PATTERNS

- **Validation in Controller** — Validators are for Service layer, not Controller input
- **Missing async validators** — Use RuleFor().MustAsync() for DB uniqueness checks
- **Over-validation** — Don't validate presentation concerns; validate business rules only
