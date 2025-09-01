# LYBT.Infrastructure 项目文档

## 📋 项目概述

**LYBT.Infrastructure**是凌隐宝堂中医诊所系统的**企业级基础设施平台**，提供完整的安全防护、配置管理、数据访问、文件存储、API框架等基础服务。作为整个系统的基础支撑层，Infrastructure包含11个专业子系统，为上层8个业务模块提供统一的基础设施服务。

### 🎯 项目定位
- **企业级基础设施平台**: 不仅仅是数据访问层，而是完整的基础设施支撑体系
- **安全防护中心**: 提供加密、JWT增强、输入验证等企业级安全服务
- **配置管理中心**: 统一的配置、环境变量、机密管理
- **API开发框架**: 完整的API控制器基础架构和响应标准化
- **文件存储平台**: 智能文件管理和存储服务

### 🏗️ 11个核心子系统

#### 🔧 Configuration子系统 - 企业级配置管理
- **ConfigurationManager** - 统一配置管理，环境变量替换，配置验证
- **EnvironmentManager** - 环境管理服务
- **SecretManager** - 机密配置管理
- **7个专业配置类** - JwtOptions、SecurityOptions、CacheOptions等

#### 🛡️ Security子系统 - 企业级安全防护
- **EncryptionService** - AES-256加密、SHA-256哈希、HMAC-SHA256签名
- **EnhancedJwtService** - 增强JWT服务，令牌生成/验证/刷新/撤销
- **InputValidationService** - 全面输入验证，防SQL注入/XSS/路径遍历等攻击
- **SecurityMiddleware** - 安全中间件

#### 💾 Storage子系统 - 完整文件管理平台
- **LocalFileStorageService** - 智能文件存储，按日期分层，元数据管理
- **FileMetadata** - 完整文件元数据模型

#### 🌐 Web子系统 - 完整API框架体系
- **BaseControllerCore** - 控制器核心基类，用户身份管理、统一日志
- **BaseApiController** - API控制器框架，统一响应包装、异常处理
- **BaseSystemController** - 系统管理控制器基类

#### 📚 其他专业子系统
- **Data** - AppDbContext、仓储模式实现
- **Repositories** - 仓储模式，包含优化版本
- **Database** - 数据库初始化、迁移管理
- **Caching** - 缓存管理系统
- **Logging** - 日志服务
- **Specifications** - 规约模式实现
- **Services** - 基础服务类

### 🎯 在系统中的位置
Infrastructure作为整个系统的**基础设施层**，被所有8个业务模块依赖：
- **向上提供**: 统一的数据访问、安全服务、配置管理、文件存储等基础设施
- **横向协作**: 与Shared项目协作，提供完整的技术支撑体系
- **向下管理**: 数据库、文件系统、配置文件等底层资源

## 🏗️ 技术架构

### 企业级架构设计
Infrastructure采用模块化架构设计，11个子系统协同工作：

```
┌─────────────────────────────────────────────────────────────┐
│                    业务模块层 (8个模块)                        │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure基础设施层                     │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┐  │
│  │Configuration│  Security   │   Storage   │     Web     │  │
│  │   子系统     │   子系统     │   子系统     │   子系统     │  │
│  ├─────────────┼─────────────┼─────────────┼─────────────┤  │
│  │    Data     │ Repositories│  Database   │   Caching   │  │
│  │   子系统     │   子系统     │   子系统     │   子系统     │  │
│  ├─────────────┼─────────────┼─────────────┼─────────────┤  │
│  │  Logging    │    Services │Specifications│ Extensions  │  │
│  │   子系统     │   子系统     │   子系统     │   子系统     │  │
│  └─────────────┴─────────────┴─────────────┴─────────────┘  │
├─────────────────────────────────────────────────────────────┤
│                底层资源层 (数据库/文件系统/配置)                │
└─────────────────────────────────────────────────────────────┘
```

### 核心技术栈
- **.NET 8.0**: 最新LTS版本，性能和安全性优化
- **Entity Framework Core 8.0.17**: ORM框架，支持LINQ查询和迁移
- **AES-256加密**: 企业级数据加密标准，CBC模式，PKCS7填充
- **SHA-256/HMAC-SHA256**: 哈希算法和数字签名
- **System.Security.Cryptography**: .NET安全加密API
- **Microsoft.Extensions.Caching.Memory**: 内存缓存优化
- **System.Text.Json**: 高性能JSON序列化
- **Microsoft.Extensions.Logging**: 结构化日志记录
- **System.Text.RegularExpressions**: 安全输入验证模式匹配

