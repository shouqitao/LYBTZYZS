# 👤 用户管理模块 (Users)

## 📦 模块定位

- **层级**：Server端 + Client端
- **类型**：核心业务模块（用户管理）
- **职责**：提供完整的用户账户管理、角色权限控制和用户信息维护功能。支持Admin/Doctor双角色体系，包含用户CRUD、密码管理、状态管理、批量操作等核心功能。专为小型中医诊所(<20人)优化，确保系统安全性和易用性。

## 🎯 功能概述

用户管理模块是系统的核心管理模块，为管理员提供用户账户的完整生命周期管理。通过Admin/Doctor双角色体系、密码安全策略、用户状态管理等功能，实现高效的人员管理和权限控制。Client端采用MVVM架构和分页优化，确保大数据量下的流畅体验。

### 核心价值

- **双角色体系**：Admin（管理员）和Doctor（医生）角色分离，权限清晰
- **完整CRUD**：用户创建、编辑、删除、批量操作
- **密码管理**：密码强度验证、密码重置（管理员功能）、密码修改（用户自助）
- **状态管理**：用户激活/停用、状态切换、最后一个管理员保护
- **高级搜索**：多条件过滤（角色、状态、关键字、创建时间）、分页查询、排序
- **用户体验**：UnifiedViewModelBase统一分页逻辑、对话框通信、权限控制

## 🏗️ 模块架构

### Server端架构（LYBT.Module.Users）

```
LYBT.Module.Users/
├── UsersModule.cs                    # 模块依赖注入注册
│   └── AddUsersModule()              # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                       # 模块接口定义
│   └── IUserRepository.cs            # 用户仓储接口(25个方法)
├── Services/                         # 业务逻辑实现
│   └── UserService.cs                # 用户服务(19个方法)
│       ├── GetPagedAsync()           # 分页查询用户
│       ├── GetByIdAsync()            # 按ID查询用户详情
│       ├── SearchAsync()             # 搜索用户(按用户名/角色/状态)
│       ├── CreateAsync()             # 创建用户
│       ├── UpdateAsync()             # 更新用户
│       ├── DeleteAsync()             # 删除用户
│       ├── BatchDeleteAsync()        # 批量删除用户
│       ├── DisableAsync()            # 禁用用户
│       ├── EnableAsync()             # 启用用户
│       ├── ToggleStatusAsync()       # 切换用户状态
│       ├── ResetPasswordAsync()      # 重置密码(两个重载)
│       ├── ChangePasswordAsync()     # 修改密码
│       ├── ChangeProfileAsync()      # 修改用户资料
│       └── GenerateTemporaryPassword() # 生成临时密码
├── Repositories/                     # 数据仓储实现
│   └── UserRepository.cs             # 用户仓储(25个方法)
│       ├── GetByUsernameAsync()      # 按用户名查询
│       ├── GetByEmailAsync()         # 按邮箱查询
│       ├── IsUsernameExistsAsync()   # 检查用户名是否存在
│       ├── IsEmailExistsAsync()      # 检查邮箱是否存在
│       └── 其他CRUD方法(21个)        # 完整数据访问能力
├── Validators/                       # FluentValidation验证器
│   ├── UserCreateDtoValidator.cs     # 创建用户DTO验证
│   └── UserUpdateDtoValidator.cs     # 更新用户DTO验证
└── Mapping/                          # AutoMapper映射配置
    └── UserMappingProfile.cs         # Entity ↔ DTO映射规则
```

**依赖关系**：
- **依赖项目**：LYBT.Entities（UserModel、UserRole、UserStatus枚举）、LYBT.Infrastructure（AppDbContext）、LYBT.Shared.Models（UserDto）
- **被依赖项目**：LYBT.Module.Auth（认证模块依赖用户验证）、LYBT.Module.MedicalCase（医案模块关联医生用户）、LYBT.WebAPI（UsersController）

### Client端架构（LYBT.Desktop.Users）

