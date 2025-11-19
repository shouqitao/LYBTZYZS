# LYBT.Module.Users - 用户管理模块

## 📦 项目定位

- **层级**:Server端
- **类型**:业务模块(用户管理)
- **职责**:提供用户账户管理、角色权限控制和用户信息维护功能。支持Admin/Doctor双角色体系，包含用户CRUD、密码管理、状态管理、角色分配等核心功能。专为小型中医诊所(<20人)优化，采用标准三层架构（Controller → Service → Repository）,确保业务逻辑清晰、数据访问高效。

## 📂 代码结构

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
│       ├── GetByIdAsync()            # 按ID查询用户
│       ├── GetAllAsync()             # 获取所有用户
│       ├── FindAsync()               # 条件查询用户
│       ├── GetPagedAsync()           # 分页查询(两个重载)
│       ├── GetSingleAsync()          # 单条件查询
│       ├── ExistsAsync()             # 存在性检查(两个重载)
│       ├── CountAsync()              # 统计数量(两个重载)
│       ├── AddAsync()                # 添加用户
│       ├── AddRangeAsync()           # 批量添加
│       ├── UpdateAsync()             # 更新用户
│       ├── DeleteAsync()             # 删除用户(两个重载)
│       ├── DeleteRangeAsync()        # 批量删除(两个重载)
│       ├── SaveChangesAsync()        # 保存变更
│       ├── GetByUsernameAsync()      # 按用户名查询
│       ├── GetByEmailAsync()         # 按邮箱查询
│       ├── IsUsernameExistsAsync()   # 检查用户名是否存在
│       └── IsEmailExistsAsync()      # 检查邮箱是否存在
├── Validators/                       # FluentValidation验证器
│   ├── UserCreateDtoValidator.cs     # 创建用户DTO验证
│   └── UserUpdateDtoValidator.cs     # 更新用户DTO验证
└── Mapping/                          # AutoMapper映射配置
    └── UserMappingProfile.cs         # Entity ↔ DTO映射规则
```

**说明**:
- **UsersModule**:依赖注入注册中心,统一注册仓储、服务和验证器
- **UserService**:19个方法覆盖用户的增删改查、密码管理、状态管理、批量操作等功能
- **UserRepository**:25个方法提供完整的数据访问能力(CRUD、分页、条件查询、用户名/邮箱唯一性检查)
- **Admin/Doctor双角色体系**:通过UserRole枚举控制用户角色
- **Validators**:FluentValidation验证器确保DTO数据完整性
- **Mapping**:AutoMapper配置统一处理Entity与DTO的转换

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Entities** - 数据实体定义(UserModel、UserRole、UserStatus枚举)
2. **LYBT.Infrastructure** - 基础设施(AppDbContext、BaseRepository)
3. **LYBT.Shared.Models** - 共享DTO模型(UserDto、UserCreateDto、UserUpdateDto等)
4. **LYBT.Server.Interfaces** - Server端接口定义(IUserService、IUserRepository)

### 被依赖项目
1. **LYBT.Module.Auth** - 认证模块依赖用户模块进行身份验证
2. **LYBT.Module.MedicalCase** - 医案模块通过DoctorId关联医生用户
3. **LYBT.Module.Patients** - 患者模块可能关联创建人用户
4. **LYBT.WebAPI** - Web服务层通过UsersController暴露API
5. **测试项目**:
   - LYBT.Module.Users.Tests（单元测试）
   - LYBT.Module.Users.IntegrationTests（集成测试）
   - LYBT.Server.ArchTests（架构测试）

### NuGet包
- **FluentValidation** (11.x) - DTO验证框架
- **AutoMapper** (13.x) - 对象映射框架
- **Microsoft.Extensions.DependencyInjection** (8.0.x) - 依赖注入容器
- **Microsoft.AspNetCore.Identity** (8.0.x) - 密码哈希（PasswordHasher）

## 🛠 技术栈

- **.NET 8**: 基础框架
- **Entity Framework Core 8**: 通过Repository模式间接使用,用于数据持久化
- **AutoMapper 13.x**: Entity与DTO之间的自动映射
- **FluentValidation 11.x**: DTO数据验证框架
- **ASP.NET Core Identity**: 密码哈希和安全管理
- **LINQ**: 复杂查询表达式(分页、搜索、过滤)
- **异步编程**: 全异步方法(async/await),提升性能

## 🎉 2025-09-20 DTO优化成果

###  三阶段优化完成
- **第一阶段**: UserMutationDto拆分为UserCreateDto和UserUpdateDto，职责分离
- **第二阶段**: UserPagedQueryDto重命名为UserSearchDto，查询命名规范统一
- **第三阶段**: UserDto.Role从string改为UserRole枚举，类型安全增强

### 🎯 优化前后对比
| 方面 | 优化前 | 优化后 | 改进 |
|------|--------|--------|------|
| DTO命名 | UserPagedQueryDto | UserSearchDto |  查询命名统一 |
| 创建更新 | UserMutationDto (单一DTO) | UserCreateDto + UserUpdateDto |  职责分离 |
| 角色字段 | string类型 | UserRole枚举 |  类型安全 |
| 编译状态 | 存在类型不匹配 | 零错误零警告 |  生产就绪 |

##  快速开始

此项目是一个类库,作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj
```

