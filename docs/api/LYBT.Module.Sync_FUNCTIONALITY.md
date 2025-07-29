# LYBT.Module.Sync 功能说明文档

## 模块概述

同步模块是中医诊疗系统的数据集成核心，负责系统与外部数据源（如中心数据库、第三方系统、分支机构）之间的数据同步管理。本模块实现了多种同步模式的支持，包括手动同步、自动同步和定时同步，提供完整的同步任务管理和日志记录功能，确保数据的一致性、完整性和实时性。

## 业务价值

- **数据一致性**: 确保多系统间数据的一致性和准确性
- **实时同步**: 支持实时或近实时的数据同步机制
- **任务管理**: 提供完整的同步任务生命周期管理
- **异常处理**: 完善的同步失败处理和重试机制
- **监控审计**: 详细的同步日志和监控功能
- **灵活配置**: 支持多种同步模式和策略配置

## 数据模型

### SyncTaskModel (同步任务实体)

**文件位置**: `LYBT.Module.Sync/Models/SyncTaskModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | Guid | 同步任务ID（主键） | 自动生成，唯一标识 | 同步任务唯一标识 |
| TaskType | string | 任务类型 | 必填，如"全量"、"增量"、"手动"、"自动" | 区分同步任务的类型和处理方式 |
| Status | string | 任务状态 | 必填，如"已完成"、"进行中"、"失败" | 跟踪同步任务的执行状态 |
| TriggerTime | DateTime | 任务触发时间 | 必填，默认当前时间 | 记录任务创建或触发的时间 |
| ExecuteTime | DateTime? | 实际执行时间 | 可选，任务开始执行时设置 | 记录任务实际开始执行的时间 |
| Remark | string? | 日志说明 | 可选，最大1000字符 | 记录任务执行过程中的重要信息 |

### SyncLogModel (同步日志实体)

**文件位置**: `LYBT.Module.Sync/Models/SyncLogModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | string | 同步日志ID（主键） | 自动生成GUID字符串 | 同步日志唯一标识 |
| SyncTime | DateTime | 同步时间 | 必填，默认当前时间 | 记录同步操作的时间 |
| Mode | SyncMode | 同步模式 | 必填，枚举值 | 标识同步的触发方式 |
| Status | SyncStatus | 同步状态 | 必填，枚举值 | 记录同步操作的结果状态 |
| Message | string? | 错误或成功信息 | 可选，详细信息描述 | 记录同步过程的详细信息或错误原因 |

### SyncMode (同步模式枚举)

**文件位置**: `LYBT.Common/Enums/System/SystemEnums.cs`

| 枚举值 | 中文名称 | 数值 | 说明 | 使用场景 |
|--------|----------|------|------|----------|
| Manual | 手动 | 0 | 用户手动触发的同步 | 管理员主动执行的同步操作 |
| Auto | 自动 | 1 | 系统自动触发的同步 | 基于事件或条件自动触发 |
| Scheduled | 定时 | 2 | 按计划定时执行的同步 | 定时任务或周期性同步 |

### SyncStatus (同步状态枚举)

**文件位置**: `LYBT.Common/Enums/System/SystemEnums.cs`

| 枚举值 | 中文名称 | 数值 | 说明 | 业务含义 |
|--------|----------|------|------|----------|
| Pending | 待同步 | 0 | 等待执行的同步任务 | 任务已创建但未开始执行 |
| Syncing | 同步中 | 1 | 正在执行的同步任务 | 任务正在执行过程中 |
| Completed | 已完成 | 2 | 成功完成的同步任务 | 任务执行成功完成 |
| Failed | 失败 | -1 | 执行失败的同步任务 | 任务执行过程中出现错误 |
| Cancelled | 已取消 | -2 | 被取消的同步任务 | 任务被手动取消或系统中断 |

## DTO 数据传输对象

### SyncTaskCreateDto (新增同步任务)

**使用场景**: 创建新的同步任务
**特点**: 包含任务基本信息和初始状态设置

```csharp
- TaskType: 任务类型（必填，如"手动同步"、"自动同步"）
- Status: 任务状态（必填，如"进行中"、"已完成"）
- TriggerTime: 任务触发时间（可选，默认当前时间）
- Remark: 备注信息（可选，任务说明）
```

