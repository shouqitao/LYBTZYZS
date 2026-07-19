# BaseApiController Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor BaseApiController (438 lines, 6 responsibilities) into focused helper classes + extension methods, reducing the base class to ~80 lines of pure delegation.

**Architecture:** Extract operator extraction to `OperatorAccessor`, response helpers to `ControllerBase` extensions, and validation to action filters. BaseApiController becomes a thin facade that delegates to these focused components. 18 existing controllers continue to work without modification.

**Tech Stack:** C# / .NET 8 / ASP.NET Core 8 / CommunityToolkit.Mvvm

## Global Constraints

- All 18 existing controllers (Server + LocalWebAPI) MUST continue to compile without changes
- BaseApiController protected API surface MUST remain identical (same method names, signatures)
- No new NuGet packages — use only built-in ASP.NET Core abstractions
- Each task produces a working, buildable solution
- Chinese business comments, English identifiers

---

## File Structure

| Action | Path | Responsibility |
|--------|------|----------------|
| **Create** | `src/Server/Core/LYBT.Infrastructure/Web/OperatorAccessor.cs` | Static operator extraction from ClaimsPrincipal |
| **Create** | `src/Server/Core/LYBT.Infrastructure/Web/ControllerBaseExtensions.cs` | Extension methods for ApiResponse wrapping |
| **Create** | `src/Server/Core/LYBT.Infrastructure/Web/Filters/ModelValidationFilter.cs` | ModelState validation filter |
| **Create** | `src/Server/Core/LYBT.Infrastructure/Web/Filters/PaginationValidationFilter.cs` | Pagination parameter validation filter |
| **Create** | `src/Server/Core/LYBT.Infrastructure/Web/Filters/OwnershipValidationFilter.cs` | Resource ownership check filter |
| **Modify** | `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs` | Slim down to ~80 lines, delegate to helpers |
| **Modify** | `src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs` | Register global filters |

---

### Task 1: Extract OperatorAccessor

**Covers:** Claims extraction, role parsing, logging context

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Web/OperatorAccessor.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs:36-85`

**Interfaces:**
- Consumes: `ClaimsPrincipal`, `ILogger`, `RoleConstants`, `UserRole`
- Produces: `OperatorAccessor.GetOperator(ClaimsPrincipal, ILogger)`, `OperatorAccessor.ParseUserRole(string?, ILogger)`

- [ ] **Step 1: Create OperatorAccessor.cs**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LYBT.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// 从 ClaimsPrincipal 提取操作者信息 — 支持多种 JWT Claims 标准
/// </summary>
public static class OperatorAccessor
{
    public record OperatorInfo(Guid Id, string Name, UserRole Role);

    public static OperatorInfo GetOperator(ClaimsPrincipal? user, ILogger logger)
    {
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? user?.FindFirst("sub")?.Value;

        var userName = user?.Identity?.Name
                      ?? user?.FindFirst(ClaimTypes.Name)?.Value
                      ?? user?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                      ?? user?.FindFirst("unique_name")?.Value
                      ?? user?.FindFirst("name")?.Value;

        var roleStr = user?.FindFirst(ClaimTypes.Role)?.Value
                     ?? user?.FindFirst("role")?.Value
                     ?? user?.FindFirst("roles")?.Value
                     ?? user?.FindFirst(RoleConstants.Admin)?.Value;

        if (Guid.TryParse(userId, out var opId) && opId != Guid.Empty && !string.IsNullOrEmpty(userName))
        {
            var role = ParseUserRole(roleStr, logger);
            return new OperatorInfo(opId, userName, role);
        }

        logger.LogWarning("GetOperator失败: userId={UserId}, userName={UserName}, opId={OpId}, opIdIsEmpty={OpIdIsEmpty}",
            userId, userName, opId, opId == Guid.Empty);

        throw new UnauthorizedAccessException("未登录或用户信息无效");
    }

    public static UserRole ParseUserRole(string? roleStr, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(roleStr))
        {
            logger.LogWarning("角色值为空，默认使用Doctor");
            return UserRole.Doctor;
        }

        if (roleStr.Equals("SysAdmin", StringComparison.OrdinalIgnoreCase))
            roleStr = RoleConstants.SuperAdmin;

        if (Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            return role;

        logger.LogWarning("无效的角色值: {RoleString}，默认使用Doctor", roleStr);
        return UserRole.Doctor;
    }
}
```

- [ ] **Step 2: Update BaseApiController to delegate to OperatorAccessor**

Replace `GetOperator()` and `ParseUserRole()` in BaseApiController with:

