# 患者 API

> Controller: `PatientsController` | 路由前缀: `/api/v1/patients` | 默认权限: `[Authorize(Policy = "PatientAccess")]`

## 概述

患者管理 CRUD、Excel 导出、软删除恢复、批量操作、引用检查。支持 OutputCache (`PatientsCache`)。
Doctor 只能编辑自己创建的患者，Admin 可操作全部。

> **注意**: 患者 Excel 导入在客户端 (Desktop) 完成，服务端无 `POST /patients/import` 端点。服务端仅提供 `GET /patients/import-template` 下载模板。

---

## GET /patients

获取患者列表 (分页)。启用 OutputCache。

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词 (姓名/拼音码) |

**成功响应** (200): `ApiResponse<PagedResult<PatientListDto>>`

---

## GET /patients/{id}

获取患者详情。返回 `PatientDetailDto`，含年龄自动计算。

**路径参数**: `id` (Guid)

**PatientDetailDto**:

```json
{
  "id": "guid",
  "name": "string",
  "gender": "Unknown|Male|Female",
  "birthDate": "date",
  "age": 30,                    // 自动计算
  "phoneNumber": "string",
  "idNumber": "string",         // 敏感数据
  "address": "string",
  "allergyHistory": "string",
  "medicalHistory": "string",
  "remark": "string",
  "createdBy": "guid",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

**错误响应**: 404 (患者不存在)

---

## POST /patients

新增患者。

**请求体** (`PatientInputDto`):

```json
{
  "name": "string",
  "gender": "Unknown|Male|Female",
  "birthDate": "date",
  "phoneNumber": "string",
  "idNumber": "string",
  "address": "string",
  "allergyHistory": "string",
  "medicalHistory": "string",
  "remark": "string"
}
```

**成功响应** (201 Created): `ApiResponse<PatientDetailDto>`

---

## PUT /patients/{id}

更新患者信息。执行所有权检查 (Doctor 只能改自己的)。

**路径参数**: `id` (Guid)

**请求体**: `PatientInputDto` (同创建)

**成功响应** (200): `ApiResponse<PatientDetailDto>`

**错误响应**:
- 403: 权限不足 (非所有者非管理员)
- 404: 患者不存在

---

## DELETE /patients/{id}

删除患者 (软删除)。执行所有权检查。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<bool>` (true)

---

## GET /patients/import-template

下载患者导入 Excel 模板。包含 5 行示例数据。

- **权限**: 继承类级别 `[Authorize(Policy = "PatientAccess")]`

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `sampleRowCount` | int | 5 | 示例数据行数 |

- **响应类型**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者导入模板.xlsx`

---

## GET /patients/export

导出患者数据到 Excel。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `keyword` | string? | 筛选条件 |

- **响应类型**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者数据.xlsx`

---

## POST /patients/{id}/restore

恢复已删除的患者。绕过软删除全局过滤器。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<PatientDetailDto>` ("患者已恢复")

**注意**: 此端点不使用 `GetEntityWithOwnershipCheckAsync`，因为 `GetByIdAsync` 受全局软删除过滤器影响。`RestoreAsync` 内部使用 `GetByIdIncludingDeletedAsync` 绕过过滤器。

---

## POST /patients/batch-delete

批量删除患者。

**请求体** (`BatchDeleteInputDto`):

```json
{
  "ids": ["guid1", "guid2", ...]
}
```

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## POST /patients/{id}/toggle-status

切换患者状态 (启用/禁用)。无请求体，自动在 Enabled/Disabled 间切换。

> **US-PAT-013**: PRD 原文为 `PUT /{id}/status` (带 body 指定状态)，但代码实现为 `POST /{id}/toggle-status` (自动切换，无 body)。文档以代码为准。

**路径参数**: `id` (Guid)

**业务规则**:
1. 仅 Admin/SuperAdmin 可执行状态切换
2. 禁用时: 检查患者是否有 Draft/Active 医案，有则拒绝 (需先完成或取消)
3. 禁用后: 禁止为该患者创建新医案 (见 medical-cases.md ERR-30105)
4. 禁用后: 历史医案可查阅，PatientName 按角色脱敏 (Admin 完整/Doctor 掩码如 "张*")
5. 启用后: 所有限制解除，脱敏自动取消
6. v1.0 主要禁用场景: 患者已故 (PAT-D05)

**成功响应** (200): `ApiResponse<PatientDetailDto>`

响应 message 示例: "患者已禁用" 或 "患者已启用"

**错误响应**:
- 403: 权限不足 (非 Admin)
- 404: 患者不存在 (ERR-20001)
- 422: 患者有进行中的医案 (ERR-20005)

> **交叉引用**: 禁用联动规则见 [medical-cases.md](06-medical-cases.md) MC-D16; 查询可见性见 patients PRD FR-PAT-002 规则 5 (Receptionist 不可见禁用患者)

---

## GET /patients/{id}/check-reference

检查患者是否被医案引用，用于删除前确认。

> 对应 [FR-PAT-011](../02-requirements/04-patients.md)。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<PatientReferenceCheckDto>`