### 依赖项目
**直接依赖**:
- `LYBT.Shared.Models` - 数据传输对象和实体模型
- `LYBT.Shared.Interfaces` - 服务接口和API契约定义
- `LYBT.Shared.Utilities` - 通用工具类和扩展方法

**被依赖项目** (8个业务模块):
- `LYBT.Module.Auth`, `LYBT.Module.Users`, `LYBT.Module.Patients`
- `LYBT.Module.MedicalCase`, `LYBT.Module.Consultation`
- `LYBT.Module.Prescriptions`, `LYBT.Module.Herbs`, `LYBT.Module.Formula`

## 🎯 核心功能详述

### 🔧 Configuration子系统

#### ConfigurationManager.cs - 统一配置管理核心
```csharp
public class ConfigurationManager : IConfigurationManager
{
    // 环境变量替换：支持${VAR_NAME}格式
    public T GetSection<T>(string sectionName) where T : class, new()
    
    // 连接字符串管理和环境特定处理
    public string GetConnectionString(string name = "DefaultConnection")
    
    // 配置验证：JWT、Auth、Security全面验证
    public ValidationResult ValidateConfiguration()
    
    // 环境变量字符串替换处理
    private string ProcessEnvironmentVariableString(string value)
}
```

**主要功能**:
- ✅ **环境变量替换**: 支持`${VAR_NAME}`格式的环境变量占位符
- ✅ **配置验证**: 全面验证JWT、Auth、Security等关键配置
- ✅ **连接字符串管理**: 支持多环境连接字符串配置
- ✅ **生产环境保护**: 生产环境环境变量缺失时抛出异常

#### 配置选项体系 (7个专业配置类)
- **CacheOptions.cs** - 缓存配置选项
- **DatabaseOptions.cs** - 数据库配置选项
- **JwtOptions.cs** - JWT认证配置选项
- **PasswordOptions.cs** - 密码策略配置选项
- **SecurityOptions.cs** - 安全配置选项
- **SysAdminOptions.cs** - 系统管理员配置选项
- **StorageOptions.cs** - 存储配置选项

### 🛡️ Security子系统

#### EncryptionService.cs - 专业数据加密服务
```csharp
public class EncryptionService : IEncryptionService
{
    // AES-256加密/解密 (CBC模式, PKCS7填充)
    public string Encrypt(string plainText)
    public string Decrypt(string cipherText)
    
    // SHA-256哈希计算
    public string Hash(string input)
    
    // HMAC-SHA256数字签名和验证
    public string Sign(string data, string key)
    public bool VerifySignature(string data, string signature, string key)
    
    // 安全密钥生成 (256位密码学安全)
    public string GenerateSecureKey()
    
    // 连接字符串密码字段专门加密
    public string EncryptConnectionString(string connectionString)
    public string DecryptConnectionString(string encryptedConnectionString)
}
```

**主要功能**:
- ✅ **AES-256加密**: 使用CBC模式和PKCS7填充的工业级加密
- ✅ **数字签名**: HMAC-SHA256签名，支持数据完整性验证
- ✅ **连接字符串保护**: 专门针对数据库连接字符串的密码加密
- ✅ **密码学安全**: 使用.NET密码学安全随机数生成器

#### EnhancedJwtService.cs - 增强JWT管理服务
```csharp
public interface IEnhancedJwtService
{
    // 访问令牌生成/验证/刷新/撤销
    Task<TokenResult> GenerateAccessTokenAsync(TokenRequest request);
    Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string? clientIP = null);
    Task<TokenResult> RefreshAccessTokenAsync(string refreshToken, string? clientIP = null);
    
    // 令牌撤销 (单个或批量)
    Task RevokeAccessTokenAsync(string tokenId, string reason = "用户注销");
    Task RevokeAllUserTokensAsync(Guid userId, string reason = "安全操作");
}
```

**增强功能**:
- ✅ **设备追踪**: 支持DeviceId和SessionId追踪
- ✅ **IP验证**: 客户端IP地址验证防止令牌盗用
- ✅ **令牌撤销**: 支持单个令牌或用户所有令牌撤销
- ✅ **短期/长期策略**: 8小时/30天令牌过期策略
- ✅ **安全审计**: 详细的令牌操作日志记录

