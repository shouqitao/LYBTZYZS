# 数据同步 API

> 🔴 **v2.0 规划，v1.0 不实现。**
>
> **基线§2 N1 决策（用户 2026-06-28）**：v1.0 远程库与本地库**数据孤立，不互通**。本地模式定位为「远程故障应急降级」，断网期录入的数据事后手动补录或可丢。**无 `SyncController`**，下列 7 端点均无对应实现。
>
> 本文内容**保留作 v2.0 同步模块的设计参考**，不代表 v1.0 可用功能。详见 `docs/compose/specs/2026-06-28-docs-reconciliation-baseline.md` §2。

---

> Controller: `SyncController`（🚧 v2.0 规划，当前不存在）| 路由前缀: `/api/v1/sync` | 默认权限: `[Authorize(Policy = "DoctorOrReceptionist")]`

## 概述

基础数据 (Herb/Patient/Formula) 的双向同步 API。用于本地模式 (SQL Server LocalDB) 与远程服务器 (SQL Server) 之间的数据同步。
同步流程: 获取元数据 -> 比对差异 -> 上传/下载变更。

---

## GET /sync/entity-types

获取支持同步的实体类型列表。

**认证**: DoctorOrReceptionist

**成功响应** (200): `ApiResponse<IReadOnlyList<string>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": ["Herb", "Patient", "Formula"],
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9B..."
}
```

**curl 示例：**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')

curl -X GET http://localhost:5000/api/v1/sync/entity-types \
  -H "Authorization: Bearer $TOKEN"
```

---

## GET /sync/metadata

获取指定实体类型的同步元数据 (用于客户端 Checksum 比对)。

**认证**: DoctorOrReceptionist

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `entityType` | string | 必填，实体类型 (Herb/Patient/Formula) |

**成功响应** (200): `ApiResponse<List<SyncMetadataDto>>`

```json
{
  "success": true,
  "message": "获取成功",
  "data": [
    {
      "entityId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "checksum": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "updatedAt": "2026-06-25T10:30:00Z",
      "isDeleted": false
    },
    {
      "entityId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "checksum": "a7ffc6f8bf1ed76651c14756a061d662f580ff4de43b49fa82d80a4b80f8434a",
      "updatedAt": "2026-06-25T11:00:00Z",
      "isDeleted": false
    }
  ],
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9C..."
}
```

**错误响应**: 400 (实体类型为空或不支持)

```json
{
  "success": false,
  "message": "不支持的实体类型",
  "data": null,
  "errors": ["不支持的实体类型: UnknownType"],
  "timestamp": 1750873200,
  "requestId": "0HN9C..."
}
```

**curl 示例：**

```bash
curl -X GET "http://localhost:5000/api/v1/sync/metadata?entityType=Herb" \
  -H "Authorization: Bearer $TOKEN"
