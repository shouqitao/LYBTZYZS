# LYBT.Module.Sync

数据同步模块，记录同步任务及日志。

## 主要服务及接口
- `ISyncService` / `SyncService`
- `ISyncRepository` / `SyncRepository`

## 重要模型和DTO
- `SyncTaskModel`、`SyncLogModel`
- `SyncTaskDto`、`SyncTaskCreateDto`、`SyncTaskEditDto`、`SyncTaskDetailDto`
- `SyncLogDto`、`SyncLogCreateDto`

## 用法
在启动时调用 `SyncModule.Register(services)`，随后使用 `ISyncService` 创建或查询同步任务和日志。
