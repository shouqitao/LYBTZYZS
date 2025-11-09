# Desktop 端架构设计标准

> **文档版本**: v1.0
> **最后更新**: 2025-10-12
> **适用范围**: LYBT.Desktop.* 所有业务模块

## 📋 目录

- [1. 架构概述](#1-架构概述)
- [2. 三层架构设计](#2-三层架构设计)
- [3. Repository 设计规范](#3-repository-设计规范)
- [4. ViewModel 设计规范](#4-viewmodel-设计规范)
- [5. View 设计规范](#5-view-设计规范)
- [6. 模块设计规范](#6-模块设计规范)
- [7. 服务分层标准](#7-服务分层标准)
- [8. 依赖注入规范](#8-依赖注入规范)
- [9. 命名规范](#9-命名规范)
- [10. 代码示例](#10-代码示例)
- [11. 架构测试](#11-架构测试)
- [12. 常见问题](#12-常见问题)

---

## 1. 架构概述

### 1.1 技术栈

- **UI 框架**: WPF (Windows Presentation Foundation)
- **架构模式**: MVVM (Model-View-ViewModel)
- **模块化框架**: Prism.DryIoc 8.x+
- **依赖注入**: Prism.DryIoc (DryIoc 容器)
- **对象映射**: AutoMapper 13.0+
- **.NET 版本**: .NET 8.0

### 1.2 架构原则

1. **分层清晰**: View-ViewModel-Repository 三层架构，职责明确
2. **依赖方向**: View → ViewModel → Repository → API Client
3. **松耦合**: 面向接口编程，使用依赖注入
4. **模块化**: 按业务功能划分模块，模块间通过事件通信
5. **SSOT 原则**: 单一事实源，避免重复定义

### 1.3 架构层次

```
┌─────────────────────────────────────────────────────┐
│                    Desktop Client                    │
├─────────────────────────────────────────────────────┤
│  View 层 (XAML + Code-Behind)                       │
│  - 负责UI展示和用户交互                              │
│  - 通过 DataBinding 绑定 ViewModel                   │
├─────────────────────────────────────────────────────┤
│  ViewModel 层 (UnifiedViewModelBase)                │
│  - 负责业务逻辑和数据转换                            │
│  - 调用 Repository 获取数据                          │
│  - 使用 AutoMapper 进行 DTO ↔ UI Model 转换         │
├─────────────────────────────────────────────────────┤
│  Repository 层 (IXxxRepository)                     │
│  - 负责数据访问和API调用                             │
│  - 返回裸类型 (不包装 ServiceResult)                 │
│  - 异常向上抛出由 ViewModel 处理                     │
├─────────────────────────────────────────────────────┤
│  API Client 层 (Refit Interface)                    │
│  - HTTP API 调用接口                                │
│  - 由 Shell 统一注册                                │
└─────────────────────────────────────────────────────┘
```

---

## 2. 三层架构设计

### 2.1 View 层

**职责**:
- 负责 UI 展示和用户交互
- 通过 DataBinding 绑定 ViewModel 属性
- 不包含业务逻辑（仅限 UI 逻辑）

**约束**:
- ✅ 使用 `{Binding}` 绑定 ViewModel 属性
- ✅ 使用 `{x:Bind}` 优化性能（可选）
- ✅ 代码隐藏仅包含 UI 逻辑（如动画、焦点控制）
- ❌ 禁止在 Code-Behind 中调用 Repository
- ❌ 禁止在 Code-Behind 中包含业务逻辑

### 2.2 ViewModel 层

**职责**:
- 负责业务逻辑和数据转换
- 调用 Repository 获取数据
- 使用 AutoMapper 进行 DTO ↔ UI Model 转换
- 处理异常并显示用户友好的错误信息

**约束**:
- ✅ 继承 `UnifiedViewModelBase` 或 `UnifiedListViewModelBase<T>`
- ✅ 使用构造函数注入依赖
- ✅ 使用 AutoMapper 进行对象映射
- ✅ 使用 `INotificationService` 显示消息
- ❌ 禁止直接调用 API（必须通过 Repository）
- ❌ 禁止在 ViewModel 中创建 UI 元素

### 2.3 Repository 层

**职责**:
- 负责数据访问和 API 调用
- 封装 API 调用细节
- 提供业务友好的数据访问接口

**约束**:
- ✅ 返回裸类型（如 `Task<List<UserDto>>`）
- ✅ 异常向上抛出由 ViewModel 处理
- ✅ 使用 Refit 接口调用 API
- ❌ 禁止返回 `ServiceResult<T>`（Server 端专用）
- ❌ 禁止在 Repository 中显示 UI 消息

---

## 3. Repository 设计规范

### 3.1 接口定义

**位置**: `{模块}/Interfaces/IXxxRepository.cs`

**命名规范**:
- 接口名称: `I{业务实体}Repository`
- 方法名称: 动词 + 名词（如 `GetUsersAsync`, `AddUserAsync`）

**示例**:
```csharp
namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户数据访问接口
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<List<UserDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<UserDto?> GetByIdAsync(int id);

    /// <summary>
    /// 添加用户
    /// </summary>
    Task<UserDto> AddAsync(CreateUserDto dto);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task UpdateAsync(int id, UpdateUserDto dto);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task DeleteAsync(int id);
}
```

### 3.2 实现类

**位置**: `{模块}/Repositories/XxxRepository.cs`

**命名规范**:
- 类名: `{业务实体}Repository`
- 继承: 实现对应的 `IXxxRepository` 接口

**示例**:
```csharp
namespace LYBT.Desktop.Users.Repositories;

/// <summary>
/// 用户数据访问实现
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IUserApi _userApi;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IUserApi userApi,
        ILogger<UserRepository> logger)
    {
        _userApi = userApi;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("正在获取所有用户...");
            return await _userApi.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有用户失败");
            throw; // 异常向上抛出
        }
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在获取用户 {UserId}...", id);
            return await _userApi.GetUserByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 失败", id);
            throw;
        }
    }

    // 其他方法实现...
}
```

### 3.3 注册位置

**位置**: `{模块}/{模块名}Module.cs` 的 `RegisterTypes` 方法

**注册方式**: `RegisterSingleton<IXxxRepository, XxxRepository>()`

**示例**:
```csharp
public class UsersModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

        // 注册 ViewModel
        containerRegistry.Register<UserManagementViewModel>();
        containerRegistry.Register<UserDetailViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.UserManagementView>();
        containerRegistry.RegisterForNavigation<Views.UserDetailView>();
    }
}
```

### 3.4 返回值约定

**✅ 正确示例**:
```csharp
// 返回裸类型
Task<List<UserDto>> GetAllAsync();
Task<UserDto?> GetByIdAsync(int id);
Task<UserDto> AddAsync(CreateUserDto dto);
Task UpdateAsync(int id, UpdateUserDto dto);
Task DeleteAsync(int id);
```

**❌ 错误示例**:
```csharp
// ❌ 不要返回 ServiceResult（Server 端专用）
Task<ServiceResult<List<UserDto>>> GetAllAsync();
Task<ServiceResult<UserDto>> GetByIdAsync(int id);
```

### 3.5 异常处理

**原则**: Repository 不处理异常，仅记录日志后向上抛出

**示例**:
```csharp
public async Task<List<UserDto>> GetAllAsync()
{
    try
    {
        _logger.LogInformation("正在获取所有用户...");
        return await _userApi.GetAllUsersAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取所有用户失败");
        throw; // 异常向上抛出，由 ViewModel 处理
    }
}
```

---

## 4. ViewModel 设计规范

### 4.1 基类选择

| 场景 | 基类 | 说明 |
|------|------|------|
| 普通页面 | `UnifiedViewModelBase` | 基础 ViewModel 功能（INotifyPropertyChanged, Busy 状态等） |
| 列表管理页面 | `UnifiedListViewModelBase<T>` | 包含列表加载、分页、搜索、刷新功能 |
| 导航页面 | `NavigationViewModelBase` | 支持 Prism 导航（OnNavigatedTo/OnNavigatedFrom） |
| 对话框页面 | `DialogViewModelBase` | 支持 Prism 对话框（IDialogAware） |

### 4.2 构造函数注入

**示例**:
```csharp
namespace LYBT.Desktop.Users.ViewModels;

public class UserManagementViewModel : UnifiedListViewModelBase<UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserManagementViewModel> _logger;

    public UserManagementViewModel(
        IUserRepository userRepository,
        INotificationService notificationService,
        IDialogService dialogService,
        IMapper mapper,
        ILogger<UserManagementViewModel> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _mapper = mapper;
        _logger = logger;

        InitializeCommands();
    }

    // ViewModel 实现...
}
```

### 4.3 命令定义

**使用**: `DelegateCommand` 或 `DelegateCommand<T>`

**示例**:
```csharp
public class UserManagementViewModel : UnifiedViewModelBase
{
    public DelegateCommand LoadUsersCommand { get; private set; }
    public DelegateCommand AddUserCommand { get; private set; }
    public DelegateCommand<int> EditUserCommand { get; private set; }
    public DelegateCommand<int> DeleteUserCommand { get; private set; }

    private void InitializeCommands()
    {
        LoadUsersCommand = new DelegateCommand(async () => await LoadUsersAsync());
        AddUserCommand = new DelegateCommand(async () => await AddUserAsync());
        EditUserCommand = new DelegateCommand<int>(async (id) => await EditUserAsync(id));
        DeleteUserCommand = new DelegateCommand<int>(async (id) => await DeleteUserAsync(id));
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "正在加载用户列表...";

            var users = await _userRepository.GetAllAsync();
            Items = new ObservableCollection<UserDto>(users);

            _notificationService.ShowSuccess("用户列表加载成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户列表失败");
            _notificationService.ShowError($"加载用户列表失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 4.4 异常处理模式

**标准模式**:
```csharp
private async Task ExecuteActionAsync()
{
    try
    {
        // 1. 设置忙状态
        IsBusy = true;
        BusyMessage = "正在执行操作...";

        // 2. 调用 Repository
        var result = await _repository.GetDataAsync();

        // 3. 更新 UI
        Items = new ObservableCollection<Item>(result);

        // 4. 显示成功消息
        _notificationService.ShowSuccess("操作成功");
    }
    catch (Exception ex)
    {
        // 5. 记录日志
        _logger.LogError(ex, "操作失败");

        // 6. 显示用户友好的错误消息
        _notificationService.ShowError($"操作失败: {ex.Message}");
    }
    finally
    {
        // 7. 清除忙状态
        IsBusy = false;
    }
}
```

### 4.5 AutoMapper 使用

**配置**: 在 `{模块}/Mappings/AutoMapperProfile.cs` 中定义映射

**示例**:
```csharp
namespace LYBT.Desktop.Users.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // DTO → UI Model
        CreateMap<UserDto, UserItem>();

        // UI Model → DTO
        CreateMap<UserItem, UpdateUserDto>();
    }
}
```

**使用**:
```csharp
// ViewModel 中使用
var userItems = _mapper.Map<List<UserItem>>(users);
var updateDto = _mapper.Map<UpdateUserDto>(userItem);
```

---

## 5. View 设计规范

### 5.1 XAML 设计原则

1. **数据绑定**: 使用 `{Binding}` 绑定 ViewModel 属性
2. **命令绑定**: 使用 `{Binding XxxCommand}` 绑定命令
3. **样式统一**: 使用 `MaterialDesignThemes` 统一样式
4. **响应式布局**: 使用 Grid/DockPanel 实现响应式布局

### 5.2 XAML 示例

```xml
<UserControl x:Class="LYBT.Desktop.Users.Views.UserManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <!-- 工具栏 -->
        <DockPanel DockPanel.Dock="Top" Margin="16">
            <Button Content="添加用户"
                    Command="{Binding AddUserCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"/>
            <Button Content="刷新"
                    Command="{Binding LoadUsersCommand}"
                    Style="{StaticResource MaterialDesignFlatButton}"/>
        </DockPanel>

        <!-- 列表 -->
        <DataGrid ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="用户名" Binding="{Binding Username}"/>
                <DataGridTextColumn Header="姓名" Binding="{Binding FullName}"/>
                <DataGridTextColumn Header="角色" Binding="{Binding RoleName}"/>
                <DataGridTemplateColumn Header="操作">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="编辑"
                                        Command="{Binding DataContext.EditUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding Id}"/>
                                <Button Content="删除"
                                        Command="{Binding DataContext.DeleteUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding Id}"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 忙状态指示器 -->
        <md:Card Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}" IsIndeterminate="True"/>
                <TextBlock Text="{Binding BusyMessage}" Margin="0,16,0,0"/>
            </StackPanel>
        </md:Card>
    </Grid>
</UserControl>
```

### 5.3 Code-Behind 约束

**✅ 允许的场景**:
```csharp
public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();
    }

    // ✅ UI 逻辑：焦点控制
    private void SearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
    }

    // ✅ UI 逻辑：动画
    private void ListItem_MouseEnter(object sender, MouseEventArgs e)
    {
        // 播放动画
    }
}
```

**❌ 禁止的场景**:
```csharp
public partial class UserManagementView : UserControl
{
    // ❌ 禁止：直接调用 Repository
    private readonly IUserRepository _userRepository;

    // ❌ 禁止：包含业务逻辑
    private async void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var users = await _userRepository.GetAllAsync();
        UserList.ItemsSource = users;
    }
}
```

---

## 6. 模块设计规范

### 6.1 模块目录结构

```
LYBT.Desktop.{模块名}/
├── Interfaces/               # 接口定义
│   └── IXxxRepository.cs
├── Repositories/             # Repository 实现
│   └── XxxRepository.cs
├── ViewModels/               # ViewModel
│   ├── XxxManagementViewModel.cs
│   └── XxxDetailViewModel.cs
├── Views/                    # View
│   ├── XxxManagementView.xaml
│   └── XxxDetailView.xaml
├── Models/                   # UI 模型
│   ├── XxxItem.cs
│   └── XxxInfo.cs
├── Mappings/                 # AutoMapper 配置
│   └── XxxMappingProfile.cs
├── Events/                   # 模块事件
│   └── XxxChangedEvent.cs
└── {模块名}Module.cs          # 模块入口
```

### 6.2 模块类实现

**示例**:
```csharp
namespace LYBT.Desktop.Users;

[Module(ModuleName = nameof(UsersModule))]
[ModuleDependency("AuthenticationModule")] // 依赖认证模块
public class UsersModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑（可选）
        var logger = containerProvider.Resolve<ILogger<UsersModule>>();
        logger.LogInformation("用户管理模块已初始化");
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

        // 注册 ViewModel
        containerRegistry.Register<UserManagementViewModel>();
        containerRegistry.Register<UserDetailViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.UserManagementView>();
        containerRegistry.RegisterForNavigation<Views.UserDetailView>();

        // 注册对话框（可选）
        containerRegistry.RegisterDialog<Views.UserEditorDialog, ViewModels.UserEditorDialogViewModel>();
    }
}
```

### 6.3 模块依赖配置

**原则**:
- 使用 `[ModuleDependency]` 特性声明依赖关系
- 避免循环依赖
- 模块依赖应尽量少

**示例**:
```csharp
// 认证模块：无依赖
[Module(ModuleName = nameof(AuthenticationModule))]
public class AuthenticationModule : IModule { }

// 用户模块：依赖认证
[Module(ModuleName = nameof(UsersModule))]
[ModuleDependency("AuthenticationModule")]
public class UsersModule : IModule { }

// 患者模块：依赖认证和用户
[Module(ModuleName = nameof(PatientsModule))]
[ModuleDependency("AuthenticationModule")]
[ModuleDependency("UsersModule")]
public class PatientsModule : IModule { }
```

---

## 7. 服务分层标准

### 7.1 服务分类

根据 ADR-002 架构决策，Desktop 端服务分为三层：

| 层级 | 位置 | 注册位置 | 职责 |
|------|------|---------|------|
| **Foundation 层** | `LYBT.Desktop.Infrastructure` | Shell 统一注册 | 基础设施服务（导航、对话框、会话管理等） |
| **Infrastructure 层** | `LYBT.Desktop.Infrastructure` | Shell 统一注册 | 横切关注点（日志、缓存、配置等） |
| **Repository 层** | `LYBT.Desktop.{模块}.Repositories` | 模块自行注册 | 数据访问层（API 调用、数据缓存） |

### 7.2 Foundation 层服务

**定义**: 应用程序基础功能，所有模块都依赖

**示例**:
- `INavigationService`: 导航服务
- `IDialogService`: 对话框服务
- `ISessionManager`: 会话管理
- `IThemeService`: 主题服务
- `INotificationService`: 通知服务

**注册位置**: `LYBT.Desktop.Shell/App.xaml.cs` 或 `ShellModule.cs`

```csharp
// Shell 统一注册 Foundation 服务
containerRegistry.RegisterSingleton<INavigationService, EnhancedNavigationService>();
containerRegistry.RegisterSingleton<IDialogService, PrismDialogService>();
containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
containerRegistry.RegisterSingleton<INotificationService, NotificationService>();
```

### 7.3 Infrastructure 层服务

**定义**: 横切关注点，非业务核心但通用的功能

**示例**:
- `ILogger<T>`: 日志服务（Serilog）
- `ICacheService`: 缓存服务
- `IConfigurationService`: 配置服务
- `IMapper`: AutoMapper 映射

**注册位置**: `LYBT.Desktop.Shell/App.xaml.cs` 或 `InfrastructureModule.cs`

```csharp
// Shell 统一注册 Infrastructure 服务
containerRegistry.RegisterSingleton<ILogger<T>, Logger<T>>();
containerRegistry.RegisterSingleton<ICacheService, MemoryCacheService>();
containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();
containerRegistry.RegisterSingleton<IMapper>(provider =>
{
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
    });
    return config.CreateMapper();
});
```

### 7.4 Repository 层服务

**定义**: 数据访问层，封装 API 调用逻辑

**示例**:
- `IUserRepository`: 用户数据访问
- `IPatientRepository`: 患者数据访问
- `IPrescriptionRepository`: 处方数据访问

**注册位置**: 各业务模块的 `{模块名}Module.cs`

```csharp
// 模块自行注册 Repository
public class UsersModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();
        // ...
    }
}
```

---

## 8. 依赖注入规范

### 8.1 注入方式

**✅ 唯一推荐方式**: 构造函数注入

```csharp
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserManagementViewModel> _logger;

    // ✅ 构造函数注入
    public UserManagementViewModel(
        IUserRepository userRepository,
        INotificationService notificationService,
        ILogger<UserManagementViewModel> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _logger = logger;
    }
}
```

**❌ 禁止方式**:
```csharp
// ❌ 禁止：属性注入
[Dependency]
public IUserRepository UserRepository { get; set; }

