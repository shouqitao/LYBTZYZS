# 患者 API

> Controller: `PatientsController` | 路由前缀: `/api/v1/patients` | 默认权限: `[Authorize(Policy = "DoctorOrAdmin")]`

## 概述

患者管理 CRUD、Excel 导入导出、软删除恢复、批量操作。支持 OutputCache (`PatientsCache`)。
Doctor 只能编辑自己创建的患者，Admin 可操作全部。

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
  "gender": "Male|Female",
  "birthDate": "date",
  "age": 30,                    // 自动计算
  "phoneNumber": "string",
  "idCardNumber": "string",     // 敏感数据
  "address": "string",
  "allergies": "string",
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
  "gender": "Male|Female",
  "birthDate": "date",
  "phoneNumber": "string",
  "idCardNumber": "string",
  "address": "string",
  "allergies": "string",
  "medicalHistory": "string",
  "remark": "string"
}
```

**成功响应** (200): `ApiResponse<PatientDetailDto>`

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

## POST /patients/import

批量导入患者数据 (Excel 文件)。

- **Content-Type**: `multipart/form-data`
- **文件限制**: 仅 `.xlsx`，最大 10MB

**请求**: `IFormFile file`

**成功响应** (200): `ApiResponse<PatientBatchImportResultDto>`

```json
{
  "data": {
    "successCount": 50,
    "failureCount": 2,
    "skippedCount": 1,
    "errors": [...]
  }
}
```

---

## GET /patients/import-template

下载患者导入 Excel 模板。包含 3 行示例数据。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **响应类型**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者导入模板_yyyyMMdd.xlsx`

---

## GET /patients/export

导出患者数据到 Excel。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `keyword` | string? | 筛选条件 |

- **响应类型**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `患者数据_yyyyMMdd_HHmmss.xlsx`

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

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，10 个端点 |
