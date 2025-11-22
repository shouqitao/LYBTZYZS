# Users模块架构文档

**版本**: v1.0
**更新时间**: 2025-11-10
**状态**: ✅ 已完成

---

## 📦 模块概览

**Users模块**负责用户基础数据管理，包括CRUD、用户认证、角色管理、状态管理。

### 核心功能

1. **基础CRUD**
   - 创建/更新/删除用户
   - 分页查询、按角色/状态筛选
   - 用户名/邮箱/真实姓名搜索

2. **认证与安全**
   - 用户名登录（支持用户名或邮箱）
   - 密码Hash存储
   - 管理员重置密码（支持自动生成临时密码）
   - 用户更改密码

3. **状态管理**（Issue #1162）
   - 启用/禁用用户
   - 切换用户状态
   - 状态筛选查询

4. **批量操作**（Issue #1169）
   - 批量删除用户（软删除）
   - 最多100条限制

---

## 🏗️ 架构设计

### 三层架构

```
┌──────────────────────────────────────────────────┐
│  Presentation Layer (UsersController)            │
│  - GET  /api/v1/users (分页查询+筛选)            │
│  - GET  /api/v1/users/current (当前用户)         │
│  - GET  /api/v1/users/{id} (用户详情)            │
│  - POST /api/v1/users (创建用户)                 │
│  - PUT  /api/v1/users/{id} (更新用户)            │
│  - DELETE /api/v1/users/{id} (软删除)            │
│  - POST /api/v1/users/batch-delete (批量删除)    │
│  - PUT  /api/v1/users/{id}/toggle-status         │
│  - POST /api/v1/users/{id}/reset-password        │
│  - POST /api/v1/users/{id}/change-password       │
└──────────────────────────────────────────────────┘
                       ↓
┌──────────────────────────────────────────────────┐
│  Application Layer (UserService)                 │
│  - GetPagedAsync(page, size, keyword, role...)   │
│  - GetByIdAsync(id)                              │
│  - CreateAsync(dto)                              │
│  - UpdateAsync(id, dto)                          │
│  - DeleteAsync(id)                               │
│  - BatchDeleteAsync(ids)                         │
│  - ToggleStatusAsync(id)                         │
│  - ResetPasswordAsync(id, newPassword)           │
│  - ChangePasswordAsync(id, oldPwd, newPwd)       │
│  - ChangeProfileAsync(userId, dto)               │
└──────────────────────────────────────────────────┘
                       ↓
┌──────────────────────────────────────────────────┐
│  Infrastructure Layer (UserRepository)           │
│  - GetByUsernameAsync(username)                  │
│  - IsUsernameExistsAsync(username)               │
│  + IBaseRepository<User>标准CRUD方法             │
└──────────────────────────────────────────────────┘
```

---

## 📋 Repository层

### IUserRepository接口

```csharp
namespace LYBT.Module.Users.Interfaces;

/// <summary>
/// 用户仓储接口 - 继承IBaseRepository<User>标准接口
/// Phase 1 Task 1.2: 实现基础数据模块统一Repository规范
/// </summary>
/// <remarks>
/// 设计原则：
/// - ⭐ 统一共性：继承IBaseRepository<User>获得11个标准CRUD方法
/// - ⭐ 保持特性：保留用户模块特定业务方法
/// </remarks>
public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>
    /// 根据用户名获取用户（支持用户名或邮箱登录）
    /// </summary>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// 检查用户名是否已存在
    /// </summary>
    Task<bool> IsUsernameExistsAsync(string username);
}
```

### 关键实现要点

**1. 用户名登录（支持用户名或邮箱）**：

```csharp
public async Task<User?> GetByUsernameAsync(string username)
{
    return await DbContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => 
            (u.UserName == username || u.Email == username) 
            && !u.IsDeleted);
}
```

**2. 用户名唯一性检查**：

```csharp
public async Task<bool> IsUsernameExistsAsync(string username)
{
    return await DbContext.Users
        .AsNoTracking()
        .AnyAsync(u => u.UserName == username && !u.IsDeleted);
}
```

