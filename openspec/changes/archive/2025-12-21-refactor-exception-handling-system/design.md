# Design: refactor-exception-handling-system

**Created**: 2025-12-20
**Author**: Claude Code

---

## 架构概览

### 端到端异常处理流程

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              异常处理体系架构                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐  │
│  │   Service   │───▶│  Controller │───▶│  Exception  │───▶│ ProblemDetails│ │
│  │    Layer    │    │    Layer    │    │   Handler   │    │   Response  │  │
│  │             │    │             │    │    Chain    │    │             │  │
│  │ throw异常   │    │  无try-catch │    │ 统一转换    │    │ RFC 7807    │  │
│  └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘  │
│         │                                                        │          │
│         │                                                        ▼          │
│         │              ┌─────────────────────────────────────────────┐      │
│         │              │              HTTP Response                  │      │
│         │              │  Status: 400/404/409/500                   │      │
│         │              │  Body: ProblemDetails JSON                 │      │
│         │              └─────────────────────────────────────────────┘      │
│         │                                     │                             │
│         │                                     ▼                             │
│  ┌──────┴──────────────────────────────────────────────────────────────┐   │
│  │                         Desktop Client                               │   │
│  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐              │   │
│  │  │  HttpClient │───▶│  ApiClient  │───▶│  ViewModel  │              │   │
│  │  │   + Polly   │    │  解析响应   │    │ SafeExecute │              │   │
│  │  │             │    │             │    │             │              │   │
│  │  │ Retry/CB/TO │    │ → ApiException│   │ → 用户提示  │              │   │
│  │  └─────────────┘    └─────────────┘    └─────────────┘              │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 后端异常体系

### 异常类层级

```
Exception (System)
    └── AppException (Base)
            ├── BusinessException      [400 Bad Request]
            ├── ValidationException    [400 Bad Request]
            ├── NotFoundException      [404 Not Found]
            ├── ConflictException      [409 Conflict]
            ├── UnauthorizedException  [401 Unauthorized]
            ├── ForbiddenException     [403 Forbidden]
            ├── TransientException     [503 Service Unavailable] (NEW)
            └── RateLimitException     [429 Too Many Requests]  (NEW)
```

### ExceptionHandler链

```csharp
// 处理顺序（优先级从高到低）
1. ValidationExceptionHandler  → 400 + 验证错误详情
2. BusinessExceptionHandler    → 4xx + ErrorCode
3. SystemExceptionHandler      → 500 + 通用消息

// Program.cs配置
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
builder.Services.AddExceptionHandler<SystemExceptionHandler>();
builder.Services.AddProblemDetails();
```

### Service层异常抛出模式

```csharp
public class PatientService : IPatientService
{
    public async Task<Patient> GetByIdAsync(Guid id)
    {
        // 资源未找到 → NotFoundException
        var patient = await _repository.GetByIdAsync(id)
            ?? throw ExceptionFactory.NotFound(
                ErrorCode.PatientNotFound,
                $"患者 {id} 不存在");

        return patient;
    }

    public async Task<Patient> CreateAsync(CreatePatientDto dto)
    {
        // 业务规则验证 → BusinessException
        if (await _repository.ExistsByIdCardAsync(dto.IdCard))
            throw ExceptionFactory.Business(
                ErrorCode.PatientIdCardDuplicate,
                $"身份证号 {dto.IdCard} 已存在");

        // 数据验证 → ValidationException (由FluentValidation处理)
        // ...

        return await _repository.AddAsync(patient);
    }

    public async Task UpdateAsync(Guid id, UpdatePatientDto dto)
    {
        var patient = await GetByIdAsync(id);

        // 并发冲突 → ConflictException
        if (patient.Version != dto.Version)
            throw ExceptionFactory.Conflict(
                ErrorCode.ConcurrencyConflict,
                "数据已被其他用户修改，请刷新后重试");

        // ...
    }
}
```

---

## 前端异常体系

### ViewModelBase扩展

