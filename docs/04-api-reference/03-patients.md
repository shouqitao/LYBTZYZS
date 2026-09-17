# 患者 API
> 版本: v1.0 | 日期: 2026-08-20

> Controller: `PatientsController` | 路由前缀: `/api/v1/patients` | 类级默认权限: `DoctorOrAdminOrReceptionist`
>
> **当前操作级策略**（与代码一致，见 [04-permissions.md](../01-product/04-permissions.md)）：
> - GET/POST/PUT、引用检查、导入模板/导出、by-id-number：类级 `DoctorOrAdminOrReceptionist`
> - DELETE、batch-delete、toggle-status、batch-import：`AdminOrSuperAdmin`
> - restore：`AdminBusinessOnly`

## 概述

患者管理 CRUD、身份证号查询、软删除恢复、批量操作、引用检查。支持 OutputCache。
Doctor 只能操作自己创建的患者，Admin 可操作全部（详见 [04-patients.md US-PAT-004/005](../02-requirements/04-patients.md)）。导入/导出为 JSON 格式。

---

## 端点列表

### GET /patients

获取患者分页列表。Non-Admin 仅可见 Enabled 患者。

- **权限**: `DoctorOrAdminOrReceptionist`

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词 (姓名/拼音码) |

**响应**: `ApiResponse<PagedResult<PatientListDto>>`

```json
{
  "items": [
    { "id": "...", "name": "张三", "gender": "Male", "age": 35, "phoneNumber": "13800138000", "status": "Enabled", "createdAt": "..." },
    { "id": "...", "name": "李四", "gender": "Female", "age": 28, "status": "Enabled", "createdAt": "..." }
  ],
  "totalCount": 156, "page": 1, "pageSize": 20, "totalPages": 8
}
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20705 | 400 | 分页参数无效 |

---

### GET /patients/{id}

获取患者详情（含年龄自动计算）。

- **权限**: `DoctorOrAdminOrReceptionist`
- **路径参数**: `id` (Guid)

**响应**: `ApiResponse<PatientDetailDto>`（字段与 `Patient` 实体/`PatientDetailDto` 对齐）

```json
{
  "id": "...",
  "name": "张三",
  "gender": "Male",
  "birthDate": "1991-03-15",
  "age": 35,
  "idNumber": "110101199103150012",
  "phoneNumber": "13800138000",
  "pinYinCode": "ZS",
  "status": "Enabled",
  "createdAt": "...",
  "updatedAt": "...",
  "createdBy": "..."
}
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20001 | 404 | 患者不存在 |

---

### POST /patients

新增患者。

- **权限**: `DoctorOrAdminOrReceptionist`

