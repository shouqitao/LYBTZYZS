# Solution 级架构优化

> 日期: 2026-06-29
> 状态: 草稿
> 范围: 全解决方案架构审计 + 优化建议

## [O1] 当前架构问题

### 严重问题（CRITICAL）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| C1 | Server 模块间直接引用 | `Module.Users` → `Module.Registration` | 违反模块隔离原则 |
| C2 | Server 模块间直接引用 | `Module.MedicalCase` → `Module.Registration` | 违反模块隔离原则 |

### 高优先级（HIGH）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| H1 | 架构测试覆盖缺口 | `ArchTests.cs` P06 | 未检测模块间引用 |
| H2 | Desktop 项目未纳入架构测试 | `DesktopLayerArchTests.cs` | 8 个项目不受保护 |
| H3 | WebAPI/LocalWebAPI Controller 重复 | 10 对 Controller | 维护漂移风险 |

### 中优先级（MEDIUM）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| M1 | ExceptionHandling 引入 ASP.NET Core + EF Core | Desktop 依赖链 | 轻量客户端依赖过重 |
| M2 | 共享 Controller 基类未统一 | WebAPI/LocalWebAPI | 重复代码 |

## [O2] 模块依赖图

### Server 端（当前）

```
LYBT.WebAPI
├── LYBT.Module.Auth
├── LYBT.Module.Formula
├── LYBT.Module.Herbs
├── LYBT.Module.MedicalCase ──→ LYBT.Module.Registration ⚠️
├── LYBT.Module.Patients
├── LYBT.Module.Registration
├── LYBT.Module.Reports
└── LYBT.Module.Users ──→ LYBT.Module.Registration ⚠️
```

### Server 端（目标）

```
LYBT.WebAPI
├── LYBT.Module.Auth
├── LYBT.Module.Formula
├── LYBT.Module.Herbs
├── LYBT.Module.MedicalCase
├── LYBT.Module.Patients
├── LYBT.Module.Registration
├── LYBT.Module.Reports
└── LYBT.Module.Users

所有模块通过 ICrossModuleService 接口通信，无直接引用
```

### Desktop 端

```
Shell
├── Roles (Admin/Clinical/Receptionist/Sysadmin)
│   └── Modules (按角色加载)
├── Core
│   ├── Infrastructure
│   ├── Foundation
│   ├── Contracts
│   ├── Controls
│   ├── Navigation
│   ├── Printing
│   ├── CardReader
│   └── LocalData (例外: 引用 LYBT.Entities)
└── LocalWebAPI (例外: 引用所有 Server 模块)
```

## [O3] 优化方案

### 方案 1: 模块间通信重构（C1/C2）

**当前问题**: `Module.MedicalCase` 和 `Module.Users` 直接引用 `Module.Registration`

**解决方案**: 引入 `ICrossModuleService` 接口

```
// 在 LYBT.Shared.Models 或 LYBT.Infrastructure 中定义
public interface IRegistrationCrossModuleService
{
    Task CompleteByMedicalCaseAsync(Guid medicalCaseId);
    Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId);
    Task<Guid> StartVisitAsync(Guid registrationId);
}

// Module.Registration 实现
public class RegistrationCrossModuleService : IRegistrationCrossModuleService
{
    // 实现...
}

// Module.MedicalCase 注入 IRegistrationCrossModuleService
// 而非直接引用 Module.Registration
```

**实施步骤**:
1. 在 `LYBT.Shared.Models` 或 `LYBT.Infrastructure` 定义 `IRegistrationCrossModuleService`
2. `Module.Registration` 实现该接口
3. `Module.MedicalCase` 和 `Module.Users` 移除对 `Module.Registration` 的 ProjectReference
4. 改为注入 `IRegistrationCrossModuleService`
5. 更新 DI 注册

### 方案 2: 架构测试补全（H1/H2）

**当前问题**: 架构测试未覆盖模块间引用和 Desktop 项目

**解决方案**: 补全 `ArchTests.cs` 和 `DesktopLayerArchTests.cs`

```csharp
// ArchTests.cs - 补充模块间引用检查
[Fact]
public void ServerModules_Should_Not_Reference_Other_ServerModules()
{
    var moduleAssemblies = new[]
    {
        typeof(LYBT.Module.Formula.FormulaModule).Assembly,
        typeof(LYBT.Module.Herbs.HerbsModule).Assembly,
        typeof(LYBT.Module.MedicalCase.MedicalCaseModule).Assembly,
        typeof(LYBT.Module.Patients.PatientsModule).Assembly,
        typeof(LYBT.Module.Registration.RegistrationModule).Assembly,
        typeof(LYBT.Module.Reports.ReportsModule).Assembly,
        typeof(LYBT.Module.Users.UsersModule).Assembly,
    };

    var otherModuleNames = moduleAssemblies
        .SelectMany(a => a.GetReferencedAssemblies())
        .Where(a => a.Name?.StartsWith("LYBT.Module.") == true)
        .Select(a => a.Name)
        .Distinct();

    // 断言: 模块不应引用其他模块
}

// DesktopLayerArchTests.cs - 补充 8 个项目
private static readonly string[] DesktopAssemblies = new[]
{
    "LYBT.Desktop.LocalData",
    "LYBT.Desktop.Navigation",
    "LYBT.Desktop.Printing",
    "LYBT.Desktop.CardReader",
    "LYBT.Desktop.Reports",
    "LYBT.Desktop.Registration",
    "LYBT.Desktop.Sysadmin",
    "LYBT.Desktop.Receptionist",
    // ... 现有项目
};
```

### 方案 3: Controller 去重（H3）

**当前问题**: WebAPI 和 LocalWebAPI 有 10 对重复 Controller

**解决方案**: 提取共享基类或使用代码生成

```
// 共享基类方案
public abstract class BaseMedicalCaseController : BaseApiController
{
    protected readonly IMedicalCaseFacade _facade;
    
    // 共享的 CRUD 逻辑
}

// WebAPI
[ApiController]
[Route("api/v{version:apiVersion}/medicalcases")]
public class MedicalCasesController : BaseMedicalCaseController
{
    // WebAPI 特有逻辑（权限、审计等）
}

// LocalWebAPI
[ApiController]
[Route("api/v1/medicalcases")]
public class MedicalCasesController : BaseMedicalCaseController
{
    // LocalWebAPI 特有逻辑（简化认证等）
}
```

### 方案 4: ExceptionHandling 依赖优化（M1）

**当前问题**: `LYBT.Shared.ExceptionHandling` 引入 ASP.NET Core + EF Core

**解决方案**: 拆分为 Desktop 和 Server 两个版本

```
LYBT.Shared.ExceptionHandling           # 仅基础异常类型
LYBT.Shared.ExceptionHandling.AspNetCore # ASP.NET Core 处理器（Server 用）
```

## [O4] 实施优先级

| 批次 | 任务 | 工作量 | 风险 |
|------|------|--------|------|
| 1 | C1/C2: 模块间通信重构 | 高 | 中 |
| 2 | H1/H2: 架构测试补全 | 低 | 低 |
| 3 | H3: Controller 去重 | 中 | 低 |
| 4 | M1: ExceptionHandling 拆分 | 中 | 低 |

## [O5] 成功标准

1. **模块隔离**: 所有 Server 模块间无直接 ProjectReference
2. **架构测试**: 100% 覆盖模块间引用检查
3. **Controller 去重**: WebAPI/LocalWebAPI 共享基类，重复代码减少 50%+
4. **依赖清洁**: Desktop 不依赖 ASP.NET Core/EF Core
