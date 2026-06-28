# 患者 API

> Controller: `PatientsController` | 路由前缀: `/api/v1/patients` | 默认权限: `[Authorize(Policy = "DoctorOrReceptionist")]`
>
> ⚠️ **权限待对齐（D7，基线§3）**：文档目标策略为 `DoctorOrReceptionist`；代码当前为 `DoctorOrAdmin`（`PatientsController.cs:23`），待对齐。

## 概述

患者管理 CRUD、Excel 导出/导入模板、软删除恢复、批量操作、引用检查。支持 OutputCache (`PatientsCache`)。
Doctor 只能编辑自己创建的患者，Admin 可操作全部。

> **注意**: 患者 Excel 导入在客户端 (Desktop) 完成，服务端无 `POST /patients/import` 端点。服务端仅提供 `GET /patients/import-template` 下载模板。

---

## GET /patients

获取患者列表 (分页)。启用 OutputCache。

- **权限**: `DoctorOrReceptionist`（Non-Admin 用户仅可见 Enabled 患者）

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词 (姓名/拼音码) |

**成功响应** (200): `ApiResponse<PagedResult<PatientListDto>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "items": [
      {
        "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "name": "张三",
        "gender": "Male",
        "age": 35,
        "phoneNumber": "13800138000",
        "pinYinCode": "ZS",
        "status": "Enabled",
        "createdAt": "2026-01-15T08:30:00Z"
      },
      {
        "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
        "name": "李四",
        "gender": "Female",
        "age": 28,
        "phoneNumber": "13900139000",
        "pinYinCode": "LS",
        "status": "Enabled",
        "createdAt": "2026-02-20T10:00:00Z"
      }
    ],
    "totalCount": 156,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B3C"
}
```

**curl 示例：**

```bash
# 获取患者列表
curl -X GET "http://localhost:5000/api/v1/patients?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"

# 按关键词搜索
curl -X GET "http://localhost:5000/api/v1/patients?keyword=张&page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 分页参数无效 (ERR-20705) |
| 401 | 未认证 |

---

## GET /patients/{id}

获取患者详情。返回 `PatientDetailDto`，含年龄自动计算。

- **权限**: `DoctorOrReceptionist`

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<PatientDetailDto>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "张三",
    "gender": "Male",
    "birthDate": "1991-03-15",
    "age": 35,
    "phoneNumber": "13800138000",
    "idNumber": "110101199103150012",
    "address": "北京市东城区王府井大街1号",
    "maritalStatus": "Married",
    "idType": "IdCard",
    "bloodType": "A",
    "emergencyContactName": "张四",
    "emergencyContactPhone": "13700137000",
    "emergencyContactRelation": "配偶",
    "allergyHistory": "青霉素过敏",
    "medicalHistory": "高血压病史5年",
    "lastVisitTime": "2026-06-20T14:30:00Z",
    "visitCount": 12,
    "disableReason": null,
    "pinYinCode": "ZS",
    "status": "Enabled",
    "remark": "老患者，定期复诊",
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-01-15T08:30:00Z",
    "updatedAt": "2026-06-20T14:30:00Z"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B3D"
}
```

**错误响应** (404):

```json
{
  "success": false,
  "message": "患者不存在",
  "data": null,
  "errors": {
    "code": "ERR-20001"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B3E"
}
```

**curl 示例：**

