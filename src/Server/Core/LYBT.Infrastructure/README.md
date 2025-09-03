# LYBT.Infrastructure

> **基础设施模块** - UltraThink架构优化版  
> 系统底层数据访问、配置管理和核心服务的统一基础设施

## 🎯 模块概述

LYBT.Infrastructure是系统的核心基础设施模块，采用UltraThink简化架构设计，专为小型诊所（<20人）优化。提供数据库访问、配置管理、安全服务等核心基础功能，是所有业务模块的统一底层支撑。

## 🏗️ 核心能力 (UltraThink简化版)

- **统一数据访问**: 基于Entity Framework Core 8.0.17的AppDbContext，管理所有业务实体
- **智能缓存系统**: 基于IMemoryCache的高效缓存，适合小规模部署  
- **配置管理**: 统一Configuration/Options体系，环境变量安全覆盖
- **数据库初始化**: 自动化DatabaseInitializationService，种子数据管理
- **Repository模式**: OptimizedBaseRepository基类，带缓存优化的CRUD操作
- **安全组件**: JWT认证、Token存储、密码安全等核心安全服务

## 🎆 P8-01E架构重构成果

**UltraThink目录结构简化 (2025-09-02完成)**:
- ✅ **目录精简52%**: 从21个目录 → 10个目录
- ✅ **命名空间统一**: 消除层级混乱，统一命名规范
- ✅ **功能集中化**: Data、Configuration、Security功能明确分离
- ✅ **标准化迁移**: Migrations目录符合EF Core标准位置

**重构前后对比**:
```
重构前 (21个目录):          重构后 (10个目录):
├── Data/                  ├── Configuration/
├── Database/              │   └── Options/
├── Options/               ├── Data/
├── Extensions/            ├── Migrations/ 
├── Logging/               ├── Repositories/
├── Services/              ├── Security/
├── Specifications/        ├── BaseService.cs
├── Security/              ├── ServiceCollectionExtensions.cs
│   ├── Data/              ├── SimpleLog.cs
│   └── Interfaces/        └── Specification.cs
├── Repositories/
│   ├── Base/
│   └── Optimized/
└── ...13 more dirs
```

## 📦 核心组件 (UltraThink简化架构)

### 数据访问层 (Data/)

| 组件 | 功能描述 | 位置 | 状态 |
|------|----------|------|------|
| **AppDbContext** | EF Core上下文，管理8个业务实体 | `Data/AppDbContext.cs` | ✅ 完成 |
| **AppDbContextFactory** | 设计时工厂，支持迁移和测试 | `Data/AppDbContextFactory.cs` | ✅ 完成 |
| **DatabaseInitializationService** | 数据库初始化和种子数据 | `Data/DatabaseInitializationService.cs` | ✅ 完成 |

### Repository基础架构 (Repositories/)

| 组件 | 功能描述 | 位置 | 状态 |
|------|----------|------|------|
| **BaseRepository<T>** | 基础仓储模式，标准CRUD操作 | `Repositories/BaseRepository.cs` | ✅ 完成 |
| **OptimizedBaseRepository<T>** | 优化仓储，带缓存和性能优化 | `Repositories/OptimizedBaseRepository.cs` | ✅ 完成 |

### 配置管理系统 (Configuration/)

| 组件 | 功能描述 | 位置 | 状态 |
|------|----------|------|------|
| **SimplifiedConfigurationService** | 简化配置服务，环境变量支持 | `Configuration/SimplifiedConfigurationService.cs` | ✅ 完成 |
| **AuthOptions** | JWT认证配置选项 | `Configuration/Options/AuthOptions.cs` | ✅ 完成 |
| **StorageOptions** | 存储配置选项 | `Configuration/Options/StorageOptions.cs` | ✅ 完成 |

### 安全组件 (Security/)

| 组件 | 功能描述 | 位置 | 状态 |
|------|----------|------|------|
| **TokenStoreEntity** | JWT令牌存储实体 | `Security/TokenStoreEntity.cs` | ✅ 完成 |
| **ITokenStoreService** | 令牌存储服务接口 | `Security/ITokenStoreService.cs` | ✅ 完成 |
| **PasswordHelper** | 密码哈希和验证工具 | `Security/PasswordHelper.cs` | ✅ 完成 |

