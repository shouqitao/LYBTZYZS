# 事务系统 API 参考文档

## 概述

本文档描述了事务系统的所有公共API接口，包括核心接口、监控接口和健康检查接口。

## 核心接口

### ITransactionCoordinator&lt;TContext&gt;

事务协调器核心接口，负责执行事务定义。

#### 方法

##### ExecuteAsync

```csharp
Task<TransactionResult<TContext>> ExecuteAsync(
    TransactionDefinition<TContext> definition,
    TContext context,
    CancellationToken cancellationToken = default)
```

**描述**: 执行指定的事务定义

**参数**:
- `definition`: 事务定义，包含步骤列表和配置
- `context`: 事务上下文，在步骤间传递数据
- `cancellationToken`: 取消令牌，用于取消长时间运行的事务

**返回值**: `TransactionResult<TContext>` - 事务执行结果

**异常**:
- `ArgumentNullException`: 当definition或context为null时
- `OperationCanceledException`: 当事务被取消或超时时
- `TransactionException`: 当事务执行失败且无法补偿时

**示例**:
```csharp
var coordinator = serviceProvider.GetRequiredService<ITransactionCoordinator<MyContext>>();
var definition = new TransactionDefinition<MyContext> { /* ... */ };
var context = new MyContext { /* ... */ };

var result = await coordinator.ExecuteAsync(definition, context);

if (result.Status == TransactionStatus.Completed)
{
    Console.WriteLine("事务执行成功");
}
```

## 事务步骤接口

### ITransactionStep&lt;TContext&gt;

事务步骤基础接口。

#### 属性

```csharp
string StepName { get; }           // 步骤名称
int Order { get; }                 // 执行顺序
bool SupportsCompensation { get; } // 是否支持补偿
TimeSpan Timeout { get; }          // 步骤超时时间
```

#### 方法

##### CanExecuteAsync

```csharp
Task<bool> CanExecuteAsync(
    TContext context, 
    CancellationToken cancellationToken = default)
```

**描述**: 检查步骤是否可以执行

**参数**:
- `context`: 当前事务上下文
- `cancellationToken`: 取消令牌

**返回值**: `bool` - 是否可以执行

##### ExecuteAsync

```csharp
Task<TransactionStepResult> ExecuteAsync(
    TContext context, 
    CancellationToken cancellationToken = default)
```

**描述**: 执行步骤的主要逻辑

**参数**:
- `context`: 事务上下文
- `cancellationToken`: 取消令牌

**返回值**: `TransactionStepResult` - 步骤执行结果

##### CompensateAsync

```csharp
Task<TransactionStepResult> CompensateAsync(
    TContext context, 
    TransactionStepResult originalResult, 
    CancellationToken cancellationToken = default)
```

**描述**: 执行补偿操作，回滚步骤的影响

**参数**:
- `context`: 事务上下文
- `originalResult`: 原始执行结果
- `cancellationToken`: 取消令牌

**返回值**: `TransactionStepResult` - 补偿执行结果

## 监控接口

### ITransactionLogger

事务日志记录接口。

#### 方法

##### LogTransactionStartAsync

```csharp
Task LogTransactionStartAsync(
    Guid transactionId,
    string transactionName,
    string description,
    Guid? userId = null,
    CancellationToken cancellationToken = default)
```

**描述**: 记录事务开始

**参数**:
- `transactionId`: 事务唯一标识符
- `transactionName`: 事务名称
- `description`: 事务描述
- `userId`: 关联用户ID（可选）
- `cancellationToken`: 取消令牌

##### LogTransactionEndAsync

```csharp
Task LogTransactionEndAsync(
    Guid transactionId,
    TransactionStatus status,
    TimeSpan duration,
    string message,
    Exception? exception = null,
    CancellationToken cancellationToken = default)
```

**描述**: 记录事务结束

**参数**:
- `transactionId`: 事务ID
- `status`: 最终状态
- `duration`: 执行时长
- `message`: 结束消息
- `exception`: 异常信息（可选）
- `cancellationToken`: 取消令牌

##### LogStepStartAsync

```csharp
Task LogStepStartAsync(
    Guid transactionId,
    string stepName,
    int stepOrder,
    CancellationToken cancellationToken = default)
```

**描述**: 记录步骤开始

##### LogStepEndAsync

```csharp
Task LogStepEndAsync(
    Guid transactionId,
    string stepName,
    TransactionStepStatus status,
    TimeSpan duration,
    string message,
    Exception? exception = null,
    CancellationToken cancellationToken = default)
```

