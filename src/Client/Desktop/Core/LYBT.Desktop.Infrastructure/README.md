# LYBT.Desktop.Infrastructure - WPF基础设施库

## 📦 项目定位

- **层级**：Client端
- **类型**：核心库（基础设施）
- **职责**：提供WPF应用的基础设施支持，包括会话管理、错误处理、自定义控件、数据转换器、导航服务、事件系统、工具类等。为所有Desktop模块提供统一的UI组件和服务基础。

## 📂 代码结构

```
LYBT.Desktop.Infrastructure/
├── Commands/                            # 应用全局命令
│   └── ApplicationCommands.cs           # 全局命令定义
├── Configuration/                       # 配置扩展
│   └── ConfigurationExtensions.cs       # 配置扩展方法
├── Constants/                           # 常量定义（3个）
│   ├── RegionNames.cs                   # Prism区域名称常量
│   ├── ResourcePaths.cs                 # 资源路径常量
│   └── SystemConstants.cs               # 系统常量定义
├── Controls/                            # 自定义控件库（7个控件）
│   ├── Auth/
│   │   └── LoginStatusControl.xaml      # 登录状态控件
│   ├── Authentication/
│   │   └── LoginControl.xaml            # 登录控件
│   ├── ErrorHandling/
│   │   └── ErrorNotificationControl.xaml # 错误通知控件
│   ├── FormulaTemplates/
│   │   └── FormulaTemplateListItemControl.xaml # 方剂模板列表项
│   ├── GlobalStatusBar.xaml             # 全局状态栏
│   ├── VirtualizedDataGrid.xaml         # 虚拟化数据网格（性能优化）
│   └── VirtualizedListView.xaml         # 虚拟化列表视图（性能优化）
├── Converters/                          # 数据转换器（13个）
│   ├── ApiHealthStatusToColorConverter.cs # API健康状态 → 颜色
│   ├── BooleanToVisibilityConverter.cs  # 布尔值 → 可见性
│   ├── BoolToBrushConverter.cs          # 布尔值 → 画刷
│   ├── DateTimeFormatConverter.cs       # 日期时间格式化
│   ├── EnumConverters.cs                # 枚举转换器
│   ├── EnumDescriptionConverter.cs      # 枚举 → 描述文本
│   ├── FirstCharacterConverter.cs       # 首字符提取
│   ├── InverseBooleanConverter.cs       # 布尔值反转
│   ├── InverseBooleanToVisibilityConverter.cs # 反向布尔值 → 可见性
│   ├── NullToVisibilityConverter.cs     # 空值 → 可见性
│   ├── StatusToColorConverter.cs        # 状态 → 颜色
│   ├── StringToVisibilityConverter.cs   # 字符串 → 可见性
│   └── ZeroToVisibilityConverter.cs     # 零值 → 可见性
├── DependencyInjection/
│   └── RepositoryContainerRegistryExtensions.cs # 仓储容器注册扩展
├── Events/                              # 事件系统（11个事件）
│   ├── DataRefreshEvent.cs              # 数据刷新事件
│   ├── DraftSavedEvent.cs               # 草稿保存事件
│   ├── LoginSuccessEvent.cs             # 登录成功事件
│   ├── LogoutEvent.cs                   # 登出事件
│   ├── MedicalCaseFlowCancelledEvent.cs # 医案流程取消事件
│   ├── PatientSelectedEvent.cs          # 患者选中事件
│   ├── PrescriptionCompletedEvent.cs    # 处方完成事件
│   └── UserLoggedInEvent.cs             # 用户已登录事件
├── Extensions/                          # 扩展方法
│   ├── AsyncExtensions.cs               # 异步扩展方法
│   └── DialogRegistrationExtensions.cs  # 对话框注册扩展
├── Helpers/                             # 辅助类（3个）
│   ├── ExcelHelper.cs                   # Excel操作辅助类（基于NPOI）
│   ├── SearchHelper.cs                  # 搜索辅助类
│   └── WpfEnumHelper.cs                 # WPF枚举辅助类
├── Interfaces/                          # 接口定义（11个接口）
│   ├── ICommonDialogService.cs          # 通用对话框服务接口
│   ├── ICustomDialogAware.cs            # 自定义对话框感知接口
│   ├── IFeatureToggleService.cs         # 功能开关服务接口
│   ├── IKeyboardShortcutService.cs      # 键盘快捷键服务接口
│   ├── IMainWindowServicesFacade.cs     # 主窗口服务门面接口
│   ├── IPermissionService.cs            # 权限服务接口
│   ├── IRoleNavigationService.cs        # 角色导航服务接口
│   ├── ISessionManager.cs               # 会话管理器接口
│   ├── ITokenManager.cs                 # 令牌管理器接口
│   ├── IUserNotificationService.cs      # 用户通知服务接口
│   └── IUserSessionManager.cs           # 用户会话管理器接口
├── Mapping/
│   └── MappingExtensions.cs             # 映射扩展方法
├── Repositories/
│   └── RepositoryBase.cs                # 仓储基类（客户端数据访问）
├── Services/                            # 服务实现（8个核心服务）
│   ├── ErrorHandling/
│   │   ├── ErrorContext.cs              # 错误上下文
│   │   ├── ErrorHandlingService.cs      # 错误处理服务（13个方法）
│   │   └── IExceptionHandler.cs         # 异常处理器接口
│   ├── Navigation/
│   │   └── EnhancedNavigationService.cs # 增强导航服务（6个方法）
│   ├── FeatureToggleService.cs          # 功能开关服务实现（2个方法）
│   ├── KeyboardShortcutService.cs       # 键盘快捷键服务实现（11个方法）
│   ├── MainWindowServicesFacade.cs      # 主窗口服务门面实现（2个成员）
│   ├── RoleNavigationService.cs         # 角色导航服务实现（2个方法）
│   ├── SessionManager.cs                # 会话管理器实现（27个成员）
│   │   ├── CurrentUser                  # 当前用户属性
│   │   ├── CurrentToken                 # 当前令牌属性
│   │   ├── IsAuthenticated              # 认证状态属性
│   │   ├── SetSession()                 # 设置会话
│   │   ├── ClearSession()               # 清除会话
│   │   ├── HasPermission()              # 权限检查（2个重载）
│   │   ├── HasRole()                    # 角色检查
│   │   └── IsAdmin()                    # 管理员检查
│   ├── StandardErrorHandler.cs          # 标准错误处理器
│   └── UserNotificationService.cs       # 用户通知服务实现（8个方法）
└── Templates/
    └── ModernViewModelTemplate.tt       # 现代ViewModel模板（T4模板）
```

