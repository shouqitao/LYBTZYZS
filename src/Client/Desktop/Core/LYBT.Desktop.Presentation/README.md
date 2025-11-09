# LYBT.Desktop.Presentation - Desktop端UI基础设施层

## 📦 项目定位

- **层级**: Client端核心库
- **类型**: UI基础设施层
- **职责**: 提供与用户界面相关的技术基础服务，包括导航、通知、主题、用户体验优化、错误处理和跨模块共享组件。作为Desktop端的UI技术支撑层，为业务模块提供统一的用户交互体验标准。

## 📂 代码结构

```
LYBT.Desktop.Presentation/
├── Extensions/                            # 服务注册扩展
│   └── PresentationServiceCollectionExtensions.cs  # DI注册扩展(1个方法)
├── Navigation/                            # 导航服务
│   └── INavigationService.cs              # 导航服务接口(5个方法)
├── Notifications/                         # 通知与错误处理
│   ├── INotificationService.cs            # 通知服务接口(13个方法+2个事件)
│   ├── NotificationService.cs             # 通知服务实现(17个方法+2个事件)
│   └── UnifiedErrorHandlingService.cs     # 统一错误处理服务(20个方法+2个事件)
├── Theming/                               # 主题管理
│   └── ThemeService.cs                    # 主题服务(5个方法/属性)
├── UserExperience/                        # 用户体验
│   └── UserExperienceService.cs           # 用户体验服务(29个方法/属性/事件)
├── Components/                            # 跨模块共享组件
│   └── PatientSelector/                   # 患者选择器组件
│       ├── PatientSelectorControl.xaml    # 控件视图
│       ├── PatientSelectorControl.xaml.cs # 控件代码隐藏
│       ├── PatientSelectorViewModel.cs    # 组件ViewModel(34个成员)
│       └── README.md                      # 组件使用文档
├── Mapping/                               # AutoMapper配置
│   ├── PatientSelectorMappingProfile.cs   # PatientSelector映射配置
│   └── PresentationMappingExtensions.cs   # 映射扩展方法
└── DependencyInjection/                   # Prism容器扩展
    └── ViewModelContainerRegistryExtensions.cs  # ViewModel注册扩展
```

