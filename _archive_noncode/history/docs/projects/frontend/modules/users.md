# LYBT.Desktop.Users - 用户管理模块

## 📋 项目概览

**项目名称**: LYBT.Desktop.Users  
**项目类型**: WPF 模块化业务组件  
**技术栈**: .NET 8.0, WPF, Prism.DryIoc 9.0.537  
**架构模式**: MVVM + Prism 模块化架构  
**业务职责**: 系统用户管理、角色分配、密码管理、用户档案维护

### 核心功能

1. **用户档案管理** - 创建、编辑、查看、删除用户信息
2. **角色权限管理** - Admin/Doctor角色分配和权限控制
3. **密码管理** - 密码重置、修改密码、安全策略
4. **状态管理** - 用户启用/禁用、批量操作
5. **搜索查询** - 用户搜索、分页查询、筛选功能
6. **个人资料** - 用户个人信息维护和更新

### 依赖关系

- **Desktop.Core** - 基础控件和设计系统
- **Desktop.Infrastructure** - 基础服务接口
- **Desktop.Services** - API客户端和数据服务
- **Shared.Models** - 用户相关DTO模型
- **第三方依赖**: Prism.DryIoc 9.0.537, AutoMapper, Refit

## 🏗️ 项目架构

### 目录结构

```
LYBT.Desktop.Users/
├── Api/                              # API接口定义 (空目录，使用Services层接口)
├── Services/                         # 业务服务层
│   └── UserModule.cs                # 核心用户业务服务
├── ViewModels/                       # MVVM视图模型
│   ├── UserManagementViewModel.cs   # 用户管理主界面视图模型
│   └── UserAddEditDialogViewModel.cs # 用户添加/编辑对话框视图模型
├── Views/                           # WPF视图界面
│   ├── UserManagementView.xaml     # 用户管理主界面
│   ├── UserAddEditDialog.xaml      # 用户添加/编辑对话框
│   ├── UserDetailView.xaml         # 用户详情查看界面
│   ├── ChangePasswordDialog.xaml   # 修改密码对话框
│   ├── ResetPasswordDialog.xaml    # 重置密码对话框
│   └── UserProfileDialog.xaml      # 用户个人资料对话框
├── UsersModule.cs                   # Prism模块注册
└── LYBT.Desktop.Users.csproj
```

### 架构模式

#### 1. Prism模块化架构
```csharp
// UsersModule.cs - 模块注册
public class UsersModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // UltraThink修复：模块自己注册服务接口实现
        containerRegistry.RegisterSingleton<UserModule>();
        containerRegistry.RegisterSingleton<IUserService>(container => container.Resolve<UserModule>());
        
        // 注册视图和视图模型
        containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModel>();
        containerRegistry.RegisterForNavigation<UserAddEditDialog, UserAddEditDialogViewModel>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成后的操作
    }
}
```

#### 2. 业务服务模块架构
```csharp
// UserModule.cs - 核心业务服务
public class UserModule : IUserService
{
    private readonly IUserApi _apiService;
    private readonly IMapper _mapper;
    
    public UserModule(IUserApi apiService, IMapper mapper)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    // 基础CRUD操作
    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        // UltraThink v2.0: 调用Refit API客户端，处理ApiResponse包装格式
        var apiResponse = await _apiService.GetUserByIdAsync(id);
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<UserDto>.Failure("获取用户详情失败");
        }
        
        // 手动解包ApiResponse包装格式
        var wrappedResponse = apiResponse.Content;
        if (!wrappedResponse.Success || wrappedResponse.Data == null)
        {
            return ServiceResult<UserDto>.Failure(wrappedResponse.Message ?? "获取用户详情失败");
        }
        
        return ServiceResult<UserDto>.Success(wrappedResponse.Data);
    }
}
```

#### 3. MVVM视图模型架构
```csharp
// UserManagementViewModel - 用户管理主界面
public class UserManagementViewModel : ViewModelBase
{
    private readonly UserModule _userModule;
    private readonly IDialogService _dialogService;
    
    // 数据绑定属性
    public ObservableCollection<UserDto> Users { get; set; }
    public UserDto SelectedUser { get; set; }
    public string SearchKeyword { get; set; }
    
    // 命令定义
    public DelegateCommand LoadUsersCommand { get; }
    public DelegateCommand<UserDto> EditUserCommand { get; }
    public DelegateCommand AddUserCommand { get; }
    public DelegateCommand<UserDto> DeleteUserCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    
    // 分页属性
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
}
```

## 🔧 核心组件

### 1. UserModule (核心业务服务)

#### 主要功能
- **基础CRUD**: 创建、读取、更新、删除用户
- **状态管理**: 启用/禁用用户、批量操作
- **密码管理**: 重置密码、修改密码
- **搜索查询**: 用户搜索、分页查询
- **角色管理**: 获取角色列表、角色分配
- **数据验证**: 用户名唯一性、手机号重复检查

#### 核心方法

