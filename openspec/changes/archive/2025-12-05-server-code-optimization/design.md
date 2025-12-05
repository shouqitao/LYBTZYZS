# Design: Server端代码优化重构

**提案ID**: server-code-optimization
**创建日期**: 2025-12-05

## 1. 当前架构分析

### 1.1 控制器继承结构

```
Microsoft.AspNetCore.Mvc.ControllerBase
    │
    └── BaseControllerCore (175行)
            │  - GetOperator(): 用户信息提取
            │  - LogOperation(): 统一日志
            │  - HandleExceptionCore(): 异常处理
            │  - GetModelErrors(): 模型验证
            │
            └── BaseApiController (340行)
                    │  - Error() / Error<T>()
                    │  - NotFound() / NotFound<T>()
                    │  - ValidationFail() / ValidationFail<T>()
                    │  - HandleException() / HandleException<T>()
                    │  - Success() / Success<T>()
                    │  - SuccessResult() / SuccessResult<T>()
                    │
                    └── 具体Controllers (MedicalCaseController等)
```

**问题**:
1. 两层基类功能重叠
2. 大量泛型/非泛型重复方法
3. 继承深度增加理解成本

### 1.2 Service继承结构

```
BaseService (非泛型, ~115行)
    │  - ValidateEditPermission()
    │  - ValidateDeletePermission()
    │  - ExtractUserInfoAsync()
    │  - IsToday()
    │  - GetRoleDisplayName()
    │
    └── BaseService<T> (泛型, ~270行)
            │  - ExecuteAsync<TResult>()
            │  - ValidateAsync<TDto>()
            │  - GetEntityId<TEntity>() → throws NotImplementedException
            │  - GetCreatedUserId<TEntity>() → throws NotImplementedException
            │  - GetCreatedDate<TEntity>() → throws NotImplementedException
            │
            └── 具体Services (UserService, PatientService等)
```

**问题**:
1. `NotImplementedException`设计违反里氏替换原则
2. 泛型约束不明确
3. 权限验证与业务逻辑耦合

### 1.3 Repository继承结构

```
IRepository<T>
    │
    └── IReadRepository<T>
            │
            └── BaseReadRepository<T> (121行)
                    │
                    └── BaseRepository<T> (617行)
                            │  - CRUD操作
                            │  - 分页查询（多个重载）
                            │  - 模板方法: ApplyKeywordFilter, ApplyDefaultOrdering
                            │
                            └── 具体Repositories
```

**问题**:
1. 过多重载方法
2. 部分模板方法未被使用
3. 代码量过大

---

## 2. 优化设计

### 2.1 控制器基类合并设计

#### 目标结构
```
Microsoft.AspNetCore.Mvc.ControllerBase
    │
    └── LybtControllerBase (合并后, 目标~300行)
            │  - GetOperator()
            │  - LogOperation()
            │  - Result<T> helpers (统一泛型)
            │
            └── 具体Controllers
```

#### 实现方案

```csharp
/// <summary>
/// LYBT统一控制器基类
/// 合并BaseControllerCore和BaseApiController功能
/// </summary>
[ApiController]
public abstract class LybtControllerBase : ControllerBase
{
    protected readonly ILogger Logger;

    protected LybtControllerBase(ILogger logger)
    {
        Logger = logger;
    }

    #region 用户信息提取

    /// <summary>
    /// 获取当前操作者信息
    /// </summary>
    protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
    {
        // 实现保持不变
    }

    #endregion

    #region 统一响应方法 (消除重复)

    /// <summary>
    /// 成功响应 - 统一泛型实现
    /// </summary>
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")
        => Ok(ApiResponse<T>.Success(data, message));

    /// <summary>
    /// 成功响应 - 无数据
    /// </summary>
    protected ActionResult<ApiResponse> Success(string message = "操作成功")
        => Success<object?>(null, message);

    /// <summary>
    /// 错误响应 - 统一实现
    /// </summary>
    protected ActionResult<ApiResponse<T>> Error<T>(string message, int statusCode = 400)
        => StatusCode(statusCode, ApiResponse<T>.Failure(message));

    /// <summary>
    /// 错误响应 - 无数据类型
    /// </summary>
    protected ActionResult<ApiResponse> Error(string message, int statusCode = 400)
        => Error<object?>(message, statusCode);

    /// <summary>
    /// NotFound响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "资源不存在")
        => Error<T>(message, 404);

    /// <summary>
    /// 验证失败响应
    /// </summary>
    protected ActionResult<ApiResponse<T>> ValidationFail<T>(IEnumerable<string> errors)
        => Error<T>(string.Join("; ", errors), 400);

    protected ActionResult<ApiResponse<T>> ValidationFail<T>(params string[] errors)
        => ValidationFail<T>(errors.AsEnumerable());

    #endregion

    #region 异常处理

    /// <summary>
    /// 统一异常处理
    /// </summary>
    protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation)
    {
        Logger.LogError(ex, "{Operation}失败", operation);
        return Error<T>($"{operation}失败: {ex.Message}", 500);
    }

    #endregion
}
```