```csharp
protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
{
    var info = OperatorAccessor.GetOperator(User, _logger);
    return (info.Id, info.Name, info.Role);
}
```

Remove the `ParseUserRole` private method.

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Web/OperatorAccessor.cs src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs
git commit -m "refactor(Server): extract OperatorAccessor from BaseApiController"
```

---

### Task 2: Create ControllerBase Extension Methods

**Covers:** ApiResponse wrapping, response helpers, Result mapping

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Web/ControllerBaseExtensions.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs:129-297`

**Interfaces:**
- Consumes: `ApiResponse<T>`, `PagedResult<T>`, `Result<T>`, `GenericErrorCode`
- Produces: Extension methods on `ControllerBase`: `Success()`, `Success<T>()`, `SuccessPaged<T>()`, `Error()`, `NotFoundResponse()`, `BusinessFail()`, `ValidationFail()`, `ForbidResponse()`, `HandleResult<T>()`, `HandleResult()`

- [ ] **Step 1: Create ControllerBaseExtensions.cs**

```csharp
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.AspNetCore.Mvc;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// ControllerBase 扩展方法 — ApiResponse 包装和 Result 映射
/// </summary>
public static class ControllerBaseExtensions
{
    private static string GetRequestId(ControllerBase controller)
        => controller.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

    // ==================== 成功响应 ====================

    public static IActionResult Success(this ControllerBase controller, string message = "操作成功")
    {
        var response = ApiResponse.CreateSuccess(message: message);
        response.RequestId = GetRequestId(controller);
        return controller.Ok(response);
    }

    public static IActionResult Success<T>(this ControllerBase controller, T data, string message = "操作成功")
    {
        var response = ApiResponse<T>.CreateSuccess(data, message);
        response.RequestId = GetRequestId(controller);
        return controller.Ok(response);
    }

    public static IActionResult SuccessPaged<T>(this ControllerBase controller, PagedResult<T> pagedResult, string message = "查询成功")
    {
        var items = pagedResult.Items is List<T> list ? list : pagedResult.Items.ToList();
        var pageResult = new PagedResult<T>(items, pagedResult.TotalCount, pagedResult.CurrentPage, pagedResult.PageSize);
        var response = ApiResponse<PagedResult<T>>.CreateSuccess(pageResult, message);
        response.RequestId = GetRequestId(controller);
        return controller.Ok(response);
    }

    // ==================== 错误响应 ====================

    public static IActionResult Error(this ControllerBase controller, string message)
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        return controller.BadRequest(response);
    }

    public static IActionResult NotFoundResponse(this ControllerBase controller, string message = "资源未找到")
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        return controller.NotFound(response);
    }

    public static IActionResult BusinessFail(this ControllerBase controller, string message, string? errorCode = null)
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        if (errorCode != null)
            response.Errors = new { code = errorCode };
        return controller.StatusCode(422, response);
    }

    public static IActionResult ValidationFail(this ControllerBase controller, string message = "参数验证失败")
    {
        var errors = controller.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
        var response = ApiResponse.CreateFail(message, errors.Count > 0 ? errors : null);
        response.RequestId = GetRequestId(controller);
        return controller.BadRequest(response);
    }

    public static IActionResult ForbidResponse(this ControllerBase controller, string message)
    {
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        return controller.StatusCode(403, response);
    }

    // ==================== Result 映射 ====================

    public static IActionResult HandleResult<T>(this ControllerBase controller, Result<T> result, string successMessage = "操作成功", bool useAuthMapping = false)
    {
        if (result.IsSuccess)
            return controller.Success(result.Data!, successMessage);

        var message = result.ErrorMessage ?? "操作失败";

        if (result.ModuleErrorCode.HasValue)
        {
            var moduleCode = result.ModuleErrorCode.Value;
            var httpStatus = moduleCode.ToHttpStatusCode();

            if (useAuthMapping)
            {
                var errorResponse = CreateModuleErrorResponse<T>(controller, message, moduleCode);
                return httpStatus switch
                {
                    401 => controller.Unauthorized(errorResponse),
                    403 => controller.StatusCode(403, errorResponse),
                    404 => controller.NotFound(errorResponse),
                    422 => controller.StatusCode(422, errorResponse),
                    503 => controller.StatusCode(503, errorResponse),
                    500 => controller.StatusCode(500, errorResponse),
                    _ => controller.StatusCode(httpStatus, errorResponse)
                };
            }

            var failResponse = ApiResponse.CreateFail(message);
            failResponse.RequestId = GetRequestId(controller);
            failResponse.Errors = new { code = moduleCode.ToFormattedString(), numericCode = (int)moduleCode };
            return controller.StatusCode(httpStatus, failResponse);
        }

        return controller.BusinessFail(message);
    }

    public static IActionResult HandleResult(this ControllerBase controller, Result result, string successMessage = "操作成功")
    {
        if (result.IsSuccess)
            return controller.Success(successMessage);

        var message = result.ErrorMessage ?? "操作失败";

        if (result.ModuleErrorCode.HasValue)
        {
            var moduleCode = result.ModuleErrorCode.Value;
            var httpStatus = moduleCode.ToHttpStatusCode();
            var response = ApiResponse.CreateFail(message);
            response.RequestId = GetRequestId(controller);
            response.Errors = new { code = moduleCode.ToFormattedString(), numericCode = (int)moduleCode };
            return controller.StatusCode(httpStatus, response);
        }

        return controller.BusinessFail(message);
    }

    private static ApiResponse<T> CreateModuleErrorResponse<T>(ControllerBase controller, string message, GenericErrorCode errorCode)
    {
        var response = ApiResponse<T>.CreateFail(message);
        response.RequestId = GetRequestId(controller);
        response.Errors = new { code = errorCode.ToFormattedString(), numericCode = (int)errorCode };
        return response;
    }
}
```

