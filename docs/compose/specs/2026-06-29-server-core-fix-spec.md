# Server/Core 修复规格

> 日期: 2026-06-29
> 状态: 草稿
> 优先级: CRITICAL/HIGH
> 来源: server-core-architecture-optimization.md [O1] C1/H1-H4

## [S1] 问题

Server/Core 存在以下问题：

| # | 问题 | 优先级 | 影响 |
|---|------|--------|------|
| C1 | HardDeleteAsync 使用 FindAsync | CRITICAL | 无法硬删除已软删记录 |
| H1 | BaseService 包含业务权限逻辑 | HIGH | 基础设施层不应有业务逻辑 |
| H2 | BaseApiController 包含 HTTP 层逻辑 | HIGH | 基础设施层不应有表现层逻辑 |
| H3 | ApplicationUser 未继承 BaseEntity | HIGH | 手动重复审计字段，维护风险 |
| H4 | IRepository<T> 约束不匹配 | HIGH | 接口约束 class，实现约束 BaseEntity |

## [S2] 目标

1. 修复 HardDeleteAsync bug
2. 分离 BaseService 业务逻辑
3. 移动 BaseApiController 到 WebAPI
4. 统一 ApplicationUser 审计字段
5. 统一 Repository 约束

## [S3] HardDeleteAsync 修复（C1）

### 当前代码（有问题）

```csharp
// BaseRepository.cs
public async Task HardDeleteAsync(TEntity entity)
{
    var existing = await _dbSet.FindAsync(entity.Id); // 受全局过滤器影响
    if (existing != null)
    {
        _dbSet.Remove(existing);
        await SaveChangesAsync();
    }
}
```

### 修复方案

```csharp
// BaseRepository.cs
public async Task HardDeleteAsync(Guid id)
{
    // 使用 IgnoreQueryFilters 绕过全局过滤器
    var entity = await _dbSet
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => e.Id == id);
    
    if (entity != null)
    {
        _dbSet.Remove(entity);
        await SaveChangesAsync();
    }
}

// 或者如果需要传入实体
public async Task HardDeleteAsync(TEntity entity)
{
    // 先从数据库重新查询（绕过过滤器）
    var existing = await _dbSet
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => e.Id == entity.Id);
    
    if (existing != null)
    {
        _dbSet.Remove(existing);
        await SaveChangesAsync();
    }
}
```

## [S4] BaseService 业务逻辑分离（H1）

### 当前代码

```csharp
// BaseService.cs
public abstract class BaseService
{
    protected async Task<Result<T>> ExecuteAsync<TResult>(...)
    {
        // 包含业务权限逻辑（同一天编辑规则、所有权检查）
    }
}
```

### 优化方案

```csharp
// BaseService.cs - 仅保留基础设施功能
public abstract class BaseService
{
    protected readonly ILogger _logger;
    protected readonly ICacheInvalidationService _cacheInvalidation;
    
    protected BaseService(ILogger logger, ICacheInvalidationService cacheInvalidation)
    {
        _logger = logger;
        _cacheInvalidation = cacheInvalidation;
    }
    
    protected async Task<Result<T>> ExecuteAsync<TResult>(
        Func<Task<T>> operation,
        ErrorCode errorCode,
        [CallerMemberName] string methodName = "")
    {
        try
        {
            _logger.LogDebug("Executing {MethodName}", methodName);
            var result = await operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", methodName);
            return Result<T>.Fail(errorCode, ex.Message);
        }
    }
}

// BusinessService.cs - 业务逻辑
public abstract class BusinessService : BaseService
{
    protected async Task<Result<T>> ExecuteWithPermissionAsync<TResult>(
        Func<Task<T>> operation,
        ErrorCode errorCode,
        [CallerMemberName] string methodName = "")
    {
        // 权限验证逻辑
        // 同一天编辑规则
        // 所有权检查
        return await ExecuteAsync(operation, errorCode, methodName);
    }
}
```

## [S5] BaseApiController 移动（H2）

### 当前位置

```
LYBT.Infrastructure/Web/BaseApiController.cs
```

### 移动到

```
LYBT.WebAPI/Controllers/BaseApiController.cs
```

### 修改

```csharp
// LYBT.WebAPI/Controllers/BaseApiController.cs
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    // API 响应映射逻辑
    protected IActionResult HandleResult<T>(Result<T> result, int successStatusCode = 200)
    {
        if (result.IsSuccess)
            return StatusCode(successStatusCode, ApiResponse<T>.Success(result.Data));
        
        return StatusCode(result.Error.StatusCode, ApiResponse<T>.Fail(result.Error));
    }
}
```

## [S6] ApplicationUser 审计字段统一（H3）

### 方案 A: 使用接口（推荐）

```csharp
// LYBT.Entities/Common/IApplicationUserAudit.cs
public interface IApplicationUserAudit
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
    bool IsDeleted { get; set; }
    byte[]? RowVersion { get; set; }
}

// LYBT.Entities/Users/ApplicationUser.cs
public class ApplicationUser : IdentityUser<Guid>, IApplicationUserAudit, IAuditableEntity, ISoftDeletable
{
    // 显式实现接口属性
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }
    
    // 业务字段
    public string RealName { get; set; }
    public UserRole Role { get; set; }
    public bool IsSysAdmin { get; set; }
    // ...
}
```

### 方案 B: 创建中间基类

```csharp
// LYBT.Entities/Common/AuditableIdentityUser.cs
public abstract class AuditableIdentityUser<TUser> : IdentityUser<TUser>, IAuditableEntity, ISoftDeletable
    where TUser : IEquatable<TUser>
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }
}

// LYBT.Entities/Users/ApplicationUser.cs
public class ApplicationUser : AuditableIdentityUser<Guid>
{
    // 仅业务字段
    public string RealName { get; set; }
    public UserRole Role { get; set; }
    public bool IsSysAdmin { get; set; }
    // ...
}
```

## [S7] Repository 约束统一（H4）

### 当前问题

```csharp
// 接口约束 class
public interface IRepository<T> where T : class { }

// 实现约束 BaseEntity
public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity { }
```

### 优化方案

```csharp
// 方案 A: 统一约束 BaseEntity
public interface IRepository<T> where T : BaseEntity { }
public class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity { }

// 方案 B: 为非 BaseEntity 实体提供单独的 Repository
public interface IPrescriptionItemRepository
{
    Task<PrescriptionItem?> GetByIdAsync(Guid id);
    Task<List<PrescriptionItem>> GetByPrescriptionIdAsync(Guid prescriptionId);
    // ...
}

public class PrescriptionItemRepository : IPrescriptionItemRepository
{
    // 独立实现，不使用 BaseRepository
}
```

## [S8] 实施步骤

1. 修复 HardDeleteAsync（C1）
2. 分离 BaseService 业务逻辑（H1）
3. 移动 BaseApiController 到 WebAPI（H2）
4. 统一 ApplicationUser 审计字段（H3）
5. 统一 Repository 约束（H4）
6. 运行 `dotnet build` 和 `dotnet test` 验证

## [S9] 验收标准

1. HardDeleteAsync 可以硬删除已软删记录
2. BaseService 仅包含基础设施功能
3. BaseApiController 在 WebAPI 项目中
4. ApplicationUser 审计字段与 BaseEntity 一致
5. Repository 约束统一
6. 所有现有测试通过
7. `dotnet build` 成功
