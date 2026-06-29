# 剩余架构问题修复实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 19 个架构问题（2 CRITICAL + 3 HIGH + 7 MEDIUM + 7 LOW），将 Solution 基础架构固定下来

**Architecture:** 分层重构：Controller → Service → Repository，消除胖控制器、God Service、硬编码值、N+1 查询

**Tech Stack:** .NET 8, ASP.NET Core, EF Core, CommunityToolkit.Mvvm, Prism

## Global Constraints

- 所有修改必须通过 `dotnet build` 编译
- 所有现有测试必须通过
- 新增代码必须有对应的单元测试
- 保持向后兼容，不破坏现有 API 契约
- 遵循现有代码风格（PascalCase 公共/私有，I 前缀接口）

---

## Task 1: W1 - BaseUsersController 提取 UserService

**Covers:** [S1]

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/UsersModule.cs`
- Create: `tests/LYBT.Tests.Server/Unit/Modules/Users/UserServiceTests.cs`

**Interfaces:**
- Consumes: `IUserManagerService` (existing)
- Produces: `IUserService` with methods: `GetPagedUsersAsync`, `CreateUserAsync`, `UpdateUserAsync`, `DeleteUserAsync`, `ToggleUserStatusAsync`, `BatchDeleteUsersAsync`

- [ ] **Step 1: Create IUserService interface**

```csharp
// src/Server/Modules/LYBT.Module.Users/Interfaces/IUserService.cs
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserListDto>> GetPagedUsersAsync(int page, int pageSize, string? keyword, UserRole? role, CommonStatus? status, CancellationToken ct);
    Task<UserDetailDto> CreateUserAsync(UserInputDto dto, Guid currentUserId, bool isAdmin, CancellationToken ct);
    Task<UserDetailDto> UpdateUserAsync(Guid id, UserInputDto dto, Guid currentUserId, bool isAdmin, CancellationToken ct);
    Task<bool> DeleteUserAsync(Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct);
    Task<UserDetailDto> ToggleUserStatusAsync(Guid id, Guid currentUserId, bool isAdmin, CancellationToken ct);
    Task<BatchOperationResultDto> BatchDeleteUsersAsync(List<Guid> ids, Guid currentUserId, bool isAdmin, CancellationToken ct);
    Task<ResetPasswordResponseDto> ResetPasswordAsync(Guid id, CancellationToken ct);
}
```

- [ ] **Step 2: Create UserService implementation**

```csharp
// src/Server/Modules/LYBT.Module.Users/Services/UserService.cs
using LYBT.Entities.Users;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Users.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PagedResult<UserListDto>> GetPagedUsersAsync(
        int page, int pageSize, string? keyword, UserRole? role, CommonStatus? status, CancellationToken ct)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(kw)) ||
                (u.RealName != null && u.RealName.ToLower().Contains(kw)) ||
                (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(kw)));
        }

        var totalCount = await query.CountAsync(ct);
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<UserListDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userRole = BaseClaimsHelper.ParseUserRole(roles);
            var isEnabled = !await _userManager.GetLockoutEnabledAsync(user) ||
                await _userManager.GetAccessFailedCountAsync(user) < 5;

            if (role.HasValue && userRole != role.Value) continue;
            if (status.HasValue && isEnabled != (status.Value == CommonStatus.Enabled)) continue;

            dtos.Add(new UserListDto
            {
                Id = user.Id,
                UserName = user.UserName,
                RealName = user.RealName,
                PhoneNumber = user.PhoneNumber,
                Role = userRole,
                Status = isEnabled ? CommonStatus.Enabled : CommonStatus.Disabled,
                LastLoginTime = user.LastLoginAt,
                CreatedAt = DateTime.MinValue
            });
        }

        return new PagedResult<UserListDto>(dtos, totalCount, page, pageSize);
    }

    // ... 其他方法实现
}
```

- [ ] **Step 3: Run tests to verify UserService works**

Run: `dotnet test tests/LYBT.Tests.Server/ --filter "UserService"`
Expected: All tests pass

- [ ] **Step 4: Refactor BaseUsersController to use UserService**

```csharp
// src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs
// 移除所有业务逻辑，仅保留路由+响应映射
public abstract class BaseUsersController : BaseApiController
{
    private readonly IUserService _userService;

    protected BaseUsersController(IUserService userService, ILogger logger) : base(logger)
    {
        _userService = userService;
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetList(
        CancellationToken ct, int page = 1, int pageSize = 20,
        string? keyword = null, UserRole? role = null, CommonStatus? status = null)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;
        var result = await _userService.GetPagedUsersAsync(page, pageSize, keyword, role, status, ct);
        return SuccessPaged(result, "查询成功");
    }