- [ ] **Step 2: Update BaseApiController to delegate**

Replace the `#region API响应方法` and `#region Result处理方法` sections with thin delegations:

```csharp
#region API响应方法 - 委托给 ControllerBaseExtensions

protected IActionResult Success(string message = "操作成功")
    => this.Success(message);

protected IActionResult Success<T>(T data, string message = "操作成功")
    => this.Success(data, message);

protected IActionResult SuccessPaged<T>(PagedResult<T> pagedResult, string message = "查询成功")
    => this.SuccessPaged(pagedResult, message);

protected IActionResult Error(string message)
{
    _logger?.LogWarning("API错误: {Message}", message);
    return this.Error(message);
}

protected IActionResult NotFound(string message = "资源未找到")
    => this.NotFoundResponse(message);

protected IActionResult BusinessFail(string message, string? errorCode = null)
    => this.BusinessFail(message, errorCode);

protected IActionResult ValidationFail(string message = "参数验证失败")
    => this.ValidationFail(message);

protected IActionResult Forbid(string message)
    => this.ForbidResponse(message);

#endregion

#region Result处理方法 - 委托给 ControllerBaseExtensions

protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "操作成功", bool useAuthMapping = false)
    => this.HandleResult(result, successMessage, useAuthMapping);

protected IActionResult HandleResult(Result result, string successMessage = "操作成功")
    => this.HandleResult(result, successMessage);

#endregion
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
Expected: 0 errors

- [ ] **Step 4: Full solution build**

Run: `dotnet build LYBTZYZS.sln --verbosity quiet`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Web/ControllerBaseExtensions.cs src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs
git commit -m "refactor(Server): extract response helpers to ControllerBase extensions"
```

---

### Task 3: Create Validation Action Filters

**Covers:** Model validation, pagination validation, ownership validation

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Web/Filters/ModelValidationFilter.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Web/Filters/PaginationValidationFilter.cs`
- Create: `src/Server/Core/LYBT.Infrastructure/Web/Filters/OwnershipValidationFilter.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `ModelStateDictionary`, `ClaimsPrincipal`, `ILogger`
- Produces: Three `IAsyncActionFilter` implementations + global registration

- [ ] **Step 1: Create ModelValidationFilter.cs**

```csharp
using LYBT.Infrastructure.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.Infrastructure.Web.Filters;

/// <summary>
/// 全局模型验证过滤器 — 自动返回 ApiResponse 格式的 400 错误
/// 替代控制器中的手动 ModelState.IsValid 检查
/// </summary>
public class ModelValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var controller = context.Controller as ControllerBase;
            if (controller != null)
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                var message = errors.Count > 0 ? $"参数验证失败: {string.Join("; ", errors)}" : "参数验证失败";
                context.Result = controller.ValidationFail(message);
                return;
            }
        }

        await next();
    }
}
```

- [ ] **Step 2: Create PaginationValidationFilter.cs**

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.Infrastructure.Web.Filters;