**验证规则**:
- 任务类型和状态必须在预定义的值范围内
- 触发时间不能早于当前时间（对于计划任务）
- 备注信息不超过1000字符

### SyncTaskDetailDto (同步任务详情)

**使用场景**: 查看完整的同步任务信息
**特点**: 包含任务的完整生命周期信息

### SyncTaskDto (同步任务列表)

**使用场景**: 同步任务列表展示和管理
**特点**: 精简信息，适合列表显示和筛选

### SyncTaskEditDto (编辑同步任务)

**使用场景**: 更新同步任务状态和信息
**特点**: 包含ID标识和可修改的字段

### SyncLogCreateDto (新增同步日志)

**使用场景**: 记录同步操作的详细日志
**特点**: 包含同步过程的完整信息

```csharp
- Mode: 同步模式（必填，SyncMode枚举）
- Status: 同步状态（必填，SyncStatus枚举）
- SyncTime: 同步时间（可选，默认当前时间）
- Message: 错误或成功信息（可选，详细描述）
```

### SyncLogDto (同步日志)

**使用场景**: 同步日志查询和展示
**特点**: 完整的同步操作记录信息

## 服务层 (ISyncService & SyncService)

### 同步日志管理方法

#### GetLogListAsync

```csharp
Task<List<SyncLogDto>> GetLogListAsync()
```

**功能**: 获取所有同步日志记录
**业务逻辑**: 
- 查询所有同步日志记录
- 按时间倒序排列
- 使用AutoMapper进行实体到DTO转换

**使用场景**: 同步历史查看、问题排查、统计分析

#### AddLogAsync

```csharp
Task<bool> AddLogAsync(SyncLogCreateDto syncLogCreateDto)
```

**功能**: 创建新的同步日志记录
**业务逻辑**: 
- 验证输入数据的有效性
- 生成唯一的日志ID
- 设置当前时间为同步时间
- 保存到数据库

**特殊处理**:
- 自动生成GUID格式的日志ID
- 自动设置同步时间为当前时间
- 验证同步模式和状态的有效性

**使用场景**: 同步操作执行时自动记录、手动记录重要操作

#### DeleteLogAsync

```csharp
Task<bool> DeleteLogAsync(string id)
```

**功能**: 删除指定的同步日志
**业务逻辑**: 
- 验证日志ID的存在性
- 执行物理删除操作
- 返回删除结果

**使用场景**: 日志清理、存储空间管理、隐私数据处理

#### GetLastSyncInfoAsync

```csharp
Task<SyncLogDto?> GetLastSyncInfoAsync()
```

**功能**: 获取最近一次同步的信息
**业务逻辑**: 
- 查询最新的同步日志记录
- 返回同步时间、状态、结果等信息
- 用于显示当前同步状态

**使用场景**: 系统状态显示、同步状态检查、用户界面展示

#### GetSyncLogPagedAsync

```csharp
Task<List<SyncLogDto>> GetSyncLogPagedAsync(int page, int pageSize)
```

**功能**: 分页查询同步日志
**业务逻辑**: 
- 支持分页查询减少数据加载量
- 按时间倒序排列
- 提供高效的日志浏览功能

**使用场景**: 日志管理界面、历史记录查看、大量日志的分页浏览

#### CheckConnectionStatusAsync

```csharp
Task<bool> CheckConnectionStatusAsync()
```

**功能**: 检测中心数据库连接状态
**业务逻辑**: 
- 尝试连接目标数据库
- 验证连接的有效性
- 返回连接状态结果

**使用场景**: 同步前的连接检查、系统健康监控、故障诊断

#### TriggerManualSyncAsync

```csharp
Task<bool> TriggerManualSyncAsync()
```

**功能**: 手动触发同步操作
**业务逻辑**: 
- 创建手动同步日志记录
- 设置同步模式为Manual
- 记录触发时间和操作者
- 可扩展为实际执行同步逻辑

**使用场景**: 管理员手动同步、紧急数据同步、测试同步功能

### 同步任务管理方法

#### GetTaskListAsync

```csharp
Task<List<SyncTaskDto>> GetTaskListAsync()
```

**功能**: 获取所有同步任务列表
**业务逻辑**: 
- 查询所有同步任务记录
- 按创建时间或优先级排序
- 返回任务列表信息

**使用场景**: 任务管理界面、任务监控、系统管理

#### GetTaskDetailAsync

