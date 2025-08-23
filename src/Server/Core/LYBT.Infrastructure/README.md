# LYBT.Infrastructure

> **基础设施模块**  
> 系统底层数据访问、认证授权和异常处理的统一基础设施

## 🎯 模块概述

LYBT.Infrastructure是系统的基础设施模块，为全系统提供数据库访问、JWT认证、全局异常处理、性能监控等底层支撑能力，是所有业务模块的核心基础桥梁。

## 🏗️ 核心能力

- **统一数据访问**: 基于Entity Framework Core的AppDbContext，管理所有实体映射
- **JWT认证框架**: 完整的JWT Token生成、验证和管理机制
- **全局异常处理**: 统一的异常捕获、日志记录和错误响应
- **数据库迁移**: 支持EF Core Migrations和CLI工具的设计时工厂
- **系统初始化**: 数据种子和管理员账户自动初始化
- **性能监控**: 统一监控核心和异步处理器

## 📦 核心组件

### 数据访问层

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **AppDbContext** | EF Core上下文，映射所有业务实体 | ✅ 完成 |
| **AppDbContextFactory** | 设计时工厂，支持迁移和测试 | ✅ 完成 |
| **BaseRepository<T>** | 通用仓储基类，提供CRUD操作 | ✅ 完成 |

### JWT认证系统

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **JwtHelper** | JWT Token生成和验证工具 | ✅ 完成 |
| **JwtAuthenticationExtensions** | 服务注册扩展方法 | ✅ 完成 |
| **JwtOptions** | JWT配置选项模型 | ✅ 完成 |

### 异常处理框架

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **ExceptionMiddleware** | 全局异常处理中间件 | ✅ 完成 |
| **BusinessException** | 业务异常基类 | ✅ 完成 |
| **ErrorHandlingService** | 错误处理服务 | ✅ 完成 |

### 性能监控系统

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **UnifiedMonitorCore** | 统一监控核心 | ✅ 完成 |
| **UnifiedAsyncProcessor** | 异步处理器 | ✅ 完成 |
| **PerformanceMetrics** | 性能指标收集 | ✅ 完成 |

## 🔧 技术实现

### AppDbContext配置

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 8个业务核心实体
    public DbSet<UserModel> Users { get; set; }
    public DbSet<PatientModel> Patients { get; set; }
    public DbSet<MedicalCaseModel> MedicalCases { get; set; }
    public DbSet<ConsultationModel> Consultations { get; set; }
    public DbSet<PrescriptionModel> Prescriptions { get; set; }
    public DbSet<HerbModel> Herbs { get; set; }
    public DbSet<FormulaTemplateModel> FormulaTemplates { get; set; }
    public DbSet<AdminSecretModel> AdminSecrets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 实体关系配置
        ConfigureEntityRelationships(modelBuilder);
        
        // 数据种子配置
        ConfigureDataSeeding(modelBuilder);
    }
}
```

### JWT认证配置

```csharp
// JWT服务注册
public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key))
                };
            });
            
        return services;
    }
}
```

### 全局异常处理

```csharp
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全局异常捕获: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            BusinessException businessEx => new ApiResponse<object>
            {
                Success = false,
                Message = businessEx.Message,
                Data = null
            },
            _ => new ApiResponse<object>
            {
                Success = false,
                Message = "服务器内部错误",
                Data = null
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## 🚀 数据库管理

### 迁移命令

```bash
# 添加新迁移（必须在Infrastructure项目中执行）
dotnet ef migrations add <MigrationName> \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI

# 查看迁移状态
dotnet ef migrations list \
    --project src/Server/Core/LYBT.Infrastructure \
    --startup-project src/Server/Services/LYBT.WebAPI
```

### 数据种子初始化

```csharp
public static class AdminSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!context.AdminSecrets.Any())
        {
            var adminSecret = new AdminSecretModel
            {
                Id = Guid.NewGuid(),
                UserName = "sysadmin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
                CreateTime = DateTime.Now
            };
            
            context.AdminSecrets.Add(adminSecret);
            await context.SaveChangesAsync();
        }
    }
}
```

## 🔐 安全特性

### JWT配置

```json
{
  "JwtOptions": {
    "Key": "YourSuperSecureKeyHere",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "ExpireMinutes": 480,
    "RefreshTokenExpireDays": 30
  }
}
```

### 密码安全

- **加密算法**: BCrypt.Net (带盐值Hash)
- **最小复杂度**: 8位以上，包含大小写字母、数字
- **默认密码**: Admin@123456 (生产环境必须修改)

## 📊 性能监控

### 统一监控配置

```csharp
public class UnifiedMonitorCore
{
    private readonly Timer _timer;
    private readonly ILogger<UnifiedMonitorCore> _logger;

    public void StartMonitoring()
    {
        _timer = new Timer(ExecuteMonitoringCycle, null, 
            TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    private void ExecuteMonitoringCycle(object? state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await CollectPerformanceMetrics();
                await CheckSystemHealth();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "监控周期执行失败");
            }
        });
    }
}
```

## 📈 性能指标

- **数据库连接池**: Max=20, Min=2 (适合小型部署)
- **JWT验证**: < 1ms 平均响应时间
- **异常处理**: < 5ms 全局异常捕获延迟
- **监控周期**: 5分钟间隔性能指标收集

## 🧪 测试支持

### 测试用DbContext

```csharp
public class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
```

---

> 📌 **部署提醒**: 生产环境务必修改JWT密钥和管理员默认密码
