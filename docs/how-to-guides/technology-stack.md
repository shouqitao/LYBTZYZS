# 技术栈详细说明

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-16  
**维护团队**：开发组  

## 🎯 技术栈总览

凌隐宝堂中医诊所管理系统采用现代化的.NET技术栈，基于.NET 8.0 LTS版本构建，确保系统的稳定性、性能和长期支持。

### 📊 技术选择原则

1. **稳定性优先** - 选择LTS版本和成熟技术
2. **性能考虑** - 选用高性能的框架和库
3. **生态完整** - 确保有丰富的文档和社区支持
4. **安全可靠** - 重视安全特性和最佳实践

## 🏗️ Server端技术栈

### 核心框架
| 技术 | 版本 | 用途 | 选择理由 |
|------|------|------|----------|
| **.NET** | 8.0.406 | 运行时环境 | 最新的LTS版本，性能优异，长期支持 |
| **ASP.NET Core** | 8.0.20 | Web框架 | 跨平台、高性能、模块化设计 |
| **Entity Framework Core** | 8.0.20 | ORM框架 | 成熟稳定、性能优秀、支持多种数据库 |

### 数据库技术
| 技术 | 版本 | 用途 | 配置说明 |
|------|------|------|----------|
| **SQL Server** | 2019+ | 主数据库 | 企业级关系型数据库，高可靠性 |
| **Dapper** | 2.1.35 | 微ORM | 高性能数据访问，补充EF Core |
| **连接池** | 默认配置 | 数据库连接管理 | 优化连接复用，提高性能 |

### API和文档
| 技术 | 版本 | 用途 | 特性说明 |
|------|------|------|----------|
| **Swagger/OpenAPI** | 7.2.0 | API文档 | 自动生成API文档，支持交互测试 |
| **API版本控制** | 1.0 | 版本管理 | URL路径版本控制，向后兼容 |
| **JSON序列化** | System.Text.Json 8.0 | JSON处理 | 高性能、内置支持 |

### 认证和安全
| 技术 | 版本 | 用途 | 安全特性 |
|------|------|------|----------|
| **JWT Bearer** | 8.0.20 | 身份认证 | 无状态、跨域支持 |
| **ASP.NET Core Identity** | 8.0.20 | 用户管理 | 密码哈希、账户锁定、双因子认证 |
| **数据保护** | 8.0.20 | 数据加密 | 敏感数据保护、密钥管理 |

### 测试框架
| 技术 | 版本 | 用途 | 测试类型 |
|------|------|------|----------|
| **NUnit** | 3.13.3 | 单元测试 | 成熟稳定、功能完整 |
| **Moq** | 4.20.69 | 模拟框架 | Mock对象创建、行为验证 |
| **FluentAssertions** | 6.12.0 | 断言库 | 流畅的断言语法、可读性强 |

### 日志和监控
| 技术 | 版本 | 用途 | 特性说明 |
|------|------|------|----------|
| **Serilog** | 3.1.1 | 结构化日志 | 多目标输出、格式化支持 |
| **Application Insights** | 2.22.0 | 性能监控 | APM功能、异常追踪 |
| **Health Checks** | 8.0.20 | 健康检查 | 系统状态监控、依赖检查 |

## 🖥️ Client端技术栈

### 核心框架
| 技术 | 版本 | 用途 | 选择理由 |
|------|------|------|----------|
| **.NET** | 8.0.406 | 运行时环境 | 与服务端保持一致，便于维护 |
| **WPF** | .NET 8.0 | UI框架 | 成熟稳定、强大数据绑定、MVVM支持 |
| **Prism** | 8.1.97 | MVVM框架 | 模块化开发、依赖注入、导航服务 |

### UI组件和样式
| 技术 | 版本 | 用途 | 特性说明 |
|------|------|------|----------|
| **Material Design** | 4.9.0 | UI主题库 | 现代化设计、丰富的控件 |
| **MahApps.Metro** | 2.4.9 | Metro风格 | 现代化UI主题、窗口管理 |
| **HandyControl** | 3.4.0 | 控件库 | 丰富的自定义控件、样式统一 |

