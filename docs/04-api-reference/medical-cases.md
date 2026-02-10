# 医案 API

> Controller: `MedicalCaseController` | 路由前缀: `/api/v1/medicalcases` | 默认权限: `[Authorize(Policy = "DoctorOrAdmin")]`

## 概述

医案 (MedicalCase) 是系统核心聚合根，包含 Consultation (诊断) 和 Prescription (处方) 子实体。
采用 CQRS 原则: Command/Query/State 服务分离。所有写操作通过聚合根统一保存。
资源级授权通过 `MedicalCaseAuthorizationHandler` 实现。

---

## 写操作 (Command)

### POST /medicalcases

创建新医案。

- **权限**: Doctor Only (`[Authorize(Roles = "Doctor")]`)
- 支持创建时同时包含 Consultation 和 Prescription 数据

**请求体** (`MedicalCaseInputDto`):

```json
{
  "id": null,                    // null 触发创建
  "patientId": "guid",
  "remark": "string",
  "consultation": {              // 可选
    "presentIllness": "string",
    "tongueDiagnosis": "string",
    "pulseDiagnosis": "string",
    "tcmDiagnosis": "string"
  },
  "prescription": {              // 可选
    "dosageCount": 7,
    "discount": 1.0,
    "advice": "string",
    "remark": "string",
    "items": [
      {
        "herbId": "guid",
        "herbName": "string",
        "dosage": 10.0,
        "unit": "g",
        "unitPrice": 0.5,
        "usage": "string",
        "remark": "string",
        "decocteMethod": "string"
      }
    ]
  }
}
```

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>`

**错误响应**:
- 404: 患者不存在
- 400: 参数验证失败
- 422: 业务规则验证失败

---

### PUT /medicalcases/{id}

聚合保存 -- 在单个事务中同时保存 Consultation 和 Prescription。

**路径参数**: `id` (Guid)

**请求体**: `MedicalCaseInputDto` (同创建，但 `id` 必须与路由一致)

**授权**: 资源级授权 (Edit 操作)

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>`

**错误响应**:
- 400: 请求 ID 与路由 ID 不一致
- 403: 无权编辑此病案
- 404: 病案不存在

---

### PUT /medicalcases/{id}/prescription-flag

标记是否需要开处方 (三步流程 Step 2)。

**路径参数**: `id` (Guid)

**请求体**:

```json
{
  "needsPrescription": true
}
```

**授权**: 资源级授权 (Edit 操作)

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>`

---

### PUT /medicalcases/{id}/status

更新病案状态。支持 Draft/Active/Completed/Cancelled 状态流转。

**路径参数**: `id` (Guid)

**请求体**:

```json
{
  "status": "Draft|Active|Completed|Cancelled"
}
```

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>`

**错误响应**: 422 (非法状态流转)

---

### PUT /medicalcases/{id}/close

关闭病案 (直接标记为 Completed，不验证三步流程)。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` ("病案已关闭")

---

### PUT /medicalcases/{id}/draft

暂存医案 (保存草稿)。保存当前数据，设置状态为 Draft，不触发完成验证。

**路径参数**: `id` (Guid)

**请求体** (可选): `ConsultationInputDto`

**授权**: 资源级授权 (Edit 操作)

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` ("病案已暂存")

---

### PUT /medicalcases/{id}/cancel

取消医案。需要审计理由 (非当天本人操作时)。

**路径参数**: `id` (Guid)

**请求体** (可选):

```json
{
  "reason": "string"   // 取消原因
}
```

**授权**: 资源级授权 (Edit 操作)

**成功响应** (200): `ApiResponse<MedicalCaseDetailDto>` ("病案已取消")

---

### DELETE /medicalcases/{id}

删除病案 (软删除)。

**路径参数**: `id` (Guid)

**授权**: 资源级授权 (Delete 操作)

**成功响应** (204): 无内容

**错误响应**:
- 403: 无权删除
- 404: 病案不存在

---

### POST /medicalcases/batch-delete

批量删除医案。

**请求体**: `BatchDeleteInputDto`

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

### POST /medicalcases/batch-details

批量获取医案详情 (解决 N+1 查询问题)。

**请求体**:

```json
{
  "ids": ["guid1", "guid2", ...]   // 最多 50 个
}
```

**成功响应** (200): `ApiResponse<List<MedicalCaseDetailDto>>`

---

## 读操作 (Query)

### GET /medicalcases/{id}

获取病案详情 (含 Consultation + Prescription 完整数据)。

**路径参数**: `id` (Guid)

**MedicalCaseDetailDto**:

```json
{
  "id": "guid",
  "patientId": "guid",
  "patientName": "string",
  "userId": "guid",
  "doctorName": "string",
  "caseStatus": "Draft|Active|Completed|Cancelled",
  "remark": "string",
  "diagnosis": "string",
  "createdAt": "datetime",
  "consultation": {
    "id": "guid",
    "medicalCaseId": "guid",
    "presentIllness": "string",
    "tongueDiagnosis": "string",
    "pulseDiagnosis": "string",
    "tcmDiagnosis": "string",
    "createdAt": "datetime",
    "updatedAt": "datetime"
  },
  "prescription": {
    "id": "guid",
    "medicalCaseId": "guid",
    "prescriptionNumber": "string",
    "dosageCount": 7,
    "discount": 1.0,
    "advice": "string",
    "referencedFormulas": "string",
    "remark": "string",
    "singleDosePrice": 50.0,
    "totalPrice": 350.0,
    "totalWeight": 120.0,
    "items": [
      {
        "id": "guid",
        "herbId": "guid",
        "herbName": "string",
        "dosage": 10.0,
        "unit": "g",
        "unitPrice": 0.5,
        "totalPrice": 5.0,
        "usage": "string",
        "remark": "string",
        "decocteMethod": "string"
      }
    ],
    "createdAt": "datetime",
    "updatedAt": "datetime"
  }
}
```

---

### GET /medicalcases

医案列表 (分页)。Doctor 角色自动过滤为仅看到自己的医案。

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `status` | MedicalCaseStatus? | null | 状态筛选 |
| `patientId` | Guid? | null | 按患者筛选 |
| `page` | int | 1 | 页码 |
| `pageSize` | int | 20 | 每页大小 |
| `includeAllDoctors` | bool | false | 跨医生查询 |
| `keyword` | string? | null | 搜索关键词 |

**成功响应** (200): `ApiResponse<PagedResult<MedicalCaseListDto>>`

---

### GET /medicalcases/query

统一查询端点。整合多种查询类型。

**查询参数** (`MedicalCaseQueryDto`):

| 参数 | 类型 | 说明 |
|------|------|------|
| `queryType` | enum | All/ByPatient/Pending/Unfinished/Recent |
| `pageIndex` | int | 页码 |
| `pageSize` | int | 每页大小 (1-100) |
| `patientId` | Guid? | 患者 ID (ByPatient/Recent 必填) |
| `doctorId` | Guid? | 医生 ID (不传使用当前用户) |
| `includeAllDoctors` | bool | Admin 自动设置为 true |

**成功响应** (200): `ApiResponse<PagedResult<MedicalCaseListDto>>`

---

### GET /medicalcases/search

跨医案搜索。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `patientName` | string? | 患者名称 (模糊) |
| `diagnosisKeyword` | string? | 诊断关键词 |
| `startDate` | DateTime? | 开始日期 |
| `endDate` | DateTime? | 结束日期 |
| `page` | int | 页码 |
| `pageSize` | int | 每页大小 |

**成功响应** (200): `ApiResponse<PagedResult<MedicalCaseDetailDto>>`

---

### GET /medicalcases/{id}/permissions

获取当前用户对指定医案的权限。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<MedicalCasePermissionDto>`

```json
{
  "data": {
    "canEdit": true,
    "canDelete": false,
    "requiresReason": true,
    "editableFields": [...]
  }
}
```

---

### GET /medicalcases/{id}/audit-logs

获取医案审计日志 (分页)。

**路径参数**: `id` (Guid)

**查询参数**: `page`, `pageSize`

**成功响应** (200): `ApiResponse<MedicalCaseAuditLogPagedResultDto>`

```json
{
  "data": {
    "logs": [
      {
        "id": "guid",
        "medicalCaseId": "guid",
        "operatorId": "guid",
        "operatorName": "string",
        "operatorRole": "Doctor|Admin",
        "operationType": "string",
        "changedFields": "string",
        "oldValues": "string",
        "newValues": "string",
        "reason": "string",
        "createdAt": "datetime"
      }
    ],
    "totalCount": 10,
    "currentPage": 1,
    "pageSize": 20
  }
}
```

---

### GET /medicalcases/{medicalCaseId}/consultations

获取医案的诊断记录列表。

**路径参数**: `medicalCaseId` (Guid)

**成功响应** (200): `ApiResponse<List<ConsultationDetailDto>>`

---

### GET /medicalcases/{medicalCaseId}/prescriptions

获取医案的处方记录列表。

**路径参数**: `medicalCaseId` (Guid)

**成功响应** (200): `ApiResponse<List<PrescriptionDetailDto>>`

---

## 废弃端点

以下端点已标记 `[Obsolete]`，请迁移到统一查询端点:

| 废弃端点 | 替代方案 |
|----------|----------|
| `GET /{id}/with-details` | `GET /{id}` (已统一返回完整详情) |
| `GET /pending` | `GET /query?queryType=Pending` |
| `GET /by-patient/{patientId}` | `GET /query?queryType=ByPatient&patientId=...` |
| `GET /patient/{patientId}/recent` | `GET /query?queryType=Recent&patientId=...` |
| `GET /patient/{patientId}/unfinished` | `GET /query?queryType=Unfinished&patientId=...` |

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，18+ 端点 (含 5 个废弃) |
