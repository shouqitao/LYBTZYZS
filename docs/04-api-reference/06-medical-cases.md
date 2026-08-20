# 医案 API

> Controllers: `MedicalCasesController`, `MedicalCaseProcessingController` | 路由前缀: `/api/v1/medicalcases` | 默认权限: `[Authorize(Policy = "DoctorOrAdmin")]`
>
> ⚠️ **权限详见** [04-permissions.md](../01-product/04-permissions.md) §医案管理。代码已按此实现（`MedicalCasesController.cs:98` 使用 `DoctorOnly`），前台不可见。

## 概述

医案 (MedicalCase) 是系统核心聚合根，包含 Consultation (诊断) 和 Prescription (处方) 子实体。
采用 CQRS 原则（见 [[03-server]]）: Command/Query/State 服务分离。所有写操作通过聚合根统一保存。
资源级授权通过 `MedicalCaseAuthorizationHandler` 实现。

**所有响应使用统一信封**: `ApiResponse<T>` (`{ "success": true, "message": "...", "data": T, "errors": null, "timestamp": ..., "requestId": "..." }`，**无 `code` 字段**，基线§6)

---

## 写操作 (Command)

### POST /medicalcases

创建新医案，支持同时包含 Consultation 和 Prescription。`Id=null` 触发创建逻辑。

- **权限**: `[Authorize(Policy = "DoctorOrAdmin")]`（目标态详见 [04-permissions.md](../01-product/04-permissions.md) §医案管理）

**请求体** (`MedicalCaseInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | Guid? | 是 | 创建时必须为 `null` |
| `patientId` | Guid | 是 | 患者 ID |
| `userId` | Guid | 是 | 医生 ID |
| `registrationId` | Guid? | 否 | 关联挂号 ID |
| `editReason` | string? | 否 | 编辑原因 (非当天本人操作时必填) |
| `needsPrescription` | bool? | 否 | 是否开处方标志 |
| `consultation` | object? | 否 | 诊断信息 (嵌套) |
| `prescription` | object? | 否 | 处方信息 (嵌套) |
| `prescription.items` | array | 是 | 处方项列表 (至少一项) |

```bash
curl -X POST "https://api.example.com/api/v1/medicalcases" \
  -H "Authorization: Bearer ***" \
  -H "Content-Type: application/json" \
  -d '{
    "id": null,
    "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "consultation": {
      "presentIllness": "患者近一周反复咳嗽，痰多色白",
      "tongueDiagnosis": "舌淡红，苔白腻",
      "pulseDiagnosis": "脉滑",
      "tcmDiagnosis": "痰湿蕴肺证"
    },
    "prescription": {
      "dosageCount": 7,
      "items": [
        {"herbId": "c1d2e3f4-a5b6-7890-abcd-ef1234567890", "dosage": 10, "unit": "g", "unitPrice": 0.50, "subtotal": 5.00},
        {"herbId": "d2e3f4a5-b6c7-8901-abcd-ef2345678901", "dosage": 6, "unit": "g", "unitPrice": 0.30, "subtotal": 1.80}
      ]
    }
  }'
