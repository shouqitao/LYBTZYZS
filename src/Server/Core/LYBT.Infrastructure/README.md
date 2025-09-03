# LYBT.Infrastructure

> **基础设施核心模块** - UltraThink架构优化版  
> 系统底层数据访问、配置管理和核心服务的统一基础设施
> **项目状态**: ✅ **生产就绪** | 🎆 **P8-01E架构重构完成** | **零编译错误**

## 🎯 模块概述

LYBT.Infrastructure是系统的核心基础设施模块，采用UltraThink简化架构设计，专为小型中医诊所（<20人）优化。提供数据库访问、配置管理、安全服务等核心基础功能，是所有业务模块的统一底层支撑。

**技术栈**: .NET 8.0 + Entity Framework Core 8.0.17 + SQL Server + IMemoryCache

## 🎆 P8-01E架构重构成果 (历史性完成)

**UltraThink目录结构简化 (2025-09-02完成)**:
- ✅ **目录精简52%**: 从21个目录 → 10个目录
- ✅ **命名空间统一**: 消除层级混乱，统一命名规范  
- ✅ **功能集中化**: Data、Configuration、Security功能明确分离
- ✅ **标准化迁移**: Migrations目录符合EF Core标准位置
- ✅ **代码无冗余**: 移除单文件目录和过度嵌套结构

**重构前后对比**:
```
重构前 (21个目录):                重构后 (10个目录):
├── Data/                        ├── Configuration/
├── Database/                    │   ├── Options/      (配置选项类)
├── Options/                     │   └── Dtos/         (配置DTO)
├── Extensions/                  ├── Data/             (数据访问核心)
├── Logging/                     ├── Migrations/       (EF Core迁移)
├── Services/                    ├── Repositories/     (仓储模式)  
├── Specifications/              ├── Security/         (安全服务)
├── Security/                    │   └── Services/     (安全子服务)
│   ├── Data/                    ├── Storage/          (文件存储)
│   └── Interfaces/              ├── Web/              (Web基类)
├── Repositories/                ├── Interfaces/       (接口定义)  
│   ├── Base/                    └── [根级文件]        (基础组件)
│   └── Optimized/
└── ...13 more directories
```

## 🏗️ 核心架构 (UltraThink简化版)

### 统一数据访问 - AppDbContext

```csharp
public class AppDbContext : DbContext
{
    // 8个业务核心实体
    public DbSet<UserModel> Users { get; set; }
    public DbSet<PatientModel> Patients { get; set; }
    public DbSet<MedicalCaseModel> MedicalCases { get; set; }
    public DbSet<ConsultationModel> Consultations { get; set; }
    public DbSet<PrescriptionModel> Prescriptions { get; set; }
    public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }
    public DbSet<HerbModel> Herbs { get; set; }
    public DbSet<FormulaModel> Formulas { get; set; }
    
    // 支持实体
    public DbSet<FormulaHerbItem> FormulaHerbItems { get; set; }
    public DbSet<AuthSessionModel> AuthSessions { get; set; }
    public DbSet<AdminSecretModel> AdminSecrets { get; set; }
    public DbSet<TokenStoreEntity> TokenStore { get; set; }
}
```

### 优化Repository模式

```csharp
public class OptimizedBaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly IMemoryCache _cache;
    protected readonly ILogger _logger;
    
    // 智能缓存CRUD操作
    public async Task<T?> GetByIdAsync(Guid id, bool useCache = true)
    {
        if (useCache && _cache.TryGetValue($"{typeof(T).Name}_{id}", out T? cached))
            return cached;
            
        var entity = await _context.Set<T>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            
        if (entity != null && useCache)
            _cache.Set($"{typeof(T).Name}_{id}", entity, TimeSpan.FromMinutes(10));
            
        return entity;
    }
}
```

## 📦 核心组件架构

### 1. 数据访问层 (Data/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **AppDbContext** | 统一数据库上下文，管理所有实体 | ✅ 完成 |
| **AppDbContextFactory** | 设计时DbContext工厂，支持迁移 | ✅ 完成 |
| **DatabaseInitializationService** | 数据库初始化和种子数据管理 | ✅ 完成 |

