# 药材 API

> Controller: `HerbsController` | 路由前缀: `/api/v1/herbs` | 默认权限: `[Authorize(Policy = "DoctorOrReceptionist")]`

## 概述

药材管理 CRUD、分类筛选、JSON 批量导入、状态切换、批量操作。启用 OutputCache (`HerbsCache`)。
Doctor 只能编辑自己创建的药材，Admin 可操作全部。

> **注意**: 药材 Excel 导入/导出在客户端 (Desktop) 完成，服务端无 `POST /herbs/import`、`GET /herbs/export`、`GET /herbs/export-all` 端点。服务端仅提供 `POST /herbs/batch-import` (JSON 批量导入)。

---

## GET /herbs

获取药材分页列表。启用 OutputCache。

- **权限**: `DoctorOrReceptionist`

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词 (名称/拼音码) |
| `category` | string? | null | 分类筛选 |

**成功响应** (200): `ApiResponse<PagedResult<HerbListDto>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "items": [
      {
        "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "name": "黄芪",
        "pinYinCode": "HQ",
        "category": "补气药",
        "origin": "蒙古黄芪或膜荚黄芪的干燥根",
        "spec": "统货",
        "unit": "克",
        "price": 28.50,
        "status": "Enabled",
        "createdAt": "2026-01-10T08:00:00Z"
      },
      {
        "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
        "name": "当归",
        "pinYinCode": "DG",
        "category": "补血药",
        "origin": "当归的干燥根",
        "spec": "精选",
        "unit": "克",
        "price": 45.00,
        "status": "Enabled",
        "createdAt": "2026-01-10T08:00:00Z"
      },
      {
        "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
        "name": "金银花",
        "pinYinCode": "JYH",
        "category": "清热解毒药",
        "origin": "忍冬的干燥花蕾",
        "spec": "特级",
        "unit": "克",
        "price": 68.00,
        "status": "Enabled",
        "createdAt": "2026-02-15T10:00:00Z"
      }
    ],
    "totalCount": 230,
    "page": 1,
    "pageSize": 20,
    "totalPages": 12
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B50"
}
```

**curl 示例：**

```bash
# 获取药材列表
curl -X GET "http://localhost:5000/api/v1/herbs?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"

# 按关键词搜索
curl -X GET "http://localhost:5000/api/v1/herbs?keyword=黄芪&page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"

# 按分类筛选
curl -X GET "http://localhost:5000/api/v1/herbs?category=补气药&page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 分页参数无效 (ERR-50106) |
| 401 | 未认证 |

---

## GET /herbs/{id}

获取药材详情。

- **权限**: `DoctorOrReceptionist`

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<HerbDetailDto>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "黄芪",
    "pinYinCode": "HQ",
    "category": "补气药",
    "properties": "甘，微温。归脾、肺经。",
    "effect": "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌。",
    "origin": "蒙古黄芪或膜荚黄芪的干燥根",
    "spec": "统货",
    "costPrice": 18.00,
    "price": 28.50,
    "unit": "克",
    "usage": "9～30g",
    "remark": "蜜炙增强补中益气作用",
    "status": "Enabled",
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-01-10T08:00:00Z",
    "updatedAt": "2026-06-15T14:30:00Z"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B51"
}
```

**错误响应** (404):

```json
{
  "success": false,
  "message": "药材不存在",
  "data": null,
  "errors": {
    "code": "ERR-50101"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B52"
}
```

**curl 示例：**

