# LYBT.Desktop.Foundation - Desktop Foundation Layer

**Purpose**: HTTP client, security, configuration for Desktop client.

## Structure

```
LYBT.Desktop.Foundation/
├── Security/            # 24 security-related files (JWT, tokens, encryption)
├── Http/                # HTTP client setup, RetryPolicyExtensions
├── Configuration/       # App configuration
└── Services/            # Foundation services
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Security | `Security/` | JWT handling, token storage, encryption |
| HTTP client | `Http/` | Refit client setup, retry policies |
| Retry policies | `Http/RetryPolicyExtensions.cs` | HTTP retry configuration |

## CONVENTIONS

- **Refit HTTP** — All API calls through Refit interfaces
- **Token management** — JWT + RefreshToken + AutoLoginToken lifecycle

## ANTI-PATTERNS

- **Direct HttpClient** — Use Refit interfaces from Contracts layer
- **Hardcoded URLs** — Use configuration for API endpoints