##### 基础CRUD操作
```csharp
// 分页查询用户
public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
{
    try
    {
        // 转换为基础查询DTO
        var baseQuery = new PagedQueryBaseDto
        {
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            Keyword = query.Keyword
        };
        
        // 调用Refit API客户端
        var apiResponse = await _apiService.GetUsersAsync(
            page: baseQuery.PageIndex,
            pageSize: baseQuery.PageSize,
            keyword: baseQuery.Keyword);
        
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult<PagedResult<UserDto>>.Failure($"API调用失败: {apiResponse.StatusCode}");
        }
        
        // 解包ApiResponse包装格式
        var wrappedResponse = apiResponse.Content;
        if (!wrappedResponse.Success || wrappedResponse.Data == null)
        {
            return ServiceResult<PagedResult<UserDto>>.Failure(wrappedResponse.Message ?? "获取用户列表失败");
        }
        
        return ServiceResult<PagedResult<UserDto>>.Success(wrappedResponse.Data);
    }
    catch (Exception ex)
    {
        return ServiceResult<PagedResult<UserDto>>.Failure($"获取用户列表异常: {ex.Message}");
    }
}

// 创建新用户
public async Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto)
{
    try
    {
        // 设置为创建操作
        dto.IsCreateOperation = true;
        
        // 业务验证
        var validationResult = await ValidateMutationDtoAsync(dto);
        if (!validationResult.IsSuccess)
        {
            return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage ?? "创建用户验证失败");
        }
        
        // 检查用户名是否已存在
        var usernameExistsResult = await IsUsernameExistsAsync(dto.Username);
        if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
        {
            return ServiceResult<UserDto>.Failure("该用户名已被使用");
        }
        
        // 检查电话号码是否已存在
        if (!string.IsNullOrEmpty(dto.PhoneNumber))
        {
            var phoneExistsResult = await IsPhoneExistsAsync(dto.PhoneNumber);
            if (phoneExistsResult.IsSuccess && phoneExistsResult.Data)
            {
                return ServiceResult<UserDto>.Failure("该电话号码已被使用");
            }
        }
        
        // 调用API创建用户
        var apiResponse = await _apiService.CreateUserAsync(dto);
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<UserDto>.Failure("创建用户失败");
        }
        
        // 解包响应
        var wrappedResponse = apiResponse.Content;
        if (!wrappedResponse.Success || wrappedResponse.Data == null)
        {
            return ServiceResult<UserDto>.Failure(wrappedResponse.Message ?? "创建用户失败");
        }
        
        return ServiceResult<UserDto>.Success(wrappedResponse.Data);
    }
    catch (Exception ex)
    {
        return ServiceResult<UserDto>.Failure($"创建用户异常: {ex.Message}");
    }
}

// 更新用户信息
public async Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto)
{
    try
    {
        if (dto.Id == Guid.Empty)
        {
            return ServiceResult<UserDto>.Failure("用户ID不能为空");
        }
        
        // 设置为更新操作
        dto.IsCreateOperation = false;
        
        // 业务验证
        var validationResult = await ValidateMutationDtoAsync(dto);
        if (!validationResult.IsSuccess)
        {
            return ServiceResult<UserDto>.Failure(validationResult.ErrorMessage ?? "更新用户验证失败");
        }
        
        // 检查用户名是否已被其他用户使用
        var usernameExistsResult = await IsUsernameExistsAsync(dto.Username, dto.Id);
        if (usernameExistsResult.IsSuccess && usernameExistsResult.Data)
        {
            return ServiceResult<UserDto>.Failure("该用户名已被其他用户使用");
        }
        
        // 调用API更新用户
        var apiResponse = await _apiService.UpdateUserAsync(dto.Id, dto);
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
        {
            return ServiceResult<UserDto>.Failure("更新用户失败");
        }
        
        // 解包响应
        var wrappedResponse = apiResponse.Content;
        if (!wrappedResponse.Success || wrappedResponse.Data == null)
        {
            return ServiceResult<UserDto>.Failure(wrappedResponse.Message ?? "更新用户失败");
        }
        
        return ServiceResult<UserDto>.Success(wrappedResponse.Data);
    }
    catch (Exception ex)
    {
        return ServiceResult<UserDto>.Failure($"更新用户异常: {ex.Message}");
    }
}
```

##### 状态管理
```csharp
// 启用用户
public async Task<ServiceResult<bool>> EnableAsync(Guid id)
{
    try
    {
        if (id == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("用户ID不能为空");
        }
        
        var apiResponse = await _apiService.ToggleStatusAsync(id);
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult<bool>.Failure("启用用户失败");
        }
        
        return ServiceResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"启用用户异常: {ex.Message}");
    }
}

// 批量启用用户
public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids)
{
    try
    {
        if (ids == null || !ids.Any())
        {
            return ServiceResult<int>.Failure("用户ID列表不能为空");
        }
        
        int successCount = 0;
        foreach (var id in ids)
        {
            var result = await EnableAsync(id);
            if (result.IsSuccess)
            {
                successCount++;
            }
        }
        
        return ServiceResult<int>.Success(successCount);
    }
    catch (Exception ex)
    {
        return ServiceResult<int>.Failure($"批量启用用户异常: {ex.Message}");
    }
}
```