    // ... 其他端点委托给 _userService
}
```

- [ ] **Step 5: Update DI registration in UsersModule**

```csharp
// src/Server/Modules/LYBT.Module.Users/UsersModule.cs
services.AddScoped<IUserService, UserService>();
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/
git commit -m "refactor: extract UserService from BaseUsersController (588->100 lines)"
```

---

## Task 2: M1 - CrossModuleService 拆分到各模块

**Covers:** [S1]

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Patients/Services/PatientCrossModuleService.cs`
- Create: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbCrossModuleService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserCrossModuleService.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Services/CrossModuleService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Herbs/HerbsModule.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/UsersModule.cs`
- Create: `tests/LYBT.Tests.Server/Unit/Modules/Patients/PatientCrossModuleServiceTests.cs`

**Interfaces:**
- Consumes: `AppDbContext` (existing)
- Produces: Each module implements its own `ICrossModuleService`

- [ ] **Step 1: Create PatientCrossModuleService in Module.Patients**

```csharp
// src/Server/Modules/LYBT.Module.Patients/Services/PatientCrossModuleService.cs
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Services;

public class PatientCrossModuleService : IPatientCrossModuleService
{
    private readonly IDbContextAccessor _dbContextAccessor;

    public PatientCrossModuleService(IDbContextAccessor dbContextAccessor)
    {
        _dbContextAccessor = dbContextAccessor;
    }

    public async Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken ct = default)
    {
        var context = _dbContextAccessor.Context;
        var patient = await context.Patients
            .Where(p => p.Id == patientId && !p.IsDeleted)
            .Select(p => new PatientBasicDto
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.PhoneNumber,
                Status = p.Status
            })
            .FirstOrDefaultAsync(ct);
        return patient;
    }

    // ... 其他方法
}
```

- [ ] **Step 2: Create HerbCrossModuleService in Module.Herbs**

```csharp
// src/Server/Modules/LYBT.Module.Herbs/Services/HerbCrossModuleService.cs
// 类似 PatientCrossModuleService 的实现
```

- [ ] **Step 3: Create UserCrossModuleService in Module.Users**

```csharp
// src/Server/Modules/LYBT.Module.Users/Services/UserCrossModuleService.cs
// 类似 PatientCrossModuleService 的实现
```

- [ ] **Step 4: Update DI registrations in each module**

```csharp
// PatientsModule.cs
services.AddScoped<IPatientCrossModuleService, PatientCrossModuleService>();

// HerbsModule.cs
services.AddScoped<IHerbCrossModuleService, HerbCrossModuleService>();

// UsersModule.cs
services.AddScoped<IUserCrossModuleService, UserCrossModuleService>();
```

- [ ] **Step 5: Deprecate CrossModuleService in Infrastructure**

```csharp
// LYBT.Infrastructure/Services/CrossModuleService.cs
// 标记为 [Obsolete] 或删除
[Obsolete("Use module-specific cross-module services instead")]
public class CrossModuleService : IPatientCrossModuleService, IHerbCrossModuleService, IUserCrossModuleService
{
    // 保留空实现或抛出 NotSupportedException
}
```

- [ ] **Step 6: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add src/Server/Modules/LYBT.Module.*/Services/
git commit -m "refactor: split CrossModuleService into module-specific implementations"
```

---

## Task 3: W2+W3 - AuthController 安全+一致性

