# LYBT.Shared.ExceptionHandling - Unified Exception Handling

**Purpose**: Centralized exception handlers for both Desktop and Server.

## Structure

```
LYBT.Shared.ExceptionHandling/
├── Handlers/
│   ├── Desktop/
│   │   └── DesktopExceptionHandler.cs    # WPF exception handling
│   └── Server/
│       ├── BusinessExceptionHandler.cs   # Domain/business exceptions
│       └── SystemExceptionHandler.cs     # System-level exceptions
└── ErrorCodes/                           # Shared error code definitions
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Desktop handler | `Handlers/Desktop/DesktopExceptionHandler.cs` | WPF UI error display |
| Business handler | `Handlers/Server/BusinessExceptionHandler.cs` | Domain error responses |
| System handler | `Handlers/Server/SystemExceptionHandler.cs` | System error logging |
| Error codes | `ErrorCodes/` | Shared error code definitions |

## CONVENTIONS

- **Platform-specific handlers** — Desktop vs Server have separate exception handlers
- **Business vs System** — Server distinguishes business logic errors from system errors
- **Error codes** — Centralized error code definitions in `LYBT.Shared.Primitives`

## ANTI-PATTERNS

- **Empty catch blocks** — Never swallow exceptions silently
- **Generic exception messages** — Use structured error codes and messages