```csharp
Task<SyncTaskDetailDto?> GetTaskDetailAsync(Guid id)
```

**功能**: 获取指定任务的详细信息
**业务逻辑**: 
- 根据任务ID查询详细信息
- 包含任务执行的完整历史
- 提供任务状态和进度信息

**使用场景**: 任务详情查看、问题诊断、任务监控

#### AddTaskAsync

```csharp
Task<bool> AddTaskAsync(SyncTaskCreateDto syncTaskCreateDto)
```

**功能**: 创建新的同步任务
**业务逻辑**: 
- 验证任务参数的有效性
- 生成唯一的任务ID
- 设置任务初始状态
- 记录任务创建时间

**特殊处理**:
- 自动生成GUID格式的任务ID
- 设置触发时间为当前时间
- 验证任务类型和状态的合法性

**使用场景**: 创建定时同步任务、创建手动同步任务、批量任务生成

#### UpdateTaskAsync

```csharp
Task<bool> UpdateTaskAsync(SyncTaskEditDto syncTaskEditDto)
```

**功能**: 更新同步任务信息
**业务逻辑**: 
- 验证任务的存在性
- 更新任务状态和执行时间
- 更新任务备注信息
- 保持任务历史记录

**特殊处理**:
- 状态转换的业务规则验证
- 执行时间的合理性检查
- 任务状态变更的日志记录

**使用场景**: 任务状态更新、任务进度跟踪、任务信息修正

#### DeleteTaskAsync

```csharp
Task<bool> DeleteTaskAsync(Guid id)
```

**功能**: 删除指定的同步任务
**业务逻辑**: 
- 验证任务的存在性
- 检查任务是否可以删除
- 执行删除操作
- 记录删除日志

**安全考虑**:
- 正在执行的任务不能删除
- 需要管理员权限
- 删除前的确认机制

**使用场景**: 任务清理、错误任务删除、系统维护

## 仓储层 (ISyncRepository & SyncRepository)

### 同步日志数据操作

#### GetLogListAsync

```csharp
Task<List<SyncLogModel>> GetLogListAsync()
```

**功能**: 获取所有同步日志记录
**实现细节**: 
- 查询所有日志记录
- 按时间倒序排列
- 支持大量数据的高效查询

#### AddLogAsync

```csharp
Task<bool> AddLogAsync(SyncLogModel model)
```

**功能**: 新增同步日志到数据库
**实现细节**: 
- 插入新的日志记录
- 确保数据完整性
- 返回操作结果

#### DeleteLogAsync

```csharp
Task<bool> DeleteLogAsync(string id)
```

**功能**: 删除指定的同步日志
**实现细节**: 
- 根据ID删除日志记录
- 物理删除策略
- 返回删除结果

#### GetLastLogAsync

```csharp
Task<SyncLogModel?> GetLastLogAsync()
```

**功能**: 获取最新的同步日志
**实现细节**: 
- 按时间倒序查询第一条记录
- 高效的单记录查询
- 处理无记录的情况

#### GetLogPagedAsync

```csharp
Task<List<SyncLogModel>> GetLogPagedAsync(int page, int pageSize)
```

**功能**: 分页查询同步日志
**实现细节**: 
- 使用Skip和Take进行分页
- 按时间倒序排列
- 优化查询性能

#### CanConnectAsync

```csharp
Task<bool> CanConnectAsync()
```

**功能**: 检测数据库连接状态
**实现细节**: 
- 尝试建立数据库连接
- 执行简单的连接测试
- 返回连接状态

### 同步任务数据操作

#### GetTaskListAsync

```csharp
Task<List<SyncTaskModel>> GetTaskListAsync()
```

**功能**: 获取所有同步任务记录
**实现细节**: 
- 查询所有任务记录
- 按优先级和时间排序
- 支持任务状态筛选

#### GetTaskByIdAsync

```csharp
Task<SyncTaskModel?> GetTaskByIdAsync(Guid id)
```

**功能**: 根据ID获取任务详情
**实现细节**: 
- 使用主键查询
- 高效的单记录查询
- 包含完整的任务信息

#### AddTaskAsync

```csharp
Task<bool> AddTaskAsync(SyncTaskModel model)
```

**功能**: 新增同步任务到数据库
**实现细节**: 
- 插入新的任务记录
- 验证数据完整性
- 返回操作结果

#### UpdateTaskAsync

