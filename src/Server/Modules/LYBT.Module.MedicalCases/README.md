# LYBT.Module.MedicalCase

> 医案管理(核心聚合根) | Service 方法级 CQRS | 状态机驱动

## 项目定位

- **层级**: Server端
- **架构模式**: CQRS（读写分离，Service 方法级拆分，非 MediatR 请求类）
- **跨模块通信**: IMedicalCaseCrossModuleService（供 Patients/Registration 消费）；反向消费 IPatientCrossModuleService/ICatalogCrossModuleService/IRegistrationCrossModuleService

## 目录结构（2026-08 实际）

```
LYBT.Module.MedicalCases/
├── MedicalCaseModule.cs            # DI 注册（AddMedicalCaseModule）
├── Controllers/
│   └── BaseMedicalCasesController.cs   # 抽象基础控制器（宿主继承）
├── Interfaces/                     # IMedicalCase*Service + IMedicalCase*Repository
├── Services/
│   ├── MedicalCaseQueryService.cs      # 读操作（方法级 Query 拆分）
│   ├── MedicalCaseCommandService.cs    # 写操作（partial 主文件 + Deletion 片段）
│   ├── MedicalCaseStateService.cs      # 状态流转
│   ├── PrescriptionItemService.cs      # 处方条目（无接口）
│   ├── MedicalCasePrescriptionService.cs # 处方生命周期（无接口）
│   ├── MedicalCaseCrossModuleService.cs  # 跨模块门面实现
│   └── MedicalCaseServiceHelper.cs     # 静态共享 Helper
├── Repositories/
│   └── MedicalCaseRepository.cs        # internal partial ×4（主/Update/PendingCases/AuditLogs）
├── Mappers/
│   └── MedicalCaseMapper.cs            # Mapperly + 手动 Enrich 混合
└── Infrastructure/
    └── MedicalCaseDbContext.cs         # 模块级 DbContext（ADR-0017 同库逻辑隔离）
```

> ⚠️ **与标准分层模块（Catalog/Patients/Registration/Identity）的结构差异是有意设计**，详见下文「差异化理由」。

## 状态机

```
Active <-> Suspended
  |            |
  v            v
Completed   (Cancel = SoftDelete)
```

- **Active**: 初始状态，可编辑
- **Suspended**: 挂起，保存当前数据，可恢复编辑
- **Completed**: 完成（两种路径）:
  - CompleteAsync(skipWorkflowValidation=false): 验证 NeedsPrescription + 处方存在性
  - CloseCaseAsync / CompleteAsync(skipWorkflowValidation=true): 直接完成
- **Cancel**: 不是状态值，而是 SoftDelete (IsDeleted=true)

### 三步流程 (BF-002)

1. **Step 1**: UpdateConsultationAsync — 更新诊断（4个核心字段）
2. **Step 2**: SetPrescriptionFlagAsync — 标记是否需要处方
3. **Step 3a/3b**: CreatePrescriptionAsync / UpdatePrescriptionAsync — 处方操作

### 统一保存 (SaveAsync)

- Id 为 null: 调用 CreateFromInputDtoAsync（创建）
- Id 有值: 调用 ExecuteSaveAttemptAsync（更新）
- 单事务同时保存诊断和处方数据

## 差异化理由（S1 固化，2026-08-10）

MedicalCase 是**聚合根承载模块**（MedicalCase 聚合 Consultation/Prescription/PrescriptionItem），其分层结构与其他标准分层模块不同。**此差异在 2026-08-08 结构审计中曾被标记为「需确认是有意还是 A-03 简化后遗留」（structure-audit-module-level-2026-08-08.md），本批（S1 P0-2）经评估固化为有意设计**：

