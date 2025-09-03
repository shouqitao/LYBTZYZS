# LYBT.Module.Users

> **用户管理核心模块** - UltraThink双层架构版  
> Admin/Doctor用户管理 | 专为小型中医诊所(<20人)优化
> **模块状态**: ✅ **生产就绪** | 🎆 **UltraThink双层架构完成** | **零编译错误**

## 🎯 模块概述

LYBT.Module.Users是系统的用户管理核心模块，采用UltraThink双层架构设计，提供完整的用户账户管理、角色权限控制和用户信息维护功能。专为小型中医诊所场景优化，支持Admin/Doctor双角色体系。

**技术栈**: UltraThink双层架构 + Entity Framework Core + AutoMapper + RBAC权限

## 🎆 UltraThink双层架构设计

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
        ├── 角色管理 (UpdateUserRoleAsync)
        └── 业务验证逻辑 (ValidateUserData)
```

### 核心接口设计

```csharp
// 主服务接口 (统一入口)
public interface IUserService
{
    // 委托到BusinessService的CRUD操作
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto dto);
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task<ServiceResult<bool>> DeleteUserAsync(Guid id);
    Task<ServiceResult<UserDto?>> GetByIdAsync(Guid id);
    
    // 委托到QueryService的查询操作
    Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria);
    Task<ServiceResult<List<UserDto>>> GetUsersByRoleAsync(UserRole role);
    Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync();
    
    // 委托到BusinessService的业务操作
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid userId);
    Task<ServiceResult<bool>> UpdateUserStatusAsync(Guid userId, UserStatus status);
}

// 查询专业化接口
public interface IUserQueryService
{
    Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria);
    Task<ServiceResult<List<UserDto>>> GetUsersByRoleAsync(UserRole role);
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
    Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync();
}

