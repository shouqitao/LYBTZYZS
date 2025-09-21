# LYBT.Desktop.基础设施（基础设施（Infrastructure）） v2.1 - WPF基础设施服务库

> **API客户端管理** | HTTP通信层 | 数据访问抽象 | Refit类型安全集成 
> 项目状态: ✅ **生产就绪** | 🎆 **2025-09-02重构完成** | **通信标准**

## 🎯 项目概述

LYBT.Desktop.Infrastructure是凌隐宝堂中医诊所系统WPF客户端的核心基础设施库，提供完整的HTTP通信、API客户端生成、弹性处理和数据访问抽象。基于Refit 8.0.0构建类型安全的REST客户端，集成Polly弹性处理策略，为8个业务模块和7个工作台提供统一的通信底座。

**核心价值**:
- 🔗 **类型安全**: Refit生成的强类型API客户端，编译时检查
- ⚡ **弹性通信**: Polly集成的重试、熔断、超时策略
- 🏗️ **架构统一**: 为所有前端模块提供统一的HTTP通信基础
- 📱 **高可用性**: 网络异常自动恢复，用户体验平滑

## 核心功能

### 🌐 HTTP通信基础设施
- **Refit 8.0.0**: 类型安全的REST API客户端生成，零手写HTTP代码
- **HttpClientFactory**: HTTP客户端统一管理和连接池优化
- **Polly弹性策略**: HTTP请求重试、熔断、超时的处理
- **JSON序列化**: 统一的Newtonsoft.Json配置和类型转换器

### ⚙️ API客户端管理
- **自动生成**: 基于接口定义自动生成API客户端实现
- **类型安全**: 编译时验证API调用的参数和返回类型
- **统一响应**: 所有API返回统一的`ApiResponse<T>`格式
- **错误处理**: 自动处理HTTP错误和业务异常转换

### 🔄 弹性和可靠性
- **指数退避重试**: 瞬态故障自动重试，避免服务压力
- **熔断器模式**: 连续失败时自动熔断，保护后端服务
- **超时控制**: 防止长时间阻塞的请求超时机制
- **连接池管理**: 优化连接复用，提升网络性能

### 🎨 WPF扩展支持
- **行为扩展**: Microsoft.Xaml.Behaviors.Wpf行为库集成
- **WPF兼容**: 为WPF应用提供必要的框架支持
- **UI资源**: 支持WPF UI控件和样式系统

### 📄 文件处理能力
- **NPOI 2.7.4**: Excel文件读写和数据导入导出
- **数据转换**: 患者、药材、验方数据的Excel格式支持
- **文档生成**: 处方单据和诊疗报表的格式化输出

## 📦 项目结构

```
src/Client/Desktop/Infrastructure/
├── ApiClients/                 # API客户端相关
│   ├── RefitConfiguration.cs      # Refit配置和序列化设置
│   ├── HttpClientExtensions.cs    # HTTP客户端扩展方法
│   └── ApiResponseHandler.cs      # API响应统一处理
├── Policies/                   # Polly弹性策略
│   ├── RetryPolicies.cs           # 重试策略定义
│   ├── CircuitBreakerPolicies.cs  # 熔断策略定义
│   └── TimeoutPolicies.cs         # 超时策略定义
├── Extensions/                 # 扩展方法
│   ├── ServiceCollectionExtensions.cs # DI容器扩展
│   └── HttpExtensions.cs           # HTTP相关扩展
├── Converters/                # 数据转换器
│   └── JsonConverters.cs          # JSON转换器集合
└── Resources/                 # 资源文件
    └── DefaultSettings.json      # 默认配置设置
```

## 🛠 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架，支持C# 12最新特性
- **WPF**: Windows Presentation Foundation UI框架
- **Microsoft.Xaml.Behaviors.Wpf 1.1.135**: WPF行为扩展库
- **NPOI 2.7.4**: Excel文件处理，支持导入导出功能