**描述**: 记录步骤结束

##### GetTransactionHistoryAsync

```csharp
Task<PagedResult<TransactionHistoryItem>> GetTransactionHistoryAsync(
    DateTime startTime,
    DateTime endTime,
    int pageIndex = 1,
    int pageSize = 20,
    string? transactionName = null,
    Guid? userId = null,
    CancellationToken cancellationToken = default)
```

**描述**: 获取事务历史记录

**参数**:
- `startTime`: 开始时间
- `endTime`: 结束时间
- `pageIndex`: 页码（从1开始）
- `pageSize`: 页大小
- `transactionName`: 事务名称筛选（可选）
- `userId`: 用户ID筛选（可选）
- `cancellationToken`: 取消令牌

**返回值**: `PagedResult<TransactionHistoryItem>` - 分页的历史记录

##### GetTransactionByIdAsync

```csharp
Task<TransactionDetails?> GetTransactionByIdAsync(
    Guid transactionId,
    CancellationToken cancellationToken = default)
```

**描述**: 获取特定事务的详细信息

**参数**:
- `transactionId`: 事务ID
- `cancellationToken`: 取消令牌

**返回值**: `TransactionDetails?` - 事务详细信息，不存在时返回null

##### GetTransactionStatisticsAsync

```csharp
Task<TransactionStatistics> GetTransactionStatisticsAsync(
    DateTime startTime,
    DateTime endTime,
    CancellationToken cancellationToken = default)
```

**描述**: 获取时间段内的事务统计信息

**参数**:
- `startTime`: 统计开始时间
- `endTime`: 统计结束时间
- `cancellationToken`: 取消令牌

**返回值**: `TransactionStatistics` - 统计信息

### ITransactionMetrics

事务性能指标接口。

#### 方法

##### RecordTransactionStartAsync

```csharp
Task RecordTransactionStartAsync(
    Guid transactionId,
    string transactionName,
    Guid? userId = null,
    CancellationToken cancellationToken = default)
```

**描述**: 记录事务开始指标

##### RecordTransactionCompleteAsync

```csharp
Task RecordTransactionCompleteAsync(
    Guid transactionId,
    TransactionStatus status,
    long durationMs,
    int stepCount,
    CancellationToken cancellationToken = default)
```

**描述**: 记录事务完成指标

##### RecordStepExecutionAsync

```csharp
Task RecordStepExecutionAsync(
    Guid transactionId,
    string stepName,
    int stepOrder,
    long durationMs,
    TransactionStepStatus status,
    CancellationToken cancellationToken = default)
```

**描述**: 记录步骤执行指标

##### GetCurrentMetricsAsync

```csharp
Task<TransactionMetricsSnapshot> GetCurrentMetricsAsync(
    CancellationToken cancellationToken = default)
```

**描述**: 获取当前性能指标快照

**返回值**: `TransactionMetricsSnapshot` - 当前指标快照

##### GetMetricsStatisticsAsync

```csharp
Task<TransactionMetricsStatistics> GetMetricsStatisticsAsync(
    DateTime startTime,
    DateTime endTime,
    CancellationToken cancellationToken = default)
```

**描述**: 获取时间段内的性能统计

##### GetSlowTransactionAlertsAsync

```csharp
Task<List<SlowTransactionAlert>> GetSlowTransactionAlertsAsync(
    long thresholdMs = 5000,
    CancellationToken cancellationToken = default)
```

**描述**: 获取慢事务告警信息

**参数**:
- `thresholdMs`: 慢事务阈值（毫秒），默认5秒
- `cancellationToken`: 取消令牌

**返回值**: `List<SlowTransactionAlert>` - 慢事务告警列表

##### GetErrorStatisticsAsync

```csharp
Task<TransactionErrorStatistics> GetErrorStatisticsAsync(
    int hoursBack = 24,
    CancellationToken cancellationToken = default)
```

**描述**: 获取错误统计信息

**参数**:
- `hoursBack`: 回溯小时数，默认24小时
- `cancellationToken`: 取消令牌

**返回值**: `TransactionErrorStatistics` - 错误统计信息

## 数据模型

### TransactionDefinition&lt;TContext&gt;

