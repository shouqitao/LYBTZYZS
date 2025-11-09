# LYBT.Desktop.Users - 用户管理模块

## 📦 项目定位

- **层级**:Client端
- **类型**:业务模块(用户管理)
- **职责**:为管理员提供完整的用户管理功能，包括用户的创建、编辑、角色分配、状态管理、密码重置等。采用MVVM架构和Repository模式，通过Prism实现模块化和依赖注入。支持分页查询、多条件过滤、批量操作等高级功能，确保大数据量下的流畅体验。

## 📂 代码结构

```
LYBT.Desktop.Users/
├── ViewModels/                                # MVVM视图模型层(7个)
│   ├── UserManagementViewModel.cs            # 用户管理主视图模型(19属性+20方法)
│   │   ├── 筛选属性(3):SelectedRole, SelectedStatus, ShowInactiveUsers
│   │   ├── 选项属性(2):RoleOptions, StatusOptions
│   │   ├── 命令属性(14):AddCommand, EditCommand, DeleteCommand, SearchCommand,
│   │   │              RefreshCommand, ResetPasswordCommand, ToggleUserStatusCommand,
│   │   │              ViewDetailsCommand, ClearFiltersCommand, FirstPageCommand,
│   │   │              PreviousPageCommand, NextPageCommand, LastPageCommand
│   │   └── 方法(20):构造函数, GetItemsAsync, InitializeUserCommands,
│   │                OnExecuteAddAsync, OnExecuteDeleteAsync, OnExecuteBatchDeleteAsync,
│   │                ExecuteEditUser, CanExecuteEditUser, ExecuteResetPasswordAsync,
│   │                CanExecuteResetPassword, ExecuteToggleUserStatusAsync,
│   │                CanExecuteToggleUserStatus, ExecuteViewDetails, ExecuteClearFilters,
│   │                ExecuteFirstPage, ExecuteLastPage, HasActiveFilters,
│   │                RefreshCanExecuteChanged
│   ├── UserCreateViewModel.cs                # 用户创建视图模型
│   ├── UserEditViewModel.cs                  # 用户编辑视图模型
│   ├── UserDetailViewModel.cs                # 用户详情视图模型
│   ├── ChangePasswordDialogViewModel.cs      # 修改密码对话框视图模型
│   ├── ResetPasswordDialogViewModel.cs       # 重置密码对话框视图模型
│   └── UserProfileDialogViewModel.cs         # 用户资料对话框视图模型
├── Views/                                     # WPF视图层(12个文件)
│   ├── UserManagementView.xaml               # 用户管理主视图
│   ├── UserManagementView.xaml.cs            # UserManagementView代码后置
│   ├── UserCreateView.xaml                   # 用户创建视图
│   ├── UserCreateView.xaml.cs                # UserCreateView代码后置
│   ├── UserEditView.xaml                     # 用户编辑视图
│   ├── UserEditView.xaml.cs                  # UserEditView代码后置
│   ├── UserDetailView.xaml                   # 用户详情视图
│   ├── UserDetailView.xaml.cs                # UserDetailView代码后置
│   ├── ChangePasswordDialog.xaml             # 修改密码对话框
│   ├── ChangePasswordDialog.xaml.cs          # ChangePasswordDialog代码后置
│   ├── ResetPasswordDialog.xaml              # 重置密码对话框（管理员功能）
│   ├── ResetPasswordDialog.xaml.cs           # ResetPasswordDialog代码后置
│   ├── UserProfileDialog.xaml                # 用户资料对话框
│   └── UserProfileDialog.xaml.cs             # UserProfileDialog代码后置
├── Repositories/                              # 数据仓储层(1个)
│   └── UserRepository.cs                      # 用户仓储实现(继承BaseApiRepository)
├── Interfaces/                                # 接口定义层(1个)
│   └── IUserRepository.cs                     # 用户仓储接口(9个方法)
├── Models/                                    # 本地模型层(1个)
│   └── UserItem.cs                            # 用户列表项模型(用于DataGrid绑定)
├── UsersModule.cs                             # Prism模块定义(2个方法)
│   ├── OnInitialized()                        # 模块初始化
│   └── RegisterTypes()                        # 类型注册(Views + ViewModels + Repository)
├── LYBT.Desktop.Users.csproj                  # 项目配置文件
└── README.md                                  # 本文档
```

