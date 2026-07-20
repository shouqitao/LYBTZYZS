# LYBT.Desktop.Infrastructure - WPF Infrastructure Layer

**Purpose**: WPF services, controls, converters, themes, and infrastructure utilities for the Desktop client.

## Structure

```
LYBT.Desktop.Infrastructure/
├── Controls/          # ~20 custom WPF controls (42 files incl. code-behind pairs)
├── Converters/        # 22 IValueConverter implementations
├── Services/          # 23 WPF service implementations (+ Interfaces/, Notifications/, Toast/)
├── Themes/            # 15 theme resource dictionaries
├── ViewModels/        # MasterDetailViewModelBase, NavigableViewModelBase and derivatives
├── Views/             # Infrastructure-level views
├── Navigation/        # NavigationCoordinator, region management
├── Security/          # Security-related WPF services
├── Repositories/      # WPF-side repository implementations
├── Http/              # HTTP infrastructure services
├── DependencyInjection/ # DI registration extensions
├── Bootstrapping/     # Application bootstrap helpers
├── Commands/          # Custom WPF commands
├── Constants/         # Shared constants
├── Events/            # Event definitions
├── Helpers/           # Utility helpers
├── Interfaces/        # Infrastructure-level interfaces
├── Logging/           # Logging integration
├── Models/            # Infrastructure-level models
├── Performance/       # Performance monitoring
├── Roles/             # Role-related infrastructure
├── Windows/           # Custom window implementations
├── Extensions/        # Task, Configuration extension methods
├── Configuration/     # Configuration helpers
└── Behaviors/         # Attached behaviors
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Custom controls | `Controls/` | HerbSelector, PrescriptionGrid, custom TextBoxes |
| Value converters | `Converters/` | XAML binding converters |
| Dialog service | `Services/DialogService.cs` | Unified dialog implementation |
| Navigation service | `Services/NavigationService.cs` | Prism region navigation |
| Theme resources | `Themes/` | Generic.xaml, color brushes |

## CONVENTIONS

- All custom controls inherit from appropriate WPF base (Control, UserControl, ContentControl)
- Converters implement `IValueConverter` or `IMultiValueConverter`
- Services registered via DI extensions in `Shell/Extensions/`
- Theme resources use `{DynamicResource}` for runtime switching

## ANTI-PATTERNS

- **Code-behind in Views** — Business logic belongs in ViewModel, not XAML.cs
- **Hardcoded colors** — Use theme resources from `Themes/`
- **Direct control instantiation** — Use DI or Prism navigation

## 职责边界

**Infrastructure = WPF/MVVM 基础设施层**，依赖 WPF + Prism。

| 文件夹 | 职责 |
|--------|------|
| Behaviors/ | XAML 行为 |
| CardReader/ | 读卡器集成 |
| Commands/ | MVVM 命令 |
| Configuration/ | WPF 配置 |
| Constants/ | 常量定义 |
| DependencyInjection/ | DI 注册 |
| Events/ | 事件聚合器 |
| Extensions/ | 扩展方法 |
| Helpers/ | 工具类 |
| Http/ | HTTP 辅助（ApiResponseHelper, LoggingHttpHandler） |
| Interfaces/ | 接口定义 |
| LocalData/ | 本地数据存储 |
| Logging/ | 日志辅助 |
| Models/ | UI 模型 |
| Navigation/ | 导航服务 |
| Performance/ | 性能监控 |
| Repositories/ | UI 仓储 |
| Roles/ | 角色定义 |
| Security/ | 安全辅助（SensitiveInfoFilter） |
| Services/ | UI 服务 |
| ViewModels/ | 共享 ViewModel |
| Views/ | 共享 View |
| Windows/ | 窗口 |

**规则**：Infrastructure 可引用 Foundation，但 Foundation 不得引用 Infrastructure。Infrastructure 引用 Server/LYBT.Entities 是 LocalData 合并的有意设计。