```csharp
public class TransactionDefinition<TContext> where TContext : TransactionContext
{
    public string Name { get; set; }                                          // 事务名称
    public string Description { get; set; }                                   // 事务描述
    public List<ITransactionStep<TContext>> Steps { get; set; }              // 事务步骤列表
    public TimeSpan Timeout { get; set; }                                     // 超时时间
    public int MaxRetryCount { get; set; }                                    // 最大重试次数
    public bool EnableAutoCompensation { get; set; }                          // 启用自动补偿
    public bool EnableParallelExecution { get; set; }                         // 启用并行执行
    public Dictionary<string, object> Metadata { get; set; }                  // 元数据
}
```

### TransactionResult&lt;TContext&gt;

```csharp
public class TransactionResult<TContext> where TContext : TransactionContext
{
    public Guid TransactionId { get; set; }                                   // 事务ID
    public TransactionStatus Status { get; set; }                             // 执行状态
    public TContext Context { get; set; }                                     // 事务上下文
    public string Message { get; set; }                                       // 结果消息
    public DateTime StartTime { get; set; }                                   // 开始时间
    public DateTime? EndTime { get; set; }                                    // 结束时间
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero; // 执行时长
    public List<TransactionStepExecution> ExecutedSteps { get; set; }         // 已执行步骤
    public List<TransactionStepExecution> CompensatedSteps { get; set; }      // 已补偿步骤
    public Exception? Exception { get; set; }                                 // 异常信息
}
```

### TransactionStepResult

```csharp
public class TransactionStepResult
{
    public TransactionStepStatus Status { get; set; }                         // 步骤状态
    public string Message { get; set; }                                       // 结果消息
    public Dictionary<string, object> Data { get; set; }                      // 结果数据
    public Exception? Exception { get; set; }                                 // 异常信息
    public DateTime ExecutedAt { get; set; }                                  // 执行时间
    public TimeSpan Duration { get; set; }                                    // 执行时长
}
```

### TransactionMetricsSnapshot

```csharp
public class TransactionMetricsSnapshot
{
    public DateTime SnapshotTime { get; set; }                                // 快照时间
    public int ActiveTransactionCount { get; set; }                           // 活跃事务数
    public long TotalCompletedToday { get; set; }                            // 今日完成事务数
    public long TotalFailedToday { get; set; }                               // 今日失败事务数
    public double AverageExecutionTimeMs { get; set; }                        // 平均执行时间
    public long SlowestExecutionTimeMs { get; set; }                         // 最慢执行时间
    public double SuccessRate { get; set; }                                   // 成功率
    public double TransactionsPerMinute { get; set; }                         // 每分钟事务数
    public int ActiveUsersLastHour { get; set; }                             // 最近1小时活跃用户
    public DateTime SystemStartTime { get; set; }                             // 系统启动时间
    public long CacheSizeBytes { get; set; }                                 // 缓存大小
}
```

### SlowTransactionAlert

```csharp
public class SlowTransactionAlert
{
    public Guid TransactionId { get; set; }                                   // 事务ID
    public string TransactionName { get; set; }                               // 事务名称
    public long DurationMs { get; set; }                                      // 执行时长
    public DateTime StartTime { get; set; }                                   // 开始时间
    public DateTime? EndTime { get; set; }                                    // 结束时间
    public Guid? UserId { get; set; }                                         // 用户ID
    public TransactionStatus Status { get; set; }                             // 事务状态
    public string SlowReason { get; set; }                                    // 慢事务原因
    public List<SlowStepInfo> SlowSteps { get; set; }                         // 慢步骤信息
}
```

## 枚举类型

### TransactionStatus

```csharp
public enum TransactionStatus
{
    InProgress = 0,    // 执行中
    Completed = 1,     // 已完成
    Failed = 2,        // 失败
    Cancelled = 3,     // 已取消
    Compensated = 4    // 已补偿
}
```

### TransactionStepStatus

```csharp
public enum TransactionStepStatus
{
    Success = 1,       // 成功
    Failed = 2,        // 失败
    Compensated = 3,   // 已补偿
    Skipped = 4        // 跳过
}
```

## HTTP API 端点

### 健康检查

#### GET /health/transaction

获取事务系统健康状态。

**响应**:
```json
{
  "status": "Healthy|Degraded|Unhealthy",
  "description": "事务系统运行正常",
  "data": {
    "database_connected": true,
    "active_transaction_count": 5,
    "recent_transaction_count": 123,
    "success_rate": 98.5,
    "average_execution_time_ms": 1250.0
  }
}
```

**状态码**:
- `200 OK`: 系统健康
- `503 Service Unavailable`: 系统不健康

## 使用示例

### 基本事务执行