**说明**:
- **UserManagementViewModel**:继承自UnifiedViewModelBase，提供分页、搜索、排序、批量操作等19属性+20方法
- **7个ViewModels**:覆盖用户管理全流程（列表、创建、编辑、详情、密码管理、资料维护）
- **12个Views**:主视图(UserManagementView) + 3个子视图(Create/Edit/Detail) + 3个对话框(ChangePassword/ResetPassword/UserProfile)
- **Repository模式**:UserRepository继承BaseApiRepository，提供9个数据访问方法
- **UserItem模型**:DataGrid专用列表项模型，优化UI绑定性能

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Foundation** - 平台无关基础服务(BaseApiRepository, IApiService, ICacheService)
2. **LYBT.Desktop.Infrastructure** - WPF基础设施(UnifiedViewModelBase, Converters, DialogService)
3. **LYBT.Desktop.Contracts** - 共享契约(UserDto, CreateUserDto, UpdateUserDto, PagedResult)
4. **LYBT.Desktop.Presentation** - UI展示层(DataGrid样式、Command样式、对话框样式)
5. **LYBT.Shared.Models** - 跨端共享模型(UserRole枚举, UserStatus枚举)
6. **LYBT.Shared.Interfaces** - 跨端共享接口

### 被依赖项目
1. **LYBT.Desktop.Shell** - Shell加载此模块并将UserManagementView注册到主区域
2. **其他业务模块** - 可能需要查询用户列表（如Patients模块选择医生）

### NuGet包
- **Prism.DryIoc** (8.x) - MVVM框架和依赖注入容器
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件库
- **Microsoft.Extensions.Logging** (8.0.x) - 日志记录

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF**: Windows Presentation Foundation UI框架
- **Prism.DryIoc 8.x**: MVVM框架、模块化、依赖注入、区域导航
- **MaterialDesignThemes 5.1.x**: Material Design风格UI组件库（DataGrid、Button、Dialog、Card）
- **Repository模式**: 数据访问层抽象，继承BaseApiRepository
- **UnifiedViewModelBase**: 统一的分页、搜索、排序基类
- **Microsoft.Extensions.Logging**: 结构化日志记录
- **异步编程**: async/await提升UI响应性

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Users/LYBT.Desktop.Users.csproj
```

**集成说明**:

### 1. 在Shell中加载Users模块

```csharp
// App.xaml.cs (Shell项目)
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Users模块（仅限管理员访问）
    moduleCatalog.AddModule<UsersModule>(InitializationMode.OnDemand);
}

