# CCPM 错误代码参考手册

## 概述

本手册提供CCPM (Code-Claude Project Manager) 系统中所有错误代码的详细说明、产生原因和解决方案。基于LYBTZYZS项目实际运行过程中收集的错误信息编写。

## 错误代码分类

### 系统级错误 (1000-1999)
- **1000-1099**: 应用启动和初始化错误
- **1100-1199**: 配置和环境错误
- **1200-1299**: 依赖注入和服务错误
- **1300-1399**: 认证和授权错误

### 编译级错误 (2000-2999)
- **2000-2099**: 语法和编译错误
- **2100-2199**: 类型和命名空间错误
- **2200-2299**: 项目引用错误
- **2300-2399**: NuGet包版本错误

### 运行时错误 (3000-3999)
- **3000-3099**: 数据库操作错误
- **3100-3199**: API请求处理错误
- **3200-3299**: 业务逻辑错误
- **3300-3399**: 文件和IO错误

### 性能和资源错误 (4000-4999)
- **4000-4099**: 内存和CPU错误
- **4100-4199**: 网络连接错误
- **4200-4299**: 超时错误
- **4300-4399**: 资源限制错误

## 详细错误代码说明

## 系统级错误 (1000-1999)

### 1001 - 应用启动失败
**错误信息**: Application failed to start due to startup configuration error

**产生原因**:
- Program.cs 或 Startup.cs 中的配置错误
- 必要的配置文件缺失
- 环境变量未正确设置

**解决方案**:
```csharp
// 检查 Program.cs 中的服务配置
var builder = WebApplication.CreateBuilder(args);

// 确保所有必要的服务都已注册
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 检查配置文件存在
if (!File.Exists("appsettings.json"))
{
    throw new FileNotFoundException("Configuration file appsettings.json not found");
}
```

**检查命令**:
```bash
# 检查配置文件
dir appsettings*.json

# 检查环境变量
echo %ASPNETCORE_ENVIRONMENT%

# 详细启动日志
dotnet run --verbosity diagnostic
```

### 1101 - 数据库连接字符串配置错误
**错误信息**: Invalid connection string configuration

**产生原因**:
- 连接字符串格式不正确
- 数据库服务器不可达
- 认证凭据错误

**解决方案**:
```json
// appsettings.json 正确配置格式
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=30;"
  }
}
```

**测试命令**:
```bash
# 测试数据库连接
sqlcmd -S localhost -E -Q "SELECT @@VERSION"

# 检查SQL Server服务状态
sc query MSSQLSERVER
```

### 1201 - 依赖注入容器配置错误
**错误信息**: DI container configuration error - service not registered

**产生原因**:
- 服务未在DI容器中注册
- 服务注册顺序错误
- 循环依赖问题

**解决方案**:
```csharp
// 检查服务注册 - ServiceCollectionExtensions.cs
public static IServiceCollection AddUserModule(this IServiceCollection services)
{
    // 确保按正确顺序注册服务
    services.AddTransient<IUserRepository, UserRepository>();
    services.AddTransient<UserQueryService>();
    services.AddTransient<UserBusinessService>();
    services.AddTransient<IUserService, UserModule>();
    
    return services;
}

// 检查 ViewModel 构造函数
public UserManagementViewModel(IUserService userService, IMapper mapper)
{
    _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
}
```

### 1301 - JWT认证配置错误
**错误信息**: JWT authentication configuration error

**产生原因**:
- JWT密钥长度不足
- Token过期时间配置错误
- 认证中间件配置顺序错误

**解决方案**:
```csharp
// JWT配置检查 - Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JWT");
        var secretKey = jwtSettings["SecretKey"];
        
        // 确保密钥长度至少32字符
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
        }
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });
```

## 编译级错误 (2000-2999)

### 2001 - CS0246 类型或命名空间名不存在
**错误信息**: The type or namespace name 'TypeName' could not be found

**产生原因**:
- 缺少 using 语句
- 项目引用缺失
- NuGet包未正确安装

**解决方案**:
```csharp
// 添加必要的 using 语句
using LYBT.Shared.Models;
using LYBT.Infrastructure.Data;
using LYBT.Desktop.Services;

// 检查项目引用
<ProjectReference Include="..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />

// 检查包引用
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
```

**修复命令**:
```bash
# 添加项目引用
dotnet add reference path\to\project.csproj

# 添加包引用
dotnet add package PackageName

# 清理和重建
dotnet clean && dotnet restore && dotnet build
```