### HTTP和API通信
- **Refit 8.0.0**: 类型安全REST客户端生成器
- **Refit.Newtonsoft.Json 8.0.0**: Refit的JSON序列化支持
- **Microsoft.Extensions.Http 8.0.0**: HTTP客户端工厂和管理
- **Microsoft.Extensions.Http.Polly 8.0.0**: HTTP弹性策略集成
- **Polly.Extensions.Http 3.0.0**: HTTP专用的弹性处理模式

## 核心特性

### 🚀 类型安全API客户端

基于Refit的强类型REST客户端生成，提供编译时类型检查和零手写HTTP代码的开发体验。

#### Refit配置标准
```csharp
/// <summary>
/// 获取统一的Refit配置 - 企业级JSON序列化设置
/// </summary>
/// <returns>标准化的Refit设置</returns>
public static RefitSettings GetRefitSettings()
{
    return new RefitSettings
    {
        ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Converters = { new StringEnumConverter() }
        })
    };
}
```

#### API接口标准化示例
```csharp
/// <summary>
/// 患者管理API客户端 - 提供完整的患者信息CRUD操作
/// </summary>
[Description("患者档案管理API - 支持分页查询、创建、更新、删除操作")]
public interface IPatientApi
{
    /// <summary>分页获取患者列表</summary>
    /// <param name="page">页码(从1开始)</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="keyword">搜索关键词(姓名/手机号)</param>
    /// <returns>分页患者数据</returns>
    [Get("/api/v1/patients")]
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
        [Query] int page = 1, 
        [Query] int pageSize = 20,
        [Query] string? keyword = null);

    /// <summary>创建新患者档案</summary>
    /// <param name="dto">患者创建数据</param>
    /// <returns>创建的患者信息</returns>
    [Post("/api/v1/patients")]
    Task<ApiResponse<PatientDto>> CreatePatientAsync([Body] PatientCreateDto dto);
    
    /// <summary>根据ID获取患者详情</summary>
    /// <param name="id">患者ID</param>
    /// <returns>患者详细信息</returns>
    [Get("/api/v1/patients/{id}")]
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);
}
```

### 🔄 弹性策略

集成Polly库提供完整的弹性处理能力，确保网络通信的高可用性和用户体验的平滑性。

#### 重试策略配置
```csharp
/// <summary>
/// HTTP请求重试策略 - 指数退避算法
/// </summary>
/// <returns>重试策略实例</returns>
public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 处理HttpRequestException和5XX/408状态码
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2^n秒指数退避
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Logger.LogWarning("重试第{RetryCount}次，延迟{Delay}秒", retryCount, timespan.TotalSeconds);
            });
}
```

#### 熔断器策略
```csharp
/// <summary>
/// 熔断器策略 - 保护后端服务避免过载
/// </summary>
public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5, // 连续5次失败后熔断
            durationOfBreak: TimeSpan.FromMinutes(1), // 熔断1分钟
            onBreak: (exception, duration) =>
            {
                Logger.LogError("熔断器开启，持续时间: {Duration}秒", duration.TotalSeconds);
            },
            onReset: () => Logger.LogInformation("熔断器重置，服务恢复"));
}
```

#### 超时策略
- **请求超时**: 单个请求30秒超时
- **全局超时**: 应用级别2分钟超时
- **取消令牌**: 支持用户主动取消长时间操作

#### 连接池优化
- **连接复用**: HttpClientFactory管理连接生命周期
- **DNS刷新**: 定期刷新DNS解析，适应网络变化
- **并发控制**: 限制同时进行的HTTP请求数量

### ⚙️ 依赖注入（DI）集成

提供完整的服务注册和配置管理，简化API客户端的使用。

