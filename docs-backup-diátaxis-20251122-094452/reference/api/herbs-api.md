# Herbs API 参考文档

**版本**: v1.1 (Epic #1962)
**基础路径**: `/api/v1/herbs`
**认证方式**: Bearer Token (JWT)
**Epic来源**: 
- #1600 - Server端重构（基础CRUD）
- #1962 - 药材管理增强（批量导入/导出、分类管理、引用检查）

---

## 📋 目录

- [概述](#概述)
- [基础CRUD操作](#基础crud操作)
  - [GET /api/v1/herbs](#1-get-apiv1herbs---分页查询药材)
  - [GET /api/v1/herbs/{id}](#2-get-apiv1herbsid---获取单个药材)
  - [POST /api/v1/herbs](#3-post-apiv1herbs---创建药材)
  - [PUT /api/v1/herbs/{id}](#4-put-apiv1herbsid---更新药材)
  - [DELETE /api/v1/herbs/{id}](#5-delete-apiv1herbsid---删除药材)
- [批量操作（Epic #1962）](#批量操作epic-1962)
  - [POST /api/v1/herbs/batch-import](#6-post-apiv1herbsbatch-import---批量导入药材)
  - [GET /api/v1/herbs/export-all](#7-get-apiv1herbsexport-all---导出所有药材)
  - [POST /api/v1/herbs/batch-delete](#8-post-apiv1herbsbatch-delete---批量删除药材)
- [引用检查（Epic #1962）](#引用检查epic-1962)
  - [GET /api/v1/herbs/{id}/check-reference](#9-get-apiv1herbsidcheck-reference---检查单个药材引用)
  - [POST /api/v1/herbs/batch-check-reference](#10-post-apiv1herbsbatch-check-reference---批量检查药材引用)
- [模板与导入导出](#模板与导入导出)
  - [GET /api/v1/herbs/import-template](#11-get-apiv1herbsimport-template---导出导入模板)
  - [POST /api/v1/herbs/import](#12-post-apiv1herbsimport---导入药材旧版)
  - [GET /api/v1/herbs/export](#13-get-apiv1herbsexport---导出药材旧版)
- [通用响应格式](#通用响应格式)
- [业务规则说明](#业务规则说明)
- [性能基准](#性能基准)

---

## 概述

### 架构设计原则

Herbs API遵循三层对齐架构和MVP原则：

- **基础CRUD**: 标准的增删改查操作
- **批量操作**: Desktop-Led模式（Excel处理在Client端，Server端处理业务逻辑）
- **引用检查**: 跨模块依赖Prescriptions模块，支持软删除（BR-007）
- **分类管理**: 支持按分类分组管理药材（Epic #1962 新增）

### 核心业务规则

#### 数据验证规则

- **BR-001**: 药材名称1-50字符，必填
- **BR-002**: 药材名称唯一性约束（同一名称只能存在一条有效记录）
- **BR-003**: 分类字段可选，最大50字符
- **BR-004**: 拼音码自动生成（调用PinYinHelper工具类）

#### 批量操作规则

- **BR-006**: 批量操作限制
  - 批量导入：单次最多10000条
  - 批量删除：单次最多100条
  - 批量引用检查：单次最多100条

#### 软删除规则

- **BR-007**: 软删除支持
  - 即使被处方引用也可删除（设置IsDeleted=true）
  - 删除后不影响已开具的处方
  - CanDelete字段总是返回true

#### 性能规则

- **BR-008**: 性能要求
  - 批量导入1000条 < 10秒
  - 导出10000条 < 2秒
  - 引用检查单次 < 500ms

---

## 基础CRUD操作

### 1. GET /api/v1/herbs - 分页查询药材

**描述**: 分页查询药材列表，支持按分类过滤。

**业务规则**:
- 默认每页20条记录
- 仅返回未删除的药材（IsDeleted=false）
- 支持Category分类过滤

**请求**:
```http
GET /api/v1/herbs?pageIndex=1&pageSize=20&category=补血药
Authorization: Bearer {token}
```

**查询参数**:

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| pageIndex | int | ❌ | 1 | 页码（从1开始） |
| pageSize | int | ❌ | 20 | 每页记录数 |
| category | string | ❌ | null | 分类过滤（如："补血药"） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "当归",
        "pinYinCode": "DG",
        "category": "补血药",
        "origin": "甘肃",
        "price": 25.50,
        "unit": "克",
        "status": 1,
        "remark": "补血活血，调经止痛",
        "createdAt": "2025-01-01T10:00:00Z",
        "updatedAt": "2025-01-15T14:30:00Z"
      }
    ],
    "totalCount": 156,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

---

### 2. GET /api/v1/herbs/{id} - 获取单个药材

**描述**: 根据药材ID获取药材详情。

**请求**:
```http
GET /api/v1/herbs/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| id | Guid | ✅ | 药材ID |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "当归",
    "pinYinCode": "DG",
    "category": "补血药",
    "origin": "甘肃",
    "price": 25.50,
    "unit": "克",
    "status": 1,
    "remark": "补血活血，调经止痛",
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-01-15T14:30:00Z"
  }
}
```

❌ **失败 - 404 Not Found**
```json
{
  "success": false,
  "message": "药材不存在"
}
```

---

### 3. POST /api/v1/herbs - 创建药材

**描述**: 创建新的药材记录。

**业务规则**:
- **BR-001**: 药材名称1-50字符，必填
- **BR-002**: 药材名称唯一性检查
- **BR-004**: 拼音码自动生成（无需手动输入）

**请求**:
```http
POST /api/v1/herbs
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "黄芪",
  "category": "补气药",
  "origin": "内蒙古",
  "price": 18.00,
  "unit": "克",
  "status": 1,
  "remark": "补气升阳，固表止汗"
}
```

**请求体**:

| 字段 | 类型 | 必填 | 长度 | 说明 |
|------|------|------|------|------|
| name | string | ✅ | 1-50 | 药材名称（BR-001） |
| category | string | ❌ | 0-50 | 分类（BR-003） |
| origin | string | ❌ | 0-100 | 产地 |
| price | decimal | ❌ | - | 价格 |
| unit | string | ❌ | 0-20 | 单位 |
| status | int | ❌ | - | 状态（0=禁用，1=启用） |
| remark | string | ❌ | 0-500 | 备注 |

**响应**:

✅ **成功 - 201 Created**
```json
{
  "success": true,
  "message": "创建成功",
  "data": {
    "id": "7c8e9a1b-2c3d-4e5f-6a7b-8c9d0e1f2a3b",
    "name": "黄芪",
    "pinYinCode": "HQ",
    "category": "补气药",
    "origin": "内蒙古",
    "price": 18.00,
    "unit": "克",
    "status": 1,
    "remark": "补气升阳，固表止汗",
    "createdAt": "2025-11-10T10:00:00Z",
    "updatedAt": "2025-11-10T10:00:00Z"
  }
}
```

❌ **失败 - 400 Bad Request**
```json
{
  "success": false,
  "message": "药材名称已存在",
  "errors": {
    "Name": ["药材'黄芪'已存在，请使用其他名称"]
  }
}
```

---

### 4. PUT /api/v1/herbs/{id} - 更新药材

**描述**: 更新现有药材信息。

**业务规则**:
- **BR-001**: 药材名称1-50字符
- **BR-002**: 药材名称唯一性检查（排除自身ID）
- **BR-004**: 拼音码自动更新

**请求**:
```http
PUT /api/v1/herbs/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "当归",
  "category": "补血药",
  "origin": "甘肃岷县",
  "price": 28.00,
  "unit": "克",
  "status": 1,
  "remark": "补血活血，调经止痛，润肠通便"
}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "更新成功",
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "当归",
    "pinYinCode": "DG",
    "category": "补血药",
    "origin": "甘肃岷县",
    "price": 28.00,
    "unit": "克",
    "status": 1,
    "remark": "补血活血，调经止痛，润肠通便",
    "createdAt": "2025-01-01T10:00:00Z",
    "updatedAt": "2025-11-10T10:30:00Z"
  }
}
```

---

### 5. DELETE /api/v1/herbs/{id} - 删除药材

**描述**: 软删除药材记录（设置IsDeleted=true）。

**业务规则**:
- **BR-007**: 即使被处方引用也可删除（软删除）
- 删除后不影响已开具的处方

**请求**:
```http
DELETE /api/v1/herbs/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "删除成功"
}
```

---

## 批量操作（Epic #1962）

### 6. POST /api/v1/herbs/batch-import - 批量导入药材

**描述**: 批量导入药材数据（Desktop层提供DTO列表）。

**业务规则**:
- **BR-006**: 单次最多导入10000条
- **BR-001**: 每条记录的名称1-50字符
- **BR-002**: 支持3种重复策略（Skip/Update/Error）
- **BR-004**: 拼音码自动生成

**重复策略说明**:

| 策略 | 枚举值 | 遇到重复时 | 适用场景 |
|-----|--------|-----------|---------|
| Skip | 0 | 跳过，不导入 | 追加新数据，保留旧数据 |
| Update | 1 | 更新现有记录 | 数据同步，覆盖旧数据 |
| Error | 2 | 报错，全部回滚 | 严格数据检查，不允许重复 |

**请求**:
```http
POST /api/v1/herbs/batch-import
Authorization: Bearer {token}
Content-Type: application/json

{
  "items": [
    {
      "name": "当归",
      "category": "补血药",
      "origin": "甘肃",
      "price": 25.50,
      "unit": "克",
      "remark": "补血活血"
    },
    {
      "name": "黄芪",
      "category": "补气药",
      "origin": "内蒙古",
      "price": 18.00,
      "unit": "克",
      "remark": "补气升阳"
    }
  ],
  "duplicateStrategy": 0
}
```

**请求体**:

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| items | array | ✅ | 药材列表（≤10000条） |
| duplicateStrategy | int | ✅ | 重复策略（0=Skip, 1=Update, 2=Error） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "批量导入完成",
  "data": {
    "totalCount": 2,
    "successCount": 1,
    "skippedCount": 1,
    "failedCount": 0,
    "skippedItems": [
      {
        "herbName": "当归",
        "reason": "药材已存在（重复策略：跳过）"
      }
    ],
    "importTime": "2025-11-10T10:00:00Z"
  }
}
```

❌ **失败 - 400 Bad Request**
```json
{
  "success": false,
  "message": "批量导入数量不能超过10000条",
  "errors": {
    "Items": ["当前提交了15000条记录，超出限制"]
  }
}
```

---

### 7. GET /api/v1/herbs/export-all - 导出所有药材

**描述**: 导出所有药材数据（JSON格式，Desktop层负责生成Excel）。

**业务规则**:
- **BR-008**: 10000条记录 < 2秒
- 使用AsNoTracking()提升性能
- 支持Category分类过滤

**请求**:
```http
GET /api/v1/herbs/export-all?category=补血药
Authorization: Bearer {token}
```

**查询参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| category | string | ❌ | 分类过滤（如："补血药"） |

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "导出成功",
  "data": [
    {
      "name": "当归",
      "pinYinCode": "DG",
      "category": "补血药",
      "origin": "甘肃",
      "price": 25.50,
      "unit": "克",
      "status": 1,
      "remark": "补血活血，调经止痛"
    },
    {
      "name": "熟地黄",
      "pinYinCode": "SDH",
      "category": "补血药",
      "origin": "河南",
      "price": 22.00,
      "unit": "克",
      "status": 1,
      "remark": "补血滋阴，益精填髓"
    }
  ]
}
```

---

### 8. POST /api/v1/herbs/batch-delete - 批量删除药材

**描述**: 批量软删除药材记录。

**业务规则**:
- **BR-006**: 单次最多删除100条
- **BR-007**: 即使被引用也可删除（软删除）
- 事务保证：全部成功或全部回滚

**请求**:
```http
POST /api/v1/herbs/batch-delete
Authorization: Bearer {token}
Content-Type: application/json

[
  "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "7c8e9a1b-2c3d-4e5f-6a7b-8c9d0e1f2a3b"
]
```

**请求体**: Guid数组（药材ID列表）

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "批量删除完成",
  "data": {
    "totalCount": 2,
    "successCount": 2,
    "failedCount": 0
  }
}
```

❌ **失败 - 400 Bad Request**
```json
{
  "success": false,
  "message": "批量删除数量不能超过100条"
}
```

---

## 引用检查（Epic #1962）

### 9. GET /api/v1/herbs/{id}/check-reference - 检查单个药材引用

**描述**: 检查药材是否被处方引用，返回引用统计和最近引用记录。

**业务规则**:
- **BR-007**: CanDelete始终返回true（支持软删除）
- **BR-008**: 单次检查 < 500ms
- 跨模块依赖：查询Prescriptions模块数据

**请求**:
```http
GET /api/v1/herbs/3fa85f64-5717-4562-b3fc-2c963f66afa6/check-reference
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK（有引用）**
```json
{
  "success": true,
  "message": "检查完成",
  "data": {
    "herbId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "herbName": "当归",
    "totalReferenceCount": 15,
    "prescriptionCount": 15,
    "canDelete": true,
    "deleteRestriction": "该药材被15个处方引用，仅可软删除",
    "recentReferences": [
      {
        "prescriptionNumber": "RX20251110001",
        "patientName": "张三",
        "prescribedDate": "2025-11-10T09:00:00Z",
        "quantity": 10,
        "herbName": "当归"
      },
      {
        "prescriptionNumber": "RX20251109015",
        "patientName": "李四",
        "prescribedDate": "2025-11-09T15:30:00Z",
        "quantity": 15,
        "herbName": "当归"
      }
    ]
  }
}
```

✅ **成功 - 200 OK（无引用）**
```json
{
  "success": true,
  "message": "检查完成",
  "data": {
    "herbId": "7c8e9a1b-2c3d-4e5f-6a7b-8c9d0e1f2a3b",
    "herbName": "黄芪",
    "totalReferenceCount": 0,
    "prescriptionCount": 0,
    "canDelete": true,
    "deleteRestriction": null,
    "recentReferences": []
  }
}
```

---

### 10. POST /api/v1/herbs/batch-check-reference - 批量检查药材引用

**描述**: 批量检查药材引用情况。

**业务规则**:
- **BR-006**: 单次最多检查100条
- **BR-007**: CanDelete始终为true

**请求**:
```http
POST /api/v1/herbs/batch-check-reference
Authorization: Bearer {token}
Content-Type: application/json

{
  "herbIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "7c8e9a1b-2c3d-4e5f-6a7b-8c9d0e1f2a3b"
  ]
}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "批量检查完成",
  "data": [
    {
      "herbId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "herbName": "当归",
      "totalReferenceCount": 15,
      "canDelete": true,
      "deleteRestriction": "该药材被15个处方引用，仅可软删除"
    },
    {
      "herbId": "7c8e9a1b-2c3d-4e5f-6a7b-8c9d0e1f2a3b",
      "herbName": "黄芪",
      "totalReferenceCount": 0,
      "canDelete": true,
      "deleteRestriction": null
    }
  ]
}
```

---

## 模板与导入导出

### 11. GET /api/v1/herbs/import-template - 导出导入模板

**描述**: 下载Excel导入模板（Desktop层生成）。

**请求**:
```http
GET /api/v1/herbs/import-template
Authorization: Bearer {token}
```

**响应**:

✅ **成功 - 200 OK**
```json
{
  "success": true,
  "message": "模板生成成功",
  "data": {
    "fileName": "药材导入模板_20251110.xlsx",
    "fileSize": 15360,
    "downloadUrl": null,
    "note": "模板由Desktop层生成，包含示例数据"
  }
}
```

---

### 12. POST /api/v1/herbs/import - 导入药材（旧版）

**描述**: 旧版导入接口，已被batch-import替代。

**状态**: ⚠️ 已弃用，建议使用 `POST /api/v1/herbs/batch-import`

---

### 13. GET /api/v1/herbs/export - 导出药材（旧版）

**描述**: 旧版导出接口，已被export-all替代。

**状态**: ⚠️ 已弃用，建议使用 `GET /api/v1/herbs/export-all`

---

## 通用响应格式

所有API端点返回统一的响应格式：

```json
{
  "success": true/false,
  "message": "操作结果描述",
  "data": { ... },
  "errors": { ... }
}
```

**字段说明**:

| 字段 | 类型 | 说明 |
|------|------|------|
| success | boolean | 操作是否成功 |
| message | string | 操作结果描述 |
| data | object | 成功时返回的数据 |
| errors | object | 失败时返回的错误详情 |

---

## 业务规则说明

### BR-001: 药材名称约束
- 长度：1-50字符
- 必填
- 数据库字段：nvarchar(50)

### BR-002: 药材名称唯一性
- 同一名称只能存在一条有效记录（IsDeleted=false）
- 数据库索引：IX_Herbs_Name（唯一索引，带IsDeleted过滤）

### BR-003: 分类字段
- 长度：0-50字符
- 可选
- 示例：补血药、补气药、清热药

### BR-004: 拼音码自动生成
- 调用工具类：LYBT.Shared.Utilities.Text.PinYinHelper
- 基于药材名称自动生成
- 示例：当归 → DG，黄芪 → HQ

### BR-006: 批量操作限制
- 批量导入：≤10000条
- 批量删除：≤100条
- 批量引用检查：≤100条
- 超出限制返回400错误

### BR-007: 软删除支持
- 即使被处方引用也可删除
- 删除方式：设置IsDeleted=true
- CanDelete字段总是返回true
- 删除后不影响已开具的处方

### BR-008: 性能要求
- 批量导入1000条 < 10秒
- 导出10000条 < 2秒
- 引用检查单次 < 500ms

---

## 性能基准

**测试环境**: Intel Core i7-7700 CPU 3.60GHz, .NET 8.0.21, InMemory Database
**测试工具**: BenchmarkDotNet v0.14.0
**测试配置**: IterationCount=10, WarmupCount=3

### 性能测试结果

| 操作 | 平均耗时 | 内存分配 | 备注 |
|------|---------|---------|------|
| 分页查询（100条取20条） | 135.33 μs | 66.05 KB | 包含类别关联查询 |
| 单条创建 | 10.37 ms | 4.75 MB | 性能表现最优 ⭐ |
| 批量导入（1000条模拟） | 246.76 ms | 173.3 MB | Desktop端主导模式 |

**性能对比**（与其他模块）:
- **分页查询**: Users最快（91μs） < Patients（99μs） < Herbs最慢（135μs）
- **单条创建**: **Herbs最快（10ms）** ⭐ < Patients（16ms） < Users（18ms）
- **批量导入**: Users最快（237ms） < Herbs（247ms） < Patients最慢（330ms）

---

## 错误码参考

| HTTP状态码 | 错误类型 | 示例 |
|-----------|---------|------|
| 400 | 请求参数错误 | 药材名称超出长度限制 |
| 401 | 未授权 | Token过期或无效 |
| 404 | 资源不存在 | 药材ID不存在 |
| 409 | 数据冲突 | 药材名称已存在 |
| 500 | 服务器错误 | 数据库连接失败 |

---

## 相关文档

- **[Herbs模块架构文档](../../explanation/architecture/server/modules/herbs.md)** - 完整架构设计
- **[批量操作模式](../../how-to/patterns/batch-operations.md)** - 批量导入/导出实现模式
- **[跨模块依赖](../../explanation/architecture/shared/cross-module-dependencies.md)** - Herbs → Prescriptions依赖说明
- **[MedicalCase API](./medicalcase-api.md)** - 处方创建相关API

---

## 版本历史

| 版本 | 日期 | 作者 | 变更说明 |
|------|------|------|---------|
| v1.1 | 2025-11-10 | Claude | Epic #1962 - 新增批量导入/导出、引用检查端点 |
| v1.0 | 2025-10-15 | Claude | 初始版本（基础CRUD） |

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
