# 药材 API

> Controller: `HerbsController` | 路由前缀: `/api/v1/herbs` | 默认权限: `DoctorOrAdmin`
>
> ⚠️ **权限详见** [04-permissions.md](../01-product/04-permissions.md) §药材管理。代码已按此实现（类级 `DoctorOrAdmin` + 写操作 `AdminOrSuperAdmin`），前台不可查已落地。

## 概述

药材管理 CRUD、分类筛选、JSON 批量导入、状态切换、批量操作。启用 OutputCache。
Doctor 只能编辑自己创建的药材，Admin 可操作全部。导入/导出为 JSON 格式（Remote 与 LocalWebAPI 均提供）。

---

## 端点列表

### GET /herbs

获取药材分页列表。

- **权限**: `Doctor/Admin`（前台不可查）

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词 (名称/拼音码) |
| `category` | string? | null | 分类筛选 |

**响应**: `ApiResponse<PagedResult<HerbListDto>>`

```json
{
  "items": [
    { "id": "...", "name": "黄芪", "pinYinCode": "HQ", "category": "补气药", "origin": "...", "spec": "统货", "unit": "克", "price": 28.50, "status": "Enabled", "createdAt": "..." },
    { "id": "...", "name": "当归", "pinYinCode": "DG", "category": "补血药", "price": 45.00, "status": "Enabled", "createdAt": "..." }
  ],
  "totalCount": 230, "page": 1, "pageSize": 20, "totalPages": 12
}
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50106 | 400 | 分页参数无效 |

---

### GET /herbs/{id}

获取药材详情。

- **权限**: `Doctor/Admin`（前台不可查）
- **路径参数**: `id` (Guid)

**响应**: `ApiResponse<HerbDetailDto>`

```json
{
  "id": "...", "name": "黄芪", "pinYinCode": "HQ", "category": "补气药",
  "properties": "甘，微温。归脾、肺经。", "effect": "补气升阳，固表止汗...",
  "origin": "蒙古黄芪或膜荚黄芪的干燥根", "spec": "统货",
  "costPrice": 18.00, "price": 28.50, "unit": "克", "usage": "9～30g",
  "remark": "蜜炙增强补中益气作用", "status": "Enabled",
  "createdBy": "...", "createdAt": "...", "updatedAt": "..."
}
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50101 | 404 | 药材不存在 |

---

### POST /herbs

创建新药材。

- **权限**: `Admin+`

**请求体** (`HerbInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 是 | 药材名称，最大 100 字符 |
| `pinYinCode` | string | 否 | 拼音码 |
| `category` | string | 否 | 分类 |
| `properties` | string | 否 | 性味归经 |
| `origin` | string | 否 | 产地 |
| `spec` | string | 否 | 规格 |
| `unit` | string | 否 | 单位，默认 "克" |
| `price` | decimal | 是 | 售价，0-999999.99 |
| `costPrice` | decimal? | 否 | 成本价 |
| `effect` | string | 否 | 功效 |
| `usage` | string | 否 | 用法用量 |
| `remark` | string | 否 | 备注 |

**响应** (201): `ApiResponse<HerbDetailDto>` — 同 GET /herbs/{id}

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50102 | 400 | 验证失败 |

---

### PUT /herbs/{id}

更新药材信息。执行所有权检查。

- **权限**: `Admin+`
- **路径参数**: `id` (Guid)
- **请求体**: `HerbInputDto`（同 POST）
- **响应**: `ApiResponse<HerbDetailDto>` — 返回更新后详情

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50102 | 400 | 验证失败 |
| ERR-50103 | 403 | 无权限操作此药材 |
| ERR-50101 | 404 | 药材不存在 |

---

### DELETE /herbs/{id}

软删除药材。执行所有权检查。

- **权限**: `Admin+`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<bool>` → `true`

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50103 | 403 | 无权限操作此药材 |
| ERR-50101 | 404 | 药材不存在 |

---

### POST /herbs/{id}/toggle-status

切换药材状态（启用/禁用），无请求体。执行所有权检查。

- **权限**: `Admin+`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<HerbDetailDto>` — 返回切换后详情

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50103 | 403 | 无权限操作此药材 |
| ERR-50101 | 404 | 药材不存在 |

---

### POST /herbs/batch-delete

批量软删除药材。

- **权限**: `Admin+`

**请求体** (`BatchDeleteInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 药材 ID 列表，不能为空 |

**响应**: `ApiResponse<BatchOperationResultDto>`

```json
{ "totalCount": 2, "successCount": 2, "failureCount": 0, "errors": [] }
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50201 | 400 | 请至少选择一个药材 |

---

### POST /herbs/batch-import

JSON 批量导入药材（直接 DTO 数组，非 Excel）。

- **权限**: `Admin+`

**请求体** (`HerbBatchImportInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `herbs` | HerbInputDto[] | 是 | 药材列表，最多 10000 条 |
| `strategy` | enum | 是 | 重复策略: `Skip` / `Overwrite` / `Error` |

```json
{
  "herbs": [
    { "name": "黄芪", "pinYinCode": "HQ", "category": "补气药", "price": 28.50, "costPrice": 18.00, ... },
    { "name": "当归", "pinYinCode": "DG", "category": "补血药", "price": 45.00, "costPrice": 30.00, ... }
  ],
  "strategy": "Skip"
}
```

**响应**: `ApiResponse<HerbBatchImportResultDto>`

```json
{ "totalCount": 2, "successCount": 2, "failureCount": 0, "skippedCount": 0 }
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50102 | 400 | 验证失败 |
| ERR-50202 | 400 | 批量导入最多10000条 |

---

### POST /herbs/{id}/restore

恢复已软删除的药材（绕过软删除全局过滤器）。

- **权限**: `Admin`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<HerbDetailDto>`

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-50104 | 200 | 该药材未被删除 |
| ERR-50101 | 404 | 药材不存在 |

---

### GET /herbs/{id}/check-reference

检查药材是否被处方/验方引用（删除前确认）。

- **权限**: `Doctor/Admin`（前台不可查）
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<HerbReferenceCheckDto>`

---

### POST /herbs/batch-check-reference

批量检查多个药材的引用关系。

- **权限**: `Doctor/Admin`（前台不可查）

**请求体**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `herbIds` | Guid[] | 是 | 药材 ID 列表 |

- **响应**: `ApiResponse<List<HerbReferenceCheckDto>>`

---

### POST /herbs/batch-enable / batch-disable

批量启用或禁用药材。

- **权限**: `AdminOrSuperAdmin`

**请求体** (两个端点相同):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 药材 ID 列表 |

- **响应**: `ApiResponse<BatchOperationResultDto>`

---

## 错误码汇总

> 完整定义见 [herbs.md PRD](../02-requirements/05-herbs.md)。分区: 5xxxx。

| 错误码 | HTTP | 用户消息 | 触发端点 |
|--------|------|----------|----------|
| ERR-50101 | 404 | 药材不存在 | GET/PUT/DELETE /{id} |
| ERR-50102 | 400 | 验证失败 | POST /, PUT /{id}, batch-import |
| ERR-50103 | 403 | 无权限操作此药材 | PUT/DELETE /{id}, toggle-status |
| ERR-50104 | 200 | 该药材未被删除 | POST /{id}/restore |
| ERR-50106 | 400 | 分页参数无效 | GET / |
| ERR-50201 | 400 | 请至少选择一个药材 | POST /batch-delete |
| ERR-50202 | 400 | 批量导入最多10000条 | POST /batch-import |