// ❌ 禁止：方法注入
public void SetRepository(IUserRepository repository) { }

// ❌ 禁止：服务定位器模式
var repository = Container.Resolve<IUserRepository>();
```

### 8.2 生命周期管理

| 方法 | 生命周期 | 适用场景 |
|------|---------|---------|
| `RegisterSingleton<TInterface, TImplementation>()` | 单例 | Repository, Foundation 服务, Infrastructure 服务 |
| `Register<TViewModel>()` | 瞬时 | ViewModel |
| `RegisterForNavigation<TView>()` | 瞬时 | View（导航） |
| `RegisterDialog<TView, TViewModel>()` | 瞬时 | Dialog（对话框） |

**示例**:
```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 单例：Repository（每个模块只需要一个实例）
    containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

    // 瞬时：ViewModel（每次导航创建新实例）
    containerRegistry.Register<UserManagementViewModel>();
    containerRegistry.Register<UserDetailViewModel>();

    // 导航：View
    containerRegistry.RegisterForNavigation<Views.UserManagementView>();

    // 对话框：View + ViewModel
    containerRegistry.RegisterDialog<Views.UserEditorDialog, ViewModels.UserEditorDialogViewModel>();
}
```

---

## 9. 命名规范

### 9.1 命名空间

| 类型 | 命名空间 | 示例 |
|------|---------|------|
| 模块根 | `LYBT.Desktop.{模块名}` | `LYBT.Desktop.Users` |
| Repository 接口 | `LYBT.Desktop.{模块名}.Interfaces` | `LYBT.Desktop.Users.Interfaces` |
| Repository 实现 | `LYBT.Desktop.{模块名}.Repositories` | `LYBT.Desktop.Users.Repositories` |
| ViewModel | `LYBT.Desktop.{模块名}.ViewModels` | `LYBT.Desktop.Users.ViewModels` |
| View | `LYBT.Desktop.{模块名}.Views` | `LYBT.Desktop.Users.Views` |
| UI Model | `LYBT.Desktop.{模块名}.Models` | `LYBT.Desktop.Users.Models` |
| AutoMapper | `LYBT.Desktop.{模块名}.Mappings` | `LYBT.Desktop.Users.Mappings` |
| 事件 | `LYBT.Desktop.{模块名}.Events` | `LYBT.Desktop.Users.Events` |

### 9.2 类命名

| 类型 | 命名规范 | 示例 |
|------|---------|------|
| Repository 接口 | `I{实体}Repository` | `IUserRepository` |
| Repository 实现 | `{实体}Repository` | `UserRepository` |
| ViewModel | `{功能}ViewModel` | `UserManagementViewModel`, `UserDetailViewModel` |
| View | `{功能}View` | `UserManagementView`, `UserDetailView` |
| Dialog | `{功能}Dialog` | `UserEditorDialog` |
| UI Model | `{实体}Item`, `{实体}Info` | `UserItem`, `UserInfo` |
| AutoMapper Profile | `{模块}MappingProfile` | `UserMappingProfile` |
| Event | `{实体}{动作}Event` | `UserCreatedEvent`, `UserUpdatedEvent` |

### 9.3 成员命名

| 类型 | 命名规范 | 示例 |
|------|---------|------|
| 私有字段 | `_camelCase` | `_userRepository`, `_logger` |
| 属性 | `PascalCase` | `IsEnabled`, `UserName` |
| 方法 | `PascalCase` | `GetAllAsync`, `AddUserAsync` |
| 命令 | `{动作}Command` | `LoadUsersCommand`, `AddUserCommand` |
| 异步方法 | `{动作}Async` | `LoadUsersAsync`, `SaveDataAsync` |

---

## 10. 代码示例

### 10.1 完整模块示例

以 `Users` 模块为例，展示完整的三层架构实现。

#### 10.1.1 Repository 接口

**文件**: `LYBT.Desktop.Users/Interfaces/IUserRepository.cs`

```csharp
using LYBT.Shared.Dtos.User;

