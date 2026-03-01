# LYBT.Module.MedicalCase 代码知识

## 模块概述

医案模块 -- Server 端核心业务模块，管理完整诊疗流程。采用 CQRS 架构拆分为 Command/Query/State 三个职责单一的 Service，通过 Facade 模式聚合供 Controller 调用。

### 架构分层

```
MedicalCaseController (API 层)
  |
MedicalCaseFacade (门面 -- 聚合 5 个 CQRS 服务，降低 Controller 依赖 8->3)
  |
  +-- IMedicalCaseCommandService  (写操作: Create/Update/Delete/Save/Print)
  +-- IMedicalCaseQueryService    (读操作: GetById/GetList/Search/Pending)
  +-- IMedicalCaseStateService    (状态操作: UpdateStatus/Complete/Suspend/Cancel)
  +-- IMedicalCasePermissionService (权限: CanEdit/CanDelete/RequiresEditReason)
  +-- IMedicalCaseAuditService    (审计: LogAsync/GetLogsPagedAsync)
  |
MedicalCaseRepository (数据访问，继承 BaseRepository<MedicalCase>)
```

### DI 注册 (MedicalCaseModule.cs)

```csharp
services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
services.AddScoped<IMedicalCaseCommandService, MedicalCaseCommandService>();
services.AddScoped<IMedicalCaseQueryService, MedicalCaseQueryService>();
services.AddScoped<IMedicalCaseStateService, MedicalCaseStateService>();
services.AddScoped<IMedicalCasePermissionService, MedicalCasePermissionService>();
services.AddScoped<IMedicalCaseAuditService, MedicalCaseAuditService>();
services.AddScoped<IMedicalCaseFacade, MedicalCaseFacade>();
services.AddSingleton<MedicalCaseMapper>();  // Mapperly 无状态单例
```

## 架构决策

| 决策 | 原因 | 日期 | 关联 OpenSpec |
|------|------|------|--------------|
| CQRS 三服务拆分 (Command/Query/State) | 单一 Service 过于庞大，Phase 3 拆分后每个 Service 职责清晰 | Phase 3 | - |
| Facade 模式聚合 5 个 Service | 降低 Controller 构造函数依赖数量 (8->3) | Phase 3 | - |
| Mapperly 替代 AutoMapper | 编译时生成映射代码，零运行时开销 | - | adopt-mapperly-unified-mapping |
| MedicalCaseRules 委托到 Shared.MedicalCaseBusinessRules | Server/Client 共享业务规则，Server 端 MedicalCaseRules 是适配器 | - | design-issues-solutions |
| 取消操作统一为软删除 | 移除 CaseStatus.Cancelled，使用 IsDeleted=true 替代 | LIFECYCLE-011 | refactor-medicalcase-api |
| 并发重试机制 (ExecuteWithConcurrencyRetryAsync) | 处方创建/更新可能遇到并发冲突，通过 ServiceHelper 统一重试 | - | - |
| GetByIdWithDetailsFreshAsync 强制从 DB 刷新 | 并发场景下 ChangeTracker 缓存可能导致 RowVersion 过期 | Issue #1669 | - |

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

1. **Step 1**: UpdateConsultationAsync -- 更新诊断 (4个核心字段)
2. **Step 2**: SetPrescriptionFlagAsync -- 标记是否需要处方
3. **Step 3a/3b**: CreatePrescriptionAsync / UpdatePrescriptionAsync -- 处方操作

### 统一保存 (SaveAsync)

- Id 为 null: 调用 CreateFromInputDtoAsync (创建)
- Id 有值: 调用 ExecuteSaveAttemptAsync (更新)
- 单事务同时保存诊断和处方数据

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

## OpenSpec 追踪