// 业务逻辑接口
public interface IUserBusinessService
{
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto dto);
    Task<ServiceResult<UserDto>> UpdateUserAsync(Guid id, UpdateUserDto dto);
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
public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto dto)
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
        Role = dto.Role,
        Status = UserStatus.Active,
        PasswordHash = _passwordHasher.HashPassword(null, dto.Password)
    };
    
    // 4. 保存到数据库
    var createdUser = await _repository.CreateAsync(user);
    
    // 5. 记录操作日志
    _logger.LogInformation("用户 {Username} 创建成功，角色: {Role}", 
        dto.Username, dto.Role);
    
    // 6. 返回DTO
    return ServiceResult<UserDto>.Success(_mapper.Map<UserDto>(createdUser));
}
```

### 2. 用户查询和搜索

**高级搜索功能**:
```csharp
public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria)
{
    try
    {
        var query = _repository.GetQueryable();
        
        // 关键词搜索 (用户名、显示名)
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            query = query.Where(u => 
                u.Username.Contains(criteria.Keyword) ||
                (u.DisplayName != null && u.DisplayName.Contains(criteria.Keyword)));
        }
        
        // 角色筛选
        if (criteria.Role.HasValue)
        {
            query = query.Where(u => u.Role == criteria.Role.Value);
        }
        
        // 状态筛选
        if (criteria.Status.HasValue)
        {
            query = query.Where(u => u.Status == criteria.Status.Value);
        }
        
        // 创建时间范围
        if (criteria.CreateTimeFrom.HasValue)
        {
            query = query.Where(u => u.CreateTime >= criteria.CreateTimeFrom.Value);
        }
        if (criteria.CreateTimeTo.HasValue)
        {
            query = query.Where(u => u.CreateTime <= criteria.CreateTimeTo.Value);
        }
        
        // 排序
        query = criteria.SortBy?.ToLower() switch
        {
            "username" => criteria.SortDescending ? 
                query.OrderByDescending(u => u.Username) : 
                query.OrderBy(u => u.Username),
            "createtime" => criteria.SortDescending ? 
                query.OrderByDescending(u => u.CreateTime) : 
                query.OrderBy(u => u.CreateTime),
            _ => query.OrderBy(u => u.CreateTime) // 默认按创建时间排序
        };
        
        // 分页查询
        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((criteria.PageIndex - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync();
            
        var userDtos = _mapper.Map<List<UserDto>>(users);
        
        var result = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            PageIndex = criteria.PageIndex,
            PageSize = criteria.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize)
        };
        
        return ServiceResult<PagedResult<UserDto>>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "搜索用户时发生错误");
        return ServiceResult<PagedResult<UserDto>>.Failure("搜索用户失败");
    }
}
```

### 3. 密码管理

**密码修改流程**:
```csharp
public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
{
    try
    {
        // 1. 获取用户
        var user = await _repository.GetByIdAsync(userId);
        if (user == null)
            return ServiceResult<bool>.Failure("用户不存在");
            
        // 2. 验证当前密码
        var isCurrentPasswordValid = _passwordHasher
            .VerifyHashedPassword(null, user.PasswordHash, dto.CurrentPassword);
        if (isCurrentPasswordValid == PasswordVerificationResult.Failed)
            return ServiceResult<bool>.Failure("当前密码错误");
            
        // 3. 验证新密码强度
        var passwordValidation = ValidatePasswordStrength(dto.NewPassword);
        if (!passwordValidation.IsValid)
            return ServiceResult<bool>.Failure(passwordValidation.Message);
            
        // 4. 生成新密码哈希
        var newPasswordHash = _passwordHasher.HashPassword(null, dto.NewPassword);
        
        // 5. 更新密码
        user.PasswordHash = newPasswordHash;
        user.UpdateTime = DateTime.Now;
        
        await _repository.UpdateAsync(user);
        
        // 6. 记录操作日志
        _logger.LogInformation("用户 {UserId} 修改密码成功", userId);
        
        return ServiceResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "修改密码时发生错误，用户ID: {UserId}", userId);
        return ServiceResult<bool>.Failure("修改密码失败");
    }
}
```

### 4. 用户统计分析

**统计数据生成**:
```csharp
public async Task<ServiceResult<UserStatisticsDto>> GetUserStatisticsAsync()
{
    try
    {
        var totalUsers = await _repository.CountAsync();
        var activeUsers = await _repository.CountAsync(u => u.Status == UserStatus.Active);
        var adminCount = await _repository.CountAsync(u => u.Role == UserRole.Admin);
        var doctorCount = await _repository.CountAsync(u => u.Role == UserRole.Doctor);
        
        // 近30天新增用户
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        var newUsersLast30Days = await _repository
            .CountAsync(u => u.CreateTime >= thirtyDaysAgo);
            
        // 最近登录统计
        var recentLoginUsers = await _context.AuthSessions
            .Where(s => s.LoginTime >= thirtyDaysAgo)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync();
            
        var statistics = new UserStatisticsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = totalUsers - activeUsers,
            AdminCount = adminCount,
            DoctorCount = doctorCount,
            NewUsersLast30Days = newUsersLast30Days,
            RecentLoginUsers = recentLoginUsers,
            ActivityRate = totalUsers > 0 ? (double)recentLoginUsers / totalUsers * 100 : 0
        };
        
        return ServiceResult<UserStatisticsDto>.Success(statistics);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取用户统计数据时发生错误");
        return ServiceResult<UserStatisticsDto>.Failure("获取统计数据失败");
    }
}
```

## 🔧 Repository层设计

### UserRepository
```csharp
public class UserRepository : BaseRepository<UserModel>, IUserRepository
{
    public async Task<UserModel?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
    }
    
    public async Task<List<UserModel>> GetByRoleAsync(UserRole role)
    {
        return await _context.Users
            .Where(u => u.Role == role && !u.IsDeleted)
            .OrderBy(u => u.CreateTime)
            .ToListAsync();
    }
    
    public async Task<List<UserModel>> GetActiveUsersAsync()
    {
        return await _context.Users
            .Where(u => u.Status == UserStatus.Active && !u.IsDeleted)
            .OrderBy(u => u.DisplayName ?? u.Username)
            .ToListAsync();
    }
    
    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeUserId = null)
    {
        var query = _context.Users
            .Where(u => u.Username == username && !u.IsDeleted);
            
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }
        
        return await query.AnyAsync();
    }
    
    public async Task<int> CountByRoleAsync(UserRole role)
    {
        return await _context.Users
            .CountAsync(u => u.Role == role && !u.IsDeleted);
    }
}
```

## 🧪 数据传输对象 (DTOs)

### 请求DTOs
```csharp
public record CreateUserDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50字符")]
    public string Username { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度必须在8-100字符之间")]
    public string Password { get; init; } = string.Empty;
    
    [StringLength(100, ErrorMessage = "显示名长度不能超过100字符")]
    public string? DisplayName { get; init; }
    
    [Required(ErrorMessage = "用户角色不能为空")]
    public UserRole Role { get; init; }
}