// 管理员登录后加载Users模块
private void OnUserLoggedIn(UserDto user)
{
    if (user.Role == UserRole.Admin)
    {
        // 按需加载Users模块
        _moduleManager.LoadModule("UsersModule");
    }
}
```

### 2. IUserRepository核心接口

**完整接口表**（9个方法）:

| 方法名 | 功能描述 | 返回类型 |
|-------|---------|---------|
| **基础CRUD** (5) | | |
| GetAllAsync | 获取所有用户列表 | `Task<List<UserDto>>` |
| GetPagedAsync | 分页查询用户（支持搜索关键字） | `Task<PagedResult<UserDto>>` |
| GetByIdAsync | 按ID查询用户详情 | `Task<UserDto>` |
| CreateAsync | 创建新用户 | `Task<UserDto>` |
| UpdateAsync | 更新用户信息 | `Task<UserDto>` |
| **扩展查询** (3) | | |
| DeleteAsync | 删除用户（软删除） | `Task<bool>` |
| GetByUsernameAsync | 按用户名查询用户 | `Task<UserDto>` |
| SearchAsync | 搜索用户（多条件） | `Task<List<UserDto>>` |
| GetDoctorsAsync | 获取所有医生列表 | `Task<List<UserDto>>` |

### 3. UserManagementViewModel核心属性与方法

**完整接口表**（19属性+20方法）:

| 成员类型 | 名称 | 功能描述 |
|---------|------|---------|
| **筛选属性** (3) | | |
| | SelectedRole | 选中的角色过滤器（Admin/Doctor/全部） |
| | SelectedStatus | 选中的状态过滤器（Active/Inactive/全部） |
| | ShowInactiveUsers | 是否显示停用用户（CheckBox绑定） |
| **选项属性** (2) | | |
| | RoleOptions | 角色选项列表（绑定到ComboBox） |
| | StatusOptions | 状态选项列表（绑定到ComboBox） |
| **命令属性** (14) | | |
| | AddCommand | 添加用户命令（导航到创建视图） |
| | EditCommand | 编辑用户命令（导航到编辑视图） |
| | DeleteCommand | 删除用户命令（批量删除） |
| | SearchCommand | 搜索命令（触发分页查询） |
| | RefreshCommand | 刷新命令（重新加载当前页） |
| | ResetPasswordCommand | 重置密码命令（管理员功能） |
| | ToggleUserStatusCommand | 切换用户状态命令（激活/停用） |
| | ViewDetailsCommand | 查看详情命令（打开详情对话框） |
| | ClearFiltersCommand | 清除过滤器命令（重置筛选条件） |
| | FirstPageCommand | 首页命令（跳转到第一页） |
| | PreviousPageCommand | 上一页命令（继承自UnifiedViewModelBase） |
| | NextPageCommand | 下一页命令（继承自UnifiedViewModelBase） |
| | LastPageCommand | 末页命令（跳转到最后一页） |
| **关键方法** (20) | | |
| | 构造函数 | 初始化依赖服务、命令、事件订阅、自动加载首页 |
| | GetItemsAsync | 分页查询用户（支持角色/状态过滤、搜索关键字） |
| | InitializeUserCommands | 初始化所有用户管理命令 |
| | OnExecuteAddAsync | 添加用户（导航到UserCreateView） |
| | OnExecuteDeleteAsync | 删除用户（确认对话框 + API调用） |
| | OnExecuteBatchDeleteAsync | 批量删除用户（确认对话框 + 循环删除） |
| | ExecuteEditUser | 编辑用户（导航到UserEditView） |
| | CanExecuteEditUser | 编辑用户条件（必须选中用户） |
| | ExecuteResetPasswordAsync | 重置密码（打开重置对话框 + API调用） |
| | CanExecuteResetPassword | 重置密码条件（必须选中用户） |
| | ExecuteToggleUserStatusAsync | 切换用户状态（Active ↔ Inactive） |
| | CanExecuteToggleUserStatus | 切换状态条件（必须选中用户） |
| | ExecuteViewDetails | 查看详情（打开详情对话框） |
| | ExecuteClearFilters | 清除过滤器（重置筛选条件 + 刷新列表） |
| | ExecuteFirstPage | 首页（跳转到第一页） |
| | ExecuteLastPage | 末页（跳转到最后一页） |
| | HasActiveFilters | 检查是否有激活的过滤器 |
| | RefreshCanExecuteChanged | 刷新所有命令的CanExecute状态 |

### 4. 用户列表加载与分页示例

```csharp
// UserManagementViewModel.cs
public class UserManagementViewModel : UnifiedViewModelBase<UserDto>
{
    private readonly IUserRepository _userRepository;

    public UserManagementViewModel(
        IUserRepository userRepository,
        IEventAggregator eventAggregator,
        IRegionManager regionManager,
        IDialogService dialogService)
        : base(eventAggregator, regionManager, dialogService)
    {
        _userRepository = userRepository;

        // 初始化命令
        InitializeUserCommands();

        // 订阅用户登录事件（刷新列表）
        EventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

        // 自动加载首页数据
        _ = RefreshDataAsync();
    }