#### InputValidationService.cs - 全面输入验证防护
```csharp
public class InputValidationService : IInputValidationService
{
    // 4类攻击检测
    public bool IsSqlInjection(string input)     // SQL注入检测
    public bool IsXssAttack(string input)       // XSS攻击检测
    public bool IsPathTraversal(string input)   // 路径遍历检测
    public bool IsCommandInjection(string input) // 命令注入检测
    
    // 7种输入类型验证
    public ValidationResult ValidateAndSanitize(string input, InputType inputType)
    // InputType: General, HTML, SQL, FileName, URL, Email, JSON
}
```

**防护能力**:
- ✅ **26个攻击模式**: 预编译正则表达式，高性能模式匹配
- ✅ **威胁分类**: SQL注入、XSS、路径遍历、命令注入四大类
- ✅ **输入净化**: HTML编码、白名单过滤、安全字符处理
- ✅ **详细结果**: 威胁类型、错误信息、净化后的值

### 💾 Storage子系统

#### LocalFileStorageService.cs - 企业级文件存储服务
```csharp
public class LocalFileStorageService : IFileStorageService
{
    // 智能文件路径管理 (按日期分层存储)
    public async Task<string> UploadAsync(string fileName, Stream stream, string? contentType = null)
    
    // 完整文件操作
    public async Task<Stream?> DownloadAsync(string filePath)
    public async Task<bool> DeleteAsync(string filePath)
    public async Task<bool> CopyAsync(string sourceFilePath, string destinationFilePath)
    public async Task<bool> MoveAsync(string sourceFilePath, string destinationFilePath)
    
    // 元数据和目录操作
    public async Task<FileMetadata?> GetMetadataAsync(string filePath)
    public async Task<IEnumerable<FileMetadata>> ListFilesAsync(string directoryPath, string searchPattern = "*")
}
```

**核心特性**:
- ✅ **智能路径管理**: 自动按`年/月/日/文件名_随机ID.扩展名`分层存储
- ✅ **文件安全化**: 自动清理文件名中的非法字符，防止路径注入
- ✅ **完整元数据**: 文件大小、MIME类型、创建/修改时间、MD5哈希
- ✅ **MIME类型识别**: 自动识别9种常见文件类型
- ✅ **云存储预留**: 预留GenerateAccessUrlAsync接口支持云存储扩展

### 🌐 Web子系统 - 完整API框架体系

#### BaseControllerCore.cs - 控制器核心基类
```csharp
public abstract class BaseControllerCore : ControllerBase
{
    // 统一用户身份管理
    protected (Guid operatorId, string operatorName, string operatorRole) GetOperator()
    
    // 统一操作日志记录
    protected void LogOperation(string operation, object? data = null, Guid? targetId = null)
    
    // 核心异常处理
    protected void HandleExceptionCore(Exception ex, string operation, object? context = null)
    
    // 基础验证方法
    protected bool IsValidGuid(Guid id)
    protected List<string> GetModelErrors()
}
```

#### BaseApiController.cs - API控制器专业基类
```csharp
public abstract class BaseApiController : BaseControllerCore
{
    // 统一API响应包装 (10种响应类型)
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")
    protected ActionResult<ApiResponse<T>> BusinessFail<T>(string message, string? errorCode = null)
    protected ActionResult<ApiResponse> ValidationFail(string message = "参数验证失败")
    protected ActionResult<ApiResponse> Unauthorized(string message = "未授权访问")
    
    // ServiceResult自动处理
    protected ActionResult<ApiResponse<T>> HandleServiceResult<T>(ServiceResult<T> serviceResult, string? successMessage = null)
    
    // 分页响应专门处理
    protected ActionResult<ApiResponse<PagedResult<T>>> Success<T>(PagedResult<T> pagedResult, string message = "查询成功")
    
    // 统一异常处理
    protected ActionResult<ApiResponse<T>> HandleException<T>(Exception ex, string operation, object? context = null)
}
```

**API框架特性**:
- ✅ **统一响应格式**: 所有API返回统一的ApiResponse<T>格式
- ✅ **ServiceResult自动解包**: 自动将Service层结果转换为API响应
- ✅ **分页支持**: 完整的分页响应处理体系
- ✅ **异常分类**: 根据异常类型自动返回合适的HTTP状态码
- ✅ **链路追踪**: 自动为所有响应添加RequestId

## 📁 实际代码结构

基于实际代码分析的完整目录结构：

