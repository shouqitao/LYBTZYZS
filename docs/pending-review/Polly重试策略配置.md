# Polly 重试策略配置

## 概述

系统已配置 Polly 库来为所有 HTTP 请求提供自动重试和超时处理功能。这提高了系统在网络不稳定或服务暂时不可用时的可靠性。

## 配置详情

### 1. 重试策略

- **重试次数**：3 次
- **重试间隔**：指数退避（2秒、4秒、8秒）
- **重试条件**：
  - HTTP 5XX 错误（服务器错误）
  - HTTP 408（请求超时）
  - HttpRequestException（网络错误）
  - 其他非成功状态码（除了 401 未授权）

### 2. 超时策略

- **超时时间**：60 秒
- **超时策略**：悲观策略（Pessimistic）

### 3. 不重试的情况

- HTTP 401（未授权）- 避免因为认证问题导致无意义的重试

## 实现位置

### HttpClientFactory.cs
位置：`src/Frontend/Desktop/Infrastructure/HttpClientFactory.cs`

负责创建配置了 Polly 策略的 HttpClient 实例。

### ServiceCollectionExtensions.cs
位置：`src/Frontend/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

在依赖注入容器中注册使用 Polly 的 HttpClient。

## 使用方式

所有通过 Refit 创建的 API 服务都会自动使用配置好的重试策略：

```csharp
// 自动包含重试策略
var response = await _userApiService.GetUsersAsync();
```

## 日志输出

重试时会在调试输出中记录：
```
[Polly] 重试 1/3: GET https://localhost:7001/api/v1/users 等待 2 秒后重试
[Polly] 重试 2/3: GET https://localhost:7001/api/v1/users 等待 4 秒后重试
[Polly] 重试 3/3: GET https://localhost:7001/api/v1/users 等待 8 秒后重试
```

## 最佳实践

1. **幂等性**：确保 API 操作是幂等的，特别是 POST、PUT、DELETE 操作
2. **超时设置**：根据具体业务调整超时时间
3. **错误处理**：即使有重试，也要妥善处理最终失败的情况
4. **监控**：在生产环境中监控重试频率，及时发现服务质量问题

## 自定义配置

如需调整重试策略，修改 `HttpClientFactory.GetRetryPolicy()` 方法：

```csharp
// 修改重试次数和间隔
.WaitAndRetryAsync(
    5, // 重试 5 次
    retryAttempt => TimeSpan.FromSeconds(retryAttempt), // 线性退避
    ...
)
```