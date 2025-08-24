# LYBT.Infrastructure 功能说明文档

## 模块概述
基础设施模块为整个系统提供统一的底层服务，包括身份认证、数据缓存、配置管理、日志记录、文件存储等核心功能。本模块采用依赖注入模式，为所有业务模块提供标准化的基础服务。

## 核心服务

### 1. 统一认证服务 (Authentication)

#### JwtAuthenticationService
**文件位置**: `Authentication/JwtAuthenticationService.cs`

**功能**: JWT令牌生成、验证和解析
- **GenerateTokenAsync**: 生成JWT访问令牌
- **GenerateRefreshTokenAsync**: 生成刷新令牌
- **ValidateTokenAsync**: 验证令牌有效性
- **GetUserInfoFromTokenAsync**: 从令牌解析用户信息
- **RevokeTokenAsync**: 撤销令牌

**使用场景**: 用户登录后的身份验证和授权

#### AuthorizationService
**文件位置**: `Authentication/AuthorizationService.cs`

**功能**: 基于角色和权限的授权服务
- **CheckPermissionAsync**: 检查用户权限
- **HasRoleAsync**: 验证用户角色
- **GetUserRolesAsync**: 获取用户角色列表
- **IsAuthorizedAsync**: 综合授权检查

**使用场景**: API接口的权限控制和业务操作授权

### 2. 统一缓存服务 (Caching)

#### MemoryCacheService & DistributedCacheService
**文件位置**: `Caching/MemoryCacheService.cs`, `Caching/DistributedCacheService.cs`

**功能**: 提供内存缓存和分布式缓存服务
```csharp
// 基础缓存操作
Task<T?> GetAsync<T>(string key)
Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
Task RemoveAsync(string key)
Task<bool> ExistsAsync(string key)

// 高级缓存操作
Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null)
Task RemoveByPatternAsync(string pattern)
```

**使用场景**: 
- 用户会话缓存
- 配置信息缓存
- 查询结果缓存
- 临时数据存储

#### CacheKeyGenerator
**文件位置**: `Caching/CacheKeyGenerator.cs`

**功能**: 标准化缓存键生成
- **GenerateKey**: 生成标准缓存键
- **GenerateUserKey**: 生成用户相关缓存键
- **GenerateEntityKey**: 生成实体相关缓存键

### 3. 统一配置管理 (Configuration)

#### UnifiedConfigService
**文件位置**: `Configuration/UnifiedConfigService.cs`

**功能**: 系统配置的统一管理
```csharp
// 全局设置管理
Task<GlobalSettingsDto> GetGlobalSettingsAsync()
Task<bool> UpdateGlobalSettingsAsync(GlobalSettingsDto settings)
Task InitializeDefaultGlobalSettingsAsync()

// 诊断目录管理
Task<List<DiagnosisCatalogDto>> GetDiagnosisCatalogsAsync()
Task<bool> CreateDiagnosisCatalogAsync(DiagnosisCatalogDto catalog)

// 治疗目录管理
Task<List<TreatmentCatalogDto>> GetTreatmentCatalogsAsync()
Task<bool> CreateTreatmentCatalogAsync(TreatmentCatalogDto catalog)

// 设置项管理
Task<List<SettingsDto>> GetSettingsAsync(string category)
Task<SettingsDto?> GetSettingAsync(string key)
Task<bool> SetSettingAsync(string key, string value, string category, string description)
```

**使用场景**: 
- 系统参数配置
- 业务规则配置
- 诊疗目录维护
- 动态配置更新

#### 配置模型

##### GlobalSettingsModel
```csharp
- Id: 配置ID
- ClinicName: 诊所名称
- ClinicAddress: 诊所地址
- ClinicPhone: 诊所电话
- BusinessHours: 营业时间
- MaxPatientsPerDay: 每日最大接诊人数
- EnableSpecialPatients: 是否启用特殊患者功能
- RequireRealNameRegistration: 是否要求实名挂号
- AutoGenerateRecords: 是否自动生成病历
- DefaultPrescriptionDays: 默认处方天数
- CreatedAt/UpdatedAt: 时间戳
```

### 4. 统一日志服务 (Logging)

#### UnifiedLogService
**文件位置**: `Logging/UnifiedLogService.cs`

**功能**: 统一的日志记录和管理
```csharp
// 系统日志
Task LogInfoAsync(string source, string message, object? data, string? correlationId)
Task LogWarningAsync(string source, string message, object? data, string? correlationId)
Task LogErrorAsync(string source, string message, Exception? exception, string? correlationId)

// 用户操作日志
Task LogUserActionAsync(Guid userId, string userName, LogActionType actionType, 
    string module, string action, string content, string? parameters = null)

// 审计日志
Task CreateAuditLogAsync(Guid operatorId, string operatorName, string entityType, 
    Guid entityId, string action, string? oldValue, string? newValue)

// 性能日志
Task LogPerformanceAsync(string operation, long duration, string? details)

// 日志查询
Task<PagedResultDto<SystemLogDto>> GetSystemLogsAsync(LogQueryDto query)
Task<PagedResultDto<UserActionLogDto>> GetUserActionLogsAsync(LogQueryDto query)
```