| 差异 | 标准分层模块（Catalog/Patients 等） | MedicalCase（本模块） | 理由 |
|------|------|------|------|
| 命令/查询表达 | MediatR `IRequest` + `IRequestHandler`（record 命令+Handler 平铺） | **无 IRequest 类型**，CQRS 以 CommandService/QueryService/StateService 方法级拆分 | 与 A-14（2026-08-07 master-plan §九）「MediatR+Service 混合注入是有意设计」一致：查询走 Service 绕过管道（性能更优）、命令走 Service 方法级拆分；聚合根内多实体读写共享同一事务边界与状态机，方法级拆分比请求类更贴合聚合根内聚，避免为聚合根内部操作创建大量 trivial Handler 类（P1-04 已记录 MediatR 过度设计风险） |
| 目录结构 | Application/ 子目录（Commands/Queries/Validators/Mappers） | Controllers/Repositories/Mappers/Interfaces 在根层，无 Application/ 层 | 聚合根模块的读写操作按职责切分（Service/Repository/Mapper）而非按请求类切分，结构更清晰 |
| 仓储组织 | 单文件仓储 | `MedicalCaseRepository` internal partial ×4（主/Update/PendingCases/AuditLogs） | 聚合根仓储逻辑量大（EF 状态修复/待诊队列/审计），partial 按职责拆文件保持单文件 ≤240 行，同时保留一个聚合根仓储类型 |
| 验证器 | 模块内 Application/Validators | **外置** Shared.Models（`MedicalCaseInputDtoValidator`）+ 业务规则 Shared BusinessRules | 医案业务规则需 Server/Client 共享（Desktop 复用同一验证规则），故验证器放共享程序集 |
| 跨模块接口 | 接口在模块 Interfaces/ | `IMedicalCaseCrossModuleService` 定义于 LYBT.Infrastructure.Services.CrossModule | 跨模块通道统一收敛到 Infrastructure CrossModule（A-31-C8），本模块仅实现 |

**结论**：MedicalCase 保持「聚合根 + Service 方法级 CQRS + partial 仓储」结构；不迁移到 MediatR 请求类模式。此文档为差异的权威解释，后续重构若调整结构须先更新本说明。

## 核心接口

| 接口 | 说明 |
|------|------|
| IMedicalCaseQueryService | 读操作（分页/详情/搜索/待诊队列） |
| IMedicalCaseCommandService | 写操作（Create/Save/三步流程/删除，partial） |
| IMedicalCaseStateService | 状态流转（Complete/Suspend/Cancel/CloseCase） |
| IMedicalCaseRepository | 仓储（继承 IRepository<MedicalCase>，含编号生成/医生隔离） |
| IMedicalCaseReferenceRepository | 轻量只读计数（跨模块统计） |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| Repository.UpdateAsync 中 Prescription 被标记为 Modified 但实际是新建 | EF Core 自动状态检测与 MedicalCase 聚合根的交互 | FixPrescriptionEntityStatesAsync 检查 DB 中是否存在，若不存在则改为 Added |
| PrescriptionItem 更新时新 Item 被标记为 Modified | 同上，Items.Clear() + Add() 后 EF Core 误判状态 | FixExistingPrescriptionItemsStateAsync 逐个检查 |
| MaskPhoneNumber 不能在 EF Core 查询中翻译 | LINQ to SQL 不支持自定义字符串方法 | 先查询原始数据，在内存中脱敏 |
| ChangeTracker 缓存导致并发冲突 | 同一请求中多次查询同一实体，RowVersion 不一致 | 使用 GetByIdWithDetailsFreshAsync 分离后重新查询 |
| 编号生成 CountByPrefixAsync 使用 IgnoreQueryFilters | 包含软删除记录，避免编号重复 | 设计如此，不要改为带 IsDeleted 过滤 |
| 打印保护: 已打印+已完成的医案禁止修改处方 | 业务规则 T2-X8-01 | IsPrinted && IsCompleted 时抛出 BusinessException |
| 非当天本人取消需要原因 | 业务规则 T5-P2-16 | CancelAsync 中检查 isSameDay && isOwner |

## 设计依据

- 采用 CQRS 模式 (Command/Query/State) 分离读写操作，Service 方法级拆分（Phase 3）
- MedicalCase 作为聚合根，Consultation/Prescription 作为内部实体，保证数据一致性
- 状态机驱动医案生命周期 (Active/Suspended/Completed)，防止非法状态转换
- 业务规则委托到 Shared 层 BusinessRules，实现 Server/Client 规则共享
- 并发重试机制 (ExecuteWithConcurrencyRetryAsync) 处理乐观并发冲突
- Mapperly + 手动 Enrich 模式替代 AutoMapper（adopt-mapperly-unified-mapping）

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository/BaseService/CrossModule 接口/ValidationBehavior)
- LYBT.Entities (MedicalCase, Consultation, Prescription, PrescriptionItem)
- LYBT.Shared.Models (DTO + MedicalCaseInputDtoValidator)
- LYBT.Shared.ExceptionHandling (BusinessException)

### 被依赖
- LYBT.WebAPI (MedicalCasesController 继承 BaseMedicalCasesController)
- LYBT.LocalWebAPI (MedicalCasesController 继承 BaseMedicalCasesController)
- Patients 模块（经 IMedicalCaseCrossModuleService 做引用检查）
- Registration 模块（StartVisit/QuickVisit 创建医案）