```csharp
public abstract class ViewModelBase : BindableBase
{
    protected readonly IExceptionDisplayService _exceptionDisplay;
    protected readonly INavigationService _navigation;
    protected readonly ILogger _logger;

    #region SafeExecuteAsync

    /// <summary>
    /// 安全执行异步操作（有返回值）
    /// </summary>
    protected async Task<T?> SafeExecuteAsync<T>(
        Func<Task<T>> action,
        string operationName,
        T? fallbackValue = default,
        Action<Exception>? onError = null)
    {
        try
        {
            IsBusy = true;
            BusyMessage = $"正在{operationName}...";
            return await action();
        }
        catch (ApiException ex)
        {
            await HandleApiExceptionAsync(ex, operationName);
            onError?.Invoke(ex);
            return fallbackValue;
        }
        catch (Exception ex)
        {
            await HandleUnexpectedExceptionAsync(ex, operationName);
            onError?.Invoke(ex);
            return fallbackValue;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    /// <summary>
    /// 安全执行异步操作（无返回值）
    /// </summary>
    protected async Task<bool> SafeExecuteAsync(
        Func<Task> action,
        string operationName,
        Action<Exception>? onError = null)
    {
        try
        {
            IsBusy = true;
            BusyMessage = $"正在{operationName}...";
            await action();
            return true;
        }
        catch (ApiException ex)
        {
            await HandleApiExceptionAsync(ex, operationName);
            onError?.Invoke(ex);
            return false;
        }
        catch (Exception ex)
        {
            await HandleUnexpectedExceptionAsync(ex, operationName);
            onError?.Invoke(ex);
            return false;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }

    #endregion

    #region Exception Handlers

    protected virtual async Task HandleApiExceptionAsync(
        ApiException ex,
        string operationName)
    {
        _logger.LogWarning(ex,
            "[{CorrelationId}] API异常: {Operation} - {StatusCode}",
            ex.CorrelationId, operationName, ex.StatusCode);

        switch (ex.StatusCode)
        {
            case 401:
                await HandleUnauthorizedAsync();
                break;

            case 403:
                await _exceptionDisplay.ShowErrorAsync(
                    "您没有权限执行此操作");
                break;

            case 409:
                await HandleConflictAsync(operationName);
                break;

            case 429:
                await _exceptionDisplay.ShowWarningAsync(
                    "操作过于频繁，请稍后再试");
                break;

            case 503:
            case 504:
                await _exceptionDisplay.ShowWarningAsync(
                    "服务暂时不可用，请稍后重试");
                break;

            default:
                var message = ClientErrorMessageMapper.GetMessage(ex.ErrorCode);
                await _exceptionDisplay.ShowErrorAsync(message);
                break;
        }
    }

    protected virtual async Task HandleUnauthorizedAsync()
    {
        await _exceptionDisplay.ShowWarningAsync(
            "登录已过期，请重新登录");

        // 清除会话
        await _authService.LogoutAsync();

        // 导航到登录页
        _navigation.NavigateTo("LoginView");
    }

    protected virtual async Task HandleConflictAsync(string operationName)
    {
        var result = await _exceptionDisplay.ShowConfirmAsync(
            "数据冲突",
            "数据已被其他用户修改，是否刷新页面获取最新数据？");

        if (result)
        {
            await RefreshDataAsync();
        }
    }

    protected virtual async Task HandleUnexpectedExceptionAsync(
        Exception ex,
        string operationName)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogError(ex,
            "[{CorrelationId}] 未预期异常: {Operation}",
            correlationId, operationName);

        await _exceptionDisplay.ShowErrorAsync(
            $"操作失败，请稍后重试。\n错误追踪码: {correlationId}");
    }

    /// <summary>
    /// 刷新数据（子类可重写）
    /// </summary>
    protected virtual Task RefreshDataAsync() => Task.CompletedTask;

    #endregion
}
```

### IExceptionDisplayService接口

```csharp
public interface IExceptionDisplayService
{
    /// <summary>显示错误消息</summary>
    Task ShowErrorAsync(string message, string? title = null);

    /// <summary>显示警告消息</summary>
    Task ShowWarningAsync(string message, string? title = null);

    /// <summary>显示确认对话框</summary>
    Task<bool> ShowConfirmAsync(string title, string message);
}

public class DialogExceptionDisplayService : IExceptionDisplayService
{
    private readonly IDialogService _dialogService;

    public async Task ShowErrorAsync(string message, string? title = null)
    {
        await _dialogService.ShowDialogAsync(
            title ?? "错误",
            message,
            DialogType.Error);
    }

    public async Task ShowWarningAsync(string message, string? title = null)
    {
        await _dialogService.ShowDialogAsync(
            title ?? "提示",
            message,
            DialogType.Warning);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        return await _dialogService.ShowConfirmDialogAsync(title, message);
    }
}
```

---

## HTTP韧性层设计

### Polly策略配置

```csharp
public static class HttpPolicyFactory
{
    /// <summary>
    /// 重试策略（仅幂等操作）
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    logger.LogWarning(
                        "HTTP请求失败，{RetryAttempt}秒后重试第{Attempt}次: {Reason}",
                        timespan.TotalSeconds,
                        retryAttempt,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// 熔断器策略
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    logger.LogWarning(
                        "熔断器打开，暂停{Seconds}秒: {Reason}",
                        breakDelay.TotalSeconds,
                        outcome.Exception?.Message);
                },
                onReset: () =>
                {
                    logger.LogInformation("熔断器重置，恢复正常");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("熔断器半开，尝试恢复");
                });
    }

    /// <summary>
    /// 超时策略
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(30),
            TimeoutStrategy.Optimistic);
    }
}
```

### HttpClient配置

```csharp
// Program.cs / App.xaml.cs
services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(configuration["ApiBaseUrl"]);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddPolicyHandler((sp, _) =>
    HttpPolicyFactory.GetRetryPolicy(sp.GetRequiredService<ILogger<ApiClient>>()))
.AddPolicyHandler((sp, _) =>
    HttpPolicyFactory.GetCircuitBreakerPolicy(sp.GetRequiredService<ILogger<ApiClient>>()))
.AddPolicyHandler(HttpPolicyFactory.GetTimeoutPolicy());
```