```

**成功响应** (201 Created) `ApiResponse<MedicalCaseDetailDto>` — data 部分 (下方为完整 DTO 结构，其他端点引用此处):

```json
{
  "id": "12345678-abcd-ef01-2345-678901234567",
  "createdAt": "2026-06-25T10:30:00Z",
  "caseNumber": "MC-20260625-001",
  "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "patientName": "张三",
  "doctorName": "李医生",
  "caseStatus": "Active",
  "diagnosis": "痰湿蕴肺证",
  "hasConsultation": true,
  "hasPrescription": true,
  "isLocked": false,
  "consultation": {
    "id": "22345678-abcd-ef01-2345-678901234567",
    "presentIllness": "患者近一周反复咳嗽，痰多色白，伴胸闷气短，食欲不振。",
    "tongueDiagnosis": "舌淡红，苔白腻",
    "pulseDiagnosis": "脉滑",
    "tcmDiagnosis": "痰湿蕴肺证"
  },
  "prescription": {
    "id": "32345678-abcd-ef01-2345-678901234567",
    "prescriptionNumber": "RX-20260625-001",
    "dosageCount": 7,
    "usage": "每日一剂，水煎服",
    "totalPrice": 93.80,
    "status": "Active",
    "items": [
      {
        "id": "42345678-abcd-ef01-2345-678901234567",
        "herbName": "半夏",
        "unit": "g",
        "unitPrice": 0.50,
        "dosage": 10,
        "subtotal": 5.00
      }
    ]
  }
}
```

**错误响应** (通用 401/403 见 [README](README.md#通用-http-状态码)):

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 404 | ERR-30101 | 患者不存在 |
| 422 | ERR-30103 | 该患者已有进行中的医案 (BR-001) |
| 422 | ERR-30104 | 该患者已有挂起的医案 (BR-001) |
| 422 | ERR-30105 | 该患者已被禁用 |

---

### PUT /medicalcases/{id}

聚合保存 -- 在单个事务中同时保存 Consultation 和 Prescription。

**路径参数**: `id` (Guid)

**请求体**: `MedicalCaseInputDto` (同创建，但 `id` 必须与路由一致)

| 字段 | 类型 | 说明 |
|------|------|------|
| `editReason` | string? | 修改原因。以下场景必填: 编辑已完成医案、隔天编辑、非本人编辑、**打印后编辑** (MC-D15) |

**授权**: 资源级授权 (Edit 操作)

**打印保护规则** (MC-D15):
- 当 `MedicalCase.IsPrinted=true` 时，修改 Consultation 或 Prescription 内容需提供 `editReason`，否则返回 ERR-30403
- 修改成功后: `MedicalCase.IsPrinted=false`、`MedicalCase.PrintVersion++` (标记需重新打印)
- 打印后删除处方始终禁止 (ERR-30404)

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- 结构同 POST 创建响应

**错误响应**:

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 400 | ERR-30601 | 路由 ID 与请求体 `id` 不一致 |
| 422 | ERR-30403 | 医案已打印，修改需要提供修改原因 (MC-D15) |
| 422 | ERR-30404 | 医案已打印，不允许删除处方 (MC-D15) |

---

### PUT /medicalcases/{id}/prescription-flag

标记是否需要开处方 (三步流程 Step 2)。

**路径参数**: `id` (Guid)

**请求体** (`SetPrescriptionFlagRequest`): `{ "needsPrescription": true }`

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- 返回更新后的医案详情

---

### PUT /medicalcases/{id}/status

更新医案状态。支持 Suspended/Active 状态流转。

> **注意**: 远程/本地模式的行为差异见 [05-dual-mode.md](../03-architecture/05-dual-mode.md)。`Cancelled` 状态已移除，取消操作请使用 `PUT /{id}/cancel` (软删除)。

**路径参数**: `id` (Guid)

**请求体** (`MedicalCaseStatusInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `status` | MedicalCaseStatus | 是 | `Suspended` (0) 或 `Active` (1) |
| `statusChangeReason` | string? | 否 | 状态变更原因，最多 500 字符 |

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- `caseStatus` 变为目标状态

**错误响应**:

| HTTP | 说明 |
|------|------|
| 422 | 非法状态转换 (如 Suspended → Completed) |
| 422 | 尝试设置 Completed 状态 (请使用 /close) |

---

### PUT /medicalcases/{id}/close

完成医案 (统一入口)。底层调用 `CompleteAsync`，支持 `skipWorkflowValidation` 参数跳过三步流程校验。通过聚合根域方法 `MedicalCase.Complete()` 设置状态和 CompletedAt。

**路径参数**: `id` (Guid)

**成功响应** (200) `ApiResponse<MedicalCaseDetailDto>` — `caseStatus` 变为 `Completed`，`completedAt` 设置为当前时间

**错误响应**:

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 422 | ERR-30301 | 不允许的状态转换 |
| 422 | ERR-30302 | BR-003 校验失败：未标记 NeedsPrescription |
| 422 | ERR-30303 | BR-003 校验失败：缺少处方或 TcmDiagnosis |

---

### PUT /medicalcases/{id}/suspend

挂起医案。保存当前数据，设置状态为 Suspended，不触发完成验证。

**路径参数**: `id` (Guid)

**请求体** (可选): `ConsultationInputDto` -- 可附带诊断信息一起保存

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` -- `caseStatus` 变为 `Suspended`

**错误响应**:

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 422 | ERR-30502 | 保存失败 (重试后仍失败) |

---

### PUT /medicalcases/{id}/cancel

取消医案 (统一为软删除 + 审计日志)。需要审计理由 (非当天本人操作时)。

**路径参数**: `id` (Guid)

**请求体** (可选) (`CancelMedicalCaseRequestDto`): `{ "reason": "患者要求取消挂号" }`

**成功响应** (204): 无内容 (取消操作统一为软删除，不再返回 DTO)

---

### DELETE /medicalcases/{id}

删除医案 (软删除)。

**路径参数**: `id` (Guid)

**成功响应** (200) `ApiResponse<bool>` — data: `true`

---

### POST /medicalcases/batch-delete

批量删除医案。

**请求体** (`BatchDeleteInputDto`): `{ "ids": ["guid1", "guid2"] }`

```bash
curl -X POST "https://api.example.com/api/v1/medicalcases/batch-delete" \
  -H "Authorization: Bearer ***" \
  -H "Content-Type: application/json" \
  -d '{"ids": ["12345678-abcd-ef01-2345-678901234567", "22345678-abcd-ef01-2345-678901234567"]}'