```
LYBT.Desktop.Users/
├── ViewModels/                                # MVVM视图模型层(7个)
│   ├── UserManagementViewModel.cs            # 用户管理主视图模型(19属性+20方法)
│   │   ├── 筛选属性(3): SelectedRole, SelectedStatus, ShowInactiveUsers
│   │   ├── 选项属性(2): RoleOptions, StatusOptions
│   │   ├── 命令属性(14): AddCommand, EditCommand, DeleteCommand, SearchCommand等
│   │   └── 方法(20): GetItemsAsync, OnExecuteAddAsync, ExecuteEditUser等
│   ├── UserCreateViewModel.cs                # 用户创建视图模型
│   ├── UserEditViewModel.cs                  # 用户编辑视图模型
│   ├── UserDetailViewModel.cs                # 用户详情视图模型
│   ├── ChangePasswordDialogViewModel.cs      # 修改密码对话框视图模型
│   ├── ResetPasswordDialogViewModel.cs       # 重置密码对话框视图模型
│   └── UserProfileDialogViewModel.cs         # 用户资料对话框视图模型
├── Views/                                     # WPF视图层(12个文件)
│   ├── UserManagementView.xaml               # 用户管理主视图
│   ├── UserCreateView.xaml                   # 用户创建视图
│   ├── UserEditView.xaml                     # 用户编辑视图
│   ├── UserDetailView.xaml                   # 用户详情视图
│   ├── ChangePasswordDialog.xaml             # 修改密码对话框
│   ├── ResetPasswordDialog.xaml              # 重置密码对话框（管理员功能）
│   ├── UserProfileDialog.xaml                # 用户资料对话框
│   └── 对应的7个代码后置文件(.xaml.cs)
├── Repositories/                              # 数据仓储层(1个)
│   └── UserRepository.cs                      # 用户仓储实现(继承BaseApiRepository)
├── Interfaces/                                # 接口定义层(1个)
│   └── IUserRepository.cs                     # 用户仓储接口(9个方法)
├── Models/                                    # 本地模型层(1个)
│   └── UserItem.cs                            # 用户列表项模型(用于DataGrid绑定)
└── UsersModule.cs                             # Prism模块定义(2个方法)
    ├── OnInitialized()                        # 模块初始化
    └── RegisterTypes()                        # 类型注册(Views + ViewModels + Repository)
```

**依赖关系**：
- **依赖服务**：LYBT.Desktop.Foundation（BaseApiRepository、IApiService）、LYBT.Desktop.Infrastructure（UnifiedViewModelBase、DialogService）、LYBT.Desktop.Contracts（UserDto、CreateUserDto）
- **UI依赖**：MaterialDesignThemes 5.1.x（Material Design组件）、Prism.DryIoc 8.x（MVVM框架）
- **Shell集成**：Shell根据Admin角色加载Users模块（OnDemand模式）

## 🔧 核心功能

### 1. 用户创建与角色分配（Server端 + Client端）

**Server端业务逻辑**：
```csharp
// UserService.cs - 创建用户
public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
{
    // 1. 数据验证
    var validationResult = await ValidateCreateUserAsync(dto);
    if (!validationResult.IsSuccess)
        return ServiceResult<UserDto>.Failure(validationResult.Message);

    // 2. 检查用户名唯一性
    var existingUser = await _repository.GetByUsernameAsync(dto.Username);
    if (existingUser != null)
        return ServiceResult<UserDto>.Failure("用户名已存在");

    // 3. 创建用户实体
    var user = new UserModel
    {
        Username = dto.Username,
        DisplayName = dto.DisplayName,
        Role = dto.Role,  // UserRole枚举类型(Admin或Doctor)
        Status = UserStatus.Active,
        PasswordHash = _passwordHasher.HashPassword(null, dto.Password)
    };

    // 4. 保存到数据库
    var createdUser = await _repository.AddAsync(user);
    await _repository.SaveChangesAsync();

    // 5. 返回DTO
    return ServiceResult<UserDto>.Success(_mapper.Map<UserDto>(createdUser));
}
```