##### 密码管理
```csharp
// 重置用户密码
public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword)
{
    try
    {
        if (id == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("用户ID不能为空");
        }
        
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return ServiceResult<bool>.Failure("新密码不能为空");
        }
        
        if (newPassword.Length < 6)
        {
            return ServiceResult<bool>.Failure("新密码长度不能少于6个字符");
        }
        
        var apiResponse = await _apiService.ResetPasswordAsync(id);
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult<bool>.Failure("重置密码失败");
        }
        
        return ServiceResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"重置密码异常: {ex.Message}");
    }
}

// 修改用户密码
public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
{
    try
    {
        if (id == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("用户ID不能为空");
        }
        
        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            return ServiceResult<bool>.Failure("原密码不能为空");
        }
        
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return ServiceResult<bool>.Failure("新密码不能为空");
        }
        
        if (newPassword.Length < 6)
        {
            return ServiceResult<bool>.Failure("新密码长度不能少于6个字符");
        }
        
        var changePasswordDto = new ChangePasswordDto
        {
            UserId = id,
            OldPassword = oldPassword,
            NewPassword = newPassword
        };
        
        var apiResponse = await _apiService.ChangePasswordAsync(changePasswordDto);
        if (!apiResponse.IsSuccessStatusCode)
        {
            return ServiceResult<bool>.Failure("更改密码失败");
        }
        
        return ServiceResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"更改密码异常: {ex.Message}");
    }
}

// 修改用户个人信息
public async Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto)
{
    try
    {
        if (dto.UserId == Guid.Empty)
        {
            return ServiceResult<bool>.Failure("用户ID不能为空");
        }
        
        // 获取现有用户信息
        var existingUserResult = await GetByIdAsync(dto.UserId);
        if (!existingUserResult.IsSuccess)
        {
            return ServiceResult<bool>.Failure("获取用户信息失败");
        }
        
        var existingUser = existingUserResult.Data;
        if (existingUser == null)
        {
            return ServiceResult<bool>.Failure("用户信息不存在");
        }
        
        // 直接使用UserMutationDto，无需额外转换
        var updateResult = await UpdateAsync(new UserMutationDto
        {
            Id = dto.UserId,
            Username = existingUser.Username,
            RealName = dto.RealName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Role = existingUser.Role,
            Status = existingUser.IsActive ? CommonStatus.Enabled : CommonStatus.Disabled,
            IsCreateOperation = false
        });
        
        if (!updateResult.IsSuccess)
        {
            return ServiceResult<bool>.Failure(updateResult.ErrorMessage ?? "修改个人信息失败");
        }
        
        return ServiceResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"修改个人信息异常: {ex.Message}");
    }
}
```

##### 搜索查询
```csharp
// 搜索用户
public async Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword)
{
    try
    {
        var query = new UserPagedQueryDto
        {
            PageIndex = 1,
            PageSize = 100, // 搜索时使用较大的页面大小
            Keyword = keyword
        };
        
        var result = await GetPagedAsync(query);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage ?? "搜索用户失败");
        }
        
        return ServiceResult<List<UserDto>>.Success(result.Data?.Items?.ToList() ?? new List<UserDto>());
    }
    catch (Exception ex)
    {
        return ServiceResult<List<UserDto>>.Failure($"搜索用户异常: {ex.Message}");
    }
}

// 获取活跃用户列表
public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
{
    try
    {
        var query = new UserPagedQueryDto
        {
            PageIndex = 1,
            PageSize = 1000, // 获取所有活跃用户
            Keyword = string.Empty
        };
        
        var result = await GetPagedAsync(query);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<UserDto>>.Failure(result.ErrorMessage ?? "获取用户列表失败");
        }
        
        // 过滤活跃用户
        var activeUsers = result.Data?.Items?.Where(u => u.IsActive).ToList() ?? new List<UserDto>();
        return ServiceResult<List<UserDto>>.Success(activeUsers);
    }
    catch (Exception ex)
    {
        return ServiceResult<List<UserDto>>.Failure($"获取活跃用户列表异常: {ex.Message}");
    }
}
```

##### 角色管理
```csharp
// 获取所有角色列表
public Task<ServiceResult<List<object>>> GetRolesAsync()
{
    try
    {
        var roles = Enum.GetNames(typeof(UserRole))
            .Select(name => new { 
                Value = name, 
                Text = name,
                EnumValue = (int)Enum.Parse(typeof(UserRole), name)
            })
            .Cast<object>()
            .ToList();
        
        return Task.FromResult(ServiceResult<List<object>>.Success(roles));
    }
    catch (Exception ex)
    {
        return Task.FromResult(ServiceResult<List<object>>.Failure($"获取角色列表异常: {ex.Message}"));
    }
}
```

##### 数据验证
```csharp
// 统一的UserMutationDto验证方法
private Task<ServiceResult> ValidateMutationDtoAsync(UserMutationDto dto)
{
    if (dto == null) return Task.FromResult(ServiceResult.Failure("用户信息不能为空"));
    if (string.IsNullOrWhiteSpace(dto.Username)) return Task.FromResult(ServiceResult.Failure("用户名不能为空"));
    if (dto.Username.Length < 3 || dto.Username.Length > 50) return Task.FromResult(ServiceResult.Failure("用户名长度必须在3到50个字符之间"));
    if (string.IsNullOrWhiteSpace(dto.RealName)) return Task.FromResult(ServiceResult.Failure("真实姓名不能为空"));
    if (dto.RealName.Length > 50) return Task.FromResult(ServiceResult.Failure("真实姓名长度不能超过50个字符"));
    
    // 创建操作时密码必填
    if (dto.IsCreateOperation && string.IsNullOrWhiteSpace(dto.Password))
        return Task.FromResult(ServiceResult.Failure("创建用户时密码不能为空"));
        
    return Task.FromResult(ServiceResult.Success());
}

// 检查用户名是否存在
private async Task<ServiceResult<bool>> IsUsernameExistsAsync(string username, Guid? excludeId = null)
{
    try
    {
        var searchResult = await SearchAsync(username);
        if (!searchResult.IsSuccess)
        {
            return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
        }
        
        var exists = searchResult.Data?.Any(u => 
            u.Username == username && 
            (excludeId == null || u.Id != excludeId.Value)) ?? false;
        
        return ServiceResult<bool>.Success(exists);
    }
    catch
    {
        return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
    }
}

// 检查手机号是否存在
private async Task<ServiceResult<bool>> IsPhoneExistsAsync(string phoneNumber, Guid? excludeId = null)
{
    try
    {
        var searchResult = await SearchAsync(phoneNumber);
        if (!searchResult.IsSuccess)
        {
            return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
        }
        
        var exists = searchResult.Data?.Any(u => 
            u.PhoneNumber == phoneNumber && 
            (excludeId == null || u.Id != excludeId.Value)) ?? false;
        
        return ServiceResult<bool>.Success(exists);
    }
    catch
    {
        return ServiceResult<bool>.Success(false); // 检查失败时假设不存在
    }
}
```

