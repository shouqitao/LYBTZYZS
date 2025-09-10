# 事务系统开发者指南

## 快速开始

### 1. 环境准备

确保你的开发环境包含以下组件：
- .NET 8 SDK
- SQL Server (LocalDB 或完整版本)
- Visual Studio 2022 或 VS Code
- Entity Framework Core Tools

### 2. 项目依赖

事务系统位于 `LYBT.Infrastructure` 项目中，主要依赖：
- Microsoft.EntityFrameworkCore (8.0.17)
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Caching.Memory

### 3. 基本配置

在 `Program.cs` 中添加事务系统服务：

```csharp
// 添加完整的事务系统
builder.Services.AddCompleteTransactionSystem();

// 或者按需添加组件
builder.Services.AddTransactionServices();           // 核心服务
builder.Services.AddConsultationTransactionServices(); // 诊疗事务
builder.Services.AddPrescriptionTransactionServices();  // 处方事务
builder.Services.AddTransactionHealthChecks();         // 健康检查
```

## 核心概念

### 1. 事务定义 (Transaction Definition)

事务定义描述了一个完整的业务流程，包含多个按顺序执行的步骤。

```csharp
var definition = new TransactionDefinition<MyTransactionContext>
{
    Name = "MyBusinessProcess",
    Description = "我的业务流程",
    Steps = new List<ITransactionStep<MyTransactionContext>>
    {
        new ValidateInputStep(),
        new ProcessDataStep(),
        new SaveResultStep()
    },
    Timeout = TimeSpan.FromMinutes(10),
    MaxRetryCount = 3,
    EnableAutoCompensation = true,
    EnableParallelExecution = false
};
```

### 2. 事务上下文 (Transaction Context)

上下文在事务步骤间传递数据和状态：

```csharp
public class MyTransactionContext : TransactionContext
{
    public string InputData { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Dictionary<string, object> ProcessingResults { get; set; } = new();
    
    // 验证上下文数据
    public (bool IsValid, List<string> Errors) ValidateContext()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(InputData))
            errors.Add("输入数据不能为空");
            
        if (EntityId == Guid.Empty)
            errors.Add("实体ID无效");
            
        return (errors.Count == 0, errors);
    }
}
```

### 3. 事务步骤 (Transaction Steps)

事务步骤实现具体的业务操作：

```csharp
public class ProcessDataStep : TransactionStepBase<MyTransactionContext>
{
    public override string StepName => "ProcessData";
    public override int Order => 2;
    public override bool SupportsCompensation => true;
    public override TimeSpan Timeout => TimeSpan.FromSeconds(30);

    private readonly IDataService _dataService;
    
    public ProcessDataStep(IDataService dataService)
    {
        _dataService = dataService;
    }

    public override async Task<TransactionStepResult> ExecuteAsync(
        MyTransactionContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 执行业务逻辑
            var result = await _dataService.ProcessAsync(context.InputData);
            
            // 更新上下文
            context.ProcessingResults["ProcessedData"] = result;
            context.EntityId = result.EntityId;
            
            // 返回成功结果
            return CreateSuccessResult(new Dictionary<string, object>
            {
                ["ProcessedCount"] = result.Count,
                ["ProcessingTime"] = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return CreateFailureResult(ex);
        }
    }

    public override async Task<TransactionStepResult> CompensateAsync(
        MyTransactionContext context,
        TransactionStepResult originalResult,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 回滚操作
            await _dataService.RollbackAsync(context.EntityId);
            
            return CreateSuccessResult();
        }
        catch (Exception ex)
        {
            return CreateFailureResult(ex);
        }
    }
}
```

## 内置步骤类型

### 1. 数据库事务步骤

用于涉及数据库操作的步骤：