**集成说明**:

### 1. 注册用户模块(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册用户模块(自动注册仓储+服务+验证器)
        services.AddUsersModule();
    }
}
```

### 2. API Controller集成(在LYBT.WebAPI中)
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // 分页查询用户
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] UserRole? role = null,
        [FromQuery] UserStatus? status = null)
    {
        var query = new UserSearchDto
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            Role = role,
            Status = status
        };

        var result = await _userService.SearchAsync(query);
        return Ok(result);
    }

    // 创建用户
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return CreatedAtAction(nameof(GetUserById), new { id = result.Data.Id }, result.Data);
    }
}
```

### 3. 用户创建流程(业务逻辑)
```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IMapper _mapper;

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
        var user = new User
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
}
```

### 4. 用户搜索功能(高级查询)
```csharp
public async Task<ServiceResult<PagedResult<UserDto>>> SearchAsync(UserSearchDto criteria)
{
    var query = _repository.GetQueryable();

    // 关键词搜索(用户名或显示名称)
    if (!string.IsNullOrWhiteSpace(criteria.Keyword))
    {
        query = query.Where(u =>
            u.Username.Contains(criteria.Keyword) ||
            u.DisplayName.Contains(criteria.Keyword));
    }

    // 角色筛选(UserRole枚举)
    if (criteria.Role.HasValue)
    {
        query = query.Where(u => u.Role == criteria.Role.Value);
    }

    // 状态筛选(Active/Inactive/Locked)
    if (criteria.Status.HasValue)
    {
        query = query.Where(u => u.Status == criteria.Status.Value);
    }

    // 创建时间范围筛选
    if (criteria.CreateTimeFrom.HasValue)
    {
        query = query.Where(u => u.CreateTime >= criteria.CreateTimeFrom.Value);
    }
    if (criteria.CreateTimeTo.HasValue)
    {
        query = query.Where(u => u.CreateTime <= criteria.CreateTimeTo.Value);
    }

    // 排序
    if (!string.IsNullOrWhiteSpace(criteria.SortBy))
    {
        query = criteria.SortDescending
            ? query.OrderByDescending(u => EF.Property<object>(u, criteria.SortBy))
            : query.OrderBy(u => EF.Property<object>(u, criteria.SortBy));
    }
    else
    {
        query = query.OrderByDescending(u => u.CreateTime);  // 默认按创建时间倒序
    }

    // 分页查询
    var totalCount = await query.CountAsync();
    var users = await query
        .Skip((criteria.PageIndex - 1) * criteria.PageSize)
        .Take(criteria.PageSize)
        .ToListAsync();

    var userDtos = _mapper.Map<List<UserDto>>(users);

    return ServiceResult<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
    {
        Items = userDtos,
        TotalCount = totalCount,
        PageIndex = criteria.PageIndex,
        PageSize = criteria.PageSize
    });
}
```

