# 凌隐宝堂中医诊所管理系统 - 开发指南

> 版本：1.0.0  
> 更新时间：2025-01-02  
> 维护频率：月度更新

## 一、快速开始

### 1.1 环境准备

#### 必需环境
- **操作系统**: Windows 10/11 或 Windows Server 2019+
- **.NET SDK**: 8.0 或更高版本
- **Visual Studio**: 2022 (17.4+) 或 VS Code
- **SQL Server**: 2019 或更高版本
- **Git**: 2.30+
- **Node.js**: 18+ (前端构建工具)

#### 开发工具
```powershell
# 安装 .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# 安装 EF Core 工具
dotnet tool install --global dotnet-ef

# 安装代码格式化工具
dotnet tool install --global dotnet-format
```

### 1.2 项目克隆与初始化

```powershell
# 克隆代码库
git clone https://github.com/shouqitao/LYBTZYZS.git
cd LYBTZYZS

# 还原 NuGet 包
dotnet restore LYBT.All.sln

# 初始化数据库
cd src/Server/Services/LYBT.WebAPI
dotnet ef database update

# 构建项目
cd ../../../../
dotnet build LYBT.All.sln -c Release
```

### 1.3 运行项目

```powershell
# 启动 Web API（终端1）
cd src/Server/Services/LYBT.WebAPI
dotnet run --launch-profile https

# 启动桌面客户端（终端2）
cd src/Client/Desktop/LYBT.Desktop.Shell
dotnet run
```

## 二、项目结构

### 2.1 解决方案架构

```
LYBTZYZS/
├── src/
│   ├── Server/                     # 服务器端代码
│   │   ├── Core/                   # 核心层
│   │   │   ├── LYBT.Entities/      # 实体模型
│   │   │   └── LYBT.Infrastructure/# 基础设施
│   │   ├── Modules/                # 业务模块
│   │   │   ├── LYBT.Module.Auth/   # 认证模块
│   │   │   ├── LYBT.Module.Patients/# 患者模块
│   │   │   ├── LYBT.Module.MedicalCase/# 病历模块
│   │   │   ├── LYBT.Module.Consultation/# 诊疗模块
│   │   │   ├── LYBT.Module.Prescriptions/# 处方模块
│   │   │   ├── LYBT.Module.Herbs/  # 药材模块
│   │   │   ├── LYBT.Module.Formula/# 方剂模块
│   │   │   └── LYBT.Module.Users/  # 用户模块
│   │   └── Services/
│   │       └── LYBT.WebAPI/        # Web API服务
│   ├── Client/                     # 客户端代码
│   │   └── Desktop/                # WPF桌面客户端
│   │       ├── LYBT.Desktop.Shell/ # 主程序壳
│   │       ├── Core/               # 客户端核心
│   │       └── Modules/            # 客户端模块
│   └── Shared/                     # 共享代码
│       ├── LYBT.Shared.Models/     # DTO和契约
│       └── LYBT.Shared.Utilities/  # 工具类
├── tests/                          # 测试项目
├── docs/                           # 文档
└── scripts/                        # 脚本工具
```

### 2.2 模块职责

| 模块 | 职责 | 核心实体 | 依赖关系 |
|------|------|----------|----------|
| **MedicalCase** | 病历管理（聚合根） | MedicalCase | → Patients |
| **Consultation** | 诊疗记录管理 | Consultation | → MedicalCase |
| **Prescriptions** | 处方管理 | Prescription, PrescriptionItem | → MedicalCase, Herbs |
| **Patients** | 患者档案管理 | Patient | 独立模块 |
| **Herbs** | 药材基础数据 | Herb | 独立模块 |
| **Formula** | 方剂模板管理 | Formula, FormulaItem | → Herbs |
| **Auth** | 认证授权 | User, RefreshToken | 独立模块 |
| **Users** | 用户信息管理 | UserProfile | → Auth |

## 三、编码规范

### 3.1 命名约定

