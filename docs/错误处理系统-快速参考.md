# 错误处理系统 - 快速参考卡

## 🚀 快速开始

### 1️⃣ 安装包
```bash
dotnet add package Serilog --version 3.1.1
dotnet add package Polly --version 8.2.0
```

### 2️⃣ 注入服务
```csharp
public class MyViewModel
{
    private readonly IStructuredLoggingService _logging;
    private readonly IUserNotificationService _notification;
    
    public MyViewModel(
        IStructuredLoggingService logging,
        IUserNotificationService notification)
    {
        _logging = logging;
        _notification = notification;
    }
}
```

## 📝 常用代码片段

### 记录日志
```csharp
// 信息
_logging.LogInformation("操作成功");

// 警告
_logging.LogWarning("配置项缺失，使用默认值");

// 错误
_logging.LogError(ex, "数据库连接失败");

// 带参数
_logging.LogInformation("用户 {Username} 登录成功", username);
```

### 性能监控
```csharp
using (_logging.BeginPerformanceLog("LoadData"))
{
    // 耗时操作
    await LoadDataAsync();
}  // 自动记录耗时
```

### 操作日志
```csharp
_logging.LogOperation("SavePatient", new 
{ 
    PatientId = id, 
    Name = name 
});
```

### 审计日志
```csharp
_logging.LogAudit(
    action: "Update",
    entityType: "Patient", 
    entityId: patientId,
    oldValue: oldPatient,
    newValue: newPatient
);
```

### 业务事件
```csharp
_logging.LogBusinessEvent("PrescriptionCreated", new
{
    PrescriptionId = id,
    PatientName = name,
    Timestamp = DateTime.Now
});
```

## 🔔 用户通知

### 成功提示
```csharp
await _notification.ShowSuccessAsync("保存成功！");
```

### 错误提示
```csharp
await _notification.ShowErrorAsync(
    "操作失败，请重试",
    ErrorSeverity.Error
);
```

### 警告提示
```csharp
await _notification.ShowWarningAsync("数据将被覆盖");
```

### 信息提示
```csharp
await _notification.ShowInfoAsync("正在处理，请稍候...");
```

### 确认对话框
```csharp
var confirmed = await _notification.ShowConfirmationAsync(
    "确定要删除这条记录吗？",
    "确认删除"
);

if (confirmed)
{
    // 执行删除
}
```

### 输入对话框
```csharp
var name = await _notification.ShowInputAsync(
    "请输入患者姓名：",
    "新建患者",
    "张三"  // 默认值
);

if (!string.IsNullOrEmpty(name))
{
    // 使用输入的名称
}
```

## 🎯 异常处理

### 抛出业务异常
```csharp
throw new BusinessException("库存不足")
{
    UserFriendlyMessage = "药材库存不足，请联系药房",
    ErrorCode = "STOCK_001"
};
```

### 抛出验证异常
```csharp
throw new ValidationException("年龄必须大于0", "Age")
{
    InvalidValue = age
};
```

### 抛出网络异常
```csharp
throw new NetworkException(
    "API服务不可用",
    endpoint: "https://api.example.com",
    statusCode: 503
);
```

### 让全局处理器处理
```csharp
try
{
    // 危险操作
}
catch (Exception ex)
{
    _logging.LogError(ex, "操作失败");
    throw;  // 重新抛出，让全局处理器处理
}
```

## 📊 日志级别指南

| 级别 | 使用场景 | 示例 |
|------|---------|------|
| **Trace** | 详细跟踪 | SQL语句、详细参数 |
| **Debug** | 调试信息 | 变量值、流程步骤 |
| **Information** | 重要流程 | 登录、订单创建 |
| **Warning** | 潜在问题 | 配置缺失、重试 |
| **Error** | 错误但可恢复 | 数据库连接失败 |
| **Critical** | 严重错误 | 数据损坏、服务崩溃 |

## 🎨 错误严重程度

| 级别 | 说明 | 用户提示时长 |
|------|------|-------------|
| **Info** | 信息提示 | 3秒 |
| **Warning** | 需要注意 | 5秒 |
| **Error** | 操作失败 | 8秒 |
| **Critical** | 严重问题 | 10秒 |
| **Fatal** | 系统崩溃 | 不自动关闭 |

## 📁 日志文件

### 位置
```
C:\Users\[用户名]\AppData\Local\LYBT\Logs\
├── lybt-20250110.log    # 今天
├── lybt-20250109.log    # 昨天
└── ...                   # 保留30天
```

### 查看日志
```powershell
# PowerShell
Get-Content "$env:LOCALAPPDATA\LYBT\Logs\lybt-$(Get-Date -Format 'yyyyMMdd').log" -Tail 50
```

### 搜索错误
```powershell
# 搜索ERROR级别日志
Select-String -Path "$env:LOCALAPPDATA\LYBT\Logs\*.log" -Pattern '"Level":"Error"'
```

## 🛠️ 调试技巧

### 1. 启用详细日志
```csharp
#if DEBUG
_logging.LogTrace("详细调试信息: {Data}", complexObject);
#endif
```

### 2. 添加上下文
```csharp
using (_logging.BeginScope("PatientId: {PatientId}", patientId))
{
    // 这个范围内的所有日志都会包含PatientId
    _logging.LogInformation("开始处理患者数据");
    // ...
}
```

### 3. 关联ID追踪
```csharp
// 每个请求自动生成关联ID
// 在日志中查找 CorrelationId 字段
```

## ⚡ 性能建议

1. **异步日志**：日志写入是异步的，不会阻塞主线程
2. **批量写入**：日志会批量写入文件，提高性能
3. **条件日志**：使用日志级别控制，避免不必要的字符串构造
   ```csharp
   if (_logger.IsEnabled(LogLevel.Debug))
   {
       _logger.LogDebug("复杂对象: {Object}", ExpensiveToString());
   }
   ```

## 🔧 故障排查

### 日志不输出？
1. 检查日志级别配置
2. 确认NuGet包已安装
3. 查看事件查看器

### 通知不显示？
1. 确认服务已初始化
2. 检查主窗口是否存在
3. 查看调试输出

### 异常未捕获？
1. 确认全局处理器已注册
2. 检查是否被try-catch吞没
3. 查看FirstChanceException日志

## 📞 支持

遇到问题？查看：
- 完整文档：`docs/UltraThink错误处理系统-完整报告.md`
- NuGet包：`docs/错误处理系统-NuGet包要求.md`
- 源代码：`src/Frontend/Desktop/Core/`