### 2. UserManagementViewModel (用户管理主界面)

#### 主要功能
- **用户列表管理**: 分页显示、搜索筛选、刷新加载
- **用户操作**: 添加、编辑、删除、启用/禁用
- **批量操作**: 批量启用、批量禁用用户
- **对话框管理**: 调用各种对话框进行用户操作

#### 核心属性
```csharp
public class UserManagementViewModel : ViewModelBase
{
    // 依赖服务
    private readonly UserModule _userModule;
    private readonly IDialogService _dialogService;
    
    // 数据绑定属性
    public ObservableCollection<UserDto> Users { get; set; } = new();
    public UserDto SelectedUser { get; set; }
    public List<UserDto> SelectedUsers { get; set; } = new();
    public string SearchKeyword { get; set; } = string.Empty;
    
    // 分页属性
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    // UI状态属性
    public bool IsLoading { get; set; }
    public bool HasUsers => Users?.Any() == true;
    public bool CanEdit => SelectedUser != null;
    public bool CanDelete => SelectedUser != null && !SelectedUser.Username.Equals("sysadmin", StringComparison.OrdinalIgnoreCase);
    
    // 命令
    public DelegateCommand LoadUsersCommand { get; }
    public DelegateCommand AddUserCommand { get; }
    public DelegateCommand<UserDto> EditUserCommand { get; }
    public DelegateCommand<UserDto> ViewUserCommand { get; }
    public DelegateCommand<UserDto> DeleteUserCommand { get; }
    public DelegateCommand<UserDto> ToggleStatusCommand { get; }
    public DelegateCommand<UserDto> ResetPasswordCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand BatchEnableCommand { get; }
    public DelegateCommand BatchDisableCommand { get; }
    
    // 分页命令
    public DelegateCommand FirstPageCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }
    public DelegateCommand LastPageCommand { get; }
}
```

#### 核心方法
```csharp
// 加载用户列表
private async Task LoadUsersAsync()
{
    try
    {
        IsLoading = true;
        
        var query = new UserPagedQueryDto
        {
            PageIndex = CurrentPage,
            PageSize = PageSize,
            Keyword = SearchKeyword?.Trim()
        };
        
        var result = await _userModule.GetPagedAsync(query);
        
        if (result.IsSuccess && result.Data != null)
        {
            Users.Clear();
            foreach (var user in result.Data.Items)
            {
                Users.Add(user);
            }
            
            TotalCount = result.Data.TotalCount;
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(HasUsers));
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? "加载用户列表失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"加载用户列表异常: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    finally
    {
        IsLoading = false;
    }
}

// 添加用户
private void AddUser()
{
    var dialogParameters = new DialogParameters();
    
    _dialogService.ShowDialog(nameof(UserAddEditDialog), dialogParameters, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            // 刷新用户列表
            LoadUsersCommand.Execute();
        }
    });
}

// 编辑用户
private void EditUser(UserDto user)
{
    if (user == null) return;
    
    var dialogParameters = new DialogParameters
    {
        { "User", user },
        { "IsEditMode", true }
    };
    
    _dialogService.ShowDialog(nameof(UserAddEditDialog), dialogParameters, result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            // 刷新用户列表
            LoadUsersCommand.Execute();
        }
    });
}

// 删除用户
private async void DeleteUser(UserDto user)
{
    if (user == null) return;
    
    // 确认删除
    var confirmResult = MessageBox.Show(
        $"确定要删除用户 '{user.Username} ({user.RealName})' 吗？\n\n注意：这将禁用该用户账户。",
        "确认删除",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);
    
    if (confirmResult != MessageBoxResult.Yes) return;
    
    try
    {
        var result = await _userModule.DeleteAsync(user.Id);
        
        if (result.IsSuccess)
        {
            MessageBox.Show("用户删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadUsersCommand.Execute();
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? "删除用户失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"删除用户异常: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

// 切换用户状态
private async void ToggleUserStatus(UserDto user)
{
    if (user == null) return;
    
    try
    {
        ServiceResult<bool> result;
        string action;
        
        if (user.IsActive)
        {
            result = await _userModule.DisableAsync(user.Id);
            action = "禁用";
        }
        else
        {
            result = await _userModule.EnableAsync(user.Id);
            action = "启用";
        }
        
        if (result.IsSuccess)
        {
            MessageBox.Show($"用户{action}成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadUsersCommand.Execute();
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? $"{action}用户失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"操作用户状态异常: {ex.Message}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### 3. UserAddEditDialogViewModel (用户添加/编辑对话框)

#### 主要功能
- **用户信息编辑**: 创建/编辑用户基本信息
- **数据验证**: 实时验证用户输入
- **角色选择**: 角色下拉选择
- **状态管理**: 启用/禁用状态选择

#### 核心属性
```csharp
public class UserAddEditDialogViewModel : IDialogAware
{
    // 依赖服务
    private readonly UserModule _userModule;
    
    // 数据绑定属性
    public UserMutationDto UserData { get; set; } = new();
    public List<object> AvailableRoles { get; set; } = new();
    public object SelectedRole { get; set; }
    public List<object> AvailableStatuses { get; set; } = new();
    public object SelectedStatus { get; set; }
    
    // UI状态属性
    public bool IsEditMode { get; set; }
    public bool IsSaving { get; set; }
    public string DialogTitle => IsEditMode ? "编辑用户" : "添加用户";
    public string SaveButtonText => IsEditMode ? "保存" : "创建";
    public bool CanChangeUsername => !IsEditMode; // 编辑模式下不能修改用户名
    
    // 验证属性
    public string UsernameError { get; set; }
    public string RealNameError { get; set; }
    public string PasswordError { get; set; }
    public string PhoneNumberError { get; set; }
    public string EmailError { get; set; }
    
