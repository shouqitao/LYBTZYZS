# 验方 API

> Controller: `FormulasController` | 路由前缀: `/api/v1/formulas` | 默认权限: `DoctorOrAdmin`
>
> ⚠️ **权限详见** [04-permissions.md](../01-product/04-permissions.md) §验方管理。代码已按此实现（类级 `DoctorOrAdmin` + 批量 `AdminOrSuperAdmin`），前台不可查已落地。

## 概述

验方 (Formula) 管理模块，覆盖 CRUD、药材组成管理、延迟绑定验证、导入、状态切换、批量删除。启用 OutputCache。
Doctor 只能看到自己的和共享的验方，Admin/SuperAdmin 可操作全部。资源级授权通过 `FormulaAuthorizationHandler` 实现。

---

## 端点列表

### GET /formulas

获取验方分页列表。Doctor 仅看到自己创建的 + 共享的验方。

- **权限**: `Doctor/Admin`（前台不可查）

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词（名称、功效） |
| `category` | string? | null | 分类筛选 |

**响应**: `ApiResponse<PagedResult<FormulaListDto>>`

```json
{
  "items": [
    { "id": "...", "name": "六味地黄丸", "effect": "滋阴补肾", "indications": "...", "category": "补益剂", "isShared": true, "validationStatus": "Validated", "status": "Enabled", "herbCount": 6, "totalPrice": 45.80, "createdAt": "..." },
    { "id": "...", "name": "四物汤", "effect": "补血调血", "category": "补益剂", "isShared": false, "validationStatus": "PendingValidation", "status": "Enabled", "herbCount": 4, "totalPrice": 32.50, "createdAt": "..." }
  ],
  "totalCount": 25, "page": 1, "pageSize": 20, "totalPages": 2
}
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60108 | 400 | 分页参数无效 |

---

### GET /formulas/{id}

获取验方详情（含药材组成）。

- **权限**: `Doctor/Admin`（前台不可查）
- **路径参数**: `id` (Guid)

**响应**: `ApiResponse<FormulaDetailDto>`

```json
{
  "id": "...", "name": "六味地黄丸", "category": "补益剂",
  "description": "经典滋阴补肾名方", "source": "《小儿药证直诀》",
  "effect": "滋阴补肾", "usage": "口服，一次6g，一日2次",
  "contraindications": "脾虚便溏者慎用", "status": "Enabled", "isShared": true,
  "createdBy": "...", "createdAt": "...", "updatedAt": "...",
  "herbs": [
    { "id": "...", "herbId": "...", "herbName": "熟地黄", "dosage": 24.0, "unit": "g", "remark": "君药", "isValidated": true },
    { "id": "...", "herbId": null, "herbName": "茯苓", "dosage": 9.0, "unit": "g", "remark": "佐药", "isValidated": false }
  ]
}
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60101 | 404 | 验方不存在 |

---

### POST /formulas

新增验方。自动设置 `createdBy` 为当前用户。

- **权限**: `Admin/Doctor`（Doctor 仅自己创建）

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
| `herbs` | array? | 否 | 药材列表（含 herbName/dosage/unit/remark） |

```json
{
  "name": "逍遥散", "category": "理气剂", "effect": "疏肝解郁，养血健脾",
  "usage": "口服，一次6g，一日2次", "isShared": true,
  "herbs": [
    { "herbName": "柴胡", "dosage": 10.0, "unit": "g", "remark": "君药" },
    { "herbName": "当归", "dosage": 10.0, "unit": "g", "remark": "臣药" }
  ]
}
```

**响应** (201): `ApiResponse<FormulaDetailDto>` — 同 GET /formulas/{id}

| 错误码 | HTTP | 说明 |
|--------|------|------|
| 400 | — | 参数验证失败（name/effect/usage 为空） |

---

### PUT /formulas/{id}

更新验方。执行所有权检查。

- **权限**: `Admin/Doctor`（Doctor 仅自己创建）
- **路径参数**: `id` (Guid)
- **请求体**: `FormulaInputDto`（同 POST）
- **响应**: `ApiResponse<FormulaDetailDto>` — 返回更新后详情

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60103 | 403 | 无权限操作此验方 |
| ERR-60101 | 404 | 验方不存在 |

---

### DELETE /formulas/{id}

软删除验方。执行所有权检查。

- **权限**: `Admin/Doctor`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<bool>` → `true`

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60101 | 404 | 验方不存在 |
| ERR-60103 | 403 | 无权限操作此验方 |

---

### POST /formulas/batch-import

JSON 批量导入验方（Server 端只处理 DTO，Excel 解析由 Client 端负责）。

- **权限**: `Admin+`

**请求体** (`FormulaBatchImportInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `formulas` | array | 是 | 验方列表（含 name/effect/usage/herbs 等） |
| `fileName` | string | 否 | 原始文件名（用于日志） |

