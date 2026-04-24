# LYBT.Shared.Models - Shared DTOs and Contracts

**Purpose**: DTOs, contracts, enums shared between Client and Server.

## Structure

```
LYBT.Shared.Models/
├── Contracts/
│   ├── Common/          # 16 common DTOs
│   ├── MedicalCase/     # 13 medical case DTOs
│   ├── Formula/         # 13 formula DTOs
│   ├── Auth/            # 11 auth DTOs
│   ├── Patients/        # 11 patient DTOs
│   ├── Sync/            # 10 sync DTOs
│   └── Herbs/           # Herb DTOs
├── Enums/               # 13 shared enumerations
└── Extensions/          # DtoConversionExtensions, EnumExtensions
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Common DTOs | `Contracts/Common/` | Result, ApiResponse, ValidationResult |
| MedicalCase DTOs | `Contracts/MedicalCase/` | Consultation, Prescription, MedicalCase DTOs |
| Auth DTOs | `Contracts/Auth/` | Login, Token, User DTOs |
| Enums | `Enums/` | Shared enumeration types |
| DTO conversions | `Extensions/DtoConversionExtensions.cs` | Entity ↔ DTO mapping helpers |

## CONVENTIONS

- **No business logic** — Pure data transfer objects only
- **Nullable reference types** — All DTOs use nullable annotations
- **Record types preferred** — Immutable DTOs as C# records when possible

## ANTI-PATTERNS

- **Entity types here** — Entities belong in `LYBT.Entities`
- **Business methods on DTOs** — DTOs are data-only