```csharp
Task<bool> UpdateTaskAsync(SyncTaskModel model)
```

**功能**: 更新同步任务信息
**实现细节**: 
- 更新指定字段
- 保持数据一致性
- 处理并发更新

#### DeleteTaskAsync

```csharp
Task<bool> DeleteTaskAsync(Guid id)
```

**功能**: 删除指定的同步任务
**实现细节**: 
- 先查询再删除
- 确保任务可以安全删除
- 返回删除结果

## 权限控制策略

### 操作权限

- **查看权限**: 管理员可查看所有同步日志和任务，普通用户可查看基本状态
- **执行权限**: 只有系统管理员可以手动触发同步操作
- **管理权限**: 同步任务的创建、修改、删除需要管理员权限
- **配置权限**: 同步策略和参数配置需要系统管理员权限

### 安全控制

- **操作审计**: 所有同步操作都有详细的审计记录
- **权限验证**: 每个操作前都要验证用户权限
- **数据安全**: 同步过程中的数据传输加密和验证

## 日志审计机制

### 操作日志

所有同步相关操作都会记录详细日志：

- **同步执行**: 记录每次同步的详细过程和结果
- **任务管理**: 记录任务的创建、修改、删除操作
- **手动操作**: 记录管理员的手动同步操作
- **异常处理**: 记录同步失败和异常处理过程

### 性能日志

- **执行时间**: 记录同步任务的执行时间和性能指标
- **数据量统计**: 记录同步的数据量和处理速度
- **资源使用**: 记录同步过程中的系统资源使用情况
- **网络状态**: 记录网络连接状态和传输质量

### 审计内容

- 操作时间和操作者信息
- 同步任务的完整执行历史
- 数据变更的详细记录
- 异常情况和处理过程

## 集成依赖

### 外部系统依赖

- **中心数据库**: 主要的数据同步目标
- **第三方系统**: 可能的外部数据源
- **分支机构**: 多点部署的同步需求
- **备份系统**: 数据备份和恢复集成

### 基础服务依赖

- **IMapper**: AutoMapper对象映射服务
- **SyncDbContext**: 专用数据库上下文
- **IConfigurationService**: 配置管理服务
- **INotificationService**: 通知服务（同步完成通知）

## 使用示例

### 手动触发同步

```csharp
// 手动触发同步操作
var success = await syncService.TriggerManualSyncAsync();
if (success)
{
    logger.LogInformation("手动同步触发成功");
    
    // 获取同步结果
    var lastSync = await syncService.GetLastSyncInfoAsync();
    if (lastSync != null)
    {
        Console.WriteLine($"同步时间: {lastSync.SyncTime}");
        Console.WriteLine($"同步状态: {lastSync.Status}");
        Console.WriteLine($"同步信息: {lastSync.Message}");
    }
}
```

### 创建定时同步任务

```csharp
var createTaskDto = new SyncTaskCreateDto
{
    TaskType = "定时同步",
    Status = "待执行",
    TriggerTime = DateTime.Now.AddHours(1), // 1小时后执行
    Remark = "每日定时数据同步任务"
};

var taskSuccess = await syncService.AddTaskAsync(createTaskDto);
if (taskSuccess)
{
    logger.LogInformation("定时同步任务创建成功");
}
```

### 查询同步历史

```csharp
// 分页查询同步日志
var page = 1;
var pageSize = 20;
var syncLogs = await syncService.GetSyncLogPagedAsync(page, pageSize);

Console.WriteLine($"找到 {syncLogs.Count} 条同步记录：");
foreach (var log in syncLogs)
{
    Console.WriteLine($"{log.SyncTime:yyyy-MM-dd HH:mm:ss} - {log.Mode} - {log.Status}");
    if (!string.IsNullOrEmpty(log.Message))
    {
        Console.WriteLine($"  详情: {log.Message}");
    }
}
```

### 监控同步状态

```csharp
// 检查连接状态
var connectionStatus = await syncService.CheckConnectionStatusAsync();
Console.WriteLine($"数据库连接状态: {(connectionStatus ? "正常" : "异常")}");

// 获取最近同步信息
var lastSync = await syncService.GetLastSyncInfoAsync();
if (lastSync != null)
{
    var timeSinceLastSync = DateTime.Now - lastSync.SyncTime;
    Console.WriteLine($"距离上次同步: {timeSinceLastSync.TotalMinutes:F0} 分钟");
    
    if (lastSync.Status == SyncStatus.Failed)
    {
        Console.WriteLine($"上次同步失败: {lastSync.Message}");
        // 可能需要发送告警
    }
}
```