#### 迁移策略
1. 创建新的`LybtControllerBase`
2. 逐个模块迁移Controller
3. 删除旧的`BaseControllerCore`和`BaseApiController`

### 2.2 Service基类重构设计

#### 定义实体接口

```csharp
/// <summary>
/// 可标识实体接口
/// </summary>
public interface IIdentifiable<TKey>
{
    TKey Id { get; }
}

/// <summary>
/// 可审计实体接口 - 包含创建信息
/// </summary>
public interface IAuditableEntity
{
    Guid CreatedUserId { get; }
    DateTime CreatedDate { get; }
}

/// <summary>
/// 完整的可审计可标识实体
/// </summary>
public interface IAuditableIdentifiable : IIdentifiable<Guid>, IAuditableEntity
{
}
```

#### 重构后的BaseService

```csharp
/// <summary>
/// BaseService - 仅提供通用功能
/// </summary>
public abstract class BaseService
{
    protected readonly ILogger Logger;

    protected BaseService(ILogger logger)
    {
        Logger = logger;
    }

    #region 权限验证（使用接口约束）

    /// <summary>
    /// 验证编辑权限 - 使用接口约束
    /// </summary>
    protected (bool IsAuthorized, string ErrorMessage) ValidateEditPermission<TEntity>(
        TEntity entity,
        Guid currentUserId,
        bool isAdmin = false,
        string entityType = "实体")
        where TEntity : IAuditableIdentifiable
    {
        if (isAdmin) return (true, string.Empty);

        if (entity.CreatedUserId != currentUserId)
            return (false, $"只能编辑自己创建的{entityType}");

        if (entity.CreatedDate.Date != DateTime.Today)
            return (false, $"只能编辑当天创建的{entityType}");

        return (true, string.Empty);
    }

    #endregion

    #region 统一执行方法

    protected async Task<Result<TResult>> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        string operationName)
    {
        try
        {
            var result = await operation();
            return Result<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Operation}失败", operationName);
            return Result<TResult>.Failure($"{operationName}失败");
        }
    }

    #endregion
}
```

**优势**:
- 使用接口约束替代`NotImplementedException`
- 编译时类型检查
- 符合里氏替换原则

### 2.3 Repository优化设计

#### 简化重载方法

```csharp
// 之前: 多个重载
Task<PagedResult<T>> GetPagedAsync(int page, int pageSize);
Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, string? keyword);
Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, string? keyword, Expression<Func<T, bool>>? filter);
Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, string? keyword, Expression<Func<T, bool>>? filter, Expression<Func<T, object>>? orderBy);

// 之后: 单一方法 + 可选参数
Task<PagedResult<T>> GetPagedAsync(
    int page,
    int pageSize,
    string? keyword = null,
    Expression<Func<T, bool>>? filter = null,
    Expression<Func<T, object>>? orderBy = null,
    bool ascending = true);
```

#### 提取查询扩展方法

```csharp
public static class QueryableExtensions
{
    /// <summary>
    /// 应用分页
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        int page,
        int pageSize)
    {
        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    public static IQueryable<T> ApplyOrdering<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool ascending = true)
    {
        return ascending
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);
    }
}
```

---

## 3. 数据库影响

**无数据库Schema变更**

本次重构仅涉及代码层面优化，不涉及：
- 数据库表结构
- 存储过程
- 索引
- 数据迁移

---

## 4. API兼容性

**保持100%向后兼容**

- API端点不变
- 请求/响应格式不变
- 错误码不变

---

## 5. 测试策略

### 5.1 单元测试
- 为新的基类编写单元测试
- 确保权限验证逻辑正确
- 测试边界条件

### 5.2 集成测试
- 运行现有集成测试套件
- 验证API响应格式

### 5.3 回归测试
- 手动测试关键业务流程
- 验证前端功能正常