```csharp
public class SaveEntityStep : DatabaseTransactionStep<MyTransactionContext>
{
    public override string StepName => "SaveEntity";
    public override int Order => 3;

    public SaveEntityStep(AppDbContext dbContext, ILogger<SaveEntityStep> logger)
        : base(dbContext, logger)
    {
    }

    protected override async Task<TransactionStepResult> ExecuteDatabaseOperationAsync(
        MyTransactionContext context, 
        CancellationToken cancellationToken = default)
    {
        // 创建实体
        var entity = new MyEntity
        {
            Id = context.EntityId,
            Data = context.InputData,
            CreatedAt = DateTime.UtcNow
        };

        // 保存到数据库
        await CreateEntityAsync(entity, cancellationToken);
        
        return CreateSuccessResult(new Dictionary<string, object>
        {
            ["EntityId"] = entity.Id
        });
    }

    public override async Task<TransactionStepResult> CompensateAsync(
        MyTransactionContext context,
        TransactionStepResult originalResult,
        CancellationToken cancellationToken = default)
    {
        // 删除已创建的实体
        await DeleteEntityAsync<MyEntity>(context.EntityId, cancellationToken);
        
        return CreateSuccessResult();
    }
}
```

### 2. 条件执行步骤

根据条件决定是否执行的步骤：

```csharp
public class ConditionalProcessStep : ConditionalTransactionStep<MyTransactionContext>
{
    public override string StepName => "ConditionalProcess";
    public override int Order => 4;

    protected override Task<bool> EvaluateConditionAsync(
        MyTransactionContext context, 
        CancellationToken cancellationToken = default)
    {
        // 检查条件
        bool shouldExecute = context.ProcessingResults.ContainsKey("RequiresAdditionalProcessing");
        return Task.FromResult(shouldExecute);
    }

    protected override Task<TransactionStepResult> ExecuteConditionalOperationAsync(
        MyTransactionContext context, 
        CancellationToken cancellationToken = default)
    {
        // 执行条件性操作
        Logger?.LogInformation("Executing conditional operation for {EntityId}", context.EntityId);
        
        return Task.FromResult(CreateSuccessResult());
    }
}
```

## 执行事务

### 1. 基本执行

```csharp
public class MyBusinessService
{
    private readonly ITransactionCoordinator<MyTransactionContext> _coordinator;
    
    public MyBusinessService(ITransactionCoordinator<MyTransactionContext> coordinator)
    {
        _coordinator = coordinator;
    }

    public async Task<TransactionResult<MyTransactionContext>> ExecuteBusinessProcessAsync(
        string inputData, 
        Guid entityId)
    {
        // 创建上下文
        var context = new MyTransactionContext
        {
            InputData = inputData,
            EntityId = entityId
        };

        // 创建事务定义
        var definition = CreateTransactionDefinition();

        // 执行事务
        var result = await _coordinator.ExecuteAsync(definition, context);

        return result;
    }

    private TransactionDefinition<MyTransactionContext> CreateTransactionDefinition()
    {
        return new TransactionDefinition<MyTransactionContext>
        {
            Name = "MyBusinessProcess",
            Description = "我的业务流程",
            Steps = new List<ITransactionStep<MyTransactionContext>>
            {
                _serviceProvider.GetRequiredService<ValidateInputStep>(),
                _serviceProvider.GetRequiredService<ProcessDataStep>(),
                _serviceProvider.GetRequiredService<SaveEntityStep>()
            },
            Timeout = TimeSpan.FromMinutes(5),
            MaxRetryCount = 2,
            EnableAutoCompensation = true
        };
    }
}
```

### 2. 处理事务结果

```csharp
var result = await ExecuteBusinessProcessAsync("test data", Guid.NewGuid());

switch (result.Status)
{
    case TransactionStatus.Completed:
        Console.WriteLine($"事务成功完成，耗时: {result.Duration}");
        break;
        
    case TransactionStatus.Failed:
        Console.WriteLine($"事务执行失败: {result.Message}");
        // 查看失败的步骤
        var failedSteps = result.ExecutedSteps.Where(s => s.Status == TransactionStepStatus.Failed);
        break;
        
    case TransactionStatus.Compensated:
        Console.WriteLine($"事务已补偿，补偿步骤数: {result.CompensatedSteps.Count}");
        break;
        
    case TransactionStatus.Cancelled:
        Console.WriteLine("事务被取消");
        break;
}
```