namespace LYBT.Desktop.Users.Interfaces;

/// <summary>
/// 用户数据访问接口
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 获取所有用户
    /// </summary>
    Task<List<UserDto>> GetAllAsync();

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<UserDto?> GetByIdAsync(int id);

    /// <summary>
    /// 添加用户
    /// </summary>
    Task<UserDto> AddAsync(CreateUserDto dto);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task UpdateAsync(int id, UpdateUserDto dto);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 搜索用户
    /// </summary>
    Task<List<UserDto>> SearchAsync(string keyword);
}
```

#### 10.1.2 Repository 实现

**文件**: `LYBT.Desktop.Users/Repositories/UserRepository.cs`

```csharp
using LYBT.Desktop.Users.Interfaces;
using LYBT.Shared.ApiInterfaces;
using LYBT.Shared.Dtos.User;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Users.Repositories;

/// <summary>
/// 用户数据访问实现
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IUserApi _userApi;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IUserApi userApi,
        ILogger<UserRepository> logger)
    {
        _userApi = userApi;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("正在获取所有用户...");
            return await _userApi.GetAllUsersAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有用户失败");
            throw;
        }
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在获取用户 {UserId}...", id);
            return await _userApi.GetUserByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户 {UserId} 失败", id);
            throw;
        }
    }

    public async Task<UserDto> AddAsync(CreateUserDto dto)
    {
        try
        {
            _logger.LogInformation("正在添加用户 {Username}...", dto.Username);
            return await _userApi.CreateUserAsync(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加用户失败");
            throw;
        }
    }

    public async Task UpdateAsync(int id, UpdateUserDto dto)
    {
        try
        {
            _logger.LogInformation("正在更新用户 {UserId}...", id);
            await _userApi.UpdateUserAsync(id, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户 {UserId} 失败", id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            _logger.LogInformation("正在删除用户 {UserId}...", id);
            await _userApi.DeleteUserAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户 {UserId} 失败", id);
            throw;
        }
    }

    public async Task<List<UserDto>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogInformation("正在搜索用户，关键字: {Keyword}...", keyword);
            return await _userApi.SearchUsersAsync(keyword);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索用户失败");
            throw;
        }
    }
}
```

#### 10.1.3 UI Model

**文件**: `LYBT.Desktop.Users/Models/UserItem.cs`

```csharp
namespace LYBT.Desktop.Users.Models;

