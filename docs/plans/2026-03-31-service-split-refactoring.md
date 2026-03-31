# AuthService & UserService 拆分实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 拆分 AuthService (493行) 和 UserService (587行) 为职责单一的小型服务，遵循单一职责原则

**Architecture:** 基于现有模块化架构，每模块保持独立 DI 注册，新增服务遵循 I{Service} 接口 + {Service} 实现模式

**Tech Stack:** ASP.NET Core 8, EF Core, FluentValidation, BCrypt

---

## 一、本周任务 (3月31日 - 4月6日)

### Task 1: 创建 AutoLoginService

**目标:** 将 AutoLogin 相关逻辑从 AuthService 分离

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Auth/Interfaces/IAutoLoginService.cs`
- Create: `src/Server/Modules/LYBT.Module.Auth/Services/AutoLoginService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/AuthModule.cs`

**Step 1: 定义 IAutoLoginService 接口**

```csharp
// src/Server/Modules/LYBT.Module.Auth/Interfaces/IAutoLoginService.cs
namespace LYBT.Module.Auth.Interfaces;

using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;

/// <summary>
/// AutoLoginToken 服务 - 负责 AutoLogin 的生成、验证、轮换
/// </summary>
public interface IAutoLoginService
{
    /// <summary>
    /// 使用 AutoLoginToken 自动登录
    /// </summary>
    Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成 AutoLoginToken
    /// </summary>
    string GenerateAutoLoginToken(
        Guid userId,
        string userName,
        string? deviceId,
        string? deviceName,
        string? clientIp,
        string? userAgent,
        string? familyId = null);

    /// <summary>
    /// 撤销 AutoLoginToken Family (重放攻击检测)
    /// </summary>
    Task RevokeAutoLoginTokenFamilyAsync(string familyId, string reason);
}
```

**Step 2: 实现 AutoLoginService**

```csharp
// src/Server/Modules/LYBT.Module.Auth/Services/AutoLoginService.cs
namespace LYBT.Module.Auth.Services;

using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Models;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class AutoLoginService : IAutoLoginService
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AutoLoginService> _logger;
    private readonly IJwtService _jwtService;
    private readonly IUserCrossModuleService _crossModuleQuery;
    private readonly ISecurityAuditService _auditService;

    public AutoLoginService(
        AppDbContext dbContext,
        IConfiguration configuration,
        ILogger<AutoLoginService> logger,
        IJwtService jwtService,
        IUserCrossModuleService crossModuleQuery,
        ISecurityAuditService auditService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _jwtService = jwtService;
        _crossModuleQuery = crossModuleQuery;
        _auditService = auditService;
    }

    public async Task<Result<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request, CancellationToken cancellationToken = default)
    {
        // 实现从 AuthService.LoginWithAutoTokenAsync 移动过来
        // ... (完整实现见 AuthService.cs 第 276-393 行)
    }

    public string GenerateAutoLoginToken(
        Guid userId,
        string userName,
        string? deviceId,
        string? deviceName,
        string? clientIp,
        string? userAgent,
        string? familyId = null)
    {
        // 实现从 AuthService.GenerateAutoLoginToken 移动过来
        // ... (完整实现见 AuthService.cs 第 400-425 行)
    }

    public async Task RevokeAutoLoginTokenFamilyAsync(string familyId, string reason)
    {
        // 实现从 AuthService.RevokeAutoLoginTokenFamilyAsync 移动过来
        // ... (完整实现见 AuthService.cs 第 430-455 行)
    }
}
```

**Step 3: 修改 AuthService**

```csharp
// 移除 LoginWithAutoTokenAsync 方法
// 移除 GenerateAutoLoginToken 私有方法
// 移除 RevokeAutoLoginTokenFamilyAsync 私有方法
// 注入 IAutoLoginService
// LoginAsync 中的 AutoLoginToken 生成改为调用 _autoLoginService.GenerateAutoLoginToken
```

**Step 4: 注册 DI**

```csharp
// src/Server/Modules/LYBT.Module.Auth/AuthModule.cs
services.AddScoped<IAutoLoginService, AutoLoginService>();
```

**Step 5: 编写单元测试**

```csharp
// tests/LYBT.Tests.Server.Unit/Modules/Auth/AutoLoginServiceTests.cs
// 测试用例:
// - LoginWithAutoTokenAsync_有效Token_返回登录响应
// - LoginWithAutoTokenAsync_无效Token_返回失败
// - LoginWithAutoTokenAsync_重放攻击_撤销Family
// - GenerateAutoLoginToken_生成有效Token
// - RevokeAutoLoginTokenFamilyAsync_撤销所有Token
```

**Step 6: 运行测试验证**

```bash
dotnet test tests/LYBT.Tests.Server.Unit/ --filter "AutoLoginService"
dotnet test tests/LYBT.Tests.Server/ --filter "Auth"
```

**Step 7: 提交**

```bash
git add src/Server/Modules/LYBT.Module.Auth/
git add tests/LYBT.Tests.Server.Unit/Modules/Auth/
git commit -m "refactor(auth): extract AutoLoginService from AuthService

