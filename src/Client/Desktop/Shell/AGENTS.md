# LYBT.Desktop.Shell - Desktop Application Entry Point

**Purpose**: PrismApplication entry point, bootstrapper, startup pipeline, main window.

## Structure

```
Shell/
├── App.xaml.cs              # PrismApplication, OnStartup pipeline
├── Views/
│   ├── MainWindow.xaml      # Shell main window with regions
│   ├── SplashScreenWindow   # Startup splash screen
│   └── AccountSettingsView  # Account settings dialog
├── ViewModels/              # MainWindowViewModel, account settings VMs
├── Services/
│   └── Startup/Steps/       # StartupPipeline steps
├── Controls/                # Shell-level custom controls
├── Dialogs/                 # Dialog windows
├── Models/                  # Shell-level models
├── Resources/               # XAML resources
├── Styles/                  # Style dictionaries
├── Assets/                  # Images, icons
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

- **StartupPipeline** — Step-based startup pattern, each step implements `IStartupStep`; all steps registered as `IStartupStep` and collected via `IEnumerable<IStartupStep>` in `AppStartupOrchestrator`（无 IContainerProvider）
- **Service 层无 MessageBox** — 启动管线/步骤通过 `IUserNotificationService` 通知用户，不直接调用 `System.Windows.MessageBox`
- **Role-based modules** — `ApplicationBootstrapper.LoadModulesForRoleAsync()` loads modules per user role
- **Explicit ModuleCatalog** — No DirectoryModuleCatalog; modules registered manually in `App.ConfigureModuleCatalog` (Authentication/Admin/Sysadmin `WhenAvailable`; Clinical and business modules `OnDemand`)
- **Two-phase Serilog** — Bootstrap logger → final logger

## ANTI-PATTERNS

- **ContainerLocator** — Service locator anti-pattern (documented in Desktop README)
- **IContainerProvider in services** — 注入具体依赖或 `IEnumerable<T>`，禁止服务内手动 Resolve
- **Blocking startup** — All startup steps must be async; splash screen shows progress
- **Direct module references** — Modules MUST NOT reference each other