```csharp
// 类型和公共成员：PascalCase
public class PatientService { }
public string PatientName { get; set; }

// 私有字段：_camelCase
private readonly ILogger<PatientService> _logger;
private string _patientId;

// 参数和局部变量：camelCase
public void UpdatePatient(string patientName, int age)
{
    var localVariable = GetData();
}

// 常量：UPPER_CASE
public const string DEFAULT_CONNECTION = "DefaultConnection";

// 异步方法：以Async结尾
public async Task<Patient> GetPatientAsync(Guid id)

// 接口：以I开头
public interface IPatientService { }
```

### 3.2 代码组织

```csharp
// 文件组织顺序
namespace LYBT.Module.Patients.Services
{
    using System;                      // 1. 系统引用
    using Microsoft.Extensions.Logging; // 2. 第三方引用
    using LYBT.Entities.Patients;      // 3. 项目引用
    
    /// <summary>
    /// 患者服务实现
    /// </summary>
    public class PatientService : IPatientService
    {
        // 1. 常量
        private const int MAX_RETRY = 3;
        
        // 2. 字段
        private readonly ILogger<PatientService> _logger;
        
        // 3. 构造函数
        public PatientService(ILogger<PatientService> logger)
        {
            _logger = logger;
        }
        
        // 4. 属性
        public string ServiceName => "PatientService";
        
        // 5. 公共方法
        public async Task<Patient> GetPatientAsync(Guid id)
        {
            // 实现
        }
        
        // 6. 私有方法
        private void ValidateInput(Patient patient)
        {
            // 验证逻辑
        }
    }
}
```

### 3.3 注释规范

```csharp
/// <summary>
/// 获取患者信息
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>患者信息，如果不存在返回null</returns>
/// <exception cref="ArgumentException">当ID为空时抛出</exception>
public async Task<Patient?> GetPatientAsync(Guid id)
{
    // 验证输入参数
    if (id == Guid.Empty)
    {
        throw new ArgumentException("患者ID不能为空", nameof(id));
    }
    
    // TODO: 添加缓存支持 - Issue #123
    
    // 从数据库查询患者信息
    return await _context.Patients
        .Where(p => p.Id == id)
        .FirstOrDefaultAsync();
}
```

## 四、开发流程

### 4.1 Git分支策略

```mermaid
gitGraph
    commit id: "master"
    branch feature/feature-name
    checkout feature/feature-name
    commit id: "开发中"
    commit id: "功能完成"
    checkout master
    merge feature/feature-name
    branch hotfix/bug-fix
    checkout hotfix/bug-fix
    commit id: "修复bug"
    checkout master
    merge hotfix/bug-fix
```

#### 分支命名规范
- **master**: 主分支，生产环境代码
- **feature/xxx**: 功能分支，如 `feature/prescription-import`
- **fix/xxx**: 缺陷修复，如 `fix/login-error`
- **hotfix/xxx**: 紧急修复，如 `hotfix/critical-bug`
- **refactor/xxx**: 重构分支，如 `refactor/repository-pattern`

### 4.2 提交信息规范

```bash
# 格式：<类型>(<范围>): <描述> - Issue #编号

# 示例
feat(prescriptions): 实现处方快速录入功能 - Issue #456
fix(auth): 修复JWT刷新token失效问题 - Issue #789
refactor(patients): 简化患者查询逻辑 - Issue #234
docs(api): 更新API接口文档 - Issue #567
test(consultation): 添加诊疗模块单元测试 - Issue #890
```

#### 提交类型
- **feat**: 新功能
- **fix**: 缺陷修复
- **refactor**: 重构（不影响功能）
- **docs**: 文档更新
- **test**: 测试相关
- **style**: 代码格式调整
- **perf**: 性能优化
- **chore**: 构建或辅助工具变更

### 4.3 代码审查要点

