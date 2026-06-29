# MedicalCase 模块设计

> 日期: 2026-06-29
> US 数量: 18 (PRD)
> 复杂度: 8/10
> 状态: 草稿

## 模块概述

MedicalCase 是系统核心聚合根，承载中医诊疗全流程：挂号→就诊→诊断→开方→打印→取药。

**职责边界**:
- 医案的创建、保存、查询、删除（软删）
- 诊断记录（Consultation）和处方（Prescription）的聚合管理
- 医案状态流转（Suspended ↔ Active → Completed）
- 与 Registration 模块联动（创建/完成/取消时同步状态）

**依赖关系**:
- 上游: Registration（创建时关联）、Patients（患者信息）、Herbs（药材价格）
- 下游: Printing（打印处方）、Formula（导入验方）
- 无模块间直接引用（通过接口/DTO 通信）

**关键 US 清单**:
MC-001 ~ MC-018（详见 PRD）

## 接口契约

### Service 接口（Server 端）

```
IMedicalCaseQueryService
├── GetByIdAsync(Guid id) → MedicalCaseDetailDto?
├── GetListAsync(MedicalCaseQueryDto) → PagedResult<MedicalCaseListDto>
├── GetPendingListAsync() → List<PendingMedicalCaseDto>
├── SearchAsync(string keyword) → List<MedicalCaseListDto>
└── GetConsultationListAsync(Guid id) → List<ConsultationDetailDto>

IMedicalCaseCommandService
├── CreateAsync(MedicalCaseInputDto) → MedicalCaseDetailDto
├── SaveAsync(Guid id, MedicalCaseInputDto) → MedicalCaseDetailDto
├── DeleteAsync(Guid id) → bool
├── BatchDeleteAsync(List<Guid>) → int
└── SetPrescriptionFlagAsync(Guid id, bool needs) → MedicalCaseDetailDto?

IMedicalCaseStateService
├── UpdateStatusAsync(Guid id, MedicalCaseStatus) → MedicalCase?
├── CompleteAsync(Guid id, Guid operatorId, bool isAdmin, bool skipWorkflow) → MedicalCase?
├── CloseCaseAsync(Guid id) → MedicalCase?
├── SuspendAsync(Guid id, ConsultationInputDto?, Guid operatorId, bool isAdmin) → MedicalCase?
└── CancelAsync(Guid id, Guid operatorId, bool isAdmin, string? reason) → MedicalCase?

IMedicalCaseFacade（组合以上三个接口）
└── 所有方法合并暴露
```

### API 端点映射

| HTTP | 路由 | 方法 | 权限 |
|------|------|------|------|
| POST | `/api/v1/medicalcases` | CreateMedicalCase | DoctorOrAdmin |
| PUT | `/api/v1/medicalcases/{id}` | Save | DoctorOrAdmin |
| PUT | `/api/v1/medicalcases/{id}/prescription-flag` | SetPrescriptionFlag | DoctorOrAdmin |
| DELETE | `/api/v1/medicalcases/{id}` | DeleteMedicalCase | DoctorOrAdmin |
| POST | `/api/v1/medicalcases/batch-delete` | BatchDelete | DoctorOrAdmin |
| GET | `/api/v1/medicalcases/{id}` | GetById | DoctorOrAdmin |
| GET | `/api/v1/medicalcases` | GetList | DoctorOrAdmin |
| GET | `/api/v1/medicalcases/query` | GetMedicalCases | DoctorOrAdmin |
| GET | `/api/v1/medicalcases/search` | SearchMedicalCases | DoctorOrAdmin |
| GET | `/api/v1/medicalcases/{id}/consultations` | GetConsultationList | DoctorOrAdmin |
| GET | `/api/v1/medicalcases/{id}/prescriptions` | GetPrescriptionList | DoctorOrAdmin |
| PUT | `/api/v1/medicalcases/{id}/status` | UpdateStatus | DoctorOrAdmin |
| PUT | `/api/v1/medicalcases/{id}/close` | CloseMedicalCase | DoctorOrAdmin |
| PUT | `/api/v1/medicalcases/{id}/suspend` | Suspend | DoctorOrAdmin |
| PUT | `/api/v1/medicalcases/{id}/cancel` | CancelMedicalCase | DoctorOrAdmin |

### DTO 结构

```
MedicalCaseInputDto（统一输入）
├── Id: Guid?（null=创建，有值=更新）
├── PatientId: Guid
├── DoctorId: Guid?
├── DiagnosisType: DiagnosisType
├── Consultation: ConsultationInputDto?
│   ├── ChiefComplaint, PresentIllness, PastHistory
│   ├── TcmDiagnostic, TcmPattern, TcmSyndrome
│   ├── TongueInspection, PulseCondition
│   └── PhysicalExam, AuxiliaryExam, TreatmentPrinciple
└── Prescription: PrescriptionInputDto?
    ├── Diagnosis: string
    ├── Items: List<PrescriptionItemInputDto>
    │   └── HerbId, HerbName, Dosage, Unit, Usage, Note
    └── TotalPrice: decimal

MedicalCaseDetailDto（详情输出）
├── 所有 MedicalCase 字段
├── Consultation: ConsultationDetailDto?
└── Prescription: PrescriptionDetailDto?
    └── Items: List<PrescriptionItemDetailDto>

MedicalCaseListDto（列表输出）
├── Id, CaseNumber, PatientId, PatientName
├── Status, CreatedAt, LastModifiedAt
└── HasConsultation, HasPrescription, NeedsPrescription

PendingMedicalCaseDto（待诊队列）
├── PatientId, PatientName, PhoneMasked
├── QueueNumber, MedicalCaseId
```

