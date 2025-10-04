# LYBT.Module.Users

> **用户管理核心模块** - 分层架构精品版
> Admin/Doctor用户管理 | 专为小型中医诊所(<20人)优化
> 模块状态: ✅ **生产就绪** | 🎆 **DTO优化完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Module.Users是系统的用户管理核心模块，采用分层架构设计，提供完整的用户账户管理、角色权限控制和用户信息维护功能。专为小型中医诊所场景优化，支持Admin/Doctor双角色体系。

**技术栈**: .NET 8 + 实体（实体（Entity）） Framework Core 8.0 + AutoMapper + JWT/RBAC
**最新优化**: DTO规范化完成、UserCreateDto/UserUpdateDto分离、类型安全增强

## 🎉 2025-09-20 DTO优化成果

### ✅ 三阶段优化完成
- **第一阶段**: UserMutationDto拆分为UserCreateDto和UserUpdateDto，职责分离
- **第二阶段**: UserPagedQueryDto重命名为UserSearchDto，查询命名规范统一
- **第三阶段**: UserDto.Role从string改为UserRole枚举，类型安全增强

### 🎯 优化前后对比
| 方面 | 优化前 | 优化后 | 改进 |
|------|--------|--------|------|
| DTO命名 | UserPagedQueryDto | UserSearchDto | ✅ 查询命名统一 |
| 创建更新 | UserMutationDto (单一DTO) | UserCreateDto + UserUpdateDto | ✅ 职责分离 |
| 角色字段 | string类型 | UserRole枚举 | ✅ 类型安全 |
| 编译状态 | 存在类型不匹配 | 零错误零警告 | ✅ 生产就绪 |

## 🎆 分层架构设计

**架构层次**:
```
UserService (主服务层 - 纯委托模式)
    │
    ├── UserQueryService (查询专业化层)
    │   ├── 用户搜索和筛选 (SearchUsersAsync)
    │   ├── 角色统计分析 (GetUserStatisticsAsync)
    │   ├── 活跃用户查询 (GetActiveUsersAsync)
    │   └── 复杂条件查询 (GetUsersByRoleAsync)
    │
    └── UserBusinessService (业务逻辑+CRUD层)
        ├── 用户CRUD操作 (Create/Update/Delete/GetById)
        ├── 密码管理 (ChangePasswordAsync, ResetPasswordAsync)
        ├── 用户状态管理 (ActivateAsync, DeactivateAsync)
        └── 角色管理 (UpdateUserRoleAsync)
```

### 核心接口设计

```csharp
// 主服务接口 (统一入口) - 2025-09-20更新
public interface IUserService
{
    // 委托到BusinessService的CRUD操作 - 使用新DTOs
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

    // 委托到QueryService的查询操作 - 标准化命名
    Task<ServiceResult<PagedResult<UserDto>>> SearchAsync(UserSearchDto query);
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string userName);
    Task<ServiceResult<bool>> ValidateUsernameAsync(string userName);
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();

    // 委托到BusinessService的业务操作
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId);
    Task<ServiceResult<bool>> UpdateUserStatusAsync(Guid userId, UserStatus status);
}

// 查询专业化接口 - 2025-09-20更新
public interface IUserQueryService
{
    Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria);
    Task<ServiceResult<List<UserDto>>> GetUsersByRoleAsync(UserRole role);
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
    Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync();
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string userName);
    Task<ServiceResult<bool>> ValidateUsernameAsync(string userName);
}

// 业务逻辑接口 - 2025-09-20更新
public interface IUserBusinessService
{
    Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto);
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UserUpdateDto dto);
    Task<ServiceResult<bool>> DeleteUserAsync(Guid id);
    Task<ServiceResult<UserDto?>> GetByIdAsync(Guid id);
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId);
}
```

## 📦 核心功能模块

### 1. 用户CRUD管理

**创建用户流程**:
```csharp
public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto)
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
        Role = dto.Role,  // UserRole枚举类型
        Status = UserStatus.Active,
        PasswordHash = _passwordHasher.HashPassword(null, dto.Password)
    };

    // 4. 保存到数据库
    var createdUser = await _repository.CreateAsync(user);

    // 5. 返回DTO
    return ServiceResult<UserDto>.Success(_mapper.Map<UserDto>(createdUser));
}
```

### 2. 用户查询和搜索

**高级搜索功能**:
```csharp
public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria)
{
    var query = _repository.GetQueryable();

    // 关键词搜索
    if (!string.IsNullOrWhiteSpace(criteria.Keyword))
    {
        query = query.Where(u =>
            u.Username.Contains(criteria.Keyword) ||
            u.DisplayName.Contains(criteria.Keyword));
    }

    // 角色筛选 (UserRole枚举)
    if (criteria.Role.HasValue)
    {
        query = query.Where(u => u.Role == criteria.Role.Value);
    }

    // 状态筛选
    if (criteria.Status.HasValue)
    {
        query = query.Where(u => u.Status == criteria.Status.Value);
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

## 🧪 数据传输对象 (数据传输对象（数据传输对象（DTO））) - 2025-09-20更新

### 请求DTOs
```csharp
// 创建用户DTO
public class UserCreateDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "角色不能为空")]
    public UserRole Role { get; set; } = UserRole.Doctor;
}

// 更新用户DTO
public class UserUpdateDto
{
    public Guid Id { get; set; }

    [StringLength(100)]
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
```

### 响应DTOs
```csharp
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }  // 枚举类型，原为string
    public UserStatus Status { get; set; }
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

    return new ValidationResult
    {
        IsValid = !errors.Any(),
        Message = errors.Any() ? string.Join("；", errors) : "密码强度验证通过"
    };
}
```

## 🎯 分层架构优势

**适合小型中医诊所(<20人)的精简设计**:
- ✅ **分层架构**: Query+Business分层，职责清晰
- ✅ **类型安全**: UserRole枚举替代字符串，编译时检查
- ✅ **DTO规范**: Create/Update分离，查询命名统一
- ✅ **角色简化**: Admin/Doctor双角色体系
- ✅ **功能完整**: CRUD、搜索、统计、密码管理全覆盖

## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- src/Shared/LYBT.Shared.Interfaces/Api/IUsersApi.cs
## 🚀 使用示例

### 控制器集成
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> SearchUsers(
        [FromQuery] UserSearchDto criteria)
    {
        var result = await _userService.SearchAsync(criteria);
        return HandleServiceResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] UserCreateDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return HandleServiceResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        Guid id, [FromBody] UserUpdateDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return HandleServiceResult(result);
    }
}
```

### 服务注册
```csharp
// 在UsersModule.cs中注册
services.AddScoped<IUserService, UserService>();
services.AddScoped<IUserQueryService, UserQueryService>();
services.AddScoped<IUserBusinessService, UserBusinessService>();
services.AddScoped<IUserRepository, UserRepository>();
```

---

> 📌 **最新成果**: DTO优化三阶段完成，类型安全增强，编译通过
> 🎆 **生产就绪**: 完整的用户管理体系，可直接支撑小型诊所需求

## 🎯 项目概述
- [待补充] 简要描述 LYBT.Module.Users 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

## 🔌 API 接口
- 控制器: 路由前缀: /api/v1/Users
- 控制器: 路由前缀: /api/v1/users/operation