---

## 📋 Service层

### IUserService接口

```csharp
namespace LYBT.Module.Users.Interfaces;

/// <summary>
/// 用户服务统一接口 - 标准CRUD模式
/// Issue #1008: 重构为标准接口，移除过度设计方法
/// </summary>
public interface IUserService
{
    #region 查询操作

    /// <summary>
    /// 分页获取用户列表（Issue #1162: 支持角色和状态筛选）
    /// </summary>
    Task<Result<PagedResult<UserDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        UserRole? role = null,
        CommonStatus? status = null);

    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    Task<Result<UserDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 搜索用户（返回所有匹配结果）
    /// </summary>
    Task<Result<List<UserDto>>> SearchAsync(string keyword);

    #endregion

    #region 业务操作

    /// <summary>
    /// 创建用户
    /// </summary>
    Task<Result<UserDto>> CreateAsync(UserInputDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户
    /// </summary>
    Task<Result<UserDto>> UpdateAsync(Guid id, UserInputDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户（软删除）
    /// </summary>
    Task<Result> DeleteAsync(Guid id);

    /// <summary>
    /// 批量删除用户（软删除）(Issue #1169)
    /// </summary>
    Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

    /// <summary>
    /// 切换用户状态 (Issue #1162)
    /// </summary>
    Task<Result<UserDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 管理员重置密码（Issue #1162: 支持自动生成临时密码）
    /// </summary>
    Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request);

    /// <summary>
    /// 更改密码
    /// </summary>
    Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

    /// <summary>
    /// 修改个人信息 (Issue #1888)
    /// </summary>
    Task<Result<UserDto>> ChangeProfileAsync(Guid userId, ChangeProfileDto dto);

    #endregion
}
```

### 关键实现要点

**1. 分页查询（支持多条件筛选）**：

```csharp
public async Task<Result<PagedResult<UserDto>>> GetPagedAsync(
    int page = 1,
    int pageSize = 20,
    string? keyword = null,
    UserRole? role = null,
    CommonStatus? status = null)
{
    var query = _repository.Query()
        .Where(u => !u.IsDeleted);

    // 关键字搜索（用户名/邮箱/真实姓名）
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(u => 
            u.UserName.Contains(keyword) ||
            u.Email!.Contains(keyword) ||
            u.RealName.Contains(keyword));
    }

    // 角色筛选
    if (role.HasValue)
        query = query.Where(u => u.Role == role.Value);

    // 状态筛选
    if (status.HasValue)
        query = query.Where(u => u.Status == status.Value);

    var pagedResult = await _repository.GetPagedAsync(query, page, pageSize);
    var dtoList = _mapper.Map<List<UserDto>>(pagedResult.Items);

    return Result<PagedResult<UserDto>>.Success(
        new PagedResult<UserDto>(dtoList, pagedResult.TotalCount, page, pageSize));
}
```

**2. 创建用户（用户名唯一性检查 + 密码Hash）**：

```csharp
public async Task<Result<UserDto>> CreateAsync(UserInputDto dto, CancellationToken cancellationToken = default)
{
    // 1. 用户名唯一性检查
    if (await _repository.IsUsernameExistsAsync(dto.UserName))
        return Result<UserDto>.Fail("用户名已存在");

    // 2. 邮箱唯一性检查
    if (!string.IsNullOrEmpty(dto.Email) && 
        await _repository.Query().AnyAsync(u => u.Email == dto.Email && !u.IsDeleted))
        return Result<UserDto>.Fail("邮箱已被使用");

    // 3. ⭐ 密码Hash（使用BCrypt）
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

    var user = _mapper.Map<User>(dto);
    user.PasswordHash = passwordHash;
    user.Status = CommonStatus.Enabled;

    await _repository.AddAsync(user);
    await _unitOfWork.SaveChangesAsync();

    var resultDto = _mapper.Map<UserDto>(user);
    return Result<UserDto>.Success(resultDto);
}
```

