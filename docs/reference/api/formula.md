# Formula API - 方剂管理模块API参考

**Formula Management API Reference**

本文档提供LYBTZYZS方剂管理模块的完整API接口参考，包括所有HTTP端点、请求/响应格式、错误码和示例。

---

## 基础信息

**Base URL**: `/api/v1/formulas`

**认证方式**: Bearer Token (JWT)

**Content-Type**: `application/json`

**API版本**: v1.0

---

## 端点概览

| 端点 | 方法 | 说明 | 认证 |
|------|------|------|------|
| `/api/v1/formulas` | GET | 分页查询验方列表 | ✓ |
| `/api/v1/formulas/{id}` | GET | 获取单个验方详情 | ✓ |
| `/api/v1/formulas` | POST | 创建新验方 | ✓ |
| `/api/v1/formulas/{id}` | PUT | 更新验方信息 | ✓ |
| `/api/v1/formulas/{id}` | DELETE | 删除验方（软删除） | ✓ |
| `/api/v1/formulas/{id}/clone` | POST | 克隆验方 | ✓ |
| `/api/v1/formulas/search` | GET | 搜索验方 | ✓ |
| `/api/v1/formulas/import` | POST | Excel批量导入 | ✓ |
| `/api/v1/formulas/export` | GET | 导出验方到Excel | ✓ |
| `/api/v1/formulas/template` | GET | 下载导入模板 | ✓ |
| `/api/v1/formulas/pending-validation` | GET | 获取待验证验方 | ✓ |
| `/api/v1/formulas/{id}/validate-herbs` | POST | 验证验方药材 | ✓ |

---

## 1. 分页查询验方列表

### 请求

```http
GET /api/v1/formulas?page=1&pageSize=20&keyword=四君子&category=补益方&isShared=false
```

### 查询参数

| 参数 | 类型 | 必填 | 说明 | 默认值 | 示例 |
|------|------|------|------|--------|------|
| page | integer | 否 | 页码（从1开始） | 1 | 1 |
| pageSize | integer | 否 | 每页数量 | 20 | 20 |
| keyword | string | 否 | 关键词搜索（名称、功效） | - | 四君子 |
| category | string | 否 | 分类筛选 | - | 补益方 |
| isShared | boolean | 否 | 是否共享验方 | false | false |

### 响应

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "四君子汤",
        "category": "补益方",
        "description": "益气健脾，补中益气",
        "usageInstructions": "水煎服，日一剂",
        "isShared": false,
        "herbCount": 4,
        "totalPrice": 63.60,
        "createdBy": "张医生",
        "createdAt": "2025-01-20T10:30:00",
        "updatedAt": "2025-01-21T15:20:00"
      }
    ],
    "totalCount": 50,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 3
  },
  "message": null,
  "errors": null
}
```

### 响应字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| id | guid | 验方唯一标识 |
| name | string | 方剂名称 |
| category | string | 分类（补益方/清热方等） |
| description | string | 功效说明 |
| usageInstructions | string | 用法用量 |
| isShared | boolean | 是否共享验方 |
| herbCount | integer | 药材数量 |
| totalPrice | decimal | 总价（元） |
| createdBy | string | 创建人 |
| createdAt | datetime | 创建时间 |
| updatedAt | datetime | 更新时间 |

---

## 2. 获取单个验方详情

### 请求

```http
GET /api/v1/formulas/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### 路径参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | guid | ✓ | 验方ID |

### 响应

```json
{
  "isSuccess": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "四君子汤",
    "category": "补益方",
    "description": "益气健脾，补中益气",
    "usageInstructions": "水煎服，日一剂",
    "isShared": false,
    "herbItems": [
      {
        "id": "herb-item-001",
        "herbId": "herb-001",
        "herbName": "人参",
        "dosage": 9,
        "unit": "克",
        "herbPrice": 5.0,
        "subtotal": 45.0,
        "notes": null
      },
      {
        "id": "herb-item-002",
        "herbId": "herb-002",
        "herbName": "白术",
        "dosage": 12,
        "unit": "克",
        "herbPrice": 0.8,
        "subtotal": 9.6,
        "notes": null
      },
      {
        "id": "herb-item-003",
        "herbId": "herb-003",
        "herbName": "茯苓",
        "dosage": 12,
        "unit": "克",
        "herbPrice": 0.5,
        "subtotal": 6.0,
        "notes": null
      },
      {
        "id": "herb-item-004",
        "herbId": "herb-004",
        "herbName": "甘草",
        "dosage": 6,
        "unit": "克",
        "herbPrice": 0.5,
        "subtotal": 3.0,
        "notes": null
      }
    ],
    "herbCount": 4,
    "totalPrice": 63.60,
    "createdBy": "张医生",
    "createdAt": "2025-01-20T10:30:00",
    "updatedAt": "2025-01-21T15:20:00"
  }
}
```

---

## 3. 创建新验方

### 请求

```http
POST /api/v1/formulas
Content-Type: application/json
```

