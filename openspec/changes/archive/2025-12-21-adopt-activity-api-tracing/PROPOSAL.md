# adopt-activity-api-tracing

## 概述

采用.NET原生Activity API替换自定义CorrelationId实现，统一分布式追踪机制。

## 动机

当前系统存在多个分散的CorrelationId实现：
- `LYBT.Shared.Logging.Abstractions.AsyncLocalCorrelationIdProvider`
- `LYBT.Desktop.Foundation.Logging.CorrelationIdContext`
- `LYBT.Desktop.Infrastructure.Http.CorrelationIdDelegatingHandler`

这些自定义实现存在以下问题：
1. **维护成本高** - 需维护多个AsyncLocal管理类
2. **非标准格式** - 使用Guid格式，不兼容W3C TraceContext
3. **手动HTTP传播** - 需自定义DelegatingHandler添加header
4. **不兼容OpenTelemetry** - 无法与现代可观测性工具集成

## 方案

采用`System.Diagnostics.Activity` API作为统一的分布式追踪解决方案。

### 技术优势

| 维度 | 当前方案 | Activity API方案 |
|------|---------|-----------------|
| 实现方式 | 自定义AsyncLocal | .NET内置Activity |
| ID格式 | Guid (32字符) | W3C TraceId (32字符hex) |
| 异步传播 | 手动管理 | 自动传播Activity.Current |
| HTTP传播 | 自定义DelegatingHandler | HttpClient自动添加traceparent头 |
| 日志集成 | 手动enricher | Serilog原生TraceId enricher |
| OpenTelemetry | 不兼容 | 完全兼容 |

### 实现步骤

#### Phase 1: 基础设施准备

1. **配置Serilog TraceId Enricher**
   ```csharp
   // Program.cs 或 Serilog配置
   .Enrich.WithProperty("TraceId", () => Activity.Current?.TraceId.ToString())
   ```

2. **创建Activity辅助类** (可选，简化使用)
   ```csharp
   namespace LYBT.Shared.Logging;
   
   public static class TraceContext
   {
       public static string? CurrentTraceId => Activity.Current?.TraceId.ToString();
       
       public static string TraceIdOrNew => 
           Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
       
       public static Activity StartActivity(string operationName)
       {
           return new Activity(operationName).Start();
       }
   }
   ```

#### Phase 2: 迁移使用处

| 文件 | 当前代码 | 迁移后 |
|-----|---------|--------|
| ViewModelBase.cs | `CorrelationIdContext.CurrentOrNew` | `TraceContext.TraceIdOrNew` |
| ClientErrorMessageMapper.cs | `CorrelationIdContext.CurrentOrNew` | `TraceContext.TraceIdOrNew` |
| UserCommandHandler.cs | `CorrelationIdContext.BeginScope()` | 删除(Activity自动传播) |

#### Phase 3: 删除旧代码

删除以下文件：
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Logging/CorrelationIdContext.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Http/CorrelationIdDelegatingHandler.cs`
- `src/Shared/LYBT.Shared.Logging/Abstractions/AsyncLocalCorrelationIdProvider.cs` (评估是否仍需)
- `src/Shared/LYBT.Shared.Logging/CorrelationId.cs` (如已创建)

#### Phase 4: HttpClient配置优化

HttpClient在.NET 8中默认启用W3C TraceContext传播，删除自定义DelegatingHandler：

```csharp
// 删除此配置
services.AddHttpClient("ApiClient")
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

// 保持简洁配置，HttpClient自动处理
services.AddHttpClient("ApiClient");
```

### 风险评估

- **低风险**: 迁移逻辑简单，一对一替换
- **兼容性**: Activity API在.NET 8完全支持
- **回滚能力**: 可保留TraceContext适配器快速回滚

### 验收标准

1. 所有日志包含有效的TraceId
2. HTTP请求自动携带traceparent头
3. 删除所有自定义CorrelationId类
4. 编译通过，无警告
5. 单元测试通过

## 影响范围

### 修改文件
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/ViewModels/ViewModelBase.cs`
- `src/Client/Desktop/Infrastructure/LYBT.Desktop.Infrastructure/Localization/ClientErrorMessageMapper.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/Components/UserCommandHandler.cs`
- Serilog配置文件

### 删除文件
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Logging/CorrelationIdContext.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Http/CorrelationIdDelegatingHandler.cs`

### 新增文件
- `src/Shared/LYBT.Shared.Logging/TraceContext.cs` (轻量辅助类)

## 时间线

- 创建日期: 2025-12-20
- 状态: proposed

## 参考资料

- [System.Diagnostics.Activity Class](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity)
- [Distributed tracing in .NET](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [Serilog.Enrichers.Span](https://github.com/serilog/serilog-enrichers-span)