**3. 密码管理**：

```csharp
public async Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
{
    var user = await _repository.GetByIdAsync(id);
    if (user == null)
        return Result.Fail("用户不存在");

    // 1. ⭐ 验证旧密码
    if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
        return Result.Fail("原密码错误");

    // 2. ⭐ Hash新密码
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

    await _repository.UpdateAsync(user);
    await _unitOfWork.SaveChangesAsync();

    return Result.Success();
}
```

**4. 批量删除（Issue #1169）**：

```csharp
public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
{
    // BR-001: 批量删除数量限制（≤100条）
    if (ids.Count > 100)
        return Result<BatchOperationResultDto>.Fail("单次批量删除最多支持100条记录");

    var result = new BatchOperationResultDto
    {
        TotalCount = ids.Count
    };

    foreach (var id in ids)
    {
        try
        {
            var user = await _repository.GetByIdAsync(id);
            if (user == null)
            {
                result.FailureCount++;
                result.FailedItems.Add(new FailedItem
                {
                    Id = id.ToString(),
                    Reason = "用户不存在"
                });
                continue;
            }

            // ⭐ 软删除
            user.IsDeleted = true;
            user.DeletedAt = DateTime.Now;
            await _repository.UpdateAsync(user);

            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.FailureCount++;
            result.FailedItems.Add(new FailedItem
            {
                Id = id.ToString(),
                Reason = ex.Message
            });
        }
    }

    // ⭐ 一次性保存（事务保证）
    await _unitOfWork.SaveChangesAsync();

    result.Message = $"批量删除完成: 成功{result.SuccessCount}条, 失败{result.FailureCount}条";
    return Result<BatchOperationResultDto>.Success(result);
}
```

---

## 📋 Controller端点

### API端点列表

| 端点 | 方法 | 说明 | 业务规则 | Issue |
|-----|------|------|---------|-------|
| `/api/v1/users` | GET | 分页查询用户列表 | 支持keyword、role、status筛选 | #1162 |
| `/api/v1/users/current` | GET | 获取当前登录用户 | 超级管理员特殊处理 | - |
| `/api/v1/users/{id}` | GET | 查询用户详情 | - | - |
| `/api/v1/users` | POST | 创建用户 | BR-002（用户名唯一）<br>BR-003（邮箱唯一）<br>BR-004（密码Hash） | - |
| `/api/v1/users/{id}` | PUT | 更新用户 | BR-002, BR-003 | - |
| `/api/v1/users/{id}` | DELETE | 删除用户（软删除） | BR-005（软删除） | - |
| `/api/v1/users/batch-delete` | POST | 批量删除 | BR-001（≤100条）<br>BR-005（软删除） | #1169 |
| `/api/v1/users/{id}/toggle-status` | PUT | 切换用户状态 | - | #1162 |
| `/api/v1/users/{id}/reset-password` | POST | 管理员重置密码 | BR-006（支持自动生成） | #1162 |
| `/api/v1/users/{id}/change-password` | POST | 用户更改密码 | BR-007（验证旧密码） | - |
| `/api/v1/users/{id}/profile` | PUT | 修改个人资料 | - | #1888 |

### 关键端点实现

**分页查询端点（支持多条件筛选）**：

```csharp
/// <summary>
/// 获取用户列表（分页）（Issue #1162: 支持角色和状态筛选）
/// </summary>
[HttpGet]
[ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), 200)]
[ProducesResponseType(400)]
public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
    int page = 1,
    int pageSize = 20,
    string? keyword = null,
    UserRole? role = null,
    CommonStatus? status = null)
{
    try
    {
        var result = await _userService.GetPagedAsync(page, pageSize, keyword, role, status);
        return HandlePagedResult(result);
    }
    catch (Exception ex)
    {
        return HandleExceptionPaged<UserDto>(ex, "获取用户列表");
    }
}
```

**当前用户端点（支持超级管理员）**：