```json
{
  "name": "六君子汤",
  "category": "补益方",
  "description": "益气健脾，燥湿化痰",
  "usageInstructions": "水煎服，日一剂",
  "isShared": false,
  "herbItems": [
    {
      "herbId": "herb-001",
      "dosage": 9,
      "unit": "克",
      "notes": null
    },
    {
      "herbId": "herb-002",
      "dosage": 12,
      "unit": "克"
    },
    {
      "herbId": "herb-003",
      "dosage": 12,
      "unit": "克"
    },
    {
      "herbId": "herb-004",
      "dosage": 6,
      "unit": "克"
    },
    {
      "herbId": "herb-005",
      "dosage": 9,
      "unit": "克"
    },
    {
      "herbId": "herb-006",
      "dosage": 9,
      "unit": "克"
    }
  ]
}
```

### 请求体字段说明

| 字段 | 类型 | 必填 | 说明 | 验证规则 |
|------|------|------|------|---------|
| name | string | ✓ | 方剂名称 | 1-100字符，不能重复 |
| category | string | 否 | 分类 | 最大50字符 |
| description | string | 否 | 功效说明 | 最大500字符 |
| usageInstructions | string | 否 | 用法用量 | 最大200字符 |
| isShared | boolean | 否 | 是否共享 | 默认false |
| herbItems | array | ✓ | 药材列表 | 至少1味药材 |
| herbItems[].herbId | guid | ✓ | 药材ID | 必须存在 |
| herbItems[].dosage | decimal | ✓ | 用量 | 大于0 |
| herbItems[].unit | string | ✓ | 单位 | 默认"克" |
| herbItems[].notes | string | 否 | 备注 | 最大200字符 |

### 响应

```json
{
  "isSuccess": true,
  "data": {
    "id": "new-formula-guid",
    "name": "六君子汤",
    "category": "补益方",
    "herbCount": 6,
    "totalPrice": 81.60,
    "createdAt": "2025-01-22T10:00:00"
  },
  "message": "验方创建成功"
}
```

---

## 4. 更新验方信息

### 请求

```http
PUT /api/v1/formulas/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json
```

请求体与创建验方相同，但所有字段均为可选（仅更新提供的字段）。

### 响应

```json
{
  "isSuccess": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "六君子汤",
    "updatedAt": "2025-01-22T11:00:00"
  },
  "message": "验方更新成功"
}
```

---

## 5. 删除验方（软删除）

### 请求

```http
DELETE /api/v1/formulas/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### 响应

```http
HTTP/1.1 204 No Content
```

或

```json
{
  "isSuccess": true,
  "message": "验方删除成功"
}
```

---

## 6. 克隆验方

### 请求

```http
POST /api/v1/formulas/3fa85f64-5717-4562-b3fc-2c963f66afa6/clone?newName=六君子汤
```

### 查询参数

| 参数 | 类型 | 必填 | 说明 | 默认值 |
|------|------|------|------|--------|
| newName | string | 否 | 新验方名称 | 原名称_副本 |

### 响应

```json
{
  "isSuccess": true,
  "data": {
    "id": "new-clone-guid",
    "name": "六君子汤",
    "category": "补益方",
    "herbCount": 6,
    "totalPrice": 81.60,
    "isShared": false,
    "createdAt": "2025-01-22T10:30:00"
  },
  "message": "验方克隆成功"
}
```

---

## 7. 搜索验方

### 请求

```http
GET /api/v1/formulas/search?keyword=四君子
```

### 查询参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| keyword | string | ✓ | 搜索关键词（名称/功效） |

### 响应

返回匹配的验方列表（格式同分页查询）。

---

## 8. Excel批量导入

### 请求

```http
POST /api/v1/formulas/import?strategy=Skip
Content-Type: multipart/form-data
```

### 查询参数

| 参数 | 类型 | 必填 | 说明 | 可选值 |
|------|------|------|------|--------|
| strategy | string | 否 | 重复处理策略 | Skip/Update/Error |

### 请求体

```
--boundary
Content-Disposition: form-data; name="file"; filename="formulas.xlsx"
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

[Excel文件二进制数据]
--boundary--
```

### 响应

```json
{
  "isSuccess": true,
  "data": {
    "totalCount": 100,
    "successCount": 85,
    "failureCount": 15,
    "skippedCount": 0,
    "autoMatchedCount": 70,
    "fuzzyMatchedCount": 15,
    "failures": [
      {
        "rowNumber": 3,
        "formulaName": "六味地黄丸",
        "herbName": "熟地黄",
        "reason": "药材不存在",
        "suggestion": "建议添加药材或使用'生地黄'替代"
      }
    ]
  },
  "message": "导入完成：成功85条，失败15条"
}
```

---

## 9. 导出验方到Excel

### 请求

```http
GET /api/v1/formulas/export?category=补益方
```

### 查询参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| category | string | 否 | 分类筛选 |

### 响应

```http
HTTP/1.1 200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="formulas_2025-01-22.xlsx"