**Client端创建流程**：
```csharp
// UserManagementViewModel.cs - 打开创建对话框
private async Task OnExecuteAddAsync()
{
    var parameters = new DialogParameters { { "Mode", "Create" } };

    _dialogService.ShowDialog("UserCreateView", parameters, async result =>
    {
        if (result.Result == ButtonResult.OK)
        {
            var createDto = result.Parameters.GetValue<CreateUserDto>("UserDto");
            if (createDto != null)
            {
                IsBusy = true;
                var createdUser = await _userRepository.CreateAsync(createDto);
                if (createdUser != null)
                {
                    SetSuccessMessage($"用户 {createdUser.Username} 创建成功");
                    await RefreshDataAsync();  // 刷新列表
                }
                IsBusy = false;
            }
        }
    });
}
```

**UserRole枚举定义**：
```csharp
public enum UserRole
{
    Admin = 1,      // 系统管理员（全权限）
    Doctor = 2      // 医生（诊疗权限）
}
```

### 2. 密码管理（Server端）

**密码强度验证**：
```csharp
// UserService.cs - 密码强度验证
private ValidationResult ValidatePasswordStrength(string password)
{
    var errors = new List<string>();

    if (password.Length < 8)
        errors.Add("密码长度至少8位");

    if (!password.Any(char.IsUpper))
        errors.Add("密码必须包含大写字母");

    if (!password.Any(char.IsLower))
        errors.Add("密码必须包含小写字母");

    if (!password.Any(char.IsDigit))
        errors.Add("密码必须包含数字");

    if (!password.Any(c => !char.IsLetterOrDigit(c)))
        errors.Add("密码必须包含特殊字符");

    return new ValidationResult
    {
        IsValid = !errors.Any(),
        Message = errors.Any() ? string.Join("；", errors) : "密码强度验证通过"
    };
}
```

**修改密码**（用户自助）：
```csharp
// UserService.cs - 修改密码
public async Task<ServiceResult<bool>> ChangePasswordAsync(
    Guid userId,
    ChangePasswordDto dto)
{
    // 1. 获取用户
    var user = await _repository.GetByIdAsync(userId);
    if (user == null)
        return ServiceResult<bool>.Failure("用户不存在");

    // 2. 验证旧密码
    var verifyResult = _passwordHasher.VerifyHashedPassword(
        user,
        user.PasswordHash,
        dto.OldPassword
    );

    if (verifyResult == PasswordVerificationResult.Failed)
        return ServiceResult<bool>.Failure("旧密码不正确");

    // 3. 验证新密码强度
    var passwordValidation = ValidatePasswordStrength(dto.NewPassword);
    if (!passwordValidation.IsValid)
        return ServiceResult<bool>.Failure(passwordValidation.Message);

    // 4. 更新密码哈希
    user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
    await _repository.UpdateAsync(user);
    await _repository.SaveChangesAsync();

    _logger.LogInformation($"用户 {user.Username} 修改密码成功");
    return ServiceResult<bool>.Success(true);
}
```

**重置密码**（管理员功能，Client端）：
```csharp
// UserManagementViewModel.cs - 重置密码
private async Task ExecuteResetPasswordAsync()
{
    if (SelectedItem == null) return;

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
                var success = await _userRepository.ResetPasswordAsync(SelectedItem.Id, newPassword);
                if (success)
                {
                    SetSuccessMessage($"用户 {SelectedItem.Username} 的密码已重置");
                }
                IsBusy = false;
            }
        }
    });
}
```

### 3. 用户状态管理（Server端 + Client端）

**UserStatus枚举定义**：
```csharp
public enum UserStatus
{
    Active = 1,     // 正常（可登录）
    Inactive = 2,   // 停用（无法登录）
    Locked = 3      // 锁定（多次登录失败）
}
```