**说明**：
- **Commands/**：应用级全局命令定义
- **Controls/**：7个自定义WPF控件，包含认证、错误处理、虚拟化等
- **Converters/**：13个数据转换器，支持XAML数据绑定
- **Events/**：11个Prism事件，支持跨模块通信
- **Services/**：8个核心服务（会话管理、导航、错误处理、通知等）
- **Interfaces/**：11个服务接口，支持依赖注入和测试
- **Helpers/**：3个辅助类（Excel、搜索、枚举）

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Shared.Models** - 共享DTO模型（UserDto、请求/响应模型等）
2. **LYBT.Shared.Utilities** - 共享工具类库（密码哈希、扩展方法等）
3. **LYBT.Desktop.Foundation** - Desktop端基础类型和接口

### 被依赖项目
1. **LYBT.Desktop.Shell** - Shell层使用会话管理、导航服务
2. **LYBT.Desktop.Modules.*** - 所有业务模块使用控件、转换器、服务
3. **LYBT.Desktop.Workstations.*** - 所有工作站使用基础设施组件

### NuGet包
- **Prism.Core** (8.x) - Prism核心库（事件聚合器、命令等）
- **Prism.Wpf** (8.x) - Prism WPF扩展（区域管理、导航等）
- **NPOI** - Excel文件读写库（ExcelHelper使用）
- **System.Reactive** - 响应式扩展库（Rx.NET）
- **Microsoft.Extensions.Configuration** (8.0.x) - 配置框架
- **Microsoft.Extensions.Configuration.Json** (8.0.x) - JSON配置提供程序
- **System.ComponentModel.Annotations** - 数据注解支持

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF (Windows Presentation Foundation)**: UI框架
- **Prism 8.x**: MVVM框架（区域管理、事件聚合器、命令、依赖注入）
- **NPOI**: Excel文件操作库
- **Reactive Extensions (Rx.NET)**: 响应式编程支持
- **Microsoft.Extensions.Configuration**: 配置管理框架

##  快速开始

此项目是一个类库，无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj
```

## 🔌 核心服务接口

### 1. ISessionManager - 会话管理器接口（27个成员）

```csharp
public interface ISessionManager
{
    // 核心属性（9个）
    UserDto? CurrentUser { get; }                    // 当前用户
    string? CurrentToken { get; }                    // 当前令牌
    Guid? CurrentUserId { get; }                     // 当前用户ID
    string? CurrentUserName { get; }                 // 当前用户名
    bool IsAuthenticated { get; }                    // 是否已认证
    bool IsLoggedIn { get; }                         // 是否已登录
    string? AccessToken { get; }                     // 访问令牌
    string? RefreshToken { get; }                    // 刷新令牌

    // 核心方法（9个）
    void SetSession(UserDto user, string token);     // 设置会话
    void ClearSession();                             // 清除会话
    void SetUserSession(UserDto user);               // 设置用户会话
    void ClearUserSession();                         // 清除用户会话
    void SetCurrentUser(UserDto user);               // 设置当前用户
    void UpdateAccessToken(string token);            // 更新访问令牌

    // 权限检查（4个）
    bool HasPermission(string permission);           // 权限检查（单个）
    bool HasPermission(params string[] permissions); // 权限检查（多个）
    bool HasRole(string role);                       // 角色检查
    bool IsAdmin();                                  // 管理员检查
    string GetCurrentUserRoleDisplay();              // 获取角色显示名

    // 事件（3个）
    event EventHandler? SessionExpiring;             // 会话即将过期
    event EventHandler? SessionExpired;              // 会话已过期
    event EventHandler<SessionChangedEventArgs>? SessionChanged; // 会话变更
}
```

### 2. ErrorHandlingService - 错误处理服务（13个方法）

```csharp
public class ErrorHandlingService
{
    // 核心异常处理（2个）
    Task HandleExceptionAsync(Exception exception);   // 处理异常
    Task RegisterGlobalExceptionHandlers();          // 注册全局异常处理器

    // 全局异常捕获（2个）
    void OnUnhandledException(object sender, UnhandledExceptionEventArgs e);
    void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e);

    // 用户通知（5个）
    Task ShowErrorAsync(string message, string title = "错误");
    Task ShowSuccessAsync(string message, string title = "成功");
    Task ShowWarningAsync(string message, string title = "警告");
    Task ShowInfoAsync(string message, string title = "提示");
    Task<bool> ShowConfirmAsync(string message, string title = "确认");

    // 友好消息转换（1个）
    string GetUserFriendlyMessage(Exception exception); // 异常 → 友好消息
}
```

### 3. EnhancedNavigationService - 增强导航服务（6个方法）

```csharp
public interface IEnhancedNavigationService
{
    // 核心导航（2个）
    Task<bool> NavigateAsync(string viewName, NavigationParameters? parameters = null);
    Task<bool> NavigateBackAsync();

    // 导航状态（2个）
    bool CanNavigateBack(string regionName);
    void ClearHistory(string regionName);

    // 当前视图（1个）
    object? GetCurrentView(string regionName);
}
```

### 4. UserNotificationService - 用户通知服务（8个方法）

```csharp
public interface IUserNotificationService
{
    // 异常处理（2个）
    void HandleExceptionAsync(Exception exception);
    void RegisterGlobalExceptionHandlers();

    // 通知方法（5个）
    void ShowErrorAsync(string message, string title = "错误");
    void ShowSuccessAsync(string message, string title = "成功");
    void ShowWarningAsync(string message, string title = "警告");
    void ShowInfoAsync(string message, string title = "提示");
    Task<bool> ShowConfirmAsync(string message, string title = "确认");
}
```

### 5. KeyboardShortcutService - 键盘快捷键服务（11个方法）

```csharp
public interface IKeyboardShortcutService
{
    // 注册快捷键（2个重载）
    void RegisterGlobalShortcut(Key key, ModifierKeys modifiers, Action action, string description);
    void RegisterGlobalShortcut(string shortcutName, Key key, ModifierKeys modifiers, Action action);

    // 管理快捷键（4个）
    void UnregisterShortcut(string shortcutName);
    void EnableShortcuts();
    void DisableShortcuts();
    IReadOnlyDictionary<string, ShortcutInfo> GetRegisteredShortcuts();

    // 处理快捷键（1个）
    void HandleShortcut(object sender, KeyEventArgs e);
}
```

### 6. FeatureToggleService - 功能开关服务（2个方法）

```csharp
public interface IFeatureToggleService
{
    // 功能开关检查
    bool IsEnabled(string featureName);
}
```

### 7. RoleNavigationService - 角色导航服务（2个方法）

```csharp
public interface IRoleNavigationService
{
    // 角色导航
    void NavigateToRoleHome(UserRole role);
}
```

### 8. MainWindowServicesFacade - 主窗口服务门面（2个成员）

```csharp
public interface IMainWindowServicesFacade
{
    // 服务聚合
    IAuthenticationService AuthenticationService { get; }
}
```

## 📋 集成示例

### 示例1：会话管理器 - 认证状态与权限检查

```csharp
public class YourViewModel : BindableBase
{
    private readonly ISessionManager _sessionManager;

    public YourViewModel(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public void CheckAuthenticationState()
    {
        // 检查认证状态
        if (_sessionManager.IsAuthenticated)
        {
            var userName = _sessionManager.CurrentUserName;
            var userId = _sessionManager.CurrentUserId;
            Console.WriteLine($"当前用户：{userName}（ID: {userId}）");
        }

        // 检查权限
        if (_sessionManager.HasPermission("Patients.View"))
        {
            // 允许查看患者
        }

        // 检查角色
        if (_sessionManager.IsAdmin())
        {
            // 管理员专属功能
        }

        // 监听会话变更
        _sessionManager.SessionChanged += OnSessionChanged;
        _sessionManager.SessionExpired += OnSessionExpired;
    }

    private void OnSessionChanged(object? sender, SessionChangedEventArgs e)
    {
        // 刷新UI
        RaisePropertyChanged(nameof(IsLoggedIn));
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        // 跳转到登录页
        NavigateToLogin();
    }
}
```

### 示例2：自定义控件 - VirtualizedDataGrid

```xml
<Window xmlns:controls="clr-namespace:LYBT.Desktop.Infrastructure.Controls;assembly=LYBT.Desktop.Infrastructure">
    <Grid>
        <!-- 虚拟化数据网格（性能优化） -->
        <controls:VirtualizedDataGrid
            ItemsSource="{Binding Patients}"
            SelectedItem="{Binding SelectedPatient}"
            AutoGenerateColumns="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="患者姓名" Binding="{Binding Name}" />
                <DataGridTextColumn Header="年龄" Binding="{Binding Age}" />
                <DataGridTextColumn Header="联系电话" Binding="{Binding Phone}" />
            </DataGrid.Columns>
        </controls:VirtualizedDataGrid>

        <!-- 全局状态栏 -->
        <controls:GlobalStatusBar
            DockPanel.Dock="Bottom"
            Message="{Binding StatusMessage}"
            IsLoading="{Binding IsLoading}" />
    </Grid>
</Window>
```

**性能优势**：
-  虚拟化行渲染（仅渲染可见行）
-  延迟加载列内容
-  支持大数据量（10,000+行）
-  滚动性能优化

### 示例3：数据转换器 - XAML绑定

```xml
<Window xmlns:converters="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure">
    <Window.Resources>
        <!-- 注册转换器 -->
        <converters:BooleanToVisibilityConverter x:Key="BoolToVis" />
        <converters:InverseBooleanConverter x:Key="InverseBool" />
        <converters:DateTimeFormatConverter x:Key="DateTimeFormat" />
        <converters:EnumDescriptionConverter x:Key="EnumDesc" />
        <converters:StatusToColorConverter x:Key="StatusColor" />
    </Window.Resources>

    <Grid>
        <!-- 布尔值 → 可见性 -->
        <TextBlock Text="加载中..."
                   Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}" />

        <!-- 日期时间格式化 -->
        <TextBlock Text="{Binding CreatedAt, Converter={StaticResource DateTimeFormat}, ConverterParameter='yyyy-MM-dd HH:mm'}" />

        <!-- 枚举 → 描述文本 -->
        <TextBlock Text="{Binding Status, Converter={StaticResource EnumDesc}}" />

        <!-- 状态 → 颜色 -->
        <Border Background="{Binding Status, Converter={StaticResource StatusColor}}">
            <TextBlock Text="{Binding StatusText}" />
        </Border>

        <!-- 反向布尔值 -->
        <Button Content="提交"
                IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}" />
    </Grid>
</Window>
```

**转换器列表**：
| 转换器 | 输入 | 输出 | 用途 |
|--------|------|------|------|
| BooleanToVisibilityConverter | bool | Visibility | 布尔值 → 可见性 |
| InverseBooleanConverter | bool | bool | 布尔值反转 |
| DateTimeFormatConverter | DateTime | string | 日期时间格式化 |
| EnumDescriptionConverter | Enum | string | 枚举 → 描述文本 |
| StatusToColorConverter | Status | Brush | 状态 → 颜色 |
| NullToVisibilityConverter | object | Visibility | 空值 → 可见性 |
| StringToVisibilityConverter | string | Visibility | 字符串 → 可见性 |
| ZeroToVisibilityConverter | int | Visibility | 零值 → 可见性 |
| FirstCharacterConverter | string | string | 首字符提取 |
| BoolToBrushConverter | bool | Brush | 布尔值 → 画刷 |
| ApiHealthStatusToColorConverter | HealthStatus | Brush | API状态 → 颜色 |

### 示例4：事件系统 - 跨模块通信

```csharp
// 发布事件（在模块A中）
public class PatientListViewModel
{
    private readonly IEventAggregator _eventAggregator;

    public PatientListViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    private void SelectPatient(PatientDto patient)
    {
        // 发布患者选中事件
        _eventAggregator.GetEvent<PatientSelectedEvent>().Publish(new PatientSelectedPayload
        {
            PatientId = patient.Id,
            PatientName = patient.Name
        });
    }
}

// 订阅事件（在模块B中）
public class MedicalCaseViewModel
{
    public MedicalCaseViewModel(IEventAggregator eventAggregator)
    {
        // 订阅患者选中事件
        eventAggregator.GetEvent<PatientSelectedEvent>().Subscribe(payload =>
        {
            // 处理患者选中事件
            LoadMedicalCases(payload.PatientId);
        });

        // 订阅处方完成事件
        eventAggregator.GetEvent<PrescriptionCompletedEvent>().Subscribe(payload =>
        {
            RefreshMedicalCase(payload.MedicalCaseId);
        });

        // 订阅登出事件
        eventAggregator.GetEvent<LogoutEvent>().Subscribe(() =>
        {
            ClearData();
        });
    }
}
```

**事件列表**（11个）：
| 事件 | Payload | 用途 |
|------|---------|------|
| PatientSelectedEvent | PatientSelectedPayload | 患者选中 |
| LoginSuccessEvent | UserDto | 登录成功 |
| LogoutEvent | - | 登出 |
| PrescriptionCompletedEvent | PrescriptionCompletedPayload | 处方完成 |
| MedicalCaseFlowCancelledEvent | MedicalCaseFlowCancelledPayload | 医案流程取消 |
| DataRefreshEvent | DataRefreshPayload | 数据刷新 |
| DraftSavedEvent | DraftSavedPayload | 草稿保存 |
| UserLoggedInEvent | UserDto | 用户已登录 |

### 示例5：ExcelHelper - NPOI Excel操作

```csharp
public class ExportService
{
    public async Task ExportPatientsToExcel(List<PatientDto> patients)
    {
        // 创建Excel工作簿
        var workbook = ExcelHelper.CreateWorkbook();
        var sheet = workbook.CreateSheet("患者列表");

        // 创建表头
        var headerRow = sheet.CreateRow(0);
        headerRow.CreateCell(0).SetCellValue("患者姓名");
        headerRow.CreateCell(1).SetCellValue("性别");
        headerRow.CreateCell(2).SetCellValue("年龄");
        headerRow.CreateCell(3).SetCellValue("联系电话");

        // 填充数据
        for (int i = 0; i < patients.Count; i++)
        {
            var dataRow = sheet.CreateRow(i + 1);
            var patient = patients[i];

            dataRow.CreateCell(0).SetCellValue(patient.Name);
            dataRow.CreateCell(1).SetCellValue(patient.Gender.ToString());
            dataRow.CreateCell(2).SetCellValue(patient.Age);
            dataRow.CreateCell(3).SetCellValue(patient.PhoneNumber ?? "");
        }

        // 自动调整列宽
        for (int i = 0; i < 4; i++)
        {
            sheet.AutoSizeColumn(i);
        }

        // 保存到文件
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                     "患者列表.xlsx");
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(fileStream);

        Console.WriteLine($"导出成功：{filePath}");
    }

    public async Task<List<PatientDto>> ImportPatientsFromExcel(string filePath)
    {
        var patients = new List<PatientDto>();

        // 读取Excel文件
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var workbook = ExcelHelper.LoadWorkbook(fileStream);
        var sheet = workbook.GetSheetAt(0);

        // 读取数据行（跳过表头）
        for (int i = 1; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            if (row == null) continue;

            var patient = new PatientDto
            {
                Name = row.GetCell(0)?.StringCellValue ?? "",
                Gender = Enum.Parse<Gender>(row.GetCell(1)?.StringCellValue ?? "Male"),
                Age = (int)(row.GetCell(2)?.NumericCellValue ?? 0),
                PhoneNumber = row.GetCell(3)?.StringCellValue
            };

            patients.Add(patient);
        }

        return patients;
    }
}
```

**ExcelHelper能力**：
-  创建Excel工作簿（.xlsx）
-  创建多个Sheet
-  设置单元格样式（字体、颜色、边框）
-  合并单元格
-  自动调整列宽
-  读取Excel文件
-  支持大数据量导出（10,000+行）

### 示例6：RepositoryBase - 客户端数据访问

```csharp
public class LocalPatientRepository : RepositoryBase<PatientDto>
{
    public LocalPatientRepository()
    {
        // 初始化本地数据存储
    }

    public async Task<List<PatientDto>> GetPatientsAsync()
    {
        // 从本地存储读取患者列表
        return await LoadDataAsync("patients.json");
    }

    public async Task SavePatientAsync(PatientDto patient)
    {
        // 保存到本地存储
        var patients = await GetPatientsAsync();
        patients.Add(patient);
        await SaveDataAsync("patients.json", patients);
    }

    public async Task DeletePatientAsync(Guid patientId)
    {
        // 从本地存储删除
        var patients = await GetPatientsAsync();
        patients.RemoveAll(p => p.Id == patientId);
        await SaveDataAsync("patients.json", patients);
    }
}
```

**RepositoryBase特点**：
-  统一的客户端数据访问接口
-  JSON文件持久化
-  支持CRUD操作
-  异步操作

### 示例7：功能开关 - FeatureToggleService

```csharp
public class YourViewModel : BindableBase
{
    private readonly IFeatureToggleService _featureToggleService;

    public YourViewModel(IFeatureToggleService featureToggleService)
    {
        _featureToggleService = featureToggleService;
    }

    public void LoadFeatures()
    {
        // 检查功能是否启用
        if (_featureToggleService.IsEnabled("NewDashboard"))
        {
            LoadNewDashboard();
        }
        else
        {
            LoadLegacyDashboard();
        }

        // 条件性显示功能
        ShowExperimentalFeature = _featureToggleService.IsEnabled("ExperimentalFeatures");
    }

    private bool _showExperimentalFeature;
    public bool ShowExperimentalFeature
    {
        get => _showExperimentalFeature;
        set => SetProperty(ref _showExperimentalFeature, value);
    }
}
```

**功能开关配置**（appsettings.json）：
```json
{
  "FeatureToggles": {
    "NewDashboard": true,
    "ExperimentalFeatures": false,
    "AdvancedSearch": true,
    "BetaFeatures": false
  }
}
```

## 🏗️ 依赖注入注册

### 在Prism模块中注册所有服务

```csharp
public class YourModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册会话管理器（单例）
        containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();

        // 注册错误处理服务（单例）
        containerRegistry.RegisterSingleton<ErrorHandlingService>();

        // 注册导航服务
        containerRegistry.Register<IEnhancedNavigationService, EnhancedNavigationService>();

        // 注册通知服务
        containerRegistry.Register<IUserNotificationService, UserNotificationService>();

        // 注册键盘快捷键服务（单例）
        containerRegistry.RegisterSingleton<IKeyboardShortcutService, KeyboardShortcutService>();

        // 注册功能开关服务（单例）
        containerRegistry.RegisterSingleton<IFeatureToggleService, FeatureToggleService>();

        // 注册角色导航服务
        containerRegistry.Register<IRoleNavigationService, RoleNavigationService>();

        // 注册主窗口服务门面（单例）
        containerRegistry.RegisterSingleton<IMainWindowServicesFacade, MainWindowServicesFacade>();
    }
}
```

##  服务架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                   LYBT.Desktop.Infrastructure                    │
│                     （WPF基础设施层）                            │
└─────────────────────────────────────────────────────────────────┘
                               │
                ┌──────────────┼──────────────┐
                │              │              │
        ┌───────▼───────┐ ┌───▼───────┐ ┌───▼───────┐
        │ 会话管理      │ │ 错误处理  │ │ 导航服务  │
        │ SessionManager│ │ ErrorHandl│ │ Navigation│
        │               │ │ ingService│ │ Service   │
        └───────┬───────┘ └───────┬───┘ └───────┬───┘
                │                 │             │
        ┌───────▼───────┐ ┌───────▼───────┐ ┌─▼─────────┐
        │ 通知服务      │ │ 快捷键服务    │ │ 功能开关  │
        │ Notification  │ │ Keyboard      │ │ Feature   │
        │ Service       │ │ Shortcut      │ │ Toggle    │
        └───────────────┘ └───────────────┘ └───────────┘
                               │
                ┌──────────────┼──────────────┐
                │              │              │
        ┌───────▼───────┐ ┌───▼───────┐ ┌───▼───────┐
        │ 自定义控件    │ │ 转换器    │ │ 事件系统  │
        │ Controls      │ │ Converters│ │ Events    │
        │ (7个)         │ │ (13个)    │ │ (11个)    │
        └───────────────┘ └───────────┘ └───────────┘
```

##  设计原则

### 1. Infrastructure vs Foundation 职责划分

| 维度 | Infrastructure | Foundation |
|------|---------------|------------|
| **性质** | WPF特定 | 平台无关 |
| **组件** | 控件、转换器、事件 | HTTP客户端、缓存、配置 |
| **依赖** | 依赖WPF/Prism | 无UI依赖 |
| **示例** | GlobalStatusBar, BooleanToVisibilityConverter | HttpClientFactory, CacheService |

### 2. 会话管理原则

**核心设计**：
-  单例模式（全局唯一）
-  线程安全（ConcurrentDictionary）
-  事件驱动（SessionChanged, SessionExpired）
-  缓存优化（_cachedUser, _cachedToken）

**权限检查策略**：
-  基于角色（HasRole）
-  基于权限（HasPermission）
-  组合检查（HasPermission + HasRole）

### 3. 错误处理原则

**全局异常捕获**：
-  AppDomain.CurrentDomain.UnhandledException
-  TaskScheduler.UnobservedTaskException
-  Dispatcher.UnhandledException（WPF）

**友好错误消息**：
-  网络错误："请检查网络连接"
-  验证错误："输入数据不符合要求"
-  业务错误：显示业务规则消息
-  系统错误："请联系管理员"

### 4. 虚拟化性能优化

**VirtualizedDataGrid/ListView**：
-  仅渲染可见行（减少DOM元素）
-  延迟加载列内容
-  支持大数据量（10,000+行）
-  滚动性能优化

**适用场景**：
-  患者列表（数千条记录）
-  医案列表（长期积累）
-  处方列表（大量历史）

### 5. 事件系统设计

**Prism EventAggregator模式**：
-  解耦模块（发布者/订阅者）
-  类型安全（强类型Payload）
-  线程安全（ThreadOption.UIThread）
-  弱引用（防止内存泄漏）

**事件命名规范**：
-  过去时态：PatientSelectedEvent, LoginSuccessEvent
-  清晰描述：MedicalCaseFlowCancelledEvent
-  避免缩写：PrescriptionCompletedEvent（非PrxCompEvent）

## 📚 详细文档

- **完整模块文档**：[docs/reference/modules/infrastructure/](../../../../../docs/reference/modules/infrastructure/) *(待创建)*
- **架构设计**：[docs/explanation/architecture/client/infrastructure-layer-design.md](../../../../../docs/explanation/architecture/client/infrastructure-layer-design.md) *(待创建)*
- **开发指南**：[docs/how-to-guides/client/infrastructure-usage.md](../../../../../docs/how-to-guides/client/infrastructure-usage.md) *(待创建)*
- **Prism框架**：[docs/explanation/architecture/client/README.md](../../../../../docs/explanation/architecture/client/README.md) - 参见"Prism框架应用"章节

---

**最后更新**：2025-10-29
**维护负责**：Client端开发组