- Move LoginWithAutoTokenAsync, GenerateAutoLoginToken, RevokeAutoLoginTokenFamilyAsync
- Add IAutoLoginService interface
- Update AuthService to use IAutoLoginService
- Add unit tests for AutoLoginService"
```

---

### Task 2: 创建 RefreshTokenRepository

**目标:** 替代 AuthService 中直接使用的 AppDbContext.RefreshTokens

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Auth/Interfaces/IRefreshTokenRepository.cs`
- Create: `src/Server/Modules/LYBT.Module.Auth/Repositories/RefreshTokenRepository.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/AutoLoginService.cs`

**Step 1: 定义 IRefreshTokenRepository 接口**

```csharp
// src/Server/Modules/LYBT.Module.Auth/Interfaces/IRefreshTokenRepository.cs
namespace LYBT.Module.Auth.Interfaces;

using LYBT.Entities.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Step 2: 实现 RefreshTokenRepository**

```csharp
// src/Server/Modules/LYBT.Module.Auth/Repositories/RefreshTokenRepository.cs
namespace LYBT.Module.Auth.Repositories;

using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;

internal class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public RefreshTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

**Step 3: 创建 AutoLoginTokenRepository**

```csharp
// 类似结构，处理 AutoLoginToken 实体
```

**Step 4: 修改 AuthService 和 AutoLoginService**

```csharp
// 移除 AppDbContext 注入
// 注入 IRefreshTokenRepository 和 IAutoLoginTokenRepository
// 替换所有 _dbContext.RefreshTokens 调用
```

**Step 5: 注册 DI**

```csharp
// src/Server/Modules/LYBT.Module.Auth/AuthModule.cs
services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
services.AddScoped<IAutoLoginTokenRepository, AutoLoginTokenRepository>();
```

**Step 6: 编写测试**

**Step 7: 运行测试并提交**

---

### Task 3: 创建 UserQueryService

**目标:** 将用户查询逻辑从 UserService 分离

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserQueryService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserQueryService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/UsersModule.cs`

**Step 1: 定义 IUserQueryService 接口**

```csharp
// src/Server/Modules/LYBT.Module.Users/Interfaces/IUserQueryService.cs
namespace LYBT.Module.Users.Interfaces;

using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;

public interface IUserQueryService
{
    Task<Result<PagedResult<UserListDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        UserRole? role = null,
        CommonStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Result<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<List<UserListDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}