| OpenSpec ID | 内容 | 状态 |
|-------------|------|------|
| adopt-mapperly-unified-mapping | 使用 Mapperly 替代 AutoMapper | 已完成 |
| simplify-medicalcase-dataflow | 统一 SaveAsync、DoctorId -> UserId 重命名 | 已完成 |
| refactor-medicalcase-management | 权限服务 + 审计服务 (LIFECYCLE-008) | 已完成 |
| refactor-medicalcase-api | 挂起 (LIFECYCLE-010)、取消 (LIFECYCLE-011) | 已完成 |
| refactor-server-ddd-aggregates | 移除反向导航属性，共享主键关联 | 已完成 |
| refactor-diagnosis-fields | 诊断精简为 4 个核心字段 | 已完成 |
| consultation-field-alignment | PrescriptionEnabled 移至 MedicalCase.NeedsPrescription | 已完成 |
| design-issues-solutions | MedicalCaseRules 兼容设计 | 待清理 |
| optimize-batch-operations | Phase 2 批量删除 | 已完成 |
| unify-pending-query-api | 待诊队列添加 patientId 筛选 | 已完成 |
| redesign-pending-queue | 正确的状态判定和序号计算 | 已完成 |
| optimize-module-list-ui | 角色过滤支持 | 已完成 |
| optimize-entity-data-flow | 增量 API (GetListDtoAsync) | 已完成 |
| consolidate-medicalcase-queries | 跨医案搜索 (LIFECYCLE-015/016) | 已完成 |
| optimize-medicalcase-api | 统一查询接口 QueryAsync | 已完成 |
| consolidate-medicalcase-detail-queries | 批量获取详情 GetBatchAsync | 已完成 |
| clarify-cancel-consultation-logic | 删除统一为软删除 | 已完成 |
| unify-case-status | 直接使用 CaseStatus，移除 PendingCaseType 枚举 | 已完成 |

## 代码文件结构

```
LYBT.Module.MedicalCase/
├── Interfaces/
│   ├── IMedicalCaseFacade.cs          # 门面接口 (聚合 5 个 CQRS 服务)
│   ├── IMedicalCaseCommandService.cs  # 写操作接口
│   ├── IMedicalCaseQueryService.cs    # 读操作接口
│   ├── IMedicalCaseStateService.cs    # 状态管理接口
│   ├── IMedicalCasePermissionService.cs # 权限接口
│   ├── IMedicalCaseAuditService.cs    # 审计接口
│   └── IMedicalCaseRepository.cs      # 仓储接口 (继承 IRepository<MedicalCase>)
├── Services/
│   ├── MedicalCaseFacade.cs           # 门面实现 (纯委托，无业务逻辑)
│   ├── MedicalCaseCommandService.cs   # 写操作实现 (~957 行)
│   ├── MedicalCaseQueryService.cs     # 读操作实现 (~403 行)
│   ├── MedicalCaseStateService.cs     # 状态管理实现 (~305 行)
│   ├── MedicalCasePermissionService.cs # 权限实现 (~238 行)
│   ├── MedicalCaseAuditService.cs     # 审计实现 (~227 行)
│   ├── MedicalCaseServiceHelper.cs    # 共享 Helper (静态类)
│   └── MedicalCaseRules.cs           # 业务规则适配器 (静态类，兼容设计)
├── Repositories/
│   └── MedicalCaseRepository.cs       # 仓储实现 (internal, ~631 行)
├── Mapping/
│   └── MedicalCaseMapper.cs           # Mapperly 编译时映射器 (~399 行)
└── MedicalCaseModule.cs               # DI 注册模块
```

### MedicalCaseFacade (门面 -- 纯委托)

所有方法均为一行委托，无额外逻辑。Controller 仅依赖 Facade。