### 通信和数据
| 技术 | 版本 | 用途 | 特性说明 |
|------|------|------|----------|
| **Refit** | 8.0.0 | HTTP客户端 | 类型安全的API调用、自动生成 |
| **System.Net.Http** | 8.0.20 | HTTP通信 | HttpClient工厂、请求重试 |
| **Newtonsoft.Json** | 13.0.3 | JSON序列化 | 功能丰富、配置灵活 |

### 架构模式
| 技术 | 版本 | 用途 | 实现方式 |
|------|------|------|----------|
| **MVVM** | - | 架构模式 | 数据绑定、命令绑定、视图分离 |
| **依赖注入** | 8.0.20 | DI容器 | Microsoft.Extensions.DependencyInjection |
| **事件聚合** | - | 事件通信 | Prism事件聚合器、松耦合通信 |

## 🔄 共享层技术栈

### 核心组件
| 技术 | 版本 | 用途 | 跨端特性 |
|------|------|------|----------|
| **AutoMapper** | 12.0.1 | 对象映射 | DTO与实体转换、配置映射 |
| **FluentValidation** | 11.9.0 | 数据验证 | 链式验证规则、自定义验证器 |
| **MediatR** | 12.2.0 | 中介模式 | 命令查询分离、事件处理 |

### 工具库
| 技术 | 版本 | 用途 | 功能特性 |
|------|------|------|----------|
| **Humanizer** | 2.14.1 | 字符串处理 | 人性化显示、复数处理 |
| **PinyinNet** | 1.2.0 | 拼音转换 | 中文转拼音、声调支持 |
| **NodaTime** | 3.1.9 | 日期时间 | 时区处理、日历支持 |

### 缓存和存储
| 技术 | 版本 | 用途 | 特性说明 |
|------|------|------|----------|
| **MemoryCache** | 8.0.20 | 内存缓存 | 高性能缓存、过期策略 |
| **System.IO.Abstractions** | 19.3.0 | 文件操作抽象 | 便于测试、文件系统模拟 |

## 📦 NuGet包管理

### Directory.Packages.props
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- 核心框架 -->
    <PackageVersion Include="Microsoft.AspNetCore.App" />
    <PackageVersion Include="Microsoft.NET.Sdk" />
    
    <!-- 数据访问 -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.20" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.20" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.20" />
    <PackageVersion Include="Dapper" Version="2.1.35" />
    
    <!-- API和文档 -->
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="7.2.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Versioning" Version="5.1.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer" Version="5.1.0" />
    
    <!-- 认证和安全 -->
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.20" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.20" />
    <PackageVersion Include="System.IdentityModel.Tokens.Jwt" Version="7.1.2" />
    
    <!-- 验证和映射 -->
    <PackageVersion Include="FluentValidation.AspNetCore" Version="11.3.0" />
    <PackageVersion Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
    
    <!-- 客户端框架 -->
    <PackageVersion Include="Prism.DryIoc" Version="8.1.97" />
    <PackageVersion Include="Prism.Wpf" Version="8.1.97" />
    <PackageVersion Include="Refit" Version="8.0.0" />
    
    <!-- UI组件 -->
    <PackageVersion Include="MaterialDesignThemes" Version="4.9.0" />
    <PackageVersion Include="MahApps.Metro" Version="2.4.9" />
    <PackageVersion Include="HandyControl" Version="3.4.0" />
    
    <!-- 工具库 -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.20" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="5.0.1" />
    <PackageVersion Include="Serilog.Sinks.File" Version="5.0.0" />
    
    <!-- 测试框架 -->
    <PackageVersion Include="NUnit" Version="3.13.3" />
    <PackageVersion Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageVersion Include="Moq" Version="4.20.69" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>
</Project>
```

## 🛠️ 开发工具配置

### IDE和编辑器
| 工具 | 版本 | 用途 | 推荐插件 |
|------|------|------|----------|
| **Visual Studio** | 2022 Professional | 主要IDE | ReSharper、CodeMaid |
| **Visual Studio Code** | 1.85+ | 轻量编辑 | C#、GitLens、Docker |
| **SQL Server Management Studio** | 19+ | 数据库管理 | - |

### 版本控制
| 工具 | 版本 | 用途 | 配置说明 |
|------|------|------|----------|
| **Git** | 2.42+ | 版本控制 | 使用Git Flow工作流 |
| **GitHub Desktop** | 3.4+ | Git客户端 | 可视化操作、冲突解决 |

### 构建和部署
| 工具 | 版本 | 用途 | 特性说明 |
|------|------|------|----------|
| **Docker Desktop** | 4.26+ | 容器化 | 开发环境容器化 |
| **Azure Data Studio** | 1.45+ | 数据库管理 | 跨平台数据库工具 |

## 🔧 环境配置

### 开发环境要求
```bash
# 必需的.NET SDK
dotnet --version  # 应显示 8.0.406