#### 服务注册模式
```csharp
/// <summary>
/// 注册Infrastructure服务到DI容器
/// </summary>
/// <param name="services">服务容器</param>
/// <param name="configuration">配置对象</param>
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    var apiBaseUrl = configuration.GetConnectionString("ApiBaseUrl") 
                     ?? "https://localhost:7001";
    
    // 注册所有API客户端
    services.AddRefitClient<IAuthApi>(RefitConfiguration.GetRefitSettings())
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
        
    services.AddRefitClient<IPatientApi>(RefitConfiguration.GetRefitSettings())
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    
    // 注册其他基础设施服务
    services.AddSingleton<IApiResponseHandler, ApiResponseHandler>();
    services.AddScoped<IExcelExportService, ExcelExportService>();
    
    return services;
}
```

## 配置和使用

### 应用程序集成

#### App.xaml.cs中的配置
```csharp
/// <summary>
/// WPF应用程序启动配置 - 集成Infrastructure服务
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();
        
        // 添加Infrastructure服务
        services.AddInfrastructureServices(configuration);
        
        // 注册其他应用服务
        services.AddCoreServices(configuration);
        services.AddBusinessServices();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // 启动主窗口
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }
    
    private IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
```

#### 配置文件设置 (appsettings.json)
```json
{
  "ConnectionStrings": {
    "ApiBaseUrl": "https://localhost:7001"
  },
  "HttpSettings": {
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationMinutes": 1
  },
  "Excel": {
    "ExportPath": "C:\\LYBT\\Exports",
    "MaxRowsPerSheet": 65000,
    "DefaultFileName": "LYBT_Export_{0:yyyyMMdd_HHmmss}.xlsx"
  }
}
```

### API客户端使用示例