| 方法 | 委托目标 | 说明 |
|------|----------|------|
| `SaveAsync` | CommandService.SaveAsync | 统一保存 (创建/更新) |
| `SetPrescriptionFlagAsync` | CommandService.SetPrescriptionFlagAsync | 处方标记 |
| `DeleteAsync` | CommandService.DeleteAsync | 单条软删除 |
| `BatchDeleteAsync` | CommandService.BatchDeleteAsync | 批量软删除 |
| `RecordPrintCompletedAsync` | CommandService.RecordPrintCompletedAsync | 打印回写 |
| `AddPrintLogAsync` | CommandService.AddPrintLogAsync | 打印日志 |
| `UpdateStatusAsync` | StateService.UpdateStatusAsync | 状态更新 |
| `CompleteAsync` | StateService.CompleteAsync | 完成医案 |
| `SuspendAsync` | StateService.SuspendAsync | 挂起医案 |
| `CancelAsync` | StateService.CancelAsync | 取消 (软删除) |
| `GetByIdAsync` | QueryService.GetByIdAsync | 按 ID 查询 |
| `GetListDtoAsync` | QueryService.GetListDtoAsync | 分页列表 (DTO) |
| `GetConsultationListAsync` | QueryService.GetConsultationListAsync | 辨证记录 |
| `GetPrescriptionListAsync` | QueryService.GetPrescriptionListAsync | 处方记录 |
| `GetPendingCasesAsync` | QueryService.GetPendingCasesAsync | 待诊队列 (医生) |
| `GetAllPendingCasesAsync` | QueryService.GetAllPendingCasesAsync | 待诊队列 (管理员) |
| `SearchMedicalCasesAsync` | QueryService.SearchMedicalCasesAsync | 跨医案搜索 |
| `QueryAsync` | QueryService.QueryAsync | 统一查询 |
| `GetBatchAsync` | QueryService.GetBatchAsync | 批量获取 |
| `GetPermissions` | PermissionService.GetPermissions | 权限详情 |
| `GetAuditLogsPagedAsync` | AuditService.GetLogsPagedAsync | 审计日志 |

### MedicalCaseCommandService (写操作)

| 公共方法 | 说明 | Facade 暴露 |
|----------|------|-------------|
| `CreateAsync(patientId, visitDate, doctorId)` | 创建医案 (委托 CreateFromInputDtoAsync) | 否 (仅接口定义，未被外部调用) |
| `SaveAsync(input, currentUserId, isAdmin)` | 统一保存 (Id=null 创建, Id 有值更新) | 是 |
| `UpdateConsultationAsync(id, request, userId, isAdmin, editReason)` | 更新辨证 (三步流程 Step 1) | 否 (直接通过 Controller 调用) |
| `SetPrescriptionFlagAsync(id, flag, userId, isAdmin)` | 标记处方需求 (三步流程 Step 2) | 是 |
| `CreatePrescriptionAsync(mcId, request)` | 创建处方 (三步流程 Step 3a, 含并发重试) | 否 (直接通过 Controller 调用) |
| `UpdatePrescriptionAsync(mcId, rxId, request, userId, isAdmin, editReason)` | 更新处方 (三步流程 Step 3b) | 否 (直接通过 Controller 调用) |
| `DeletePrescriptionAsync(mcId, rxId, userId, isAdmin)` | 删除处方 (软删除) | 否 (直接通过 Controller 调用) |
| `DeleteAsync(id, operatorId, isAdmin)` | 删除医案 (软删除) | 是 |
| `BatchDeleteAsync(ids, operatorId, isAdmin)` | 批量删除 | 是 |
| `RecordPrintCompletedAsync(mcId, printType, printedBy, name, printer)` | 打印完成回写 (T2-X8) | 是 |
| `AddPrintLogAsync(mcId, printType, isSuccess, printedBy, name, printer, error)` | 打印日志 (T4-S5-02) | 是 |