    // 命令
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand ValidateUsernameCommand { get; }
}
```

#### 核心方法
```csharp
// 保存用户
private async void SaveUser()
{
    try
    {
        IsSaving = true;
        
        // 客户端验证
        if (!ValidateInput())
        {
            return;
        }
        
        // 设置操作类型
        UserData.IsCreateOperation = !IsEditMode;
        
        ServiceResult<UserDto> result;
        
        if (IsEditMode)
        {
            result = await _userModule.UpdateAsync(UserData);
        }
        else
        {
            result = await _userModule.CreateAsync(UserData);
        }
        
        if (result.IsSuccess)
        {
            MessageBox.Show(
                IsEditMode ? "用户更新成功" : "用户创建成功", 
                "成功", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
            
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }
        else
        {
            MessageBox.Show(
                result.ErrorMessage ?? (IsEditMode ? "更新用户失败" : "创建用户失败"), 
                "错误", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show(
            $"{(IsEditMode ? "更新" : "创建")}用户异常: {ex.Message}", 
            "异常", 
            MessageBoxButton.OK, 
            MessageBoxImage.Error);
    }
    finally
    {
        IsSaving = false;
    }
}

// 输入验证
private bool ValidateInput()
{
    bool isValid = true;
    
    // 重置错误信息
    UsernameError = string.Empty;
    RealNameError = string.Empty;
    PasswordError = string.Empty;
    PhoneNumberError = string.Empty;
    EmailError = string.Empty;
    
    // 用户名验证
    if (string.IsNullOrWhiteSpace(UserData.Username))
    {
        UsernameError = "用户名不能为空";
        isValid = false;
    }
    else if (UserData.Username.Length < 3 || UserData.Username.Length > 50)
    {
        UsernameError = "用户名长度必须在3到50个字符之间";
        isValid = false;
    }
    
    // 真实姓名验证
    if (string.IsNullOrWhiteSpace(UserData.RealName))
    {
        RealNameError = "真实姓名不能为空";
        isValid = false;
    }
    else if (UserData.RealName.Length > 50)
    {
        RealNameError = "真实姓名长度不能超过50个字符";
        isValid = false;
    }
    
    // 密码验证（仅创建模式）
    if (!IsEditMode && string.IsNullOrWhiteSpace(UserData.Password))
    {
        PasswordError = "创建用户时密码不能为空";
        isValid = false;
    }
    else if (!string.IsNullOrWhiteSpace(UserData.Password) && UserData.Password.Length < 6)
    {
        PasswordError = "密码长度不能少于6个字符";
        isValid = false;
    }
    
    // 手机号验证
    if (!string.IsNullOrWhiteSpace(UserData.PhoneNumber))
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(UserData.PhoneNumber, @"^1[3-9]\d{9}$"))
        {
            PhoneNumberError = "请输入有效的手机号码";
            isValid = false;
        }
    }
    
    // 邮箱验证
    if (!string.IsNullOrWhiteSpace(UserData.Email))
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(UserData.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            EmailError = "请输入有效的邮箱地址";
            isValid = false;
        }
    }
    
    return isValid;
}

