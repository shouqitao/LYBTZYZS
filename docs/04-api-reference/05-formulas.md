# 验方 API

> Controller: `FormulasController` | 路由前缀: `/api/v1/formulas` | 默认权限: `[Authorize(Policy = "DoctorOrReceptionist")]`
>
> ⚠️ **权限待对齐（D7，基线§3）**：文档目标策略为 `DoctorOrReceptionist`；代码当前为 `DoctorOrAdmin`，待对齐。

## 概述

验方 (Formula) 管理模块，覆盖 CRUD、药材组成管理、延迟绑定验证、导入、状态切换、批量删除。启用 OutputCache (`FormulasCache`)。

Doctor 只能看到自己的和共享的验方，Admin/SuperAdmin 可操作全部。资源级授权通过 `FormulaAuthorizationHandler` 实现。

---

## GET /formulas

获取验方列表 (分页)。Doctor 角色自动过滤为仅看到自己创建的和共享的验方。

- **权限**: DoctorOrReceptionist

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词（匹配名称、功效） |
| `category` | string? | null | 分类筛选 |

**角色过滤逻辑**:
- Admin/SuperAdmin: 查看全部
- Doctor: 只看到 `currentUserId` 创建的 + 共享的

**成功响应** (200): `ApiResponse<PagedResult<FormulaListDto>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "items": [
      {
        "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "name": "六味地黄丸",
        "effect": "滋阴补肾",
        "indications": "肾阴虚证，腰膝酸软，头晕耳鸣",
        "category": "补益剂",
        "isShared": true,
        "validationStatus": "Validated",
        "status": "Enabled",
        "herbCount": 6,
        "totalPrice": 45.80,
        "createdAt": "2026-03-15T09:30:00Z"
      },
      {
        "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
        "name": "四物汤",
        "effect": "补血调血",
        "indications": "血虚证，面色萎黄，月经不调",
        "category": "补益剂",
        "isShared": false,
        "validationStatus": "PendingValidation",
        "status": "Enabled",
        "herbCount": 4,
        "totalPrice": 32.50,
        "createdAt": "2026-04-20T14:15:00Z"
      }
    ],
    "totalCount": 25,
    "page": 1,
    "pageSize": 20,
    "totalPages": 2
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
# 获取验方列表（默认分页）
curl -X GET "http://localhost:5000/api/v1/formulas?page=1&pageSize=20" \
  -H "Authorization: Bearer $TOKEN"

# 按关键词搜索
curl -X GET "http://localhost:5000/api/v1/formulas?keyword=%E5%85%AD%E5%91%B3" \
  -H "Authorization: Bearer $TOKEN"

# 按分类筛选
curl -X GET "http://localhost:5000/api/v1/formulas?category=%E8%A1%A5%E7%9B%8A%E5%89%82" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 400 | ERR-60108 | 分页参数无效 (page<1 或 pageSize 超限) |

---

## GET /formulas/{id}

获取验方详情 (含药材组成)。

- **权限**: DoctorOrReceptionist

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<FormulaDetailDto>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "六味地黄丸",
    "category": "补益剂",
    "description": "经典滋阴补肾名方",
    "source": "《小儿药证直诀》",
    "effect": "滋阴补肾",
    "usage": "口服，一次6g，一日2次",
    "contraindications": "脾虚便溏者慎用",
    "status": "Enabled",
    "isShared": true,
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-03-15T09:30:00Z",
    "updatedAt": "2026-06-01T11:20:00Z",
    "herbs": [
      {
        "id": "d4e5f6a7-b8c9-0123-def0-123456789012",
        "herbId": "e5f6a7b8-c9d0-1234-ef01-234567890123",
        "herbName": "熟地黄",
        "dosage": 24.0,
        "unit": "g",
        "remark": "君药",
        "isValidated": true
      },
      {
        "id": "f6a7b8c9-d0e1-2345-f012-345678901234",
        "herbId": "a7b8c9d0-e1f2-3456-0123-456789012345",
        "herbName": "山茱萸",
        "dosage": 12.0,
        "unit": "g",
        "remark": "臣药",
        "isValidated": true
      },
      {
        "id": "b8c9d0e1-f2a3-4567-1234-567890123456",
        "herbId": null,
        "herbName": "茯苓",
        "dosage": 9.0,
        "unit": "g",
        "remark": "佐药",
        "isValidated": false
      }
    ]
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**错误响应** (404):

```json
{
  "success": false,
  "message": "验方不存在",
  "data": null,
  "errors": {
    "code": "ERR-60101",
    "details": "未找到ID为 a1b2c3d4-e5f6-7890-abcd-ef1234567890 的验方"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X GET "http://localhost:5000/api/v1/formulas/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN"
```

---

## POST /formulas

新增验方。自动设置 `createdBy` 为当前用户 ID。

- **权限**: DoctorOrReceptionist

**请求体** (`FormulaInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `name` | string | 是 | 验方名称 |
| `category` | string? | 否 | 分类 |
| `description` | string? | 否 | 描述 |
| `effect` | string | 是 | 功效 |
| `usage` | string | 是 | 用法用量 |
| `property` | string? | 否 | 药性 |
| `isShared` | bool | 否 | 是否共享，默认 false |
| `instructions` | string? | 否 | 煎服法 |
| `indications` | string? | 否 | 主治 |
| `contraindications` | string? | 否 | 禁忌 |
| `preparation` | string? | 否 | 制备方法 |
| `remark` | string? | 否 | 备注 |
| `herbs` | array? | 否 | 药材列表 |

```json
{
  "name": "逍遥散",
  "category": "理气剂",
  "description": "疏肝解郁，养血健脾经典方",
  "effect": "疏肝解郁，养血健脾",
  "usage": "口服，一次6g，一日2次",
  "property": "性平",
  "isShared": true,
  "instructions": "温水送服",
  "indications": "肝郁血虚脾弱证，两胁作痛，头痛目眩",
  "contraindications": "阴虚火旺者不宜",
  "preparation": "上为粗末，每服二钱",
  "remark": "经典名方",
  "herbs": [
    { "herbName": "柴胡", "dosage": 10.0, "unit": "g", "remark": "君药" },
    { "herbName": "当归", "dosage": 10.0, "unit": "g", "remark": "臣药" },
    { "herbName": "白芍", "dosage": 10.0, "unit": "g", "remark": "臣药" },
    { "herbName": "白术", "dosage": 10.0, "unit": "g", "remark": "佐药" },
    { "herbName": "茯苓", "dosage": 10.0, "unit": "g", "remark": "佐药" },
    { "herbName": "薄荷", "dosage": 3.0, "unit": "g", "remark": "佐使药" },
    { "herbName": "生姜", "dosage": 3.0, "unit": "g", "remark": "佐使药" },
    { "herbName": "甘草", "dosage": 5.0, "unit": "g", "remark": "使药" }
  ]
}
```

**成功响应** (201 Created): `ApiResponse<FormulaDetailDto>`

```json
{
  "success": true,
  "message": "验方创建成功",
  "data": {
    "id": "c4d5e6f7-a8b9-0123-cdef-123456789012",
    "name": "逍遥散",
    "category": "理气剂",
    "description": "疏肝解郁，养血健脾经典方",
    "source": null,
    "effect": "疏肝解郁，养血健脾",
    "usage": "口服，一次6g，一日2次",
    "contraindications": "阴虚火旺者不宜",
    "status": "Enabled",
    "isShared": true,
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-06-25T10:30:00Z",
    "updatedAt": null,
    "herbs": [
      {
        "id": "d5e6f7a8-b9c0-1234-def0-123456789012",
        "herbId": null,
        "herbName": "柴胡",
        "dosage": 10.0,
        "unit": "g",
        "remark": "君药",
        "isValidated": false
      }
    ]
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X POST "http://localhost:5000/api/v1/formulas" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "逍遥散",
    "category": "理气剂",
    "effect": "疏肝解郁，养血健脾",
    "usage": "口服，一次6g，一日2次",
    "isShared": true,
    "herbs": [
      {"herbName": "柴胡", "dosage": 10.0, "unit": "g", "remark": "君药"},
      {"herbName": "当归", "dosage": 10.0, "unit": "g", "remark": "臣药"}
    ]
  }'
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 400 | — | 参数验证失败（name/effect/usage 为空） |

---

## PUT /formulas/{id}

更新验方。执行所有权检查。

- **权限**: DoctorOrReceptionist（Admin 可操作全部，Doctor 只能操作自己的）

**路径参数**: `id` (Guid)

**请求体**: `FormulaInputDto`（同 POST /formulas）

```json
{
  "name": "六味地黄丸",
  "category": "补益剂",
  "description": "经典滋阴补肾名方（修订版）",
  "effect": "滋阴补肾",
  "usage": "口服，一次6g，一日2次，饭前服用",
  "isShared": true,
  "contraindications": "脾虚便溏者慎用；感冒发热期间停服",
  "herbs": [
    { "herbName": "熟地黄", "dosage": 24.0, "unit": "g", "remark": "君药" },
    { "herbName": "山茱萸", "dosage": 12.0, "unit": "g", "remark": "臣药" },
    { "herbName": "山药", "dosage": 12.0, "unit": "g", "remark": "臣药" },
    { "herbName": "泽泻", "dosage": 9.0, "unit": "g", "remark": "佐药" },
    { "herbName": "茯苓", "dosage": 9.0, "unit": "g", "remark": "佐药" },
    { "herbName": "牡丹皮", "dosage": 9.0, "unit": "g", "remark": "佐药" }
  ]
}
```

**成功响应** (200): `ApiResponse<FormulaDetailDto>`

```json
{
  "success": true,
  "message": "验方更新成功",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "六味地黄丸",
    "category": "补益剂",
    "description": "经典滋阴补肾名方（修订版）",
    "source": "《小儿药证直诀》",
    "effect": "滋阴补肾",
    "usage": "口服，一次6g，一日2次，饭前服用",
    "contraindications": "脾虚便溏者慎用；感冒发热期间停服",
    "status": "Enabled",
    "isShared": true,
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-03-15T09:30:00Z",
    "updatedAt": "2026-06-25T10:30:00Z",
    "herbs": []
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**错误响应** (403):

```json
{
  "success": false,
  "message": "无权限操作此验方",
  "data": null,
  "errors": {
    "code": "ERR-60103",
    "details": "您没有权限修改此验方"
  },
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X PUT "http://localhost:5000/api/v1/formulas/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "六味地黄丸",
    "effect": "滋阴补肾",
    "usage": "口服，一次6g，一日2次，饭前服用",
    "isShared": true
  }'
```

---

## DELETE /formulas/{id}

删除验方 (软删除)。执行所有权检查。

- **权限**: DoctorOrReceptionist（Admin 可操作全部，Doctor 只能操作自己的）

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<bool>`

```json
{
  "success": true,
  "message": "删除成功",
  "data": true,
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X DELETE "http://localhost:5000/api/v1/formulas/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 404 | ERR-60101 | 验方不存在 |
| 403 | ERR-60103 | 无权限操作此验方 |

---

## POST /formulas/batch-import

JSON 批量导入验方（Server 端只处理 DTO，Excel 解析由 Client 端负责）。

- **权限**: DoctorOrReceptionist

**请求体** (`FormulaBatchImportInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `formulas` | array | 是 | 验方列表 |
| `fileName` | string | 否 | 原始文件名（用于日志） |

```json
{
  "formulas": [
    {
      "name": "四君子汤",
      "category": "补益剂",
      "effect": "益气健脾",
      "usage": "口服，一次6g，一日2次",
      "herbs": [
        { "herbName": "人参", "dosage": 10.0, "unit": "g", "remark": "君药" },
        { "herbName": "白术", "dosage": 10.0, "unit": "g", "remark": "臣药" },
        { "herbName": "茯苓", "dosage": 10.0, "unit": "g", "remark": "佐药" },
        { "herbName": "甘草", "dosage": 5.0, "unit": "g", "remark": "使药" }
      ]
    },
    {
      "name": "参苓白术散",
      "category": "补益剂",
      "effect": "益气健脾，渗湿止泻",
      "usage": "口服，一次6g，一日2-3次",
      "herbs": []
    }
  ],
  "fileName": "验方数据.xlsx"
}
```

**成功响应** (200): `ApiResponse<FormulaBatchImportResultDto>`

```json
{
  "success": true,
  "message": "批量导入完成",
  "data": {
    "totalCount": 50,
    "successCount": 48,
    "failureCount": 2,
    "message": "批量导入完成"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X POST "http://localhost:5000/api/v1/formulas/batch-import" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "formulas": [
      {
        "name": "四君子汤",
        "category": "补益剂",
        "effect": "益气健脾",
        "usage": "口服，一次6g，一日2次",
        "herbs": [
          {"herbName": "人参", "dosage": 10.0, "unit": "g", "remark": "君药"}
        ]
      }
    ],
    "fileName": "验方数据.xlsx"
  }'
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 400 | ERR-60302 | 导入数据不能为空 |

---

## GET /formulas/pending-validation

获取待校验的验方列表（含未绑定系统药材的 HerbItem）。

- **权限**: DoctorOrReceptionist

**成功响应** (200): `ApiResponse<List<FormulaDetailDto>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": [
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "name": "四物汤",
      "category": "补益剂",
      "description": "补血调血基础方",
      "source": "《太平惠民和剂局方》",
      "effect": "补血调血",
      "usage": "口服，一次6g，一日2次",
      "contraindications": null,
      "status": "Enabled",
      "isShared": false,
      "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "createdAt": "2026-04-20T14:15:00Z",
      "updatedAt": null,
      "herbs": [
        {
          "id": "e6f7a8b9-c0d1-2345-ef01-234567890123",
          "herbId": null,
          "herbName": "当归",
          "dosage": 10.0,
          "unit": "g",
          "remark": "君药",
          "isValidated": false
        },
        {
          "id": "f7a8b9c0-d1e2-3456-f012-345678901234",
          "herbId": null,
          "herbName": "川芎",
          "dosage": 8.0,
          "unit": "g",
          "remark": "臣药",
          "isValidated": false
        }
      ]
    }
  ],
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X GET "http://localhost:5000/api/v1/formulas/pending-validation" \
  -H "Authorization: Bearer $TOKEN"
```

---

## POST /formulas/{formulaId}/herbs/{herbItemId}/validate

验证验方药材 — 将未绑定的 HerbItem 手动绑定到系统药材库。

- **权限**: DoctorOrReceptionist

**路径参数**:
- `formulaId` (Guid): 验方 ID
- `herbItemId` (Guid): 药材项 ID

**请求体** (`ValidateFormulaHerbInputDto`):

```json
{
  "selectedHerbId": "f8a9b0c1-d2e3-4567-0123-456789012345"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `selectedHerbId` | Guid | 是 | 系统药材库中的药材 ID |

**成功响应** (200): `ApiResponse`

```json
{
  "success": true,
  "message": "药材验证成功",
  "data": null,
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X POST "http://localhost:5000/api/v1/formulas/b2c3d4e5-f6a7-8901-bcde-f12345678901/herbs/e6f7a8b9-c0d1-2345-ef01-234567890123/validate" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"selectedHerbId": "f8a9b0c1-d2e3-4567-0123-456789012345"}'
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 400 | ERR-60201 | 参数不能为空 |
| 404 | ERR-60202 | 药材项不存在 |
| 404 | ERR-60204 | 所选药材不存在 |

---

## POST /formulas/{id}/toggle-status

切换验方状态 (启用/禁用)。执行所有权检查。

- **权限**: DoctorOrReceptionist（Admin 可操作全部，Doctor 只能操作自己的）

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<FormulaDetailDto>`

```json
{
  "success": true,
  "message": "验方状态已更新",
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "六味地黄丸",
    "category": "补益剂",
    "description": "经典滋阴补肾名方",
    "source": "《小儿药证直诀》",
    "effect": "滋阴补肾",
    "usage": "口服，一次6g，一日2次",
    "contraindications": "脾虚便溏者慎用",
    "status": "Disabled",
    "isShared": true,
    "createdBy": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "createdAt": "2026-03-15T09:30:00Z",
    "updatedAt": "2026-06-25T10:30:00Z",
    "herbs": []
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X POST "http://localhost:5000/api/v1/formulas/a1b2c3d4-e5f6-7890-abcd-ef1234567890/toggle-status" \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 404 | ERR-60101 | 验方不存在 |
| 403 | ERR-60103 | 无权限操作此验方 |

---

## POST /formulas/batch-delete

批量删除验方。

- **权限**: DoctorOrReceptionist

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
| `ids` | Guid[] | 是 | 验方 ID 列表 |

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

```json
{
  "success": true,
  "message": "批量删除完成",
  "data": {
    "totalCount": 2,
    "successCount": 2,
    "failureCount": 0,
    "message": "批量删除完成"
  },
  "errors": null,
  "timestamp": 1750864800,
  "requestId": "0HN8VABCDEF12345"
}
```

**curl 示例**:

```bash
curl -X POST "http://localhost:5000/api/v1/formulas/batch-delete" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "ids": [
      "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "b2c3d4e5-f6a7-8901-bcde-f12345678901"
    ]
  }'
```

**错误码**:

| HTTP 状态码 | 错误码 | 说明 |
|------------|--------|------|
| 400 | ERR-60301 | 请至少选择一个方剂 |

---

## 错误码汇总

> 完整错误码定义见 [formulas.md PRD](../02-requirements/06-formulas.md)。错误码分区: 6xxxx。

### 核心错误 (601xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-60101 | FormulaNotFound | 404 | 验方不存在 | GET/PUT/DELETE /{id}, POST /{id}/toggle-status |
| ERR-60102 | FormulaIdInvalid | 400 | 验方ID不能为空 | 传入 Guid.Empty |
| ERR-60103 | FormulaNoPermission | 403 | 无权限操作此验方 | PUT/DELETE /{id}, POST /{id}/toggle-status |
| ERR-60108 | FormulaInvalidPagination | 400 | 分页参数无效 | GET / |

### 药材验证错误 (602xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-60201 | HerbItemIdInvalid | 400 | 参数不能为空 | POST /{id}/herbs/{herbItemId}/validate |
| ERR-60202 | HerbItemNotFound | 404 | 药材项不存在 | POST /{id}/herbs/{herbItemId}/validate |
| ERR-60204 | SystemHerbNotFound | 404 | 所选药材不存在 | POST /{id}/herbs/{herbItemId}/validate |

### 批量操作错误 (603xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-60301 | FormulaBatchEmpty | 400 | 请至少选择一个方剂 | POST /batch-delete |
| ERR-60302 | FormulaBatchImportEmpty | 400 | 导入数据不能为空 | POST /batch-import |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-18 | v1.1 | 新增错误码章节 (ERR-60101~60302) |
| 2026-06-25 | v2.0 | 全面重写：移除不存在的 batch-enable/batch-disable/export/import-template 端点；为全部 10 个端点补充完整请求/响应 JSON 示例、curl 命令、错误码表 |
| 2026-06-28 | v2.1 | 文档对齐基线：权限策略加 D7 待对齐标注（目标 DoctorOrReceptionist，代码 DoctorOrAdmin） |