```yaml
审查清单:
  架构层面:
    - 是否符合分层架构设计
    - 是否遵循模块依赖关系
    - 是否避免使用禁用技术
    
  代码质量:
    - 命名是否规范清晰
    - 是否有适当的注释
    - 是否处理了异常情况
    - 是否有重复代码
    
  性能考虑:
    - 是否使用了异步方法
    - 是否有N+1查询问题
    - 是否合理使用缓存
    
  安全性:
    - 是否验证了输入参数
    - 是否防止SQL注入
    - 是否保护了敏感信息
    
  测试:
    - 是否有对应的单元测试
    - 测试覆盖率是否达标
```

## 五、API开发规范

### 5.1 RESTful设计

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    // GET: api/v1/patients
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PatientDto>), 200)]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // 实现
    }
    
    // GET: api/v1/patients/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PatientDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPatient(Guid id)
    {
        // 实现
    }
    
    // POST: api/v1/patients
    [HttpPost]
    [ProducesResponseType(typeof(PatientDto), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreatePatient(
        [FromBody] PatientCreateDto dto)
    {
        // 实现
    }
    
    // PUT: api/v1/patients/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PatientDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePatient(
        Guid id, 
        [FromBody] PatientUpdateDto dto)
    {
        // 实现
    }
    
    // DELETE: api/v1/patients/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        // 实现
    }
}
```

### 5.2 响应格式

```csharp
// 成功响应
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public static ServiceResult<T> Success(T data, string? message = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }
    
    public static ServiceResult<T> Failure(string error)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Errors = new List<string> { error }
        };
    }
}

// 分页响应
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

## 六、数据库开发

### 6.1 Entity Framework Core

```csharp
// 实体配置
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(p => p.IdCardNumber)
            .HasMaxLength(18);
            
        builder.HasIndex(p => p.IdCardNumber)
            .IsUnique()
            .HasFilter("[IdCardNumber] IS NOT NULL");
            
        // 软删除过滤器
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

// 迁移命令
// 添加迁移
dotnet ef migrations add AddPatientTable

// 更新数据库
dotnet ef database update

// 回滚迁移
dotnet ef database update PreviousMigrationName

// 生成SQL脚本
dotnet ef migrations script
```

### 6.2 查询优化

```csharp
// 避免N+1查询
public async Task<List<MedicalCaseDto>> GetMedicalCasesAsync()
{
    return await _context.MedicalCases
        .Include(m => m.Consultation)      // 预加载诊疗记录
        .Include(m => m.Prescription)      // 预加载处方
        .ThenInclude(p => p.Items)         // 预加载处方明细
        .Where(m => !m.IsDeleted)
        .OrderByDescending(m => m.CreatedAt)
        .Take(100)
        .Select(m => new MedicalCaseDto    // 投影，只选择需要的字段
        {
            Id = m.Id,
            PatientName = m.PatientName,
            DoctorName = m.DoctorName,
            ConsultationDate = m.ConsultationDate,
            Status = m.Status
        })
        .ToListAsync();
}

// 使用AsNoTracking提高查询性能
public async Task<PatientDto?> GetPatientByIdAsync(Guid id)
{
    return await _context.Patients
        .AsNoTracking()  // 只读查询，不跟踪实体
        .Where(p => p.Id == id)
        .Select(p => new PatientDto
        {
            Id = p.Id,
            Name = p.Name,
            Gender = p.Gender,
            Age = p.Age
        })
        .FirstOrDefaultAsync();
}
```

## 七、测试规范

### 7.1 单元测试

```csharp
[TestClass]
public class PatientServiceTests
{
    private Mock<IPatientRepository> _repositoryMock;
    private Mock<ILogger<PatientService>> _loggerMock;
    private PatientService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _loggerMock = new Mock<ILogger<PatientService>>();
        _service = new PatientService(_repositoryMock.Object, _loggerMock.Object);
    }
    
    [TestMethod]
    public async Task GetPatientAsync_WithValidId_ShouldReturnPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedPatient = new Patient 
        { 
            Id = patientId, 
            Name = "张三" 
        };
        
        _repositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(expectedPatient);
        
        // Act
        var result = await _service.GetPatientAsync(patientId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedPatient.Id, result.Id);
        Assert.AreEqual(expectedPatient.Name, result.Name);
        
        _repositoryMock.Verify(r => r.GetByIdAsync(patientId), Times.Once);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetPatientAsync_WithEmptyId_ShouldThrowException()
    {
        // Act
        await _service.GetPatientAsync(Guid.Empty);
    }
}
```

