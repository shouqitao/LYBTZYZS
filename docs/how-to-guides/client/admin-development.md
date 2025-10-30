# Admin模块开发指南

> **文档类型**: How-to Guide
> **目标读者**: Client端开发人员
> **前置阅读**: [Admin模块架构设计](../../explanation/architecture/client/admin-module-design.md)

---

## 1. 概述

本指南提供Admin模块的完整实施步骤，包括模块创建、ViewModel实现、View设计、权限控制集成等。

**模块职责**：
- 用户管理（CRUD操作）
- 系统配置管理
- 数据库维护
- 审计日志查看
- 系统统计数据查看

**遵循规范**：
- 业务规则：AC-002（角色路由规则）
- 架构模式：MVVM + Prism
- 代码规范：PascalCase、依赖注入、异步编程

---

## 2. 前置条件

### 环境要求
- Visual Studio 2022 17.8+
- .NET 8.0 SDK
- LYBT项目已克隆到本地
- WebAPI已启动（`https://localhost:5001`）

### 依赖检查
```powershell
# 检查.NET版本
dotnet --version  # 应该显示 8.0.x

# 检查Prism包
dotnet list package | Select-String "Prism"

# 检查Server端运行状态
curl https://localhost:5001/api/v1/health
```

### 必读文档
- `docs/explanation/architecture/client/README.md` - Client端架构总览
- `docs/explanation/architecture/client/admin-module-design.md` - Admin模块架构设计
- `docs/explanation/business-rules.md` - 业务规则AC-002

---

## 3. 模块结构创建

### Step 1: 创建项目结构

在 `src/Client/Desktop/Modules/` 目录下创建Admin模块项目：

```powershell
cd D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules

# 创建Admin模块项目
dotnet new classlib -n LYBT.Desktop.Admin -f net8.0-windows

# 创建标准目录结构
cd LYBT.Desktop.Admin
mkdir ViewModels
mkdir Views
mkdir Models
mkdir Services
```

### Step 2: 添加Prism依赖

编辑 `LYBT.Desktop.Admin.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Prism框架 -->
    <PackageReference Include="Prism.Unity" Version="9.0.537" />
    <PackageReference Include="Prism.Wpf" Version="9.0.537" />

    <!-- 日志 -->
    <PackageReference Include="NLog" Version="5.2.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- 项目引用 -->
    <ProjectReference Include="..\..\Foundation\LYBT.Desktop.Foundation\LYBT.Desktop.Foundation.csproj" />
    <ProjectReference Include="..\..\..\Shared\LYBT.Shared.Contracts\LYBT.Shared.Contracts.csproj" />
  </ItemGroup>
</Project>
```

### Step 3: 创建Prism模块类

创建 `AdminModule.cs`：

```csharp
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using LYBT.Desktop.Admin.Views;
using LYBT.Desktop.Admin.ViewModels;

namespace LYBT.Desktop.Admin;

/// <summary>
/// Admin模块定义
/// </summary>
public class AdminModule : IModule
{
    private readonly IRegionManager _regionManager;

    public AdminModule(IRegionManager regionManager)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化后的逻辑（如需要）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册ViewModels（自动解析）
        containerRegistry.RegisterForNavigation<AdminHomeView, AdminHomeViewModel>();
        containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModel>();
        containerRegistry.RegisterForNavigation<SystemConfigView, SystemConfigViewModel>();
        containerRegistry.RegisterForNavigation<DatabaseMaintenanceView, DatabaseMaintenanceViewModel>();

        // 注册服务（如有自定义服务）
        // containerRegistry.RegisterSingleton<IAdminService, AdminService>();
    }
}
```

---

## 4. 实现AdminHomeViewModel

### Step 1: 创建ViewModel基础结构

创建 `ViewModels/AdminHomeViewModel.cs`：

