# 统一错误处理服务 - 使用指南

## 概述

统一错误处理服务 (IErrorHandlingService) 是为 LYBTZYZS 项目设计的企业级错误处理解决方案，提供：

- 🔍 **智能错误分类**：自动识别和分类不同类型的错误
- 🌏 **用户友好提示**：将技术错误转换为用户可理解的中文消息
- 📝 **结构化日志**：详细记录错误信息，便于问题追踪和调试
- 🔄 **全局异常捕获**：处理未捕获的异常，提升应用稳定性
- 💡 **错误恢复建议**：为常见错误提供解决方案和操作建议
- 🎨 **丰富的UI支持**：错误通知组件和详情对话框

## 核心特性

### 1. 错误分类系统

错误按类型和严重程度进行分类：

**错误类型 (ErrorCategory)：**
- `Network` - 网络相关错误
- `Authentication` - 认证和授权错误
- `Validation` - 数据验证错误
- `Business` - 业务逻辑错误
- `System` - 系统内部错误
- `UserOperation` - 用户操作错误

**严重程度 (ErrorSeverity)：**
- `Info` - 信息提示
- `Warning` - 警告
- `Error` - 错误
- `Critical` - 严重错误
- `Fatal` - 致命错误

### 2. 自定义异常类型

提供专门的异常类型以更好地表达错误语义：

```csharp
// 业务异常
throw new BusinessException("当前状态不允许此操作", "INVALID_STATE", ErrorSeverity.Warning);

// 验证异常
var validationEx = new ValidationException("数据验证失败");
validationEx.AddError("姓名", "姓名不能为空");
validationEx.AddError("年龄", "年龄必须在0-150之间");
throw validationEx;

// 网络异常
throw new NetworkException("网络连接失败", HttpStatusCode.ServiceUnavailable, canRetry: true);

// 认证异常
throw AuthenticationException.TokenExpired();
```

### 3. 错误上下文信息

每个错误都包含丰富的上下文信息：

```csharp
var context = new ErrorContext
{
    OperationName = "患者信息保存",
    ModuleName = "Patients",
    ViewName = "PatientRegistration",
    UserId = "user123",
    Username = "张医生"
};
context.AddData("PatientId", patientId);
context.AddData("FormData", formData);
```

## 使用方法

### 1. 基本用法

#### 在 ViewModel 中使用（推荐方式）

```csharp
public class PatientViewModel : BaseViewModel
{
    public PatientViewModel(IEventAggregator eventAggregator, IErrorHandlingService errorHandlingService)
        : base(eventAggregator, errorHandlingService)
    {
    }

    public async Task SavePatientAsync()
    {
        // 方式1：使用安全执行方法（推荐）
        var success = await ExecuteSafelyAsync(
            async () => await _patientService.SaveAsync(Patient),
            operationName: "保存患者信息",
            showErrorDialog: true
        );

        if (success)
        {
            StatusMessage = "患者信息保存成功";
        }

        // 方式2：手动异常处理
        try
        {
            await _patientService.SaveAsync(Patient);
            StatusMessage = "患者信息保存成功";
        }
        catch (Exception ex)
        {
            await HandleErrorAsync("保存患者信息", ex, showDialog: true);
        }
    }
}
```

#### 在服务类中使用

```csharp
public class PatientService
{
    private readonly IErrorHandlingService _errorHandlingService;

    public PatientService(IErrorHandlingService errorHandlingService)
    {
        _errorHandlingService = errorHandlingService;
    }

    public async Task<Patient> GetPatientAsync(int id)
    {
        var context = new ErrorContext
        {
            OperationName = "获取患者信息",
            ModuleName = "PatientService"
        };
        context.AddData("PatientId", id);

        return await _errorHandlingService.ExecuteSafelyAsync(
            async () => await _repository.GetByIdAsync(id),
            context,
            showErrorDialog: false // 服务层通常不直接显示UI
        );
    }
}
```

### 2. 高级用法

#### 自定义错误处理

```csharp
// 手动处理异常并显示详细信息
try
{
    await SomeComplexOperation();
}
catch (Exception ex)
{
    var context = new ErrorContext
    {
        OperationName = "复杂操作",
        ModuleName = "Business",
        ViewName = "ComplexView"
    };

    var handledError = await _errorHandlingService.HandleExceptionAsync(ex, context);
    
    // 根据错误类型采取不同措施
    switch (handledError.Category)
    {
        case ErrorCategory.Network:
            // 网络错误：尝试离线模式
            await EnableOfflineMode();
            break;
        case ErrorCategory.Authentication:
            // 认证错误：重新登录
            await ReLogin();
            break;
        default:
            // 其他错误：显示给用户
            await _errorHandlingService.ShowErrorAsync(handledError);
            break;
    }
}
```

#### 监听错误事件