---

## 6. 代码量预估

| 组件 | 当前行数 | 优化后行数 | 减少 |
|------|---------|-----------|------|
| BaseControllerCore | 175 | 0 (合并) | -175 |
| BaseApiController | 340 | ~300 | -40 |
| BaseService | 385 | ~250 | -135 |
| BaseRepository | 617 | ~500 | -117 |
| **总计** | **1517** | **~1050** | **~467 (31%)** |

---

## 7. Repository命名规范化设计

### 7.1 当前命名不一致问题

| 模块 | 当前方法名 | 问题描述 |
|------|-----------|----------|
| IUserRepository | `IsUsernameExistsAsync` | 使用`Is...Exists`，与其他模块`ExistsAsync`不一致 |
| IPrescriptionRepository | `GetByIdWithItemsAsync` | 使用`WithItems`，与其他模块`WithDetails`不一致 |
| IPrescriptionRepository | `GetByMedicalCaseIdAsync` | 返回`List<T>`，但命名未体现 |
| IConsultationRepository | `GetByMedicalCaseIdAsync` | 返回单个`T`，命名正确 |

### 7.2 统一命名规范

#### 7.2.1 存在性检查方法

**规范**: 统一使用 `ExistsAsync` 前缀

```csharp
// 标准模式
Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

// 按条件检查 - 使用方法重载
Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default);
Task<bool> ExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default);

// 重命名
IUserRepository.IsUsernameExistsAsync → ExistsAsync(string username)
```

#### 7.2.2 详情查询方法后缀

**规范**: 统一使用 `WithDetailsAsync` 后缀

```csharp
// 标准模式 - 获取实体及其关联数据
Task<T?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

// 重命名
IPrescriptionRepository.GetByIdWithItemsAsync → GetByIdWithDetailsAsync
```

#### 7.2.3 返回类型命名约定

**规范**:
- 单个结果: `GetByXxxAsync` 返回 `T?`
- 多个结果: `GetListByXxxAsync` 返回 `List<T>`

```csharp
// 单个结果
Task<Consultation?> GetByMedicalCaseIdAsync(Guid medicalCaseId);  // 正确

// 多个结果
Task<List<Prescription>> GetListByMedicalCaseIdAsync(Guid medicalCaseId);  // 重命名

// 重命名
IPrescriptionRepository.GetByMedicalCaseIdAsync → GetListByMedicalCaseIdAsync
```

### 7.3 命名规范汇总表

| 场景 | 命名模式 | 返回类型 | 示例 |
|------|----------|----------|------|
| 按ID获取 | `GetByIdAsync` | `T?` | `GetByIdAsync(Guid id)` |
| 按ID获取含关联 | `GetByIdWithDetailsAsync` | `T?` | `GetByIdWithDetailsAsync(Guid id)` |
| 按条件获取单个 | `GetByXxxAsync` | `T?` | `GetByMedicalCaseIdAsync(Guid id)` |
| 按条件获取列表 | `GetListByXxxAsync` | `List<T>` | `GetListByMedicalCaseIdAsync(Guid id)` |
| 获取全部 | `GetAllAsync` | `List<T>` | `GetAllAsync()` |
| 分页查询 | `GetPagedAsync` | `PagedResult<T>` | `GetPagedAsync(int page, int size)` |
| 存在性检查 | `ExistsAsync` | `bool` | `ExistsAsync(Guid id)` |
| 添加 | `AddAsync` | `T` | `AddAsync(T entity)` |
| 更新 | `UpdateAsync` | `void` | `UpdateAsync(T entity)` |
| 删除 | `DeleteAsync` | `void` | `DeleteAsync(T entity)` |
| 软删除 | `SoftDeleteAsync` | `void` | `SoftDeleteAsync(Guid id)` |

### 7.4 迁移策略

1. **创建命名规范文档**: 在`docs/`目录创建`repository-naming-conventions.md`
2. **逐模块重命名**: 按模块逐个更新接口和实现
3. **更新调用点**: 使用IDE重构功能批量更新
4. **编译验证**: 确保编译通过
5. **测试验证**: 运行单元测试和集成测试

---

## 8. 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 引入运行时错误 | 充分的单元测试和集成测试 |
| 合并冲突 | 分阶段提交，及时合并主分支 |
| 性能影响 | 基类优化通常提升性能，无负面影响 |
| 命名重构遗漏调用点 | 使用IDE重构功能，编译验证 |