**状态切换**（Server端）：
```csharp
// UserService.cs - 启用用户
public async Task<ServiceResult<bool>> EnableAsync(Guid userId)
{
    var user = await _repository.GetByIdAsync(userId);
    if (user == null)
        return ServiceResult<bool>.Failure("用户不存在");

    if (user.Status == UserStatus.Active)
        return ServiceResult<bool>.Failure("用户已是激活状态");

    user.Status = UserStatus.Active;
    await _repository.UpdateAsync(user);
    await _repository.SaveChangesAsync();

    _logger.LogInformation($"用户 {user.Username} 已启用");
    return ServiceResult<bool>.Success(true);
}

// UserService.cs - 禁用用户
public async Task<ServiceResult<bool>> DisableAsync(Guid userId)
{
    var user = await _repository.GetByIdAsync(userId);
    if (user == null)
        return ServiceResult<bool>.Failure("用户不存在");

    if (user.Status == UserStatus.Inactive)
        return ServiceResult<bool>.Failure("用户已是禁用状态");

    user.Status = UserStatus.Inactive;
    await _repository.UpdateAsync(user);
    await _repository.SaveChangesAsync();

    _logger.LogInformation($"用户 {user.Username} 已禁用");
    return ServiceResult<bool>.Success(true);
}
```

**状态切换**（Client端）：
```csharp
// UserManagementViewModel.cs - 切换用户状态
private async Task ExecuteToggleUserStatusAsync()
{
    if (SelectedItem == null) return;

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
        SetSuccessMessage($"用户 {updatedUser.Username} 已{action}");
        await RefreshDataAsync();  // 刷新列表
    }
}
```

### 4. 批量删除与安全保护（Server端）

**批量删除逻辑**（防止删除最后一个管理员）：
```csharp
// UserService.cs - 批量删除用户
public async Task<ServiceResult<BatchOperationResult>> BatchDeleteAsync(List<Guid> userIds)
{
    var result = new BatchOperationResult { TotalCount = userIds.Count };

    foreach (var userId in userIds)
    {
        try
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null)
            {
                result.FailedItems.Add(new BatchOperationError
                {
                    ItemId = userId.ToString(),
                    ErrorMessage = "用户不存在"
                });
                continue;
            }

            // 检查是否为最后一个Admin用户
            if (user.Role == UserRole.Admin)
            {
                var adminCount = await _repository.CountAsync(u => u.Role == UserRole.Admin);
                if (adminCount <= 1)
                {
                    result.FailedItems.Add(new BatchOperationError
                    {
                        ItemId = userId.ToString(),
                        ErrorMessage = "不能删除最后一个管理员用户"
                    });
                    continue;
                }
            }

            // 软删除
            user.IsDeleted = true;
            await _repository.UpdateAsync(user);
            result.SuccessCount++;

            _logger.LogInformation($"用户 {user.Username} 已删除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"删除用户 {userId} 失败");
            result.FailedItems.Add(new BatchOperationError
            {
                ItemId = userId.ToString(),
                ErrorMessage = ex.Message
            });
        }
    }

    await _repository.SaveChangesAsync();
    return ServiceResult<BatchOperationResult>.Success(result);
}
```

### 5. 高级搜索与分页（Client端）

**UserManagementViewModel继承UnifiedViewModelBase**：
```csharp
// UserManagementViewModel.cs
public class UserManagementViewModel : UnifiedViewModelBase<UserDto>
{
    // 继承自UnifiedViewModelBase的分页属性
    // PageIndex, PageSize, TotalCount, TotalPages
    // PreviousPageCommand, NextPageCommand, RefreshCommand

    // 筛选属性（3个）
    public string? SelectedRole { get; set; }      // 角色过滤器（Admin/Doctor/全部）
    public string? SelectedStatus { get; set; }    // 状态过滤器（Active/Inactive/全部）
    public bool ShowInactiveUsers { get; set; }    // 是否显示停用用户

    // 选项属性（2个）
    public List<string> RoleOptions { get; } = new() { "全部", "Admin", "Doctor" };
    public List<string> StatusOptions { get; } = new() { "全部", "Active", "Inactive", "Locked" };

    // 分页查询用户（支持过滤）
    protected override async Task<(List<UserDto> items, int totalCount)> GetItemsAsync(
        int pageIndex, int pageSize, string? searchTerm)
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
        return (result.Items ?? new List<UserDto>(), result.TotalCount);
    }
}
```