/// <summary>
/// 分页参数验证过滤器 — 通过 ActionNameAttribute 匹配特定 Action
/// 使用方式: [ServiceFilter(typeof(PaginationValidationFilter))]
/// </summary>
public class PaginationValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.TryGetValue("page", out var pageObj) &&
            context.ActionArguments.TryGetValue("pageSize", out var pageSizeObj) &&
            pageObj is int page && pageSizeObj is int pageSize)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                var controller = context.Controller as ControllerBase;
                if (controller != null)
                {
                    context.Result = controller.ValidationFail("分页参数无效：page 和 pageSize 必须大于 0，pageSize 不能超过 100");
                    return;
                }
            }
        }

        await next();
    }
}
```

- [ ] **Step 3: Create OwnershipValidationFilter.cs**

```csharp
using System.Security.Claims;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LYBT.Infrastructure.Web.Filters;

/// <summary>
/// 所有权验证过滤器 — 通过 Resources 属性传递资源名称
/// 使用方式: [ServiceFilter(typeof(OwnershipValidationFilter))]
/// </summary>
public class OwnershipValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var createdByArg = context.ActionArguments.Values
            .OfType<Guid?>()
            .FirstOrDefault();

        if (createdByArg.HasValue)
        {
            var user = context.HttpContext.User;
            var roleStr = user?.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var isAdmin = roleStr is "SuperAdmin" or "Admin";

            if (!isAdmin)
            {
                var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var operatorId) || createdByArg.Value != operatorId)
                {
                    var controller = context.Controller as ControllerBase;
                    if (controller != null)
                    {
                        context.Result = controller.StatusCode(403, new { success = false, message = "您没有权限操作此资源" });
                        return;
                    }
                }
            }
        }

        await next();
    }
}
```

- [ ] **Step 4: Register global filters in ApiServiceCollectionExtensions.cs**

Find the `services.AddControllers()` call and add filters after it:

```csharp
services.AddControllers(options =>
{
    // 全局模型验证过滤器
    options.Filters.Add<Filters.ModelValidationFilter>();
});
```

Register filter services:

```csharp
services.AddScoped<Filters.PaginationValidationFilter>();
services.AddScoped<Filters.OwnershipValidationFilter>();
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build LYBTZYZS.sln --verbosity quiet`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Web/Filters/ src/Server/Services/LYBT.WebAPI/Extensions/ApiServiceCollectionExtensions.cs
git commit -m "refactor(Server): add validation action filters (Model, Pagination, Ownership)"
```

---

### Task 4: Clean Up BaseApiController

**Covers:** Final slim-down, remove redundant code

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`

**Interfaces:**
- Consumes: `OperatorAccessor`, `ControllerBaseExtensions`
- Produces: Slimmed-down BaseApiController (~120 lines)

- [ ] **Step 1: Rewrite BaseApiController.cs**

The final file should contain:

```csharp
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Web;

