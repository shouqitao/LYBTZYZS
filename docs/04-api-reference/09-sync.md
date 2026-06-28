# 数据同步 API

> 🔴 **v2.0 规划，v1.0 不实现。**
>
> **基线§2 N1 决策（用户 2026-06-28）**：v1.0 远程库与本地库**数据孤立，不互通**。本地模式定位为「远程故障应急降级」，断网期录入的数据事后手动补录或可丢。**无 `SyncController`**，下列 6 端点均无对应实现。
>
> 本文内容**保留作 v2.0 同步模块的设计参考**，不代表 v1.0 可用功能。详见 `docs/compose/specs/2026-06-28-docs-reconciliation-baseline.md` §2 及 `docs/03-architecture/05-dual-mode.md` 的 v2.0 同步协议规划。

---

> Controller: `SyncController`（🚧 v2.0 规划，当前不存在）| 路由前缀: `/api/v1/sync` | 默认权限: `[Authorize(Policy = "DoctorOrReceptionist")]`

## 概述

基础数据 (Herb/Patient/Formula) 的双向同步 API。用于本地模式 (SQL Server LocalDB) 与远程服务器 (SQL Server) 之间的数据同步。同步流程: 获取元数据 → 比对差异 → 上传/下载变更。

## 端点骨架

| 方法 | 路径 | 请求体/参数 | 响应 data | 说明 |
|------|------|------------|----------|------|
| GET | `/sync/entity-types` | — | `List<string>` (Herb/Patient/Formula) | 获取支持同步的实体类型 |
| GET | `/sync/metadata` | `?entityType=Herb` | `List<SyncMetadataDto>` (entityId/checksum/updatedAt/isDeleted) | 获取同步元数据 |
| POST | `/sync/compare` | `SyncCompareInputDto` (entityType + localMetadata[]) | `SyncCompareResultDto` (toUpload/toDownload/toDelete/conflicted + summary) | 比对本地与服务器差异 |
| POST | `/sync/upload` | `SyncUploadInputDto` (entityType + entities[id/data/checksum][]) | `SyncUploadResultDto` (successCount/failureCount/errors[]) | 上传本地数据 |
| POST | `/sync/download` | `SyncDownloadInputDto` (entityType + entityIds[]) | `SyncDownloadResultDto` (entities[id/data/checksum][] + totalCount) | 下载服务端数据 |
| POST | `/sync/delete` | `SyncDeleteInputDto` (entityType + entityIds[]) | `SyncDeleteResultDto` (successCount/failureCount/errors[]) | 同步删除（带引用检查）|

## 典型同步流程

```
1. GET  /sync/entity-types
2. GET  /sync/metadata?entityType=Herb
3. POST /sync/compare
4. POST /sync/upload
5. POST /sync/download
6. POST /sync/delete
```

## 错误码

> 完整错误码定义见 [PRD v2.0 规划范围](../02-requirements/01-prd.md#v20-规划范围)。错误码分区: 7xxxx。通用状态码（401/403）见 [README](README.md#通用-http-状态码)。

### 服务端 (701xx)

| 错误码 | 枚举名 | HTTP | 用户消息 |
|--------|--------|------|----------|
| ERR-70101 | UnsupportedEntityType | 400 | 不支持的实体类型 |
| ERR-70102 | JsonDeserializeFailed | 400 | JSON 反序列化失败 |
| ERR-70103 | SyncDataConflict | 409 | 服务器已存在该数据 |

### 上传/MC/删除特有 (702xx-704xx)

| 错误码 | 枚举名 | HTTP | 用户消息 |
|--------|--------|------|----------|
| ERR-70301 | SyncPatientNotFound | 422 | 患者不存在，请先同步患者 |
| ERR-70302 | SyncHerbNotFound | 422 | 药材不存在，请先同步药材 |
| ERR-70304 | SyncCaseLocked | 422 | 医案已锁定 |
| ERR-70402 | SyncHerbHasReference | 422 | 药材被处方引用 |
| ERR-70403 | SyncPatientHasReference | 422 | 患者有医案记录 |
| ERR-70404 | SyncEntityNotFound | 404 | 实体不存在或已删除 |

> 完整错误码（含 702xx 上传失败、70401 引用检查异常、705xx 客户端错误）见 v2.0 同步协议设计文档。

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-28 | v2.1 | 文档结构优化批次1：v2.0 虚构示例（请求体/响应/curl）删除，改为端点骨架表 + 错误码分区简化；详见 v2.0 同步协议设计 |
| 2026-06-28 | v2.0-note | 文档对齐基线：顶部加 v2.0 规划醒目标注（N1 决策） |