## 📋 业务规则

### 1. 用户创建规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **USER-R01** | 用户名必须唯一（3-50字符，字母数字下划线） | UserService.ValidateUsernameAsync |
| **USER-R02** | 密码强度要求（≥8位，包含大小写字母、数字、特殊字符） | UserService.ValidatePasswordStrength |
| **USER-R03** | 新用户默认状态为Active | UserService.CreateAsync |
| **USER-R04** | 角色必须为Admin或Doctor（枚举类型） | UserCreateDto.Role |

### 2. 密码管理规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **USER-R05** | 修改密码需要验证旧密码 | UserService.ChangePasswordAsync |
| **USER-R06** | 重置密码仅限管理员操作 | UsersController + Authorize(Roles="Admin") |
| **USER-R07** | 密码哈希使用ASP.NET Core Identity PasswordHasher | UserService（Server端） |
| **USER-R08** | 重置密码需要确认对话框（Client端） | UserManagementViewModel.ExecuteResetPasswordAsync |

### 3. 用户状态规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **USER-R09** | 停用用户无法登录系统 | AuthService.VerifyCredentialsAsync |
| **USER-R10** | 状态切换需要确认对话框 | UserManagementViewModel.ExecuteToggleUserStatusAsync |
| **USER-R11** | 管理员可以管理所有用户状态 | UsersController + Authorize(Roles="Admin") |

### 4. 删除保护规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **USER-R12** | 不能删除最后一个管理员用户 | UserService.BatchDeleteAsync |
| **USER-R13** | 用户删除为软删除（IsDeleted标记） | UserService.DeleteAsync |
| **USER-R14** | 批量删除失败项不影响其他项 | UserService.BatchDeleteAsync |

## 🔌 API 端点

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `UsersController` 对外暴露。

- **API路由前缀**: `/api/v1/users`

**主要端点**：

| 端点 | 方法 | 功能描述 | 权限 | 请求体 | 响应体 |
|-----|------|---------|------|--------|--------|
| `/api/v1/users` | GET | 分页查询用户 | Admin | - | PagedResult<UserDto> |
| `/api/v1/users/{id}` | GET | 按ID查询用户详情 | Admin | - | UserDto |
| `/api/v1/users/username/{username}` | GET | 按用户名查询用户 | Admin | - | UserDto |
| `/api/v1/users/statistics` | GET | 获取用户统计信息 | Admin | - | UserStatisticsDto |
| `/api/v1/users` | POST | 创建用户 | Admin | UserCreateDto | UserDto |
| `/api/v1/users/{id}` | PUT | 更新用户 | Admin | UserUpdateDto | UserDto |
| `/api/v1/users/{id}` | DELETE | 删除用户 | Admin | - | bool |
| `/api/v1/users/batch-delete` | POST | 批量删除用户 | Admin | List<Guid> | BatchOperationResult |
| `/api/v1/users/{id}/enable` | POST | 启用用户 | Admin | - | bool |
| `/api/v1/users/{id}/disable` | POST | 禁用用户 | Admin | - | bool |
| `/api/v1/users/{id}/toggle-status` | POST | 切换用户状态 | Admin | - | bool |
| `/api/v1/users/{id}/change-password` | POST | 修改密码 | User | ChangePasswordDto | bool |
| `/api/v1/users/{id}/reset-password` | POST | 重置密码 | Admin | ResetPasswordDto | bool |
| `/api/v1/users/{id}/profile` | PUT | 修改用户资料 | User | UpdateProfileDto | UserDto |
| `/api/v1/users/validate-username/{username}` | GET | 验证用户名是否可用 | Admin | - | bool |