关键私有方法:
- `CreateFromInputDtoAsync` -- 统一创建入口 (SaveAsync 的创建分支)
- `ExecuteSaveAttemptAsync` -- 更新逻辑 (含并发重试)
- `HandlePrescriptionUpdateAsync` -- 处方子操作分发 (创建/更新/软删除)
- `CreatePrescriptionItemsAsync` -- 处方项创建 (含 UnitPrice 自动填充 T2-S4-02)
- `GenerateCaseNumberAsync` -- MC 编号生成 (格式: MC + yyyyMMdd + 3位序号)
- `GeneratePrescriptionNumberAsync` -- RX 编号生成 (格式: RX + yyyyMMdd + 4位序号)
- `CloneMedicalCaseForAudit` -- 审计快照 (委托 ServiceHelper)
- `LogUpdateAuditAsync` -- 更新审计日志

### MedicalCaseQueryService (读操作)

| 公共方法 | 说明 | Facade 暴露 |
|----------|------|-------------|
| `GetByIdAsync(id)` | 按 ID 获取详情 (含关联数据) | 是 |
| `GetListAsync(status, patientId, page, pageSize, doctorId, isAdmin, keyword)` | 分页列表 (返回实体) | 否 (仅被 GetListDtoAsync 内部调用) |
| `GetListDtoAsync(status, patientId, page, pageSize, doctorId, isAdmin, keyword)` | 分页列表 (返回 DTO) | 是 |
| `GetConsultationListAsync(mcId)` | 辨证记录列表 | 是 |
| `GetPrescriptionListAsync(mcId)` | 处方记录列表 | 是 |
| `GetUnfinishedCaseByPatientIdAsync(patientId, doctorId)` | 患者未完成医案 | 否 (被 QueryUnfinishedAsync 内部调用) |
| `GetPendingCasesAsync(doctorId, patientId)` | 待诊队列 (医生维度) | 是 |
| `GetAllPendingCasesAsync()` | 待诊队列 (管理员) | 是 |
| `SearchMedicalCasesAsync(patientName, diagnosis, start, end, page, pageSize)` | 跨医案搜索 | 是 |
| `GetPatientRecentMedicalCasesAsync(patientId, count)` | 患者最近医案 | 否 (被 QueryRecentAsync 内部调用) |
| `QueryAsync(query)` | 统一查询 (按 QueryType 分发) | 是 |
| `GetBatchAsync(ids)` | 批量获取详情 | 是 |

QueryAsync 分发表:
- `ByPatient` -> QueryByPatientAsync (private)
- `Pending` -> QueryPendingAsync (private)
- `Unfinished` -> QueryUnfinishedAsync (private)
- `Recent` -> QueryRecentAsync (private)
- 默认 -> GetListDtoAsync

### MedicalCaseStateService (状态管理)

| 公共方法 | 说明 | Facade 暴露 |
|----------|------|-------------|
| `UpdateStatusAsync(id, status)` | 状态更新 (禁止直接设 Completed) | 是 |
| `CompleteAsync(id, operatorId, isAdmin, skipWorkflowValidation)` | 完成医案 (含处方验证) | 是 |
| `CloseCaseAsync(id)` | 关闭医案 (委托 CompleteAsync, skip=true) | 否 (接口定义，未被 Facade/Controller 调用) |
| `SuspendAsync(id, input, operatorId, isAdmin)` | 挂起 (可同时更新诊断) | 是 |
| `CancelAsync(id, operatorId, isAdmin, reason)` | 取消 (软删除, T5-P2-16 原因验证) | 是 |

### MedicalCasePermissionService (权限)

| 公共方法 | 说明 | 调用方 |
|----------|------|--------|
| `CanEdit(userId, role, mc)` | 编辑权限 (UserRole 重载) | ServiceHelper, GetPermissions |
| `CanEdit(userId, isAdmin, mc)` | 编辑权限 (bool 重载) | ServiceHelper.EnsureCanEdit |
| `CanCreate(userId, role)` | 创建权限 (仅 Doctor 可创建) | MedicalCaseAuthorizationHandler |
| `CanDelete(userId, role, mc)` | 删除权限 (UserRole 重载) | GetPermissions |
| `CanDelete(userId, isAdmin, mc)` | 删除权限 (bool 重载) | ServiceHelper.EnsureCanDelete |
| `RequiresEditReason(mc)` | 是否需要修改原因 | GetPermissions |
| `RequiresEditReason(mc, currentUserId)` | 是否需要修改原因 (扩展版) | CommandService, StateService |
| `GetPermissions(userId, role, mc)` | 权限详情 DTO | Facade |