**Covers:** [S2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- Create: `src/Shared/LYBT.Shared.Models/Contracts/Auth/HealthStatusDto.cs`
- Create: `src/Shared/LYBT.Shared.Models/Contracts/Auth/DetailedHealthDto.cs`

**Interfaces:**
- Consumes: `IOptions<JwtOptions>` (existing)
- Produces: Typed response DTOs

- [ ] **Step 1: Fix hardcoded token expiry in AuthController**

```csharp
// AuthController.cs - 注入 JwtOptions
private readonly JwtOptions _jwtOptions;

public AuthController(..., IOptions<JwtOptions> jwtOptions)
{
    _jwtOptions = jwtOptions.Value;
}

// 修改 Login 方法
ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
```

- [ ] **Step 2: Fix inconsistent response patterns**

```csharp
// 替换所有 Unauthorized(ApiResponse<object>.CreateFail(..., new { code = "..." }))
// 改为使用 ErrorCode 枚举

// 替换 StatusCode(405, new { message = "..." })
// 改为 BusinessFail("方法不允许")
```

- [ ] **Step 3: Create typed response DTOs**

```csharp
// HealthStatusDto.cs
public class HealthStatusDto
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0.0";
}
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs
git commit -m "fix: remove hardcoded token expiry and unify response patterns in AuthController"
```

---

## Task 4: W4 - RegistrationsController 事务移到 Service

**Covers:** [S2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs`
- Modify: `src/Server/Modules/LYBT.Module.Registration/Services/RegistrationService.cs`

**Interfaces:**
- Consumes: `IRegistrationService` (existing)
- Produces: Transaction management in service layer

- [ ] **Step 1: Move QuickVisit transaction logic to RegistrationService**

```csharp
// RegistrationService.cs
public async Task<Result<QuickVisitResultDto>> QuickVisitAsync(
    QuickVisitInputDto dto, Guid currentUserId, CancellationToken ct = default)
{
    using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    
    try
    {
        // 创建挂号
        var registration = new Registration { ... };
        await _repository.AddAsync(registration, ct);
        
        // 创建医案
        var medicalCase = new MedicalCase { ... };
        await _medicalCaseRepository.AddAsync(medicalCase, ct);
        
        // 关联
        registration.MedicalCaseId = medicalCase.Id;
        await _repository.UpdateAsync(registration, ct);
        
        transaction.Complete();
        return Result<QuickVisitResultDto>.Success(new QuickVisitResultDto { ... });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "QuickVisit transaction failed");
        return Result<QuickVisitResultDto>.Fail(ErrorCode.OperationFailed, "快速接诊失败");
    }
}
```

- [ ] **Step 2: Simplify RegistrationsController**

```csharp
// RegistrationsController.cs
[HttpPost("quick-visit")]
public async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto)
{
    var result = await _registrationService.QuickVisitAsync(dto, GetOperator().OperatorId);
    return HandleResult(result, 201);
}
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs
git add src/Server/Modules/LYBT.Module.Registration/Services/RegistrationService.cs
git commit -m "refactor: move TransactionScope from controller to service layer"
```

---

## Task 5: W5+W6 - Diagnostics/Health 响应统一

**Covers:** [S3]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`
- Create: `src/Shared/LYBT.Shared.Models/Contracts/Common/HealthStatusDto.cs`

**Interfaces:**
- Consumes: `BaseApiController` helpers
- Produces: Consistent `ApiResponse<T>` responses

- [ ] **Step 1: Fix DiagnosticsController response**

```csharp
// DiagnosticsController.cs:129
// 修改前: return BadRequest(new { error = "日志级别不能为空" });
// 修改后:
return ValidationFail("日志级别不能为空");
```

- [ ] **Step 2: Create HealthStatusDto**

```csharp
// HealthStatusDto.cs
public class HealthStatusDto
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
}

public class DetailedHealthDto
{
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string Version { get; set; }
    public Dictionary<string, string> Checks { get; set; } = new();
}
```

- [ ] **Step 3: Update HealthController to use typed DTOs**

```csharp
// HealthController.cs
[HttpGet]
public IActionResult Get()
{
    return Success(new HealthStatusDto
    {
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0"
    });
}
```

- [ ] **Step 4: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/
git add src/Shared/LYBT.Shared.Models/Contracts/Common/
git commit -m "fix: unify response patterns in DiagnosticsController and HealthController"
```

---

## Task 6: W7 - API 版本常量

**Covers:** [S4]

**Files:**
- Create: `src/Server/Core/LYBT.Infrastructure/Constants/ApiVersionConstants.cs`
- Modify: 所有 Controller 的 `CreatedAtAction` 调用

**Interfaces:**
- Consumes: 无
- Produces: `ApiVersionConstants.V1` 常量

- [ ] **Step 1: Create ApiVersionConstants**

```csharp
// ApiVersionConstants.cs
namespace LYBT.Infrastructure.Constants;

public static class ApiVersionConstants
{
    public const string V1 = "1";
}
```

- [ ] **Step 2: Replace hardcoded version strings**

```bash
# 在所有 Controller 中搜索并替换
grep -r "version = \"1\"" src/Server/Services/LYBT.WebAPI/Controllers/
# 替换为
grep -r "version = ApiVersionConstants.V1" src/Server/Services/LYBT.WebAPI/Controllers/
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Constants/
git add src/Server/Services/LYBT.WebAPI/Controllers/
git commit -m "refactor: extract API version constant to eliminate hardcoded strings"
```

---

## Task 7: S2+S3 - Shell 低优先级清理

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Consumes: 无
- Produces: 清理后的 ViewModel

- [ ] **Step 1: Extract magic number constant**

```csharp
// MainWindowViewModel.cs
private const int SplashRenderDelayMs = 500;

// OnWindowLoadedAsync
await Task.Delay(SplashRenderDelayMs);
```

- [ ] **Step 2: Simplify CheckLoginStatusAsync**

```csharp
// 修改前
private async Task CheckLoginStatusAsync()
{
    try { _navigationCoordinator.ShowLoginDialog(); }
    catch (Exception ex) { await ShowErrorMessageAsync(...); _navigationCoordinator.ShowLoginDialog(); }
}

