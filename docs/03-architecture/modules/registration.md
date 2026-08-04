# Registration 模块设计

> 日期: 2026-06-29
> US 数量: 8 (PRD)
> 复杂度: 6/10
> 状态: 草稿

## 模块概述

Registration 管理挂号流程，连接前台/医生与医案。支持两种入口：前台挂号（Source=Receptionist）和医生快速接诊（Source=Doctor）。

**职责边界**:
- 挂号的创建、取消、查询
- 待诊队列管理（Waiting Queue）
- 快速接诊（QuickVisit）— 医生直接创建挂号+医案
- 与 MedicalCase 联动（状态同步、D4 回滚）

**依赖关系**:
- 上游: Patients（患者信息）、Users（医生列表）
- 下游: MedicalCase（医案创建/完成/取消时同步）
- 无模块间直接引用（通过接口通信）

**关键 US 清单**:
REG-001 ~ REG-008（详见 PRD）

## 接口契约

### Service 接口（Server 端）

```csharp
public interface IRegistrationService
{
    Task<Result<RegistrationDetailDto>> CreateAsync(RegistrationInputDto dto);
    Task<Result> CancelAsync(Guid id);
    Task<Result<RegistrationDetailDto>> GetByIdAsync(Guid id);
    Task<Result<List<RegistrationListDto>>> GetWaitingQueueAsync(Guid? doctorId = null);
    Task<Result<PagedResult<RegistrationListDto>>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId);
    Task<Result<Guid>> StartVisitAsync(Guid registrationId);
    Task<Result> CompleteByMedicalCaseAsync(Guid medicalCaseId);
    Task<Result> HandleMedicalCaseCancelledAsync(Guid medicalCaseId);
    Task<Result<QuickVisitResultDto>> QuickVisitAsync(
        QuickVisitInputDto dto, Guid currentUserId,
        CancellationToken cancellationToken = default);
}
```

### API 端点映射

> ⚠️ 权限列为**代码实际**（2026-08-03 前旧文档为 DoctorOrAdmin 目标态）。目标态按操作级细分见 [04-permissions.md](../../01-product/04-permissions.md)：创建/取消仅前台、接诊/QuickVisit 仅 Doctor、Admin 只读。

| HTTP | 路由 | 方法 | 权限（代码实际） |
|------|------|------|------|
| POST | `/api/v1/registrations` | Create | DoctorOrAdminOrReceptionist |
| GET | `/api/v1/registrations/{id}` | GetById | DoctorOrAdminOrReceptionist |
| GET | `/api/v1/registrations` | GetList | DoctorOrAdminOrReceptionist |
| GET | `/api/v1/registrations/queue` | GetQueue | DoctorOrAdminOrReceptionist |
| PUT | `/api/v1/registrations/{id}/start-visit` | StartVisit | DoctorOrAdminOrReceptionist |
| PUT | `/api/v1/registrations/{id}/cancel` | Cancel | DoctorOrAdminOrReceptionist |
| POST | `/api/v1/registrations/quick-visit` | QuickVisit | DoctorOrAdmin |

### DTO 结构

```
RegistrationInputDto
├── PatientId: Guid
├── DoctorId: Guid
├── Source: RegistrationSource（Receptionist=0, Doctor=1）
├── RegistrationFee: decimal
└── Remark: string?

RegistrationDetailDto
├── 所有 Registration 字段
├── PatientName, DoctorName（反规范化）
└── MedicalCaseId: Guid?

RegistrationListDto
├── Id, QueueNumber, PatientName, DoctorName
├── Source, Status, RegistrationFee, CreatedAt

QuickVisitInputDto
├── PatientId: Guid
├── DoctorId: Guid

QuickVisitResultDto
├── RegistrationId, MedicalCaseId
├── PatientId, PatientName
├── DoctorId, DoctorName, CreatedAt
```

## 状态机

> **权威定义**：完整状态机（守卫条件/转换规则/联动）见 [08-registration.md](../../02-requirements/08-registration.md)；状态枚举见 [04-data-model.md](../04-data-model.md)。下图为模块视图。

> **2026-08-03 决策修订**：医案取消=物理删除（无 Cancelled 医案状态）；「接诊即建」——StartVisit 原子创建 MedicalCase(Active)+Registration(InProgress)。REG-BR-005 **放弃恢复**（D4 回滚到 Waiting 已移除）。

```
┌─────────────┐
│   Waiting    │
│  (等待中)    │
└──┬───────┬──┘
   │       │
   ▼       ▼
┌──────────────┐   ┌───────────┐
│  InProgress  │   │ Cancelled │
│  (接诊中)    │──▶│ (已取消)  │
└──────┬───────┘   └───────────┘
       │
       ▼
┌───────────┐
│ Completed │
│ (已完成)  │
└───────────┘
```

**允许的转换**: Waiting→InProgress, Waiting→Cancelled, InProgress→Completed, InProgress→Cancelled
**禁止的转换**: Waiting→Completed, InProgress→Waiting（D4 回滚已移除，REG-BR-005 放弃恢复）, Completed→任何, Cancelled→任何

**接诊即建**（2026-08-03 决策）:
- `Waiting → InProgress` 时**原子创建** MedicalCase(Active) + Registration(InProgress)，统一远程/本地两条接诊路径
- 医案取消（物理删除）时：关联 Registration 自动转为 `Cancelled`（InProgress→Cancelled）

## 数据流

### 前台挂号
```
Desktop → POST /api/v1/registrations
Controller → RegistrationService.CreateAsync
  → 校验患者存在且未禁用
  → 检查重复 Waiting 挂号（同一患者）
  → 创建 Registration（Source=Receptionist, Status=Waiting）
  → 分配 QueueNumber = 当日最大号 + 1
  → 返回 RegistrationDetailDto
```

### 医生快速接诊（QuickVisit）
```
Desktop → POST /api/v1/registrations/quick-visit
Controller → TransactionScope
  → RegistrationService.QuickVisitAsync
    → 创建 Registration（Source=Doctor, Status=InProgress）
  → MedicalCaseCommandService.CreateAsync
    → 创建 MedicalCase（Status=Suspended）
  → 返回 QuickVisitResultDto
```

### 开始就诊
```
Desktop → PUT /api/v1/registrations/{id}/start-visit
Controller → RegistrationService.StartVisitAsync
  → 校验 Status=Waiting
  → 设置 Status=InProgress
  → 返回 MedicalCaseId（如有）
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| BusinessException | 重复 Waiting 挂号 (REG-70007) | 返回 400 |
| BusinessException | 患者不存在或已禁用 (AD-01) | 返回 400 |
| BusinessException | 状态转换不允许 | 返回 400 |
| NotFoundException | GetById 找不到 | 返回 404 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| REG-70007 | 同一患者不能有多个 Waiting 挂号 | REG-001 |
| AD-01 | 患者必须存在且未禁用 | REG-001 |
| D4-Rollback | Receptionist 源→回滚 Waiting；Doctor 源→取消 | REG-004 |
| QueueNumber | 每日自动递增，跨天重置 | REG-002 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| Patients | 查询患者信息（创建时校验） | Registration → Patients |
| Users | 查询医生列表 | Registration → Users |
| MedicalCase | 状态同步（创建/完成/取消） | MedicalCase ↔ Registration |

**架构违规（待修复）**:
- MedicalCase 模块直接使用 IRegistrationRepository（而非 IRegistrationService）
- 应通过 IRegistrationService 接口通信

**SignalR**: 未实现。待诊队列通过客户端 PeriodicTimer（30秒）轮询刷新。