**说明**:
- **Extensions/**: 服务注册扩展方法，统一注册Presentation层服务
- **Navigation/**: 导航服务接口，提供页面导航、返回和历史管理
- **Notifications/**: 13个通知方法（同步+异步）+ 20个错误处理方法 + 4个事件
- **Theming/**: 主题服务（亮色/暗色主题切换、动态主题应用）
- **UserExperience/**: 29个用户体验方法（加载指示器、进度条、反馈系统、友好错误）
- **Components/**: 跨模块共享组件（如PatientSelector患者选择器）
- **Mapping/**: AutoMapper配置，支持跨模块组件的DTO映射
- **DependencyInjection/**: Prism容器扩展，支持ViewModel注册

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Foundation** - Desktop端技术基础设施（HttpClient、缓存、配置）
2. **LYBT.Desktop.Contracts** - Desktop端API契约接口（Refit API定义）
3. **LYBT.Shared.Models** - 共享DTO模型（PatientDto、UserDto等）
4. **LYBT.Shared.Interfaces** - 共享接口定义
5. **LYBT.Shared.Utilities** - 共享工具类

### 被依赖项目
1. **LYBT.Desktop.Auth** - 认证模块（使用导航、通知、用户体验服务）
2. **LYBT.Desktop.Users** - 用户模块（使用通知、错误处理、主题服务）
3. **LYBT.Desktop.Patients** - 患者模块（使用PatientSelector组件、通知服务）
4. **LYBT.Desktop.MedicalCase** - 医疗案例模块（使用导航、通知、PatientSelector）
5. **LYBT.Desktop.Consultation** - 诊疗模块（使用导航、用户体验、错误处理）
6. **LYBT.Desktop.Prescriptions** - 处方模块（使用通知、用户体验服务）
7. **LYBT.Desktop.Herbs** - 中药材模块（使用通知、主题服务）
8. **LYBT.Desktop.Formula** - 验方模块（使用通知、用户体验服务）
9. **LYBT.Desktop.Shell** - Shell主窗口（使用所有Presentation层服务）
10. **测试项目**:
    - LYBT.Desktop.Presentation.Tests（单元测试）
    - LYBT.Desktop.IntegrationTests（集成测试）

### NuGet包
- **Prism.Core** (8.x) - MVVM框架核心（IEventAggregator、INavigationAware）
- **Prism.Wpf** (8.x) - WPF集成（DelegateCommand、IContainerRegistry）
- **Microsoft.Extensions.Logging** (8.0.x) - 日志框架
- **Microsoft.Extensions.DependencyInjection** (8.0.x) - 依赖注入容器
- **AutoMapper** (13.x) - 对象映射框架（支持PatientSelector的DTO转换）

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF**: Windows Presentation Foundation（桌面端UI框架）
- **Prism 8.x**: MVVM框架（导航、命令、事件聚合器）
- **AutoMapper 13.x**: 对象映射框架（跨模块组件的DTO转换）
- **System.Windows.Threading**: UI线程调度器（确保UI线程安全）
- **System.ComponentModel**: INotifyPropertyChanged支持（MVVM数据绑定）

## 🚀 快速开始

此项目是一个类库，作为Desktop端应用的一部分被 `LYBT.Desktop.Shell` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Presentation/LYBT.Desktop.Presentation.csproj
```

**集成说明**:

### 1. 注册Presentation层服务(在App.xaml.cs中)
```csharp
using LYBT.Desktop.Presentation.Extensions;
using LYBT.Desktop.Foundation.Extensions;
using Microsoft.Extensions.DependencyInjection;

public partial class App : PrismApplication
{
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. 注册Foundation层（技术基础设施）
        containerRegistry.AddDesktopFoundation(Configuration);

        // 2. 注册Presentation层（UI基础设施）
        var services = new ServiceCollection();
        services.AddDesktopPresentation();

        // 3. 注册到Prism容器
        foreach (var service in services)
        {
            containerRegistry.Register(
                service.ServiceType,
                service.ImplementationType,
                service.Lifetime switch
                {
                    ServiceLifetime.Singleton => true,
                    _ => false
                }
            );
        }
    }
}
```

### 2. 导航服务使用（页面跳转与返回）
```csharp
using LYBT.Desktop.Presentation.Navigation;
using Prism.Commands;
using Prism.Mvvm;

public class MainWindowViewModel : BindableBase
{
    private readonly INavigationService _navigation;

    public MainWindowViewModel(INavigationService navigation)
    {
        _navigation = navigation;
        NavigateToPatientListCommand = new DelegateCommand(
            async () => await NavigateToPatientListAsync()
        );
    }

    public DelegateCommand NavigateToPatientListCommand { get; }

    private async Task NavigateToPatientListAsync()
    {
        // 导航到患者列表视图
        await _navigation.NavigateAsync("PatientListView");
    }

    private async Task GoBackAsync()
    {
        // 返回上一页（如果可以返回）
        if (_navigation.CanGoBack)
        {
            await _navigation.GoBackAsync();
        }
    }

    private async Task NavigateToHomeAsync()
    {
        // 导航到首页并清除历史记录
        await _navigation.NavigateToHomeAsync();
        _navigation.ClearHistory();
    }
}
```

### 3. 通知服务使用（Toast通知与对话框）
```csharp
using LYBT.Desktop.Presentation.Notifications;
using LYBT.Desktop.Contracts.Api;

public class PatientViewModel : BindableBase
{
    private readonly INotificationService _notification;
    private readonly IPatientApi _patientApi;

    public PatientViewModel(
        INotificationService notification,
        IPatientApi patientApi)
    {
        _notification = notification;
        _patientApi = patientApi;
    }

    // 同步通知（快速反馈）
    public async Task SavePatientAsync()
    {
        var result = await _patientApi.CreatePatientAsync(patient);

        if (result.IsSuccess)
        {
            _notification.ShowSuccess("患者信息保存成功");
        }
        else
        {
            _notification.ShowError(result.ErrorMessage);
        }
    }

    // 异步通知（需要等待用户确认）
    public async Task DeletePatientAsync()
    {
        var confirmed = await _notification.ShowConfirmAsync(
            "确认删除患者信息？此操作不可撤销。",
            "删除确认"
        );

        if (confirmed)
        {
            await _patientApi.DeletePatientAsync(patient.Id);
            await _notification.ShowSuccessAsync("删除成功");
        }
    }

    // 加载指示器（长时间操作）
    public async Task LoadPatientsAsync()
    {
        _notification.ShowLoading("正在加载患者列表...");

        try
        {
            var result = await _patientApi.GetPatientsAsync(page: 1, pageSize: 20);
            // 处理结果...
        }
        finally
        {
            _notification.HideLoading();
        }
    }
}
```

### 4. 统一错误处理（全局异常捕获与友好提示）
```csharp
using LYBT.Desktop.Presentation.Notifications;

public class App : PrismApplication
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 注册全局异常处理器
        var errorHandlingService = Container.Resolve<IErrorHandlingService>();
        errorHandlingService.RegisterGlobalExceptionHandlers();

        // 监听严重错误（记录日志、发送遥测）
        errorHandlingService.CriticalErrorOccurred += (sender, args) =>
        {
            // 记录严重错误到日志文件
            _logger.LogCritical(args.Exception, "严重错误: {Message}", args.Message);
        };
    }
}

// 在ViewModel中使用ExecuteSafelyAsync（自动处理异常）
public class PatientViewModel : BindableBase
{
    private readonly IErrorHandlingService _errorHandling;

    public async Task SavePatientWithErrorHandlingAsync()
    {
        await _errorHandling.ExecuteSafelyAsync(
            async () => await _patientApi.CreatePatientAsync(patient),
            errorMessage: "保存患者信息失败",
            onError: (ex) => _logger.LogError(ex, "保存患者错误")
        );
    }

    // 有返回值的版本
    public async Task<PatientDto?> LoadPatientWithErrorHandlingAsync(Guid patientId)
    {
        return await _errorHandling.ExecuteSafelyAsync(
            async () => await _patientApi.GetPatientByIdAsync(patientId),
            errorMessage: "加载患者信息失败"
        );
    }
}
```

### 5. 主题服务使用（亮色/暗色主题切换）
```csharp
using LYBT.Desktop.Presentation.Theming;

public class SettingsViewModel : BindableBase
{
    private readonly IThemeService _theme;

    public SettingsViewModel(IThemeService theme)
    {
        _theme = theme;
        ToggleThemeCommand = new DelegateCommand(async () => await ToggleThemeAsync());
    }

    public DelegateCommand ToggleThemeCommand { get; }

    // 切换主题（亮色 ↔ 暗色）
    private async Task ToggleThemeAsync()
    {
        await _theme.ToggleThemeAsync();
    }

    // 应用指定主题
    private async Task ApplyDarkThemeAsync()
    {
        await _theme.ApplyThemeAsync(Theme.Dark);
    }

    // 获取当前主题状态
    public bool IsDarkMode => _theme.IsDarkMode;
}
```

### 6. 用户体验服务（加载指示器、进度条、反馈系统）
```csharp
using LYBT.Desktop.Presentation.UserExperience;

public class ImportViewModel : BindableBase
{
    private readonly IUserExperienceService _ux;

    public ImportViewModel(IUserExperienceService ux)
    {
        _ux = ux;

        // 监听加载状态变化（更新UI）
        _ux.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_ux.IsGlobalLoading))
            {
                RaisePropertyChanged(nameof(IsProcessing));
            }
        };
    }

    // 全局加载指示器（覆盖整个窗口）
    public async Task ImportPatientsAsync()
    {
        _ux.StartGlobalLoading("正在导入患者数据...");

        try
        {
            await _patientImporter.ImportAsync();
        }
        finally
        {
            _ux.StopGlobalLoading();
        }
    }

    // 进度条（显示操作进度）
    public async Task ImportWithProgressAsync(List<PatientDto> patients)
    {
        _ux.StartGlobalLoading("正在导入患者数据...");

        for (int i = 0; i < patients.Count; i++)
        {
            await _patientApi.CreatePatientAsync(patients[i]);

            // 更新进度（0-100）
            _ux.UpdateProgress((i + 1) * 100 / patients.Count,
                $"已导入 {i + 1}/{patients.Count} 条记录");
        }

        _ux.StopGlobalLoading();
    }

    // 友好错误提示（转换技术错误为用户可理解的消息）
    public async Task SaveWithFriendlyErrorAsync()
    {
        try
        {
            await _patientApi.CreatePatientAsync(patient);
        }
        catch (Exception ex)
        {
            await _ux.ShowFriendlyErrorAsync(ex);
        }
    }

    // ExecuteWithFeedbackAsync（操作+反馈一体化）
    public async Task SaveWithFeedbackAsync()
    {
        await _ux.ExecuteWithFeedbackAsync(
            async () => await _patientApi.CreatePatientAsync(patient),
            successMessage: "保存成功",
            errorMessage: "保存失败",
            loadingMessage: "正在保存..."
        );
    }
}
```

### 7. PatientSelector跨模块组件（患者选择器）
```csharp
// 在XAML中使用PatientSelector控件
<Window xmlns:components="clr-namespace:LYBT.Desktop.Presentation.Components.PatientSelector;assembly=LYBT.Desktop.Presentation"
        x:Class="LYBT.Desktop.MedicalCase.Views.MedicalCaseCreateView">
    <Grid>
        <components:PatientSelectorControl x:Name="PatientSelector" />
    </Grid>
</Window>

// 在ViewModel中监听患者选择事件
public class MedicalCaseCreateViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;
    private Guid? _selectedPatientId;

    public MedicalCaseCreateViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;

        // 订阅患者选择事件
        _eventAggregator.GetEvent<PatientSelectedEvent>().Subscribe(OnPatientSelected);
    }

    private void OnPatientSelected(PatientSelectedPayload payload)
    {
        _selectedPatientId = payload.PatientId;
        PatientName = payload.PatientName;

        // 继续创建医疗案例流程...
        _notification.ShowSuccess($"已选择患者: {payload.PatientName}");
    }
}
```

## 🔌 API 接口

此项目为UI基础设施层，不直接对外提供API接口。它定义的服务被Desktop端业务模块使用。

**主要服务接口**:

### INavigationService（导航服务）
| 方法/属性 | 签名 | 说明 |
|----------|------|------|
| NavigateAsync | `Task NavigateAsync(string viewName, NavigationParameters? parameters = null)` | 导航到指定视图 |
| GoBackAsync | `Task GoBackAsync()` | 返回上一页 |
| ClearHistory | `void ClearHistory()` | 清除导航历史 |
| NavigateToHomeAsync | `Task NavigateToHomeAsync()` | 导航到首页 |
| CanGoBack | `bool CanGoBack { get; }` | 是否可以返回 |

### INotificationService（通知服务）
| 方法/事件 | 签名 | 说明 |
|----------|------|------|
| ShowInfo | `void ShowInfo(string message)` | 显示信息通知（同步） |
| ShowSuccess | `void ShowSuccess(string message)` | 显示成功通知（同步） |
| ShowWarning | `void ShowWarning(string message)` | 显示警告通知（同步） |
| ShowError | `void ShowError(string message)` | 显示错误通知（同步） |
| ShowInfoAsync | `Task ShowInfoAsync(string message)` | 显示信息通知（异步） |
| ShowSuccessAsync | `Task ShowSuccessAsync(string message)` | 显示成功通知（异步） |
| ShowWarningAsync | `Task ShowWarningAsync(string message)` | 显示警告通知（异步） |
| ShowErrorAsync | `Task ShowErrorAsync(string message)` | 显示错误通知（异步） |
| ShowConfirmAsync | `Task<bool> ShowConfirmAsync(string message, string title = "确认")` | 显示确认对话框 |
| ShowLoading | `void ShowLoading(string message = "加载中...")` | 显示加载指示器 |
| HideLoading | `void HideLoading()` | 隐藏加载指示器 |
| NotificationShown | `event EventHandler<NotificationEventArgs> NotificationShown` | 通知显示事件 |
| LoadingStateChanged | `event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged` | 加载状态变化事件 |

### IErrorHandlingService（错误处理服务）
| 方法/事件 | 签名 | 说明 |
|----------|------|------|
| HandleException | `ErrorHandlingResult HandleException(Exception exception, string? context = null)` | 处理异常（同步） |
| HandleExceptionAsync | `Task<ErrorHandlingResult> HandleExceptionAsync(Exception exception, string? context = null)` | 处理异常（异步） |
| ExecuteSafelyAsync | `Task ExecuteSafelyAsync(Func<Task> action, string? errorMessage = null, Action<Exception>? onError = null)` | 安全执行操作（无返回值） |
| ExecuteSafelyAsync | `Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action, string? errorMessage = null)` | 安全执行操作（有返回值） |
| RegisterGlobalExceptionHandlers | `void RegisterGlobalExceptionHandlers()` | 注册全局异常处理器 |
| ShowErrorAsync | `Task ShowErrorAsync(Exception exception, string? title = null)` | 显示友好错误提示 |
| LogErrorAsync | `Task LogErrorAsync(Exception exception, string? context = null)` | 记录错误日志 |
| GetUserFriendlyMessage | `string GetUserFriendlyMessage(Exception exception)` | 获取用户友好错误消息 |
| GetErrorCategory | `ErrorCategory GetErrorCategory(Exception exception)` | 获取错误类别 |
| GetErrorSeverity | `ErrorSeverity GetErrorSeverity(Exception exception)` | 获取错误严重程度 |
| GetSuggestedActions | `List<string> GetSuggestedActions(Exception exception)` | 获取建议操作 |
| CanRetry | `bool CanRetry(Exception exception)` | 是否可以重试 |
| ErrorOccurred | `event EventHandler<ErrorEventArgs> ErrorOccurred` | 错误发生事件 |
| CriticalErrorOccurred | `event EventHandler<ErrorEventArgs> CriticalErrorOccurred` | 严重错误事件 |

### IThemeService（主题服务）
| 方法/属性 | 签名 | 说明 |
|----------|------|------|
| ApplyThemeAsync | `Task ApplyThemeAsync(Theme theme)` | 应用指定主题 |
| ToggleThemeAsync | `Task ToggleThemeAsync()` | 切换主题（亮色↔暗色） |
| CurrentTheme | `Theme CurrentTheme { get; }` | 当前主题 |
| IsDarkMode | `bool IsDarkMode { get; }` | 是否暗色主题 |

### IUserExperienceService（用户体验服务）
| 方法/属性/事件 | 签名 | 说明 |
|--------------|------|------|
| StartGlobalLoading | `void StartGlobalLoading(string message = "加载中...")` | 开始全局加载 |
| StopGlobalLoading | `void StopGlobalLoading()` | 停止全局加载 |
| UpdateProgress | `void UpdateProgress(double progress, string? message = null)` | 更新进度（0-100） |
| ShowSuccessFeedback | `void ShowSuccessFeedback(string message)` | 显示成功反馈 |
| ShowErrorFeedback | `void ShowErrorFeedback(string message)` | 显示错误反馈 |
| ShowWarningFeedback | `void ShowWarningFeedback(string message)` | 显示警告反馈 |
| ShowInfoFeedback | `void ShowInfoFeedback(string message)` | 显示信息反馈 |
| ClearStatusMessage | `void ClearStatusMessage()` | 清除状态消息 |
| ExecuteWithFeedbackAsync | `Task ExecuteWithFeedbackAsync(Func<Task> action, string? successMessage = null, string? errorMessage = null, string? loadingMessage = null)` | 带反馈执行操作（无返回值） |
| ExecuteWithFeedbackAsync | `Task<T?> ExecuteWithFeedbackAsync<T>(Func<Task<T>> action, string? successMessage = null, string? errorMessage = null, string? loadingMessage = null)` | 带反馈执行操作（有返回值） |
| ShowFriendlyErrorAsync | `Task ShowFriendlyErrorAsync(Exception exception)` | 显示友好错误提示 |
| ShowStatusMessage | `void ShowStatusMessage(string message, int durationSeconds = 3)` | 显示状态消息（自动消失） |
| GetUserFriendlyErrorMessage | `string GetUserFriendlyErrorMessage(Exception exception)` | 获取友好错误消息 |
| IsGlobalLoading | `bool IsGlobalLoading { get; set; }` | 是否全局加载中 |
| LoadingMessage | `string? LoadingMessage { get; set; }` | 加载消息 |
| StatusMessage | `string? StatusMessage { get; set; }` | 状态消息 |
| CurrentFeedbackType | `FeedbackType CurrentFeedbackType { get; set; }` | 当前反馈类型 |
| OperationProgress | `double OperationProgress { get; set; }` | 操作进度（0-100） |
| PropertyChanged | `event PropertyChangedEventHandler? PropertyChanged` | 属性变化事件 |

**完整接口定义**请参考各服务类的源代码和接口定义。

## 📊 核心服务架构

### 导航服务架构
```
INavigationService（接口）
   ↓ 实现
NavigationService（Prism Region导航）
   ↓ 依赖
IRegionManager（Prism区域管理器）
```

### 通知服务架构
```
INotificationService（接口）
   ↓ 实现
NotificationService（WPF MessageBox + Toast）
   ↓ 触发
NotificationShown事件（通知显示）
LoadingStateChanged事件（加载状态变化）
```

### 错误处理服务架构
```
IErrorHandlingService（接口）
   ↓ 实现
UnifiedErrorHandlingService（统一错误处理）
   ↓ 功能
1. 全局异常捕获（AppDomain.CurrentDomain.UnhandledException）
2. 任务异常捕获（TaskScheduler.UnobservedTaskException）
3. 友好错误消息转换（技术错误 → 用户消息）
4. 错误分类（Network/Validation/Business/System）
5. 严重程度判断（Info/Warning/Error/Critical）
6. 建议操作生成（"检查网络连接"、"请重试"等）
7. 重试逻辑判断（网络错误可重试，验证错误不可重试）
8. 错误事件发布（ErrorOccurred/CriticalErrorOccurred）
```

### 主题服务架构
```
IThemeService（接口）
   ↓ 实现
ThemeService（ResourceDictionary切换）
   ↓ 管理
Light.xaml（亮色主题资源字典）
Dark.xaml（暗色主题资源字典）
```

### 用户体验服务架构
```
IUserExperienceService（接口）
   ↓ 实现
UserExperienceService（用户体验优化）
   ↓ 功能
1. 全局加载指示器（覆盖整个窗口）
2. 操作进度条（0-100%进度显示）
3. 反馈系统（Success/Error/Warning/Info）
4. 状态消息（自动消失的提示条）
5. 友好错误提示（技术错误 → 用户消息）
6. 安全执行封装（自动处理异常和反馈）
   ↓ 依赖
INotificationService（通知服务）
```

### 跨模块组件架构（PatientSelector示例）
```
PatientSelectorControl.xaml（UI视图）
   ↓ 绑定
PatientSelectorViewModel（独立ViewModel）
   ↓ 功能
1. 搜索患者（Debounce防抖 + 取消令牌）
2. 选择患者（触发PatientSelectedEvent）
3. 快速创建患者（模态对话框）
   ↓ 发布事件
PatientSelectedEvent（通过IEventAggregator）
   ↓ 订阅者
MedicalCaseCreateViewModel（医疗案例创建）
ConsultationViewModel（诊疗视图）
PrescriptionViewModel（处方视图）
```

## 🎯 设计原则

### UI基础设施vs技术基础设施
| 特性 | Foundation（技术基础设施） | Presentation（UI基础设施） |
|------|-------------------------|-------------------------|
| **职责** | HTTP、缓存、配置、安全 | 导航、通知、主题、用户体验 |
| **依赖** | 无UI依赖 | 依赖WPF、Prism |
| **使用场景** | 所有Desktop应用 | 仅WPF应用 |
| **示例** | HttpClient、CacheService | NavigationService、NotificationService |

### 跨模块组件设计原则
1. **独立ViewModel**: 组件拥有独立的ViewModel，不依赖父级ViewModel
2. **事件驱动通信**: 通过IEventAggregator发布事件，解耦组件与使用者
3. **AutoMapper支持**: 提供独立的MappingProfile，支持DTO转换
4. **文档齐全**: 每个组件提供独立的README文档，说明使用方法

### 错误处理分级策略
| 错误类别 | 用户提示 | 日志级别 | 是否可重试 |
|---------|---------|---------|-----------|
| Network | "网络连接失败，请检查网络" | Warning | ✅ 是 |
| Validation | "输入数据不符合要求" | Info | ❌ 否 |
| Business | "操作失败：{业务规则}" | Warning | ❌ 否 |
| System | "系统错误，请联系管理员" | Error | ✅ 视情况 |
| Critical | "严重错误，应用将退出" | Critical | ❌ 否 |

## 📚 详细文档

- **完整模块文档**: [docs/reference/modules/presentation/](../../../../docs/reference/modules/presentation/) *(待创建)*
- **架构设计**: [docs/explanation/architecture/client/presentation-design.md](../../../../docs/explanation/architecture/client/presentation-design.md) *(待创建)*
- **开发指南**: [docs/how-to-guides/client/presentation-development.md](../../../../docs/how-to-guides/client/presentation-development.md) *(待创建)*
- **组件文档**: [Components/PatientSelector/README.md](Components/PatientSelector/README.md)

## 🔄 版本历史

### v2.0 (2025-10-10) - 模块化重构
- ✅ 从LYBT.Desktop.Infrastructure迁移UI基础设施服务
- ✅ 新增Navigation/导航服务
- ✅ 新增Notifications/通知服务（13个方法+2个事件）
- ✅ 新增UnifiedErrorHandlingService/统一错误处理服务（20个方法+2个事件）
- ✅ 新增Theming/主题服务
- ✅ 新增UserExperience/用户体验服务（29个方法/属性/事件）
- ✅ 新增Components/PatientSelector跨模块组件（34个成员）
- ✅ 新增Mapping/AutoMapper配置（支持跨模块组件的DTO转换）
- ✅ 新增DependencyInjection/Prism容器扩展（支持ViewModel注册）
- ✅ 实施Issue #1114: Desktop端模块化架构重构

---

**最后更新**: 2025-10-29
**维护负责**: Client端开发组

🤖 Generated with [Claude Code](https://claude.com/claude-code)