### 2101 - CS1061 不包含定义，也找不到可访问的扩展方法
**错误信息**: 'Type' does not contain a definition for 'Method'

**产生原因**:
- 方法名拼写错误
- 方法访问级别问题
- 接口实现不完整

**解决方案**:
```csharp
// 检查接口定义和实现
public interface IUserService
{
    Task<ServiceResult<User>> GetByIdAsync(Guid id);  // 确保方法签名正确
    Task<ServiceResult<User>> CreateUserAsync(UserCreateDto dto);
}

public class UserModule : IUserService
{
    // 确保实现所有接口方法
    public async Task<ServiceResult<User>> GetByIdAsync(Guid id)
    {
        return await _businessService.GetByIdAsync(id);
    }
    
    public async Task<ServiceResult<User>> CreateUserAsync(UserCreateDto dto)
    {
        return await _businessService.CreateUserAsync(dto);
    }
}
```

### 2201 - 项目引用循环依赖
**错误信息**: Project reference creates circular dependency

**产生原因**:
- A项目引用B项目，B项目同时引用A项目
- 传递性循环依赖

**解决方案**:
```bash
# 分析项目依赖关系
dotnet list reference

# 重新设计项目结构，避免循环引用
# 通常的解决方案是提取共享接口到独立项目

Project Structure:
├── LYBT.Shared.Interfaces/     # 共享接口
├── LYBT.Infrastructure/        # 数据访问层
├── LYBT.Server.Services/       # 业务服务层
└── LYBT.Desktop/              # 前端应用层
```

### 2301 - NuGet包版本冲突
**错误信息**: Package version conflict detected

**产生原因**:
- 同一包在不同项目中使用不同版本
- 依赖包的传递性版本冲突

**解决方案**:
```xml
<!-- Directory.Packages.props 统一版本管理 -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
    <PackageVersion Include="AutoMapper" Version="15.0.1" />
    <PackageVersion Include="Prism.DryIoc" Version="9.0.537" />
  </ItemGroup>
</Project>
```

**检查命令**:
```bash
# 检查包版本冲突
dotnet list package --include-transitive | findstr "Version conflict"

# 更新所有包到最新兼容版本
dotnet list package --outdated
```

## 运行时错误 (3000-3999)

### 3001 - 数据库连接超时
**错误信息**: Database connection timeout occurred

**产生原因**:
- 数据库服务器响应慢
- 连接池耗尽
- 网络延迟过高

**解决方案**:
```csharp
// 优化DbContext配置
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);              // 命令超时30秒
        sqlOptions.EnableRetryOnFailure(3);         // 重试3次
    })
    .EnableSensitiveDataLogging(isDevelopment)       // 开发环境启用敏感数据日志
    .EnableDetailedErrors(isDevelopment));           // 开发环境启用详细错误

// 连接池配置
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=LYBTDB;Integrated Security=true;Connection Timeout=30;Max Pool Size=100;Min Pool Size=5;"
}
```

### 3101 - API请求参数验证失败
**错误信息**: API request validation failed

**产生原因**:
- 请求参数格式不正确
- 必需参数缺失
- 参数值超出有效范围

**解决方案**:
```csharp
// 使用数据注解进行验证
public class UserCreateDto
{
    [Required(ErrorMessage = "用户名是必需的")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50字符之间")]
    public string Username { get; set; }

    [Required(ErrorMessage = "密码是必需的")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "密码必须至少8位，包含大小写字母、数字和特殊字符")]
    public string Password { get; set; }

    [Required(ErrorMessage = "角色是必需的")]
    public UserRole Role { get; set; }
}

// 控制器中的验证处理
[HttpPost]
public async Task<ActionResult<ApiResponse<User>>> CreateUser([FromBody] UserCreateDto dto)
{
    if (!ModelState.IsValid)
    {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage);
        return BadRequest(new { Errors = errors });
    }
    
    var result = await _userService.CreateUserAsync(dto);
    return HandleServiceResult(result, "用户创建成功");
}
```

### 3201 - 业务规则验证失败
**错误信息**: Business rule validation failed

**产生原因**:
- 违反业务约束条件
- 数据状态不一致
- 并发操作冲突

