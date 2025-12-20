# Design: 统一日志系统项目

## 架构设计

### 1. 依赖关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application Layer                         │
├─────────────────────────────┬───────────────────────────────────┤
│   LYBT.WebAPI               │   LYBT.Desktop.Shell              │
│   ├─ Serilog.AspNetCore     │   └─ (Serilog via Infrastructure)│
│   └─ Serilog.Sinks.MSSqlServer                                  │
└─────────────┬───────────────┴───────────────┬───────────────────┘
              │                               │
              ▼                               ▼
┌─────────────────────────────┐ ┌─────────────────────────────────┐
│   LYBT.Infrastructure       │ │   LYBT.Desktop.Infrastructure   │
│   └─ LYBT.Shared.Logging    │ │   └─ LYBT.Shared.Logging        │
└─────────────┬───────────────┘ └─────────────┬───────────────────┘
              │                               │
              └───────────────┬───────────────┘
                              ▼
              ┌───────────────────────────────┐
              │     LYBT.Shared.Logging       │
              │   ├─ Serilog                  │
              │   ├─ Serilog.Extensions.*     │
              │   ├─ Serilog.Sinks.File       │
              │   ├─ Serilog.Sinks.Console    │
              │   └─ Serilog.Enrichers.*      │
              └───────────────┬───────────────┘
                              │
                              ▼
              ┌───────────────────────────────┐
              │    LYBT.Shared.Primitives     │
              │   └─ SensitiveDataAttribute   │
              └───────────────────────────────┘
```

### 2. 核心接口设计

#### ICorrelationIdProvider
解耦HttpContext依赖，支持Server和Desktop不同实现：

```csharp
namespace LYBT.Shared.Logging.Abstractions;

/// <summary>
/// CorrelationId提供者接口
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// 获取当前CorrelationId
    /// </summary>
    string? GetCorrelationId();
    
    /// <summary>
    /// 设置当前CorrelationId
    /// </summary>
    void SetCorrelationId(string correlationId);
}
```

#### Server实现 (依赖HttpContextAccessor)
```csharp
public class HttpContextCorrelationIdProvider : ICorrelationIdProvider
{
    private readonly IHttpContextAccessor _accessor;
    public string? GetCorrelationId() => 
        _accessor.HttpContext?.Items["CorrelationId"] as string;
}
```

#### Desktop实现 (使用AsyncLocal)
```csharp
public class AsyncLocalCorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string?> _correlationId = new();
    public string? GetCorrelationId() => _correlationId.Value;
    public void SetCorrelationId(string id) => _correlationId.Value = id;
}
```

### 3. 日志配置层次

```
LoggingConfigurationBase (抽象基类)
├── 通用Enrichers配置
├── 通用格式化模板
├── 敏感数据脱敏策略
│
├── ServerLoggingConfiguration (Server端扩展)
│   ├── MSSqlServer Sink配置
│   ├── Request/Response日志
│   └── 健康检查过滤
│
└── DesktopLoggingConfiguration (Desktop端扩展)
    ├── 文件路径配置(%LOCALAPPDATA%)
    ├── 30天保留策略
    └── Rolling策略配置
```

### 4. 敏感数据脱敏架构

```
SensitiveDataMasker (静态工具类)
├── Mask(value, mode, dataType)     # 核心脱敏方法
├── MaskPartial()                   # 部分隐藏
├── MaskHash()                      # 哈希脱敏
├── SanitizeText()                  # 文本级脱敏
└── IsSensitiveFieldName()          # 字段名检测

SensitiveDataDestructuringPolicy (Serilog策略)
└── TryDestructure()                # 自动解构脱敏
```

### 5. 日志级别动态控制

```
LoggingLevelManager
├── LevelSwitch: LoggingLevelSwitch
├── EnableDebugMode(level, duration)
├── DisableDebugMode()
├── GetStatus() -> DebugModeInfo
└── 自动过期Timer
```

## 文件迁移计划

| 源文件 | 目标位置 | 变更说明 |
|--------|----------|----------|
| Server/Logging/SensitiveDataMasker.cs | Shared.Logging/Masking/ | 移除Entities引用,使用Primitives |
| Server/Logging/SensitiveDataDestructuringPolicy.cs | Shared.Logging/Masking/ | 同上 |
| Server/Logging/LoggingLevelManager.cs | Shared.Logging/Management/ | 无变更 |
| Server/Logging/LogCleanupService.cs | Shared.Logging/Management/ | 抽取接口,解耦AppDbContext |
| Server/Logging/CorrelationIdEnricher.cs | Shared.Logging/Enrichers/ | 使用ICorrelationIdProvider |
| Server/Logging/SerilogExtensions.cs | Shared.Logging/Extensions/ | 扩展方法合并 |
| Desktop/Logging/DesktopSerilogConfiguration.cs | Shared.Logging/Configuration/ | 重构为配置类 |
| Desktop/Logging/CorrelationIdEnricher.cs | 删除 | 使用统一Enricher |

## csproj设计

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>LYBT.Shared.Logging</RootNamespace>
  </PropertyGroup>

  <!-- Serilog核心 -->
  <ItemGroup>
    <PackageReference Include="Serilog" />
    <PackageReference Include="Serilog.Extensions.Logging" />
    <PackageReference Include="Serilog.Sinks.File" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Enrichers.Environment" />
    <PackageReference Include="Serilog.Enrichers.Thread" />
  </ItemGroup>

  <!-- Microsoft扩展 -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>

  <!-- 项目引用 -->
  <ItemGroup>
    <ProjectReference Include="..\LYBT.Shared.Primitives\LYBT.Shared.Primitives.csproj" />
  </ItemGroup>
</Project>
```

## 扩展点设计

### 1. Server端特有Sink (保留在WebAPI)
```csharp
// LYBT.WebAPI/Program.cs
Log.Logger = new LoggerConfiguration()
    .UseSharedLogging(correlationIdProvider)  // 使用共享配置
    .WriteTo.MSSqlServer(...)                  // Server特有Sink
    .CreateLogger();
```

### 2. Desktop端特有配置
```csharp
// App.xaml.cs
Log.Logger = new LoggerConfiguration()
    .UseSharedLogging(correlationIdProvider)
    .UseDesktopDefaults()                      // Desktop默认配置
    .CreateLogger();
```

## 测试策略

1. **单元测试**: 迁移现有LoggingTests,添加新组件测试
2. **集成测试**: 验证Server和Desktop日志配置正确工作
3. **回归测试**: 确保现有日志行为不变