**DTO定义示例**：

```csharp
// UserCreateDto
public class UserCreateDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度应在3-50字符之间")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度应在8-100字符之间")]
    public string Password { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "显示名称长度不能超过100字符")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "角色不能为空")]
    public UserRole Role { get; set; } = UserRole.Doctor;
}

// UserDto
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }  // 枚举类型(Admin或Doctor)
    public UserStatus Status { get; set; }  // Active/Inactive/Locked
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }

    // 显示友好名称
    public string RoleDisplayName => Role == UserRole.Admin ? "管理员" : "医生";
    public string StatusDisplayName => Status switch
    {
        UserStatus.Active => "正常",
        UserStatus.Inactive => "停用",
        UserStatus.Locked => "锁定",
        _ => "未知"
    };
}

// BatchOperationResult
public class BatchOperationResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public List<BatchOperationError> FailedItems { get; set; } = new();
}
```

**完整API定义**请参考 `IUserService` 接口和 `UsersController` 的实现。

## 🎯 设计原则

### Server端设计原则（7条）

#### 1. 双角色体系简化
- **原则**：Admin/Doctor双角色体系满足小型诊所需求（<20人）
- **实现**：UserRole枚举（Admin、Doctor），避免复杂的RBAC角色系统
- **价值**：简化权限管理，降低系统复杂度

#### 2. DTO规范与类型安全
- **原则**：Create/Update DTO分离，查询DTO命名规范，枚举类型替代字符串
- **实现**：UserCreateDto/UserUpdateDto分离、UserSearchDto命名规范、UserRole枚举
- **价值**：编译时类型检查，避免运行时错误

#### 3. 密码安全优先
- **原则**：所有密码必须使用ASP.NET Core Identity PasswordHasher哈希
- **实现**：IPasswordHasher<User>统一处理密码哈希和验证
- **价值**：防止密码泄露（即使数据库被攻破）

#### 4. 用户名唯一性保证
- **原则**：用户名全局唯一（不区分大小写）
- **实现**：数据库唯一索引 + Repository查询验证
- **价值**：防止重复账户、简化用户识别

#### 5. 最后一个管理员保护
- **原则**：系统至少保留一个Admin用户
- **实现**：删除前检查Admin数量，拒绝删除最后一个Admin
- **价值**：防止误操作导致系统无法管理

#### 6. 软删除与数据审计
- **原则**：用户删除使用软删除（IsDeleted标记），保留审计记录
- **实现**：BaseEntity.IsDeleted字段 + Repository过滤
- **价值**：数据可恢复、审计追踪、合规性

#### 7. 异步优先与性能优化
- **原则**：所有I/O操作异步化（数据库查询、密码哈希）
- **实现**：async/await模式、Task异步方法
- **价值**：提升并发性能，避免阻塞线程池

### Client端设计原则（6条）

#### 1. MVVM架构与UnifiedViewModelBase
- **原则**：所有列表管理ViewModel继承UnifiedViewModelBase，统一分页逻辑
- **实现**：UserManagementViewModel继承UnifiedViewModelBase<UserDto>，自动获得分页属性和命令
- **价值**：避免重复代码，所有业务模块列表ViewModel统一使用此模式

#### 2. Repository模式与依赖注入
- **原则**：ViewModel不直接调用IApiService，而是通过Repository抽象数据访问
- **架构层次**：ViewModel → Repository → BaseApiRepository → IApiService → HTTP
- **价值**：解耦、可测试、缓存策略、业务逻辑封装