## 监控和调试

### 1. 查看事务日志

```csharp
public class TransactionMonitoringService
{
    private readonly ITransactionLogger _logger;

    public async Task<TransactionDetails> GetTransactionDetailsAsync(Guid transactionId)
    {
        return await _logger.GetTransactionByIdAsync(transactionId);
    }

    public async Task<PagedResult<TransactionHistoryItem>> GetRecentTransactionsAsync()
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-24);
        
        return await _logger.GetTransactionHistoryAsync(startTime, endTime, 1, 50);
    }
}
```

### 2. 实时性能监控

```csharp
public class PerformanceMonitoringService
{
    private readonly ITransactionMetrics _metrics;

    public async Task<TransactionMetricsSnapshot> GetCurrentStatusAsync()
    {
        return await _metrics.GetCurrentMetricsAsync();
    }

    public async Task<List<SlowTransactionAlert>> GetSlowTransactionsAsync()
    {
        return await _metrics.GetSlowTransactionAlertsAsync(5000); // 5秒阈值
    }

    public async Task<TransactionErrorStatistics> GetErrorAnalysisAsync()
    {
        return await _metrics.GetErrorStatisticsAsync(24); // 最近24小时
    }
}
```

### 3. 健康检查

健康检查端点：`GET /health/transaction`

手动检查：
```csharp
public class HealthCheckService
{
    private readonly TransactionHealthCheck _healthCheck;

    public async Task<HealthCheckResult> CheckSystemHealthAsync()
    {
        var context = new HealthCheckContext();
        return await _healthCheck.CheckHealthAsync(context);
    }
}
```

## 配置选项

### 1. 事务配置

```csharp
// appsettings.json
{
  "TransactionOptions": {
    "DefaultTimeout": "00:10:00",
    "MaxRetryCount": 3,
    "EnableAutoCompensation": true,
    "EnableParallelExecution": false,
    "LogLevel": "Information",
    "MetricsRetentionHours": 24,
    "SlowTransactionThresholdMs": 5000
  }
}

// 在代码中使用
services.Configure<TransactionOptions>(
    builder.Configuration.GetSection("TransactionOptions"));
```

### 2. 日志配置

```csharp
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "LYBT.Infrastructure.Transactions": "Information",
      "LYBT.Infrastructure.Transactions.Monitoring": "Debug"
    }
  }
}
```

## 测试

### 1. 单元测试

```csharp
[Fact]
public async Task ExecuteAsync_WithValidContext_ShouldReturnSuccess()
{
    // Arrange
    var context = new MyTransactionContext
    {
        InputData = "test",
        EntityId = Guid.NewGuid()
    };
    
    var step1 = new Mock<ITransactionStep<MyTransactionContext>>();
    step1.Setup(s => s.ExecuteAsync(It.IsAny<MyTransactionContext>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new TransactionStepResult { Status = TransactionStepStatus.Success });

    var definition = new TransactionDefinition<MyTransactionContext>
    {
        Name = "TestTransaction",
        Steps = new List<ITransactionStep<MyTransactionContext>> { step1.Object }
    };

    // Act
    var result = await _coordinator.ExecuteAsync(definition, context);

    // Assert
    result.Status.Should().Be(TransactionStatus.Completed);
    result.ExecutedSteps.Should().HaveCount(1);
}
```

### 2. 集成测试