```csharp
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NLog;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;
using LYBT.Desktop.Foundation.ViewModels;
using LYBT.Desktop.Foundation.Services;
using LYBT.Desktop.Foundation.Managers;
using LYBT.Desktop.Foundation.Helpers;
using LYBT.Shared.Contracts.DTOs;
using LYBT.Shared.Contracts.Enums;

namespace LYBT.Desktop.Admin.ViewModels;

/// <summary>
/// Admin主页ViewModel
/// </summary>
public class AdminHomeViewModel : UnifiedViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IUserService _userService;
    private readonly IStatisticsService _statisticsService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnityContainer _container;
    private readonly IEventAggregator _eventAggregator;
    private readonly IRegionManager _regionManager;

    public AdminHomeViewModel(
        IUserService userService,
        IStatisticsService statisticsService,
        IAuditLogService auditLogService,
        IUnityContainer container,
        IEventAggregator eventAggregator,
        IRegionManager regionManager)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _container = container ?? throw new ArgumentNullException(nameof(container));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

        // 初始化命令
        InitializeCommands();

        // 订阅事件
        SubscribeEvents();

        // 加载初始数据
        _ = LoadStatisticsAsync();
    }

    #region Properties

    private int _userCount;
    public int UserCount
    {
        get => _userCount;
        set => SetProperty(ref _userCount, value);
    }

    private int _todayMedicalCaseCount;
    public int TodayMedicalCaseCount
    {
        get => _todayMedicalCaseCount;
        set => SetProperty(ref _todayMedicalCaseCount, value);
    }

    private int _activeDoctorCount;
    public int ActiveDoctorCount
    {
        get => _activeDoctorCount;
        set => SetProperty(ref _activeDoctorCount, value);
    }

    private string _systemStatus = "运行正常";
    public string SystemStatus
    {
        get => _systemStatus;
        set => SetProperty(ref _systemStatus, value);
    }

    private DateTime _lastUpdateTime = DateTime.Now;
    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set => SetProperty(ref _lastUpdateTime, value);
    }

    private ObservableCollection<AuditLogDto> _recentAuditLogs = new();
    public ObservableCollection<AuditLogDto> RecentAuditLogs
    {
        get => _recentAuditLogs;
        set => SetProperty(ref _recentAuditLogs, value);
    }

    #endregion

    #region Commands

    public ICommand NavigateToUserManagementCommand { get; private set; } = null!;
    public ICommand NavigateToSystemConfigCommand { get; private set; } = null!;
    public ICommand NavigateToDatabaseMaintenanceCommand { get; private set; } = null!;
    public ICommand RefreshCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        NavigateToUserManagementCommand = new DelegateCommand(ExecuteNavigateToUserManagement);
        NavigateToSystemConfigCommand = new DelegateCommand(ExecuteNavigateToSystemConfig);
        NavigateToDatabaseMaintenanceCommand = new DelegateCommand(ExecuteNavigateToDatabaseMaintenance);
        RefreshCommand = new DelegateCommand(async () => await ExecuteRefreshAsync());
    }

    #endregion

    #region Command Handlers

    private void ExecuteNavigateToUserManagement()
    {
        _regionManager.RequestNavigate("MainRegion", "UserManagementView");
    }

    private void ExecuteNavigateToSystemConfig()
    {
        _regionManager.RequestNavigate("MainRegion", "SystemConfigView");
    }

    private void ExecuteNavigateToDatabaseMaintenance()
    {
        _regionManager.RequestNavigate("MainRegion", "DatabaseMaintenanceView");
    }

    private async Task ExecuteRefreshAsync()
    {
        await LoadStatisticsAsync();
        await LoadRecentAuditLogsAsync();
    }

    #endregion

    #region Data Loading

    private async Task LoadStatisticsAsync()
    {
        try
        {
            SetLoading(true);

            // 并行加载统计数据
            var dashboardDataTask = _statisticsService.GetDashboardDataAsync();
            var userCountTask = _userService.GetUsersAsync(new PaginationQuery { PageSize = 1 });

            await Task.WhenAll(dashboardDataTask, userCountTask);

            var dashboardData = await dashboardDataTask;
            var userPagedResult = await userCountTask;

            // 更新属性
            UserCount = userPagedResult.TotalCount;
            TodayMedicalCaseCount = dashboardData.TodayMedicalCaseCount;
            ActiveDoctorCount = dashboardData.ActiveDoctorCount;
            SystemStatus = dashboardData.SystemStatus;
            LastUpdateTime = DateTime.Now;

            // 记录审计日志
            await _auditLogService.LogOperationAsync(new AuditLogDto
            {
                UserId = SessionManager?.CurrentUser?.Id ?? Guid.Empty,
                Action = "ViewDashboard",
                Description = "管理员查看统计数据",
                Timestamp = DateTime.Now
            });

            Logger.Info("统计数据加载成功");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "加载统计数据失败");
            MessageBoxHelper.ShowError("加载统计数据失败，请检查网络连接");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task LoadRecentAuditLogsAsync()
    {
        try
        {
            var logs = await _auditLogService.GetRecentLogsAsync(10);
            RecentAuditLogs = new ObservableCollection<AuditLogDto>(logs);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "加载审计日志失败");
        }
    }

    #endregion

    #region Event Handlers

    private void SubscribeEvents()
    {
        _eventAggregator.GetEvent<UserChangedEvent>().Subscribe(OnUserChanged);
        _eventAggregator.GetEvent<MedicalCaseCreatedEvent>().Subscribe(OnMedicalCaseCreated);
    }

    private async void OnUserChanged(UserDto user)
    {
        await LoadStatisticsAsync();
    }

    private void OnMedicalCaseCreated(MedicalCaseDto medicalCase)
    {
        TodayMedicalCaseCount++;
    }

    #endregion
}
```