/// <summary>
/// 用户列表项（UI 模型）
/// </summary>
public class UserItem
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### 10.1.4 AutoMapper Profile

**文件**: `LYBT.Desktop.Users/Mappings/UserMappingProfile.cs`

```csharp
using AutoMapper;
using LYBT.Desktop.Users.Models;
using LYBT.Shared.Dtos.User;

namespace LYBT.Desktop.Users.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // DTO → UI Model
        CreateMap<UserDto, UserItem>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

        // UI Model → Update DTO
        CreateMap<UserItem, UpdateUserDto>();
    }
}
```

#### 10.1.5 ViewModel

**文件**: `LYBT.Desktop.Users/ViewModels/UserManagementViewModel.cs`

```csharp
using AutoMapper;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Desktop.Presentation.Interfaces;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Models;
using LYBT.Shared.Dtos.User;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Services.Dialogs;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.Users.ViewModels;

public class UserManagementViewModel : UnifiedListViewModelBase<UserItem>
{
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserManagementViewModel> _logger;

    public UserManagementViewModel(
        IUserRepository userRepository,
        INotificationService notificationService,
        IDialogService dialogService,
        IMapper mapper,
        ILogger<UserManagementViewModel> logger)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _mapper = mapper;
        _logger = logger;

        InitializeCommands();
    }

    #region 命令

    public DelegateCommand AddUserCommand { get; private set; }
    public DelegateCommand<UserItem> EditUserCommand { get; private set; }
    public DelegateCommand<UserItem> DeleteUserCommand { get; private set; }

    private void InitializeCommands()
    {
        AddUserCommand = new DelegateCommand(async () => await AddUserAsync());
        EditUserCommand = new DelegateCommand<UserItem>(async (user) => await EditUserAsync(user));
        DeleteUserCommand = new DelegateCommand<UserItem>(async (user) => await DeleteUserAsync(user));
    }

    #endregion

    #region 重写基类方法

    protected override async Task<List<UserItem>> LoadDataAsync()
    {
        try
        {
            _logger.LogInformation("正在加载用户列表...");
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<List<UserItem>>(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户列表失败");
            _notificationService.ShowError($"加载用户列表失败: {ex.Message}");
            return new List<UserItem>();
        }
    }

    #endregion

    #region 业务方法

    private async Task AddUserAsync()
    {
        try
        {
            var parameters = new DialogParameters();
            _dialogService.ShowDialog("UserEditorDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    var createDto = result.Parameters.GetValue<CreateUserDto>("User");
                    await _userRepository.AddAsync(createDto);
                    _notificationService.ShowSuccess("用户添加成功");
                    await RefreshAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加用户失败");
            _notificationService.ShowError($"添加用户失败: {ex.Message}");
        }
    }

    private async Task EditUserAsync(UserItem user)
    {
        try
        {
            var parameters = new DialogParameters
            {
                { "UserId", user.Id }
            };

            _dialogService.ShowDialog("UserEditorDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    var updateDto = result.Parameters.GetValue<UpdateUserDto>("User");
                    await _userRepository.UpdateAsync(user.Id, updateDto);
                    _notificationService.ShowSuccess("用户更新成功");
                    await RefreshAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败");
            _notificationService.ShowError($"更新用户失败: {ex.Message}");
        }
    }

    private async Task DeleteUserAsync(UserItem user)
    {
        try
        {
            var parameters = new DialogParameters
            {
                { "Title", "确认删除" },
                { "Message", $"确定要删除用户 {user.FullName} 吗？" }
            };

            _dialogService.ShowDialog("ConfirmationDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    await _userRepository.DeleteAsync(user.Id);
                    _notificationService.ShowSuccess("用户删除成功");
                    await RefreshAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户失败");
            _notificationService.ShowError($"删除用户失败: {ex.Message}");
        }
    }

    #endregion
}
```

