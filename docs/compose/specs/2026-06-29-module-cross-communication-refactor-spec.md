# 模块间通信重构规格

> 日期: 2026-06-29
> 状态: 草稿
> 优先级: CRITICAL
> 来源: solution-architecture-optimization.md [O1] C1/C2

## [S1] 问题

Server 端两个模块直接引用 `LYBT.Module.Registration`，违反模块隔离原则：

| 模块 | 引用 | 影响 |
|------|------|------|
| `LYBT.Module.Users` | `LYBT.Module.Registration` | 违反 AGENTS.md「Modules MUST NOT reference each other」 |
| `LYBT.Module.MedicalCase` | `LYBT.Module.Registration` | 违反 AGENTS.md「Modules MUST NOT reference each other」 |

**后果**:
- 模块无法独立部署
- 编译时耦合，修改 Registration 模块会影响 Users 和 MedicalCase
- 架构测试未覆盖此检查（H1 问题）

## [S2] 目标

1. 移除 `Module.Users` 和 `Module.MedicalCase` 对 `Module.Registration` 的 ProjectReference
2. 通过 `ICrossModuleService` 接口实现跨模块通信
3. 补全架构测试，防止未来违规

## [S3] 接口设计

### IRegistrationCrossModuleService

```csharp
// 位置: LYBT.Infrastructure/Services/CrossModule/IRegistrationCrossModuleService.cs
public interface IRegistrationCrossModuleService
{
    /// <summary>
    /// MedicalCase 完成时同步 Registration 状态
    /// </summary>
    Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default);
    
    /// <summary>
    /// MedicalCase 取消时回滚 Registration 状态
    /// </summary>
    Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default);
    
    /// <summary>
    /// 开始就诊，返回 MedicalCaseId
    /// </summary>
    Task<Guid> StartVisitAsync(Guid registrationId, CancellationToken ct = default);
    
    /// <summary>
    /// 获取医生待诊数量
    /// </summary>
    Task<int> GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken ct = default);
    
    /// <summary>
    /// 检查患者是否有等待中的挂号
    /// </summary>
    Task<bool> HasWaitingRegistrationAsync(Guid patientId, CancellationToken ct = default);
}
```

### 实现

```csharp
// 位置: LYBT.Module.Registration/Services/RegistrationCrossModuleService.cs
public class RegistrationCrossModuleService : IRegistrationCrossModuleService
{
    private readonly IRegistrationService _registrationService;
    
    public RegistrationCrossModuleService(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }
    
    public async Task CompleteByMedicalCaseAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        await _registrationService.CompleteByMedicalCaseAsync(medicalCaseId);
    }
    
    public async Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId, CancellationToken ct = default)
    {
        await _registrationService.HandleMedicalCaseCancelledAsync(medicalCaseId);
    }
    
    public async Task<Guid> StartVisitAsync(Guid registrationId, CancellationToken ct = default)
    {
        return await _registrationService.StartVisitAsync(registrationId);
    }
    
    public async Task<int> GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken ct = default)
    {
        return await _registrationService.GetWaitingCountByDoctorAsync(doctorId);
    }
    
    public async Task<bool> HasWaitingRegistrationAsync(Guid patientId, CancellationToken ct = default)
    {
        // 实现...
        return false;
    }
}
```

## [S4] 消费方修改

### Module.MedicalCase

```csharp
// 修改前
public class MedicalCaseStateService : IMedicalCaseStateService
{
    private readonly IRegistrationService _registrationService; // 直接引用
    
    public MedicalCaseStateService(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }
}

// 修改后
public class MedicalCaseStateService : IMedicalCaseStateService
{
    private readonly IRegistrationCrossModuleService _registrationService; // 接口引用
    
    public MedicalCaseStateService(IRegistrationCrossModuleService registrationService)
    {
        _registrationService = registrationService;
    }
}
```

### Module.Users

```csharp
// 修改前
// UsersController 或 UserService 直接引用 IRegistrationService

// 修改后
// 注入 IRegistrationCrossModuleService
```

## [S5] DI 注册

```csharp
// Module.Registration/RegistrationModule.cs
public static IServiceCollection AddRegistrationModule(this IServiceCollection services)
{
    services.AddScoped<IRegistrationRepository, RegistrationRepository>();
    services.AddScoped<IRegistrationService, RegistrationService>();
    services.AddScoped<IRegistrationCrossModuleService, RegistrationCrossModuleService>(); // 新增
    return services;
}
```

## [S6] 架构测试补全

```csharp
// ArchTests.cs 新增
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

    foreach (var module in moduleAssemblies)
    {
        var referencedModules = module.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("LYBT.Module.") == true 
                     && a.Name != module.GetName().Name)
            .Select(a => a.Name)
            .ToList();

        referencedModules.Should().BeEmpty(
            $"模块 {module.GetName().Name} 不应引用其他模块");
    }
}
```

## [S7] 实施步骤

1. 在 `LYBT.Infrastructure/Services/CrossModule/` 创建 `IRegistrationCrossModuleService`
2. 在 `LYBT.Module.Registration/Services/` 创建 `RegistrationCrossModuleService`
3. 更新 `RegistrationModule.cs` DI 注册
4. 修改 `Module.MedicalCase` 移除 ProjectReference，注入接口
5. 修改 `Module.Users` 移除 ProjectReference，注入接口
6. 更新架构测试
7. 运行 `dotnet build` 和 `dotnet test` 验证

## [S8] 验收标准

1. `LYBT.Module.Users.csproj` 不再引用 `LYBT.Module.Registration`
2. `LYBT.Module.MedicalCase.csproj` 不再引用 `LYBT.Module.Registration`
3. 所有现有测试通过
4. 架构测试检测到模块间引用违规
5. `dotnet build` 成功
