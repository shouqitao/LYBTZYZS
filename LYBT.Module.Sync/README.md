## AGENTS.md — 数据同步模块（LYBT.Module.Sync）

### 1. Agent 概述

数据同步模块用于管理系统内外部数据的同步任务和日志记录，包括手动和自动同步模式，支持数据同步状态监控和异常追踪。

### 2. 核心能力

- 创建、更新和删除同步任务
- 写入同步日志并查询历史
- 获取最近一次同步信息
- 检测中心数据库连接状态

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

- `Task<List<SyncTaskDto>> GetTaskListAsync()`
- `Task<SyncTaskDetailDto?> GetTaskDetailAsync(Guid id)`
- `Task<bool> AddTaskAsync(SyncTaskCreateDto dto)`
- `Task<bool> UpdateTaskAsync(SyncTaskEditDto dto)`
- `Task<bool> DeleteTaskAsync(Guid id)`
- `Task<List<SyncLogDto>> GetLogListAsync()`
- `Task<SyncLogDto?> GetLastSyncInfoAsync()`
- `Task<bool> AddLogAsync(SyncLogCreateDto dto)`
- `Task<bool> DeleteLogAsync(string id)`
- `Task<bool> CheckConnectionStatusAsync()`
- `Task<bool> TriggerManualSyncAsync()`


## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```
