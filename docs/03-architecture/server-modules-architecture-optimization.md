# Server/Modules 架构优化

> 日期: 2026-06-29
> 状态: 草稿
> 范围: 8 个业务模块

## [O1] 当前架构问题

### 严重问题（CRITICAL）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| C1 | Module.Users → Module.Registration | LYBT.Module.Users.csproj | 违反模块隔离原则 |
| C2 | Module.MedicalCase → Module.Registration | LYBT.Module.MedicalCase.csproj | 违反模块隔离原则 |

### 高优先级（HIGH）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| H1 | AuthService 是死代码 | AuthService.cs (227行) | Controller 直接用 UserManager |
| H2 | Herbs 引用检查未接入 | HerbReferenceRepository.cs | 已注册但 Service 未注入 |
| H3 | Restore 操作 4 模块断裂 | 各模块 | 软删后无法恢复 |

### 中优先级（MEDIUM）

| # | 问题 | 位置 | 影响 |
|---|------|------|------|
| M1 | 快速接诊 QuickVisit 无调用方 | RegistrationService.cs | 死代码 |
| M2 | ImportExportService 不存在 | Herbs 模块 | PRD 引用但代码缺失 |

## [O2] 模块依赖图

```
LYBT.WebAPI
├── LYBT.Module.Auth ──→ LYBT.Shared.*
├── LYBT.Module.Formula ──→ LYBT.Shared.* + IHerbCrossModuleService
├── LYBT.Module.Herbs ──→ LYBT.Shared.* + ICacheInvalidationService
├── LYBT.Module.MedicalCase ──→ LYBT.Shared.* + IRegistrationCrossModuleService ⚠️
├── LYBT.Module.Patients ──→ LYBT.Shared.* + IMedicalCaseCrossModuleService
├── LYBT.Module.Registration ──→ LYBT.Shared.* + IPatientCrossModuleService
├── LYBT.Module.Reports ──→ LYBT.Shared.*
└── LYBT.Module.Users ──→ LYBT.Shared.* + IRegistrationCrossModuleService ⚠️
```

## [O3] 优化方案

### 方案 1: 模块间通信重构（C1/C2）

**当前问题**: MedicalCase 和 Users 直接引用 Registration

**解决方案**: 引入 `IRegistrationCrossModuleService`

```csharp
// 在 LYBT.Infrastructure/Services/CrossModule/ 中定义
public interface IRegistrationCrossModuleService
{
    Task CompleteByMedicalCaseAsync(Guid medicalCaseId);
    Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId);
    Task<Guid> StartVisitAsync(Guid registrationId);
    Task<int> GetWaitingCountByDoctorAsync(Guid doctorId);
}

// Module.Registration 实现
public class RegistrationCrossModuleService : IRegistrationCrossModuleService
{
    private readonly IRegistrationService _registrationService;
    
    public async Task CompleteByMedicalCaseAsync(Guid medicalCaseId)
    {
        await _registrationService.CompleteByMedicalCaseAsync(medicalCaseId);
    }
    
    public async Task HandleMedicalCaseCancelledAsync(Guid medicalCaseId)
    {
        await _registrationService.HandleMedicalCaseCancelledAsync(medicalCaseId);
    }
    
    public async Task<Guid> StartVisitAsync(Guid registrationId)
    {
        return await _registrationService.StartVisitAsync(registrationId);
    }
    
    public async Task<int> GetWaitingCountByDoctorAsync(Guid doctorId)
    {
        return await _registrationService.GetWaitingCountByDoctorAsync(doctorId);
    }
}

// Module.MedicalCase 修改
// 移除: <ProjectReference Include="..\LYBT.Module.Registration\LYBT.Module.Registration.csproj" />
// 改为注入 IRegistrationCrossModuleService

// Module.Users 修改
// 移除: <ProjectReference Include="..\LYBT.Module.Registration\LYBT.Module.Registration.csproj" />
// 改为注入 IRegistrationCrossModuleService
```

### 方案 2: 清理死代码（H1/H2/M1）

```csharp
// H1: AuthService 是死代码
// 方案 A: 删除 AuthService，Controller 直接用 UserManager（当前状态）
// 方案 B: 恢复 AuthService 作为唯一入口（推荐，符合三层架构）

// H2: Herbs 引用检查未接入
// 在 HerbService 中注入 IHerbReferenceRepository
public class HerbService : BaseService<Herb>, IHerbService
{
    private readonly IHerbReferenceRepository _referenceRepository;
    
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var refCount = await _referenceRepository
            .GetPrescriptionReferenceCountAsync(id, ct);
        if (refCount > 0)
        {
            return Result.Fail($"药材被 {refCount} 个处方引用，无法删除");
        }
        // 执行删除...
    }
}

// M1: QuickVisit 无调用方
// 检查是否需要保留，如果不需要则删除
```

### 方案 3: 修复 Restore 操作（H3）

```csharp
// 4 个模块的 Restore 操作断裂
// 需要为每个模块实现 RestoreAsync

// Patients
public async Task<Result<PatientDetailDto>> RestoreAsync(Guid id, CancellationToken ct)
{
    var entity = await _repository.GetByIdIncludingDeletedAsync(id);
    if (entity == null) return Result.Fail("患者不存在");
    
    entity.IsDeleted = false;
    entity.DeletedAt = null;
    await _repository.UpdateAsync(entity);
    return Result.Success(_mapper.ToDetailDto(entity));
}

// Herbs, Formulas, Users 同理
```

### 方案 4: 统一模块结构

```markdown
每个模块应包含:
├── Interfaces/
│   ├── I{Module}Service.cs
│   └── I{Module}Repository.cs
├── Services/
│   └── {Module}Service.cs
├── Repositories/
│   └── {Module}Repository.cs
├── Mapping/
│   └── {Module}Mapper.cs
└── {Module}Module.cs (DI 注册)

可选:
├── DTOs/ (如果不在 Shared.Models 中)
└── Validators/ (如果不在 Shared.Validators 中)
```

## [O4] 实施优先级

| 批次 | 任务 | 工作量 | 风险 |
|------|------|--------|------|
| 1 | C1/C2: 模块间通信重构 | 高 | 中 |
| 2 | H3: 修复 Restore 操作 | 中 | 低 |
| 3 | H2: 接入引用检查 | 低 | 低 |
| 4 | H1/M1: 清理死代码 | 低 | 低 |

## [O5] 成功标准

1. **模块隔离**: 所有模块间无直接 ProjectReference
2. **Restore**: 所有模块支持软删恢复
3. **引用检查**: 删除前检查引用关系
4. **死代码**: 清理所有无调用方的代码
