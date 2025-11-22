# 用户管理功能技术设计文档

**Phase 3: Admin用户管理**

**文档版本**: v1.0
**创建日期**: 2025-11-07
**关联需求**: [user-management-requirements.md](./user-management-requirements.md)
**关联Epic**: #1886 用户自我维护与管理员用户管理

---

## 📋 目录

- [1. 设计概述](#1-设计概述)
- [2. Server端设计](#2-server端设计)
- [3. Client端设计](#3-client端设计)
- [4. 数据库设计](#4-数据库设计)
- [5. 安全设计](#5-安全设计)
- [6. 实施Phase规划](#6-实施phase规划)

---

## 1. 设计概述

### 1.1 文档目的

本文档定义Phase 3（Admin用户管理）的技术实现方案，包括Server端API、Client端UI、数据库结构和安全机制。

### 1.2 核心决策摘要（12个确认）

| 决策编号 | 决策内容 | 影响范围 |
|---------|---------|---------|
| Q1 | 统一默认密码（从配置文件读取） | Server/Client/UI |
| Q2 | 软删除方案（IsDeleted标志） | 数据库/P2 |
| Q3 | 不需要导入/导出功能 | P3排除 |
| Q4 | 密码复杂度：8位+大小写+数字+特殊字符 | Server验证 |
| Q5 | sysadmin密码重置工具（生成SQL语句） | 独立Console App |
| Q6 | 统一管理UI（Role差异化显示） | Client UI |
| Q7 | Status管理（Enabled/Disabled） | Server/Client |
| Q8 | PinYinCode首字母大写+允许重复 | Server逻辑 |
| Q9 | 电话号码11位数字 | Server验证 |
| Q10 | 邮箱标准格式（RFC 5322） | Server验证 |
| Q11 | 首次登录强制修改密码（P3） | P3排除 |
| Q12 | 不生成临时密码 | Server逻辑 |

### 1.3 架构分层

```
┌─────────────────────────────────────────┐
│         Client端 (WPF + Prism)          │
├─────────────────────────────────────────┤
│  UserManagementView (主视图)            │
│  ├─ UserManagementViewModel             │
│  ├─ CreateUserDialog                    │
│  ├─ EditUserDialog                      │
│  └─ ResetPasswordDialog (确认)          │
└─────────────────────────────────────────┘
              ↕ HTTP/REST API
┌─────────────────────────────────────────┐
│         Server端 (ASP.NET Core)         │
├─────────────────────────────────────────┤
│  Controllers: UsersController           │
│  Services: UserService                  │
│  Repositories: UserRepository           │
│  DTOs: CreateUserInputDto, etc.         │
└─────────────────────────────────────────┘
              ↕ EF Core
┌─────────────────────────────────────────┐
│         数据库 (SQL Server)              │
├─────────────────────────────────────────┤
│  Users 表                                │
│  AdminSecrets 表 (sysadmin)             │
│  RefreshTokens 表 (Token管理)           │
│  SecurityAuditLog 表 (安全审计)         │
└─────────────────────────────────────────┘
```

---

## 2. Server端设计

### 2.1 API端点设计

#### 2.1.1 用户CRUD端点

```csharp
// UsersController.cs

/// <summary>
/// 获取用户列表（分页+筛选）
/// GET /api/users/admin/list?pageIndex=1&pageSize=20&role=Doctor&status=Enabled&searchTerm=张
/// </summary>
[HttpGet("admin/list")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsersList(
    [FromQuery] UserQueryDto query)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.GetUsersAsync(query, currentUser);
    return Ok(ApiResponse<PagedResult<UserDto>>.Success(result));
}

/// <summary>
/// 创建新用户
/// POST /api/users/admin/create
/// </summary>
[HttpPost("admin/create")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
    [FromBody] CreateUserInputDto dto)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.CreateUserAsync(dto, currentUser);

    if (!result.IsSuccess)
        return BadRequest(ApiResponse<UserDto>.Failure(result.Message));

    return Ok(ApiResponse<UserDto>.Success(result.Data));
}

/// <summary>
/// 编辑用户信息
/// PUT /api/users/admin/{id}
/// </summary>
[HttpPut("admin/{id}")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
    Guid id,
    [FromBody] EditUserInputDto dto)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.AdminUpdateUserAsync(id, dto, currentUser);

    if (!result.IsSuccess)
        return BadRequest(ApiResponse<UserDto>.Failure(result.Message));

    return Ok(ApiResponse<UserDto>.Success(result.Data));
}

/// <summary>
/// 重置用户密码
/// POST /api/users/admin/{id}/reset-password
/// </summary>
[HttpPost("admin/{id}/reset-password")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<string>>> ResetPassword(Guid id)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.ResetUserPasswordAsync(id, currentUser);

    if (!result.IsSuccess)
        return BadRequest(ApiResponse<string>.Failure(result.Message));

    return Ok(ApiResponse<string>.Success("密码已重置为默认密码，所有Token已撤销。"));
}

/// <summary>
/// 启用/禁用用户
/// POST /api/users/admin/{id}/set-status
/// </summary>
[HttpPost("admin/{id}/set-status")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ApiResponse<string>>> SetUserStatus(
    Guid id,
    [FromBody] SetUserStatusDto dto)
{
    var currentUser = await GetCurrentUserAsync();
    var result = await _userService.SetUserStatusAsync(id, dto.Status, currentUser);

    if (!result.IsSuccess)
        return BadRequest(ApiResponse<string>.Failure(result.Message));

    return Ok(ApiResponse<string>.Success($"用户已{(dto.Status == CommonStatus.Enabled ? "启用" : "禁用")}"));
}
```

### 2.2 DTO定义

#### 2.2.1 输入DTO

```csharp
// UserDtos.cs

/// <summary>
/// 创建用户输入DTO（Q1决策：不需要密码字段，使用默认密码）
/// </summary>
public class CreateUserInputDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度3-50字符")]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "用户名只能包含字母和数字")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "真实姓名长度2-50字符")]
    public string RealName { get; set; } = string.Empty;

    [Required(ErrorMessage = "角色不能为空")]
    public UserRole Role { get; set; }

    [Phone(ErrorMessage = "电话号码格式不正确")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "电话号码必须为11位数字")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(100, ErrorMessage = "邮箱长度不能超过100字符")]
    public string? Email { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过500字符")]
    public string? Remark { get; set; }
}

/// <summary>
/// 编辑用户输入DTO
/// </summary>
public class EditUserInputDto
{
    [Required(ErrorMessage = "真实姓名不能为空")]
    [StringLength(50, MinimumLength = 2)]
    public string RealName { get; set; } = string.Empty;

    [Required(ErrorMessage = "角色不能为空")]
    public UserRole Role { get; set; }

    [Phone]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "电话号码必须为11位数字")]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required]
    public CommonStatus Status { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 用户查询DTO（分页+筛选）
/// </summary>
public class UserQueryDto
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; } // 搜索UserName或RealName
    public UserRole? Role { get; set; } // 角色筛选
    public CommonStatus? Status { get; set; } // 状态筛选
}

/// <summary>
/// 设置用户状态DTO
/// </summary>
public class SetUserStatusDto
{
    [Required]
    public CommonStatus Status { get; set; }
}
```

### 2.3 Service层方法

```csharp
// IUserService.cs 新增方法

public interface IUserService
{
    // === 现有方法（Phase 2已实现）===
    Task<Result<UserDto>> GetUserAsync(Guid id);
    Task<Result<UserDto>> UpdateAsync(UserInputDto dto);
    Task<Result<UserDto>> ChangeProfileAsync(Guid userId, ChangeProfileDto dto);

    // === Phase 3新增方法 ===

    /// <summary>
    /// 获取用户列表（管理员功能，带权限过滤）
    /// </summary>
    Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query, User? currentUser);

    /// <summary>
    /// 创建新用户（管理员功能）
    /// </summary>
    Task<Result<UserDto>> CreateUserAsync(CreateUserInputDto dto, User? currentUser);

    /// <summary>
    /// 管理员编辑用户
    /// </summary>
    Task<Result<UserDto>> AdminUpdateUserAsync(Guid userId, EditUserInputDto dto, User? currentUser);

    /// <summary>
    /// 重置用户密码（管理员功能）
    /// </summary>
    Task<Result> ResetUserPasswordAsync(Guid userId, User? currentUser);

    /// <summary>
    /// 启用/禁用用户（管理员功能）
    /// </summary>
    Task<Result> SetUserStatusAsync(Guid userId, CommonStatus status, User? currentUser);
}
```

### 2.4 Service层实现核心逻辑

#### 2.4.1 创建用户

```csharp
// UserService.cs

public async Task<Result<UserDto>> CreateUserAsync(CreateUserInputDto dto, User? currentUser)
{
    // 1. 权限检查
    if (currentUser != null && currentUser.Role == UserRole.Admin && dto.Role == UserRole.Admin)
    {
        return Result<UserDto>.Failure("Admin用户不能创建其他Admin用户");
    }

    // 2. 唯一性验证
    if (await _dbContext.Users.AnyAsync(u => u.UserName == dto.UserName))
        return Result<UserDto>.Failure("用户名已存在");

    if (!string.IsNullOrEmpty(dto.PhoneNumber))
    {
        if (await _dbContext.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            return Result<UserDto>.Failure("电话号码已被其他用户使用");
    }

    if (!string.IsNullOrEmpty(dto.Email))
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email == dto.Email))
            return Result<UserDto>.Failure("邮箱地址已被其他用户使用");
    }

    // 3. 架构保护：禁止创建sysadmin
    if (dto.UserName?.ToLower() == "sysadmin")
        return Result<UserDto>.Failure("禁止创建sysadmin用户，sysadmin是虚拟超级用户");

    // 4. 读取默认密码（Q1决策）
    var defaultPassword = _configuration["UserManagement:DefaultPassword"];
    if (string.IsNullOrEmpty(defaultPassword))
        return Result<UserDto>.Failure("系统配置错误：未配置默认密码");

    // 5. 生成PinYinCode（Q8决策：首字母大写）
    var pinYinCode = PinYinHelper.GetFirstLetters(dto.RealName).ToUpper();

    // 6. 创建用户实体
    var user = new User
    {
        Id = Guid.NewGuid(),
        UserName = dto.UserName,
        RealName = dto.RealName,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
        Role = dto.Role,
        PhoneNumber = dto.PhoneNumber,
        Email = dto.Email,
        PinYinCode = pinYinCode,
        Status = CommonStatus.Enabled,
        Remark = dto.Remark,
        CreatedAt = DateTime.UtcNow
    };

    await _dbContext.Users.AddAsync(user);
    await _dbContext.SaveChangesAsync();

    _logger.LogInformation("用户创建成功: {UserName} by {Creator}",
        user.UserName,
        currentUser?.UserName ?? "sysadmin");

    return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
}
```

#### 2.4.2 重置用户密码（含Token撤销）

```csharp
public async Task<Result> ResetUserPasswordAsync(Guid userId, User? currentUser)
{
    // 1. 查找用户
    var user = await _dbContext.Users.FindAsync(userId);
    if (user == null)
        return Result.Failure("用户不存在");

    // 2. 权限检查
    if (!CanManageUser(currentUser, user))
        return Result.Failure("无权限重置该用户密码");

    // 3. 读取默认密码
    var defaultPassword = _configuration["UserManagement:DefaultPassword"];
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

    // 4. ⭐ 撤销所有RefreshToken（关键安全操作）
    var userTokens = await _dbContext.RefreshTokens
        .Where(t => t.UserId == userId && !t.IsRevoked)
        .ToListAsync();

    foreach (var token in userTokens)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "管理员重置密码";
    }

    // 5. 记录安全审计日志
    _dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
    {
        Id = Guid.NewGuid(),
        EventType = "PasswordReset",
        UserId = userId,
        PerformedBy = currentUser?.Id ?? Guid.Empty,
        Timestamp = DateTime.UtcNow,
        Details = $"管理员{currentUser?.UserName ?? "sysadmin"}重置了用户{user.UserName}的密码，所有Token已撤销"
    });

    await _dbContext.SaveChangesAsync();

    _logger.LogWarning("密码重置: User={UserName}, ResetBy={Admin}",
        user.UserName,
        currentUser?.UserName ?? "sysadmin");

    return Result.Success();
}
```

#### 2.4.3 权限验证逻辑

```csharp
// UserService.cs

/// <summary>
/// 检查当前用户是否有权限管理目标用户
/// </summary>
private bool CanManageUser(User? currentUser, User targetUser)
{
    // sysadmin可以操作所有用户
    if (currentUser == null || currentUser.Id == Guid.Empty)
        return true;

    // 不能操作自己的关键字段（Role、Status）
    // 注：个人信息修改走ChangeProfileAsync，这里是管理员功能
    if (currentUser.Id == targetUser.Id)
        return false;

    // Admin只能操作非Admin用户
    if (currentUser.Role == UserRole.Admin)
    {
        return targetUser.Role != UserRole.Admin;
    }

    return false;
}

/// <summary>
/// 获取当前用户可见的用户查询（权限过滤）
/// </summary>
private IQueryable<User> GetVisibleUsersQuery(User? currentUser)
{
    var query = _dbContext.Users.AsQueryable();

    // sysadmin可以看到所有用户
    if (currentUser == null || currentUser.Id == Guid.Empty)
        return query;

    // Admin只能看到非Admin用户
    if (currentUser.Role == UserRole.Admin)
    {
        query = query.Where(u => u.Role != UserRole.Admin);
    }

    return query;
}
```

### 2.5 Repository层更新

```csharp
// IUserRepository.cs 新增方法

public interface IUserRepository
{
    // === 现有方法 ===
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUserNameAsync(string userName);
    Task<Result> ChangeProfileAsync(Guid userId, ChangeProfileDto dto);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);

    // === Phase 3新增方法 ===

    /// <summary>
    /// 获取用户列表（分页+筛选）
    /// </summary>
    Task<PagedResult<User>> GetPagedUsersAsync(UserQueryDto query, IQueryable<User> visibleUsersQuery);

    /// <summary>
    /// 创建新用户
    /// </summary>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<Result> UpdateAsync(User user);
}
```

---

## 3. Client端设计

### 3.1 模块结构

```
LYBT.Desktop.Users/
├── ViewModels/
│   ├── UserManagementViewModel.cs          ⭐ 主视图VM
│   ├── CreateUserDialogViewModel.cs        ⭐ 创建用户对话框VM
│   ├── EditUserDialogViewModel.cs          ⭐ 编辑用户对话框VM
│   └── ResetPasswordConfirmViewModel.cs    ⭐ 重置密码确认VM
├── Views/
│   ├── UserManagementView.xaml             ⭐ 主视图
│   ├── CreateUserDialog.xaml               ⭐ 创建用户对话框
│   ├── EditUserDialog.xaml                 ⭐ 编辑用户对话框
│   └── ResetPasswordConfirmDialog.xaml     ⭐ 重置密码确认对话框
├── Interfaces/
│   └── IUserRepository.cs                  (新增管理员方法)
├── Repositories/
│   └── UserRepository.cs                   (实现新增方法)
└── UsersModule.cs                          (注册新View/ViewModel)
```

### 3.2 ViewModel设计

#### 3.2.1 UserManagementViewModel

```csharp
// UserManagementViewModel.cs

public class UserManagementViewModel : BindableBase, INavigationAware, IDisposable
{
    private readonly IUserRepository _userRepository;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private readonly IUserNotificationService _notificationService;
    private readonly ISessionManager _sessionManager;
    private readonly IRegionManager _regionManager;

    // === 集合属性 ===
    public ObservableCollection<UserDto> Users { get; set; } = new();
    public ObservableCollection<UserRole> AvailableRoles { get; set; } = new();

    // === 筛选与搜索 ===
    private string _searchTerm = string.Empty;
    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            SetProperty(ref _searchTerm, value);
            SearchCommand.RaiseCanExecuteChanged();
        }
    }

    private UserRole? _selectedRoleFilter;
    public UserRole? SelectedRoleFilter
    {
        get => _selectedRoleFilter;
        set
        {
            SetProperty(ref _selectedRoleFilter, value);
            _ = LoadUsersAsync();
        }
    }

    private CommonStatus? _selectedStatusFilter;
    public CommonStatus? SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            SetProperty(ref _selectedStatusFilter, value);
            _ = LoadUsersAsync();
        }
    }

    // === 分页 ===
    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    private int _totalPages;
    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public string PageInfo => $"{CurrentPage} / {TotalPages}";

    // === 权限属性 ===
    private bool _isSysAdmin;
    public bool IsSysAdmin
    {
        get => _isSysAdmin;
        set => SetProperty(ref _isSysAdmin, value);
    }

    // === 命令 ===
    public DelegateCommand CreateUserCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand PreviousPageCommand { get; }
    public DelegateCommand NextPageCommand { get; }

    public DelegateCommand<UserDto> EditUserCommand { get; }
    public DelegateCommand<UserDto> ResetPasswordCommand { get; }
    public DelegateCommand<UserDto> EnableUserCommand { get; }
    public DelegateCommand<UserDto> DisableUserCommand { get; }

    public UserManagementViewModel(
        IUserRepository userRepository,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        IUserNotificationService notificationService,
        ISessionManager sessionManager,
        IRegionManager regionManager)
    {
        _userRepository = userRepository;
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _sessionManager = sessionManager;
        _regionManager = regionManager;

        // 初始化命令
        CreateUserCommand = new DelegateCommand(ExecuteCreateUser);
        SearchCommand = new DelegateCommand(ExecuteSearch, CanSearch);
        RefreshCommand = new DelegateCommand(ExecuteRefresh);
        PreviousPageCommand = new DelegateCommand(ExecutePreviousPage, CanGoPrevious);
        NextPageCommand = new DelegateCommand(ExecuteNextPage, CanGoNext);

        EditUserCommand = new DelegateCommand<UserDto>(ExecuteEditUser);
        ResetPasswordCommand = new DelegateCommand<UserDto>(ExecuteResetPassword);
        EnableUserCommand = new DelegateCommand<UserDto>(ExecuteEnableUser);
        DisableUserCommand = new DelegateCommand<UserDto>(ExecuteDisableUser);

        // 初始化角色列表
        InitializeRoles();
    }

    private void InitializeRoles()
    {
        var currentUser = _sessionManager.CurrentUser;

        // sysadmin可以看到所有角色（包括Admin）
        if (currentUser == null || currentUser.Id == Guid.Empty)
        {
            IsSysAdmin = true;
            AvailableRoles = new ObservableCollection<UserRole>(Enum.GetValues<UserRole>());
        }
        else
        {
            // Admin用户不能看到Admin角色
            IsSysAdmin = false;
            AvailableRoles = new ObservableCollection<UserRole>(
                Enum.GetValues<UserRole>().Where(r => r != UserRole.Admin));
        }
    }

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var query = new UserQueryDto
            {
                PageIndex = CurrentPage,
                PageSize = 20,
                SearchTerm = SearchTerm,
                Role = SelectedRoleFilter,
                Status = SelectedStatusFilter
            };

            var result = await _userRepository.GetUsersAsync(query);

            Users.Clear();
            foreach (var user in result.Items)
            {
                Users.Add(user);
            }

            TotalPages = result.TotalPages;
            RaisePropertyChanged(nameof(PageInfo));

            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"加载用户列表失败: {ex.Message}");
        }
    }

    private async void ExecuteCreateUser()
    {
        var parameters = new DialogParameters
        {
            { "AvailableRoles", AvailableRoles }
        };

        var result = await _dialogService.ShowDialogAsync("CreateUserDialog", parameters);

        if (result.Result == ButtonResult.OK)
        {
            await LoadUsersAsync();
        }
    }

    private async void ExecuteResetPassword(UserDto user)
    {
        var parameters = new DialogParameters
        {
            { "UserName", user.RealName }
        };

        var result = await _dialogService.ShowDialogAsync("ResetPasswordConfirmDialog", parameters);

        if (result.Result == ButtonResult.OK)
        {
            try
            {
                await _userRepository.ResetPasswordAsync(user.Id);
                await _notificationService.ShowSuccessAsync(
                    $"密码重置成功！\n" +
                    $"用户{user.RealName}的密码已恢复为默认密码，所有Token已撤销。\n" +
                    $"请告知用户使用默认密码重新登录。");
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorAsync($"密码重置失败: {ex.Message}");
            }
        }
    }

    // ... 其他命令实现
}
```

#### 3.2.2 CreateUserDialogViewModel

```csharp
// CreateUserDialogViewModel.cs

public class CreateUserDialogViewModel : BindableBase, IDialogAware
{
    private readonly IUserRepository _userRepository;
    private readonly IUserNotificationService _notificationService;

    // === 输入属性 ===
    private string _userName = string.Empty;
    public string UserName
    {
        get => _userName;
        set
        {
            SetProperty(ref _userName, value);
            CreateCommand.RaiseCanExecuteChanged();
        }
    }

    private string _realName = string.Empty;
    public string RealName
    {
        get => _realName;
        set
        {
            SetProperty(ref _realName, value);
            CreateCommand.RaiseCanExecuteChanged();
        }
    }

    private UserRole _selectedRole;
    public UserRole SelectedRole
    {
        get => _selectedRole;
        set
        {
            SetProperty(ref _selectedRole, value);
            CreateCommand.RaiseCanExecuteChanged();
        }
    }

    private string? _phoneNumber;
    public string? PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    private string? _email;
    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    private string? _remark;
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    // === 错误提示 ===
    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            HasError = !string.IsNullOrEmpty(value);
        }
    }

    // === 可用角色 ===
    public ObservableCollection<UserRole> AvailableRoles { get; set; } = new();

    // === 命令 ===
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public string Title => "创建新用户";
    public event Action<IDialogResult>? RequestClose;

    public CreateUserDialogViewModel(
        IUserRepository userRepository,
        IUserNotificationService notificationService)
    {
        _userRepository = userRepository;
        _notificationService = notificationService;

        CreateCommand = new DelegateCommand(ExecuteCreate, CanCreate);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    private bool CanCreate()
    {
        return !string.IsNullOrWhiteSpace(UserName) &&
               !string.IsNullOrWhiteSpace(RealName);
    }

    private async void ExecuteCreate()
    {
        ErrorMessage = string.Empty;

        // Client端验证
        if (!ValidateInput())
            return;

        try
        {
            var dto = new CreateUserInputDto
            {
                UserName = UserName.Trim(),
                RealName = RealName.Trim(),
                Role = SelectedRole,
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim()
            };

            await _userRepository.CreateUserAsync(dto);

            await _notificationService.ShowSuccessAsync($"用户 {RealName} 创建成功！\n初始密码为默认密码。");

            RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"创建失败: {ex.Message}";
        }
    }

    private bool ValidateInput()
    {
        // 用户名验证
        if (UserName.Length < 3 || UserName.Length > 50)
        {
            ErrorMessage = "用户名长度必须为3-50字符";
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(UserName, @"^[a-zA-Z0-9]+$"))
        {
            ErrorMessage = "用户名只能包含字母和数字";
            return false;
        }

        // 真实姓名验证
        if (RealName.Length < 2 || RealName.Length > 50)
        {
            ErrorMessage = "真实姓名长度必须为2-50字符";
            return false;
        }

        // 电话号码验证（Q9决策）
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneNumber, @"^\d{11}$"))
            {
                ErrorMessage = "电话号码必须为11位数字";
                return false;
            }
        }

        // 邮箱验证（Q10决策）
        if (!string.IsNullOrWhiteSpace(Email))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(Email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage = "邮箱格式不正确";
                return false;
            }
        }

        return true;
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        if (parameters.ContainsKey("AvailableRoles"))
        {
            var roles = parameters.GetValue<ObservableCollection<UserRole>>("AvailableRoles");
            AvailableRoles.Clear();
            foreach (var role in roles)
            {
                AvailableRoles.Add(role);
            }

            if (AvailableRoles.Any())
            {
                SelectedRole = AvailableRoles.First();
            }
        }
    }

    public bool CanCloseDialog() => true;
    public void OnDialogClosed() { }
}
```

### 3.3 Repository层实现（Client端）

```csharp
// UserRepository.cs 新增方法

public class UserRepository : IUserRepository
{
    private readonly IUserApi _userApi;

    // === Phase 3新增方法 ===

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserQueryDto query)
    {
        return await _userApi.GetUsersListAsync(
            query.PageIndex,
            query.PageSize,
            query.SearchTerm,
            query.Role,
            query.Status);
    }

    public async Task<Result> CreateUserAsync(CreateUserInputDto dto)
    {
        return await _userApi.CreateUserAsync(dto);
    }

    public async Task<Result> UpdateUserAsync(Guid userId, EditUserInputDto dto)
    {
        return await _userApi.UpdateUserAsync(userId, dto);
    }

    public async Task<Result> ResetPasswordAsync(Guid userId)
    {
        return await _userApi.ResetPasswordAsync(userId);
    }

    public async Task<Result> SetUserStatusAsync(Guid userId, CommonStatus status)
    {
        return await _userApi.SetUserStatusAsync(userId, new SetUserStatusDto { Status = status });
    }
}
```

### 3.4 API接口定义（Client端）

```csharp
// IUserApi.cs 新增方法

[Headers("Authorization: Bearer")]
public interface IUserApi
{
    // === Phase 3新增 ===

    [Get("/api/users/admin/list")]
    Task<PagedResult<UserDto>> GetUsersListAsync(
        [Query] int pageIndex,
        [Query] int pageSize,
        [Query] string? searchTerm,
        [Query] UserRole? role,
        [Query] CommonStatus? status);

    [Post("/api/users/admin/create")]
    Task<Result> CreateUserAsync([Body] CreateUserInputDto dto);

    [Put("/api/users/admin/{id}")]
    Task<Result> UpdateUserAsync(Guid id, [Body] EditUserInputDto dto);

    [Post("/api/users/admin/{id}/reset-password")]
    Task<Result> ResetPasswordAsync(Guid id);

    [Post("/api/users/admin/{id}/set-status")]
    Task<Result> SetUserStatusAsync(Guid id, [Body] SetUserStatusDto dto);
}
```

---

## 4. 数据库设计

### 4.1 User表（现有，无需变更）

```sql
-- Users 表（Phase 2已存在）
CREATE TABLE [dbo].[Users]
(
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserName] NVARCHAR(50) NOT NULL,
    [RealName] NVARCHAR(50) NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [Role] INT NOT NULL, -- UserRole枚举
    [PhoneNumber] NVARCHAR(20) NULL,
    [Email] NVARCHAR(100) NULL,
    [PinYinCode] NVARCHAR(20) NULL,
    [Status] INT NOT NULL DEFAULT 1, -- CommonStatus.Enabled
    [Remark] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);

-- 唯一性约束
CREATE UNIQUE INDEX UX_Users_UserName ON Users(UserName);
CREATE UNIQUE INDEX UX_Users_PhoneNumber ON Users(PhoneNumber) WHERE PhoneNumber IS NOT NULL;
CREATE UNIQUE INDEX UX_Users_Email ON Users(Email) WHERE Email IS NOT NULL;

-- 索引优化
CREATE INDEX IX_Users_Role ON Users(Role);
CREATE INDEX IX_Users_Status ON Users(Status);
CREATE INDEX IX_Users_RealName ON Users(RealName);
```

### 4.2 AdminSecrets表（sysadmin密码存储）

```sql
-- AdminSecrets 表（存储sysadmin密码）
CREATE TABLE [dbo].[AdminSecrets]
(
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [AdminUserName] NVARCHAR(50) NOT NULL, -- 固定为 'sysadmin'
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);

-- 唯一性约束
CREATE UNIQUE INDEX UX_AdminSecrets_AdminUserName ON AdminSecrets(AdminUserName);

-- 初始化sysadmin记录
INSERT INTO [dbo].[AdminSecrets] (AdminUserName, PasswordHash, CreatedAt)
VALUES ('sysadmin', '$2a$11$hashedPasswordFromConfig', GETUTCDATE());
```

### 4.3 RefreshTokens表（Token管理）

```sql
-- RefreshTokens 表（JWT刷新Token管理）
CREATE TABLE [dbo].[RefreshTokens]
(
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserId] UNIQUEIDENTIFIER NOT NULL, -- 外键到Users.Id
    [Token] NVARCHAR(500) NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsRevoked] BIT NOT NULL DEFAULT 0,
    [RevokedAt] DATETIME2 NULL,
    [ReasonRevoked] NVARCHAR(200) NULL, -- '用户修改密码' / '管理员重置密码' / 'sysadmin密码重置'

    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- 索引优化
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_IsRevoked ON RefreshTokens(IsRevoked);
```

### 4.4 SecurityAuditLog表（安全审计）

```sql
-- SecurityAuditLog 表（安全审计日志）
CREATE TABLE [dbo].[SecurityAuditLogs]
(
    [Id] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [EventType] NVARCHAR(50) NOT NULL, -- 'PasswordReset' / 'PasswordChanged' / 'SysAdminPasswordReset'
    [UserId] UNIQUEIDENTIFIER NULL, -- 被操作用户ID
    [PerformedBy] UNIQUEIDENTIFIER NULL, -- 操作者ID（Guid.Empty表示sysadmin）
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Details] NVARCHAR(1000) NULL -- 详细描述
);

-- 索引优化
CREATE INDEX IX_SecurityAuditLogs_UserId ON SecurityAuditLogs(UserId);
CREATE INDEX IX_SecurityAuditLogs_EventType ON SecurityAuditLogs(EventType);
CREATE INDEX IX_SecurityAuditLogs_Timestamp ON SecurityAuditLogs(Timestamp);
```

### 4.5 数据库迁移脚本

```csharp
// Migrations/20250107_AddUserManagementTables.cs

public partial class AddUserManagementTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // AdminSecrets表
        migrationBuilder.CreateTable(
            name: "AdminSecrets",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AdminUserName = table.Column<string>(maxLength: 50, nullable: false),
                PasswordHash = table.Column<string>(maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminSecrets", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "UX_AdminSecrets_AdminUserName",
            table: "AdminSecrets",
            column: "AdminUserName",
            unique: true);

        // RefreshTokens表
        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                UserId = table.Column<Guid>(nullable: false),
                Token = table.Column<string>(maxLength: 500, nullable: false),
                ExpiresAt = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                IsRevoked = table.Column<bool>(nullable: false, defaultValue: false),
                RevokedAt = table.Column<DateTime>(nullable: true),
                ReasonRevoked = table.Column<string>(maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        // SecurityAuditLogs表
        migrationBuilder.CreateTable(
            name: "SecurityAuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false, defaultValueSql: "NEWID()"),
                EventType = table.Column<string>(maxLength: 50, nullable: false),
                UserId = table.Column<Guid>(nullable: true),
                PerformedBy = table.Column<Guid>(nullable: true),
                Timestamp = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                Details = table.Column<string>(maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SecurityAuditLogs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SecurityAuditLogs_UserId",
            table: "SecurityAuditLogs",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SecurityAuditLogs");
        migrationBuilder.DropTable(name: "RefreshTokens");
        migrationBuilder.DropTable(name: "AdminSecrets");
    }
}
```

---

## 5. 安全设计

### 5.1 密码复杂度验证（Q4决策）

```csharp
// PasswordValidator.cs

public static class PasswordValidator
{
    /// <summary>
    /// 验证密码复杂度（Q4决策：8位+大小写+数字+特殊字符）
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "密码不能为空");

        if (password.Length < 8)
            return (false, "密码长度至少8个字符");

        if (password.Length > 20)
            return (false, "密码长度不能超过20个字符");

        if (!password.Any(char.IsUpper))
            return (false, "密码必须包含至少一个大写字母（A-Z）");

        if (!password.Any(char.IsLower))
            return (false, "密码必须包含至少一个小写字母（a-z）");

        if (!password.Any(char.IsDigit))
            return (false, "密码必须包含至少一个数字（0-9）");

        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        if (!password.Any(c => specialChars.Contains(c)))
            return (false, "密码必须包含至少一个特殊字符（!@#$%^&*等）");

        return (true, string.Empty);
    }
}
```

### 5.2 权限验证矩阵

| 操作 | sysadmin | Admin用户 | 普通用户 |
|-----|---------|----------|---------|
| 查看所有用户 | ✅ | ⚠️（仅非Admin） | ❌ |
| 创建Admin用户 | ✅ | ❌ | ❌ |
| 创建非Admin用户 | ✅ | ✅ | ❌ |
| 编辑Admin用户 | ✅ | ❌ | ❌ |
| 编辑非Admin用户 | ✅ | ✅ | ❌ |
| 重置Admin密码 | ✅ | ❌ | ❌ |
| 重置非Admin密码 | ✅ | ✅ | ❌ |
| 禁用Admin用户 | ✅ | ❌ | ❌ |
| 禁用非Admin用户 | ✅ | ✅ | ❌ |
| 修改自己信息 | ❌（sysadmin不在User表） | ✅ | ✅ |
| 修改自己密码 | ✅（走特殊逻辑） | ✅ | ✅ |

### 5.3 sysadmin架构保护机制

```csharp
// UserRepository.cs

public async Task<User> SaveAsync(User user)
{
    // ⚠️ 架构保护：禁止sysadmin写入User表
    if (user.UserName?.ToLower() == "sysadmin")
    {
        throw new InvalidOperationException(
            "sysadmin是虚拟超级用户，不允许保存到数据库。" +
            "请检查代码逻辑，确保sysadmin仅在appsettings.json + AdminSecrets表中配置。");
    }

    // 正常保存逻辑
    _dbContext.Users.Update(user);
    await _dbContext.SaveChangesAsync();
    return user;
}
```

### 5.4 Token生命周期管理

**场景1：密码重置（管理员操作）**
```
管理员点击"重置密码"
  ↓
Server端：
  1. 更新PasswordHash为默认密码
  2. 撤销所有RefreshToken (IsRevoked=true)
  3. 记录SecurityAuditLog
  ↓
用户下次登录：
  - 旧Token无效（已撤销）
  - 使用默认密码登录
  - 获取新Token
```

**场景2：用户修改密码**
```
用户在ChangePasswordDialog修改密码
  ↓
Server端：
  1. 验证旧密码
  2. 更新PasswordHash为新密码
  3. 撤销所有RefreshToken
  4. 记录SecurityAuditLog
  ↓
Client端：
  1. 自动执行logout (清除本地Token)
  2. 导航到登录页
  ↓
用户重新登录：
  - 使用新密码登录
  - 获取新Token
```

---

## 6. 实施Phase规划

### 6.1 Phase 3.1: Server端基础API（预计2天）

**目标**：完成Server端核心API和Service层

**任务清单**：
- [ ] 创建DTO类（CreateUserInputDto, EditUserInputDto等）
- [ ] UserService新增5个方法
- [ ] UsersController新增5个端点
- [ ] 权限验证逻辑（CanManageUser）
- [ ] 单元测试（UserService）
- [ ] API集成测试

**验证标准**：
- ✅ 所有API端点Swagger可访问
- ✅ 权限验证通过测试
- ✅ 单元测试覆盖率 >80%

### 6.2 Phase 3.2: Client端UI骨架（预计1.5天）

**目标**：完成Client端基础UI和ViewModel骨架

**任务清单**：
- [ ] 创建UserManagementView.xaml（DataGrid+筛选+分页）
- [ ] 创建UserManagementViewModel（集合+命令）
- [ ] 创建3个对话框XAML（Create/Edit/ResetConfirm）
- [ ] 对话框ViewModel骨架
- [ ] Repository接口定义（IUserRepository新增方法）
- [ ] API接口定义（IUserApi新增方法）

**验证标准**：
- ✅ UI可访问（从AdminHomeView导航）
- ✅ 对话框可打开（空白状态）
- ✅ 命令绑定正常（点击有响应）

### 6.3 Phase 3.3: 核心功能实现（预计3天）

**目标**：实现完整的CRUD功能

**任务清单**：

**Day 1: 查询与分页**
- [ ] UserManagementViewModel.LoadUsersAsync实现
- [ ] 分页逻辑（PreviousPage/NextPage）
- [ ] 筛选逻辑（Role/Status/SearchTerm）
- [ ] UserRepository.GetUsersAsync实现

**Day 2: 创建与编辑**
- [ ] CreateUserDialogViewModel完整实现
- [ ] EditUserDialogViewModel完整实现
- [ ] Client端验证逻辑（电话号码/邮箱格式）
- [ ] UserRepository.CreateUserAsync/UpdateUserAsync实现

**Day 3: 密码重置与状态管理**
- [ ] ResetPasswordConfirmViewModel实现
- [ ] UserRepository.ResetPasswordAsync实现
- [ ] EnableUser/DisableUser命令实现
- [ ] UserRepository.SetUserStatusAsync实现

**验证标准**：
- ✅ 创建用户成功（默认密码）
- ✅ 编辑用户成功（PinYinCode自动更新）
- ✅ 重置密码成功（Token撤销）
- ✅ 启用/禁用成功

### 6.4 Phase 3.4: 权限验证与测试（预计1.5天）

**目标**：完成权限验证和全面测试

**任务清单**：
- [ ] sysadmin场景测试（查看所有用户/创建Admin）
- [ ] Admin场景测试（仅查看非Admin/不能创建Admin）
- [ ] 边界条件测试（唯一性/格式验证）
- [ ] Token撤销验证（密码重置/修改密码）
- [ ] UI权限保护（按钮Visibility绑定）
- [ ] 单元测试（ViewModel）

**验证标准**：
- ✅ sysadmin可以看到Admin用户
- ✅ Admin用户看不到其他Admin
- ✅ Admin不能创建Admin用户
- ✅ 密码重置后Token立即失效
- ✅ 所有验证规则正常工作

### 6.5 Phase 3.5: sysadmin密码重置工具（预计0.5天）

**目标**：创建独立的Console App工具

**任务清单**：
- [ ] 创建Console App项目（LYBT.Tools.SysAdminPasswordReset）
- [ ] 实现密码输入隐藏显示
- [ ] 实现密码复杂度验证
- [ ] 生成SQL语句（含Token撤销+审计日志）
- [ ] 测试工具（生成SQL并在SSMS执行验证）

**验证标准**：
- ✅ 工具可运行
- ✅ 密码输入隐藏
- ✅ 生成的SQL语句正确
- ✅ 执行SQL后sysadmin可用新密码登录

### 6.6 总时间估算

| Phase | 预计时间 | 依赖 |
|-------|---------|------|
| Phase 3.1 | 2天 | - |
| Phase 3.2 | 1.5天 | Phase 3.1 |
| Phase 3.3 | 3天 | Phase 3.2 |
| Phase 3.4 | 1.5天 | Phase 3.3 |
| Phase 3.5 | 0.5天 | - (独立) |
| **总计** | **8.5天** | - |

---

## 7. 附录

### 7.1 配置文件示例

```json
// appsettings.json

{
  "UserManagement": {
    "DefaultPassword": "Admin123!@#"
  },
  "JWT": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "LYBTZYZS",
    "Audience": "LYBTZYZS-Client",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 7.2 PinYinHelper实现（Q8决策）

```csharp
// Shared/Helpers/PinYinHelper.cs

public static class PinYinHelper
{
    /// <summary>
    /// 获取汉字拼音首字母（大写）
    /// 例如：张三 → ZS，黄芪 → HQ
    /// </summary>
    public static string GetFirstLetters(string chineseText)
    {
        if (string.IsNullOrEmpty(chineseText))
            return string.Empty;

        var result = new StringBuilder();

        foreach (var c in chineseText)
        {
            if (char.IsWhiteSpace(c))
                continue;

            // 非中文字符直接添加
            if (c < 0x4E00 || c > 0x9FA5)
            {
                result.Append(char.ToUpper(c));
                continue;
            }

            // 中文字符转拼音首字母
            var pinyin = GetPinYinFirstChar(c);
            result.Append(char.ToUpper(pinyin));
        }

        return result.ToString();
    }

    private static char GetPinYinFirstChar(char c)
    {
        // 使用第三方库 TinyPinyin.Core 或 Pinyin4Net
        // 这里简化实现，实际项目中应使用成熟的拼音库
        var bytes = Encoding.GetEncoding("GB2312").GetBytes(c.ToString());
        if (bytes.Length < 2)
            return c;

        int code = (bytes[0] << 8) + bytes[1];

        // 简化的拼音首字母映射（实际应使用完整映射表）
        if (code >= 45217 && code <= 45252) return 'A';
        if (code >= 45253 && code <= 45760) return 'B';
        // ... 其他映射

        return c;
    }
}
```

### 7.3 关联文档

- [用户管理需求文档](./user-management-requirements.md)
- [auth-user安全改进讨论](./auth-user-security-improvement-discussion.md)
- [CLAUDE.md](../../../CLAUDE.md)
- [Constitution MVP约束](../../../.spec-workflow/steering/constitution.md)

---

**文档版本**: v1.0
**最后更新**: 2025-11-07
**审核状态**: ⏳ 待审核