```
src/Server/Core/LYBT.Infrastructure/
├── Configuration/                          # 配置管理子系统
│   ├── ConfigurationManager.cs            # 统一配置管理核心
│   ├── IConfigurationManager.cs           # 配置管理接口
│   ├── EnvironmentManager.cs              # 环境管理服务
│   ├── EnvironmentVariableReplacer.cs     # 环境变量替换器
│   ├── SecretManager.cs                   # 机密配置管理
│   ├── GlobalSettingsModel.cs             # 全局配置模型
│   ├── SettingsModel.cs                   # 本地配置模型
│   ├── DiagnosisCatalogModel.cs          # 诊断目录配置
│   ├── IUnifiedConfigService.cs          # 统一配置服务接口
│   ├── Dtos/                             # 配置DTO
│   │   ├── EnumMappingDto.cs             # 枚举映射DTO
│   │   ├── SettingsCreateDto.cs          # 配置创建DTO
│   │   └── SettingsEditDto.cs            # 配置编辑DTO
│   └── Options/                          # 配置选项类
│       ├── CacheOptions.cs              # 缓存配置
│       ├── DatabaseOptions.cs           # 数据库配置
│       ├── JwtOptions.cs                # JWT配置
│       ├── PasswordOptions.cs           # 密码策略配置
│       ├── SecurityOptions.cs           # 安全配置
│       └── SysAdminOptions.cs          # 系统管理员配置
├── Security/                            # 安全防护子系统
│   ├── EncryptionService.cs            # AES-256加密服务
│   ├── IEncryptionService.cs           # 加密服务接口
│   ├── EnhancedJwtService.cs           # 增强JWT服务
│   ├── IEnhancedJwtService.cs          # 增强JWT接口
│   ├── InputValidationService.cs       # 输入验证服务
│   ├── IInputValidationService.cs      # 输入验证接口
│   ├── SecurityConfigurationService.cs # 安全配置服务
│   ├── ISecurityConfigurationService.cs# 安全配置接口
│   └── SecurityMiddleware.cs           # 安全中间件
├── Storage/                            # 文件存储子系统
│   ├── IFileStorageService.cs         # 文件存储接口
│   └── LocalFileStorageService.cs     # 本地文件存储实现
├── Web/                               # Web API框架子系统
│   ├── BaseControllerCore.cs         # 控制器核心基类
│   ├── BaseApiController.cs          # API控制器基类
│   ├── BaseSystemController.cs       # 系统控制器基类
│   └── ApiErrorCodes.cs             # API错误代码定义
├── Data/                             # 数据访问子系统
│   ├── AppDbContext.cs              # 主数据上下文
│   └── AppDbContextFactory.cs       # 设计时工厂
├── Repositories/                     # 仓储模式子系统
│   ├── BaseRepository.cs           # 基础仓储实现
│   ├── UserRepository.cs           # 用户仓储示例
│   ├── Base/                       # 仓储基础模式
│   │   ├── IRepository.cs          # 仓储接口定义
│   │   └── RepositoryBase.cs       # 仓储基类实现
│   └── Optimized/                  # 性能优化仓储
│       └── OptimizedBaseRepository.cs # 优化版仓储
├── Database/                        # 数据库管理子系统
│   ├── DatabaseInitializationService.cs # 数据库初始化
│   ├── Extensions/                  # 数据库扩展
│   └── Migrations/                 # 迁移文件
│       └── AddPerformanceIndexes_20250811.cs # 性能索引迁移
├── Caching/                        # 缓存管理子系统
├── Logging/                        # 日志服务子系统
│   └── SimpleLog.cs               # 简单日志服务
├── Services/                       # 基础服务子系统
│   └── BaseService.cs             # 服务基类
├── Specifications/                 # 规约模式子系统
│   └── Specification.cs          # 规约模式实现
├── Extensions/                     # 扩展方法子系统
│   └── ServiceCollectionExtensions.cs # 服务注册扩展
├── Interfaces/                     # 基础接口子系统
│   ├── IBaseRepository.cs         # 基础仓储接口
│   ├── IBaseService.cs            # 基础服务接口
│   └── IModule.cs                # 模块接口
├── Options/                        # 全局选项子系统
│   ├── AuthOptions.cs            # 认证选项
│   └── StorageOptions.cs         # 存储选项
└── Migrations/                     # EF Core迁移文件
    ├── 20250802002435_InitialCreate.cs # 初始迁移
    ├── 20250810112700_Auth_UltraThink_Refactor.cs # 认证重构迁移
    └── AppDbContextModelSnapshot.cs # 模型快照
```

