# LYBT.Shared.ExceptionHandling - Shared Exception Types

**Purpose**: Shared exception base classes and domain exceptions. Platform-specific handlers live in their respective tiers.

## Structure

```
LYBT.Shared.ExceptionHandling/
└── Exceptions/
    ├── Base/           # AppException (base class)
    ├── Business/       # BusinessException, NotFoundException, ConflictException, ValidationException
    ├── External/       # ApiException
    ├── Factory/        # ExceptionFactory (convenience constructors)
    └── Security/       # UnauthorizedException
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Base exception | `Exceptions/Base/AppException.cs` | All custom exceptions inherit from this |
| Business exceptions | `Exceptions/Business/` | Domain-specific exceptions |
| Exception factory | `Exceptions/Factory/ExceptionFactory.cs` | Convenience constructors |
| Server handlers | `LYBT.Infrastructure.ExceptionHandling` | BusinessExceptionHandler, SystemExceptionHandler |
| Desktop handler | `LYBT.Desktop.Infrastructure.ExceptionHandling` | DesktopExceptionHandler, IDesktopExceptionHandler |
| Client message mapper | `LYBT.Desktop.Foundation.ExceptionHandling` | ClientErrorMessageMapper |

## CONVENTIONS

- **No platform dependencies** — Shared exceptions must not reference ASP.NET Core or WPF
- **ErrorCode enum** — Use `LYBT.Shared.Models.Primitives.ErrorCodes.ErrorCode` for typed error codes
- **UserMessage** — AppException.UserMessage for user-facing messages

## ANTI-PATTERNS

- **Empty catch blocks** — Never swallow exceptions silently
- **Generic exception messages** — Use structured error codes and messages
