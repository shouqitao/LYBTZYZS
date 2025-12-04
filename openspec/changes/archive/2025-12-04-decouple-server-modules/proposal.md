# Proposal: decouple-server-modules

## Status
- **Phase**: Approved
- **Created**: 2025-12-04
- **Updated**: 2025-12-04
- **Author**: Claude Code

## Executive Summary

Server端模块存在过度耦合问题，与Desktop端设计思想不一致。本提案通过引入CrossModuleQueryService实现模块解耦，使Server端与Desktop端保持统一的模块边界设计。

---

## Why

### 1. Server-Client设计对比分析

本次重构的核心驱动力是**保持Server端与Desktop端设计思想的一致性**。

| 维度 | Desktop端 (良好设计) | Server端 (当前问题) |
|------|----------------------|---------------------|
| **模块依赖声明** | Prism `[ModuleDependency]` | csproj `ProjectReference` |
| **跨模块数据获取** | 通过HTTP API调用 | **直接注入其他模块Repository** |
| **聚合根遵循** | MedicalCaseRepository (Issue #1606已实现) | 部分遵循 |
| **模块边界** | **清晰隔离** | **被穿透** |

#### Desktop端的良好设计示例

```csharp
// PrescriptionsModule.cs - Desktop端
[ModuleDependency("ConsultationModule")] // 功能依赖：UI组件加载顺序
[ModuleDependency("HerbsModule")]        // 功能依赖：UI组件加载顺序
[ModuleDependency("FormulaModule")]      // 功能依赖：UI组件加载顺序
public class PrescriptionsModule : IModule { }
```

**特点**：
- 这是**功能依赖**，仅影响模块加载顺序
- 数据通过API获取，不直接访问其他模块内部
- Issue #1606已删除IPrescriptionRepository，所有Write操作通过MedicalCaseRepository聚合根

#### Server端的设计问题

```csharp
// PrescriptionService.cs - Server端 (当前)
public class PrescriptionService
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;   // 跨模块
    private readonly IPatientRepository _patientRepository;          // 跨模块
    private readonly IConsultationRepository _consultationRepository; // 跨模块
    // ...
}
```

**问题**：
- 直接穿透其他模块边界
- 违反模块封装原则
- 与Desktop端设计思想不一致

### 2. 行业最佳实践参考

#### ABP Framework的模块通信规范

根据ABP Framework文档，推荐的跨模块通信方式：

| 通信方式 | ABP实现 | 本项目对应 |
|----------|---------|------------|
| **Application Contracts** | 共享DTO和接口定义 | LYBT.Shared.Models |
| **Service接口依赖** | 通过IAppService接口 | ICrossModuleQueryService |
| **禁止直接Repository** | 模块内部实现细节 | ✅ 本提案目标 |

#### Microsoft eShopOnContainers

- 使用简化CQRS模式
- 读写操作分离
- 跨服务通信通过API或事件

### 3. 当前模块依赖现状

```
Auth ──────────────────► Users
MedicalCase ───────────► Patients, Users
Consultation ──────────► MedicalCase
Prescriptions ─────────► MedicalCase, Patients, Consultation, Herbs, Formula (5个需解耦)
Formula ───────────────► Herbs (1个需解耦)
```

### 4. 问题分析

| 问题 | 影响 | 严重程度 |
|------|------|----------|
| Prescriptions依赖5个模块 | 编译耦合、测试困难、变更传播 | **高** |
| Formula依赖Herbs模块 | 编译耦合、测试困难 | **中** |
| 跨模块直接注入Repository | 违反模块边界、绕过Service层 | **高** |
| Server与Desktop设计不一致 | 架构混乱、维护困难 | **高** |

### 5. 具体问题代码

#### PrescriptionService.LoadRelatedDataAsync

```csharp
// 当前：加载全量数据，性能风险
var allMedicalCases = await _medicalCaseRepository.GetAllAsync();
var allConsultations = await _consultationRepository.GetAllAsync();
var allPatients = await _patientRepository.GetAllAsync();
```

#### FormulaService.ValidateFormulaHerbAsync

```csharp
// 当前：直接注入IHerbRepository
var selectedHerb = await _herbRepository.GetByIdAsync(selectedHerbId);
```

---

## What Changes

### 1. 建立模块间通信规范 (NEW spec: module-communication)

定义Server模块之间的合法通信方式：

| 通信方式 | 适用场景 | 示例 |
|----------|----------|------|
| **通过DTO契约** | 跨模块数据传递 | Shared.Models中的DTO |
| **通过Service接口** | 需要业务逻辑时 | IPatientService.GetByIdAsync() |
| **通过聚合根** | DDD边界内操作 | MedicalCase包含Prescription |
| **通过CrossModuleQueryService** | 只读跨模块查询 | ICrossModuleQueryService |
| **禁止**: 跨模块Repository注入 | - | ~~IPatientRepository~~ |

### 2. 引入CrossModuleQueryService

创建轻量级的跨模块查询接口，放在Infrastructure层：

```csharp
public interface ICrossModuleQueryService
{
    // 患者查询
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId);
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds);

    // 医案查询
    Task<MedicalCaseBasicDto?> GetMedicalCaseBasicInfoAsync(Guid medicalCaseId);
    Task<Dictionary<Guid, MedicalCaseBasicDto>> GetMedicalCasesBasicInfoAsync(IEnumerable<Guid> medicalCaseIds);

    // 药材查询 (供Formula模块使用)
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId);
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin);
}
```

### 3. 设计原则

| 原则 | 说明 |
|------|------|
| **轻量封装** | 不引入框架级复杂性，仅封装跨模块查询 |
| **返回DTO** | 防止Entity泄露，符合Bounded Context |
| **批量优先** | 提供批量查询方法，避免N+1问题 |
| **只读查询** | 使用AsNoTracking()优化性能 |
| **投影查询** | 使用Select()减少数据传输 |

### 4. 重构后的依赖关系

```
                         ┌─────────────────────┐
                         │   LYBT.WebAPI       │
                         └──────────┬──────────┘
                                    │
     ┌──────────────┬───────────────┼───────────────┬──────────────┐
     ▼              ▼               ▼               ▼              ▼
┌─────────┐  ┌───────────┐  ┌─────────────┐  ┌───────────┐  ┌─────────┐
│  Auth   │  │MedicalCase│  │Prescriptions│  │  Formula  │  │  Herbs  │
│→Users   │  │→Patients  │  │→CrossModule │  │→CrossModule│  │(独立模块│
│ (Svc)   │  │→Users(Svc)│  │ QueryService│  │ QuerySvc  │  │         │
└─────────┘  └─────┬─────┘  └─────────────┘  └───────────┘  └─────────┘
                   │
                   ▼
            ┌────────────┐
            │Consultation│
            │(聚合内成员)│
            └────────────┘
```

**变化**:
- Prescriptions模块：5个跨模块依赖 → 0个 (通过CrossModuleQueryService)
- Formula模块：1个跨模块依赖 → 0个 (通过CrossModuleQueryService)

---

## Affected Files

### NEW Files
- `src/Server/Core/LYBT.Infrastructure/Services/ICrossModuleQueryService.cs`
- `src/Server/Core/LYBT.Infrastructure/Services/CrossModuleQueryService.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Common/PatientBasicDto.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Common/MedicalCaseBasicDto.cs`
- `src/Shared/LYBT.Shared.Models/Contracts/Common/HerbBasicDto.cs`
- `tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/Services/CrossModuleQueryServiceTests.cs`

### MODIFIED Files
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
- `src/Server/Modules/LYBT.Module.Prescriptions/LYBT.Module.Prescriptions.csproj`
- `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
- `src/Server/Modules/LYBT.Module.Formula/LYBT.Module.Formula.csproj`
- `src/Server/Core/LYBT.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- `tests/UnitTests/Server/Modules/LYBT.Module.Prescriptions.Tests/Services/PrescriptionServiceTests.cs`
- `tests/UnitTests/Server/Modules/LYBT.Module.Formula.Tests/Services/FormulaServiceTests.cs`

### REMOVED Dependencies (from csproj)
- `LYBT.Module.Prescriptions` → `LYBT.Module.Patients` (移除)
- `LYBT.Module.Prescriptions` → `LYBT.Module.Consultation` (移除)
- `LYBT.Module.Prescriptions` → `LYBT.Module.MedicalCase` (移除)
- `LYBT.Module.Prescriptions` → `LYBT.Module.Herbs` (移除)
- `LYBT.Module.Prescriptions` → `LYBT.Module.Formula` (移除)
- `LYBT.Module.Formula` → `LYBT.Module.Herbs` (移除)

---

## Out of Scope

- Desktop层模块解耦(已经做得很好)
- 数据库Schema变更
- API接口变更
- 新功能开发
- 微服务拆分

---

## Success Criteria

1. **Prescriptions模块依赖从5个减少到0个** (完全独立)
2. **Formula模块依赖从1个减少到0个** (完全独立)
3. **所有跨模块通信通过CrossModuleQueryService**，不直接注入Repository
4. **Server端与Desktop端设计思想统一**
5. **模块通信规范文档化**
6. **所有单元测试通过**
7. **集成测试通过**
8. **编译无警告**

---

## Risk Assessment

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 功能回归 | 中 | 分阶段重构，每阶段独立测试 |
| 性能下降 | 低 | CrossModuleQueryService使用批量查询和AsNoTracking |
| 接口变更影响 | 低 | 内部重构，不影响API |
| 测试遗漏 | 中 | 增加单元测试覆盖 |

---

## References

- [ABP Framework - Module Dependencies](https://docs.abp.io/en/abp/latest/Module-Development-Basics)
- [Clean Architecture - Module Boundaries](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [DDD Aggregate Root Pattern](https://martinfowler.com/bliki/DDD_Aggregate.html)
- [现有service-conventions规范](../specs/service-conventions/spec.md)
- [现有repository-patterns规范](../specs/repository-patterns/spec.md)
- [现有repository-patterns规范](../specs/repository-patterns/spec.md)