---

## 5. 实现AdminHomeView

### Step 1: 创建XAML布局

创建 `Views/AdminHomeView.xaml`：

```xml
<UserControl x:Class="LYBT.Desktop.Admin.Views.AdminHomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Grid Grid.Row="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <StackPanel Grid.Column="0">
                <TextBlock Text="管理员控制台" FontSize="24" FontWeight="Bold"/>
                <TextBlock Text="{Binding LastUpdateTime, StringFormat='最后更新: {0:yyyy-MM-dd HH:mm:ss}'}"
                           FontSize="12" Foreground="Gray" Margin="0,5,0,0"/>
            </StackPanel>

            <Button Grid.Column="1" Content="刷新" Command="{Binding RefreshCommand}"
                    Width="80" Height="32"/>
        </Grid>

        <!-- 统计卡片 -->
        <Grid Grid.Row="1" Margin="0,20,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- 用户总数 -->
            <Border Grid.Column="0" Background="#E3F2FD" CornerRadius="8" Padding="15" Margin="0,0,10,0">
                <StackPanel>
                    <TextBlock Text="用户总数" FontSize="14" Foreground="#1976D2"/>
                    <TextBlock Text="{Binding UserCount}" FontSize="32" FontWeight="Bold" Margin="0,10,0,0"/>
                </StackPanel>
            </Border>

            <!-- 今日病案 -->
            <Border Grid.Column="1" Background="#E8F5E9" CornerRadius="8" Padding="15" Margin="0,0,10,0">
                <StackPanel>
                    <TextBlock Text="今日病案" FontSize="14" Foreground="#388E3C"/>
                    <TextBlock Text="{Binding TodayMedicalCaseCount}" FontSize="32" FontWeight="Bold" Margin="0,10,0,0"/>
                </StackPanel>
            </Border>

            <!-- 活跃医生 -->
            <Border Grid.Column="2" Background="#FFF3E0" CornerRadius="8" Padding="15" Margin="0,0,10,0">
                <StackPanel>
                    <TextBlock Text="活跃医生" FontSize="14" Foreground="#F57C00"/>
                    <TextBlock Text="{Binding ActiveDoctorCount}" FontSize="32" FontWeight="Bold" Margin="0,10,0,0"/>
                </StackPanel>
            </Border>

            <!-- 系统状态 -->
            <Border Grid.Column="3" Background="#F3E5F5" CornerRadius="8" Padding="15">
                <StackPanel>
                    <TextBlock Text="系统状态" FontSize="14" Foreground="#7B1FA2"/>
                    <TextBlock Text="{Binding SystemStatus}" FontSize="18" FontWeight="Bold" Margin="0,10,0,0"/>
                </StackPanel>
            </Border>
        </Grid>

        <!-- 功能区 -->
        <Grid Grid.Row="2" Margin="0,20,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="2*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- 左侧：快捷入口 -->
            <StackPanel Grid.Column="0" Margin="0,0,10,0">
                <TextBlock Text="快捷入口" FontSize="18" FontWeight="Bold" Margin="0,0,0,10"/>

                <Button Content="用户管理" Command="{Binding NavigateToUserManagementCommand}"
                        Height="60" Margin="0,0,0,10" HorizontalContentAlignment="Left" Padding="20,0,0,0"/>

                <Button Content="系统配置" Command="{Binding NavigateToSystemConfigCommand}"
                        Height="60" Margin="0,0,0,10" HorizontalContentAlignment="Left" Padding="20,0,0,0"/>

                <Button Content="数据库维护" Command="{Binding NavigateToDatabaseMaintenanceCommand}"
                        Height="60" HorizontalContentAlignment="Left" Padding="20,0,0,0"/>
            </StackPanel>

            <!-- 右侧：最近审计日志 -->
            <Border Grid.Column="1" BorderBrush="LightGray" BorderThickness="1" CornerRadius="8" Padding="15">
                <StackPanel>
                    <TextBlock Text="最近操作" FontSize="18" FontWeight="Bold" Margin="0,0,0,10"/>

                    <ListBox ItemsSource="{Binding RecentAuditLogs}" BorderThickness="0">
                        <ListBox.ItemTemplate>
                            <DataTemplate>
                                <StackPanel Margin="0,5">
                                    <TextBlock Text="{Binding Action}" FontWeight="Bold"/>
                                    <TextBlock Text="{Binding Description}" FontSize="12" Foreground="Gray"/>
                                    <TextBlock Text="{Binding Timestamp, StringFormat='{}{0:HH:mm:ss}'}"
                                               FontSize="10" Foreground="LightGray"/>
                                </StackPanel>
                            </DataTemplate>
                        </ListBox.ItemTemplate>
                    </ListBox>
                </StackPanel>
            </Border>
        </Grid>

        <!-- Loading遮罩 -->
        <Grid Grid.RowSpan="3" Background="#80000000" Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar IsIndeterminate="True" Width="200" Height="10"/>
                <TextBlock Text="加载中..." Foreground="White" Margin="0,10,0,0" HorizontalAlignment="Center"/>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

### Step 2: 创建Code-Behind

创建 `Views/AdminHomeView.xaml.cs`：

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Admin.Views;

/// <summary>
/// AdminHomeView.xaml 的交互逻辑
/// </summary>
public partial class AdminHomeView : UserControl
{
    public AdminHomeView()
    {
        InitializeComponent();
    }
}
```