```csharp
// 1. 依赖注入配置
services.AddCompleteTransactionSystem();

// 2. 创建事务上下文
var context = new PrescriptionTransactionContext
{
    PatientId = patientId,
    DoctorId = doctorId,
    Items = prescriptionItems
};

// 3. 获取事务定义
var transactionFactory = serviceProvider.GetRequiredService<CreatePrescriptionTransaction>();
var definition = transactionFactory.CreateDefinition();

// 4. 执行事务
var coordinator = serviceProvider.GetRequiredService<ITransactionCoordinator<PrescriptionTransactionContext>>();
var result = await coordinator.ExecuteAsync(definition, context);

// 5. 处理结果
if (result.Status == TransactionStatus.Completed)
{
    var prescriptionId = result.Context.PrescriptionId;
    // 处理成功逻辑
}
```

### 监控事务执行

```csharp
// 获取实时指标
var metrics = serviceProvider.GetRequiredService<ITransactionMetrics>();
var snapshot = await metrics.GetCurrentMetricsAsync();

Console.WriteLine($"活跃事务: {snapshot.ActiveTransactionCount}");
Console.WriteLine($"成功率: {snapshot.SuccessRate}%");
Console.WriteLine($"平均执行时间: {snapshot.AverageExecutionTimeMs}ms");

// 获取慢事务告警
var slowTransactions = await metrics.GetSlowTransactionAlertsAsync(5000);
foreach (var alert in slowTransactions)
{
    Console.WriteLine($"慢事务: {alert.TransactionName} - {alert.DurationMs}ms");
}

// 获取事务历史
var logger = serviceProvider.GetRequiredService<ITransactionLogger>();
var history = await logger.GetTransactionHistoryAsync(
    DateTime.UtcNow.AddHours(-24), 
    DateTime.UtcNow);

foreach (var item in history.Items)
{
    Console.WriteLine($"{item.TransactionName}: {item.Status} ({item.Duration}ms)");
}
```

### 自定义事务步骤

```csharp
public class CustomValidationStep : TransactionStepBase<MyTransactionContext>
{
    public override string StepName => "CustomValidation";
    public override int Order => 1;

    private readonly IValidationService _validationService;

    public CustomValidationStep(IValidationService validationService)
    {
        _validationService = validationService;
    }

    public override async Task<TransactionStepResult> ExecuteAsync(
        MyTransactionContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isValid = await _validationService.ValidateAsync(context.Data);
            
            if (!isValid)
            {
                return CreateFailureResult(
                    new ValidationException("数据验证失败"));
            }

            return CreateSuccessResult(new Dictionary<string, object>
            {
                ["ValidationTime"] = DateTime.UtcNow,
                ["IsValid"] = true
            });
        }
        catch (Exception ex)
        {
            return CreateFailureResult(ex);
        }
    }
}

// 注册步骤
services.AddScoped<CustomValidationStep>();

// 在事务定义中使用
var definition = new TransactionDefinition<MyTransactionContext>
{
    Name = "CustomTransaction",
    Steps = new List<ITransactionStep<MyTransactionContext>>
    {
        serviceProvider.GetRequiredService<CustomValidationStep>(),
        // ... 其他步骤
    }
};
```

## 错误处理

### 异常类型

| 异常类型 | 描述 | 处理建议 |
|---------|------|----------|
| `ArgumentNullException` | 参数为null | 检查输入参数 |
| `OperationCanceledException` | 操作被取消 | 检查取消令牌状态 |
| `TransactionTimeoutException` | 事务超时 | 调整超时配置或优化步骤性能 |
| `TransactionStepException` | 步骤执行失败 | 检查具体步骤的错误信息 |
| `CompensationException` | 补偿操作失败 | 检查数据状态和补偿逻辑 |

### 错误处理最佳实践

```csharp
try
{
    var result = await coordinator.ExecuteAsync(definition, context);
    return result;
}
catch (OperationCanceledException)
{
    // 处理取消情况
    logger.LogWarning("Transaction was cancelled");
    throw;
}
catch (TransactionTimeoutException ex)
{
    // 处理超时情况
    logger.LogError(ex, "Transaction timed out after {Timeout}", ex.Timeout);
    throw;
}
catch (TransactionStepException ex)
{
    // 处理步骤失败
    logger.LogError(ex, "Transaction step {StepName} failed", ex.StepName);
    throw;
}
catch (Exception ex)
{
    // 处理其他异常
    logger.LogError(ex, "Unexpected error during transaction execution");
    throw;
}
```

---

**最后更新**: 2025-01-31  
**版本**: 1.0