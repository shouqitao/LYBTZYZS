# LYBT.Desktop.Contracts - Desktop Interface Definitions

**Purpose**: Refit IApi interfaces, IRepository contracts, IService definitions for Desktop client.

## Structure

```
LYBT.Desktop.Contracts/
├── Services/          # 31 service interface definitions
├── Api/               # 15 entity-specific Refit interfaces (IAuthApi, IPatientApi, etc.)
├── ApiClient/         # IApiClient.cs and HTTP client infrastructure
├── Repositories/      # IRepository<T> contracts
├── CommandHandlers/   # Command handler interfaces
├── Events/            # Event definitions
├── Initialization/    # Initialization interfaces
├── Models/            # Contract models
├── Performance/       # Performance monitoring interfaces
├── Roles/             # Role definitions
└── Security/          # Security contracts
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| API interfaces | `ApiClient/IApiClient.cs` | Unified HTTP API client |
| Entity APIs | `Api/` | 15 Refit interfaces (IAuthApi, IPatientApi, etc.) |
| Service contracts | `Services/` | 31 service interface definitions |
| Repository contracts | `Repositories/` | Generic repository interfaces |

## CONVENTIONS

- **Refit for HTTP** — API interfaces use Refit attributes (`[Get]`, `[Post]`, etc.)
- **Interface segregation** — Fine-grained interfaces (ISP principle)
- **Shared DTOs** — Request/response types from `Shared.Models`

## ANTI-PATTERNS

- **Concrete implementations here** — This layer is interfaces only
- **Direct HTTP calls** — Use Refit interfaces, not HttpClient directly