---

## 6. 注册模块到Shell

### Step 1: 在App.xaml.cs中加载模块

编辑 `src/Client/Desktop/Shell/LYBT.Desktop.Shell/App.xaml.cs`：

```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    base.ConfigureModuleCatalog(moduleCatalog);

    // 加载Admin模块
    moduleCatalog.AddModule<AdminModule>();

    // 其他模块...
}
```

### Step 2: 配置AC-002角色路由

在 `LoginViewModel.cs` 中实现角色路由：

```csharp
private async Task ExecuteLoginAsync()
{
    try
    {
        SetLoading(true);

        var loginResult = await _authenticationService.LoginAsync(new LoginRequest
        {
            Username = Username,
            Password = Password
        });

        if (loginResult.IsSuccess)
        {
            // 保存用户会话
            SessionManager.Instance.SetCurrentUser(loginResult.User);

            // AC-002: 根据角色导航
            string targetView = loginResult.User.Role switch
            {
                UserRole.Admin => "AdminHomeView",
                UserRole.Doctor => "ClinicalHomeView",
                _ => throw new InvalidOperationException($"未知角色: {loginResult.User.Role}")
            };

            _regionManager.RequestNavigate("MainRegion", targetView);
            Logger.Info($"用户 {loginResult.User.RealName} 登录成功，导航到 {targetView}");
        }
        else
        {
            MessageBoxHelper.ShowError("登录失败：用户名或密码错误");
        }
    }
    catch (Exception ex)
    {
        Logger.Error(ex, "登录失败");
        MessageBoxHelper.ShowError("登录失败，请检查网络连接");
    }
    finally
    {
        SetLoading(false);
    }
}
```

---

## 7. 权限控制集成

### Step 1: 创建权限装饰器（可选）

如果需要方法级权限控制，创建 `RequireAdminAttribute.cs`：

```csharp
using System;

namespace LYBT.Desktop.Admin.Attributes;

/// <summary>
/// 标记方法需要管理员权限
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RequireAdminAttribute : Attribute
{
}
```

### Step 2: 在ViewModel中使用权限检查

```csharp
[RequireAdmin]
private async Task ExecuteDeleteUserAsync(Guid userId)
{
    // 管理员才能删除用户
    var currentUser = SessionManager.Instance.CurrentUser;
    if (currentUser?.Role != UserRole.Admin)
    {
        MessageBoxHelper.ShowWarning("此功能仅管理员可用");
        return;
    }

    // 执行删除操作
    await _userService.DeleteUserAsync(userId);
}
```

---

## 8. 编译与测试

### Step 1: 编译验证

```powershell
# 编译整个解决方案
cd D:\source\repos\LYBTZYZS
dotnet build LYBT.All.sln -c Release --no-restore

# 预期结果：0 errors, 0 warnings
```

### Step 2: 运行时验证

1. **启动WebAPI**：
   ```powershell
   cd src/Server/Services/LYBT.WebAPI
   dotnet run --launch-profile Production
   ```