**日志类型**:
- **SystemLog**: 系统运行日志
- **UserActionLog**: 用户操作日志
- **AuditLog**: 审计跟踪日志
- **PerformanceLog**: 性能监控日志
- **ErrorLog**: 错误异常日志

**使用场景**: 
- 系统监控和故障排查
- 用户行为审计
- 性能分析和优化
- 合规要求追踪

### 5. 文件存储服务 (Storage)

#### LocalFileStorageService
**文件位置**: `Storage/LocalFileStorageService.cs`

**功能**: 本地文件存储管理
```csharp
// 文件操作
Task<string> SaveFileAsync(Stream fileStream, string fileName, string? subPath = null)
Task<Stream> GetFileAsync(string filePath)
Task<bool> DeleteFileAsync(string filePath)
Task<bool> FileExistsAsync(string filePath)

// 目录操作
Task<bool> CreateDirectoryAsync(string path)
Task<bool> DeleteDirectoryAsync(string path)
Task<List<string>> GetFilesAsync(string path, string? searchPattern = null)

// 文件信息
Task<long> GetFileSizeAsync(string filePath)
Task<DateTime> GetFileLastModifiedAsync(string filePath)
```

**存储策略**:
- 按日期分目录存储
- 自动生成唯一文件名
- 支持子目录分类
- 文件类型验证

**使用场景**: 
- 患者照片存储
- 处方单据存储
- 系统配置备份
- 导入导出文件

### 6. 数据库上下文 (Data)

#### InfrastructureDbContext
**文件位置**: `Data/InfrastructureDbContext.cs`

**功能**: 基础设施数据库上下文
- **Logs**: 日志实体集合
- **Settings**: 配置实体集合
- **GlobalSettings**: 全局设置实体集合
- **DiagnosisCatalogs**: 诊断目录实体集合
- **TreatmentCatalogs**: 治疗目录实体集合

**特性**:
- 自动审计字段
- 软删除支持
- 并发控制
- 索引优化

#### AppDbContext
**文件位置**: `Data/AppDbContext.cs`

**功能**: 应用主数据库上下文
- 聚合所有业务模块的DbSet
- 统一数据库配置
- 事务管理
- 连接字符串管理

## 扩展方法

### ServiceCollectionExtensions
**文件位置**: `Extensions/ServiceCollectionExtensions.cs`

**功能**: 依赖注入服务注册
```csharp
// 基础设施服务注册
services.AddInfrastructure(configuration)
    .AddAuthentication()
    .AddCaching()
    .AddConfiguration()
    .AddLogging()
    .AddStorage();

// 模块服务注册
services.AddLybtModules()
    .AddUsersModule(connectionString)
    .AddPatientsModule(connectionString)
    .AddDoctorsModule(connectionString);

// AutoMapper配置
services.AddLybtAutoMapperProfiles();
```

## 配置选项

### AuthOptions
```csharp
- JwtSecretKey: JWT密钥
- TokenExpirationMinutes: 令牌有效期（分钟）
- RefreshTokenExpirationDays: 刷新令牌有效期（天）
- RequireHttpsMetadata: 是否要求HTTPS
- ValidateIssuer: 是否验证发行者
- ValidateAudience: 是否验证受众
```

### CacheOptions
```csharp
- DefaultExpirationMinutes: 默认过期时间（分钟）
- SlidingExpirationMinutes: 滑动过期时间（分钟）
- MaxMemorySizeMB: 最大内存大小（MB）
- CompactionPercentage: 压缩百分比
- RedisConnectionString: Redis连接字符串
```

### StorageOptions
```csharp
- BasePath: 基础存储路径
- MaxFileSizeMB: 最大文件大小（MB）
- AllowedExtensions: 允许的文件扩展名
- UseSubDirectories: 是否使用子目录
- CleanupIntervalHours: 清理间隔（小时）
```

### JwtOptions
```csharp
- SecretKey: JWT密钥
- Issuer: 发行者
- Audience: 受众
- ExpirationMinutes: 过期时间（分钟）
- ClockSkewMinutes: 时钟偏差（分钟）
```

## 使用示例

### 依赖注入注册
```csharp
// Program.cs
builder.Services.AddInfrastructure(builder.Configuration);
```