#### 3. 分页优化与虚拟化
- **问题**：一次性加载所有用户（可能数千条）会导致UI卡顿和内存占用高
- **解决方案**：分页查询（每页10-50条）+ UI虚拟化（VirtualizingStackPanel）+ 按需加载
- **性能目标**：首次加载 <500ms、翻页响应 <200ms、支持1000+用户无卡顿

#### 4. 权限控制与安全性
- **权限分离**：Admin（所有功能）、Doctor（仅查看自己的资料）
- **实现**：构造函数根据角色注册命令（Admin注册所有命令，Doctor仅注册ViewProfileCommand）
- **价值**：UI层权限控制，防止越权操作

#### 5. 对话框通信与参数传递
- **Prism对话框模式**：ShowDialog（显示对话框） + DialogParameters（输入参数） + DialogResult（输出结果）
- **实现**：创建/编辑/重置密码对话框统一使用此模式
- **价值**：解耦对话框与主ViewModel，便于复用和测试

#### 6. 异步优先与用户体验
- **所有I/O操作异步化**：GetItemsAsync、OnExecuteAddAsync、ExecuteResetPasswordAsync等
- **用户体验优化**：IsBusy状态（Loading动画）、CanExecute检查（按钮自动启用/禁用）、友好错误提示、成功反馈（SnackBar + 自动刷新）
- **价值**：UI响应性、防止重复点击、友好反馈

## 🛠 技术栈

### Server端技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| **.NET 8** | 8.0 | 基础框架 |
| **Entity Framework Core** | 8.0 | 数据持久化（通过Repository模式） |
| **ASP.NET Core Identity** | 8.0 | 密码哈希（PasswordHasher） |
| **FluentValidation** | 11.x | DTO数据验证框架 |
| **AutoMapper** | 13.x | Entity与DTO之间的自动映射 |
| **Microsoft.Extensions.DependencyInjection** | 8.0.x | 依赖注入容器 |

### Client端技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| **WPF** | .NET 8 | 桌面UI框架 |
| **Prism.DryIoc** | 8.x | MVVM框架、模块化、依赖注入、区域导航 |
| **MaterialDesignThemes** | 5.1.x | Material Design风格UI组件库 |
| **UnifiedViewModelBase** | 自定义 | 统一的分页、搜索、排序基类 |
| **Microsoft.Extensions.Logging** | 8.0.x | 日志记录 |

## 🚀 快速开始

此模块是类库，作为Server端服务（LYBT.WebAPI）和Client端应用（LYBT.Desktop.Shell）的一部分被引用和托管。无法独立运行。

### Server端集成

```csharp
// Startup.cs (LYBT.WebAPI)
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册用户模块(自动注册仓储+服务+验证器)
        services.AddUsersModule();
    }
}
```

### Client端集成

```csharp
// App.xaml.cs (LYBT.Desktop.Shell)
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Users模块（仅限管理员访问，OnDemand模式）
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

## 📚 相关文档

### 模块文档
- **[用户创建与角色分配](user-creation.md)** *(待创建)* - 详细的用户创建流程和角色管理
- **[密码管理详解](password-management.md)** *(待创建)* - 密码强度验证、修改密码、重置密码
- **[用户状态管理](user-status-management.md)** *(待创建)* - 用户状态切换、最后一个管理员保护

### 开发指南
- **[Server端开发指南](../../../development/server/users-development.md)** *(待创建)* - Server端UserService开发和测试指南
- **[Client端开发指南](../../../development/client/users-development.md)** *(待创建)* - Client端UserManagementViewModel开发和测试指南

### API文档
- **[Users API完整文档](../../../api/users-api.md)** *(待创建)* - 完整的API端点定义、请求/响应示例、错误码说明

### 架构设计
- **[Server端架构设计](../../../architecture/server/users-design.md)** *(待创建)* - Server端架构决策和设计模式
- **[Client端架构设计](../../../architecture/client/users-design.md)** *(待创建)* - Client端MVVM架构和设计原则

---

**最后更新**：2025-10-29
**维护负责**：Server端开发组 + Client端开发组