// 异步验证用户名
private async void ValidateUsername()
{
    if (IsEditMode || string.IsNullOrWhiteSpace(UserData.Username))
    {
        UsernameError = string.Empty;
        return;
    }
    
    try
    {
        var result = await _userModule.ValidateUsernameAsync(UserData.Username);
        
        if (result.IsSuccess)
        {
            if (!result.Data) // false表示用户名已存在
            {
                UsernameError = "该用户名已被使用";
            }
            else
            {
                UsernameError = string.Empty;
            }
        }
    }
    catch (Exception ex)
    {
        // 验证异常时不显示错误，避免影响用户体验
        System.Diagnostics.Debug.WriteLine($"验证用户名异常: {ex.Message}");
    }
}
```

### 4. Views (WPF界面)

#### UserManagementView.xaml (主界面布局)
```xml
<UserControl x:Class="LYBT.Desktop.Users.Views.UserManagementView"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 搜索工具栏 -->
            <RowDefinition Height="*"/>     <!-- 用户列表 -->
            <RowDefinition Height="Auto"/>  <!-- 分页控件 -->
        </Grid.RowDefinitions>
        
        <!-- 搜索工具栏 -->
        <Border Grid.Row="0" Background="#F8F9FA" Padding="15" Margin="0,0,0,10">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <!-- 搜索框 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal">
                    <TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                             Width="300"
                             Height="35"
                             VerticalContentAlignment="Center"
                             Padding="10,5"
                             BorderBrush="#DDD"
                             BorderThickness="1"
                             FontSize="13"
                             Margin="0,0,10,0">
                        <TextBox.Style>
                            <Style TargetType="TextBox">
                                <Style.Triggers>
                                    <Trigger Property="Text" Value="">
                                        <Setter Property="Background" Value="LightGray"/>
                                    </Trigger>
                                </Style.Triggers>
                            </Style>
                        </TextBox.Style>
                        <TextBox.InputBindings>
                            <KeyBinding Key="Enter" Command="{Binding SearchCommand}"/>
                        </TextBox.InputBindings>
                    </TextBox>
                    
                    <Button Content="搜索"
                            Command="{Binding SearchCommand}"
                            Width="80"
                            Height="35"
                            Margin="0,0,10,0"
                            Background="#007BFF"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="13"/>
                    
                    <Button Content="刷新"
                            Command="{Binding RefreshCommand}"
                            Width="80"
                            Height="35"
                            Background="#28A745"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="13"/>
                </StackPanel>
                
                <!-- 操作按钮 -->
                <StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Button Content="添加用户"
                            Command="{Binding AddUserCommand}"
                            Width="100"
                            Height="35"
                            Margin="0,0,10,0"
                            Background="#28A745"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="13"/>
                    
                    <Button Content="批量启用"
                            Command="{Binding BatchEnableCommand}"
                            Width="100"
                            Height="35"
                            Margin="0,0,10,0"
                            Background="#007BFF"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="13"/>
                    
                    <Button Content="批量禁用"
                            Command="{Binding BatchDisableCommand}"
                            Width="100"
                            Height="35"
                            Background="#DC3545"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="13"/>
                </StackPanel>
            </Grid>
        </Border>
        
        <!-- 用户列表 -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Users}"
                  SelectedItem="{Binding SelectedUser}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  IsReadOnly="True"
                  GridLinesVisibility="Horizontal"
                  HorizontalGridLinesBrush="#E5E5E5"
                  RowHeight="45"
                  FontSize="13">
            
            <DataGrid.Columns>
                <DataGridCheckBoxColumn Header="选择" Width="60" Binding="{Binding IsSelected}"/>
                
                <DataGridTextColumn Header="用户名" Width="120" Binding="{Binding Username}">
                    <DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Setter Property="FontWeight" Value="Bold"/>
                            <Setter Property="Foreground" Value="#2E86AB"/>
                        </Style>
                    </DataGridTextColumn.ElementStyle>
                </DataGridTextColumn>
                
                <DataGridTextColumn Header="真实姓名" Width="120" Binding="{Binding RealName}"/>
                
                <DataGridTextColumn Header="角色" Width="80" Binding="{Binding Role}">
                    <DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Style.Triggers>
                                <DataTrigger Binding="{Binding Role}" Value="Admin">
                                    <Setter Property="Foreground" Value="#DC3545"/>
                                    <Setter Property="FontWeight" Value="Bold"/>
                                </DataTrigger>
                                <DataTrigger Binding="{Binding Role}" Value="Doctor">
                                    <Setter Property="Foreground" Value="#28A745"/>
                                </DataTrigger>
                            </Style.Triggers>
                        </Style>
                    </DataGridTextColumn.ElementStyle>
                </DataGridTextColumn>
                
                <DataGridTextColumn Header="手机号" Width="120" Binding="{Binding PhoneNumber}"/>
                <DataGridTextColumn Header="邮箱" Width="180" Binding="{Binding Email}"/>
                
                <DataGridTemplateColumn Header="状态" Width="80">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Border CornerRadius="12" Padding="8,4" HorizontalAlignment="Center">
                                <Border.Style>
                                    <Style TargetType="Border">
                                        <Style.Triggers>
                                            <DataTrigger Binding="{Binding IsActive}" Value="True">
                                                <Setter Property="Background" Value="#D4EDDA"/>
                                            </DataTrigger>
                                            <DataTrigger Binding="{Binding IsActive}" Value="False">
                                                <Setter Property="Background" Value="#F8D7DA"/>
                                            </DataTrigger>
                                        </Style.Triggers>
                                    </Style>
                                </Border.Style>
                                <TextBlock Text="{Binding IsActive, Converter={StaticResource BooleanToActiveStatusConverter}}"
                                           FontSize="11"
                                           FontWeight="Bold">
                                    <TextBlock.Style>
                                        <Style TargetType="TextBlock">
                                            <Style.Triggers>
                                                <DataTrigger Binding="{Binding IsActive}" Value="True">
                                                    <Setter Property="Foreground" Value="#155724"/>
                                                </DataTrigger>
                                                <DataTrigger Binding="{Binding IsActive}" Value="False">
                                                    <Setter Property="Foreground" Value="#721C24"/>
                                                </DataTrigger>
                                            </Style.Triggers>
                                        </Style>
                                    </TextBlock.Style>
                                </TextBlock>
                            </Border>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                
                <DataGridTextColumn Header="创建时间" Width="140" Binding="{Binding CreateTime, StringFormat=yyyy-MM-dd HH:mm}"/>
                
                <DataGridTemplateColumn Header="操作" Width="180">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                <Button Content="查看"
                                        Command="{Binding DataContext.ViewUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Width="50"
                                        Height="25"
                                        Margin="2"
                                        Background="#17A2B8"
                                        Foreground="White"
                                        BorderThickness="0"
                                        FontSize="11"/>
                                
                                <Button Content="编辑"
                                        Command="{Binding DataContext.EditUserCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Width="50"
                                        Height="25"
                                        Margin="2"
                                        Background="#007BFF"
                                        Foreground="White"
                                        BorderThickness="0"
                                        FontSize="11"/>
                                
                                <Button Content="{Binding IsActive, Converter={StaticResource BooleanToToggleStatusTextConverter}}"
                                        Command="{Binding DataContext.ToggleStatusCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Width="50"
                                        Height="25"
                                        Margin="2"
                                        Background="{Binding IsActive, Converter={StaticResource BooleanToToggleStatusBackgroundConverter}}"
                                        Foreground="White"
                                        BorderThickness="0"
                                        FontSize="11"/>
                                
                                <Button Content="重置"
                                        Command="{Binding DataContext.ResetPasswordCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                        CommandParameter="{Binding}"
                                        Width="50"
                                        Height="25"
                                        Margin="2"
                                        Background="#FFC107"
                                        Foreground="#212529"
                                        BorderThickness="0"
                                        FontSize="11"/>
                            </StackPanel>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
        
        <!-- 分页控件 -->
        <Border Grid.Row="2" Background="#F8F9FA" Padding="15" Margin="0,10,0,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <!-- 统计信息 -->
                <TextBlock Grid.Column="0"
                           Text="{Binding TotalCount, StringFormat='共 {0} 条记录'}"
                           VerticalAlignment="Center"
                           FontSize="13"
                           Foreground="#666"/>
                
                <!-- 分页按钮 -->
                <StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Button Content="首页"
                            Command="{Binding FirstPageCommand}"
                            Width="60"
                            Height="30"
                            Margin="5,0"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="12"/>
                    
                    <Button Content="上一页"
                            Command="{Binding PreviousPageCommand}"
                            Width="60"
                            Height="30"
                            Margin="5,0"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="12"/>
                    
                    <TextBlock Text="{Binding CurrentPage, StringFormat='第 {0} 页'}"
                               VerticalAlignment="Center"
                               Margin="10,0"
                               FontSize="13"/>
                    
                    <TextBlock Text="{Binding TotalPages, StringFormat='共 {0} 页'}"
                               VerticalAlignment="Center"
                               Margin="10,0"
                               FontSize="13"/>
                    
                    <Button Content="下一页"
                            Command="{Binding NextPageCommand}"
                            Width="60"
                            Height="30"
                            Margin="5,0"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="12"/>
                    
                    <Button Content="末页"
                            Command="{Binding LastPageCommand}"
                            Width="60"
                            Height="30"
                            Margin="5,0"
                            Background="#6C757D"
                            Foreground="White"
                            BorderThickness="0"
                            FontSize="12"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