### 7.2 集成测试

```csharp
[TestClass]
public class PatientApiIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    
    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // 使用内存数据库
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });
            });
            
        _client = _factory.CreateClient();
    }
    
    [TestMethod]
    public async Task GetPatients_ShouldReturnPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/patients?page=1&pageSize=10");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<PatientDto>>(content);
        
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Items.Count <= 10);
    }
}
```

### 7.3 测试覆盖率要求

```yaml
测试覆盖率目标:
  单元测试: 70%
  集成测试: 20%
  E2E测试: 10%
  
关键模块覆盖率:
  Auth模块: ≥90%       # 安全相关，需要高覆盖率
  MedicalCase: ≥85%    # 核心聚合根
  Prescriptions: ≥85%  # 核心业务
  Patients: ≥80%       # 基础数据
  其他模块: ≥70%
```

## 八、性能优化

### 8.1 缓存策略

```csharp
// 内存缓存配置
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    
    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<T?> GetOrCreateAsync<T>(
        string key, 
        Func<Task<T>> factory,
        TimeSpan? expiration = null) where T : class
    {
        // L1缓存：客户端5分钟
        // L2缓存：API层10分钟
        var cacheExpiration = expiration ?? TimeSpan.FromMinutes(10);
        
        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = cacheExpiration;
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);
            
            _logger.LogInformation($"缓存未命中，从数据源加载: {key}");
            return await factory();
        });
    }
    
    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogInformation($"清除缓存: {key}");
    }
}

// 使用示例
public async Task<List<HerbDto>> GetHerbsAsync()
{
    return await _cacheService.GetOrCreateAsync(
        "herbs:all",
        async () => await _repository.GetAllHerbsAsync(),
        TimeSpan.FromMinutes(30)  // 药材数据缓存30分钟
    );
}
```

### 8.2 查询性能优化

```csharp
// 分页查询优化
public async Task<PagedResult<T>> GetPagedAsync<T>(
    IQueryable<T> query,
    int page,
    int pageSize) where T : class
{
    // 先计算总数（使用Count而非CountAsync避免额外查询）
    var totalCount = await query.CountAsync();
    
    // 再分页查询数据
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<T>
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}

// 批量操作优化
public async Task BatchUpdateAsync(List<Patient> patients)
{
    // 使用批量更新而非逐个更新
    _context.Patients.UpdateRange(patients);
    
    // 设置批量保存大小
    _context.ChangeTracker.AutoDetectChangesEnabled = false;
    await _context.SaveChangesAsync();
    _context.ChangeTracker.AutoDetectChangesEnabled = true;
}
```

## 九、安全规范

### 9.1 输入验证

```csharp
// 使用FluentValidation进行验证
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名不能超过50个字符");
            
        RuleFor(x => x.IdCardNumber)
            .Matches(@"^\d{17}[\dX]$").When(x => !string.IsNullOrEmpty(x.IdCardNumber))
            .WithMessage("身份证号码格式不正确");
            
        RuleFor(x => x.Phone)
            .Matches(@"^1[3-9]\d{9}$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("手机号格式不正确");
            
        RuleFor(x => x.Age)
            .InclusiveBetween(0, 150).WithMessage("年龄必须在0-150之间");
    }
}
```

### 9.2 认证授权

```csharp
// JWT配置
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LYBT";
    public string Audience { get; set; } = "LYBT.Client";
    public int AccessTokenExpirationMinutes { get; set; } = 120;  // 2小时
    public int RefreshTokenExpirationDays { get; set; } = 7;      // 7天
}

// 角色授权
[Authorize(Roles = "Admin,Doctor")]
public async Task<IActionResult> DeletePatient(Guid id)
{
    // 只有管理员和医生可以删除患者
}

// 自定义授权策略
services.AddAuthorization(options =>
{
    options.AddPolicy("CanEditPrescription", policy =>
        policy.RequireAssertion(context =>
        {
            var user = context.User;
            return user.IsInRole("Admin") || 
                   (user.IsInRole("Doctor") && IsWithinEditWindow());
        }));
});
```