#### 10.1.6 View

**文件**: `LYBT.Desktop.Users/Views/UserManagementView.xaml`

```xml
<UserControl x:Class="LYBT.Desktop.Users.Views.UserManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/">

    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,16">
            <Button Content="添加用户"
                    Command="{Binding AddUserCommand}"
                    Style="{StaticResource MaterialDesignRaisedButton}"
                    Margin="0,0,8,0"/>
            <Button Content="刷新"
                    Command="{Binding LoadDataCommand}"
                    Style="{StaticResource MaterialDesignFlatButton}"/>
        </StackPanel>

        <!-- 列表 -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="用户名" Binding="{Binding Username}" Width="150"/>
                <DataGridTextColumn Header="姓名" Binding="{Binding FullName}" Width="200"/>
                <DataGridTextColumn Header="角色" Binding="{Binding RoleName}" Width="150"/>
                <DataGridCheckBoxColumn Header="启用" Binding="{Binding IsActive}" Width="80"/>
                <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt, StringFormat='yyyy-MM-dd'}" Width="120"/>
                <DataGridTemplateColumn Header="操作" Width="150">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal">
                                <Button Content="编辑"
                                        Command="{Binding DataContext.EditUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource MaterialDesignFlatButton}"
                                        Margin="0,0,4,0"/>
                                <Button Content="删除"
                                        Command="{Binding DataContext.DeleteUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Style="{StaticResource MaterialDesignFlatButton}"
                                        Foreground="Red"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 忙状态遮罩 -->
        <Border Grid.Row="1"
                Background="#80000000"
                Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}"
                             IsIndeterminate="True"
                             Width="64"
                             Height="64"/>
                <TextBlock Text="{Binding BusyMessage}"
                           Foreground="White"
                           Margin="0,16,0,0"
                           FontSize="14"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

#### 10.1.7 Module 类

**文件**: `LYBT.Desktop.Users/UsersModule.cs`

```csharp
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.Repositories;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Users;