#### 在ViewModel中使用API客户端
```csharp
/// <summary>
/// 患者管理ViewModel - 展示API客户端的典型使用方式
/// </summary>
public class PatientListViewModel : CoreViewModel
{
    private readonly IPatientApi _patientApi;
    private readonly INotificationService _notificationService;
    
    public PatientListViewModel(
        IPatientApi patientApi,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<PatientListViewModel> logger)
        : base(mapper, logger)
    {
        _patientApi = patientApi;
        _notificationService = notificationService;
        
        LoadPatientsCommand = new AsyncRelayCommand(LoadPatientsAsync);
        CreatePatientCommand = new AsyncRelayCommand<PatientCreateDto>(CreatePatientAsync);
    }
    
    /// <summary>
    /// 加载患者列表 - 展示分页查询和错误处理
    /// </summary>
    private async Task LoadPatientsAsync()
    {
        try
        {
            IsLoading = true;
            
            var response = await _patientApi.GetPatientsAsync(
                page: CurrentPage,
                pageSize: PageSize,
                keyword: SearchKeyword);
            
            if (response.Success && response.Data != null)
            {
                Patients = new ObservableCollection<PatientDto>(response.Data.Items);
                TotalCount = response.Data.TotalCount;
                
                Logger.LogInformation("成功加载{Count}个患者", response.Data.Items.Count);
            }
            else
            {
                await _notificationService.ShowErrorAsync("加载患者列表失败", response.Message);
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "网络请求异常");
            await _notificationService.ShowErrorAsync("网络连接异常", "请检查网络连接或稍后重试");
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogWarning(ex, "请求超时");
            await _notificationService.ShowWarningAsync("请求超时", "网络响应较慢，请稍后重试");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// 创建患者 - 展示POST请求和业务异常处理
    /// </summary>
    private async Task CreatePatientAsync(PatientCreateDto dto)
    {
        if (dto == null) return;
        
        try
        {
            IsLoading = true;
            
            var response = await _patientApi.CreatePatientAsync(dto);
            
            if (response.Success && response.Data != null)
            {
                Patients.Insert(0, response.Data);
                await _notificationService.ShowSuccessAsync("患者创建成功", $"已成功创建患者档案：{response.Data.Name}");
                
                Logger.LogInformation("成功创建患者：{PatientName} (ID: {PatientId})", 
                    response.Data.Name, response.Data.Id);
            }
            else
            {
                await _notificationService.ShowErrorAsync("创建患者失败", response.Message);
            }
        }
        catch (ValidationException ex)
        {
            Logger.LogWarning(ex, "数据验证失败");
            await _notificationService.ShowWarningAsync("数据验证失败", ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "创建患者时发生未知错误");
            await _notificationService.ShowErrorAsync("系统错误", "创建患者时发生未知错误，请稍后重试");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

## 开发规范

### API客户端开发标准
- **接口定义**: 所有API接口定义在LYBT.Shared.Interfaces项目中
- **命名规范**: API接口以`I{Module}Api`格式命名，如`IPatientApi`
- **HTTP方法**: 使用Refit属性标记HTTP方法 (`[Get]`, `[Post]`, `[Put]`, `[Delete]`)
- **参数标记**: 查询参数用`[Query]`，请求体用`[Body]`，路径参数直接在URL中
- **返回类型**: 统一使用`Task<ApiResponse<T>>`异步返回格式
- **异步命名**: 所有异步方法以`Async`后缀命名

### 错误处理最佳实践
```csharp
// ✅ 正确的错误处理模式
try
{
    var response = await _api.GetDataAsync();
    if (response.Success)
    {
        // 处理成功响应
        return response.Data;
    }
    else
    {
        // 处理业务错误
        Logger.LogWarning("API业务错误: {Message}", response.Message);
        await _notificationService.ShowWarningAsync("操作失败", response.Message);
    }
}
catch (HttpRequestException ex)
{
    // 网络异常处理
    Logger.LogError(ex, "网络请求异常");
    await _notificationService.ShowErrorAsync("网络异常", "请检查网络连接");
}
catch (TaskCanceledException ex)
{
    // 超时异常处理
    Logger.LogWarning(ex, "请求超时");
    await _notificationService.ShowWarningAsync("请求超时", "请稍后重试");
}
```

### 性能优化指导
- **HTTP客户端复用**: 使用HttpClientFactory管理客户端生命周期
- **取消令牌**: 长时间操作支持CancellationToken取消
- **流式传输**: 大文件上传/下载使用Stream而不是byte[]
- **缓存策略**: 适当缓存不经常变化的数据
- **批量操作**: 合并多个小请求为批量请求

### Excel文件处理规范
```csharp
/// <summary>
/// Excel导出服务使用示例
/// </summary>
public async Task ExportPatientsToExcelAsync(List<PatientDto> patients)
{
    var workbook = new XSSFWorkbook();
    var sheet = workbook.CreateSheet("患者列表");
    
    // 创建表头
    var headerRow = sheet.CreateRow(0);
    headerRow.CreateCell(0).SetCellValue("患者ID");
    headerRow.CreateCell(1).SetCellValue("姓名");
    headerRow.CreateCell(2).SetCellValue("性别");
    headerRow.CreateCell(3).SetCellValue("年龄");
    
    // 填充数据行
    for (int i = 0; i < patients.Count; i++)
    {
        var row = sheet.CreateRow(i + 1);
        var patient = patients[i];
        
        row.CreateCell(0).SetCellValue(patient.Id.ToString());
        row.CreateCell(1).SetCellValue(patient.Name);
        row.CreateCell(2).SetCellValue(patient.Gender);
        row.CreateCell(3).SetCellValue(patient.Age);
    }
    
    // 自动调整列宽
    for (int i = 0; i < 4; i++)
    {
        sheet.AutoSizeColumn(i);
    }
    
    // 保存文件
    var fileName = $"患者列表_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
    var filePath = Path.Combine(_configuration["Excel:ExportPath"], fileName);
    
    using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
    workbook.Write(fileStream);
    
    Logger.LogInformation("成功导出{Count}个患者到Excel文件: {FilePath}", patients.Count, filePath);
}
```

## 🎆 重构成果总览 (2025-09-02)

### 🏆 基础设施标准升级完成

**技术栈现代化**:
- ✅ **C# 12支持**: 最新语言特性和性能优化
- ✅ **Refit 8.0.0**: 类型安全API客户端生成
- ✅ **Polly集成**: 弹性处理策略
- ✅ **依赖组织**: 按功能分组的包引用管理

**架构优化成果**:
- ✅ **统一通信层**: 为8个业务模块提供统一HTTP基础
- ✅ **弹性策略**: 重试、熔断、超时的完整处理
- ✅ **类型安全**: 编译时API调用检查，运行时稳定
- ✅ **配置管理**: 完整的配置系统和环境区分

### 📊 技术指标

**性能优化**:
- **连接复用**: HttpClientFactory管理连接池，减少资源消耗
- **智能重试**: 指数退避算法，避免服务压力
- **请求限流**: 防止过多并发请求影响系统稳定性

**可靠性保障**:
- **熔断保护**: 连续失败时自动熔断，保护后端服务
- **超时控制**: 多层级超时机制，防止长时间阻塞
- **异常处理**: 完整的异常分类和用户友好消息

## 依赖关系

### 项目引用
- **LYBT.Desktop.Core**: 核心框架和基础配置 → 提供MVVM基础和服务接口
- 依赖关系: 基础设施（基础设施（Infrastructure）） → Core (单向依赖)

### NuGet包依赖
**WPF框架**:
- **Microsoft.Xaml.Behaviors.Wpf 1.1.135**: WPF行为扩展支持
- **NPOI 2.7.4**: Excel文件处理能力

**HTTP通信**:
- **Refit 8.0.0 + Refit.Newtonsoft.Json 8.0.0**: 类型安全REST客户端
- **Microsoft.Extensions.Http 8.0.0**: HTTP客户端工厂管理
- **Microsoft.Extensions.Http.Polly 8.0.0**: HTTP弹性策略集成
- **Polly.Extensions.Http 3.0.0**: HTTP专用弹性处理

### 被依赖模块
**业务模块** (8个):
- LYBT.Desktop.Auth、LYBT.Desktop.Users、LYBT.Desktop.Patients
- LYBT.Desktop.MedicalCase、LYBT.Desktop.Consultation
- LYBT.Desktop.Prescriptions、LYBT.Desktop.Herbs、LYBT.Desktop.Formula

**系统模块**:
- LYBT.Desktop.Services: 业务服务层
- 7个Workbenches: 各角色专用工作台
- LYBT.Desktop.Shell: 应用程序外壳

## 维护指南

### 版本升级策略
- **谨慎升级**: Refit、Polly等核心依赖升级需充分测试所有API客户端
- **兼容性检查**: 确保API接口定义向后兼容，不破坏现有业务模块
- **性能基准**: 升级前后进行网络性能和内存使用对比测试
- **回滚方案**: 准备快速回滚机制，确保生产环境稳定性

### 配置管理
- **环境配置**: Development/Staging/Production使用不同的API基址和超时设置
- **默认策略**: 提供合理的重试次数(3次)和熔断阈值(5次失败)
- **监控集成**: 关键HTTP指标的详细日志和性能监控

### 故障诊断
- **网络诊断**: HTTP请求/响应的完整日志记录
- **连接监控**: 连接池状态和资源使用情况跟踪
- **API兼容**: 后端API版本升级时的兼容性处理方案

### 开发团队指导
- **新API集成**: 添加新业务模块时的API客户端集成步骤
- **错误处理**: 统一的异常处理模式和用户提示规范
- **测试策略**: API客户端的单元测试和集成测试指南

## 相关文档

- [LYBT.Desktop.Core](../Core/README.md) - WPF核心基础设施库
- [LYBT.Shared.Interfaces](../../../Shared/LYBT.Shared.Interfaces/README.md) - API接口定义
- [接口设计标准](../../../../docs/api/interface-design-standards.md) - API接口设计规范
- [前后端契约规范](../../../../docs/前后端契约规范.md) - 前后端协作标准

---

**LYBT.Desktop.基础设施（基础设施（Infrastructure）） v2.1** - WPF基础设施服务库，为中医诊所系统提供HTTP通信基础 ✨

> 项目状态: ✅ **生产就绪** | **最后更新**: 2025-09-02 | **版本**: v2.1.0-infrastructure-enterprise