### MedicalCaseAuditService (审计)

| 公共方法 | 说明 | 调用方 |
|----------|------|--------|
| `LogAsync(before, after, operatorId, name, role, opType, reason)` | 记录变更日志 (异常不影响主流程) | CommandService, StateService |
| `GetLogsAsync(mcId)` | 获取审计日志 (全量) | 未被外部调用 |
| `GetLogsPagedAsync(mcId, page, pageSize)` | 获取审计日志 (分页) | Facade |

### MedicalCaseServiceHelper (静态工具类)

| 方法 | 说明 |
|------|------|
| `CloneMedicalCaseForAudit(source)` | 深拷贝医案 (含 Consultation/Prescription) |
| `GetOperatorInfoAsync(userCrossModule, userId, isAdmin, logger)` | 获取操作者信息 (跨模块) |
| `ValidateAndFetchCreationContextAsync(patientId, doctorId, ...)` | 创建前验证 (Patient/Doctor 存在性 + BR-001) |
| `ExecuteWithConcurrencyRetryAsync<T>(action, name, logger, maxRetries)` | 并发重试 (最多3次, 100ms*attempt 延迟) |
| `EnsureCanEdit(permissionService, mc, userId, isAdmin, op, logger)` | 编辑权限断言 (失败抛 UnauthorizedAccessException) |
| `EnsureCanDelete(permissionService, mc, userId, isAdmin, op, logger)` | 删除权限断言 |

### MedicalCaseRules (业务规则适配器)

静态类，委托到 `LYBT.Shared.Validators.BusinessRules.MedicalCaseBusinessRules`。

| 方法 | 说明 |
|------|------|
| `CanCreateNewCase(existingCases)` | 是否可创建新医案 (BR-001) |
| `HasActiveCase(existingCases)` | 是否有 Active 状态医案 |
| `HasSuspendedCase(existingCases)` | 是否有 Suspended 状态医案 |
| `IsValidStatusTransition(from, to)` | 状态流转合法性 |

### MedicalCaseRepository (仓储实现)

标记为 `internal`，继承 `BaseRepository<MedicalCase>`。

| 公共方法 | 说明 |
|----------|------|
| `GetByPatientIdAsync(patientId)` | 按患者查询 (基础查询，无关联数据) |
| `GetByIdWithDetailsAsync(id)` | 按 ID 查询 (含 Consultation/Prescription/Items) |
| `GetByIdWithDetailsFreshAsync(id)` | 按 ID 查询 (分离 ChangeTracker 后重新查询) |
| `GetPagedWithDetailsAsync(page, pageSize, status, patientId, doctorId, isAdmin, keyword)` | 分页 (全筛选版, DB 层执行) |
| `UpdateAsync(entity)` | override: 修复 Prescription/Items EF 状态 |
| `GetPendingCasesAsync(doctorId, patientId)` | 待诊队列 (Join Patient, 电话脱敏) |
| `GetAllPendingCasesAsync()` | 所有待诊队列 (管理员) |
| `QueryAsync(patientName, startDate, endDate, diagnosisKeyword)` | 多条件组合查询 |
| `GetUnfinishedCaseByPatientIdAsync(patientId, doctorId)` | 患者未完成医案 |
| `CountByPrefixAsync(prefix)` | MC 编号计数 (IgnoreQueryFilters) |
| `CountPrescriptionsByPrefixAsync(prefix)` | RX 编号计数 (IgnoreQueryFilters) |
| `GetBatchWithDetailsAsync(ids)` | 批量详情查询 (Contains 优化) |