```csharp
/// <summary>
/// 获取当前登录用户信息
/// </summary>
[HttpGet("current")]
[ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
{
    try
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized<UserDto>("无法获取当前用户信息");

        // ⭐ 特殊处理超级管理员（userId == Guid.Empty）
        if (userId == Guid.Empty)
        {
            var username = User.Identity?.Name ?? "sysadmin";
            var isSuperAdmin = User.FindFirst("IsSuperAdmin")?.Value == "true";

            if (isSuperAdmin)
            {
                return Success(new UserDto
                {
                    Id = Guid.Empty,
                    UserName = username,
                    RealName = "系统超级管理员",
                    Role = UserRole.Admin,
                    Email = "admin@lybt.com",
                    Status = CommonStatus.Enabled
                });
            }
        }

        var result = await _userService.GetByIdAsync(userId);
        return HandleResult(result);
    }
    catch (Exception ex)
    {
        return HandleException<UserDto>(ex, "获取当前用户信息");
    }
}
```

---

## 📊 性能基准

### 核心操作性能要求

| 操作 | 数据量 | 性能要求 | 实际性能（InMemory） | 优化措施 |
|-----|-------|---------|---------------------|---------|
| **分页查询** | 100条取20条 | < 500ms | ~91μs | AsNoTracking()、索引优化 |
| **单条创建** | 1条 | < 300ms | ~18ms | BCrypt Hash、事务优化 |
| **批量删除** | 100条 | < 10s | - | 软删除、一次性SaveChanges |

### 索引优化

```sql
-- 唯一索引（用户名，支持快速重复检查和登录查询）
CREATE UNIQUE INDEX IX_Users_UserName
ON Users(UserName)
WHERE IsDeleted = 0;

-- 唯一索引（邮箱，支持邮箱登录和重复检查）
CREATE UNIQUE INDEX IX_Users_Email
ON Users(Email)
WHERE IsDeleted = 0;

-- 复合索引（角色+状态，支持筛选查询）
CREATE INDEX IX_Users_Role_Status
ON Users(Role, Status)
INCLUDE (UserName, RealName, Email)
WHERE IsDeleted = 0;
```

---

## 📚 业务规则

| 规则ID | 描述 | 验证层 | 实现位置 |
|--------|------|--------|---------|
| **BR-001** | 批量删除数量限制（≤100条） | Service层 | UserService.BatchDeleteAsync |
| **BR-002** | 用户名唯一性 | Service层 | UserService.CreateAsync/UpdateAsync |
| **BR-003** | 邮箱唯一性 | Service层 | UserService.CreateAsync/UpdateAsync |
| **BR-004** | 密码必须Hash存储（BCrypt） | Service层 | UserService.CreateAsync |
| **BR-005** | 软删除支持 | Service层 | DeleteAsync（设置IsDeleted=true） |
| **BR-006** | 重置密码支持自动生成（Issue #1162） | Service层 | ResetPasswordAsync |
| **BR-007** | 更改密码需验证旧密码 | Service层 | ChangePasswordAsync |

---

## 🔗 跨模块依赖

### Users → Auth

**依赖原因**: 用户认证、JWT生成

```csharp
// 登录时需要验证用户名密码
var user = await _userRepository.GetByUsernameAsync(username);
if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
{
    // 生成JWT Token
    var token = _jwtService.GenerateToken(user);
}
```

---

## 📖 相关文档

- **Server端架构**: [docs/explanation/architecture/server/README.md](../README.md)
- **三层架构规范**: [docs/explanation/architecture/server/three-layer-architecture.md](../three-layer-architecture.md)
- **批量操作模式**: [docs/how-to/patterns/batch-operations.md](../../../../how-to/patterns/batch-operations.md)

---

## 🏷️ 变更历史

| 版本 | 日期 | 描述 | Issue |
|------|------|------|-------|
| v1.0 | 2025-11-10 | 初始版本，文档化Phase 1实现 | #2007 |

---

**最后更新**: 2025-11-10
**维护者**: @shouqitao
