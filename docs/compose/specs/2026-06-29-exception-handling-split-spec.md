# ExceptionHandling 拆分规格

> 日期: 2026-06-29
> 状态: 草稿
> 优先级: HIGH
> 来源: shared-architecture-optimization.md [O1] H1

## [S1] 问题

`LYBT.Shared.ExceptionHandling` 引入 ASP.NET Core 和 EF Core 依赖，导致 Desktop 客户端依赖过重：

```xml
<!-- LYBT.Shared.ExceptionHandling.csproj -->
<FrameworkReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.EntityFrameworkCore" />
```

**影响**:
- Desktop 客户端不需要 ASP.NET Core 功能
- 增加部署包大小
- 引入不必要的安全攻击面

## [S2] 目标

1. 拆分为 Desktop 和 Server 两个版本
2. Desktop 仅包含基础异常类型
3. Server 包含 ASP.NET Core 和 EF Core 处理器

## [S3] 拆分方案

### LYBT.Shared.ExceptionHandling（基础）

```csharp
// 位置: LYBT.Shared.ExceptionHandling/
// 包含: 基础异常类型、错误码、错误消息

// 异常类型
public class AppException : Exception { }
public class BusinessException : AppException { }
public class NotFoundException : AppException { }
public class ConflictException : AppException { }
public class UnauthorizedException : AppException { }

// 错误码
public enum ErrorCode
{
    Unknown = 0,
    // ... 所有错误码
}

// 错误消息
public static class ErrorMessages
{
    public static string Get(ErrorCode code) { }
    public static string GetUserMessage(ErrorCode code) { }
}
```

### LYBT.Shared.ExceptionHandling.AspNetCore（Server 专用）

```csharp
// 位置: LYBT.Shared.ExceptionHandling.AspNetCore/
// 包含: ASP.NET Core 处理器、中间件

// 异常处理器
public class BusinessExceptionHandler : IExceptionHandler { }
public class SystemExceptionHandler : IExceptionHandler { }

// ProblemDetails 配置
public static class ProblemDetailsConfiguration
{
    public static void UseStatusCodePagesWithProblemDetails(this IApplicationBuilder app) { }
}

// CorrelationId 中间件
public class CorrelationIdMiddleware
{
    public async Task InvokeAsync(HttpContext context) { }
}

// DI 注册
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServerExceptionHandling(this IServiceCollection services) { }
}
```

### LYBT.Shared.ExceptionHandling.EFCore（Server 专用）

```csharp
// 位置: LYBT.Shared.ExceptionHandling.EFCore/
// 包含: EF Core 异常处理器

// 并发冲突处理器
public class DbUpdateConcurrencyExceptionHandler : IExceptionHandler { }

// 数据库异常处理器
public class DbUpdateExceptionHandler : IExceptionHandler { }
```

## [S4] 依赖关系

```
LYBT.Shared.ExceptionHandling (基础)
├── 无外部依赖

LYBT.Shared.ExceptionHandling.AspNetCore
├── LYBT.Shared.ExceptionHandling (基础)
├── Microsoft.AspNetCore.App (FrameworkReference)

LYBT.Shared.ExceptionHandling.EFCore
├── LYBT.Shared.ExceptionHandling (基础)
├── Microsoft.EntityFrameworkCore (PackageReference)
```

## [S5] 项目文件

### LYBT.Shared.ExceptionHandling.csproj（修改）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>LYBT.Shared.ExceptionHandling</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <!-- 移除 ASP.NET Core 和 EF Core 依赖 -->
    <!-- <FrameworkReference Include="Microsoft.AspNetCore.App" /> -->
    <!-- <PackageReference Include="Microsoft.EntityFrameworkCore" /> -->
  </ItemGroup>
</Project>
```

### LYBT.Shared.ExceptionHandling.AspNetCore.csproj（新建）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>LYBT.Shared.ExceptionHandling.AspNetCore</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\LYBT.Shared.ExceptionHandling\LYBT.Shared.ExceptionHandling.csproj" />
  </ItemGroup>
</Project>
```

### LYBT.Shared.ExceptionHandling.EFCore.csproj（新建）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>LYBT.Shared.ExceptionHandling.EFCore</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\LYBT.Shared.ExceptionHandling\LYBT.Shared.ExceptionHandling.csproj" />
  </ItemGroup>
</Project>
```

## [S6] 消费方修改

### Server 端（LYBT.WebAPI）

```xml
<!-- LYBT.WebAPI.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\Shared\LYBT.Shared.ExceptionHandling\LYBT.Shared.ExceptionHandling.csproj" />
  <ProjectReference Include="..\..\Shared\LYBT.Shared.ExceptionHandling.AspNetCore\LYBT.Shared.ExceptionHandling.AspNetCore.csproj" />
  <ProjectReference Include="..\..\Shared\LYBT.Shared.ExceptionHandling.EFCore\LYBT.Shared.ExceptionHandling.EFCore.csproj" />
</ItemGroup>
```

### Desktop 端（LYBT.Desktop.Shell）

```xml
<!-- LYBT.Desktop.Shell.csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\Shared\LYBT.Shared.ExceptionHandling\LYBT.Shared.ExceptionHandling.csproj" />
  <!-- 不引用 AspNetCore 和 EFCore -->
</ItemGroup>
```

## [S7] 实施步骤

1. 创建 `LYBT.Shared.ExceptionHandling.AspNetCore` 项目
2. 创建 `LYBT.Shared.ExceptionHandling.EFCore` 项目
3. 移动 ASP.NET Core 相关代码到 AspNetCore 项目
4. 移动 EF Core 相关代码到 EFCore 项目
5. 修改 `LYBT.Shared.ExceptionHandling` 移除依赖
6. 更新所有消费方项目引用
7. 运行 `dotnet build` 和 `dotnet test` 验证

## [S8] 验收标准

1. `LYBT.Shared.ExceptionHandling` 不依赖 ASP.NET Core 和 EF Core
2. Desktop 仅引用基础项目
3. Server 引用所有三个项目
4. 所有现有测试通过
5. `dotnet build` 成功
6. Desktop 部署包大小减少