```

**成功响应** (200) `ApiResponse<BatchOperationResultDto>` — data:

```json
{
  "isSuccess": true,
  "message": "批量删除完成",
  "totalCount": 2,
  "successCount": 2,
  "failureCount": 0,
  "successRate": 100.0,
  "successfulIds": ["12345678-abcd-ef01-2345-678901234567", "22345678-abcd-ef01-2345-678901234567"],
  "failedIds": [],
  "failedItems": []
}
```

**部分失败时**: `isSuccess=true`，`message="批量删除完成，部分失败"`，`failedItems` 含失败项 `{ id, name, reason }`

**错误响应**:

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 400 | ERR-30604 | 请至少选择一个医案 (ids 为空) |

---

## 读操作 (Query)

### GET /medicalcases/{id}

获取医案详情 (含 Consultation + Prescription 完整数据)。

**路径参数**: `id` (Guid)

**成功响应** (200) `ApiResponse<MedicalCaseDetailDto>` — 结构同 [POST 创建响应](#post-medicalcases)

**错误响应**:

| HTTP | 错误码 | 说明 |
|------|--------|------|
| 404 | ERR-30607 | 医案不存在 |

---

### GET /medicalcases

医案列表 (分页)。Doctor 角色自动过滤为仅看到自己的医案。

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `status` | MedicalCaseStatus? | null | 状态筛选: `Suspended` (0) / `Active` (1) / `Completed` (2) |
| `patientId` | Guid? | null | 按患者筛选 |
| `page` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 |
| `includeAllDoctors` | bool | false | 跨医生查询 |
| `keyword` | string? | null | 搜索关键词 |

```bash
curl -X GET "https://api.example.com/api/v1/medicalcases?status=Active&page=1&pageSize=10" \
  -H "Authorization: Bearer ***"
```

**成功响应** (200) `ApiResponse<PagedResult<MedicalCaseListDto>>` — data:

```json
{
  "items": [
    {
      "id": "12345678-abcd-ef01-2345-678901234567",
      "caseNumber": "MC-20260625-001",
      "patientName": "张三",
      "doctorName": "李医生",
      "caseStatus": "Active",
      "diagnosis": "痰湿蕴肺证",
      "hasConsultation": true,
      "hasPrescription": true,
      "createdAt": "2026-06-25T10:30:00Z"
    }
  ],
  "totalCount": 25,
  "pageIndex": 1,
  "pageSize": 10
}
```

---

### GET /medicalcases/query

统一查询端点。整合多种查询类型。

**查询参数** (`MedicalCaseQueryDto`):

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `queryType` | MedicalCaseQueryType | All | 查询类型: All (0) / ByPatient (1) / Pending (2) / Unfinished (3) / Recent (4) |
| `pageIndex` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `patientId` | Guid? | null | 患者 ID (ByPatient/Recent 必填) |
| `doctorId` | Guid? | null | 医生 ID (不传使用当前用户) |
| `keyword` | string? | null | 关键字搜索 |
| `includeAllDoctors` | bool | false | Admin 自动设置为 true |
| `limit` | int? | null | 限制返回数量 (Recent 查询) |

```bash
# 查询所有 / 按患者查询 / 查询待看诊
curl -X GET "https://api.example.com/api/v1/medicalcases/query?queryType=All&pageIndex=1&pageSize=20" \
  -H "Authorization: Bearer ***"
curl -X GET "https://api.example.com/api/v1/medicalcases/query?queryType=ByPatient&patientId={guid}" \
  -H "Authorization: Bearer ***"
