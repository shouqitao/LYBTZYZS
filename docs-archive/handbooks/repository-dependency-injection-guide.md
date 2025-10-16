# Repository依赖注入配置指南

## 概述

本文档提供Repository统一依赖注入的标准化配置方法，确保Client端和Server端的Repository注册一致性和可维护性。

## Server端配置

### 1. 基础配置

```csharp
// 在Startup.cs或Program.cs中
using LYBT.Infrastructure.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // 方式1: 自动扫描注册所有Repository
    services.AddRepositories();

    // 方式2: 指定程序集扫描
    services.AddRepositories(Assembly.GetAssembly(typeof(UserRepository)));

    // 方式3: 手动注册特定Repository
    services.AddRepository<IUserRepository, UserRepository>();
    services.AddRepository<IPatientRepository, PatientRepository>(ServiceLifetime.Singleton);

    // 注册Repository支持服务
    services.AddRepositorySupportServices();
}
```

### 2. 模块化配置

```csharp
// 在模块初始化中
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册Repository
        services.AddRepository<IUserRepository, UserRepository>();

        // 注册服务
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
```

## Client端配置

### 1. 基础配置

```csharp
// 在App.xaml.cs或模块初始化中
using LYBT.Desktop.Infrastructure.DependencyInjection;

public partial class App : PrismApplication
{
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 方式1: 自动扫描注册所有Repository
        containerRegistry.RegisterRepositories();

        // 方式2: 指定程序集扫描
        containerRegistry.RegisterRepositories(Assembly.GetAssembly(typeof(UserRepository)));

        // 方式3: 手动注册特定Repository
        containerRegistry.RegisterRepository<IUserRepository, UserRepository>();
        containerRegistry.RegisterRepository<IPatientRepository, PatientRepository>(useSingleton: false);

        // 注册Repository基类支持
        containerRegistry.RegisterRepositoryBase();
    }
}
```

### 2. 模块化配置

```csharp
// 在模块类中
public class UsersModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 使用统一扩展方法注册Repository
        containerRegistry.RegisterRepository<IUserRepository, UserRepository>();

        // 注册视图模型
        containerRegistry.Register<UserManagementViewModel>();
        containerRegistry.RegisterForNavigation<Views.UserManagementView>();
    }
}
```

### 3. 批量注册配置

```csharp
// 批量注册所有Repository
public static class RepositoryRegistry
{
    public static IContainerRegistry RegisterAllRepositories(this IContainerRegistry containerRegistry)
    {
        var mappings = RepositoryRegistrationHelper.GetRepositoryTypeMappings(Assembly.GetExecutingAssembly());
        return containerRegistry.RegisterRepositoryModules(mappings);
    }
}
```

## 最佳实践

### 1. 生命周期选择

- **Server端**: 通常使用 `Scoped` 生命周期，每个Web请求一个实例
- **Client端**: 通常使用 `Singleton` 生命周期，整个应用程序一个实例

### 2. 命名约定

- Repository接口: `I{Entity}Repository` (如: `IUserRepository`)
- Repository实现: `{Entity}Repository` (如: `UserRepository`)
- 注册方法: `Add{Entity}Repository` (如: `AddUserRepository`)

### 3. 配置位置

- **集中配置**: 在应用程序启动时统一配置所有Repository
- **模块化配置**: 在各模块中配置相关的Repository
- **混合配置**: 基础Repository集中配置，特殊Repository模块化配置

### 4. 依赖关系

```csharp
// Repository依赖日志服务
services.AddRepository<IUserRepository, UserRepository>();

// 确保依赖服务已注册
services.AddLogging();
```

## 配置示例

### Server端完整示例

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 注册Repository
builder.Services.AddRepositories();
builder.Services.AddRepositorySupportServices();

// 注册具体Repository（覆盖自动扫描）
builder.Services.AddRepository<IUserRepository, UserRepository>(ServiceLifetime.Scoped);
builder.Services.AddRepository<IPatientRepository, PatientRepository>(ServiceLifetime.Scoped);

var app = builder.Build();
app.Run();
```

### Client端完整示例

```csharp
// App.xaml.cs
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册Repository
    containerRegistry.RegisterRepositories();

    // 注册具体Repository（覆盖自动扫描）
    containerRegistry.RegisterRepository<IUserRepository, UserRepository>();
    containerRegistry.RegisterRepository<IPatientRepository, PatientRepository>();

    // 注册错误处理和日志
    containerRegistry.RegisterErrorHandlingAndLogging();
}
```

## 迁移指南

### 从现有配置迁移

1. **识别现有Repository注册**
   ```bash
   grep -r "Register.*Repository" src/
   grep -r "services\.Add.*Repository" src/
   ```

2. **替换为统一扩展方法**
   ```csharp
   // 旧代码
   services.AddScoped<IUserRepository, UserRepository>();
   containerRegistry.RegisterSingleton<IUserRepository, UserRepository>();

   // 新代码
   services.AddRepository<IUserRepository, UserRepository>();
   containerRegistry.RegisterRepository<IUserRepository, UserRepository>();
   ```

3. **验证配置**
   - 确保所有Repository都已正确注册
   - 检查依赖注入是否正常工作
   - 运行应用程序验证功能

## 故障排除

### 常见问题

1. **Repository未找到**
   - 检查接口和实现类是否匹配命名约定
   - 确认程序集是否正确扫描

2. **生命周期冲突**
   - Server端避免使用Singleton生命周期
   - Client端Repository通常应为Singleton

3. **依赖注入失败**
   - 检查依赖服务是否已注册
   - 确认构造函数参数正确

### 调试技巧

```csharp
// 检查注册的服务
var serviceProvider = services.BuildServiceProvider();
var userRepository = serviceProvider.GetService<IUserRepository>();
Console.WriteLine($"UserRepository registered: {userRepository != null}");
```

## 总结

通过使用统一的Repository依赖注入扩展方法，可以：

1. **标准化配置**: 统一Client端和Server端的Repository注册方式
2. **简化配置**: 减少重复代码，提高可维护性
3. **灵活配置**: 支持自动扫描和手动注册两种方式
4. **类型安全**: 编译时检查，减少运行时错误
5. **易于扩展**: 便于添加新的Repository类型

这些改进使Repository依赖注入配置更加规范、简洁和可维护。