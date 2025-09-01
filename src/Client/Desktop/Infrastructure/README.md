# LYBT.Desktop.Infrastructure

## 概述

LYBT.Desktop.Infrastructure是凌隐宝堂桌面客户端的基础设施层，提供HTTP通信、API客户端、主题资源等底层技术支持。该模块专注于与外部系统的集成和基础技术服务，为上层业务模块提供统一的技术底座。

## 核心功能

### 🌐 HTTP通信基础设施
- **Refit 8.0.0**: 类型安全的REST API客户端生成
- **HttpClientFactory**: HTTP客户端统一管理和连接池优化
- **Polly集成**: HTTP请求重试、熔断、超时策略
- **JSON序列化**: 统一的System.Text.Json配置和转换器

### 🎨 主题和样式系统
- **统一样式**: 全局WPF样式和控件模板
- **主题资源**: 支持亮色/暗色主题切换的资源字典
- **行为扩展**: Microsoft.Xaml.Behaviors.Wpf行为支持

### 📄 文件处理
- **NPOI 2.7.4**: Excel文件读写和数据导入导出
- **文档生成**: 处方单据和报表文档生成

## 项目结构

```
src/Client/Desktop/Infrastructure/
├── RefitConfiguration.cs       # Refit配置和序列化设置
├── HttpClientFactory.cs        # HTTP客户端工厂
├── Http/                      # HTTP相关扩展
└── Themes/                    # 主题和样式资源
    └── Styles.xaml           # 全局样式定义
```

## 技术栈

### 核心依赖
- **.NET 8.0**: 目标框架
- **WPF**: Windows Presentation Foundation
- **Refit 8.0.0**: REST API客户端代码生成
- **Microsoft.Xaml.Behaviors.Wpf 1.1.135**: WPF行为扩展

### HTTP通信
- **Refit.Newtonsoft.Json 8.0.0**: JSON序列化支持
- **Microsoft.Extensions.Http.Polly 8.0.0**: HTTP弹性策略
- **Polly.Extensions.Http 3.0.0**: HTTP重试和熔断

### 文档处理
- **NPOI 2.7.4**: Excel文件操作库

## 核心特性

### 🚀 类型安全API客户端

#### Refit配置
```csharp
public static RefitSettings GetRefitSettings()
{
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    options.Converters.Add(new JsonStringEnumConverter());

    return new RefitSettings
    {
        ContentSerializer = new SystemTextJsonContentSerializer(options)
    };
}
```

#### API接口定义示例
```csharp
public interface IPatientApi
{
    [Get("/api/v1/patients")]
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
        [Query] int page, 
        [Query] int pageSize,
        [Query] string? keyword = null);

    [Post("/api/v1/patients")]
    Task<ApiResponse<PatientDto>> CreatePatientAsync([Body] PatientCreateDto dto);
}
```

### 🔄 弹性HTTP策略

#### 重试策略
- **指数退避**: 2^n秒间隔重试，最多3次
- **瞬态故障处理**: 网络超时、服务不可用自动重试
- **熔断器**: 连续失败时暂停请求，保护后端服务

#### 连接管理
- **连接池**: 复用HTTP连接，减少握手开销
- **超时控制**: 请求级别和全局超时设置
- **并发限制**: 防止过多并发请求影响性能

### 🎨 主题系统

#### 样式架构
- **ResourceDictionary**: 分模块的样式资源组织
- **动态主题**: 运行时主题切换不需要重启
- **继承层次**: 基础样式→模块样式→控件样式

#### 主题资源
```xaml
<!-- 主题色彩定义 -->
<ResourceDictionary>
    <SolidColorBrush x:Key="PrimaryBrush" Color="#007ACC"/>
    <SolidColorBrush x:Key="SecondaryBrush" Color="#F0F0F0"/>
    
    <!-- 控件样式 -->
    <Style TargetType="Button" BasedOn="{StaticResource BaseButtonStyle}">
        <Setter Property="Background" Value="{StaticResource PrimaryBrush}"/>
    </Style>
</ResourceDictionary>
```

## 配置和使用

### 依赖注入配置

```csharp
// 在App.xaml.cs或启动配置中
services.AddRefitClient<IPatientApi>(RefitConfiguration.GetRefitSettings())
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(Configuration["ApiBaseUrl"]))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

### HTTP策略配置

```csharp
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        );
}
```

### 主题应用

```xml
<!-- 在App.xaml中合并主题资源 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Infrastructure;component/Themes/Styles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## 开发规范

### API客户端开发
- 所有API接口定义在Shared.Interfaces项目中
- 使用Refit属性标记HTTP方法和参数
- 返回类型统一使用`ApiResponse<T>`包装
- 异步方法以Async后缀命名

### 错误处理
- HTTP错误自动重试，业务错误不重试
- 网络异常统一转换为用户友好消息
- 记录详细的请求/响应日志供调试

### 性能优化
- 复用HttpClient实例，避免重复创建
- 大文件上传使用流式传输
- 实现请求取消令牌支持长时间操作取消

## 依赖关系

### 直接依赖
- **LYBT.Desktop.Core**: 核心框架和配置
- **Microsoft外部包**: HTTP、序列化、WPF行为扩展
- **第三方库**: Refit、NPOI、Polly

### 被依赖模块
- **所有业务模块**: Auth、Users、Patients等
- **LYBT.Desktop.Services**: 业务服务层
- **工作台模块**: 各个角色工作台

## 维护说明

作为基础设施层，该项目的稳定性至关重要：

### 版本升级
- **谨慎升级**: Refit、Polly等核心依赖升级需充分测试
- **兼容性检查**: 确保API接口定义向后兼容
- **性能基准**: 升级前后进行性能对比测试

### 配置变更
- **默认配置**: 提供合理的默认HTTP超时和重试策略
- **环境差异**: Development/Production环境使用不同的配置
- **监控指标**: 关键HTTP指标的日志记录和监控

### 故障排查
- **网络诊断**: 提供HTTP请求/响应的详细日志
- **连接问题**: 监控连接池状态和资源使用
- **API变更**: 后端API变更时的兼容性处理

---

*该文档反映当前代码实现状态，与实际功能保持100%同步 - UltraThink文档驱动开发标准*