```

**Step 2: 实现 UserQueryService**

```csharp
// 从 UserService 移动 GetPagedAsync, GetByIdAsync, SearchAsync
```

**Step 3: 修改 UserService**

```csharp
// 移除查询方法
// 注入 IUserQueryService (如果需要组合调用)
```

**Step 4: 编写测试**

**Step 5: 运行测试并提交**

---

### Task 4: 创建 UserPasswordService

**目标:** 将密码相关逻辑从 UserService 分离

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserPasswordService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserPasswordService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

**Step 1: 定义 IUserPasswordService 接口**

```csharp
public interface IUserPasswordService
{
    Task<Result<ResetPasswordResponseDto>> ResetPasswordAsync(Guid id, ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<UserDetailDto>> ValidatePasswordAsync(string userName, string password, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}
```

**Step 2-5: 实现、修改、测试、提交**

---

### Task 5: 创建 UserStatusService

**目标:** 将状态管理逻辑从 UserService 分离

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Interfaces/IUserStatusService.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Services/UserStatusService.cs`
- Modify: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

**Step 1: 定义 IUserStatusService 接口**

```csharp
public interface IUserStatusService
{
    Task<Result<UserDetailDto>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UserDetailDto>> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
```

**Step 2-5: 实现、修改、测试、提交**

---

## 二、本月任务 (4月)

### Task 6: 统一 Repository 使用规范

**目标:** 统一所有模块的 Repository 模式使用

**问题:**
- 8个服务直接使用 AppDbContext
- PatientService 有 GetPagedAsync 和 GetPagedListAsync 命名不一致

**步骤:**

1. **审计直接使用 DbContext 的服务**
   - AuthService ✅ (Task 2 已处理)
   - SecurityAuditService
   - TokenManagementService
   - TokenRevocationService
   - HerbService
   - MedicalCaseAuditService
   - MedicalCaseReferenceService
   - SyncService

2. **为每个服务创建对应的 Repository**
   - ISecurityAuditRepository
   - ITokenRepository
   - IHerbQueryRepository
   - IMedicalCaseAuditRepository
   - IMedicalCaseReferenceRepository
   - ISyncRepository

3. **统一命名规范**
   - 所有分页方法统一为 `GetPagedAsync`
   - 移除 `GetPagedListAsync` (PatientService)

4. **更新 CLAUDE.md 添加规范文档**

5. **编写架构测试**
   - 验证服务不能直接注入 AppDbContext
   - 验证分页方法命名一致性

---

## 三、验收标准

### AuthService 拆分验收:
- [ ] AuthService 行数 < 300
- [ ] AutoLoginService 独立运行
- [ ] 无直接 AppDbContext 使用
- [ ] 所有现有测试通过
- [ ] 新增单元测试覆盖 AutoLoginService

### UserService 拆分验收:
- [ ] UserService 行数 < 300
- [ ] 查询、密码、状态逻辑分离
- [ ] 所有现有测试通过
- [ ] 新增单元测试覆盖新服务

### Repository 规范验收:
- [ ] 无服务直接使用 AppDbContext
- [ ] 所有分页方法统一为 GetPagedAsync
- [ ] 架构测试通过

---

## 四、风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 现有测试失败 | 高 | 每个 Task 后运行完整测试套件 |
| DI 注册遗漏 | 中 | 编写架构测试验证 DI 配置 |
| 跨模块依赖增加 | 中 | 保持 CrossModuleService 模式 |
| 性能下降 | 低 | 保持 Repository 层轻量 |

---

## 五、时间估算

| Task | 预计工时 | 优先级 |
|------|----------|--------|
| Task 1: AutoLoginService | 2小时 | P0 |
| Task 2: RefreshTokenRepository | 2小时 | P0 |
| Task 3: UserQueryService | 2小时 | P1 |
| Task 4: UserPasswordService | 2小时 | P1 |
| Task 5: UserStatusService | 1小时 | P1 |
| Task 6: Repository 规范 | 4小时 | P2 |
| **总计** | **13小时** | - |

---

**计划创建日期**: 2026-03-31
**预计完成日期**: 2026-04-06 (本周任务), 2026-04-30 (本月任务)
**下次审查**: 2026-04-07
