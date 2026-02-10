# 验方 API

> Controller: `FormulasController` | 路由前缀: `/api/v1/formulas` | 默认权限: `[Authorize(Policy = "DoctorOrAdmin")]`

## 概述

验方管理 CRUD、药材组成管理、延迟绑定验证、导入导出、状态切换、批量操作。启用 OutputCache (`FormulasCache`)。
Doctor 只能看到自己的和共享的验方，Admin 可操作全部。资源级授权通过 `FormulaAuthorizationHandler` 实现。

---

## GET /formulas

获取验方列表 (分页)。Doctor 角色自动过滤为仅看到自己创建的和共享的验方。

**查询参数**:

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `page` | int | 1 | 页码 (>0) |
| `pageSize` | int | 20 | 每页大小 (1-100) |
| `keyword` | string? | null | 搜索关键词 |
| `category` | string? | null | 分类筛选 |

**角色过滤逻辑**:
- Admin/SuperAdmin: 查看全部
- Doctor: 只看到 `currentUserId` 创建的 + 共享的

**成功响应** (200): `ApiResponse<PagedResult<FormulaListDto>>`

---

## GET /formulas/{id}

获取验方详情 (含药材组成)。

**路径参数**: `id` (Guid)

**FormulaDetailDto**:

```json
{
  "id": "guid",
  "name": "string",
  "category": "string",
  "description": "string",
  "source": "string",
  "effects": "string",
  "usage": "string",
  "contraindications": "string",
  "status": "Enabled|Disabled",
  "isShared": false,
  "createdBy": "guid",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "herbItems": [
    {
      "id": "guid",
      "herbId": "guid|null",       // null = 延迟绑定
      "herbName": "string",
      "dosage": 10.0,
      "unit": "g",
      "remark": "string",
      "isValidated": true           // 是否已绑定系统药材
    }
  ]
}
```

**错误响应**: 404 (验方不存在)

---

## POST /formulas

新增验方。自动设置 `createdBy` 为当前用户 ID。

**请求体** (`FormulaInputDto`): 含 name, category, description, source, effects, usage, contraindications, herbItems 等字段。

**成功响应** (200): `ApiResponse<FormulaDetailDto>`

---

## PUT /formulas/{id}

更新验方。执行所有权检查。

**路径参数**: `id` (Guid)

**请求体**: `FormulaInputDto`

**成功响应** (200): `ApiResponse<FormulaDetailDto>`

**错误响应**: 403 (权限不足)

---

## DELETE /formulas/{id}

删除验方 (软删除)。执行所有权检查。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse` ("删除成功")

---

## POST /formulas/batch-import

JSON 批量导入验方 (Server 端只处理 DTO，Excel 解析由 Client 端负责)。

**请求体** (`FormulaBatchImportInputDto`):

```json
{
  "formulas": [
    {
      "name": "string",
      "category": "string",
      "herbItems": [...]
    }
  ],
  "fileName": "string"   // 原始文件名 (用于日志)
}
```

**成功响应** (200): `ApiResponse<FormulaBatchImportResultDto>`

```json
{
  "data": {
    "totalCount": 50,
    "successCount": 48,
    "failureCount": 2,
    "message": "批量导入完成"
  }
}
```

---

## GET /formulas/export

导出验方数据到 Excel。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `category` | string? | 分类筛选 |

- **响应类型**: Excel 文件
- **文件名**: `验方数据_[category_]yyyyMMdd_HHmmss.xlsx`

---

## GET /formulas/import-template

下载验方导入 Excel 模板。

- **权限**: 匿名 (`[AllowAnonymous]`)
- **响应类型**: Excel 文件
- **文件名**: `验方导入模板_yyyyMMdd.xlsx`

---

## GET /formulas/pending-validation

获取待校验的验方列表 (含未绑定系统药材的 HerbItem)。

**成功响应** (200): `ApiResponse<List<FormulaDetailDto>>`

---

## POST /formulas/{formulaId}/herbs/{herbItemId}/validate

验证验方药材 -- 将未绑定的 HerbItem 手动绑定到系统药材库。

**路径参数**:
- `formulaId` (Guid): 验方 ID
- `herbItemId` (Guid): 药材项 ID

**请求体**: `Guid` (selectedHerbId) -- 系统药材 ID

**成功响应** (200): `ApiResponse` ("药材验证成功")

---

## POST /formulas/{id}/toggle-status

切换验方状态 (启用/禁用)。执行所有权检查。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<FormulaDetailDto>`

---

## POST /formulas/{id}/restore

恢复已删除的验方。绕过软删除全局过滤器。

**路径参数**: `id` (Guid)

**成功响应** (200): `ApiResponse<FormulaDetailDto>` ("验方已恢复")

---

## POST /formulas/batch-delete

批量删除验方。

**请求体**: `BatchDeleteInputDto` (`{ "ids": [...] }`)

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## POST /formulas/batch-enable

批量启用验方。

**请求体**: `BatchDeleteInputDto`

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

## POST /formulas/batch-disable

批量禁用验方。

**请求体**: `BatchDeleteInputDto`

**成功响应** (200): `ApiResponse<BatchOperationResultDto>`

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，15 个端点 |