**解决方案**:
```csharp
// 在BusinessService中实现业务规则验证
public async Task<ServiceResult<User>> CreateUserAsync(UserCreateDto dto)
{
    // 检查用户名唯一性
    var existingUser = await _repository.GetByUsernameAsync(dto.Username);
    if (existingUser != null)
    {
        return ServiceResult<User>.Failure("用户名已存在", "DUPLICATE_USERNAME", 3201);
    }
    
    // 检查角色权限（例如：只有Admin可以创建Admin用户）
    if (dto.Role == UserRole.Admin && !_currentUser.IsAdmin)
    {
        return ServiceResult<User>.Failure("权限不足，无法创建管理员用户", "INSUFFICIENT_PRIVILEGES", 3202);
    }
    
    // 执行创建操作
    var user = _mapper.Map<User>(dto);
    user.Password = _passwordHelper.HashPassword(dto.Password);
    
    var createdUser = await _repository.CreateAsync(user);
    return ServiceResult<User>.Success(createdUser);
}
```

### 3301 - 文件访问权限错误
**错误信息**: File access permission denied

**产生原因**:
- 应用程序没有文件访问权限
- 文件被其他进程锁定
- 磁盘空间不足

**解决方案**:
```csharp
// 安全的文件操作实现
public async Task<ServiceResult<string>> SaveFileAsync(IFormFile file, string directory)
{
    try
    {
        // 检查目录权限
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // 检查磁盘空间
        var drive = new DriveInfo(Path.GetPathRoot(directory));
        if (drive.AvailableFreeSpace < file.Length * 2) // 预留双倍空间
        {
            return ServiceResult<string>.Failure("磁盘空间不足", "INSUFFICIENT_DISK_SPACE", 3301);
        }
        
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(directory, fileName);
        
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        return ServiceResult<string>.Success(filePath);
    }
    catch (UnauthorizedAccessException ex)
    {
        return ServiceResult<string>.Failure("文件访问权限不足", "FILE_ACCESS_DENIED", 3301);
    }
    catch (DirectoryNotFoundException ex)
    {
        return ServiceResult<string>.Failure("目录不存在", "DIRECTORY_NOT_FOUND", 3302);
    }
    catch (IOException ex)
    {
        return ServiceResult<string>.Failure($"文件操作失败：{ex.Message}", "FILE_IO_ERROR", 3303);
    }
}
```

## 性能和资源错误 (4000-4999)

### 4001 - 内存使用过高警告
**错误信息**: High memory usage detected

**产生原因**:
- 内存泄漏
- 大对象堆积压
- 缓存数据过多

**解决方案**:
```csharp
// 实现内存监控和清理
public class MemoryMonitoringService
{
    private readonly ILogger<MemoryMonitoringService> _logger;
    private readonly IMemoryCache _cache;
    
    public async Task MonitorMemoryUsageAsync()
    {
        var process = Process.GetCurrentProcess();
        var memoryUsage = process.WorkingSet64 / 1024 / 1024; // MB
        
        if (memoryUsage > 512) // 超过512MB时警告
        {
            _logger.LogWarning("High memory usage detected: {MemoryUsage}MB", memoryUsage);
            
            // 清理缓存
            if (_cache is MemoryCache mc)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);
                var coherentState = field.GetValue(mc);
                var entriesCollection = coherentState.GetType().GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                var entries = (IDictionary)entriesCollection.GetValue(coherentState);
                
                var expiredKeys = new List<object>();
                foreach (DictionaryEntry entry in entries)
                {
                    // 移除过期的缓存项
                    expiredKeys.Add(entry.Key);
                }
                
                foreach (var key in expiredKeys.Take(entries.Count / 2)) // 清理一半
                {
                    _cache.Remove(key);
                }
            }
            
            // 强制垃圾回收
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}

// 配置内存限制
services.Configure<MemoryCacheOptions>(options =>
{
    options.SizeLimit = 1024; // 限制缓存项数量
    options.CompactionPercentage = 0.25; // 压缩百分比
});
```

### 4101 - HTTP请求超时
**错误信息**: HTTP request timeout

**产生原因**:
- 网络连接不稳定
- 服务器响应慢
- 客户端超时设置过短

**解决方案**:
```csharp
// 配置HTTP客户端超时
services.AddHttpClient<ApiClient>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // 5分钟超时
    client.DefaultRequestHeaders.Add("User-Agent", "LYBT-System/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    })
.AddPolicyHandler(GetRetryPolicy()); // 添加重试策略

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                var logger = context.GetLogger();
                logger?.LogWarning("HTTP request retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
            });
}
```