```json
{
  "data": {
    "patientId": "guid",
    "isReferenced": true,
    "referenceCount": 3,
    "canDelete": false,
    "recentCases": [
      {
        "caseId": "guid",
        "caseNumber": "MC-20260218-001",
        "status": "Completed",
        "doctorName": "张医生",
        "createdAt": "2026-02-18T10:00:00Z"
      }
    ]
  }
}
```

**业务规则**:
1. referenceCount = 该患者关联的医案总数 (含所有状态)
2. recentCases 返回最近 5 条医案摘要
3. 有关联医案时 canDelete=false (MC-D04)，提示使用禁用功能替代删除

**错误响应**:
- 404: 患者不存在 (ERR-20001)

---

## POST /patients/batch-check-reference

批量检查多个患者的引用关系。

> 对应 [FR-PAT-012](../02-requirements/04-patients.md)。

**请求体** (`PatientBatchCheckReferenceInputDto`):

```json
{
  "patientIds": ["guid1", "guid2", ...]   // 最多 100 个
}
```

**成功响应** (200): `ApiResponse<List<PatientReferenceCheckDto>>`

```json
{
  "data": [
    {
      "patientId": "guid1",
      "isReferenced": true,
      "referenceCount": 3,
      "canDelete": false,
      "recentCases": [...]
    },
    {
      "patientId": "guid2",
      "isReferenced": false,
      "referenceCount": 0,
      "canDelete": true,
      "recentCases": []
    }
  ]
}
```

**业务规则**:
1. 最多 100 个患者 ID (超出返回 ERR-20704)
2. 不存在的 ID 跳过 (不返回错误)
3. 结果顺序与请求顺序一致

**错误响应**:
- 400: 批量检查超限 (ERR-20704)

---

## 错误码

> 完整错误码定义见 [patients.md PRD](../02-requirements/04-patients.md)。错误码分区: 2xxxx。

### 核心错误 (200xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-20001 | PatientNotFound | 404 | 患者不存在 | GET/PUT/DELETE /{id}, POST /{id}/restore |
| ERR-20002 | PatientIdCardExists | 409 | 系统中已存在该身份证 | POST /, PUT /{id} |
| ERR-20003 | PatientPhoneExists | 409 | 患者电话已存在 | POST /, PUT /{id} |
| ERR-20004 | PatientHasReferencedCases | 422 | 该患者有历史医案，无法删除 | DELETE /{id}, POST /batch-delete |
| ERR-20005 | PatientDisabled | 403 | 患者已被禁用 | 需启用状态的操作 |
| ERR-20006 | InvalidPatientStatus | 400 | 无效的患者状态 | PUT /{id}/status |
| ERR-00003 | ValidationFailed | 400 | 参数验证失败 | POST /, PUT /{id} |

### 业务规则错误 (207xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-20701 | PhoneDuplicate | 400 | 手机号已存在 | POST /, PUT /{id} |
| ERR-20702 | PatientNotDeleted | 200 | 该患者未被删除 | POST /{id}/restore |
| ERR-20703 | BatchOperationEmpty | 400 | 请至少选择一个患者 | POST /batch-delete |
| ERR-20704 | BatchCheckExceeded | 400 | 批量检查最多支持100条 | POST /batch-check-reference |
| ERR-20705 | InvalidPagination | 400 | 分页参数无效 | GET / |

### 导入错误 (208xx)

> 以下错误码用于客户端 Excel 导入流程，非服务端端点触发。

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-20801 | ImportFileEmpty | 400 | 文件不能为空 | 客户端导入 |
| ERR-20802 | ImportFileFormat | 400 | 仅支持.xlsx格式 | 客户端导入 |
| ERR-20803 | ImportFileSize | 400 | 文件大小不能超过10MB | 客户端导入 |
| ERR-20804 | ImportNoWorksheet | 400 | 没有工作表 | 客户端导入 |
| ERR-20805 | ImportRowExceeded | 400 | 导入数据超过限制 | 客户端导入 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，10 个端点 |
| 2026-02-18 | v1.1 | 新增 PUT /patients/{id}/status 端点 (FR-PAT-013 患者状态管理); 补充错误码 ERR-20005/20006 |
| 2026-02-18 | v1.2 | 新增错误码章节: 补充端点级 MCCEE 错误码 (ERR-20001~20805)，含核心/业务规则/导入三类 |
| 2026-06-12 | v1.4 | 移除 POST /patients/import (客户端功能); US-PAT-013 改为 toggle-status; 导入错误码标注为客户端触发 |
