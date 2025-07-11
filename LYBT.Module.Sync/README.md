## AGENTS.md — 数据同步模块（LYBT.Module.Sync）

### 1. Agent 概述

数据同步模块用于管理系统内外部数据的同步任务和日志记录，包括手动和自动同步模式，支持数据同步状态监控和异常追踪。

### 2. 核心能力

- 创建同步任务（记录同步源、目标、时间、范围等）
- 编辑/更新同步任务状态
- 查询同步任务及同步日志（按任务、类型、时间过滤）
- 写入同步日志，记录同步详细信息和结果

### 3. 输入输出规范

#### 输入

- `SyncTaskCreateDto`：新建同步任务（需含同步类型、目标、范围）
- `SyncTaskEditDto`：编辑同步任务
- `SyncLogDto`：写入同步日志
- `SyncTaskQueryDto`/`SyncLogQueryDto`：查询参数

#### 输出

- `SyncTaskDto`：同步任务详情
- `SyncLogDto`：同步日志详情
- `(IList<SyncTaskDto>, int)`：同步任务分页
- `(IList<SyncLogDto>, int)`：日志分页
- `bool`：操作结果

### 4. 协作与依赖模块

- **基础设施模块**：同步任务/日志表数据持久化
- **系统设置模块**：同步相关配置项
- **日志模块**：记录同步流程外部日志

### 5. 示例场景

#### 新建同步任务

```csharp
var dto = new SyncTaskCreateDto {
  TaskType = SyncTaskType.Patient,
  Target = "中心数据库"
};
bool ok = await _syncService.CreateTaskAsync(dto);
```

#### 写入同步日志

```csharp
var logDto = new SyncLogDto {
  TaskId = taskId,
  Type = SyncLogType.Operation,
  Content = "同步成功"
};
bool ok = await _syncService.WriteLogAsync(logDto);
```

### 6. 接口列表

- `Task<bool> CreateTaskAsync(SyncTaskCreateDto dto)`
- `Task<bool> UpdateTaskAsync(SyncTaskEditDto dto)`
- `Task<(IList<SyncTaskDto>, int)> GetTasksAsync(SyncTaskQueryDto query)`
- `Task<bool> WriteLogAsync(SyncLogDto dto)`
- `Task<(IList<SyncLogDto>, int)> GetLogsAsync(SyncLogQueryDto query)`

