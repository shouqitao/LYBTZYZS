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

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，6 个端点 |