### 2. 配置管理层 (Configuration/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **SimplifiedConfigurationService** | 简化配置服务，环境变量支持 | ✅ 完成 |
| **Options/** | 所有配置选项类(Auth, Database, JWT等) | ✅ 完成 |
| **Dtos/** | 配置相关DTO类 | ✅ 完成 |

#### 配置选项体系

```csharp
// 主要配置选项类
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
}

public class JwtOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryInHours { get; set; } = 8;
    public int RememberMeExpiryInDays { get; set; } = 30;
}

public class CacheOptions
{
    public int DefaultExpirationMinutes { get; set; } = 10;
    public int MaxMemoryUsageMB { get; set; } = 128;
    public bool EnableCompaction { get; set; } = true;
}
```

### 3. 安全服务层 (Security/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **EnhancedJwtService** | JWT生成、验证、刷新 | ✅ 完成 |
| **EncryptionService** | 数据加密解密 | ✅ 完成 |
| **InputValidationService** | 输入验证和SQL注入防护 | ✅ 完成 |
| **SecurityConfigurationService** | 安全配置管理 | ✅ 完成 |
| **Services/DatabaseTokenStoreService** | JWT Token持久化存储 | ✅ 完成 |
| **Services/TokenCleanupService** | 过期Token清理 | ✅ 完成 |

#### JWT安全增强

```csharp
public class EnhancedJwtService : IEnhancedJwtService
{
    // JWT生成与验证
    public async Task<JwtTokenResult> GenerateTokenAsync(User user, bool rememberMe = false)
    {
        var tokenId = Guid.NewGuid();
        var expiry = rememberMe 
            ? DateTime.UtcNow.AddDays(_jwtOptions.RememberMeExpiryInDays)
            : DateTime.UtcNow.AddHours(_jwtOptions.ExpiryInHours);
            
        // Token持久化存储
        await _tokenStore.StoreTokenAsync(tokenId, user.Id, expiry);
        
        return new JwtTokenResult
        {
            Token = GenerateJwtToken(user, tokenId, expiry),
            RefreshToken = GenerateRefreshToken(),
            ExpiresAt = expiry
        };
    }
}
```

### 4. Repository层 (Repositories/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **IBaseRepository** | Repository接口定义 | ✅ 完成 |
| **BaseRepository** | 基础Repository实现 | ✅ 完成 |
| **OptimizedBaseRepository** | 带缓存优化的Repository | ✅ 完成 |
| **RepositoryBase** | 通用Repository基类 | ✅ 完成 |

### 5. Web基础设施 (Web/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **BaseControllerCore** | 控制器核心基类 | ✅ 完成 |
| **BaseApiController** | 业务API控制器基类 | ✅ 完成 |
| **BaseSystemController** | 系统管理控制器基类 | ✅ 完成 |
| **ApiErrorCodes** | 统一错误码定义 | ✅ 完成 |

### 6. 存储服务 (Storage/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **IFileStorageService** | 文件存储接口 | ✅ 完成 |
| **LocalFileStorageService** | 本地文件存储实现 | ✅ 完成 |

## 🗃️ 数据库迁移管理

### EF Core迁移命令

```bash
# 添加新迁移
dotnet ef migrations add MigrationName \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI

# 应用迁移
dotnet ef database update \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI

# 查看迁移状态
dotnet ef migrations list \
  --project src/Server/Core/LYBT.Infrastructure \
  --startup-project src/Server/Services/LYBT.WebAPI
```

### 迁移历史记录

| 迁移文件 | 功能描述 | 日期 |
|----------|----------|------|
| `20250902150113_JWT_TokenStore_Security_Enhancement` | JWT Token存储安全增强 | 2025-09-02 |
| `20250810112700_Auth_UltraThink_Refactor` | Auth模块UltraThink重构 | 2025-08-10 |
| `AddPerformanceIndexes_20250811` | 性能索引优化 | 2025-08-11 |
| `20250807044558_FieldStandardization_RemoveUnusedFields` | 字段标准化 | 2025-08-07 |
| `20250802153359_AddSysAdminSeedData` | 系统管理员种子数据 | 2025-08-02 |

## ⚡ 性能优化

### 智能缓存系统

```csharp
// 缓存策略配置
services.Configure<CacheOptions>(options =>
{
    options.DefaultExpirationMinutes = 10;    // 默认10分钟过期
    options.MaxMemoryUsageMB = 128;          // 最大内存使用128MB
    options.EnableCompaction = true;          // 启用内存压缩
});

// 使用示例
public async Task<PatientDto> GetPatientAsync(Guid id)
{
    var cacheKey = $"Patient_{id}";
    
    if (_cache.TryGetValue(cacheKey, out PatientDto? cached))
        return cached;
        
    var patient = await _repository.GetByIdAsync(id);
    var dto = _mapper.Map<PatientDto>(patient);
    
    _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(10));
    return dto;
}
```

### 数据库连接优化

```csharp
// 连接池配置 (适合小型部署<20人)
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
    });
}, ServiceLifetime.Scoped);

// 连接池大小
"ConnectionStrings": {
    "DefaultConnection": "...;Max Pool Size=20;Min Pool Size=2;..."
}
```

### 批量操作优化

```csharp
// EF Core批量更新
public async Task BatchUpdateStatusAsync(List<Guid> ids, EntityStatus status)
{
    await _context.Set<T>()
        .Where(x => ids.Contains(x.Id))
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.Status, status)
            .SetProperty(x => x.UpdateTime, DateTime.Now));
}
```

## 🔧 服务注册

### 统一服务注册

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 数据库上下文
        services.AddDbContext<AppDbContext>();
        
        // 2. Repository层
        services.AddScoped(typeof(IBaseRepository<>), typeof(OptimizedBaseRepository<>));
        
        // 3. 配置服务
        services.AddSingleton<ISimplifiedConfigurationService, SimplifiedConfigurationService>();
        
        // 4. 安全服务
        services.AddScoped<IEnhancedJwtService, EnhancedJwtService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<ITokenStoreService, DatabaseTokenStoreService>();
        
        // 5. 缓存和存储
        services.AddMemoryCache();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        
        // 6. 数据库初始化
        services.AddScoped<DatabaseInitializationService>();
        
        return services;
    }
}
```

## 🎯 UltraThink架构特点

**适合小型中医诊所(<20人)的精简设计**:
- ✅ **目录精简**: 52%目录减少，结构清晰易维护
- ✅ **智能缓存**: IMemoryCache适合小规模部署，无需Redis
- ✅ **统一数据访问**: 单一AppDbContext管理所有实体
- ✅ **安全增强**: JWT Token持久化，防重放攻击
- ✅ **性能优化**: 连接池、批量操作、缓存策略优化
- ✅ **配置简化**: 环境变量覆盖，敏感信息保护

## 🧪 使用示例

### 数据库操作示例

```csharp
// Repository层使用
public class PatientService
{
    private readonly IBaseRepository<PatientModel> _repository;
    
    public async Task<PatientDto> CreatePatientAsync(CreatePatientDto dto)
    {
        var patient = _mapper.Map<PatientModel>(dto);
        
        // 自动缓存管理
        var created = await _repository.CreateAsync(patient);
        
        return _mapper.Map<PatientDto>(created);
    }
    
    public async Task<List<PatientDto>> SearchPatientsAsync(string keyword)
    {
        // 缓存查询结果
        var cacheKey = $"PatientSearch_{keyword.GetHashCode()}";
        
        if (_cache.TryGetValue(cacheKey, out List<PatientDto>? cached))
            return cached;
            
        var patients = await _repository.FindAsync(p => 
            p.Name.Contains(keyword) || 
            p.PhoneNumber.Contains(keyword));
            
        var result = _mapper.Map<List<PatientDto>>(patients);
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return result;
    }
}
```

### 安全服务使用

```csharp
// JWT服务使用
public class AuthService
{
    private readonly IEnhancedJwtService _jwtService;
    
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // 用户验证...
        
        // 生成JWT Token
        var tokenResult = await _jwtService.GenerateTokenAsync(user, request.RememberMe);
        
        return new LoginResponse
        {
            AccessToken = tokenResult.Token,
            RefreshToken = tokenResult.RefreshToken,
            ExpiresAt = tokenResult.ExpiresAt,
            User = _mapper.Map<UserDto>(user)
        };
    }
}
```

## 📚 相关文档

- [LYBT.Entities](../LYBT.Entities/README.md) - 数据实体模型
- [业务模块文档](../../Modules/) - 8个业务模块详细说明  
- [WebAPI服务](../../Services/LYBT.WebAPI/README.md) - Web API接口文档
- [数据库设计](./Data/DATABASE_DESIGN.md) - 数据库结构设计文档

## 🚀 开发指南

### 添加新Repository

1. 继承OptimizedBaseRepository基类
2. 实现特定业务查询方法
3. 注册到DI容器
4. 配置缓存策略

### 添加新配置选项

1. 创建Options类
2. 在appsettings.json中添加配置节
3. 注册到服务容器
4. 在SimplifiedConfigurationService中添加访问方法

### 性能监控

- 使用ILogger记录关键操作
- 监控缓存命中率
- 跟踪数据库连接池使用情况
- 定期清理过期Token和缓存

---

> 📌 **UltraThink成果**: Infrastructure模块经过P8-01E重构，实现52%目录精简，架构清晰，适合小型诊所部署
> 🎆 **生产就绪**: 零编译错误，完整的安全、缓存、数据库基础设施，可直接投入生产使用