---

## 异常消息安全化

### ClientErrorMessageMapper扩展

```csharp
public static class ClientErrorMessageMapper
{
    private static readonly Dictionary<ErrorCode, string> _messages = new()
    {
        // 通用错误
        [ErrorCode.Unknown] = "操作失败，请稍后重试",
        [ErrorCode.ValidationFailed] = "数据验证失败，请检查输入",
        [ErrorCode.ConcurrencyConflict] = "数据已被修改，请刷新后重试",

        // 患者模块
        [ErrorCode.PatientNotFound] = "患者信息不存在",
        [ErrorCode.PatientIdCardDuplicate] = "该身份证号已被使用",

        // 病历模块
        [ErrorCode.MedicalCaseNotFound] = "病历不存在",
        [ErrorCode.MedicalCaseLocked] = "病历已锁定，无法修改",

        // 认证模块
        [ErrorCode.InvalidCredentials] = "用户名或密码错误",
        [ErrorCode.AccountLocked] = "账户已被锁定，请联系管理员",
        [ErrorCode.TokenExpired] = "登录已过期，请重新登录",

        // ... 其他ErrorCode
    };

    private const string DefaultMessage = "操作失败，请稍后重试";

    public static string GetMessage(ErrorCode? errorCode)
    {
        if (errorCode == null)
            return DefaultMessage;

        return _messages.TryGetValue(errorCode.Value, out var message)
            ? message
            : DefaultMessage;
    }

    public static string GetSafeMessage(Exception ex)
    {
        // 业务异常：使用ErrorCode映射
        if (ex is AppException appEx)
            return GetMessage(appEx.ErrorCode);

        // 系统异常：返回通用消息
        return DefaultMessage;
    }
}
```

### SensitiveInfoFilter

```csharp
public static class SensitiveInfoFilter
{
    private static readonly Regex[] _patterns = new[]
    {
        // 数据库连接字符串
        new Regex(@"(Server|Data Source|Initial Catalog|User Id|Password)=[^;]+", RegexOptions.IgnoreCase),

        // SQL语句
        new Regex(@"\b(SELECT|INSERT|UPDATE|DELETE|FROM|WHERE|JOIN)\b.*", RegexOptions.IgnoreCase),

        // 文件路径
        new Regex(@"[A-Z]:\\[^\s]+", RegexOptions.IgnoreCase),
        new Regex(@"/(?:home|var|usr|etc)/[^\s]+", RegexOptions.IgnoreCase),

        // 内部服务地址
        new Regex(@"(localhost|127\.0\.0\.1|192\.168\.\d+\.\d+):\d+"),

        // 认证令牌
        new Regex(@"(Bearer|JWT|Token)\s+[A-Za-z0-9\-_\.]+", RegexOptions.IgnoreCase),

        // 堆栈跟踪
        new Regex(@"at\s+[\w\.]+\([^)]*\)\s+in\s+.+:\s*line\s+\d+"),
    };

    public static string Filter(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var result = input;
        foreach (var pattern in _patterns)
        {
            result = pattern.Replace(result, "[已过滤]");
        }

        return result;
    }

    public static bool ContainsSensitiveInfo(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return _patterns.Any(p => p.IsMatch(input));
    }
}
```

---

## 决策记录

### ADR-001: 移除Result模式，统一使用异常

**决策**: Service层不再返回`Result<T>`，改为直接返回`T`并抛出异常

**理由**:
1. 异常可以被中间件统一处理，生成一致的ProblemDetails响应
2. CorrelationId可以在异常链中自动传递
3. 代码更简洁，不需要在每个调用点检查Result.IsSuccess
4. 与ASP.NET Core的IExceptionHandler机制更好地集成

**影响**:
- Service层方法签名变化
- 调用方需要try-catch或使用SafeExecuteAsync

### ADR-002: ViewModel使用SafeExecuteAsync而非全局异常处理

**决策**: 每个ViewModel操作使用SafeExecuteAsync包装，而非依赖全局异常处理

**理由**:
1. 可以针对不同操作提供不同的错误处理策略
2. 可以设置回退值，保持UI状态一致性
3. 可以在异常时执行自定义逻辑（如刷新数据）
4. 更好的IsBusy状态管理

**影响**:
- 需要迁移所有现有的try-catch代码
- ViewModelBase增加新方法

### ADR-003: Polly策略仅对幂等操作重试

**决策**: 重试策略仅应用于GET、PUT、DELETE等幂等操作，POST操作不自动重试

**理由**:
1. POST通常是创建操作，重试可能导致重复数据
2. 非幂等操作的重试需要业务层面的幂等性保证
3. 避免意外的副作用

**实现**:
- 使用自定义DelegatingHandler检查HTTP方法
- POST请求跳过重试策略