/// <summary>
/// 用户管理模块
/// </summary>
[Module(ModuleName = nameof(UsersModule))]
[ModuleDependency("AuthenticationModule")] // 用户模块依赖认证模块
public class UsersModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化逻辑（可选）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ADR-002 架构标准：
        // - Infrastructure Service (Foundation/Infrastructure) 由 Shell 统一注册
        // - Repository (数据访问层) 由各业务模块自行注册
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

        // 注册 ViewModel
        containerRegistry.Register<ViewModels.UserManagementViewModel>();
        containerRegistry.Register<ViewModels.UserDetailViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<Views.UserManagementView>();
        containerRegistry.RegisterForNavigation<Views.UserDetailView>();

        // 注册对话框
        containerRegistry.RegisterDialog<Views.UserEditorDialog, ViewModels.UserEditorDialogViewModel>();
    }
}
```

---

## 11. 架构测试

### 11.1 架构测试目的

使用 NetArchTest.Rules 编写架构测试，确保代码遵循架构约束：

- Desktop 层不依赖 Server 层
- Desktop 层不包含 DTO 类
- Desktop 层不直接使用 Entity 类
- ViewModel 必须继承标准基类
- Repository 必须在模块中注册

### 11.2 架构测试示例

**文件**: `tests/Architecture/DesktopLayerArchTests.cs`

```csharp
using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace LYBT.ArchTests;

/// <summary>
/// Desktop层架构约束测试
/// </summary>
public class DesktopLayerArchTests
{
    private static readonly Assembly[] DesktopAssemblies =
    [
        Assembly.Load("LYBT.Desktop.Infrastructure"),
        Assembly.Load("LYBT.Desktop.Models"),
        Assembly.Load("LYBT.Desktop.Shell"),
        Assembly.Load("LYBT.Desktop.Auth"),
        Assembly.Load("LYBT.Desktop.Users"),
        Assembly.Load("LYBT.Desktop.Patients"),
        Assembly.Load("LYBT.Desktop.MedicalCase"),
        Assembly.Load("LYBT.Desktop.Consultation"),
        Assembly.Load("LYBT.Desktop.Prescriptions"),
        Assembly.Load("LYBT.Desktop.Herbs"),
        Assembly.Load("LYBT.Desktop.Formula"),
        Assembly.Load("LYBT.Desktop.AdminWorkstation"),
        Assembly.Load("LYBT.Desktop.ClinicalWorkstation")
    ];