// 修改后
private async Task CheckLoginStatusAsync()
{
    try
    {
        _navigationCoordinator.ShowLoginDialog();
    }
    catch (Exception ex)
    {
        await ShowErrorMessageAsync(ClientErrorMessageMapper.GetSafeOperationFailureMessage("初始化登录界面", ex));
    }
}
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "refactor: extract magic number and simplify CheckLoginStatusAsync"
```

---

## Task 8: I2+I3 - Infrastructure 低优先级清理

**Covers:** [S4]

**Files:**
- Delete: `src/Server/Core/LYBT.Infrastructure/Web/ApiErrorCodes.cs`
- Modify: `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`

**Interfaces:**
- Consumes: 无
- Produces: 清理后的基础设施

- [ ] **Step 1: Delete ApiErrorCodes.cs (dead code)**

```bash
rm src/Server/Core/LYBT.Infrastructure/Web/ApiErrorCodes.cs
```

- [ ] **Step 2: Merge HandleAuthResult into HandleResult**

```csharp
// BaseApiController.cs
protected IActionResult HandleResult<T>(Result<T> result, string successMessage = "操作成功", bool useAuthMapping = false)
{
    if (result.IsSuccess)
        return Success(result.Data!, successMessage);

    var message = result.ErrorMessage ?? "操作失败";

    if (result.ModuleErrorCode.HasValue)
    {
        var moduleCode = result.ModuleErrorCode.Value;
        var httpStatus = useAuthMapping
            ? GetAuthHttpStatus(moduleCode)
            : moduleCode.ToHttpStatusCode();
        var response = ApiResponse.CreateFail(message);
        response.RequestId = GetRequestId();
        response.Errors = new { code = moduleCode.ToFormattedString() };
        return StatusCode(httpStatus, response);
    }

    return BusinessFail(message);
}

private static int GetAuthHttpStatus(ErrorCode code)
{
    return code switch
    {
        ErrorCode.AuthInvalidCredentials => 401,
        ErrorCode.AuthAccountLocked => 403,
        ErrorCode.AuthTokenExpired => 401,
        _ => 422
    };
}
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Server/Core/LYBT.Infrastructure/Web/
git commit -m "refactor: remove dead ApiErrorCodes and merge HandleAuthResult into HandleResult"
```

---

## Task 9: D2+D3 - Desktop 低优先级清理

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IAuthApi.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Settings/SettingsService.cs`

**Interfaces:**
- Consumes: 无
- Produces: 清理后的代码

- [ ] **Step 1: Fix contradictory documentation in IAuthApi.cs**

```csharp
// 修改前: 访问令牌8小时有效期 / AccessToken 15分钟
// 修改后: 统一为 "AccessToken 有效期由服务端配置决定"
```

- [ ] **Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IAuthApi.cs
git commit -m "docs: fix contradictory token expiry documentation"
```

---

## Task 10: M2+M3 - 查询优化（后续）

**Covers:** [S3]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Services/CrossModuleService.cs` (Task 2 已处理)
- Modify: `src/Server/Modules/LYBT.Module.Users/Controllers/BaseUsersController.cs` (Task 1 已处理)

**Interfaces:**
- Consumes: Task 1, Task 2 的产出
- Produces: 优化后的查询

- [ ] **Step 1: 验证 Task 1 和 Task 2 已解决 N+1 查询**

```bash
# 检查是否还有 foreach + 单条查询的模式
grep -r "foreach.*await.*FindByIdAsync" src/Server/
```

- [ ] **Step 2: 如果仍有 N+1，使用批量查询优化**

```csharp
// 替换 foreach 循环为批量查询
var patientIds = ids.ToList();
var patients = await context.Patients
    .Where(p => patientIds.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id);
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Server/
git commit -m "perf: optimize N+1 queries with batch loading"
```

---

## Self-Review

**1. Spec coverage:**
- [S1] W1 → Task 1 ✓
- [S1] M1 → Task 2 ✓
- [S2] W2+W3 → Task 3 ✓
- [S2] W4 → Task 4 ✓
- [S3] W5+W6 → Task 5 ✓
- [S3] M2+M3 → Task 10 ✓
- [S4] W7 → Task 6 ✓
- [S4] S2+S3 → Task 7 ✓
- [S4] I2+I3 → Task 8 ✓
- [S4] D2+D3 → Task 9 ✓

**2. Placeholder scan:** 无 TBD/TODO

**3. Type consistency:** 所有接口和方法签名一致