### 5. 密码管理(修改密码)
```csharp
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

### 6. 用户状态管理(启用/禁用)
```csharp
// 启用用户
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

// 禁用用户
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

### 7. 批量删除功能
```csharp
public async Task<ServiceResult<BatchOperationResult>> BatchDeleteAsync(List<Guid> userIds)
{
    var result = new BatchOperationResult
    {
        TotalCount = userIds.Count
    };

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

## 🧪 数据传输对象 (DTO) - 2025-09-20更新

### 请求DTOs
```csharp
// 创建用户DTO
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

// 更新用户DTO
public class UserUpdateDto
{
    public Guid Id { get; set; }

    [StringLength(100, ErrorMessage = "显示名称长度不能超过100字符")]
    public string? DisplayName { get; set; }

    public UserRole? Role { get; set; }

    public UserStatus? Status { get; set; }
}

// 用户搜索DTO (原UserPagedQueryDto)
public class UserSearchDto : PagedRequestDto
{
    public string? Keyword { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    public DateTime? CreateTimeFrom { get; set; }
    public DateTime? CreateTimeTo { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

// 修改密码DTO
public class ChangePasswordDto
{
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### 响应DTOs
```csharp
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

public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int AdminCount { get; set; }
    public int DoctorCount { get; set; }
    public int NewUsersLast30Days { get; set; }
    public double ActivityRate { get; set; }
}

public class BatchOperationResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public List<BatchOperationError> FailedItems { get; set; } = new();
}

public class BatchOperationError
{
    public string ItemId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
```

## 🔒 数据验证和安全

### 密码强度验证
```csharp
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

### 用户名唯一性检查
```csharp
public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
{
    // 1. 格式验证(3-50字符，字母数字下划线)
    if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
        return ServiceResult<bool>.Failure("用户名长度应在3-50字符之间");

    if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        return ServiceResult<bool>.Failure("用户名只能包含字母、数字和下划线");

    // 2. 唯一性检查
    var exists = await _repository.IsUsernameExistsAsync(username);
    if (exists)
        return ServiceResult<bool>.Failure("用户名已存在");

    return ServiceResult<bool>.Success(true);
}
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `UsersController` 对外暴露。

- **API路由前缀**: `/api/v1/users`

**主要端点**:
- `GET /api/v1/users` - 分页查询用户
- `GET /api/v1/users/{id}` - 按ID查询用户详情
- `GET /api/v1/users/username/{username}` - 按用户名查询用户
- `GET /api/v1/users/statistics` - 获取用户统计信息
- `POST /api/v1/users` - 创建用户
- `PUT /api/v1/users/{id}` - 更新用户
- `DELETE /api/v1/users/{id}` - 删除用户
- `POST /api/v1/users/batch-delete` - 批量删除用户
- `POST /api/v1/users/{id}/enable` - 启用用户
- `POST /api/v1/users/{id}/disable` - 禁用用户
- `POST /api/v1/users/{id}/toggle-status` - 切换用户状态
- `POST /api/v1/users/{id}/change-password` - 修改密码
- `POST /api/v1/users/{id}/reset-password` - 重置密码
- `PUT /api/v1/users/{id}/profile` - 修改用户资料
- `GET /api/v1/users/validate-username/{username}` - 验证用户名是否可用

**完整API定义**请参考 `IUserService` 接口和 `UsersController` 的实现。

## 🎯 架构优势

**适合小型中医诊所(<20人)的精简设计**:
-  **标准三层架构**: Controller → Service → Repository，职责清晰
-  **类型安全**: UserRole枚举替代字符串，编译时检查
-  **DTO规范**: Create/Update分离，查询命名统一
-  **角色简化**: Admin/Doctor双角色体系，满足小诊所需求
-  **功能完整**: CRUD、搜索、统计、密码管理、批量操作全覆盖
-  **数据验证**: FluentValidation + 业务逻辑双重验证
-  **安全性**: ASP.NET Core Identity密码哈希，强密码策略

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/users/](../../../../docs/reference/modules/users/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/users-design.md](../../../../docs/explanation/architecture/server/users-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/users-development.md](../../../../docs/how-to-guides/server/users-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组
