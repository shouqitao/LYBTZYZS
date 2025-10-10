# LYBT.Desktop.Presentation

> **版本**: 2.0.0
> **创建日期**: 2025-10-10
> **关联Issue**: [#1114](https://github.com/shouqitao/LYBTZYZS/issues/1114)
> **ADR**: [ADR-005](../../../docs/architecture/adr/ADR-005-desktop-modular-architecture.md)

## 项目概述

Desktop端**UI基础设施层**，负责提供与用户界面相关的技术基础服务，包括导航、通知、主题、用户体验优化和打印功能。

## 职责范围

### ✅ 应包含
- **导航服务**：页面导航、对话框管理、窗口管理
- **通知服务**：Toast通知、消息框、确认对话框
- **主题管理**：亮色/暗色主题切换、动态主题应用
- **用户体验**：加载指示器、进度条、动画效果
- **打印服务**：报表打印、处方打印、自定义打印模板

### ❌ 不应包含
- 业务逻辑（属于各模块的ViewModels）
- 数据访问（属于各模块的Repositories）
- 具体业务页面（属于各模块的Views）
- HTTP通信（属于Foundation层）

## 目录结构

```
LYBT.Desktop.Presentation/
├── Navigation/              # 导航服务
│   ├── INavigationService.cs
│   ├── NavigationService.cs
│   └── DialogService.cs
├── Notifications/           # 通知服务
│   ├── INotificationService.cs
│   ├── NotificationService.cs
│   └── ToastNotificationService.cs
├── Theming/                 # 主题管理
│   ├── IThemeService.cs
│   ├── ThemeService.cs
│   └── ResourceDictionaries/
├── UserExperience/          # 用户体验
│   ├── BusyIndicatorService.cs
│   ├── AnimationService.cs
│   └── FeedbackService.cs
└── Print/                   # 打印服务
    ├── IPrintService.cs
    ├── PrintService.cs
    └── Templates/
```

## 使用示例

### 导航服务

```csharp
public class MainViewModel
{
    private readonly INavigationService _navigation;

    public MainViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    public async Task NavigateToPatientListAsync()
    {
        await _navigation.NavigateAsync("PatientListView");
    }
}
```

### 通知服务

```csharp
public class PatientViewModel
{
    private readonly INotificationService _notification;

    public async Task SavePatientAsync()
    {
        var result = await _patientRepository.CreateAsync(patient);

        if (result.IsSuccess)
        {
            _notification.ShowSuccess("患者信息保存成功");
        }
        else
        {
            _notification.ShowError(result.ErrorMessage);
        }
    }
}
```

### 主题服务

```csharp
public class SettingsViewModel
{
    private readonly IThemeService _theme;

    public void ToggleTheme()
    {
        var currentTheme = _theme.GetCurrentTheme();
        var newTheme = currentTheme == Theme.Light ? Theme.Dark : Theme.Light;
        _theme.SetTheme(newTheme);
    }
}
```

## 依赖关系

### 依赖的项目
- `LYBT.Desktop.Foundation`（技术基础设施）
- `LYBT.Shared.Models`
- `LYBT.Shared.Interfaces`
- `LYBT.Shared.Utilities`

### 依赖的NuGet包
- `Prism.Core`（MVVM框架核心）
- `Prism.Wpf`（WPF集成）
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.DependencyInjection`

## 服务注册

在`App.xaml.cs`中注册Presentation层服务：

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // Foundation层
    containerRegistry.AddDesktopFoundation(Configuration);

    // Presentation层（Phase 1.5完成后启用）
    // containerRegistry.AddDesktopPresentation();
}
```

## 与Foundation的区别

| 特性 | Foundation | Presentation |
|------|-----------|--------------|
| **职责** | 技术基础设施（HTTP、缓存、配置、安全） | UI基础设施（导航、通知、主题、打印） |
| **依赖** | 无UI依赖 | 依赖WPF、Prism |
| **使用场景** | 所有Desktop应用 | 仅WPF应用 |
| **示例** | HttpClient、CacheService | NavigationService、NotificationService |

## 迁移状态

- ✅ Phase 1.4：项目创建完成（2025-10-10）
- ✅ Phase 1.5：UI基础设施迁移完成（2025-10-10）
  - ✅ Navigation/（导航服务 - INavigationService）
  - ✅ Notifications/（通知服务 - INotificationService, NotificationService, UnifiedErrorHandlingService）
  - ✅ Theming/（主题管理 - ThemeService）
  - ✅ UserExperience/（用户体验 - UserExperienceService）
  - ✅ Print/（打印服务 - IPrescriptionPrintService）
  - ✅ Extensions/（服务注册 - PresentationServiceCollectionExtensions）

## 相关文档

- [Desktop模块化架构决策](../../../docs/architecture/adr/ADR-005-desktop-modular-architecture.md)
- [Client端业务模块统一设计标准 v2.0](../../../docs/architecture/client/unified-design-standard.md)
- [Phase 1实施计划](../../../docs/reports/phase1-implementation-plan.md)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