## 🎯 接口规范定义

### Security子系统接口

#### IEncryptionService - 加密服务接口
```csharp
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string Hash(string input);
    string Sign(string data, string key);
    bool VerifySignature(string data, string signature, string key);
    string GenerateSecureKey();
    string EncryptConnectionString(string connectionString);
    string DecryptConnectionString(string encryptedConnectionString);
}
```

#### IEnhancedJwtService - 增强JWT服务接口
```csharp
public interface IEnhancedJwtService
{
    Task<TokenResult> GenerateAccessTokenAsync(TokenRequest request);
    Task<TokenValidationResult> ValidateAccessTokenAsync(string token, string? clientIP = null);
    Task<TokenResult> RefreshAccessTokenAsync(string refreshToken, string? clientIP = null);
    Task RevokeAccessTokenAsync(string tokenId, string reason = "用户注销");
    Task RevokeAllUserTokensAsync(Guid userId, string reason = "安全操作");
}
```

#### IInputValidationService - 输入验证服务接口
```csharp
public interface IInputValidationService
{
    ValidationResult ValidateAndSanitize(string input, InputType inputType);
    bool IsSqlInjection(string input);
    bool IsXssAttack(string input);
    bool IsPathTraversal(string input);
    bool IsCommandInjection(string input);
    string HtmlEncode(string input);
    string HtmlDecode(string input);
    string UrlEncode(string input);
}
```

### Storage子系统接口

#### IFileStorageService - 文件存储服务接口
```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(string fileName, Stream stream, string? contentType = null);
    Task<Stream?> DownloadAsync(string filePath);
    Task<bool> DeleteAsync(string filePath);
    Task<bool> ExistsAsync(string filePath);
    Task<FileMetadata?> GetMetadataAsync(string filePath);
    Task<IEnumerable<FileMetadata>> ListFilesAsync(string directoryPath, string searchPattern = "*");
    Task<bool> CopyAsync(string sourceFilePath, string destinationFilePath);
    Task<bool> MoveAsync(string sourceFilePath, string destinationFilePath);
    Task<string?> GenerateAccessUrlAsync(string filePath, TimeSpan? expiry = null);
}
```

### Configuration子系统接口

#### IConfigurationManager - 配置管理接口
```csharp
public interface IConfigurationManager
{
    bool IsDevelopment { get; }
    bool IsProduction { get; }
    string Environment { get; }
    T GetSection<T>(string sectionName) where T : class, new();
    string GetConnectionString(string name = "DefaultConnection");
    string GetValue(string key, string defaultValue = "");
    ValidationResult ValidateConfiguration();
}
```

## ⚙️ 配置管理

### 完整配置项定义

#### appsettings.json 配置示例
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DATABASE_CONNECTION_STRING}"
  },
  "JwtOptions": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "ShortTermExpiryMinutes": 480,
    "LongTermExpiryMinutes": 43200,
    "ValidateClientIP": true,
    "RefreshTokenExpiryDays": 90
  },
  "Security": {
    "EncryptionKey": "${ENCRYPTION_KEY}",
    "InitializationVector": "${ENCRYPTION_IV}",
    "Https": {
      "RequireHttps": true
    },
    "Environment": {
      "HideServerInfo": true
    }
  },
  "AuthOptions": {
    "MaxFailedLoginAttempts": 5,
    "LockoutMinutes": 30
  },
  "StorageOptions": {
    "LocalStorage": {
      "RootPath": "./uploads",
      "MaxFileSize": 10485760,
      "AllowedExtensions": [".jpg", ".png", ".pdf", ".docx"]
    }
  },
  "InputValidationOptions": {
    "MaxInputLength": 10000,
    "AllowHtmlContent": false,
    "AllowedUrlSchemes": ["http", "https"],
    "EnableLogging": true,
    "StrictMode": true
  }
}
```

### 环境变量支持
Infrastructure支持${VAR_NAME}格式的环境变量替换：
- **${DATABASE_CONNECTION_STRING}** - 数据库连接字符串
- **${JWT_SECRET_KEY}** - JWT签名密钥（最少256位）
- **${ENCRYPTION_KEY}** - AES加密密钥（Base64编码）
- **${ENCRYPTION_IV}** - AES初始化向量（Base64编码）

## 🚀 使用示例

### Security子系统使用示例

#### 数据加密示例
```csharp
public class PatientService
{
    private readonly IEncryptionService _encryptionService;
    