## 错误处理最佳实践

### 统一错误响应格式

```csharp
public class ErrorResponse
{
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public string ErrorDetails { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; }
}

public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ErrorResponse
        {
            ErrorCode = GetErrorCode(exception),
            ErrorMessage = GetErrorMessage(exception),
            ErrorDetails = exception.Message,
            Timestamp = DateTime.UtcNow,
            RequestId = context.TraceIdentifier
        };
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);
        
        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
```

### 错误日志记录

```csharp
public class StructuredErrorLogging
{
    private readonly ILogger<StructuredErrorLogging> _logger;
    
    public void LogError(Exception ex, string operation, object context = null)
    {
        _logger.LogError(ex, 
            "Operation {Operation} failed. Context: {@Context}. ErrorCode: {ErrorCode}",
            operation, context, GetErrorCode(ex));
    }
    
    public void LogWarning(string message, int errorCode, object context = null)
    {
        _logger.LogWarning(
            "{Message}. ErrorCode: {ErrorCode}. Context: {@Context}",
            message, errorCode, context);
    }
}
```

## 监控和告警

### 错误频率监控

```csharp
public class ErrorFrequencyMonitor
{
    private readonly Dictionary<int, int> _errorCounts = new();
    private readonly Timer _resetTimer;
    
    public ErrorFrequencyMonitor()
    {
        _resetTimer = new Timer(ResetCounts, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }
    
    public void RecordError(int errorCode)
    {
        _errorCounts[errorCode] = _errorCounts.GetValueOrDefault(errorCode, 0) + 1;
        
        // 如果错误频率过高，发送告警
        if (_errorCounts[errorCode] > 10)
        {
            SendAlert(errorCode, _errorCounts[errorCode]);
        }
    }
    
    private void SendAlert(int errorCode, int count)
    {
        // 实现告警逻辑（邮件、短信、钉钉等）
        Console.WriteLine($"ALERT: Error {errorCode} occurred {count} times in the last hour");
    }
}
```

## 快速诊断脚本

```powershell
# error-diagnosis.ps1
param(
    [Parameter(Mandatory=$true)]
    [int]$ErrorCode,
    
    [switch]$ShowLogs,
    [switch]$AutoFix
)

Write-Host "=== 错误诊断工具 ===" -ForegroundColor Green
Write-Host "诊断错误代码: $ErrorCode" -ForegroundColor Cyan

# 根据错误代码范围提供诊断建议
switch ($ErrorCode) {
    {$_ -ge 1000 -and $_ -le 1999} {
        Write-Host "系统级错误检查..." -ForegroundColor Yellow
        # 检查服务状态
        Get-Service -Name "*SQL*" | Format-Table
        # 检查配置文件
        Get-ChildItem -Path . -Filter "appsettings*.json" | Format-Table
    }
    
    {$_ -ge 2000 -and $_ -le 2999} {
        Write-Host "编译级错误检查..." -ForegroundColor Yellow
        # 检查项目文件
        dotnet restore
        dotnet build --verbosity quiet
    }
    
    {$_ -ge 3000 -and $_ -le 3999} {
        Write-Host "运行时错误检查..." -ForegroundColor Yellow
        # 检查应用日志
        if (Test-Path "logs") {
            Get-ChildItem -Path "logs" -Filter "*.log" | ForEach-Object {
                Write-Host "检查日志文件: $($_.Name)"
                if ($ShowLogs) {
                    Get-Content $_.FullName | Select-String "ERROR|FATAL" | Select-Object -Last 5
                }
            }
        }
    }
}

Write-Host "诊断完成。查看详细解决方案请参考 CPM-错误代码参考.md" -ForegroundColor Green
```

## 相关文档

- [CPM-故障排除指南.md](CPM-故障排除指南.md) - 系统性故障诊断流程
- [CPM-常见问题FAQ.md](CPM-常见问题FAQ.md) - 常见问题快速解答
- [CPM-应急响应预案.md](CPM-应急响应预案.md) - 紧急情况处理流程

## 更新记录

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0.0 | 2025-01-31 | 初始版本，定义错误代码体系和详细说明 | Claude |

---

**维护说明**:
1. 新发现的错误代码请按分类添加到相应范围
2. 每个错误代码都应该包含完整的解决方案
3. 定期回顾错误发生频率，优化系统设计
4. 保持错误代码的唯一性和连续性