```csharp
public class ErrorMonitor
{
    public ErrorMonitor(IErrorHandlingService errorHandlingService)
    {
        // 监听所有错误
        errorHandlingService.ErrorOccurred += OnErrorOccurred;
        
        // 监听严重错误
        errorHandlingService.CriticalErrorOccurred += OnCriticalErrorOccurred;
    }

    private void OnErrorOccurred(object sender, HandledError handledError)
    {
        // 记录错误统计
        LogErrorStatistics(handledError);
        
        // 发送遥测数据
        SendTelemetryData(handledError);
    }

    private void OnCriticalErrorOccurred(object sender, HandledError handledError)
    {
        // 严重错误处理
        // 1. 保存当前状态
        SaveApplicationState();
        
        // 2. 发送紧急通知
        SendEmergencyNotification(handledError);
        
        // 3. 准备应用恢复
        PrepareApplicationRecovery();
    }
}
```

## UI 组件

### 1. 错误通知控件

```xaml
<!-- 在视图中使用错误通知控件 -->
<controls:ErrorNotificationControl 
    DataContext="{Binding ErrorNotificationViewModel}"
    Margin="16,8"/>
```

```csharp
// 在 ViewModel 中
public ErrorNotificationViewModel ErrorNotificationViewModel { get; }

private async void OnSomeError(HandledError handledError)
{
    ErrorNotificationViewModel.ShowError(handledError);
}
```

### 2. 错误详情对话框

错误详情对话框会在错误严重程度为 Error 及以上时自动显示，提供：
- 错误摘要和建议操作
- 完整的技术详情
- 上下文信息
- 复制错误信息功能
- 重试选项（如果支持）

## 配置和扩展

### 1. 服务注册

服务已在 `ServiceCollectionExtensions.cs` 中自动注册：

```csharp
// 错误处理服务
containerRegistry.RegisterSingleton<IErrorHandlingService, ErrorHandlingService>();
```

### 2. 全局异常处理

在 `App.xaml.cs` 的 `OnInitialized` 方法中自动配置：

```csharp
protected override void OnInitialized()
{
    base.OnInitialized();
    
    var errorHandlingService = Container.Resolve<IErrorHandlingService>();
    errorHandlingService.RegisterGlobalExceptionHandlers();
}
```

### 3. 自定义错误消息

在 `ErrorMessages.cs` 中定义错误消息：

```csharp
public static class ErrorMessages
{
    public static class Business
    {
        public const string CustomError = "自定义业务错误消息";
    }
}
```

### 4. 扩展错误处理逻辑

继承 `ErrorHandlingService` 并重写相关方法：

```csharp
public class CustomErrorHandlingService : ErrorHandlingService
{
    public CustomErrorHandlingService(ICommonDialogService dialogService, IUserSessionManager sessionManager)
        : base(dialogService, sessionManager)
    {
    }

    protected override string GetUserFriendlyMessage(Exception exception, string defaultMessage = null)
    {
        // 自定义错误消息逻辑
        return base.GetUserFriendlyMessage(exception, defaultMessage);
    }
}
```

## 最佳实践

### 1. ViewModel 层

- 优先使用 `ExecuteSafelyAsync` 方法
- 为每个操作提供有意义的操作名称
- 在关键操作中添加上下文数据

### 2. Service 层

- 不要在服务层直接显示UI错误对话框
- 使用 `showErrorDialog: false` 参数
- 让上层调用者决定如何处理错误

### 3. 异常抛出

- 使用合适的自定义异常类型
- 提供用户友好的错误消息
- 添加必要的上下文信息

### 4. 错误分类

- 网络错误：通常可重试
- 认证错误：需要重新登录
- 验证错误：需要用户修正输入
- 业务错误：需要业务逻辑调整

## 故障排除

### 常见问题

1. **ErrorHandlingService 无法注入**
   - 确保在 `ServiceCollectionExtensions.cs` 中已注册服务
   - 检查依赖注入容器配置

2. **BaseViewModel 构造函数错误**
   - 推荐使用包含 `IErrorHandlingService` 的构造函数
   - 旧的单参数构造函数有兼容性处理

3. **错误对话框不显示**
   - 检查错误严重程度设置
   - 确保在UI线程中调用
   - 验证 `ICommonDialogService` 是否正确注册

4. **全局异常处理不工作**
   - 确保在 App.xaml.cs 中调用了 `RegisterGlobalExceptionHandlers()`
   - 检查异常是否在正确的线程中抛出

### 调试技巧

- 查看调试输出窗口的错误日志
- 使用错误ID追踪特定错误
- 检查 `HandledError.TechnicalDetails` 获取详细信息
- 监听 `ErrorOccurred` 事件进行诊断

## 示例代码

完整的使用示例请参考：
- `Examples/ErrorHandlingExample.cs` - 各种使用场景的示例
- 现有ViewModel的改进示例
- UI组件集成示例

---

此错误处理系统提供了企业级的错误处理能力，帮助开发团队构建更稳定、更用户友好的应用程序。