```bash
curl -X GET "http://localhost:5000/api/v1/patients/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | 未认证 |
| 404 | 患者不存在 (ERR-20001) |

---

## POST /patients

新增患者。

- **权限**: `DoctorOrReceptionist`

**请求体** (`PatientInputDto`):

```json
{
  "name": "王五",
  "gender": "Male",
  "birthDate": "1988-07-20",
  "phoneNumber": "13600136000",
  "idNumber": "110101198807200034",
  "address": "北京市朝阳区建国路88号",
  "allergyHistory": null,
  "medicalHistory": null,
  "remark": "初诊患者",
  "pinYinCode": "WW"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 是 | 患者姓名 |
| `gender` | enum | 否 | Male/Female/Unknown，默认 Unknown |
| `birthDate` | date | 否 | 出生日期 |
| `phoneNumber` | string | 否 | 联系电话 |
| `idNumber` | string | 否 | 身份证号 |
| `address` | string | 否 | 地址 |
| `allergyHistory` | string | 否 | 过敏史 |
| `medicalHistory` | string | 否 | 病史 |
| `remark` | string | 否 | 备注 |
| `pinYinCode` | string | 否 | 拼音码 |

**成功响应** (201 Created): `ApiResponse<PatientDetailDto>`

```json
{
  "success": true,
  "message": "创建成功",
  "data": {
    "id": "d4e5f6a7-b8c9-0123-def4-567890abcdef",
    "name": "王五",
    "gender": "Male",
    "birthDate": "1988-07-20",
    "age": 37,
    "phoneNumber": "13600136000",
    "idNumber": "110101198807200034",
    "address": "北京市朝阳区建国路88号",
    "maritalStatus": "Unknown",
    "idType": "IdCard",
    "bloodType": "Unknown",
    "emergencyContactName": null,
    "emergencyContactPhone": null,
    "emergencyContactRelation": null,
    "allergyHistory": null,
    "medicalHistory": null,
    "lastVisitTime": null,
    "visitCount": 0,
    "disableReason": null,
    "pinYinCode": "WW",
    "status": "Enabled",
    "remark": "初诊患者",
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-06-25T10:00:00Z",
    "updatedAt": null
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B3F"
}
```

**错误响应** (409 — 身份证重复):

```json
{
  "success": false,
  "message": "系统中已存在该身份证",
  "data": null,
  "errors": {
    "code": "ERR-20002"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B40"
}
```

**错误响应** (400 — 验证失败):

```json
{
  "success": false,
  "message": "参数验证失败",
  "data": null,
  "errors": {
    "code": "ERR-00003",
    "details": {
      "name": ["姓名不能为空"]
    }
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B41"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/patients" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "王五",
    "gender": "Male",
    "birthDate": "1988-07-20",
    "phoneNumber": "13600136000",
    "idNumber": "110101198807200034",
    "address": "北京市朝阳区建国路88号",
    "remark": "初诊患者",
    "pinYinCode": "WW"
  }'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 参数验证失败 (ERR-00003) |
| 401 | 未认证 |
| 409 | 身份证号已存在 (ERR-20002) |
| 409 | 患者电话已存在 (ERR-20003) |

---

## PUT /patients/{id}

更新患者信息。执行所有权检查 (Doctor 只能改自己的)。

- **权限**: `DoctorOrReceptionist`（Doctor 仅限自己创建的患者）

**路径参数**: `id` (Guid)

**请求体**: `PatientInputDto` (同创建)

```json
{
  "name": "张三",
  "gender": "Male",
  "birthDate": "1991-03-15",
  "phoneNumber": "13800138000",
  "idNumber": "110101199103150012",
  "address": "北京市东城区王府井大街1号（已搬迁）",
  "allergyHistory": "青霉素过敏",
  "medicalHistory": "高血压病史5年，糖尿病病史2年",
  "remark": "复诊记录已更新",
  "pinYinCode": "ZS"
}
```

**成功响应** (200): `ApiResponse<PatientDetailDto>` — 返回更新后的完整患者详情。

**错误响应** (403):

```json
{
  "success": false,
  "message": "权限不足",
  "data": null,
  "errors": {
    "code": "ERR-20001"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B42"
}
```

**curl 示例：**

```bash
curl -X PUT "http://localhost:5000/api/v1/patients/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "张三",
    "gender": "Male",
    "birthDate": "1991-03-15",
    "phoneNumber": "13800138000",
    "idNumber": "110101199103150012",
    "address": "北京市东城区王府井大街1号（已搬迁）",
    "allergyHistory": "青霉素过敏",
    "medicalHistory": "高血压病史5年，糖尿病病史2年",
    "remark": "复诊记录已更新",
    "pinYinCode": "ZS"
  }'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 参数验证失败 (ERR-00003) |
| 401 | 未认证 |
| 403 | 权限不足 (Doctor 非所有者) |
| 404 | 患者不存在 (ERR-20001) |
| 409 | 身份证号已存在 (ERR-20002) |
| 409 | 患者电话已存在 (ERR-20003) |

---

## DELETE /patients/{id}

删除患者 (软删除)。执行所有权检查。

- **权限**: `DoctorOrReceptionist`（Doctor 仅限自己创建的患者）

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<bool>`

```json
{
  "success": true,
  "message": "删除成功",
  "data": true,
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B43"
}
```

**错误响应** (422 — 有关联医案):

```json
{
  "success": false,
  "message": "该患者有历史医案，无法删除",
  "data": null,
  "errors": {
    "code": "ERR-20004"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B44"
}
```

**curl 示例：**

```bash
curl -X DELETE "http://localhost:5000/api/v1/patients/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | 未认证 |
| 403 | 权限不足 (Doctor 非所有者) |
| 404 | 患者不存在 (ERR-20001) |
| 422 | 患者有历史医案，无法删除 (ERR-20004) |

---

## POST /patients/{id}/toggle-status

切换患者状态 (启用/禁用)。无请求体，自动在 Enabled/Disabled 间切换。

- **权限**: `AdminOrSuperAdmin`（仅 Admin 可执行）

> **US-PAT-013**: PRD 原文为 `PUT /{id}/status` (带 body 指定状态)，但代码实现为 `POST /{id}/toggle-status` (自动切换，无 body)。文档以代码为准。

**路径参数**: `id` (Guid)

**请求体**: 无

**业务规则**:
1. 仅 Admin/SuperAdmin 可执行状态切换
2. 禁用时: 检查患者是否有 Draft/Active 医案，有则拒绝 (需先完成或取消)
3. 禁用后: 禁止为该患者创建新医案 (见 medical-cases.md ERR-30105)
4. 禁用后: 历史医案可查阅，PatientName 按角色脱敏 (Admin 完整/Doctor 掩码如 "张*")
5. 启用后: 所有限制解除，脱敏自动取消
6. v1.0 主要禁用场景: 患者已故 (PAT-D05)

**成功响应** (200): `ApiResponse<PatientDetailDto>`

```json
{
  "success": true,
  "message": "患者已禁用",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "张三",
    "gender": "Male",
    "birthDate": "1991-03-15",
    "age": 35,
    "phoneNumber": "13800138000",
    "idNumber": "110101199103150012",
    "address": "北京市东城区王府井大街1号",
    "maritalStatus": "Married",
    "idType": "IdCard",
    "bloodType": "A",
    "emergencyContactName": "张四",
    "emergencyContactPhone": "13700137000",
    "emergencyContactRelation": "配偶",
    "allergyHistory": "青霉素过敏",
    "medicalHistory": "高血压病史5年",
    "lastVisitTime": "2026-06-20T14:30:00Z",
    "visitCount": 12,
    "disableReason": null,
    "pinYinCode": "ZS",
    "status": "Disabled",
    "remark": "老患者，定期复诊",
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-01-15T08:30:00Z",
    "updatedAt": "2026-06-25T10:00:00Z"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B45"
}
```

**错误响应** (403):

```json
{
  "success": false,
  "message": "权限不足",
  "data": null,
  "errors": {
    "code": "ERR-20005"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B46"
}
```

**错误响应** (422 — 有进行中医案):

```json
{
  "success": false,
  "message": "患者有进行中的医案，无法禁用",
  "data": null,
  "errors": {
    "code": "ERR-20005"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B47"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/patients/a1b2c3d4-e5f6-7890-abcd-ef1234567890/toggle-status" \
  -H "Authorization: Bearer $TOKEN"
```

> **交叉引用**: 禁用联动规则见 [medical-cases.md](06-medical-cases.md) MC-D16; 查询可见性见 patients PRD FR-PAT-002 规则 5 (Receptionist 不可见禁用患者)

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | 未认证 |
| 403 | 权限不足 (非 Admin) |
| 404 | 患者不存在 (ERR-20001) |
| 422 | 患者有进行中的医案 (ERR-20005) |

---

## POST /patients/batch-delete

批量删除患者。

- **权限**: `DoctorOrReceptionist`

**请求体** (`BatchDeleteInputDto`):

```json
{
  "ids": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901"
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 患者 ID 列表，不能为空 |

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

```json
{
  "success": true,
  "message": "批量删除完成",
  "data": {
    "totalCount": 2,
    "successCount": 2,
    "failureCount": 0,
    "errors": []
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B48"
}
```

**错误响应** (400 — 空列表):

```json
{
  "success": false,
  "message": "请至少选择一个患者",
  "data": null,
  "errors": {
    "code": "ERR-20703"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B49"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/patients/batch-delete" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ids": [
      "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "b2c3d4e5-f6a7-8901-bcde-f12345678901"
    ]
  }'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 请至少选择一个患者 (ERR-20703) |
| 401 | 未认证 |

---

## GET /patients/import-template

> 🚧 **v2.0 规划**（基线§1 D6：Patients 导入标 v2.0；Herbs Excel 导入属 v1.0）。

下载患者导入 Excel 模板。包含示例数据。

- **权限**: `DoctorOrReceptionist`

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `sampleRowCount` | int | 5 | 示例数据行数 |

- **响应类型**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者导入模板.xlsx`

**curl 示例：**

```bash
# 下载导入模板
curl -X GET "http://localhost:5000/api/v1/patients/import-template?sampleRowCount=5" \
  -H "Authorization: Bearer $TOKEN" \
  --output "患者导入模板.xlsx"

# 自定义示例行数
curl -X GET "http://localhost:5000/api/v1/patients/import-template?sampleRowCount=10" \
  -H "Authorization: Bearer $TOKEN" \
  --output "患者导入模板.xlsx"
```

---

## GET /patients/export

> 🚧 **v2.0 规划**（基线§1 D6：Patients 导入/导出标 v2.0）。

导出患者数据到 Excel。

- **权限**: `DoctorOrReceptionist`

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `keyword` | string? | 筛选条件 |

- **响应类型**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者数据.xlsx`

**curl 示例：**

```bash
# 导出全部患者
curl -X GET "http://localhost:5000/api/v1/patients/export" \
  -H "Authorization: Bearer $TOKEN" \
  --output "患者数据.xlsx"

# 按关键词导出
curl -X GET "http://localhost:5000/api/v1/patients/export?keyword=张" \
  -H "Authorization: Bearer $TOKEN" \
  --output "患者数据_张.xlsx"
```

---

## POST /patients/{id}/restore

> 🚧 **v1.0 待实现（D4 Restore 软删除恢复补回，基线§1）**：基础设施已就绪（`IgnoreQueryFilters`），端点待补。

恢复已删除的患者。绕过软删除全局过滤器。

- **权限**: `DoctorOrReceptionist`

**路径参数**: `id` (Guid)

**请求体**: 无

**成功响应** (200): `ApiResponse<PatientDetailDto>`

```json
{
  "success": true,
  "message": "患者已恢复",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "张三",
    "gender": "Male",
    "birthDate": "1991-03-15",
    "age": 35,
    "phoneNumber": "13800138000",
    "idNumber": "110101199103150012",
    "address": "北京市东城区王府井大街1号",
    "maritalStatus": "Married",
    "idType": "IdCard",
    "bloodType": "A",
    "emergencyContactName": "张四",
    "emergencyContactPhone": "13700137000",
    "emergencyContactRelation": "配偶",
    "allergyHistory": "青霉素过敏",
    "medicalHistory": "高血压病史5年",
    "lastVisitTime": "2026-06-20T14:30:00Z",
    "visitCount": 12,
    "disableReason": null,
    "pinYinCode": "ZS",
    "status": "Enabled",
    "remark": "老患者，定期复诊",
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-01-15T08:30:00Z",
    "updatedAt": "2026-06-25T10:00:00Z"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B4A"
}
```

**错误响应** (200 — 未被删除):

```json
{
  "success": false,
  "message": "该患者未被删除",
  "data": null,
  "errors": {
    "code": "ERR-20702"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B4B"
}
```

**注意**: 此端点不使用 `GetEntityWithOwnershipCheckAsync`，因为 `GetByIdAsync` 受全局软删除过滤器影响。`RestoreAsync` 内部使用 `GetByIdIncludingDeletedAsync` 绕过过滤器。

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/patients/a1b2c3d4-e5f6-7890-abcd-ef1234567890/restore" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 200 | 该患者未被删除 (ERR-20702) |
| 401 | 未认证 |
| 404 | 患者不存在 (ERR-20001) |

---

## GET /patients/{id}/check-reference

> 🚧 **v1.0 待实现（D5 引用检查 BR-DEL-001 必做补回，基线§1）**。

检查患者是否被医案引用，用于删除前确认。

- **权限**: `DoctorOrReceptionist`
- 对应 [FR-PAT-011](../02-requirements/04-patients.md)。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<PatientReferenceCheckDto>`

```json
{
  "success": true,
  "message": "检查完成",
  "data": {
    "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "isReferenced": true,
    "referenceCount": 3,
    "canDelete": false,
    "recentCases": [
      {
        "caseId": "e5f6a7b8-c9d0-1234-ef56-7890abcdef01",
        "caseNumber": "MC-20260218-001",
        "status": "Completed",
        "doctorName": "张医生",
        "createdAt": "2026-02-18T10:00:00Z"
      },
      {
        "caseId": "f6a7b8c9-d0e1-2345-f067-890abcdef012",
        "caseNumber": "MC-20260305-003",
        "status": "Active",
        "doctorName": "李医生",
        "createdAt": "2026-03-05T14:00:00Z"
      },
      {
        "caseId": "a7b8c9d0-e1f2-3456-0178-90abcdef0123",
        "caseNumber": "MC-20260410-007",
        "status": "Draft",
        "doctorName": "张医生",
        "createdAt": "2026-04-10T09:00:00Z"
      }
    ]
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B4C"
}
```

**业务规则**:
1. referenceCount = 该患者关联的医案总数 (含所有状态)
2. recentCases 返回最近 5 条医案摘要
3. 有关联医案时 canDelete=false (MC-D04)，提示使用禁用功能替代删除

**curl 示例：**

```bash
curl -X GET "http://localhost:5000/api/v1/patients/a1b2c3d4-e5f6-7890-abcd-ef1234567890/check-reference" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | 未认证 |
| 404 | 患者不存在 (ERR-20001) |

---

## POST /patients/batch-check-reference

> 🚧 **v1.0 待实现（D5 引用检查必做补回，基线§1）**。

批量检查多个患者的引用关系。

- **权限**: `DoctorOrReceptionist`
- 对应 [FR-PAT-012](../02-requirements/04-patients.md)。

**请求体** (`PatientBatchCheckReferenceInputDto`):

```json
{
  "patientIds": [
    "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "c3d4e5f6-a7b8-9012-cdef-123456789012"
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `patientIds` | Guid[] | 是 | 患者 ID 列表，最多 100 个 |

**成功响应** (200): `ApiResponse<List<PatientReferenceCheckDto>>`

```json
{
  "success": true,
  "message": "检查完成",
  "data": [
    {
      "patientId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "isReferenced": true,
      "referenceCount": 3,
      "canDelete": false,
      "recentCases": [
        {
          "caseId": "e5f6a7b8-c9d0-1234-ef56-7890abcdef01",
          "caseNumber": "MC-20260218-001",
          "status": "Completed",
          "doctorName": "张医生",
          "createdAt": "2026-02-18T10:00:00Z"
        }
      ]
    },
    {
      "patientId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "isReferenced": false,
      "referenceCount": 0,
      "canDelete": true,
      "recentCases": []
    },
    {
      "patientId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "isReferenced": true,
      "referenceCount": 1,
      "canDelete": false,
      "recentCases": [
        {
          "caseId": "d4e5f6a7-b8c9-0123-def4-567890abcdef",
          "caseNumber": "MC-20260501-012",
          "status": "Active",
          "doctorName": "李医生",
          "createdAt": "2026-05-01T11:00:00Z"
        }
      ]
    }
  ],
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B4D"
}
```

**业务规则**:
1. 最多 100 个患者 ID (超出返回 ERR-20704)
2. 不存在的 ID 跳过 (不返回错误)
3. 结果顺序与请求顺序一致

**错误响应** (400 — 超限):

```json
{
  "success": false,
  "message": "批量检查最多支持100条",
  "data": null,
  "errors": {
    "code": "ERR-20704"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B4E"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/patients/batch-check-reference" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "patientIds": [
      "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "c3d4e5f6-a7b8-9012-cdef-123456789012"
    ]
  }'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 批量检查最多支持100条 (ERR-20704) |
| 401 | 未认证 |

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
| 2026-06-12 | v1.5 | PatientDetailDto: 新增 maritalStatus/idType/bloodType/emergencyContact*/lastVisitTime/visitCount/disableReason/pinYinCode/status 字段 |
| 2026-06-25 | v2.0 | 补充全部 12 个端点的完整请求/响应 JSON 示例、curl 命令、错误码表；修正响应字段与源码一致 |
| 2026-06-28 | v2.1 | 文档对齐基线：权限策略加 D7 待对齐标注；import-template/export 标 v2.0（D6）；restore 标 D4 待实现；check-reference/batch-check-reference 标 D5 必做补回 |