### 9.3 数据保护

```csharp
// 敏感信息加密
public class EncryptionService
{
    private readonly IDataProtector _protector;
    
    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("LYBT.PatientData");
    }
    
    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }
    
    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}

// 密码哈希（使用BCrypt）
public class PasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 10);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

## 十、部署与运维

### 10.1 配置管理

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;TrustServerCertificate=true"
  },
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "LYBT",
    "Audience": "LYBT.Client",
    "AccessTokenExpirationMinutes": 120,
    "RefreshTokenExpirationDays": 7
  },
  "Caching": {
    "SlidingExpirationMinutes": 5,
    "AbsoluteExpirationMinutes": 30
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### 10.2 日志配置

```csharp
// Program.cs中配置Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", 
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

// 使用结构化日志
_logger.LogInformation("用户 {UserId} 在 {Time} 登录系统", 
    userId, 
    DateTime.Now);
    
_logger.LogError(exception, 
    "处理患者 {PatientId} 的处方时发生错误", 
    patientId);
```

### 10.3 健康检查

```csharp
// 配置健康检查
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "database")
    .AddMemory(name: "memory");

// 健康检查端点
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

## 十一、故障排查

### 11.1 常见问题

| 问题 | 原因 | 解决方法 |
|------|------|----------|
| **编译失败** | NuGet包未还原 | 运行 `dotnet restore` |
| **数据库连接失败** | 连接字符串错误 | 检查appsettings.json |
| **JWT验证失败** | 密钥不匹配 | 确保客户端和服务器使用相同密钥 |
| **EF迁移失败** | 模型变更冲突 | 删除最后的迁移，重新生成 |
| **缓存未生效** | 配置错误 | 检查IMemoryCache注入 |

### 11.2 调试技巧

```csharp
// 条件断点
#if DEBUG
    if (patientId == specificId)
    {
        Debugger.Break();
    }
#endif

// 日志调试
_logger.LogDebug("进入方法 {Method}，参数: {@Parameters}", 
    nameof(GetPatientAsync), 
    new { patientId });

// SQL查询日志
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging()  // 仅在开发环境
           .EnableDetailedErrors());       // 仅在开发环境
```

## 十二、工具与资源

### 12.1 开发工具

- **Visual Studio 2022**: 主IDE
- **VS Code**: 轻量级编辑器
- **SQL Server Management Studio**: 数据库管理
- **Postman/Insomnia**: API测试
- **Git Extensions/SourceTree**: Git GUI工具

### 12.2 VS Code扩展

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-dotnettools.csdevkit",
    "ms-azuretools.vscode-docker",
    "ms-mssql.mssql",
    "humao.rest-client",
    "streetsidesoftware.code-spell-checker",
    "formulahendry.dotnet-test-explorer"
  ]
}
```

### 12.3 NuGet包推荐

```xml
<!-- 常用NuGet包 -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.*" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.*" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.*" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.*" />
<PackageReference Include="Moq" Version="4.20.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />
```

### 12.4 学习资源

- [官方文档 - docs/architecture/](../architecture/)
- [API文档 - docs/api/](../api/)
- [需求文档 - docs/requirements/](../requirements/)
- [技术标准 - docs/development/technical-standards.md](technical-standards.md)

## 十三、版本历史

| 版本 | 日期 | 主要变更 |
|------|------|----------|
| 1.0.0 | 2025-01-02 | 初始版本，建立开发规范基线 |

---

**维护说明**: 本文档每月更新一次，如有重大变更将立即更新。如发现文档与实际不符，请提交Issue。

**联系方式**: 技术问题请在GitHub创建Issue，紧急问题请联系项目负责人。