# 备份/恢复 API

> 版本: v1.0 | 日期: 2026-09-22

> **用户速览**：数据库备份与恢复接口（B-06 / US-SHELL-013）。sysadmin 可查看备份状态、手动备份（全量/差异、可选压缩与加密）、整库或选择性恢复、删除与清理超期备份；登录后的自动备份走 `POST /backup/auto`（任何已认证用户可调用，受 24 小时间隔判定）。

## 基本信息

| 属性 | 值 |
| ------ | ----- |
| Controller | `BackupController`（双端均继承共享基类 `BaseBackupController`） |
| 路由前缀 | `/api/v1/backup` |
| 默认权限 | 类级 `[Authorize]`（仅需已认证）；列表/状态/表清单/创建/恢复/删除/清理逐方法 `[Authorize(Policy = SysAdminOnly)]`；`POST /auto` 仅需已认证 |
| 引擎 | `IBackupService` → `SqlServerBackupService`（远程宿主对 SQL Server、本地宿主对 LocalDB 执行同一套 T-SQL 备份/恢复） |
| 备份目录 | `Backup:Directory`；未配置时远程默认 `{应用基目录}/backup`，桌面默认 `%LOCALAPPDATA%\LYBT\Desktop\Backup` |
| 保留期 | `Backup:RetentionDays`（默认 7 天） |

> **双端一致**：远程 `LYBT.WebAPI` 与本地 `LYBT.LocalWebAPI` 均直接继承 `BaseBackupController`，路由/权限/契约唯一来源（ADR-0010/0023）。运维视角见 [06-operations/06-backup-recovery.md](../06-operations/06-backup-recovery.md)。

## 响应格式

全部端点返回统一信封 `ApiResponse<T>`：

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "requestId": "0HN8V..."
}
```

业务失败（422）由 `BusinessFail` 产出，`message` 为失败原因；模型/权限/认证失败沿用通用 HTTP 状态码（见文末）。信封另含 `errors`（仅失败且带错误码时）与 `timestamp` 字段，完整定义见 [README.md](README.md#响应格式)。

## GET /backup — 备份文件列表

按备份时间倒序返回全部备份文件。`id` 为**稳定标识**（有侧车清单时取清单 Id，历史遗留 `.bak` 由文件名确定性派生），可安全用于恢复/删除。

**成功响应** (200):

```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    {
      "id": "3f2a9c1e-5b7d-4e0a-9c1e-5b3d7f2a8c60",
      "fileName": "LYBTDB_20260922104501.bak",
      "kind": "Full",
      "createdAt": "2026-09-22T10:45:01",
      "sizeBytes": 52428800,
      "isCompressed": true,
      "isEncrypted": false,
      "databaseName": "LYBTDB_Dev",
      "baseFullBackupId": null,
      "baseFullBackupFileName": null,
      "isChainBroken": false
    }
  ]
}
```

> `kind`：`Full` / `Differential` / `PreRestore`（恢复前自动保护性备份）。`isChainBroken=true` 表示差异备份的基准全量已缺失，该备份当前不可恢复。
>
> **文件命名**：全量 `LYBTDB_{yyyyMMddHHmmss}.bak`、差异 `LYBTDB_{yyyyMMddHHmmss}_diff.bak`、恢复前保护 `LYBTDB_{yyyyMMddHHmmss}_prerestore.bak`；加密备份追加 `.enc` 后缀（如 `LYBTDB_20260922104501.bak.enc`），旁挂 `{文件名}.manifest.json` 侧车清单。

## GET /backup/status — 备份状态与进度

**成功响应** (200):

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "lastBackupAt": "2026-09-22T10:45:01",
    "fileCount": 6,
    "totalSizeBytes": 314572800,
    "backupDirectory": "D:\\Services\\LYBT-API\\backup",
    "retentionDays": 7,
    "databaseName": "LYBTDB_Dev",
    "isOperationRunning": true,
    "operationKind": "备份",
    "phaseMessage": "备份数据库",
    "progressPercent": 45,
    "operationStartedAt": "2026-09-22T10:44:32",
    "lastError": null,
    "autoBackupEnabled": false,
    "autoBackupIntervalHours": 24
  }
}
```

> `progressPercent` 由轮询 `sys.dm_exec_requests.percent_complete` 得出（无法量化时为 0）。UI 轮询本端点显示进度。

## GET /backup/tables — 可选择性恢复的表清单

**成功响应** (200):

```json
{
  "success": true,
  "message": "查询成功",
  "data": [
    { "tableName": "Patients", "rowCount": 1280, "supportsRecordSelection": true },
    { "tableName": "MedicalCases", "rowCount": 5432, "supportsRecordSelection": true }
  ]
}
```

> `supportsRecordSelection` = 该表是否具备 `Id` 列（仅此类表支持记录级选择性恢复）。

## POST /backup — 创建备份

**请求体**:

```json
{
  "kind": "Full",
  "compress": true,
  "encrypt": false,
  "password": null
}
```

| 字段 | 说明 |
|------|------|
| `kind` | `Full`（默认）/ `Differential`（依赖最近一次全量，无基准报错）；`PreRestore` 仅系统内部使用 |
| `compress` | 是否 `WITH COMPRESSION`（默认 true）；**Express / LocalDB 实例不支持压缩**，此时自动降级为未压缩并在结果 `warning`/`message` 中提示，`isCompressed` 记为 false |
| `encrypt` | 是否文件级 AES-256 加密（默认 false） |
| `password` | 加密口令；`encrypt=true` 时未提供则回退 `Backup:EncryptionPassword`，两者皆空则报错 |