## 状态机

```
┌─────────────┐
│  Suspended   │◄──────────────┐
│  (暂停)      │               │
└──────┬──────┘               │
       │ UpdateStatus         │ Suspend
       ▼                      │
┌─────────────┐               │
│   Active     │──────────────┘
│  (进行中)    │
└──────┬──────┘
       │ Complete
       ▼
┌─────────────┐
│  Completed   │
│  (已完成)    │──── 同日可编辑(IsLocked=false)，次日锁定(IsLocked=true)
└──────────────┘

特殊: Cancel = 软删除(IsDeleted=true)，无独立 Cancelled 状态
```

**业务规则**:
- BR-001: 同一患者同一时间只能有一个未完成医案
- BR-003: Complete 前必须设置 NeedsPrescription + 填写处方（如需）+ TcmDiagnosis 非空
- BR-MC-LOCK: Completed 同日可编辑，次日锁定
- BR-004: Admin 可跳过工作流验证强制完成

## 数据流

### 创建医案
```
Desktop → POST /api/v1/medicalcases
Controller → MedicalCaseCommandService.CreateAsync
  → 生成医案编号(MC{yyyyMMdd}{seq:3})
  → 创建 MedicalCase 实体（Status=Suspended）
  → 关联 Registration（如提供 RegistrationId）
  → 保存 Consultation + Prescription（如有）
  → 返回 MedicalCaseDetailDto
```

### 聚合保存（诊断+处方）
```
Desktop → PUT /api/v1/medicalcases/{id}
Controller → MedicalCaseCommandService.SaveAsync
  → GetByIdWithDetailsFreshAsync（ChangeTracker 脱离后重新查询）
  → 更新 MedicalCase 字段
  → 更新/新增/删除 Consultation
  → 更新/新增/删除 Prescription + PrescriptionItems
  → ExecuteWithConcurrencyRetryAsync（乐观锁重试，最多3次）
  → 返回 MedicalCaseDetailDto
```

### 完成医案
```
Desktop → PUT /api/v1/medicalcases/{id}/status (Status=Completed)
Controller → MedicalCaseStateService.CompleteAsync
  → 校验 BR-003（NeedsPrescription、TcmDiagnosis）
  → 设置 Status=Completed, CompletedAt=now
  → 更新关联 Registration 状态为 Completed
  → 返回更新后实体
```

### 取消医案
```
Desktop → PUT /api/v1/medicalcases/{id}/cancel
Controller → MedicalCaseStateService.CancelAsync
  → 软删除(IsDeleted=true, DeletedAt=now)
  → 回滚关联 Registration 状态（前台→Waiting，医生→Cancelled）
  → 返回 null（已删除）
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| BusinessException | BR-001 违反（重复未完成医案） | 返回 400 + 错误消息 |
| BusinessException | BR-003 违反（Complete 缺必要字段） | 返回 400 + 具体缺字段 |
| DbUpdateConcurrencyException | 乐观锁冲突 | 重试最多3次，最终返回 409 |
| NotFoundException | GetById 找不到 | 返回 404 |
| ArgumentException | Create 输入无效 | 返回 400 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| BR-001 | 同一患者同时只能有一个未完成医案 | MC-001 |
| BR-003 | Complete 前必须设置 NeedsPrescription + 处方 + TcmDiagnosis | MC-005 |
| BR-MC-LOCK | Completed 同日可编辑，次日锁定 | MC-008 |
| BR-004 | Admin 可跳过工作流验证 | MC-006 |
| 编号规则 | MC{yyyyMMdd}{seq:3}，RX{yyyyMMdd}{seq:4} | MC-002 |
| 软删除 | Cancel=IsDeleted=true，Restore 恢复 | MC-010 |
| 注册联动 | Create→关联Registration，Complete→Registration.Completed，Cancel→回滚 | MC-012 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| Registration | 接口调用（状态同步） | MedicalCase → Registration |
| Patients | 查询患者信息 | MedicalCase → Patients |
| Herbs | 查询药材价格（处方计算） | MedicalCase → Herbs |
| Formula | 导入验方到处方 | Formula → MedicalCase |
| Printing | 打印处方 | MedicalCase → Printing |

**架构违规（待修复）**:
- MedicalCase csproj 直接 ProjectReference Registration（违反模块隔离原则）
- 应通过接口或 MediatR 解耦