```bash
curl -X GET "http://localhost:5000/api/v1/herbs/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | 未认证 |
| 404 | 药材不存在 (ERR-50101) |

---

## POST /herbs

创建新药材。

- **权限**: `DoctorOrReceptionist`

**请求体** (`HerbInputDto`):

```json
{
  "name": "白术",
  "pinYinCode": "BZ",
  "category": "补气药",
  "properties": "苦、甘，温。归脾、胃经。",
  "origin": "白术的干燥根茎",
  "spec": "统货",
  "unit": "克",
  "price": 32.00,
  "costPrice": 20.00,
  "effect": "健脾益气，燥湿利水，止汗，安胎。",
  "usage": "6～12g",
  "remark": null
}
```

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

**成功响应** (201 Created): `ApiResponse<HerbDetailDto>`

```json
{
  "success": true,
  "message": "创建成功",
  "data": {
    "id": "d4e5f6a7-b8c9-0123-def4-567890abcdef",
    "name": "白术",
    "pinYinCode": "BZ",
    "category": "补气药",
    "properties": "苦、甘，温。归脾、胃经。",
    "effect": "健脾益气，燥湿利水，止汗，安胎。",
    "origin": "白术的干燥根茎",
    "spec": "统货",
    "costPrice": 20.00,
    "price": 32.00,
    "unit": "克",
    "usage": "6～12g",
    "remark": null,
    "status": "Enabled",
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-06-25T10:00:00Z",
    "updatedAt": null
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B53"
}
```

**错误响应** (400 — 验证失败):

```json
{
  "success": false,
  "message": "验证失败",
  "data": null,
  "errors": {
    "code": "ERR-50102",
    "details": {
      "name": ["药材名称不能为空"],
      "price": ["售价必须大于0"]
    }
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B54"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/herbs" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "白术",
    "pinYinCode": "BZ",
    "category": "补气药",
    "properties": "苦、甘，温。归脾、胃经。",
    "origin": "白术的干燥根茎",
    "spec": "统货",
    "unit": "克",
    "price": 32.00,
    "costPrice": 20.00,
    "effect": "健脾益气，燥湿利水，止汗，安胎。",
    "usage": "6～12g"
  }'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 验证失败 (ERR-50102) |
| 401 | 未认证 |

---

## PUT /herbs/{id}

更新药材信息。执行所有权检查。

- **权限**: `DoctorOrReceptionist`（Doctor 仅限自己创建的药材）

**路径参数**: `id` (Guid)

**请求体**: `HerbInputDto` (同创建)

```json
{
  "name": "黄芪",
  "pinYinCode": "HQ",
  "category": "补气药",
  "properties": "甘，微温。归脾、肺经。",
  "origin": "蒙古黄芪或膜荚黄芪的干燥根",
  "spec": "精选",
  "unit": "克",
  "price": 30.00,
  "costPrice": 19.00,
  "effect": "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌。",
  "usage": "9～30g",
  "remark": "价格已更新"
}
```

**成功响应** (200): `ApiResponse<HerbDetailDto>` — 返回更新后的完整药材详情。

**错误响应** (403):

```json
{
  "success": false,
  "message": "无权限操作此药材",
  "data": null,
  "errors": {
    "code": "ERR-50103"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B55"
}
```

**curl 示例：**

```bash
curl -X PUT "http://localhost:5000/api/v1/herbs/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "黄芪",
    "pinYinCode": "HQ",
    "category": "补气药",
    "properties": "甘，微温。归脾、肺经。",
    "origin": "蒙古黄芪或膜荚黄芪的干燥根",
    "spec": "精选",
    "unit": "克",
    "price": 30.00,
    "costPrice": 19.00,
    "effect": "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌。",
    "usage": "9～30g",
    "remark": "价格已更新"
  }'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 验证失败 (ERR-50102) |
| 401 | 未认证 |
| 403 | 无权限操作此药材 (ERR-50103) |
| 404 | 药材不存在 (ERR-50101) |

---

## DELETE /herbs/{id}

删除药材 (软删除)。执行所有权检查。

- **权限**: `DoctorOrReceptionist`（Doctor 仅限自己创建的药材）

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<bool>`

```json
{
  "success": true,
  "message": "删除成功",
  "data": true,
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B56"
}
```

**curl 示例：**

```bash
curl -X DELETE "http://localhost:5000/api/v1/herbs/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | 未认证 |
| 403 | 无权限操作此药材 (ERR-50103) |
| 404 | 药材不存在 (ERR-50101) |

---

## POST /herbs/{id}/toggle-status

切换药材状态 (启用/禁用)。执行所有权检查。

- **权限**: `DoctorOrReceptionist`（Doctor 仅限自己创建的药材）

**路径参数**: `id` (Guid)

**请求体**: 无

**成功响应** (200): `ApiResponse<HerbDetailDto>`

```json
{
  "success": true,
  "message": "药材已禁用",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "黄芪",
    "pinYinCode": "HQ",
    "category": "补气药",
    "properties": "甘，微温。归脾、肺经。",
    "effect": "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌。",
    "origin": "蒙古黄芪或膜荚黄芪的干燥根",
    "spec": "统货",
    "costPrice": 18.00,
    "price": 28.50,
    "unit": "克",
    "usage": "9～30g",
    "remark": "蜜炙增强补中益气作用",
    "status": "Disabled",
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-01-10T08:00:00Z",
    "updatedAt": "2026-06-25T10:00:00Z"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B57"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/herbs/a1b2c3d4-e5f6-7890-abcd-ef1234567890/toggle-status" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 401 | 未认证 |
| 403 | 无权限操作此药材 (ERR-50103) |
| 404 | 药材不存在 (ERR-50101) |

---

## POST /herbs/batch-delete

批量删除药材。

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
| `ids` | Guid[] | 是 | 药材 ID 列表，不能为空 |

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
  "requestId": "0HN8V5K1A2B58"
}
```

**错误响应** (400 — 空列表):

```json
{
  "success": false,
  "message": "请至少选择一个药材",
  "data": null,
  "errors": {
    "code": "ERR-50201"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B59"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/herbs/batch-delete" \
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
| 400 | 请至少选择一个药材 (ERR-50201) |
| 401 | 未认证 |

---

## POST /herbs/batch-import

JSON 批量导入药材 (非 Excel，直接 DTO 数组)。

- **权限**: `DoctorOrReceptionist`

**请求体** (`HerbBatchImportInputDto`):

```json
{
  "herbs": [
    {
      "name": "黄芪",
      "pinYinCode": "HQ",
      "category": "补气药",
      "properties": "甘，微温。归脾、肺经。",
      "origin": "蒙古黄芪或膜荚黄芪的干燥根",
      "spec": "统货",
      "unit": "克",
      "price": 28.50,
      "costPrice": 18.00,
      "effect": "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌。",
      "usage": "9～30g",
      "remark": null
    },
    {
      "name": "当归",
      "pinYinCode": "DG",
      "category": "补血药",
      "properties": "甘、辛，温。归肝、心、脾经。",
      "origin": "当归的干燥根",
      "spec": "精选",
      "unit": "克",
      "price": 45.00,
      "costPrice": 30.00,
      "effect": "补血活血，调经止痛，润肠通便。",
      "usage": "6～12g",
      "remark": null
    }
  ],
  "strategy": "Skip"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `herbs` | HerbInputDto[] | 是 | 药材列表，最多 10000 条 |
| `strategy` | enum | 是 | 重复策略: `Skip`(跳过) / `Overwrite`(覆盖) / `Error`(报错) |

**成功响应** (200): `ApiResponse<HerbBatchImportResultDto>`

```json
{
  "success": true,
  "message": "批量导入完成",
  "data": {
    "totalCount": 2,
    "successCount": 2,
    "failureCount": 0,
    "skippedCount": 0
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B5A"
}
```

**错误响应** (400 — 超限):

```json
{
  "success": false,
  "message": "批量导入最多10000条",
  "data": null,
  "errors": {
    "code": "ERR-50202"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8V5K1A2B5B"
}
```

**curl 示例：**

```bash
curl -X POST "http://localhost:5000/api/v1/herbs/batch-import" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "herbs": [
      {
        "name": "黄芪",
        "pinYinCode": "HQ",
        "category": "补气药",
        "properties": "甘，微温。归脾、肺经。",
        "origin": "蒙古黄芪或膜荚黄芪的干燥根",
        "spec": "统货",
        "unit": "克",
        "price": 28.50,
        "costPrice": 18.00,
        "effect": "补气升阳，固表止汗，利水消肿，生津养血，行滞通痹，托毒排脓，敛疮生肌。",
        "usage": "9～30g"
      },
      {
        "name": "当归",
        "pinYinCode": "DG",
        "category": "补血药",
        "properties": "甘、辛，温。归肝、心、脾经。",
        "origin": "当归的干燥根",
        "spec": "精选",
        "unit": "克",
        "price": 45.00,
        "costPrice": 30.00,
        "effect": "补血活血，调经止痛，润肠通便。",
        "usage": "6～12g"
      }
    ],
    "strategy": "Skip"
  }'
```

**错误码：**

| HTTP 状态码 | 说明 |
|------------|------|
| 400 | 验证失败 (ERR-50102) |
| 400 | 批量导入最多10000条 (ERR-50202) |
| 401 | 未认证 |

---

## 错误码

> 完整错误码定义见 [herbs.md PRD](../02-requirements/05-herbs.md)。错误码分区: 5xxxx。

### 核心错误 (501xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-50101 | HerbNotFound | 404 | 药材不存在 | GET/PUT/DELETE /{id} |
| ERR-50102 | HerbValidationFailed | 400 | 验证失败 | POST /, PUT /{id} |
| ERR-50103 | HerbNoPermission | 403 | 无权限操作此药材 | PUT/DELETE /{id}, POST /{id}/toggle-status |
| ERR-50104 | HerbNotDeleted | 200 | 该药材未被删除 | POST /{id}/restore |
| ERR-50106 | HerbInvalidPagination | 400 | 分页参数无效 | GET / |

### 批量操作错误 (502xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-50201 | HerbBatchEmpty | 400 | 请至少选择一个药材 | POST /batch-delete |
| ERR-50202 | HerbBatchImportExceeded | 400 | 批量导入最多10000条 | POST /batch-import |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-18 | v1.1 | 新增错误码章节: 补充端点级 MCCEE 错误码 (ERR-50101~50203)，含核心/批量/导入三类 |
| 2026-06-12 | v1.2 | 标注 POST /herbs/import 为客户端功能; 服务端仅提供 batch-import (JSON) |
| 2026-06-12 | v1.3 | HerbDetailDto: 新增 origin/spec/costPrice/usage/remark 字段 |
| 2026-06-25 | v2.0 | 移除不存在的端点 (export/export-all/import-template/check-reference/batch-check-reference/batch-enable/batch-disable/restore); 补充全部 8 个端点的完整请求/响应 JSON 示例、curl 命令 |