    public PatientService(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }
    
    public async Task<Patient> CreatePatientAsync(PatientCreateDto dto)
    {
        // 敏感信息加密存储
        var encryptedIdCard = _encryptionService.Encrypt(dto.IdCard);
        var encryptedPhone = _encryptionService.Encrypt(dto.Phone);
        
        var patient = new Patient
        {
            Name = dto.Name,
            IdCard = encryptedIdCard,
            Phone = encryptedPhone
        };
        
        return await _repository.CreateAsync(patient);
    }
}
```

#### 输入验证示例
```csharp
[ApiController]
public class PatientController : BaseApiController
{
    private readonly IInputValidationService _validationService;
    
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Patient>>> Create([FromBody] PatientCreateDto dto)
    {
        try
        {
            // 验证输入安全
            var nameValidation = _validationService.ValidateAndSanitize(dto.Name, InputType.General);
            if (!nameValidation.IsValid)
            {
                return ValidationFail($"姓名输入不合法: {string.Join(", ", nameValidation.Errors)}");
            }
            
            // 使用净化后的值
            dto.Name = nameValidation.SanitizedValue;
            
            var result = await _patientService.CreatePatientAsync(dto);
            return HandleServiceResult(result, "患者创建成功");
        }
        catch (Exception ex)
        {
            return HandleException<Patient>(ex, "创建患者", dto);
        }
    }
}
```

### Storage子系统使用示例

#### 文件上传示例
```csharp
[HttpPost("upload")]
public async Task<ActionResult<ApiResponse<string>>> UploadFile(IFormFile file)
{
    try
    {
        if (file == null || file.Length == 0)
            return ValidationFail("请选择文件");
        
        using var stream = file.OpenReadStream();
        var filePath = await _fileStorageService.UploadAsync(
            file.FileName, 
            stream, 
            file.ContentType);
        
        return Success(filePath, "文件上传成功");
    }
    catch (Exception ex)
    {
        return HandleException<string>(ex, "文件上传");
    }
}
```

### Configuration子系统使用示例

#### 配置读取示例
```csharp
public class ConfigurationService
{
    private readonly IConfigurationManager _configManager;
    
    public ConfigurationService(IConfigurationManager configManager)
    {
        _configManager = configManager;
    }
    
    public JwtOptions GetJwtOptions()
    {
        return _configManager.GetSection<JwtOptions>("JwtOptions");
    }
    
    public string GetDatabaseConnection()
    {
        return _configManager.GetConnectionString(); // 自动支持环境变量替换
    }
    
    public bool ValidateAllConfigurations()
    {
        var result = _configManager.ValidateConfiguration();
        return result == ValidationResult.Success;
    }
}
```

## 🧪 测试规范

### 单元测试要求
- **测试框架**: xUnit + Moq + FluentAssertions
- **测试覆盖**: 11个子系统的核心类全覆盖
- **加密测试**: EncryptionService加密/解密/签名验证
- **输入验证测试**: InputValidationService各种攻击模式检测
- **文件存储测试**: LocalFileStorageService文件操作和元数据管理

### 测试覆盖率目标
- **Security子系统**: >95%覆盖率（关键安全功能）
- **Configuration子系统**: >90%覆盖率
- **Storage子系统**: >85%覆盖率
- **Web子系统**: >80%覆盖率
- **其他子系统**: >75%覆盖率

## 📚 相关文档链接

### 项目文档
- [LYBT.Entities项目文档](./entities.md) - 实体模型定义
- [LYBT.WebAPI项目文档](./webapi.md) - Web API实现
- [共享模型文档](../../shared/shared-models.md) - 数据传输对象

### 技术规范
- [UltraThink双层架构规范](../../../ultrathink/ultrathink-comprehensive-refactoring-complete-20250831.md)
- [API响应标准规范](../../../architecture/ultrathink-api-response-standards-20250817.md)
- [控制器设计模式](../../../architecture/ultrathink-controller-design-patterns-20250817.md)

---

**文档版本**: v2.0  
**创建日期**: 2025-09-01  
**最后更新**: 2025-09-01  
**维护者**: UltraThink项目组  
**审核状态**: ✅ **基于实际代码重写完成** - 严格匹配实际功能实现