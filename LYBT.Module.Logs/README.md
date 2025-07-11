## AGENTS.md — 日志模块（LYBT.Module.Logs）

### 1. Agent 概述

日志模块用于统一记录系统各业务模块的操作日志，包括用户操作、关键业务流程与系统异常，支撑全局审计、操作追踪与问题排查。

### 2. 核心能力

- 写入操作日志（支持类型、对象、操作人、内容等结构化存储）
- 分页查询日志（支持多条件过滤）
- 支持操作内容差异对比（变更前后对比）

### 3. 输入输出规范

#### 输入

- `LogDto`：单条日志内容（含类型、模块、对象、操作人、变更内容等）
- `LogQueryDto`：日志查询参数

#### 输出

- `(IList<LogDto>, int TotalCount)`：日志分页列表
- `bool`：操作是否成功

### 4. 协作与依赖模块

- **全部业务模块**：写入日志，支持各模块重要操作审计
- **基础设施模块**：日志数据持久化
- **系统设置模块**：日志相关配置（如保留时长）

### 5. 示例场景

#### 写操作日志

```csharp
var log = new LogDto {
  ObjectType = "Patient",
  ObjectId = patientId,
  Operation = "Edit",
  OperatorId = userId,
  OperatorName = "李医生",
  Content = "修改电话"
};
bool ok = await _logService.WriteAsync(log);
```

#### 查询日志

```csharp
var query = new LogQueryDto {
  ObjectType = "Prescription",
  DateRange = (DateTime.Today.AddDays(-7), DateTime.Today)
};
var (list, total) = await _logService.GetLogsAsync(query);
```

### 6. 接口列表

- `Task<bool> WriteAsync(LogDto dto)`
- `Task<(IList<LogDto>, int)> GetLogsAsync(LogQueryDto query)`