/// <summary>
/// API 控制器基类 — 薄代理层
/// 实际逻辑委托给 OperatorAccessor 和 ControllerBaseExtensions
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger _logger;

    protected BaseApiController(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>获取当前操作者信息</summary>
    protected (Guid OperatorId, string OperatorName, UserRole OperatorRole) GetOperator()
    {
        var info = OperatorAccessor.GetOperator(User, _logger);
        return (info.Id, info.Name, info.Role);
    }

    /// <summary>统一日志记录（带脱敏）</summary>
    protected void LogOperation(string operation, object? data = null, Guid? targetId = null)
    {
        try
        {
            var (operatorId, operatorName, _) = GetOperator();
            var logData = data != null ? Shared.Logging.Masking.SensitiveDataMasker.SerializeWithSanitization(data) : null;
            _logger.LogInformation(
                "{Operation}，操作者: {OperatorName}({OperatorId}), 目标ID: {TargetId}, 数据: {Data}",
                operation, operatorName, operatorId, targetId, logData);
        }
        catch { }
    }

    // ==================== 响应方法 — 委托给 ControllerBaseExtensions ====================

    protected IActionResult Success(string message = "操作成功")
        => this.Success(message);

    protected IActionResult Success<T>(T data, string message = "操作成功")
        => this.Success(data, message);

    protected IActionResult SuccessPaged<T>(PagedResult<T> pagedResult, string message = "查询成功")
        => this.SuccessPaged(pagedResult, message);

    protected IActionResult Error(string message)
    {
        _logger?.LogWarning("API错误: {Message}", message);
        return this.Error(message);
    }

    protected IActionResult NotFound(string message = "资源未找到")
        => this.NotFoundResponse(message);

    protected IActionResult BusinessFail(string message, string? errorCode = null)
        => this.BusinessFail(message, errorCode);

    protected IActionResult ValidationFail(string message = "参数验证失败")
        => this.ValidationFail(message);

    protected IActionResult Forbid(string message)
        => this.ForbidResponse(message);

    // ==================== Result 映射 — 委托给 ControllerBaseExtensions ====================

    protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "操作成功", bool useAuthMapping = false)
        => this.HandleResult(result, successMessage, useAuthMapping);

    protected IActionResult HandleResult(Result result, string successMessage = "操作成功")
        => this.HandleResult(result, successMessage);

    // ==================== 验证辅助方法 ====================

    protected IActionResult? ValidateGuid(Guid id, string paramName = "ID")
    {
        if (id == Guid.Empty)
            return ValidationFail($"{paramName}不能为空");
        return null;
    }

    protected bool IsAdminOrOwner(Guid? createdBy)
    {
        try
        {
            var (operatorId, _, operatorRole) = GetOperator();
            if (operatorRole is UserRole.Admin or UserRole.SuperAdmin) return true;
            return createdBy.HasValue && createdBy.Value == operatorId;
        }
        catch (UnauthorizedAccessException) { return false; }
    }

    protected IActionResult? ValidateOwnership(Guid? createdBy, string resourceName = "资源")
    {
        if (!IsAdminOrOwner(createdBy))
        {
            _logger?.LogWarning("所有权检查失败: 用户无权操作此{ResourceName}", resourceName);
            return Forbid($"您没有权限操作此{resourceName}，只能操作自己创建的数据");
        }
        return null;
    }

    protected async Task<(TDto? dto, IActionResult? error)> GetEntityWithOwnershipCheckAsync<TDto>(
        Func<Task<Result<TDto>>> getEntityFunc,
        string resourceName = "资源") where TDto : class, ICreatorTrackable
    {
        var result = await getEntityFunc();
        if (!result.IsSuccess || result.Data == null)
            return (null, NotFound($"{resourceName}不存在"));
        if (ValidateOwnership(result.Data.CreatedBy, resourceName) is { } ownerError)
            return (null, ownerError);
        return (result.Data, null);
    }

    protected async Task<(TDto? dto, IActionResult? error)> GetEntityWithOwnershipCheckAsync<TDto>(
        Guid id,
        Func<Guid, Task<Result<TDto>>> getByIdFunc,
        string resourceName = "资源") where TDto : class, ICreatorTrackable
    {
        if (ValidateGuid(id, $"{resourceName}ID") is { } guidError)
            return (null, guidError);
        return await GetEntityWithOwnershipCheckAsync(() => getByIdFunc(id), resourceName);
    }

    protected IActionResult? ValidateModel()
    {
        if (!ModelState.IsValid)
            return ValidationFail($"参数验证失败: {string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
        return null;
    }

    protected IActionResult? ValidatePagination(int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0 || pageSize > 100)
            return ValidationFail("分页参数无效：page 和 pageSize 必须大于 0，pageSize 不能超过 100");
        return null;
    }
}
```

- [ ] **Step 2: Full solution build**

Run: `dotnet build LYBTZYZS.sln --verbosity quiet`
Expected: 0 errors

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/LYBT.Tests.Server/ --verbosity quiet`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs
git commit -m "refactor(Server): slim BaseApiController to ~120 lines of delegation"
```

---

### Task 5: Update LocalWebAPI BaseApiController

**Covers:** Desktop LocalWebAPI also has a BaseApiController — ensure it stays compatible

**Files:**
- Verify: `src/Client/Desktop/LocalWebAPI/Controllers/BaseApiController.cs` (if exists)

- [ ] **Step 1: Check if LocalWebAPI has its own BaseApiController**

Search for LocalWebAPI BaseApiController. If it exists and inherits from the Infrastructure BaseApiController, no changes needed. If it's a standalone copy, apply same pattern.

- [ ] **Step 2: Build full solution**

Run: `dotnet build LYBTZYZS.sln --verbosity quiet`
Expected: 0 errors

- [ ] **Step 3: Commit (if changes needed)**

```bash
git add -A && git commit -m "refactor: sync LocalWebAPI BaseApiController with Infrastructure refactor"
```

---

## Summary

After all tasks:
- **BaseApiController**: 438 lines → ~120 lines (73% reduction)
- **New files**: OperatorAccessor (45 lines), ControllerBaseExtensions (120 lines), 3 Filters (~80 lines)
- **Total new code**: ~245 lines across 5 focused files
- **Net reduction**: ~193 lines removed from monolithic base class
- **18 controllers**: Zero modifications required