    /// <summary>
    /// Desktop层不得依赖Server层
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Depend_On_Server_Layers()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .Should()
            .NotHaveDependencyOnAll("LYBT.Infrastructure", "LYBT.Entities")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层违规依赖Server层: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// Desktop层不得包含DTO类
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Contain_DTO_Classes()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Desktop")
            .Should()
            .NotHaveNameEndingWith("Dto")
            .And()
            .NotHaveNameEndingWith("DTO")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层包含DTO类（应使用Item/ViewState/Info）: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
    }

    /// <summary>
    /// Desktop层不应直接使用Entity类
    /// </summary>
    [Fact]
    public void Desktop_Should_Not_Use_Entity_Classes()
    {
        var result = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespaceContaining("Desktop")
            .Should()
            .NotHaveDependencyOn("LYBT.Entities")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Desktop层直接使用了Entity类: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    /// <summary>
    /// Desktop层ViewModels必须继承自正确基类
    /// </summary>
    [Fact]
    public void Desktop_ViewModels_Should_Inherit_From_Base_Classes()
    {
        var allowedBaseClasses = new[]
        {
            "UnifiedViewModelBase",
            "UnifiedListViewModelBase`1",
            "ModernViewModelBase",
            "ModernManagementViewModel",
            "NavigationViewModelBase",
            "DialogViewModelBase"
        };

        var viewModelTypes = Types.InAssemblies(DesktopAssemblies)
            .That()
            .ResideInNamespace("ViewModels")
            .And()
            .HaveNameEndingWith("ViewModel")
            .And()
            .AreClasses()
            .And()
            .ArePublic()
            .GetTypes()
            .Where(t => !t.Name.Contains("Design") && !t.Name.Contains("Mock"))
            .ToList();

        foreach (var vmType in viewModelTypes)
        {
            var currentType = vmType.BaseType;
            var hasValidBase = false;

            while (currentType != null && currentType != typeof(object))
            {
                var baseName = currentType.IsGenericType
                    ? currentType.GetGenericTypeDefinition().Name
                    : currentType.Name;

                if (allowedBaseClasses.Contains(baseName))
                {
                    hasValidBase = true;
                    break;
                }
                currentType = currentType.BaseType;
            }

            Assert.True(
                hasValidBase,
                $"ViewModel {vmType.FullName} 未继承自标准基类");
        }
    }

    /// <summary>
    /// 验证所有 Repository 都在对应模块中注册
    /// </summary>
    [Fact]
    public void All_Repositories_Should_Be_Registered_In_Modules()
    {
        var moduleAssemblies = new[]
        {
            Assembly.Load("LYBT.Desktop.Auth"),
            Assembly.Load("LYBT.Desktop.Users"),
            Assembly.Load("LYBT.Desktop.Patients"),
            Assembly.Load("LYBT.Desktop.MedicalCase"),
            Assembly.Load("LYBT.Desktop.Consultation"),
            Assembly.Load("LYBT.Desktop.Prescriptions"),
            Assembly.Load("LYBT.Desktop.Herbs"),
            Assembly.Load("LYBT.Desktop.Formula")
        };

        var repositoriesWithoutRegistration = new List<string>();

        foreach (var assembly in moduleAssemblies)
        {
            var repositoryInterfaces = assembly.GetTypes()
                .Where(t => t.IsInterface && t.Name.EndsWith("Repository"))
                .ToList();

            if (!repositoryInterfaces.Any())
                continue;

            var moduleType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.EndsWith("Module") &&
                                   t.GetInterfaces().Any(i => i.Name == "IModule"));

            if (moduleType == null)
            {
                repositoriesWithoutRegistration.Add($"{assembly.GetName().Name}: 未找到 Module 类");
                continue;
            }

            var registerMethod = moduleType.GetMethod("RegisterTypes");
            if (registerMethod == null)
            {
                repositoriesWithoutRegistration.Add($"{assembly.GetName().Name}: Module 类未实现 RegisterTypes 方法");
                continue;
            }
        }

        Assert.Empty(repositoriesWithoutRegistration);
    }
}
```

---

## 12. 常见问题

### 12.1 为什么 Repository 不返回 ServiceResult？

**问题**: Server 端使用 `ServiceResult<T>` 包装返回值，为什么 Desktop 端不使用？

**答案**:
- **Server 端**: 需要返回统一的 HTTP 响应格式（包含状态码、错误消息等），所以使用 `ServiceResult<T>`
- **Desktop 端**: Repository 仅负责数据访问，不需要关心 HTTP 响应格式，异常由 ViewModel 处理并显示给用户

**示例对比**:
```csharp
// ✅ Desktop Repository - 返回裸类型
Task<List<UserDto>> GetAllAsync();