**请求体** (`PatientInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 是 | 患者姓名 |
| `gender` | enum | 否 | Male/Female/Unknown，默认 Unknown |
| `birthDate` | date | 否 | 出生日期 |
| `phoneNumber` | string | 否 | 联系电话 |
| `idNumber` | string | 否 | 身份证号 |
| `pinYinCode` | string | 否 | 拼音码 |

**响应** (201): `ApiResponse<PatientDetailDto>` — 同 GET /patients/{id}

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-00003 | 400 | 参数验证失败 |
| ERR-20002 | 409 | 身份证号已存在 |
| ERR-20003 | 409 | 患者电话已存在 |

---

### PUT /patients/{id}

更新患者信息。Doctor 仅限自己创建的患者（见 [04-patients.md US-PAT-004](../02-requirements/04-patients.md)）。

- **权限**: `DoctorOrAdminOrReceptionist`
- **路径参数**: `id` (Guid)
- **请求体**: `PatientInputDto`（同 POST）
- **响应**: `ApiResponse<PatientDetailDto>` — 返回更新后详情

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-00003 | 400 | 参数验证失败 |
| ERR-20001 | 404 | 患者不存在 |
| ERR-20002 | 409 | 身份证号已存在 |
| ERR-20003 | 409 | 患者电话已存在 |

---

### DELETE /patients/{id}

软删除患者。Doctor 仅限自己创建的患者（见 [04-patients.md US-PAT-005](../02-requirements/04-patients.md)）。

- **权限**: `AdminOrSuperAdmin`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<bool>` → `true`

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20001 | 404 | 患者不存在 |
| ERR-20004 | 422 | 患者有历史医案，无法删除 |

---

### POST /patients/{id}/toggle-status

切换患者状态（启用/禁用），无请求体，自动在 Enabled/Disabled 间切换。

- **权限**: `AdminOrSuperAdmin`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<PatientDetailDto>` — 返回切换后详情

**业务规则**: 详见 [04-patients.md US-PAT-006 业务规则](../02-requirements/04-patients.md#us-pat-006-启用禁用患者)（禁用前检查未完成医案、禁用后限制、启用后解除）。

> 交叉引用: 禁用联动见 [medical-cases.md](06-medical-cases.md) MC-D16

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20001 | 404 | 患者不存在 |
| ERR-20005 | 422 | 患者有进行中的医案 |

---

### POST /patients/batch-delete

批量软删除患者。

- **权限**: `AdminOrSuperAdmin`

**请求体** (`BatchDeleteInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 患者 ID 列表，不能为空 |

**响应**: `ApiResponse<BatchOperationResultDto>`

```json
{ "totalCount": 2, "successCount": 2, "failureCount": 0, "errors": [] }
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20703 | 400 | 请至少选择一个患者 |

---

### GET /patients/import-template

下载患者导入 JSON 模板（字段说明 + 示例，与 batch-import DTO 一致）。

- **权限**: `DoctorOrAdminOrReceptionist`
- **响应类型**: `application/json`（`ApiResponse<object>` — 含 Fields/Example）

---

### GET /patients/export

导出患者 JSON 数组（按筛选条件，敏感字段自动脱敏）。

- **权限**: `DoctorOrAdminOrReceptionist`

| 参数 | 类型 | 说明 |
|------|------|------|
| `keyword` | string? | 筛选条件 |

- **响应类型**: `application/json`（`ApiResponse<List<PatientListDto>>`）

---

### POST /patients/{id}/restore

恢复已删除的患者（绕过软删除全局过滤器）。

- **权限**: `AdminBusinessOnly`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<PatientDetailDto>`

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20702 | 200 | 该患者未被删除 |
| ERR-20001 | 404 | 患者不存在 |

---

### GET /patients/by-id-number/{idNumber}

根据身份证号查询患者（用于读卡器/快速登记）。

- **权限**: `DoctorOrAdminOrReceptionist`
- **路径参数**: `idNumber` (string)
- **响应**: `ApiResponse<PatientDetailDto>`

| 错误码 | HTTP | 说明 |
|--------|------|------|
| 404 | 404 | 未找到匹配的患者 |

---

### GET /patients/{id}/check-reference

检查患者是否被医案引用（删除前确认）。

- **权限**: `DoctorOrAdminOrReceptionist`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<PatientReferenceCheckDto>`

**业务规则**: `referenceCount` = 关联医案总数；`recentCases` 返回最近 5 条医案摘要；有关联时 `canDelete=false`（提示用禁用替代删除）。

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20001 | 404 | 患者不存在 |

---

### POST /patients/batch-check-reference

批量检查多个患者的引用关系。

- **权限**: `DoctorOrAdminOrReceptionist`

**请求体** (`PatientBatchCheckReferenceInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `patientIds` | Guid[] | 是 | 患者 ID 列表，最多 100 个 |

- **响应**: `ApiResponse<List<PatientReferenceCheckDto>>`

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-20704 | 400 | 批量检查最多支持100条 |

---

## 错误码

> 完整定义见 [patients.md PRD](../02-requirements/04-patients.md)。分区: 2xxxx。

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-20001 | PatientNotFound | 404 | 患者不存在 | GET/PUT/DELETE /{id}, POST /{id}/restore |
| ERR-20002 | PatientIdCardExists | 409 | 身份证号已存在 | POST /, PUT /{id} |
| ERR-20003 | PatientPhoneDuplicate | 409 | 患者电话已存在 | POST /, PUT /{id} |
| ERR-20004 | PatientHasActiveCases | 422 | 患者有历史医案，无法删除 | DELETE /{id}, POST /batch-delete |
| ERR-20005 | PatientDisabled | 403/422 | 患者已被禁用/有进行中医案 | 需启用状态的操作 |
| ERR-00003 | ValidationFailed | 400 | 参数验证失败 | POST /, PUT /{id} |
| ERR-20702 | PatientNotDeleted | 200 | 该患者未被删除 | POST /{id}/restore |
| ERR-20703 | PatientBatchOperationEmpty | 400 | 请至少选择一个患者 | POST /batch-delete |
| ERR-20704 | PatientBatchCheckExceeded | 400 | 批量检查最多支持100条 | POST /batch-check-reference |
| ERR-20705 | PatientInvalidPagination | 400 | 分页参数无效 | GET / |

> 客户端导入错误码 (ERR-20801~20805) 用于前端 Excel 导入流程，非服务端端点触发。