```json
{
  "formulas": [
    { "name": "四君子汤", "category": "补益剂", "effect": "益气健脾", "usage": "口服，一次6g，一日2次",
      "herbs": [
        { "herbName": "人参", "dosage": 10.0, "unit": "g", "remark": "君药" },
        { "herbName": "白术", "dosage": 10.0, "unit": "g", "remark": "臣药" }
      ] }
  ],
  "fileName": "验方数据.xlsx"
}
```

**响应**: `ApiResponse<FormulaBatchImportResultDto>`

```json
{ "totalCount": 50, "successCount": 48, "failureCount": 2, "message": "批量导入完成" }
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60302 | 400 | 导入数据不能为空 |

---

### GET /formulas/pending-validation

获取待校验的验方列表（含未绑定系统药材的 HerbItem）。

- **权限**: `Doctor/Admin`（前台不可查）
- **响应**: `ApiResponse<List<FormulaDetailDto>>`

> 响应结构同 GET /formulas/{id}，仅返回有未验证药材的验方。

---

### POST /formulas/{formulaId}/herbs/{herbItemId}/validate

验证验方药材 — 将未绑定的 HerbItem 手动绑定到系统药材库。

- **权限**: `Doctor/Admin`
- **路径参数**: `formulaId` (Guid), `herbItemId` (Guid)

**请求体** (`ValidateFormulaHerbInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `selectedHerbId` | Guid | 是 | 系统药材库中的药材 ID |

- **响应**: `ApiResponse`（`data` 为 `null`）

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60201 | 400 | 参数不能为空 |
| ERR-60202 | 404 | 药材项不存在 |
| ERR-60204 | 404 | 所选药材不存在 |

---

### POST /formulas/{id}/toggle-status

切换验方状态（启用/禁用），无请求体。执行所有权检查。

- **权限**: `Admin+`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<FormulaDetailDto>` — 返回切换后详情

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60101 | 404 | 验方不存在 |
| ERR-60103 | 403 | 无权限操作此验方 |

---

### POST /formulas/batch-delete

批量软删除验方。

- **权限**: `Admin+`

**请求体** (`BatchDeleteInputDto`):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 验方 ID 列表 |

**响应**: `ApiResponse<BatchOperationResultDto>`

```json
{ "totalCount": 2, "successCount": 2, "failureCount": 0, "message": "批量删除完成" }
```

| 错误码 | HTTP | 说明 |
|--------|------|------|
| ERR-60301 | 400 | 请至少选择一个方剂 |

---

### POST /formulas/{id}/restore

恢复已软删除的验方（绕过软删除全局过滤器）。

- **权限**: `Admin`
- **路径参数**: `id` (Guid)
- **响应**: `ApiResponse<FormulaDetailDto>`

---

### POST /formulas/batch-enable / batch-disable

批量启用或禁用验方。

- **权限**: `AdminOrSuperAdmin`

**请求体** (两个端点相同):

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | Guid[] | 是 | 验方 ID 列表 |

- **响应**: `ApiResponse<BatchOperationResultDto>`

---

## 错误码汇总

> 完整定义见 [formulas.md PRD](../02-requirements/06-formulas.md)。分区: 6xxxx。

### 核心错误 (601xx)

| 错误码 | HTTP | 用户消息 | 触发端点 |
|--------|------|----------|----------|
| ERR-60101 | 404 | 验方不存在 | GET/PUT/DELETE /{id}, toggle-status |
| ERR-60102 | 400 | 验方ID不能为空 | 传入 Guid.Empty |
| ERR-60103 | 403 | 无权限操作此验方 | PUT/DELETE /{id}, toggle-status |
| ERR-60108 | 400 | 分页参数无效 | GET / |

### 药材验证错误 (602xx)

| 错误码 | HTTP | 用户消息 | 触发端点 |
|--------|------|----------|----------|
| ERR-60201 | 400 | 参数不能为空 | POST /{id}/herbs/{herbItemId}/validate |
| ERR-60202 | 404 | 药材项不存在 | POST /{id}/herbs/{herbItemId}/validate |
| ERR-60204 | 404 | 所选药材不存在 | POST /{id}/herbs/{herbItemId}/validate |

### 批量操作错误 (603xx)

| 错误码 | HTTP | 用户消息 | 触发端点 |
|--------|------|----------|----------|
| ERR-60301 | 400 | 请至少选择一个方剂 | POST /batch-delete |
| ERR-60302 | 400 | 导入数据不能为空 | POST /batch-import |