2. **启动Desktop客户端**：
   ```powershell
   cd src/Client/Desktop/Shell/LYBT.Desktop.Shell
   dotnet run
   ```

3. **测试Admin模块**：
   - 使用管理员账号登录（`admin` / `123456`）
   - 验证自动导航到 `AdminHomeView`
   - 检查统计数据是否正确加载
   - 测试"用户管理"、"系统配置"、"数据库维护"按钮导航
   - 验证审计日志是否记录

### Step 3: 功能测试清单

- [ ] 管理员登录后自动导航到AdminHomeView（AC-002）
- [ ] 统计卡片数据正确显示（用户总数、今日病案、活跃医生、系统状态）
- [ ] 点击"刷新"按钮可重新加载数据
- [ ] 点击"用户管理"按钮可导航到UserManagementView
- [ ] 点击"系统配置"按钮可导航到SystemConfigView
- [ ] 点击"数据库维护"按钮可导航到DatabaseMaintenanceView
- [ ] 最近操作日志正确显示
- [ ] Loading状态正确显示
- [ ] 网络错误时显示友好提示

---

## 9. 常见问题

### Q1: 编译错误 - 找不到Foundation项目引用

**问题**：
```
error CS0246: The type or namespace name 'UnifiedViewModelBase' could not be found
```

**解决方案**：
检查项目引用是否正确添加：

```xml
<ProjectReference Include="..\..\Foundation\LYBT.Desktop.Foundation\LYBT.Desktop.Foundation.csproj" />
```

如果Foundation项目不存在，先创建Foundation项目。

---

### Q2: 运行时错误 - 无法解析IUserService

**问题**：
```
Prism.Ioc.ContainerResolutionException: Unable to resolve type: IUserService
```

**解决方案**：
在 `App.xaml.cs` 的 `RegisterTypes` 方法中注册服务：

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<IUserService, UserService>();
    containerRegistry.RegisterSingleton<IStatisticsService, StatisticsService>();
    containerRegistry.RegisterSingleton<IAuditLogService, AuditLogService>();
}
```

---

### Q3: AdminHomeView未显示

**问题**：登录后没有导航到AdminHomeView。

**解决方案**：
1. 检查模块是否已加载：
   ```csharp
   moduleCatalog.AddModule<AdminModule>();
   ```

2. 检查View是否已注册：
   ```csharp
   containerRegistry.RegisterForNavigation<AdminHomeView, AdminHomeViewModel>();
   ```

3. 检查角色路由逻辑：
   ```csharp
   UserRole.Admin => "AdminHomeView",
   ```

---

### Q4: 统计数据不显示

**问题**：统计卡片显示0或空白。

**解决方案**：
1. 检查WebAPI是否正常运行（`curl https://localhost:5001/api/v1/health`）
2. 检查网络请求日志（NLog）
3. 在 `LoadStatisticsAsync` 方法中添加断点调试
4. 验证Token是否有效（检查 `SessionManager.CurrentUser`）

---

### Q5: 权限装饰器不生效

**问题**：非管理员用户仍可访问管理功能。

**解决方案**：
权限装饰器需要配合AOP框架（如Unity Interception）才能自动生效。简化方案是直接在方法内检查：

```csharp
private async Task ExecuteDeleteUserAsync(Guid userId)
{
    var currentUser = SessionManager.Instance.CurrentUser;
    if (currentUser?.Role != UserRole.Admin)
    {
        MessageBoxHelper.ShowWarning("此功能仅管理员可用");
        return;
    }

    // 执行操作
}
```

---

## 10. 下一步

完成Admin模块开发后，继续以下任务：

1. **实现子页面**：
   - `UserManagementView` - 用户CRUD管理
   - `SystemConfigView` - 系统配置编辑
   - `DatabaseMaintenanceView` - 数据库备份/恢复

2. **补充单元测试**：
   - `AdminHomeViewModelTests.cs`
   - 测试LoadStatisticsAsync逻辑
   - 测试角色路由逻辑

3. **补充文档**：
   - 更新 `docs/index.md` 添加Admin模块文档链接
   - 创建 `docs/api/admin-api.md` 记录Admin相关API端点

---

## 参考资料

- [Admin模块架构设计](../../explanation/architecture/client/admin-module-design.md)
- [Client端架构总览](../../explanation/architecture/client/README.md)
- [业务规则AC-002](../../explanation/business-rules.md#ac-002-角色路由规则)
- [Prism官方文档](https://prismlibrary.com/)
- [WPF MVVM模式](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview)