**成功响应** (200):

```json
{
  "success": true,
  "message": "备份成功：LYBTDB_20260922104501.bak",
  "data": {
    "success": true,
    "error": null,
    "warning": null,
    "file": { "id": "…", "fileName": "LYBTDB_20260922104501.bak", "kind": "Full", "isEncrypted": false },
    "affectedCount": 1,
    "message": "备份成功：LYBTDB_20260922104501.bak"
  }
}
```

**失败响应**:

| 场景 | 状态码 | 说明 |
|------|--------|------|
| 差异备份无全量基准 | 422 | 需先创建全量备份 |
| 启用加密但无口令 | 422 | 请求未带 `password` 且 `Backup:EncryptionPassword` 为空 |

## POST /backup/{id}/restore — 恢复备份

`id` 来自 `GET /backup` 的 `data[].id`（路径参数，请求体中的 `backupId` 会被路径值覆盖）。

**请求体**:

```json
{
  "mode": "Full",
  "tables": [],
  "createPreRestoreBackup": true,
  "password": null
}
```

| 字段 | 说明 |
|------|------|
| `mode` | `Full`（默认，`RESTORE DATABASE … WITH REPLACE`，整库覆盖）或 `Selective` |
| `tables` | 选择性恢复的表清单（`mode=Selective` 时必填）；`ids` 为空表示恢复该表全部记录，仅支持含 `Id` 列的表 |
| `createPreRestoreBackup` | 恢复前是否自动备份当前数据（默认 true；失败不阻断恢复，但在结果 `warning` 中提示） |
| `password` | 目标备份已加密时必填 |

**成功响应** (200): 同一操作结果信封——整库恢复 `message` = `恢复完成：{文件名}，请重启应用`；选择性恢复 `message` = `选择性恢复完成：{N} 张表，请重启应用`，`affectedCount` = 回写表数，且因外键未校验而附带 `warning`。

**失败响应**:

| 场景 | 状态码 | 说明 |
|------|--------|------|
| 备份 Id 不存在 | 422 | 请先 `GET /backup` 取有效 Id |
| 差异备份基准缺失（`isChainBroken`） | 422 | 需先补齐基准全量备份 |
| 选择性恢复指定了无 `Id` 列的表 | 422 | 该表不支持记录级选择 |
| 加密备份未提供口令 | 422 | 请求需带 `password` |

> ⚠️ **选择性恢复的外键约束不校验**：引擎在回写期间临时禁用全部外键约束，结束时以 `WITH NOCHECK` 重新启用（**不做数据校验**）。结果中出现警告时请执行 `DBCC CHECKCONSTRAINTS` 复核。
>
> ⚠️ **恢复完成后必须重启应用**（桌面 UI 会提示）：引擎需独占单用户连接并清空连接池（`SqlConnection.ClearAllPools()`）。

## DELETE /backup/{id} — 删除备份文件

**成功响应** (200): 操作结果信封（`message` = `已删除备份 {文件名}`，`affectedCount` = 删除文件数）。

**失败响应**:

| 场景 | 状态码 | 说明 |
|------|--------|------|
| 备份 Id 不存在 | 422 | — |
| 该全量备份被差异备份引用 | 422 | 需先删除/重建依赖它的差异备份 |

## POST /backup/cleanup — 清理超期备份

按 `Backup:RetentionDays` 删除超期文件，**保护最新全量备份及其差异链**。

**成功响应** (200): 操作结果信封（`message` = `已清理过期备份 {N} 个`；无超期文件时为 `没有需要清理的过期备份`，`affectedCount` = 删除文件数）。

## POST /backup/auto — 自动备份（登录触发）

桌面登录成功后 fire-and-forget 调用；**仅要求已认证**（任意登录角色）。

**成功响应** (200): 操作结果信封——距上次备份未满 `Backup:AutoBackup:IntervalHours`（默认 24 小时）时为空操作（`message` = `距上次备份未满 {N} 小时，跳过自动备份`，`affectedCount` = 0）；实际执行时 `message` = `备份成功：{文件名}`（若同时清理了超期文件则追加 `；已清理过期备份 {N} 个`），`affectedCount` = 1。

> 服务端每日自动全量备份可另开宿主调度（`Backup:AutoBackup:Enabled=true` → `BackupSchedulerService`），与登录触发共用同一间隔判定。

## HTTP 状态码

| 码 | 含义 |
|----|------|
| 200 | 成功（业务成功与否见 `success`） |
| 401 | 未认证（类级 `[Authorize]`） |
| 403 | 权限不足（管理操作需 `SysAdminOnly`；`POST /auto` 仅需认证） |
| 422 | 业务规则失败（`BusinessFail`：备份失败/恢复失败/删除拒绝/清理失败等） |

## 相关文档

- 需求：[US-SHELL-013](../02-requirements/11a-shell.md)｜[NFR-AVAIL-001/002](../02-requirements/12-nfr.md)
- 运维：[06-operations/06-backup-recovery.md](../06-operations/06-backup-recovery.md)
- 端点清单：[03-architecture/13b-api-endpoints.md §3.12](../03-architecture/13b-api-endpoints.md)
- 权限矩阵：[01-product/04-permissions.md](../01-product/04-permissions.md)