```

---

## POST /sync/compare

比对本地与服务器的数据差异。

**认证**: DoctorOrReceptionist

**请求体** (`SyncCompareInputDto`):

```json
{
  "entityType": "Herb",
  "localMetadata": [
    {
      "entityId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "checksum": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
      "updatedAt": "2026-06-25T10:30:00Z"
    },
    {
      "entityId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "checksum": "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592",
      "updatedAt": "2026-06-24T08:00:00Z"
    }
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `entityType` | string | 是 | 实体类型 (Herb/Patient/Formula) |
| `localMetadata` | array | 是 | 本地元数据列表 |
| `localMetadata[].entityId` | Guid | 是 | 实体 ID |
| `localMetadata[].checksum` | string | 是 | 数据校验和 |
| `localMetadata[].updatedAt` | DateTime | 是 | 最后更新时间 |

**成功响应** (200): `ApiResponse<SyncCompareResultDto>`

```json
{
  "success": true,
  "message": "比对完成",
  "data": {
    "toUpload": ["a1b2c3d4-e5f6-7890-abcd-ef1234567890"],
    "toDownload": ["d4e5f6a7-b8c9-0123-def4-567890abcdef"],
    "toDelete": [],
    "conflicted": [],
    "summary": {
      "totalLocal": 120,
      "totalServer": 118,
      "added": 2,
      "modified": 1,
      "deleted": 0,
      "conflicted": 0
    }
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9D..."
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/sync/compare \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "entityType": "Herb",
    "localMetadata": [
      {
        "entityId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "checksum": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        "updatedAt": "2026-06-25T10:30:00Z"
      }
    ]
  }'
```

---

## POST /sync/upload

上传本地数据到服务器。

**认证**: DoctorOrReceptionist

**请求体** (`SyncUploadInputDto`):

```json
{
  "entityType": "Herb",
  "entities": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "data": {
        "name": "黄芪",
        "pinyin": "HuangQi",
        "category": "补气药",
        "properties": "甘，微温",
        "functions": "补气升阳，固表止汗，利水消肿",
        "dosageRange": "9-30g"
      },
      "checksum": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    }
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `entityType` | string | 是 | 实体类型 (Herb/Patient/Formula) |
| `entities` | array | 是 | 实体数据列表，至少 1 条 |
| `entities[].id` | Guid | 是 | 实体 ID |
| `entities[].data` | object | 是 | 实体数据 (JSON) |
| `entities[].checksum` | string | 是 | 数据校验和 |

**成功响应** (200): `ApiResponse<SyncUploadResultDto>`

```json
{
  "success": true,
  "message": "上传完成",
  "data": {
    "successCount": 5,
    "failureCount": 0,
    "errors": []
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9E..."
}
```

**部分失败响应** (200):

```json
{
  "success": true,
  "message": "上传完成，部分失败",
  "data": {
    "successCount": 4,
    "failureCount": 1,
    "errors": [
      {
        "entityId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
        "error": "服务器已存在该数据"
      }
    ]
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9E..."
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/sync/upload \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "entityType": "Herb",
    "entities": [
      {
        "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "data": {
          "name": "黄芪",
          "pinyin": "HuangQi",
          "category": "补气药"
        },
        "checksum": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
      }
    ]
  }'
```

---

## POST /sync/download

从服务器下载数据。

**认证**: DoctorOrReceptionist

**请求体** (`SyncDownloadInputDto`):

```json
{
  "entityType": "Herb",
  "entityIds": [
    "d4e5f6a7-b8c9-0123-def4-567890abcdef",
    "e5f6a7b8-c9d0-1234-ef01-234567890abc"
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `entityType` | string | 是 | 实体类型 (Herb/Patient/Formula) |
| `entityIds` | Guid[] | 是 | 需要下载的实体 ID 列表，至少 1 个 |

**成功响应** (200): `ApiResponse<SyncDownloadResultDto>`

```json
{
  "success": true,
  "message": "下载完成",
  "data": {
    "entities": [
      {
        "id": "d4e5f6a7-b8c9-0123-def4-567890abcdef",
        "data": {
          "name": "当归",
          "pinyin": "DangGui",
          "category": "补血药",
          "properties": "甘辛温",
          "functions": "补血活血，调经止痛，润肠通便",
          "dosageRange": "6-12g"
        },
        "checksum": "f82731a4b6c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5"
      },
      {
        "id": "e5f6a7b8-c9d0-1234-ef01-234567890abc",
        "data": {
          "name": "白术",
          "pinyin": "BaiZhu",
          "category": "补气药",
          "properties": "苦甘温",
          "functions": "健脾益气，燥湿利水，止汗",
          "dosageRange": "6-12g"
        },
        "checksum": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2"
      }
    ],
    "totalCount": 2
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9F..."
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/sync/download \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "entityType": "Herb",
    "entityIds": [
      "d4e5f6a7-b8c9-0123-def4-567890abcdef",
      "e5f6a7b8-c9d0-1234-ef01-234567890abc"
    ]
  }'
```

---

## POST /sync/delete

同步删除操作 (带引用检查)。

**认证**: DoctorOrReceptionist

**请求体** (`SyncDeleteInputDto`):

```json
{
  "entityType": "Herb",
  "entityIds": [
    "f6a7b8c9-d0e1-2345-ef01-234567890abc",
    "a7b8c9d0-e1f2-3456-0f12-34567890abcd"
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `entityType` | string | 是 | 实体类型 (Herb/Patient/Formula) |
| `entityIds` | Guid[] | 是 | 需要删除的实体 ID 列表，至少 1 个 |

**成功响应** (200): `ApiResponse<SyncDeleteResultDto>`

```json
{
  "success": true,
  "message": "删除完成",
  "data": {
    "successCount": 2,
    "failureCount": 0,
    "errors": []
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9G..."
}
```

**部分失败响应** (200):

```json
{
  "success": true,
  "message": "删除完成，部分失败",
  "data": {
    "successCount": 1,
    "failureCount": 1,
    "errors": [
      {
        "entityId": "a7b8c9d0-e1f2-3456-0f12-34567890abcd",
        "error": "药材被处方引用，无法删除"
      }
    ]
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9G..."
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/sync/delete \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "entityType": "Herb",
    "entityIds": [
      "f6a7b8c9-d0e1-2345-ef01-234567890abc"
    ]
  }'
```

---

## 典型同步工作流

```
1. GET  /sync/entity-types          -- 获取支持的实体类型
2. GET  /sync/metadata?entityType=Herb -- 获取服务端元数据
3. POST /sync/compare               -- 客户端发送本地元数据，比对差异
4. POST /sync/upload                 -- 上传本地新增/修改的数据
5. POST /sync/download              -- 下载服务端新增/修改的数据
6. POST /sync/delete                -- 同步删除操作
```

---

## 错误码

> 完整错误码定义见 [PRD v2.0 规划范围](../02-requirements/01-prd.md#v20-规划范围)。错误码分区: 7xxxx。

### 服务端通用 (701xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-70101 | UnsupportedEntityType | 400 | 不支持的实体类型 | GET /metadata, POST /compare, POST /upload |
| ERR-70102 | JsonDeserializeFailed | 400 | JSON 反序列化失败 | POST /upload |
| ERR-70103 | SyncDataConflict | 409 | 服务器已存在该数据 | POST /upload (OverwriteConflicts=false) |

### 上传错误 (702xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-70201 | HerbUploadFailed | 500 | 上传异常 | POST /upload (Herb) |
| ERR-70202 | PatientUploadFailed | 500 | 上传异常 | POST /upload (Patient) |
| ERR-70203 | FormulaUploadFailed | 500 | 上传异常 | POST /upload (Formula) |
| ERR-70204 | MedicalCaseUploadFailed | 500 | 上传异常 | POST /upload (MedicalCase) |

### MedicalCase 特有 (703xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-70301 | SyncPatientNotFound | 422 | 患者不存在，请先同步患者 | POST /upload (MedicalCase) |
| ERR-70302 | SyncHerbNotFound | 422 | 药材不存在，请先同步药材 | POST /upload (MedicalCase) |
| ERR-70304 | SyncCaseLocked | 422 | 医案已锁定 | POST /upload (MedicalCase) |

### 删除错误 (704xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发端点 |
|--------|--------|------|----------|----------|
| ERR-70401 | SyncReferenceCheckFailed | 500 | 引用检查异常 | POST /delete |
| ERR-70402 | SyncHerbHasReference | 422 | 药材被处方引用 | POST /delete (Herb) |
| ERR-70403 | SyncPatientHasReference | 422 | 患者有医案记录 | POST /delete (Patient) |
| ERR-70404 | SyncEntityNotFound | 404 | 实体不存在或已删除 | POST /delete |

### 客户端错误 (705xx)

> 客户端同步流程中由 Desktop 端 SyncViewModel 检查和抛出的错误。

| 错误码 | 枚举名 | 用户消息 | 触发条件 |
|--------|--------|----------|----------|
| ERR-70501 | SyncNoEntityTypeSelected | 请选择要同步的数据类型 | UI 中未选择 EntityType |
| ERR-70502 | SyncFailed | 同步失败: {错误列表} | 服务返回失败结果 |
| ERR-70503 | SyncChecksumTypeError | 不支持的实体类型: {entityType} | 计算 Checksum 时类型无效 |
| ERR-70504 | SyncDependencyNotSynced | 请先同步药材和患者数据 | MedicalCase 同步前依赖检查失败 |
| ERR-70505 | SyncPatientRemapFailed | 无法匹配患者 {PatientName}，请手动处理 | IdCardNumber 匹配失败 (本地患者无身份证号) |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，6 个端点 |
| 2026-02-18 | v1.1 | 新增错误码章节: 补充端点级 MCCEE 错误码 (ERR-70101~70404)，含通用/上传/MedicalCase/删除四类 |
| 2026-02-19 | v1.2 | 补充客户端错误码 (ERR-70501~70505)，含 UI 校验/同步失败/依赖检查/患者匹配 |
| 2026-06-12 | v1.3 | 移除 ERR-70303 SyncActiveCaseConflict (PRD 已删除) |
| 2026-06-25 | v1.4 | 补充全部端点的 curl 示例、`ApiResponse<T>` 信封、真实 JSON 响应示例 |
| 2026-06-28 | v2.0-note | 文档对齐基线：顶部加 v2.0 规划醒目标注（N1 决策：v1.0 远程与本地数据孤立，不互通）；无 SyncController |