// ✅ Server Service - 返回 ServiceResult
Task<ServiceResult<List<UserDto>>> GetAllUsersAsync();
```

### 12.2 为什么 Repository 在模块中注册而不是 Shell？

**问题**: 为什么 Repository 要在业务模块的 `*Module.cs` 中注册，而不是统一在 Shell 中注册？

**答案**:
- **职责分离**: Repository 是业务模块的数据访问层，属于业务逻辑的一部分，应由模块自行管理
- **模块化**: 每个模块负责自己的依赖注入，降低模块间耦合
- **扩展性**: 新增模块时，只需在模块内注册 Repository，无需修改 Shell

**参考**: ADR-002 架构决策记录

### 12.3 为什么 ViewModel 不直接调用 API？

**问题**: 为什么 ViewModel 不能直接注入 `IUserApi` 调用 API，而必须通过 Repository？

**答案**:
- **关注点分离**: ViewModel 关注业务逻辑和 UI 交互，Repository 关注数据访问细节
- **可测试性**: 通过 Repository 接口，ViewModel 可以轻松 Mock 数据进行单元测试
- **可维护性**: API 调用逻辑集中在 Repository，便于统一处理缓存、重试、异常等

**错误示例**:
```csharp
// ❌ 错误：ViewModel 直接调用 API
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IUserApi _userApi; // ❌ 不应直接注入 API

    public async Task LoadUsersAsync()
    {
        var users = await _userApi.GetAllUsersAsync(); // ❌ 不应直接调用 API
    }
}
```

**正确示例**:
```csharp
// ✅ 正确：ViewModel 通过 Repository 调用
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IUserRepository _userRepository; // ✅ 注入 Repository

    public async Task LoadUsersAsync()
    {
        var users = await _userRepository.GetAllAsync(); // ✅ 通过 Repository
    }
}
```

### 12.4 如何处理跨模块通信？

**问题**: 模块 A 需要通知模块 B 数据发生变化，如何实现？

**答案**: 使用 Prism EventAggregator（发布-订阅模式）

**示例**:

1. 定义事件（`LYBT.Desktop.Users/Events/UserChangedEvent.cs`）:
```csharp
public class UserChangedEvent : PubSubEvent<int> { }
```

2. 发布事件（模块 A）:
```csharp
public class UserManagementViewModel : UnifiedViewModelBase
{
    private readonly IEventAggregator _eventAggregator;

    public async Task UpdateUserAsync(int userId)
    {
        await _userRepository.UpdateAsync(userId, updateDto);
        _eventAggregator.GetEvent<UserChangedEvent>().Publish(userId); // 发布事件
    }
}
```

3. 订阅事件（模块 B）:
```csharp
public class PatientManagementViewModel : UnifiedViewModelBase
{
    private readonly IEventAggregator _eventAggregator;

    public PatientManagementViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _eventAggregator.GetEvent<UserChangedEvent>().Subscribe(OnUserChanged); // 订阅事件
    }

    private void OnUserChanged(int userId)
    {
        // 用户数据变化，刷新患者列表
        RefreshAsync().ConfigureAwait(false);
    }
}
```

### 12.5 如何进行 Repository 单元测试？

**问题**: 如何对 Repository 进行单元测试？

**答案**: 使用 Moq 框架 Mock `IXxxApi` 接口

**示例**:
```csharp
using Moq;
using Xunit;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnUserList()
    {
        // Arrange
        var mockApi = new Mock<IUserApi>();
        var mockLogger = new Mock<ILogger<UserRepository>>();

        var expectedUsers = new List<UserDto>
        {
            new UserDto { Id = 1, Username = "admin", FullName = "管理员" },
            new UserDto { Id = 2, Username = "user1", FullName = "用户1" }
        };

        mockApi.Setup(api => api.GetAllUsersAsync())
               .ReturnsAsync(expectedUsers);

        var repository = new UserRepository(mockApi.Object, mockLogger.Object);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("admin", result[0].Username);
        mockApi.Verify(api => api.GetAllUsersAsync(), Times.Once);
    }
}
```

### 12.6 如何处理长时间运行的操作？

**问题**: 用户点击按钮后，需要执行一个耗时操作（如导入数据），如何避免 UI 卡顿？

**答案**: 使用 `async/await` + `IsBusy` 状态 + `BusyMessage`

**示例**:
```csharp
public DelegateCommand ImportDataCommand { get; private set; }

private void InitializeCommands()
{
    ImportDataCommand = new DelegateCommand(async () => await ImportDataAsync());
}

private async Task ImportDataAsync()
{
    try
    {
        // 1. 设置忙状态
        IsBusy = true;
        BusyMessage = "正在导入数据，请稍候...";

        // 2. 执行耗时操作（在后台线程）
        await Task.Run(async () =>
        {
            for (int i = 0; i < 1000; i++)
            {
                await _repository.ImportItemAsync(items[i]);

                // 更新进度
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    BusyMessage = $"正在导入数据 ({i + 1}/1000)...";
                });
            }
        });

        // 3. 显示成功消息
        _notificationService.ShowSuccess("数据导入成功");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入数据失败");
        _notificationService.ShowError($"导入数据失败: {ex.Message}");
    }
    finally
    {
        // 4. 清除忙状态
        IsBusy = false;
    }
}
```

---

## 附录

### A. 参考文档

- `docs/architecture/client/unified-design-standard.md` - Client 端统一设计标准
- `docs/development/standards.md` - 开发标准
- `docs/development/minimal-practice.md` - Issue 驱动工作法
- `docs/reports/desktop-deep-analysis-2025-10-12.md` - Desktop 深度分析报告

### B. 相关 ADR

- ADR-001: 拒绝过度工程化
- ADR-002: Desktop.Services 层移除决策（Repository 注册位置）
- ADR-004: 服务接口统一设计标准

### C. 联系方式

如有疑问或建议，请在 GitHub 上提交 Issue。

---

**文档版本历史**:
- v1.0 (2025-10-12): 初始版本，涵盖完整的 Desktop 三层架构设计标准
