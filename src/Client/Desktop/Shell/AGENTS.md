# LYBT.Desktop.Shell - Desktop Application Entry Point

**Purpose**: PrismApplication entry point, bootstrapper, startup pipeline, main window.

## Structure

```
Shell/
├── App.xaml.cs              # PrismApplication, OnStartup pipeline
├── Views/
│   ├── MainWindow.xaml      # Shell main window with regions
│   └── SplashScreenWindow   # Startup splash screen
├── Services/
│   └── Startup/Steps/       # StartupPipeline steps
└── Extensions/              # DI registration extensions
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| App bootstrap | `App.xaml.cs` | Single-instance, startup pipeline, role-based module loading |
| Main window | `Views/MainWindow.xaml` | Prism regions, navigation targets |
| Startup steps | `Services/Startup/Steps/` | ErrorHandling → ModuleCoordinator → CoreServices → ApiHealthCheck → Warmup |
| DI wiring | `Extensions/` | Logging, Prism, HTTP, DataSource registration |

## CONVENTIONS

- **StartupPipeline** — Step-based startup pattern, each step implements `IStartupStep`
- **Role-based modules** — `ApplicationBootstrapper.LoadModulesForRoleAsync()` loads modules per user role
- **Explicit ModuleCatalog** — No DirectoryModuleCatalog; modules registered manually with `InitializationMode.WhenAvailable`
- **Two-phase Serilog** — Bootstrap logger → final logger

## ANTI-PATTERNS

- **ContainerLocator** — Service locator anti-pattern (documented in Desktop README)
- **Blocking startup** — All startup steps must be async; splash screen shows progress
- **Direct module references** — Modules MUST NOT reference each other