### MedicalCaseMapper (Mapperly 编译时映射)

| 方法类型 | 方法 | 说明 |
|----------|------|------|
| partial (自动生成) | `ToListDto` / `ToListDtos` | 实体 -> 列表 DTO |
| partial (自动生成) | `ToDetailDto` / `ToDetailDtos` | 实体 -> 详情 DTO (基础字段) |
| partial (自动生成) | `ToConsultationDetailDto` | Consultation -> DTO |
| partial (自动生成) | `ToPrescriptionDetailDto` | Prescription -> DTO (基础字段) |
| partial (自动生成) | `ToPrescriptionEntity` | InputDto -> Prescription |
| partial (自动生成) | `UpdatePrescriptionEntity` | InputDto -> Prescription (更新) |
| partial (自动生成) | `ToPrescriptionItemDto` / `ToPrescriptionItemDtos` | PrescriptionItem -> DTO |
| 手动实现 | `MapToMedicalCaseDto(entity)` | 医案 DTO (简化版, Controller 用) |
| 手动实现 | `MapToMedicalCaseDetailDto(entity)` | 医案 DTO (完整版, 含嵌套 DTO) |

### MedicalCaseModule (DI 注册)

注册顺序: Repository -> Command/Query/State Services -> Permission/Audit Services -> Facade -> Validators -> Mapper(Singleton)

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| MedicalCaseRules (静态类) | 兼容设计 | MedicalCaseBusinessRules (Shared) | 待 Server 端调用者直接使用 Shared 后移除 |
| CanEditResponse / CanDeleteResponse (IMedicalCasePermissionService.cs) | [已清理] 2026-03-01 | 接口中定义但模块内外均无引用 | 已移除 |
| IMedicalCaseAuditService.GetLogsAsync (非分页版) | 低价值 | GetLogsPagedAsync 已覆盖全部场景 | 接口和实现均保留，但无外部调用 |
| IMedicalCaseCommandService.CreateAsync | 低价值 | SaveAsync (Id=null) 已替代 | 接口定义存在，未被 Facade/Controller 调用 |
| IMedicalCaseStateService.CloseCaseAsync | 低价值 | CompleteAsync(skipWorkflowValidation=true) 已替代 | 接口定义存在，未被 Facade/Controller 调用 |
| MedicalCaseMapper.ToEntity / UpdateEntity | [已清理] 2026-03-01 | CommandService 手动映射字段 | 已移除 |
| MedicalCaseMapper.ToConsultationEntity / UpdateConsultationEntity | [已清理] 2026-03-01 | CommandService 手动映射 Consultation 字段 | 已移除 |
| MedicalCaseMapper.ToPrescriptionItemEntity / ToPrescriptionItemEntities | [已清理] 2026-03-01 | CommandService.CreatePrescriptionItemsAsync 手动构建 | 已移除 |
| MedicalCaseMapper.MapToPrescriptionDetailDto (手动实现) | [已清理] 2026-03-01 | Controller 使用 MapToMedicalCaseDetailDto 已包含处方映射 | 已移除 |
| MedicalCaseRepository.GetPagedWithDetailsAsync (简化版, keyword 重载) | [已清理] 2026-03-01 | 全筛选版已覆盖全部场景，接口和实现均已移除 | 已移除 |

## 模块演进记录

- **Phase 3**: CQRS 拆分 -- MedicalCaseService -> Command/Query/State 三服务
- **Epic #1612**: 聚合根模式 -- 所有操作通过 MedicalCase 聚合根，共享主键关联
- **Epic #2210**: 多医生数据隔离 -- DoctorId -> UserId，待诊队列按医生过滤
- **T2-X8**: 打印管理 -- 打印字段迁移到 MedicalCase 层级，PrintLog 记录
- **T5-P2**: 编号自动生成 -- MC/RX 编号格式，UnitPrice 自动填充
- **S3**: 修改原因 -- 已打印/历史医案修改需要 editReason