### 同步任务管理

```csharp
// 获取所有同步任务
var tasks = await syncService.GetTaskListAsync();
var pendingTasks = tasks.Where(t => t.Status == "待执行").ToList();
var runningTasks = tasks.Where(t => t.Status == "进行中").ToList();

Console.WriteLine($"待执行任务: {pendingTasks.Count} 个");
Console.WriteLine($"正在执行任务: {runningTasks.Count} 个");

// 更新任务状态
foreach (var task in runningTasks)
{
    var taskDetail = await syncService.GetTaskDetailAsync(task.Id);
    if (taskDetail != null)
    {
        // 检查任务是否应该完成
        if (ShouldCompleteTask(taskDetail))
        {
            var editDto = new SyncTaskEditDto
            {
                Id = task.Id,
                Status = "已完成",
                ExecuteTime = DateTime.Now,
                Remark = "任务执行完成"
            };
            
            await syncService.UpdateTaskAsync(editDto);
        }
    }
}
```

### 同步错误处理

```csharp
// 创建带错误处理的同步日志
public async Task<bool> PerformSyncWithErrorHandlingAsync()
{
    var syncLogDto = new SyncLogCreateDto
    {
        Mode = SyncMode.Auto,
        Status = SyncStatus.Syncing,
        Message = "开始自动同步"
    };
    
    try
    {
        // 记录开始同步
        await syncService.AddLogAsync(syncLogDto);
        
        // 执行实际同步逻辑
        var syncResult = await ExecuteActualSyncAsync();
        
        // 更新同步结果
        syncLogDto.Status = syncResult.Success ? SyncStatus.Completed : SyncStatus.Failed;
        syncLogDto.Message = syncResult.Message;
        
        await syncService.AddLogAsync(syncLogDto);
        
        return syncResult.Success;
    }
    catch (Exception ex)
    {
        // 记录同步异常
        syncLogDto.Status = SyncStatus.Failed;
        syncLogDto.Message = $"同步异常: {ex.Message}";
        
        await syncService.AddLogAsync(syncLogDto);
        
        logger.LogError(ex, "同步过程中发生异常");
        return false;
    }
}
```

### 同步性能监控

```csharp
// 同步性能统计
public async Task<SyncPerformanceStatsDto> GetSyncPerformanceStatsAsync(DateTime startDate, DateTime endDate)
{
    var allLogs = await syncService.GetLogListAsync();
    var periodLogs = allLogs.Where(l => l.SyncTime >= startDate && l.SyncTime <= endDate).ToList();
    
    return new SyncPerformanceStatsDto
    {
        TotalSyncs = periodLogs.Count,
        SuccessfulSyncs = periodLogs.Count(l => l.Status == SyncStatus.Completed),
        FailedSyncs = periodLogs.Count(l => l.Status == SyncStatus.Failed),
        ManualSyncs = periodLogs.Count(l => l.Mode == SyncMode.Manual),
        AutoSyncs = periodLogs.Count(l => l.Mode == SyncMode.Auto),
        SuccessRate = periodLogs.Count > 0 ? 
            (double)periodLogs.Count(l => l.Status == SyncStatus.Completed) / periodLogs.Count * 100 : 0,
        LastSyncTime = periodLogs.OrderByDescending(l => l.SyncTime).FirstOrDefault()?.SyncTime
    };
}
```

## 业务扩展建议

### 功能增强

- **实时同步**: 基于消息队列的实时数据同步机制
- **增量同步**: 只同步变更数据的增量同步功能
- **冲突解决**: 数据冲突的自动检测和解决机制
- **同步验证**: 同步完成后的数据一致性验证

### 性能优化

- **并行同步**: 支持多线程或多进程的并行同步
- **压缩传输**: 大数据量的压缩传输优化
- **断点续传**: 支持同步中断后的断点续传
- **负载均衡**: 多目标同步的负载均衡策略

### 监控告警

- **实时监控**: 同步过程的实时监控和状态展示
- **异常告警**: 同步失败或异常的自动告警机制
- **性能预警**: 同步性能下降的预警功能
- **健康检查**: 定期的系统健康检查和报告