## 🔧 依赖注入配置

### 1. 模块注册
```csharp
// UsersModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // UltraThink修复：模块自己注册服务接口实现
    containerRegistry.RegisterSingleton<UserModule>();
    containerRegistry.RegisterSingleton<IUserService>(container => container.Resolve<UserModule>());
    
    // 注册视图和视图模型
    containerRegistry.RegisterForNavigation<UserManagementView, UserManagementViewModel>();
    containerRegistry.RegisterForNavigation<UserAddEditDialog, UserAddEditDialogViewModel>();
}
```

### 2. 服务依赖
```csharp
// UserModule构造函数依赖
public UserModule(
    IUserApi apiService,    // API客户端 (来自Desktop.Services)
    IMapper mapper)         // 对象映射 (AutoMapper)
```

### 3. ViewModel依赖
```csharp
// UserManagementViewModel构造函数依赖
public UserManagementViewModel(
    UserModule userModule,              // 用户业务服务
    IDialogService dialogService,       // 对话框服务 (Prism)
    IEventAggregator eventAggregator)   // 事件聚合器 (Prism)
```

## 📊 性能特性

### 1. 分页查询优化
```csharp
// 分页查询减少内存占用
public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query)
{
    // 使用合理的页面大小，避免一次性加载大量数据
    var baseQuery = new PagedQueryBaseDto
    {
        PageIndex = query.PageIndex,
        PageSize = Math.Min(query.PageSize, 100), // 限制最大页面大小
        Keyword = query.Keyword
    };
    
    // 异步调用API，不阻塞UI线程
    var apiResponse = await _apiService.GetUsersAsync(
        page: baseQuery.PageIndex,
        pageSize: baseQuery.PageSize,
        keyword: baseQuery.Keyword);
    
    return ProcessApiResponse(apiResponse);
}
```

### 2. 异步操作
```csharp
// 所有数据库操作都使用异步方法
private async Task LoadUsersAsync()
{
    try
    {
        IsLoading = true;
        
        var query = new UserPagedQueryDto
        {
            PageIndex = CurrentPage,
            PageSize = PageSize,
            Keyword = SearchKeyword?.Trim()
        };
        
        // 异步调用，不阻塞UI
        var result = await _userModule.GetPagedAsync(query);
        
        // UI更新在主线程执行
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            UpdateUserList(result);
        });
    }
    finally
    {
        IsLoading = false;
    }
}
```

### 3. 智能缓存
```csharp
// 角色列表缓存，避免重复加载
private List<object> _cachedRoles;

public async Task LoadRolesAsync()
{
    if (_cachedRoles == null)
    {
        var result = await _userModule.GetRolesAsync();
        if (result.IsSuccess)
        {
            _cachedRoles = result.Data;
        }
    }
    
    AvailableRoles = _cachedRoles ?? new List<object>();
}
```

## 🧪 测试支持

### 1. 单元测试结构
```csharp
[TestClass]
public class UserModuleTests
{
    private Mock<IUserApi> _mockUserApi;
    private Mock<IMapper> _mockMapper;
    private UserModule _userModule;

    [TestInitialize]
    public void Setup()
    {
        _mockUserApi = new Mock<IUserApi>();
        _mockMapper = new Mock<IMapper>();
        
        _userModule = new UserModule(_mockUserApi.Object, _mockMapper.Object);
    }

    [TestMethod]
    public async Task GetByIdAsync_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new UserDto 
        { 
            Id = userId, 
            Username = "testuser", 
            RealName = "Test User" 
        };
        
        var apiResponse = new ApiResponse<UserDto>
        {
            Success = true,
            Data = expectedUser
        };
        
        _mockUserApi.Setup(x => x.GetUserByIdAsync(userId))
                   .ReturnsAsync(new ApiResponse<ApiResponse<UserDto>>(apiResponse, HttpStatusCode.OK));

        // Act
        var result = await _userModule.GetByIdAsync(userId);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("testuser", result.Data.Username);
    }

    [TestMethod]
    public async Task CreateAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var userDto = new UserMutationDto
        {
            Username = "newuser",
            RealName = "New User",
            Password = "password123",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            IsCreateOperation = true
        };

        var createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = userDto.Username,
            RealName = userDto.RealName
        };

        var apiResponse = new ApiResponse<UserDto>
        {
            Success = true,
            Data = createdUser
        };

        _mockUserApi.Setup(x => x.CreateUserAsync(It.IsAny<UserMutationDto>()))
                   .ReturnsAsync(new ApiResponse<ApiResponse<UserDto>>(apiResponse, HttpStatusCode.OK));

        // Act
        var result = await _userModule.CreateAsync(userDto);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("newuser", result.Data.Username);
    }
}
```