# 数据库要求
# SQL Server 2019+ 或 SQL Server Express 2019+

# 推荐的开发工具
# Visual Studio 2022 Professional
# 或 Visual Studio Code + C# Dev Kit
```

### 环境变量配置
```bash
# 开发环境
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development

# 数据库连接
export ConnectionStrings__DefaultConnection="Server=(localdb)\\mssqllocaldb;Database=LYBT_Clinic_Dev;Trusted_Connection=true"

# JWT配置
export Jwt__Issuer="LYBT-Clinic-Dev"
export Jwt__Audience="LYBT-Clinic-Users-Dev"
export Jwt__Secret="YourSecretKeyHereMustBeAtLeast32CharactersLong!"
```

## 📈 性能优化配置

### Server端优化
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LYBT_Clinic;Trusted_Connection=true;MultipleActiveResultSets=true;Max Pool Size=100;Min Pool Size=5;"
  },
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 10485760,
      "RequestHeadersTimeout": "00:01:00"
    }
  }
}
```

### Client端优化
```xml
<!-- App.config或通过代码配置 -->
<configuration>
  <runtime>
    <gcAllowVeryLargeObjects enabled="true" />
    <gcConcurrent enabled="true" />
  </runtime>
</configuration>
```

## 🔒 安全配置

### HTTPS配置
```csharp
// Program.cs
builder.Services.AddHsts(options =>
{
    options.PreloadHeaders = "Strict-Transport-Security";
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
```

### CORS配置
```csharp
// 允许的跨域配置
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy => policy.WithOrigins("https://localhost:5001")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
});
```

## 📊 版本兼容性

### 支持的操作系统
| 平台 | 最低版本 | 支持状态 | 备注 |
|------|----------|----------|------|
| **Windows** | 10 1809+ | ✅ 完全支持 | 主要开发平台 |
| **Linux** | Ubuntu 20.04+ | ✅ 完全支持 | 服务端部署 |
| **macOS** | 12.0+ | ⚠️ 部分支持 | 服务端开发 |

### 浏览器支持
| 浏览器 | 最低版本 | 支持状态 | 备注 |
|------|----------|----------|------|
| **Chrome** | 90+ | ✅ 完全支持 | 主要测试浏览器 |
| **Edge** | 90+ | ✅ 完全支持 | Windows默认浏览器 |
| **Firefox** | 88+ | ✅ 完全支持 | 开源浏览器 |
| **Safari** | 14+ | ⚠️ 部分支持 | macOS平台 |

## 🔄 版本升级策略

### 升级原则
1. **安全性优先** - 安全补丁及时升级
2. **稳定性保障** - 选择LTS版本，避免频繁升级
3. **兼容性考虑** - 升级前进行充分测试
4. **渐进式升级** - 分模块逐步升级

### 升级计划
- **月度更新**：安全补丁和小版本更新
- **季度评估**：主要框架版本升级评估
- **年度升级**：.NET主要版本升级

## 📚 学习资源

### 官方文档
- [.NET 8.0 文档](https://docs.microsoft.com/dotnet/)
- [ASP.NET Core 8.0](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core 8.0](https://docs.microsoft.com/ef/core/)
- [WPF 文档](https://docs.microsoft.com/dotnet/desktop/wpf/)

### 技术博客
- [.NET Blog](https://devblogs.microsoft.com/dotnet/)
- [ASP.NET Blog](https://devblogs.microsoft.com/aspnet/)
- [Entity Framework Team Blog](https://devblogs.microsoft.com/dotnet/category/ef/)

### 社区资源
- [GitHub .NET](https://github.com/dotnet)
- [Stack Overflow .NET](https://stackoverflow.com/questions/tagged/.net)
- [Microsoft Q&A](https://learn.microsoft.com/en-us/answers/)

---

**文档维护**：开发组 | **最后更新**：2025-10-16  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核