public record UpdateUserDto
{
    [StringLength(100, ErrorMessage = "显示名长度不能超过100字符")]
    public string? DisplayName { get; init; }
    
    public UserStatus? Status { get; init; }
    
    public UserRole? Role { get; init; }
}

public record UserSearchDto : PagedRequestDto
{
    public string? Keyword { get; init; }
    public UserRole? Role { get; init; }
    public UserStatus? Status { get; init; }
    public DateTime? CreateTimeFrom { get; init; }
    public DateTime? CreateTimeTo { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = false;
}

public record ChangePasswordDto
{
    [Required(ErrorMessage = "当前密码不能为空")]
    public string CurrentPassword { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "新密码长度必须在8-100字符之间")]
    public string NewPassword { get; init; } = string.Empty;
    
    [Compare(nameof(NewPassword), ErrorMessage = "确认密码与新密码不匹配")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
```

### 响应DTOs
```csharp
public record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public UserStatus Status { get; init; }
    public DateTime CreateTime { get; init; }
    public DateTime? UpdateTime { get; init; }
    public string RoleDisplayName => Role == UserRole.Admin ? "管理员" : "医生";
    public string StatusDisplayName => Status switch
    {
        UserStatus.Active => "正常",
        UserStatus.Inactive => "停用",
        UserStatus.Locked => "锁定",
        _ => "未知"
    };
}

public record UserStatisticsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int InactiveUsers { get; init; }
    public int AdminCount { get; init; }
    public int DoctorCount { get; init; }
    public int NewUsersLast30Days { get; init; }
    public int RecentLoginUsers { get; init; }
    public double ActivityRate { get; init; }
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
        
    if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c)))
        errors.Add("密码必须包含特殊字符");
    
    return new ValidationResult
    {
        IsValid = !errors.Any(),
        Message = errors.Any() ? string.Join("；", errors) : "密码强度验证通过"
    };
}
```

### 用户数据验证
```csharp
private async Task<ServiceResult> ValidateCreateUserAsync(CreateUserDto dto)
{
    // 用户名格式验证
    if (!Regex.IsMatch(dto.Username, @"^[a-zA-Z0-9_]{3,50}$"))
        return ServiceResult.Failure("用户名只能包含字母、数字和下划线，长度3-50字符");
    
    // 用户名唯一性验证
    if (await _repository.UsernameExistsAsync(dto.Username))
        return ServiceResult.Failure("用户名已存在");
    
    // 密码强度验证
    var passwordValidation = ValidatePasswordStrength(dto.Password);
    if (!passwordValidation.IsValid)
        return ServiceResult.Failure(passwordValidation.Message);
    
    return ServiceResult.Success();
}
```

## 🎯 UltraThink架构优势

**适合小型中医诊所(<20人)的精简设计**:
- ✅ **双层架构**: Query+Business分层，职责清晰，易于维护
- ✅ **角色简化**: Admin/Doctor双角色，避免复杂权限体系
- ✅ **功能完整**: 用户CRUD、搜索、统计、密码管理全覆盖
- ✅ **安全可靠**: 密码强度验证、数据验证、操作日志
- ✅ **高性能**: 分页查询、索引优化、缓存支持

## 📚 相关文档

- [Auth认证模块](../LYBT.Module.Auth/README.md) - 用户认证和授权
- [Infrastructure基础设施](../../Core/LYBT.Infrastructure/README.md) - Repository基类和数据访问
- [API接口文档](../../Services/LYBT.WebAPI/README.md) - Users控制器API

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
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> SearchUsers([FromQuery] UserSearchDto criteria)
    {
        var result = await _userService.SearchUsersAsync(criteria);
        return HandleServiceResult(result, "获取用户列表成功");
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateUserAsync(dto);
        return HandleServiceResult(result, "创建用户成功");
    }
    
    [HttpPut("password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.ChangePasswordAsync(userId, dto);
        return HandleServiceResult(result, "修改密码成功");
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

> 📌 **UltraThink成果**: Users模块采用双层架构设计，功能完整，安全可靠
> 🎆 **生产就绪**: 零编译错误，完整的用户管理体系，可直接支撑小型诊所用户管理需求