[Excel文件二进制数据]
```

---

## 10. 下载导入模板

### 请求

```http
GET /api/v1/formulas/template
```

### 响应

```http
HTTP/1.1 200 OK
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="formula_import_template.xlsx"

[Excel模板文件]
```

---

## 11. 获取待验证验方

### 请求

```http
GET /api/v1/formulas/pending-validation
```

### 响应

```json
{
  "isSuccess": true,
  "data": {
    "totalCount": 5,
    "formulas": [
      {
        "formulaId": "formula-001",
        "formulaName": "六味地黄丸",
        "invalidHerbs": [
          {
            "herbId": "herb-001",
            "herbName": "熟地黄",
            "originalDosage": 15,
            "reason": "药材已删除",
            "suggestedHerbs": [
              {
                "herbId": "herb-002",
                "herbName": "生地黄"
              }
            ]
          }
        ]
      }
    ]
  }
}
```

---

## 12. 验证验方药材

### 请求

```http
POST /api/v1/formulas/3fa85f64-5717-4562-b3fc-2c963f66afa6/validate-herbs
```

### 响应

```json
{
  "isSuccess": true,
  "message": "验方药材验证通过"
}
```

或

```json
{
  "isSuccess": false,
  "message": "验方包含无效药材",
  "errors": [
    "熟地黄已被删除",
    "防风价格过期"
  ]
}
```

---

## 错误响应

### 标准错误格式

```json
{
  "isSuccess": false,
  "data": null,
  "message": "操作失败",
  "errors": [
    "方剂名称不能为空",
    "药材组成至少包含1味药材"
  ]
}
```

### HTTP状态码

| 状态码 | 说明 | 示例 |
|--------|------|------|
| 200 | 成功 | 查询成功 |
| 201 | 创建成功 | 验方创建成功 |
| 204 | 无内容（删除成功） | 验方删除成功 |
| 400 | 请求错误 | 参数验证失败 |
| 401 | 未认证 | Token无效或过期 |
| 403 | 禁止访问 | 无权限执行操作 |
| 404 | 资源不存在 | 验方不存在 |
| 409 | 冲突 | 方剂名称重复 |
| 500 | 服务器错误 | 内部错误 |

### 常见错误码

| 错误码 | 说明 | 解决方法 |
|--------|------|---------|
| FORMULA_NAME_REQUIRED | 方剂名称不能为空 | 提供name字段 |
| FORMULA_NAME_DUPLICATE | 方剂名称重复 | 修改name或删除现有验方 |
| FORMULA_HERBS_REQUIRED | 药材组成不能为空 | 提供至少1味药材 |
| HERB_NOT_FOUND | 药材不存在 | 检查herbId是否正确 |
| FORMULA_NOT_FOUND | 验方不存在 | 检查formulaId是否正确 |
| INVALID_DOSAGE | 用量必须大于0 | 检查dosage值 |
| EXCEL_FORMAT_ERROR | Excel格式错误 | 使用标准模板 |

---

## 请求示例

### cURL示例

```bash
# 1. 获取验方列表
curl -X GET "http://localhost:5000/api/v1/formulas?page=1&pageSize=20" \
  -H "Authorization: Bearer YOUR_TOKEN"

# 2. 创建新验方
curl -X POST "http://localhost:5000/api/v1/formulas" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "四君子汤",
    "category": "补益方",
    "herbItems": [
      {"herbId": "herb-001", "dosage": 9, "unit": "克"}
    ]
  }'

# 3. 克隆验方
curl -X POST "http://localhost:5000/api/v1/formulas/FORMULA_ID/clone?newName=六君子汤" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### JavaScript示例

```javascript
// 使用Fetch API
async function getFormulas(page = 1) {
  const response = await fetch(`/api/v1/formulas?page=${page}&pageSize=20`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return await response.json();
}

// 创建验方
async function createFormula(formulaData) {
  const response = await fetch('/api/v1/formulas', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(formulaData)
  });
  return await response.json();
}
```

### C# 示例 (Refit)

```csharp
public interface IFormulaApiService
{
    [Get("/formulas")]
    Task<ApiResponse<PagedResult<FormulaDto>>> GetPagedAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? keyword = null);

    [Get("/formulas/{id}")]
    Task<ApiResponse<FormulaDto>> GetByIdAsync(Guid id);

    [Post("/formulas")]
    Task<ApiResponse<FormulaDto>> CreateAsync([Body] FormulaInputDto dto);

    [Post("/formulas/{id}/clone")]
    Task<ApiResponse<FormulaDto>> CloneAsync(Guid id, [Query] string? newName = null);
}
```

---

## 相关文档

- [方剂管理教程](../../tutorials/modules/formula/formula-management-tutorial.md)
- [方剂管理问题解决指南](../../how-to-guides/modules/formula/formula-issues-guide.md)
- [方剂管理架构设计](../../explanation/architecture/formula-system/overview.md)
- [方剂模块参考](../modules/formula/README.md)

---

**API版本**: v1.0
**更新日期**: 2025-01-22
**维护团队**: LYBTZYZS开发组
