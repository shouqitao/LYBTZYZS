# 药材 API

> Controller: `HerbsController` | 路由前缀: `/api/v1/herbs` | 默认权限: `[Authorize(Policy = "DoctorOrAdmin")]`

## 概述

药材管理 CRUD、分类筛选、JSON 批量导入、Excel 导出、引用检查、状态切换、批量操作。启用 OutputCache (`HerbsCache`)。
Doctor 只能编辑自己创建的药材，Admin 可操作全部。

> **注意**: `POST /herbs/import` (Excel 导入) 为客户端功能，服务端无此端点。服务端提供 `POST /herbs/batch-import` (JSON 批量导入) 和 `GET /herbs/import-template` (下载模板)。

---

## GET /herbs

获取药材分页列表。启用 OutputCache。

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词 |
| `category` | string? | null | 分类筛选 |

**成功响应** (200): `ApiResponse<PagedResult<HerbListDto>>`

---

## GET /herbs/{id}

获取药材详情。

**路径参数**: `id` (Guid)

**HerbDetailDto**:

```json
{
  "id": "guid",
  "name": "string",
  "pinyin": "string",
  "category": "string",
  "properties": "string",
  "taste": "string",
  "meridians": "string",
  "effects": "string",
  "dosage": "string",
  "contraindications": "string",
  "unitPrice": 0.0,
  "unit": "string",
  "status": "Enabled|Disabled",
  "createdBy": "guid",
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

**错误响应**: 404 (药材不存在)

---

## POST /herbs

创建新药材。

**请求体** (`HerbInputDto`): 含 name, pinyin, category, properties, taste, meridians, effects, dosage, contraindications, unitPrice, unit 等字段。

**成功响应** (200): `ApiResponse<HerbDetailDto>`

---

## PUT /herbs/{id}

更新药材信息。执行所有权检查。

**路径参数**: `id` (Guid)

**请求体**: `HerbInputDto`

**成功响应** (200): `ApiResponse<HerbDetailDto>`

**错误响应**: 403 (权限不足)

---

## DELETE /herbs/{id}

删除药材 (软删除)。执行所有权检查。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<bool>` ("删除成功")

---

## GET /herbs/export

导出药材数据到 Excel。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `category` | string? | 分类筛选 |

- **响应类型**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **文件名**: `药材数据.xlsx`

---

## GET /herbs/import-template

下载药材导入 Excel 模板。

- **权限**: 继承类级别 `[Authorize(Policy = "DoctorOrAdmin")]`
- **响应类型**: Excel 文件
- **文件名**: `药材导入模板.xlsx`

---

## POST /herbs/batch-import

JSON 批量导入药材 (非 Excel，直接 DTO 数组)。

**请求体** (`HerbBatchImportInputDto`):

```json
{
  "herbs": [
    { "name": "...", "pinyin": "...", ... }
  ],
  "strategy": "Skip|Overwrite|Error"    // 重复策略
}
```

**约束**: `herbs` 最多 10000 条。

**成功响应** (200): `ApiResponse<HerbBatchImportResultDto>`

```json
{
  "data": {
    "totalCount": 100,
    "successCount": 95,
    "failureCount": 3,
    "skippedCount": 2
  }
}
```

---

## GET /herbs/export-all

导出全部药材数据 (返回 JSON，Desktop 层负责 Excel 生成)。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `category` | string? | 分类筛选 |

**成功响应** (200): `ApiResponse<List<HerbDetailDto>>`

---

## GET /herbs/{id}/check-reference

检查药材是否被处方引用。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<HerbReferenceCheckDto>`

```json
{
  "data": {
    "herbId": "guid",
    "isReferenced": true,
    "referenceCount": 5,
    "referencedByFormulas": [...],
    "referencedByPrescriptions": [...]
  }
}
```

---

## POST /herbs/batch-check-reference

批量检查药材引用关系。

**请求体** (`HerbBatchCheckReferenceInputDto`):

```json
{
  "herbIds": ["guid1", "guid2", ...]   // 最多 100 个
}
```

**成功响应** (200): `ApiResponse<List<HerbReferenceCheckDto>>`

---

## POST /herbs/{id}/toggle-status

切换药材状态 (启用/禁用)。执行所有权检查。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<HerbDetailDto>` ("药材已启用" 或 "药材已禁用")

---

## POST /herbs/{id}/restore

恢复已删除的药材。绕过软删除全局过滤器。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<HerbDetailDto>` ("药材已恢复")

---

## POST /herbs/batch-enable

批量启用药材。

**请求体**: `BatchDeleteInputDto` (`{ "ids": [...] }`)

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## POST /herbs/batch-disable

批量禁用药材。

**请求体**: `BatchDeleteInputDto`

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## POST /herbs/batch-delete

批量删除药材。

**请求体**: `BatchDeleteInputDto`

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## 错误码

> 完整错误码定义见 [herbs.md PRD](../02-requirements/05-herbs.md)。错误码分区: 5xxxx。

### 核心错误 (501xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-50101 | HerbNotFound | 404 | 药材不存在 | GET/PUT/DELETE /{id}, POST /{id}/restore |
| ERR-50102 | HerbValidationFailed | 400 | 验证失败 | POST /, PUT /{id} |
| ERR-50103 | HerbNoPermission | 403 | 无权限操作此药材 | PUT/DELETE /{id}, POST /{id}/toggle-status |
| ERR-50104 | HerbNotDeleted | 200 | 该药材未被删除 | POST /{id}/restore |
| ERR-50106 | HerbInvalidPagination | 400 | 分页参数无效 | GET / |

### 批量操作错误 (502xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-50201 | HerbBatchEmpty | 400 | 请至少选择一个药材 | POST /batch-delete, POST /batch-enable, POST /batch-disable |
| ERR-50202 | HerbBatchImportExceeded | 400 | 批量导入最多10000条 | POST /batch-import |
| ERR-50203 | HerbBatchCheckExceeded | 400 | 批量检查最多100条 | POST /batch-check-reference |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，17 个端点 |
| 2026-02-18 | v1.1 | 新增错误码章节: 补充端点级 MCCEE 错误码 (ERR-50101~50305)，含核心/批量/导入三类 |
| 2026-06-12 | v1.2 | 标注 POST /herbs/import 为客户端功能; 服务端仅提供 batch-import (JSON) 和 import-template |