### 基础服务 (根目录)

| 组件 | 功能描述 | 位置 | 状态 |
|------|----------|------|------|
| **BaseService** | 服务基类，通用功能 | `BaseService.cs` | ✅ 完成 |
| **ServiceCollectionExtensions** | 依赖注入扩展 | `ServiceCollectionExtensions.cs` | ✅ 完成 |
| **SimpleLog** | 简化日志服务 | `SimpleLog.cs` | ✅ 完成 |
| **Specification<T>** | 规约模式基类 | `Specification.cs` | ✅ 完成 |

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

## 🚀 数据库管理 (P8-01E标准化)

### 迁移管理 - EF Core标准位置

**迁移文件位置**: `Migrations/` (符合EF Core标准目录结构)

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

### 数据库初始化服务

**DatabaseInitializationService** (已迁移到 `Data/` 目录):

```csharp
// 命名空间: LYBT.Infrastructure.Data  
public class DatabaseInitializationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializationService> _logger;

    public async Task InitializeDatabaseAsync()
    {
        // 确保数据库存在
        await _context.Database.EnsureCreatedAsync();
        
        // 应用挂起的迁移
        await _context.Database.MigrateAsync();
        
        // 执行种子数据初始化
        await SeedAdminDataAsync();
    }

    private async Task SeedAdminDataAsync()
    {
        if (!_context.AdminSecrets.Any())
        {
            var adminSecret = new AdminSecret
            {
                Id = Guid.NewGuid(),
                Username = "sysadmin", 
                PasswordHash = PasswordHelper.Hash("Admin@123456"),
                CreateTime = DateTime.Now
            };
            
            _context.AdminSecrets.Add(adminSecret);
            await _context.SaveChangesAsync();
        }
    }
}
```

## 🔐 安全特性 (UltraThink简化版)

### 配置选项统一管理

**配置位置**: `Configuration/Options/` (P8-01E标准化)

```json
{
  "AuthOptions": {
    "Secret": "YourSuperSecureKeyHere-MinimumLength32Characters",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client", 
    "ExpireMinutes": 480,
    "RememberMeExpireMinutes": 43200
  }
}
```

### 密码安全 (PasswordHelper)

- **加密算法**: AspNetCore Identity Hash + 盐值
- **位置**: `Security/PasswordHelper.cs`
- **最小复杂度**: 8位以上，包含大小写字母、数字
- **默认密码**: Admin@123456 (⚠️ 生产环境必须修改)

### 令牌安全 (TokenStore)

```csharp
// Security/TokenStoreEntity.cs
public class TokenStoreEntity  
{
    public string TokenHash { get; set; }
    public DateTime ExpiryTime { get; set; }
    public bool IsRevoked { get; set; }
}
```

## 📈 性能指标 (小诊所优化)

### 数据库性能
- **连接池配置**: Max=20, Min=2 (适合<20人规模)  
- **查询优化**: LINQ + EF Core参数化查询
- **缓存策略**: IMemoryCache智能缓存，DefaultCacheDuration配置

### 系统性能
- **OptimizedBaseRepository**: 带缓存的Repository层
- **SimplifiedConfigurationService**: 轻量级配置管理  
- **DatabaseInitializationService**: 自动化数据库初始化

## 📁 UltraThink架构总结

### P8-01E重构成果
```
✅ 目录结构简化52%: 21个目录 → 10个目录
✅ 命名空间统一: 消除LYBT.Infrastructure.xxx层级混乱
✅ 功能集中化: Data/Configuration/Security明确分离
✅ 标准化迁移: 符合EF Core最佳实践
✅ 零编译错误: 后端整体解决方案稳定运行
```

### 小诊所适配特点
- **实用主义**: 删除企业级过度设计组件
- **简化维护**: 减少认知负荷和维护复杂度  
- **性能适配**: 专为<20人规模优化配置
- **安全简化**: 保留核心安全，移除复杂安全策略

---

> 📌 **P8-01E架构重构完成** - Infrastructure模块已达到UltraThink简化标准  
> 🎯 **下一阶段**: P8-01F Auth模块复杂性简化重构
