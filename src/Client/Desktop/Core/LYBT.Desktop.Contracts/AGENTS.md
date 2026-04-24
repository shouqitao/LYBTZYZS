# LYBT.Desktop.Contracts - Desktop Interface Definitions

**Purpose**: Refit IApi interfaces, IRepository contracts, IService definitions for Desktop client.

## Structure

```
LYBT.Desktop.Contracts/
├── Services/          # 31 service interface definitions
├── Api/               # Refit IApiClient interfaces
└── Repositories/      # IRepository<T> contracts
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| API interfaces | `Api/IApiClient.cs` | Refit HTTP API contracts |
| Service contracts | `Services/` | 31 service interface definitions |
| Repository contracts | `Repositories/` | Generic repository interfaces |

## CONVENTIONS

- **Refit for HTTP** — API interfaces use Refit attributes (`[Get]`, `[Post]`, etc.)
- **Interface segregation** — Fine-grained interfaces (ISP principle)
- **Shared DTOs** — Request/response types from `Shared.Models`

## ANTI-PATTERNS

- **Concrete implementations here** — This layer is interfaces only
- **Direct HTTP calls** — Use Refit interfaces, not HttpClient directly
