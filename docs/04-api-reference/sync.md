# 数据同步 API

> Controller: `SyncController` | 路由前缀: `/api/v1/sync` | 默认权限: `[Authorize(Policy = "DoctorOrAdmin")]`

## 概述

基础数据 (Herb/Patient/Formula) 的双向同步 API。用于本地模式 (SQLite) 与远程服务器 (SQL Server) 之间的数据同步。
同步流程: 获取元数据 -> 比对差异 -> 上传/下载变更。

---

## GET /sync/entity-types

获取支持同步的实体类型列表。

**成功响应** (200): `ApiResponse<IReadOnlyList<string>>`

```json
{
  "data": ["Herb", "Patient", "Formula"]
}
```

---

## GET /sync/metadata

获取指定实体类型的同步元数据 (用于客户端 Checksum 比对)。

**查询参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `entityType` | string | 必填，实体类型 (Herb/Patient/Formula) |

**成功响应** (200): `ApiResponse<List<SyncMetadataDto>>`

```json
{
  "data": [
    {
      "entityId": "guid",
      "checksum": "string",     // 数据校验和
      "updatedAt": "datetime",
      "isDeleted": false
    }
  ]
}
```

**错误响应**: 400 (实体类型为空或不支持)

---

## POST /sync/compare

比对本地与服务器的数据差异。

**请求体** (`SyncCompareInputDto`):

```json
{
  "entityType": "string",       // 必填
  "localMetadata": [
    {
      "entityId": "guid",
      "checksum": "string",
      "updatedAt": "datetime"
    }
  ]
}
```

**成功响应** (200): `ApiResponse<SyncCompareResultDto>`

```json
{
  "data": {
    "toUpload": ["guid1", "guid2"],    // 需要上传到服务器的
    "toDownload": ["guid3", "guid4"],  // 需要从服务器下载的
    "toDelete": ["guid5"],             // 需要删除的
    "conflicted": ["guid6"],           // 冲突项
    "summary": {
      "totalLocal": 100,
      "totalServer": 95,
      "added": 5,
      "modified": 3,
      "deleted": 2,
      "conflicted": 1
    }
  }
}
```

---

## POST /sync/upload

上传本地数据到服务器。

**请求体** (`SyncUploadInputDto`):

```json
{
  "entityType": "string",     // 必填
  "entities": [               // 必填，至少 1 条
    {
      "id": "guid",
      "data": { ... },        // 实体数据 (JSON)
      "checksum": "string"
    }
  ]
}
```

**成功响应** (200): `ApiResponse<SyncUploadResultDto>`

```json
{
  "data": {
    "successCount": 5,
    "failureCount": 0,
    "errors": []
  }
}
```

---

## POST /sync/download

从服务器下载数据。

**请求体** (`SyncDownloadInputDto`):

```json
{
  "entityType": "string",           // 必填
  "entityIds": ["guid1", "guid2"]   // 必填，至少 1 个
}
```

**成功响应** (200): `ApiResponse<SyncDownloadResultDto>`

```json
{
  "data": {
    "entities": [
      {
        "id": "guid",
        "data": { ... },
        "checksum": "string"
      }
    ],
    "totalCount": 2
  }
}
```

---

## POST /sync/delete

同步删除操作 (带引用检查)。

**请求体** (`SyncDeleteInputDto`):

```json
{
  "entityType": "string",           // 必填
  "entityIds": ["guid1", "guid2"]   // 必填，至少 1 个
}
```

**成功响应** (200): `ApiResponse<SyncDeleteResultDto>`

```json
{
  "data": {
    "successCount": 2,
    "failureCount": 0,
    "errors": []
  }
}
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

> 完整错误码定义见 [sync.md PRD](../02-requirements/sync.md)。错误码分区: 7xxxx。

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