    // 分页查询用户（支持过滤）
    protected override async Task<(List<UserDto> items, int totalCount)> GetItemsAsync(
        int pageIndex, int pageSize, string? searchTerm)
    {
        try
        {
            // 构建查询参数
            var queryParams = new Dictionary<string, string>
            {
                ["pageIndex"] = pageIndex.ToString(),
                ["pageSize"] = pageSize.ToString()
            };

            // 添加搜索关键字
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                queryParams["searchTerm"] = searchTerm;
            }

            // 添加角色过滤
            if (SelectedRole != null && SelectedRole != "全部")
            {
                queryParams["role"] = SelectedRole;
            }

            // 添加状态过滤
            if (SelectedStatus != null && SelectedStatus != "全部")
            {
                queryParams["status"] = SelectedStatus;
            }

            // 调用Repository查询
            var result = await _userRepository.GetPagedAsync(pageIndex, pageSize, queryParams);

            if (result == null)
            {
                _logger.LogWarning("分页查询用户返回null");
                return (new List<UserDto>(), 0);
            }

            _logger.LogInformation("成功加载 {Count} 个用户（共 {Total} 个）",
                result.Items?.Count ?? 0, result.TotalCount);

            return (result.Items ?? new List<UserDto>(), result.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户列表失败");
            SetErrorMessage($"加载用户列表失败: {ex.Message}");
            return (new List<UserDto>(), 0);
        }
    }
}
```

### 5. 用户创建示例（对话框表单）

```csharp
// UserManagementViewModel.cs
private async Task OnExecuteAddAsync()
{
    try
    {
        // 打开用户创建对话框
        var parameters = new DialogParameters
        {
            { "Mode", "Create" }
        };

        _dialogService.ShowDialog("UserCreateView", parameters, async result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                // 获取创建的用户DTO
                var createDto = result.Parameters.GetValue<CreateUserDto>("UserDto");

                if (createDto != null)
                {
                    IsBusy = true;

                    // 调用Repository创建用户
                    var createdUser = await _userRepository.CreateAsync(createDto);

                    if (createdUser != null)
                    {
                        _logger.LogInformation("成功创建用户: {Username}", createdUser.Username);
                        SetSuccessMessage($"用户 {createdUser.Username} 创建成功");

                        // 刷新列表
                        await RefreshDataAsync();
                    }
                    else
                    {
                        SetErrorMessage("创建用户失败");
                    }

                    IsBusy = false;
                }
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建用户过程中发生异常");
        SetErrorMessage($"创建用户失败: {ex.Message}");
    }
}

// UserCreateViewModel.cs
public class UserCreateViewModel : DialogViewModelBase
{
    private string _username;
    private string _password;
    private UserRole _selectedRole;

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public UserCreateViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave)
            .ObservesProperty(() => Username)
            .ObservesProperty(() => Password);

        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    private void ExecuteSave()
    {
        var createDto = new CreateUserDto
        {
            Username = Username.Trim(),
            Password = Password,
            Role = SelectedRole,
            Status = UserStatus.Active
        };

        // 返回创建DTO给调用者
        var parameters = new DialogParameters
        {
            { "UserDto", createDto }
        };

        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
    }

    private bool CanExecuteSave()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               Password.Length >= 6;
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
```

### 6. 用户编辑示例（导航到编辑视图）

```csharp
// UserManagementViewModel.cs
private void ExecuteEditUser()
{
    if (SelectedItem == null) return;

    try
    {
        // 导航到编辑视图（带用户ID参数）
        var parameters = new NavigationParameters
        {
            { "UserId", SelectedItem.Id }
        };

        RegionManager.RequestNavigate("ContentRegion", "UserEditView", parameters);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导航到用户编辑视图失败");
        SetErrorMessage($"无法打开编辑视图: {ex.Message}");
    }
}

private bool CanExecuteEditUser()
{
    return SelectedItem != null;
}

// UserEditViewModel.cs
public class UserEditViewModel : BindableBase, INavigationAware
{
    private readonly IUserRepository _userRepository;
    private UserDto _currentUser;

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 获取用户ID参数
        var userId = navigationContext.Parameters.GetValue<Guid>("UserId");

        // 加载用户详情
        _ = LoadUserAsync(userId);
    }

    private async Task LoadUserAsync(Guid userId)
    {
        try
        {
            IsBusy = true;

            var user = await _userRepository.GetByIdAsync(userId);

            if (user != null)
            {
                CurrentUser = user;
                Username = user.Username;
                SelectedRole = user.Role;
                SelectedStatus = user.Status;
                _logger.LogInformation("成功加载用户详情: {Username}", user.Username);
            }
            else
            {
                SetErrorMessage("用户不存在");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户详情失败");
            SetErrorMessage($"加载失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteSaveAsync()
    {
        try
        {
            IsBusy = true;

            var updateDto = new UpdateUserDto
            {
                Id = CurrentUser.Id,
                Username = Username.Trim(),
                Role = SelectedRole,
                Status = SelectedStatus
            };

            var updatedUser = await _userRepository.UpdateAsync(updateDto);

            if (updatedUser != null)
            {
                _logger.LogInformation("成功更新用户: {Username}", updatedUser.Username);
                SetSuccessMessage("用户信息已更新");

                // 返回列表视图
                RegionManager.RequestNavigate("ContentRegion", "UserManagementView");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败");
            SetErrorMessage($"更新失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 7. 用户状态切换示例（激活/停用）

```csharp
// UserManagementViewModel.cs
private async Task ExecuteToggleUserStatusAsync()
{
    if (SelectedItem == null) return;

    try
    {
        IsBusy = true;

        var targetStatus = SelectedItem.Status == UserStatus.Active
            ? UserStatus.Inactive
            : UserStatus.Active;

        var action = targetStatus == UserStatus.Active ? "激活" : "停用";

        // 确认对话框
        var result = await _dialogService.ShowConfirmationAsync(
            $"确认{action}",
            $"确定要{action}用户 \"{SelectedItem.Username}\" 吗？");

        if (result != ButtonResult.OK) return;

        // 调用Repository更新状态
        var updateDto = new UpdateUserDto
        {
            Id = SelectedItem.Id,
            Username = SelectedItem.Username,
            Role = SelectedItem.Role,
            Status = targetStatus
        };

        var updatedUser = await _userRepository.UpdateAsync(updateDto);

        if (updatedUser != null)
        {
            _logger.LogInformation("成功{Action}用户: {Username}", action, updatedUser.Username);
            SetSuccessMessage($"用户 {updatedUser.Username} 已{action}");

            // 刷新列表
            await RefreshDataAsync();
        }
        else
        {
            SetErrorMessage($"{action}用户失败");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "切换用户状态失败");
        SetErrorMessage($"操作失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

private bool CanExecuteToggleUserStatus()
{
    return SelectedItem != null;
}
```

### 8. 密码重置示例（管理员功能）

```csharp
// UserManagementViewModel.cs
private async Task ExecuteResetPasswordAsync()
{
    if (SelectedItem == null) return;

    try
    {
        // 打开重置密码对话框
        var parameters = new DialogParameters
        {
            { "UserId", SelectedItem.Id },
            { "Username", SelectedItem.Username }
        };

        _dialogService.ShowDialog("ResetPasswordDialog", parameters, async result =>
        {
            if (result.Result == ButtonResult.OK)
            {
                var newPassword = result.Parameters.GetValue<string>("NewPassword");

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    IsBusy = true;

                    // 调用API重置密码
                    var success = await _userRepository.ResetPasswordAsync(SelectedItem.Id, newPassword);

                    if (success)
                    {
                        _logger.LogInformation("成功重置用户密码: {Username}", SelectedItem.Username);
                        SetSuccessMessage($"用户 {SelectedItem.Username} 的密码已重置");
                    }
                    else
                    {
                        SetErrorMessage("重置密码失败");
                    }

                    IsBusy = false;
                }
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "重置密码过程中发生异常");
        SetErrorMessage($"重置密码失败: {ex.Message}");
    }
}

private bool CanExecuteResetPassword()
{
    return SelectedItem != null;
}

// ResetPasswordDialogViewModel.cs
public class ResetPasswordDialogViewModel : DialogViewModelBase
{
    private string _newPassword;
    private string _confirmPassword;

    public DelegateCommand ResetCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public ResetPasswordDialogViewModel(IDialogService dialogService)
        : base(dialogService)
    {
        ResetCommand = new DelegateCommand(ExecuteReset, CanExecuteReset)
            .ObservesProperty(() => NewPassword)
            .ObservesProperty(() => ConfirmPassword);

        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    private void ExecuteReset()
    {
        var parameters = new DialogParameters
        {
            { "NewPassword", NewPassword }
        };

        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
    }

    private bool CanExecuteReset()
    {
        return !string.IsNullOrWhiteSpace(NewPassword) &&
               NewPassword.Length >= 6 &&
               NewPassword == ConfirmPassword;
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
```

### 9. UserRepository实现示例

```csharp
// UserRepository.cs
public class UserRepository : BaseApiRepository<UserDto>, IUserRepository
{
    public UserRepository(IApiService apiService, ILogger<UserRepository> logger)
        : base(apiService, logger, "/api/v1/users")
    {
    }

    // 基础CRUD方法继承自BaseApiRepository
    // GetAllAsync(), GetByIdAsync(), CreateAsync(), UpdateAsync(), DeleteAsync()

    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        try
        {
            var result = await _apiService.GetAsync<UserDto>($"{_endpoint}/by-username/{username}");
            return result.IsSuccess ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按用户名查询用户失败: {Username}", username);
            return null;
        }
    }

    public async Task<List<UserDto>> SearchAsync(string keyword)
    {
        try
        {
            var result = await _apiService.GetAsync<List<UserDto>>($"{_endpoint}/search?keyword={keyword}");
            return result.IsSuccess && result.Data != null ? result.Data : new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
            return new List<UserDto>();
        }
    }

    public async Task<List<UserDto>> GetDoctorsAsync()
    {
        try
        {
            var result = await _apiService.GetAsync<List<UserDto>>($"{_endpoint}/doctors");
            return result.IsSuccess && result.Data != null ? result.Data : new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医生列表失败");
            return new List<UserDto>();
        }
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            var dto = new ResetPasswordDto { NewPassword = newPassword };
            var result = await _apiService.PostAsync<bool>($"{_endpoint}/{userId}/reset-password", dto);
            return result.IsSuccess && result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置密码失败: {UserId}", userId);
            return false;
        }
    }
}
```

### 10. UsersModule注册

```csharp
// UsersModule.cs
public class UsersModule : IModule
{
    private readonly IRegionManager _regionManager;

    public UsersModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化时的逻辑（如需要）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Repository
        containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

        // 注册Views（用于导航）
        containerRegistry.RegisterForNavigation<UserManagementView>();
        containerRegistry.RegisterForNavigation<UserCreateView>();
        containerRegistry.RegisterForNavigation<UserEditView>();
        containerRegistry.RegisterForNavigation<UserDetailView>();

        // 注册Dialogs（用于对话框）
        containerRegistry.RegisterDialog<ChangePasswordDialog, ChangePasswordDialogViewModel>();
        containerRegistry.RegisterDialog<ResetPasswordDialog, ResetPasswordDialogViewModel>();
        containerRegistry.RegisterDialog<UserProfileDialog, UserProfileDialogViewModel>();

        // 注册ViewModels（自动绑定到Views）
        containerRegistry.Register<UserManagementViewModel>();
        containerRegistry.Register<UserCreateViewModel>();
        containerRegistry.Register<UserEditViewModel>();
        containerRegistry.Register<UserDetailViewModel>();
    }
}
```

## 🎨 模块架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                     LYBT.Desktop.Users                          │
│                    (用户管理模块)                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │            Views (WPF视图层)                             │  │
│  │  ┌─────────────────┐    ┌─────────────────────────┐    │  │
│  │  │UserManagement   │    │  Create/Edit/Detail     │    │  │
│  │  │View (主视图)    │    │  (子视图)               │    │  │
│  │  └────────┬────────┘    └────────┬────────────────┘    │  │
│  │           │ DataContext          │ DataContext         │  │
│  │  ┌────────┴────────────────┬─────┴────────────┐        │  │
│  │  │   Dialogs (对话框)      │                  │        │  │
│  │  │  • ChangePassword       │                  │        │  │
│  │  │  • ResetPassword        │                  │        │  │
│  │  │  • UserProfile          │                  │        │  │
│  │  └─────────────────────────┘                  │        │  │
│  └───────────────────────────────────────────────┼────────┘  │
│                                                  │            │
│  ┌──────────────────────────────────────────────┼────────┐  │
│  │         ViewModels (业务逻辑层)              │        │  │
│  │  ┌───────────────────────────────────────────┴──────┐ │  │
│  │  │         UserManagementViewModel                  │ │  │
│  │  │  ────────────────────────────────────────────   │ │  │
│  │  │  属性: SelectedItem, RoleOptions, Commands      │ │  │
│  │  │  ────────────────────────────────────────────   │ │  │
│  │  │  方法: GetItemsAsync(), OnExecuteAddAsync()     │ │  │
│  │  │        ExecuteEditUser(), ExecuteResetPassword  │ │  │
│  │  └────────────┬──────────────────────────────────┘ │  │
│  └───────────────┼────────────────────────────────────┘  │
│                  │                                        │
│  ┌───────────────┴────────────────────────────────────┐  │
│  │       Repositories (数据访问层)                    │  │
│  │  ┌──────────────────────────────────────────────┐ │  │
│  │  │         UserRepository                       │ │  │
│  │  │  继承: BaseApiRepository<UserDto>           │ │  │
│  │  │  ────────────────────────────────────────   │ │  │
│  │  │  方法: GetPagedAsync(), CreateAsync()       │ │  │
│  │  │        UpdateAsync(), GetDoctorsAsync()     │ │  │
│  │  │        ResetPasswordAsync()                 │ │  │
│  │  └────────────┬─────────────────────────────── │ │  │
│  └───────────────┼────────────────────────────────┘  │
└──────────────────┼──────────────────────────────────────┘
                   │
                   ▼
      ┌────────────────────────────────────────┐
      │   LYBT.Desktop.Foundation (依赖服务)   │
      ├────────────────────────────────────────┤
      │  • BaseApiRepository<TDto>            │
      │  • IApiService (HTTP通信)             │
      │  • ICacheService (缓存优化)           │
      └────────────────────────────────────────┘
                   │
                   ▼
      ┌────────────────────────────────────────┐
      │    LYBT.WebAPI (后端用户服务)          │
      ├────────────────────────────────────────┤
      │  GET    /api/v1/users                 │
      │  GET    /api/v1/users/{id}            │
      │  POST   /api/v1/users                 │
      │  PUT    /api/v1/users/{id}            │
      │  DELETE /api/v1/users/{id}            │
      │  POST   /api/v1/users/{id}/reset-pwd  │
      │  GET    /api/v1/users/doctors         │
      └────────────────────────────────────────┘
```

## 🎯 设计原则

### 1. MVVM架构与UnifiedViewModelBase

**原则**：所有列表管理ViewModel继承UnifiedViewModelBase，统一分页、搜索、排序逻辑。

**实现**：
- **UserManagementViewModel**继承自UnifiedViewModelBase<UserDto>
- 获得自动的分页属性（PageIndex, PageSize, TotalCount, TotalPages）
- 获得自动的命令（PreviousPageCommand, NextPageCommand, RefreshCommand）
- 只需实现`GetItemsAsync()`方法即可完成分页查询
- 避免重复代码，所有业务模块列表ViewModel统一使用此模式

**反面案例（禁止）**：
```csharp
// ❌ 错误：手动实现分页逻辑（重复代码）
public class UserManagementViewModel : BindableBase
{
    private int _pageIndex = 1;
    private int _pageSize = 10;
    private int _totalCount;

    public async Task LoadPageAsync()
    {
        var result = await _userRepository.GetPagedAsync(_pageIndex, _pageSize);
        // 手动计算总页数
        TotalPages = (int)Math.Ceiling((double)_totalCount / _pageSize);
    }
}
```

### 2. Repository模式与依赖注入

**原则**：ViewModel不直接调用IApiService，而是通过Repository抽象数据访问。

**架构层次**：
```
ViewModel → Repository → BaseApiRepository → IApiService → HTTP
```

**优势**：
- **解耦**：ViewModel不依赖具体的HTTP实现
- **可测试**：Repository易于Mock，便于单元测试
- **缓存**：Repository可在内部实现缓存策略（如医生列表缓存5分钟）
- **业务逻辑**：Repository可封装复杂的数据转换和组合查询

**示例**：
```csharp
// ViewModel层
public class UserManagementViewModel
{
    private readonly IUserRepository _userRepository; // 依赖Repository接口

    protected override async Task<(List<UserDto>, int)> GetItemsAsync(...)
    {
        var result = await _userRepository.GetPagedAsync(pageIndex, pageSize, queryParams);
        return (result.Items, result.TotalCount);
    }
}

// Repository层
public class UserRepository : BaseApiRepository<UserDto>, IUserRepository
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(...)
    {
        // 内部调用IApiService
        var result = await _apiService.GetAsync<PagedResult<UserDto>>(...);
        return result.Data;
    }
}
```

### 3. 分页优化与虚拟化

**问题**：如果一次性加载所有用户（可能数千条），会导致UI卡顿和内存占用高。

**解决方案**：
- **分页查询**：每页仅加载10-50条数据
- **UI虚拟化**：DataGrid使用VirtualizingStackPanel（WPF默认开启）
- **按需加载**：用户滚动到底部时自动加载下一页
- **缓存策略**：Repository缓存最近访问的页面（如缓存最近3页）

**性能目标**：
- 首次加载 <500ms
- 翻页响应 <200ms
- 支持1000+用户无卡顿

### 4. 权限控制与安全性

**权限分离**：
- **Admin角色**：所有功能（创建、编辑、删除、重置密码、切换状态）
- **Doctor角色**：仅查看自己的资料（通过UserProfileDialog）

**实现**：
```csharp
// 在构造函数中根据角色注册命令
public UserManagementViewModel(IAuthenticationService authService, ...)
{
    var currentUser = authService.GetCurrentUserAsync().Result;

    if (currentUser.Role == UserRole.Admin)
    {
        // 管理员：注册所有命令
        AddCommand = new DelegateCommand(OnExecuteAddAsync);
        EditCommand = new DelegateCommand(ExecuteEditUser, CanExecuteEditUser);
        DeleteCommand = new DelegateCommand<List<UserDto>>(OnExecuteBatchDeleteAsync);
        ResetPasswordCommand = new DelegateCommand(ExecuteResetPasswordAsync, CanExecuteResetPassword);
    }
    else
    {
        // 医生：仅查看资料
        ViewProfileCommand = new DelegateCommand(ExecuteViewProfile);
    }
}
```

**密码安全**：
- 创建用户时密码最少6位
- 重置密码需要确认对话框
- 密码在传输时通过HTTPS加密
- 后端存储时使用BCrypt哈希

### 5. 对话框通信与参数传递

**Prism对话框模式**：
- **ShowDialog**：显示模态对话框，阻塞主线程
- **DialogParameters**：传递参数到对话框（输入）
- **DialogResult**：从对话框返回结果（输出）

**标准流程**：
```csharp
// Step 1: 准备参数
var parameters = new DialogParameters
{
    { "UserId", selectedUser.Id },
    { "Username", selectedUser.Username }
};

// Step 2: 显示对话框
_dialogService.ShowDialog("ResetPasswordDialog", parameters, result =>
{
    // Step 3: 处理返回结果
    if (result.Result == ButtonResult.OK)
    {
        var newPassword = result.Parameters.GetValue<string>("NewPassword");
        // 执行重置密码逻辑
    }
});
```

**对话框ViewModel**：
```csharp
public class ResetPasswordDialogViewModel : DialogViewModelBase, IDialogAware
{
    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 接收参数
        UserId = parameters.GetValue<Guid>("UserId");
        Username = parameters.GetValue<string>("Username");
    }

    private void ExecuteReset()
    {
        // 返回结果
        var resultParams = new DialogParameters
        {
            { "NewPassword", NewPassword }
        };
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, resultParams));
    }
}
```

### 6. 异步优先与用户体验

**所有I/O操作异步化**：
- `GetItemsAsync`：分页查询
- `OnExecuteAddAsync`：创建用户
- `ExecuteResetPasswordAsync`：重置密码
- `ExecuteToggleUserStatusAsync`：切换状态

**用户体验优化**：
- **IsBusy状态**：显示Loading动画（防止重复点击）
- **CanExecute检查**：命令按钮自动启用/禁用（如未选中用户时禁用编辑按钮）
- **友好错误提示**："用户名已存在" 而非 "409 Conflict"
- **成功反馈**：操作成功后显示SnackBar提示 + 自动刷新列表

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/users/](../../../../docs/reference/modules/users/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/users-design.md](../../../../docs/explanation/architecture/client/users-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/users-development.md](../../../../docs/how-to-guides/client/users-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
