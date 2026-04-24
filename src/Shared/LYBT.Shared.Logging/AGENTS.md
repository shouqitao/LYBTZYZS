# LYBT.Shared.Logging - Shared Logging Configuration

**Purpose**: Serilog configuration and logging abstractions shared across Client/Server.

## Structure

```
LYBT.Shared.Logging/
├── Extensions/
│   ├── LoggerConfigurationExtensions.cs   # Serilog config helpers
│   └── ServiceCollectionExtensions.cs     # DI wiring for logging
└── LoggingLevelManager.cs                 # Runtime log level control
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Serilog config | `Extensions/LoggerConfigurationExtensions.cs` | Serilog sink configuration |
| DI wiring | `Extensions/ServiceCollectionExtensions.cs` | Logging service registration |
| Log level manager | `LoggingLevelManager.cs` | Runtime log level adjustment |

## CONVENTIONS

- **Serilog** — Primary logging framework
- **Two-phase bootstrap** — Bootstrap logger → final logger (both WebAPI and Desktop)
- **Sinks** — Console + File + SQL Server (configurable per environment)

## ANTI-PATTERNS

- **Direct Console.WriteLine** — Use ILogger<T> or Serilog
- **Hardcoded log levels** — Use LoggingLevelManager for runtime control
