# LYBT.Desktop.Infrastructure v2.1 - WPF基础设施服务库

## 🎯 项目概述

LYBT.Desktop.Infrastructure是凌隐宝堂中医诊所系统WPF客户端的核心基础设施库，提供完整的HTTP通信、API客户端生成、弹性处理和数据访问抽象。基于Refit构建类型安全的REST客户端，集成Polly弹性处理策略，为所有业务模块提供统一的通信底座。

**核心价值**:
- 🔗 **类型安全**: Refit生成的强类型API客户端，实现编译时API调用检查。
- ⚡ **弹性通信**: 集成Polly，提供HTTP请求的自动重试、熔断和超时策略。
- 🏗️ **架构统一**: 为所有前端模块提供统一的HTTP通信层和错误处理机制。
- 📄 **文件处理**: 集成NPOI，提供Excel文件的导入导出能力。

## 📦 项目结构

```
src/Client/Desktop/Infrastructure/
├── ApiClients/                 # API客户端相关
│   ├── RefitConfiguration.cs      # Refit配置和序列化设置
│   └── ApiResponseHandler.cs      # API响应统一处理
├── Policies/                   # Polly弹性策略
│   ├── RetryPolicies.cs           # 重试策略定义
│   └── CircuitBreakerPolicies.cs  # 熔断策略定义
├── Extensions/                 # 依赖注入容器扩展方法
│   └── ServiceCollectionExtensions.cs
└── Converters/                 # 数据转换器 (例如JSON转换器)
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Refit**: 类型安全的REST API客户端生成器。
- **Polly**: 提供网络通信的重试、熔断等弹性策略。
- **Microsoft.Extensions.Http**: 用于`HttpClientFactory`的管理和集成Polly策略。
- **NPOI**: 用于读写Excel文件。

## 🚀 快速开始

此项目是一个类库，不包含可执行文件。可以通过解决方案或以下命令进行构建：

```bash
# 还原解决方案依赖
dotnet restore LYBT.All.sln

# 构建此项目
dotnet build src\Client\Desktop\Infrastructure\LYBT.Desktop.Infrastructure.csproj
```

## 🔌 API 接口

此项目为桌面端基础设施层，不直接对外提供API接口。它的核心职责是**实现和管理对后端API的调用**。

所有API接口的定义（`IUserApi`, `IPatientApi`等）位于 `LYBT.Shared.Interfaces` 项目中，此项目通过Refit为这些接口生成具体的HTTP客户端实现。

### 依赖注入集成

通过 `ServiceCollectionExtensions.cs` 中的扩展方法，将所有API客户端及Polly策略注册到DI容器中。

```csharp
// 在Shell项目的App.xaml.cs中调用
public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    var apiBaseUrl = configuration.GetConnectionString("ApiBaseUrl");
    
    // 注册所有API客户端，并附加重试和熔断策略
    services.AddRefitClient<IAuthApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
        .AddPolicyHandler(RetryPolicies.GetRetryPolicy())
        .AddPolicyHandler(CircuitBreakerPolicies.GetCircuitBreakerPolicy());
        
    services.AddRefitClient<IPatientApi>()
        // ...
    
    return services;
}
```

### 在ViewModel中使用

```csharp
public class PatientListViewModel : CoreViewModel
{
    private readonly IPatientApi _patientApi; // 直接注入Refit生成的客户端

    public PatientListViewModel(IPatientApi patientApi)
    {
        _patientApi = patientApi;
    }

    private async Task LoadPatientsAsync()
    {
        // 所有网络异常和重试都由基础设施层自动处理
        var response = await _patientApi.GetPatientsAsync();
        if (response.Success)
        {
            // ...
        }
    }
}
```