# LYBT.Module.MedicalCases - Server MedicalCases Module

**Purpose**: Server-side medical case module — 聚合根承载模块（MedicalCase 唯一 DDD 聚合根）。CQRS 以 **Service 方法级拆分**（Command/Query/State），非 MediatR 请求类模式（见模块 README「差异化理由」）。

## Structure

```
LYBT.Module.MedicalCases/
├── MedicalCaseModule.cs   # DI 注册（AddMedicalCaseModule）
├── Application/           # MediatR Command/Query Handler（宿主侧薄委托）
│   ├── Commands/          # Create/Update/Complete/Cancel/Suspend/Delete/Print/Flag/BatchDelete
│   └── Queries/           # GetMedicalCase / GetMedicalCaseList / GetPendingCases
├── Controllers/           # BaseMedicalCasesController（抽象基础控制器，宿主继承）
├── Guards/                # MedicalCaseStateGuard
├── Infrastructure/        # MedicalCaseDbContext + MedicalCaseRepository(partial×4) + MedicalCaseReferenceRepository
├── Interfaces/            # 6 interface definitions
├── Mappers/               # MedicalCaseMapper (Riok.Mapperly)
└── Services/              # Query/Command(partial)/State/Time/Prescription/PrescriptionItem/CrossModule/Helper
```

### Services（实际清单）

| 文件 | 职责 |
|------|------|
| `MedicalCaseQueryService.cs` | 读操作（分页/详情/搜索/待诊/历史聚合/批量详情） |
| `MedicalCaseCommandService.cs` + `.Creation/.Audit/.Deletion.cs` | 写操作（partial：创建/聚合保存/审计/删除） |
| `MedicalCaseStateService.cs` | 状态流转（Complete/Suspend/Cancel/Close） |
| `MedicalCaseTimeService.cs` | 医案时间/锁定判定 |
| `MedicalCasePrescriptionService.cs` | 处方生命周期 |
| `PrescriptionItemService.cs` | 处方条目 |
| `MedicalCaseCrossModuleService.cs` | 跨模块门面实现（IMedicalCaseCrossModuleService） |
| `MedicalCaseServiceHelper.cs` | 静态共享 Helper |

> 无独立 `MedicalCaseFacade` / `MedicalCaseAuditService` / `MedicalCasePermissionService` / `MedicalCasePrintService` / `MedicalCaseReferenceService` / `MedicalCaseRules` 文件——相关能力分布在上述 Service 与 Infrastructure 中。

### Interfaces（6）

`IMedicalCaseCommandService`, `IMedicalCaseQueryService`, `IMedicalCaseStateService`, `IMedicalCaseTimeService`, `IMedicalCaseRepository`, `IMedicalCaseReferenceRepository`

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Write operations | `Services/MedicalCaseCommandService*.cs` | CQRS command side（partial） |
| Read operations | `Services/MedicalCaseQueryService.cs` | CQRS query side；含 `patients/{id}/history`、batch-details |
| State transitions | `Services/MedicalCaseStateService.cs` | Complete/Cancel/Suspend + 挂号联动经领域事件（ADR-0018） |
| Cross-module | `Services/MedicalCaseCrossModuleService.cs` | 实现 Infrastructure CrossModule 接口 |
| Base controller | `Controllers/BaseMedicalCasesController.cs` | 注入 Command/Query/State 三服务 |
| Interfaces | `Interfaces/` | 6 service/repository interfaces |
| DI registration | `MedicalCaseModule.cs` | `AddMedicalCaseModule` |

## CONVENTIONS

- **Service 方法级 CQRS** — Command/Query/State 拆分，不是传统 Controller→Service→Repository，也不是 MediatR 请求类模式
- **Aggregate root** — MedicalCase is sole DDD aggregate; Consultation + Prescription are internal
- **No independent repos** — Consultation/Prescription accessed only through MedicalCase aggregate/repository
- **Domain methods** — MedicalCase has `Complete()`, `SaveAsDraft()`, `SoftDelete()`, `UpdateConsultation()`
- **Module DbContext** — `MedicalCaseDbContext`（ADR-0017 同库逻辑隔离）

## ANTI-PATTERNS

- **Direct Consultation/Prescription repos** — All operations go through the MedicalCase aggregate
- **Service injecting DbContext** — Must use Repository interface
- **Cross-module references** — MUST NOT reference other server modules；跨模块走 `IMedicalCaseCrossModuleService`（定义在 Infrastructure CrossModule）