```

**成功响应** (200): `ApiResponse<PagedResult<MedicalCaseListDto>>` -- 结构同 GET /medicalcases

---

### GET /medicalcases/search

跨医案搜索。支持按患者名称、诊断关键词等条件查询。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `patientName` | string? | 患者名称 (模糊匹配) |
| `diagnosisKeyword` | string? | 诊断关键词 |
| `startDate` | DateTime? | 开始日期 |
| `endDate` | DateTime? | 结束日期 |
| `page` | int | 页码 (默认 1) |
| `pageSize` | int | 每页大小 (默认 20) |

**成功响应** (200) `ApiResponse<PagedResult<MedicalCaseDetailDto>>` — data 同 [POST 创建响应](#post-medicalcases) 的 MedicalCaseDetailDto

---

### GET /medicalcases/{medicalCaseId}/consultations

获取医案的诊断记录列表。

**路径参数**: `medicalCaseId` (Guid)

**成功响应** (200) `ApiResponse<List<ConsultationDetailDto>>`:

```json
[
  {
    "id": "22345678-abcd-ef01-2345-678901234567",
    "medicalCaseId": "12345678-abcd-ef01-2345-678901234567",
    "patientName": "张三",
    "doctorName": "李医生",
    "presentIllness": "患者近一周反复咳嗽，痰多色白，伴胸闷气短，食欲不振。",
    "tongueDiagnosis": "舌淡红，苔白腻",
    "pulseDiagnosis": "脉滑",
    "tcmDiagnosis": "痰湿蕴肺证"
  }
]
```

---

### GET /medicalcases/{medicalCaseId}/prescriptions

获取医案的处方记录列表。

**路径参数**: `medicalCaseId` (Guid)

**成功响应** (200) `ApiResponse<List<PrescriptionDetailDto>>`:

```json
[
  {
    "id": "32345678-abcd-ef01-2345-678901234567",
    "prescriptionNumber": "RX-20260625-001",
    "dosageCount": 7,
    "usage": "每日一剂，水煎服",
    "totalPrice": 93.80,
    "status": "Active",
    "items": [
      {
        "herbName": "半夏",
        "unit": "g",
        "unitPrice": 0.50,
        "dosage": 10,
        "subtotal": 5.00
      }
    ]
  }
]
```

---

## 复制历史处方 (FR-MC-018)

复制历史处方无专用端点，通过现有 API 组合实现 (客户端驱动模式):

1. `GET /medicalcases?patientId={patientId}&status=Completed` → 获取历史已完成医案
2. `GET /medicalcases/{historicalCaseId}` → 获取历史医案完整详情 (含 Prescription.Items)
3. 客户端组装: 预览选择药材，禁用药材跳过并提示 (MC-D09)，UnitPrice 从药材库实时获取 (MC-D13)
4. `PUT /medicalcases/{currentId}` → 聚合保存

> 业务规则详见 [07-medical-cases.md US-MC-019](../02-requirements/07-medical-cases.md#us-mc-019-复制上次处方微调)（仅复制药材与剂量、价格重新计算、禁用药材跳过、无关联复制）。

---

## 已移除端点

| 已移除端点 | 替代方案 | 移除原因 |
|----------|----------|----------|
| `GET /{id}/with-details` | `GET /{id}` | 已统一返回完整详情 |
| `GET /by-patient/{patientId}` | `GET /query?queryType=ByPatient&patientId=...` | 统一查询端点已覆盖 |
| `GET /patient/{patientId}/recent` | `GET /query?queryType=Recent&patientId=...` | 统一查询端点已覆盖 |
| `GET /patient/{patientId}/unfinished` | `GET /query?queryType=Unfinished&patientId=...` | 统一查询端点已覆盖 |

---

## 错误码

> 完整错误码定义见 [medical-cases.md PRD](../02-requirements/07-medical-cases.md)。错误码分区: 3xxxx。

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-30101 | PatientNotFound | 404 | 患者不存在 | POST / |
| ERR-30102 | DoctorNotFound | 404 | 医生不存在 | POST / |
| ERR-30103 | ActiveCaseExists | 422 | 该患者已有进行中的医案 | POST / (BR-001) |
| ERR-30104 | SuspendedCaseExists | 422 | 该患者已有挂起的医案 | POST / (BR-001) |
| ERR-30105 | PatientDisabled | 422 | 该患者已被禁用 | POST / |
| ERR-30201 | CannotEditCase | 403 | 无权限编辑此医案 | PUT /{id} |
| ERR-30202 | CannotDeleteCase | 403 | 无权限删除此医案 | DELETE /{id} |
| ERR-30203 | CannotCancelCase | 403 | 无权限取消此医案 | PUT /{id}/cancel |
| ERR-30204 | CannotDeletePrescription | 403 | 无权限删除处方 | PUT /{id} (聚合保存触发) |
| ERR-30205 | CannotSuspend | 403 | 无权限挂起此医案 | PUT /{id}/suspend |
| ERR-30301 | InvalidStatusTransition | 422 | 不允许的状态转换 | PUT /{id}/close, PUT /{id}/cancel |
| ERR-30302 | PrescriptionFlagRequired | 422 | 请先标记是否需要开处方 | PUT /{id}/close (BR-003) |
| ERR-30303 | PrescriptionRequired | 422 | 处方不存在，无法完成医案 | PUT /{id}/close (BR-003) |
| ERR-30401 | PrescriptionFlagNotSet | 422 | 未标记需要开处方 | PUT /{id} (聚合保存触发) |
| ERR-30402 | PrescriptionAlreadyExists | 422 | 医案已存在处方 | PUT /{id} (聚合保存触发) |
| ERR-30403 | PrintedRequiresReason | 422 | 医案已打印，修改需要提供修改原因 | PUT /{id} (MC-D15) |
| ERR-30404 | PrintedCannotDelete | 422 | 医案已打印，不允许删除处方 | PUT /{id} (MC-D15) |
| ERR-30501 | PrescriptionCreateRetryFailed | 500 | 创建处方失败 | PUT /{id} (聚合保存触发) |
| ERR-30502 | SaveRetryFailed | 500 | 保存失败 | PUT /{id}, PUT /{id}/suspend |
| ERR-30601 | RequestIdMismatch | 400 | 路由 ID 与请求体不一致 | PUT /{id} |
| ERR-30602 | InvalidPagination | 400 | 分页参数无效 | GET / |
| ERR-30603 | BatchQueryExceeded | 400 | 单次最多查询 50 个医案 | POST /batch-details |
| ERR-30604 | BatchOperationEmpty | 400 | 请至少选择一个医案 | POST /batch-delete |
| ERR-30607 | CaseNotFound | 404 | 医案不存在 | GET /{id}, PUT /{id}, DELETE /{id} |

---

## 待实现端点（v1.0 范围，D1/D2/D9 补回项）

> 详见 [13-project-master-plan.md](../03-architecture/13-project-master-plan.md) §六 待做工作清单。

| 端点 | 说明 | 状态 |
|------|------|------|
| `POST /medicalcases/batch-details` | 批量获取医案详情（单次最多 50 个） | ✅ 已实现 |
| `GET /medicalcases/{id}/permissions` | 获取当前用户对该医案的操作权限 | ✅ 已实现 |
| `GET /medicalcases/{id}/audit-logs` | 医案审计日志查询 | ✅ 已实现 |
| `PUT /medicalcases/{id}/print-completed` | 记录打印完成 (IsPrinted/PrintVersion/PrintCount/LastPrintedAt 回写) | 🚧 v1.0 待实现（D2） |
| `POST /medicalcases/{id}/print-logs` | 添加打印日志 | 🚧 v1.0 待实现（D2） |

> **幻影实体说明**：`AuditLog`、`MedicalCasePrintLog` 实体当前**代码中不存在**（`AppDbContext` 无对应 DbSet），属 D1/D2 补回时新建。

---

## 打印操作

打印端点已独立到 [printing.md](08-printing.md)，包含:

- `PUT /medicalcases/{id}/print-completed` — 记录打印完成 🚧（D2 待实现）
- `POST /medicalcases/{id}/print-logs` — 添加打印日志 🚧（D2 待实现）

---

## 枚举值速查

### MedicalCaseStatus

| 值 | 整数 | 说明 |
|----|------|------|
| Suspended | 0 | 已挂起 (MC-D20: 原 Draft) |
| Active | 1 | 进行中 |
| Completed | 2 | 已完成 |

### MedicalCaseQueryType

| 值 | 整数 | 说明 |
|----|------|------|
| All | 0 | 全部分页列表 |
| ByPatient | 1 | 按患者查询 |
| Pending | 2 | 待看诊 |
| Unfinished | 3 | 未完成 |
| Recent | 4 | 最近记录 (处方参考) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-21 | v1.3 | 深度重构: PUT /status 移除 Cancelled; PUT /cancel 改为 204; 4 个废弃端点移除; 术语"病案"统一为"医案" |
| 2026-02-22 | v1.4 | Draft→Suspended 状态重命名; /draft→/suspend 端点重命名 |
| 2026-06-12 | v1.5 | 打印端点移至 printing.md |
| 2026-06-12 | v1.6 | MedicalCaseInputDto 新增 userId/registrationId/editReason/needsPrescription |
| 2026-06-25 | v2.0 | 补充所有端点完整请求/响应示例; 新增枚举值速查表 |
| 2026-06-29 | v2.3 | batch-details/permissions/audit-logs 变为已实现 |