### 缓存使用
```csharp
// 注入缓存服务
private readonly IMemoryCache _cache;

// 缓存用户信息
await _cache.SetAsync($"user:{userId}", userDto, TimeSpan.FromHours(1));

// 获取缓存
var user = await _cache.GetAsync<UserDto>($"user:{userId}");

// 缓存或获取
var settings = await _cache.GetOrSetAsync(
    "global_settings",
    () => _configService.GetGlobalSettingsAsync(),
    TimeSpan.FromMinutes(30)
);
```

### 配置管理
```csharp
// 注入配置服务
private readonly IUnifiedConfigService _configService;

// 获取全局设置
var settings = await _configService.GetGlobalSettingsAsync();

// 更新配置
await _configService.SetSettingAsync(
    "max_patients_per_day", 
    "100", 
    "Business", 
    "每日最大接诊人数"
);

// 获取诊断目录
var diagnoses = await _configService.GetDiagnosisCatalogsAsync();
```

### 日志记录
```csharp
// 注入日志服务
private readonly IUnifiedLogService _logService;

// 记录用户操作
await _logService.LogUserActionAsync(
    userId, userName, LogActionType.Create,
    "Patients", "CreatePatient", 
    "创建患者：张三",
    JsonSerializer.Serialize(patientDto)
);

// 记录系统日志
await _logService.LogInfoAsync(
    "PatientService", 
    "患者创建成功", 
    new { PatientId = patient.Id, Name = patient.Name },
    correlationId
);

// 记录性能日志
await _logService.LogPerformanceAsync(
    "PatientSearch", 
    stopwatch.ElapsedMilliseconds,
    $"关键词：{keyword}，结果数：{results.Count}"
);
```

### 文件存储
```csharp
// 注入存储服务
private readonly IFileStorageService _storage;

// 保存文件
var filePath = await _storage.SaveFileAsync(
    fileStream, 
    "patient_photo.jpg", 
    "patients/photos"
);

// 获取文件
var fileStream = await _storage.GetFileAsync(filePath);

// 检查文件存在
var exists = await _storage.FileExistsAsync(filePath);
```

### JWT认证
```csharp
// 注入认证服务
private readonly IJwtAuthenticationService _jwtService;

// 生成令牌
var token = await _jwtService.GenerateTokenAsync(user);

// 验证令牌
var isValid = await _jwtService.ValidateTokenAsync(token);

// 解析用户信息
var userInfo = await _jwtService.GetUserInfoFromTokenAsync(token);
```

## 数据库迁移

### 基础设施迁移
```bash
# 添加迁移
dotnet ef migrations add InitialInfrastructure --project LYBT.Infrastructure --startup-project LYBT.WebAPI

# 更新数据库
dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI
```

## 性能优化

### 缓存策略
- **热点数据**: 用户信息、系统配置缓存1小时
- **查询结果**: 分页查询结果缓存15分钟
- **计算结果**: 复杂计算结果缓存30分钟
- **静态数据**: 枚举、字典数据缓存24小时

### 日志优化
- **异步写入**: 所有日志操作异步执行
- **批量写入**: 高并发时批量写入数据库
- **分级存储**: 不同级别日志分别存储
- **定期清理**: 自动清理过期日志

### 数据库优化
- **索引优化**: 为常用查询字段建立索引
- **连接池**: 配置合理的连接池大小
- **读写分离**: 支持读写分离配置
- **分库分表**: 大数据量时支持分库分表

## 安全考虑

### 身份认证
- JWT令牌加密存储
- 令牌定期轮换
- 防止重放攻击
- 会话超时管理

### 数据安全
- 敏感数据加密存储
- 访问日志记录
- 权限最小化原则
- 数据脱敏处理

### 文件安全
- 文件类型验证
- 大小限制控制
- 路径遍历防护
- 病毒扫描集成

## 监控和诊断

### 健康检查
- 数据库连接检查
- 缓存服务检查
- 文件系统检查
- 外部服务检查

### 性能监控
- 请求响应时间
- 数据库查询时间
- 缓存命中率
- 内存使用情况

### 错误处理
- 全局异常捕获
- 错误信息标准化
- 错误通知机制
- 故障自动恢复

## 扩展建议

### 功能扩展
1. **分布式缓存**: 集成Redis或其他分布式缓存
2. **消息队列**: 集成RabbitMQ或其他消息中间件
3. **云存储**: 支持云存储服务（阿里云OSS、AWS S3等）
4. **日志中心**: 集成ELK或其他日志分析平台
5. **配置中心**: 集成Nacos或其他配置中心

### 技术优化
1. **异步编程**: 全面采用async/await模式
2. **内存优化**: 使用对象池、内存映射等技术
3. **并发优化**: 使用并发集合、无锁算法
4. **序列化优化**: 使用更高效的序列化库
5. **压缩算法**: 集成数据压缩算法

这个基础设施模块为整个中医诊所管理系统提供了稳定、高效的底层支撑，确保了系统的可扩展性、可维护性和安全性。