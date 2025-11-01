## 🎯 项目概述

LYBT.Infrastructure是系统的核心基础设施模块，采用分层架构设计，专为小型中医诊所（<20人）优化。提供数据库访问、配置管理、安全服务等核心基础功能，是所有业务模块的统一底层支撑。

## 🏗️ 核心架构

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

### Repository模式

基于EF Core的标准Repository实现，提供统一的数据访问接口。详见 `Repositories/` 目录。

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
| **Options/** | 所有配置选项类(Auth, Database, JWT等) | ✅ 完成 |
| **Extensions/** | 配置扩展方法 | ✅ 完成 |
| **Validation/** | 生产环境配置验证 | ✅ 完成 |

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

提供基础的安全配置和ASP.NET Core DataProtection支持。详见 `Security/` 目录。

### 4. Repository层 (Repositories/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **IBaseRepository** | Repository接口定义 | ✅ 完成 |
| **BaseRepository** | 基础Repository实现 | ✅ 完成 |

### 5. Web基础设施 (Web/)

| 组件 | 功能描述 | 状态 |
|------|----------|------|
| **BaseControllerCore** | 控制器核心基类 | ✅ 完成 |
| **BaseApiController** | 业务API控制器基类 | ✅ 完成 |
| **BaseSystemController** | 系统管理控制器基类 | ✅ 完成 |
| **ApiErrorCodes** | 统一错误码定义 | ✅ 完成 |

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
| `20250810112700_Auth__Refactor` | Auth模块重构 | 2025-08-10 |
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

Infrastructure层的服务注册通过多个扩展方法分离管理，详见：
- `ServiceCollectionExtensions.cs` - 主要服务注册
- `DependencyInjection/` - 分类扩展方法

主要注册内容：
- **数据库上下文**: AppDbContext (EF Core)
- **Repository层**: IBaseRepository<T> 及其实现
- **配置管理**: IOptions<T> 模式配置
- **缓存服务**: IMemoryCache 及适配器
- **安全配置**: ASP.NET Core DataProtection
- **数据库初始化**: DatabaseInitializationService

## 🎯 分层架构特点

**适合小型中医诊所(<20人)的精简设计**:
- ✅ **结构清晰**: 按功能模块组织，易于理解和维护
- ✅ **智能缓存**: IMemoryCache适合小规模部署，无需Redis
- ✅ **统一数据访问**: 单一AppDbContext管理所有实体
- ✅ **安全配置**: ASP.NET Core DataProtection支持
- ✅ **性能优化**: 连接池、批量操作、缓存策略优化
- ✅ **配置简化**: IOptions模式，环境变量支持

## 🧪 使用示例

### 数据库操作示例

```csharp
// Repository层使用
public class PatientService
{
    private readonly IBaseRepository<PatientModel> _repository;
    private readonly AppDbContext _context;

    public async Task<PatientDto> CreatePatientAsync(CreatePatientDto dto)
    {
        var patient = new PatientModel
        {
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            // ... 其他属性映射
        };

        var created = await _repository.CreateAsync(patient);
        return MapToDto(created);
    }

    public async Task<List<PatientDto>> SearchPatientsAsync(string keyword)
    {
        var patients = await _repository.FindAsync(p =>
            p.Name.Contains(keyword) ||
            p.PhoneNumber.Contains(keyword));

        return patients.Select(MapToDto).ToList();
    }
}
```

### EF Core直接使用

业务模块也可以直接注入AppDbContext使用EF Core进行数据操作，无需通过Repository层。

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

> 📌 **分层架构清晰，适合小型诊所部署**
> 🎆 **生产就绪**: 编译通过，提供缓存、数据库基础设施，可投入生产使用



## 📦 项目结构

```
LYBT.Infrastructure/
├── Authorization/           # 授权策略扩展
├── Cache/                   # 查询缓存接口
├── Caching/                 # 缓存服务实现
│   ├── Adapters/            # 缓存适配器（MemoryCache、Null）
│   ├── Interfaces/          # 缓存接口定义
│   └── Models/              # 缓存模型（优先级、统计）
├── Configuration/           # 配置管理
│   ├── Extensions/          # 配置扩展方法
│   ├── Options/             # 配置选项类（JWT、Database等）
│   └── Validation/          # 生产环境配置验证
├── Data/                    # 数据访问核心
│   ├── Configuration/       # 实体优化扩展
│   ├── Configurations/      # EF Core实体配置（14个）
│   ├── Interceptors/        # 查询性能拦截器
│   ├── Migrations/          # 数据库迁移
│   └── Monitoring/          # 查询统计收集器
├── DependencyInjection/     # 服务注册扩展
├── Interfaces/              # 接口定义（Repository等）
├── Logging/                 # 统一日志服务接口
├── Mapping/                 # AutoMapper配置
├── Migrations/              # 根级迁移（InitialCreateV2等）
├── Repositories/            # 通用仓储实现
├── Security/                # 安全服务（ASP.NET Core DataProtection）
├── Specifications/          # 规约模式实现
├── Utilities/               # 工具类（日志脱敏等）
└── Web/                     # Web API基类（3个Controller基类）
```

## 🛠 技术栈

- **.NET 8 / ASP.NET Core 8**: 目标框架
- **Entity Framework Core 8**: ORM框架，用于数据访问和数据库迁移
- **Microsoft.Extensions**: 用于依赖注入、配置、缓存等核心功能
- **JWT (JSON Web Tokens)**: 用于API的安全认证
- **AutoMapper**: 对象映射框架
- **IMemoryCache**: 进程内缓存（适合小型部署）

## 🚀 快速开始

此项目是一个类库，不包含可执行文件，但包含了数据库迁移的核心逻辑。可以通过解决方案或以下命令进行构建：

```bash
# 还原解决方案依赖
dotnet restore LYBT.All.sln

# 构建此项目
dotnet build src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj
```

关于数据库迁移操作，请参考本文档的 `🗃️ 数据库迁移管理` 章节。

## 🔌 API 接口

此项目为基础设施层，不直接对外提供任何API接口。它为上层服务（如 `LYBT.WebAPI`）提供数据库访问、安全、缓存等底层能力。