### 2. ViewModel测试
```csharp
[TestClass]
public class UserManagementViewModelTests
{
    private Mock<UserModule> _mockUserModule;
    private Mock<IDialogService> _mockDialogService;
    private UserManagementViewModel _viewModel;

    [TestMethod]
    public async Task LoadUsersCommand_Execute_LoadsUsers()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), Username = "user1", RealName = "User 1" },
            new UserDto { Id = Guid.NewGuid(), Username = "user2", RealName = "User 2" }
        };

        var pagedResult = new PagedResult<UserDto>(users, 2, 1, 20);
        var serviceResult = ServiceResult<PagedResult<UserDto>>.Success(pagedResult);

        _mockUserModule.Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>()))
                      .ReturnsAsync(serviceResult);

        // Act
        _viewModel.LoadUsersCommand.Execute();
        await Task.Delay(100); // 等待异步操作完成

        // Assert
        Assert.AreEqual(2, _viewModel.Users.Count);
        Assert.AreEqual("user1", _viewModel.Users[0].Username);
        Assert.AreEqual(2, _viewModel.TotalCount);
    }
}
```

## 📝 使用示例

### 1. 基本用户管理
```csharp
// 创建用户
var newUser = new UserMutationDto
{
    Username = "doctor01",
    RealName = "张医生",
    Password = "Doctor@123456",
    PhoneNumber = "13800138001",
    Email = "doctor01@hospital.com",
    Role = UserRole.Doctor,
    Status = CommonStatus.Enabled,
    IsCreateOperation = true
};

var createResult = await userModule.CreateAsync(newUser);
if (createResult.IsSuccess)
{
    Console.WriteLine($"用户创建成功: {createResult.Data.Username}");
}

// 查询用户列表
var query = new UserPagedQueryDto
{
    PageIndex = 1,
    PageSize = 20,
    Keyword = "张"
};

var queryResult = await userModule.GetPagedAsync(query);
if (queryResult.IsSuccess)
{
    Console.WriteLine($"找到 {queryResult.Data.TotalCount} 个用户");
    foreach (var user in queryResult.Data.Items)
    {
        Console.WriteLine($"- {user.Username}: {user.RealName}");
    }
}
```

### 2. 密码管理
```csharp
// 重置用户密码
var resetResult = await userModule.ResetPasswordAsync(userId, "NewPassword@123");
if (resetResult.IsSuccess)
{
    Console.WriteLine("密码重置成功");
}

// 修改用户密码
var changeResult = await userModule.ChangePasswordAsync(
    userId, 
    "OldPassword@123", 
    "NewPassword@456");
    
if (changeResult.IsSuccess)
{
    Console.WriteLine("密码修改成功");
}

// 修改个人资料
var profileDto = new ChangeProfileDto
{
    UserId = currentUser.Id,
    RealName = "新姓名",
    PhoneNumber = "13900139001",
    Email = "newemail@example.com"
};

var profileResult = await userModule.ChangeProfileAsync(profileDto);
if (profileResult.IsSuccess)
{
    Console.WriteLine("个人资料修改成功");
}
```

### 3. 批量操作
```csharp
// 批量启用用户
var userIds = new List<Guid> { user1.Id, user2.Id, user3.Id };
var enableResult = await userModule.BatchEnableAsync(userIds);
if (enableResult.IsSuccess)
{
    Console.WriteLine($"成功启用 {enableResult.Data} 个用户");
}

// 批量禁用用户
var disableResult = await userModule.BatchDisableAsync(userIds);
if (disableResult.IsSuccess)
{
    Console.WriteLine($"成功禁用 {disableResult.Data} 个用户");
}

// 搜索活跃用户
var activeUsersResult = await userModule.GetActiveUsersAsync();
if (activeUsersResult.IsSuccess)
{
    Console.WriteLine($"当前有 {activeUsersResult.Data.Count} 个活跃用户");
    foreach (var user in activeUsersResult.Data)
    {
        Console.WriteLine($"- {user.Username} ({user.Role})");
    }
}
```

### 4. 角色管理
```csharp
// 获取所有可用角色
var rolesResult = await userModule.GetRolesAsync();
if (rolesResult.IsSuccess)
{
    Console.WriteLine("可用角色:");
    foreach (dynamic role in rolesResult.Data)
    {
        Console.WriteLine($"- {role.Text} (值: {role.EnumValue})");
    }
}

// 验证用户名可用性
var validateResult = await userModule.ValidateUsernameAsync("newusername");
if (validateResult.IsSuccess)
{
    if (validateResult.Data)
    {
        Console.WriteLine("用户名可用");
    }
    else
    {
        Console.WriteLine("用户名已被使用");
    }
}
```

## 🔄 版本历史

- **v1.0.0** - 初始版本，基础用户CRUD功能
- **v1.1.0** - 添加分页查询和搜索功能
- **v1.2.0** - 添加密码管理和角色分配
- **v2.0.0** - UltraThink架构重构，优化API调用
- **v2.1.0** - 添加批量操作和状态管理
- **v2.2.0** - 完善个人资料管理和数据验证
- **v2.3.0** - 优化用户界面和用户体验

## 📚 相关文档

- [项目文档标准](../../PROJECT_DOCUMENTATION_STANDARDS.md)
- [Desktop.Services文档](../core/desktop-services.md)
- [Shared.Models文档](../../shared/models.md)
- [后端Users模块文档](../../backend/modules/users.md)
- [Auth模块文档](./auth.md)