```csharp
[Fact]
public async Task CreatePrescriptionTransaction_WithValidData_ShouldComplete()
{
    // Arrange
    var context = new PrescriptionTransactionContext
    {
        PatientId = TestData.ValidPatientId,
        DoctorId = TestData.ValidDoctorId,
        MedicalCaseId = TestData.ValidMedicalCaseId,
        Items = TestData.CreatePrescriptionItems()
    };

    // Act
    var transaction = _serviceProvider.GetRequiredService<CreatePrescriptionTransaction>();
    var result = await transaction.ExecuteAsync(context);

    // Assert
    result.Status.Should().Be(TransactionStatus.Completed);
    result.Context.PrescriptionId.Should().NotBeNull();
    
    // 验证数据库状态
    var prescription = await _dbContext.Prescriptions
        .FirstOrDefaultAsync(p => p.Id == result.Context.PrescriptionId);
    prescription.Should().NotBeNull();
}
```

## 常见问题

### Q: 如何处理长时间运行的步骤？

A: 设置合适的超时时间，或将长时间操作分解为多个小步骤：

```csharp
public override TimeSpan Timeout => TimeSpan.FromMinutes(30);

// 或者使用进度报告
public override async Task<TransactionStepResult> ExecuteAsync(...)
{
    var progress = new Progress<int>(percent => 
        Logger?.LogInformation("Processing: {Percent}%", percent));
        
    await LongRunningOperation(progress, cancellationToken);
}
```

### Q: 如何实现自定义重试策略？

A: 重写步骤的重试逻辑：

```csharp
public class RetryableStep : TransactionStepBase<MyTransactionContext>
{
    public override async Task<TransactionStepResult> ExecuteAsync(...)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                return await DoWork();
            }
            catch (TemporaryException ex) when (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                // 继续重试
            }
        }
        
        throw new Exception("所有重试均失败");
    }
}
```

### Q: 如何处理外部系统调用？

A: 使用专门的外部API步骤基类：

```csharp
public class CallExternalApiStep : TransactionStepBase<MyTransactionContext>
{
    private readonly HttpClient _httpClient;
    private readonly ICircuitBreaker _circuitBreaker;

    public override async Task<TransactionStepResult> ExecuteAsync(...)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsync("api/endpoint", content);
            return ProcessResponse(response);
        });
    }
}
```

### Q: 如何实现事务的暂停和恢复？

A: 使用持久化的事务状态：

```csharp
public class PersistentTransactionContext : TransactionContext
{
    public string SerializeState()
    {
        return JsonSerializer.Serialize(this);
    }

    public static PersistentTransactionContext DeserializeState(string json)
    {
        return JsonSerializer.Deserialize<PersistentTransactionContext>(json);
    }
}
```

## 性能优化建议

### 1. 数据库优化
- 使用批量操作减少数据库往返
- 合理设置连接池大小
- 为事务日志表创建适当索引

### 2. 内存优化
- 及时清理完成的事务上下文
- 使用对象池重用实例
- 定期清理指标缓存

### 3. 并发优化
- 合理使用并行执行
- 避免长时间持有锁
- 使用异步编程模式

## 扩展指南

### 添加新的步骤类型

1. **继承适当的基类**：
```csharp
public class MyCustomStep : TransactionStepBase<MyTransactionContext>
{
    // 实现必要的方法
}
```

2. **注册依赖注入**：
```csharp
services.AddScoped<MyCustomStep>();
```

3. **在事务定义中使用**：
```csharp
Steps = new List<ITransactionStep<MyTransactionContext>>
{
    serviceProvider.GetRequiredService<MyCustomStep>()
}
```

### 添加自定义监控指标

1. **扩展ITransactionMetrics接口**
2. **实现自定义指标收集逻辑**
3. **更新健康检查包含新指标**

## 最佳实践

1. **事务粒度**：保持事务简洁，避免过长的事务
2. **幂等性**：确保步骤可以安全重复执行
3. **补偿逻辑**：为每个有副作用的步骤提供补偿操作
4. **错误处理**：区分业务错误和系统错误
5. **日志记录**：记录足够的信息用于调试
6. **性能监控**：定期检查慢事务和错误率

---

**最后更新**: 2025-01-31  
**版本**: 1.0