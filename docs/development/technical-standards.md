# LYBT中医诊所管理系统 - 技术标准与开发规范

**版本**：v2.0  
**日期**：2025-09-28  
**编制**：基于架构设计文档v3.0  
**状态**：技术标准定稿  

## 一、技术选型标准

### 1.1 选型原则

| 原则 | 说明 | 权重 |
|------|------|------|
| **适度设计** | 避免过度工程，满足当前需求即可 | ★★★★★ |
| **成熟稳定** | 选择经过验证的技术，避免实验性技术 | ★★★★★ |
| **团队熟悉** | 优先选择团队已掌握的技术 | ★★★★ |
| **社区支持** | 有活跃社区和完善文档 | ★★★ |
| **许可证** | 商业友好的开源协议 | ★★★ |

### 1.2 核心技术栈

#### 1.2.1 后端技术栈
| 组件 | 选择 | 版本 | 理由 |
|------|------|------|------|
| 运行时 | .NET | 8.0 LTS | 最新长期支持版，性能优秀 |
| 框架 | ASP.NET Core | 8.0 | 成熟的Web API框架 |
| ORM | Entity Framework Core | 8.0 | 简化数据访问，支持迁移 |
| 数据库 | SQL Server | 2019+ | 企业级稳定，团队熟悉 |
| 缓存 | MemoryCache | 内置 | 简单够用，无需Redis |
| 认证 | JWT | - | 无状态，适合分布式 |
| 日志 | Serilog | 3.1.1 | 结构化日志，配置灵活 |
| API文档 | Swagger | 6.5.0 | 自动生成，方便调试 |

#### 1.2.2 前端技术栈
| 组件 | 选择 | 版本 | 理由 |
|------|------|------|------|
| 框架 | WPF | .NET 8 | 成熟稳定，适合复杂表单 |
| MVVM | Prism | 9.0 | 模块化架构，依赖注入 |
| IoC容器 | DryIoc | 5.4.3 | 轻量高效，Prism默认支持 |
| HTTP客户端 | Refit | 7.0.0 | 类型安全的REST客户端 |
| 控件库 | Material Design | 5.0.0 | 现代化UI，组件丰富 |
| 验证 | FluentValidation | 11.9.0 | 流畅的验证规则 |

#### 1.2.3 共享组件
| 组件 | 选择 | 版本 | 理由 |
|------|------|------|------|
| 映射 | AutoMapper | 13.0.1 | 简化DTO转换 |
| 序列化 | System.Text.Json | 内置 | 高性能，.NET原生 |
| 拼音 | TinyPinyin | 1.0.2 | 轻量级拼音转换 |
| Excel | ClosedXML | 0.102.2 | 无需Office，功能完整 |

### 1.3 明确禁用技术

| 技术 | 禁用理由 | 替代方案 |
|------|----------|----------|
| **CQRS/MediatR** | 过度设计，增加复杂度 | 直接调用Service |
| **微服务** | 系统规模不需要 | 单体应用 |
| **Redis** | 部署维护成本高 | MemoryCache |
| **RabbitMQ/Kafka** | 无异步处理需求 | 同步调用 |
| **Docker/K8s** | 增加运维复杂度 | 传统部署 |
| **GraphQL** | 学习成本高 | RESTful API |
| **SignalR** | 无实时通信需求 | HTTP轮询（如需要） |
| **gRPC** | 内部系统不需要 | HTTP/JSON |

## 二、编码规范

### 2.1 命名规范

#### 2.1.1 C#命名规范
```csharp
// 类名：PascalCase
public class PatientService { }

// 接口：I前缀 + PascalCase
public interface IPatientService { }

// 公有成员：PascalCase
public string PatientName { get; set; }

// 私有字段：_camelCase
private readonly ILogger _logger;

// 参数和局部变量：camelCase
public void CreatePatient(string patientName) 
{
    var localVariable = patientName;
}

// 常量：UPPER_CASE
public const int MAX_RETRY_COUNT = 3;

// 异步方法：Async后缀
public async Task<Patient> GetPatientAsync(Guid id) { }
```

#### 2.1.2 数据库命名规范
```sql
-- 表名：复数形式
CREATE TABLE Patients

-- 列名：PascalCase
PatientId, CreatedAt, IsDeleted

-- 索引：IX_表名_列名
CREATE INDEX IX_Patients_PhoneNumber

-- 外键：FK_子表_父表_列名
CONSTRAINT FK_MedicalCases_Patients_PatientId
```

#### 2.1.3 API路由规范
```
GET    /api/v1/patients          # 资源复数
GET    /api/v1/patients/{id}     # 路径参数
POST   /api/v1/patients          # 创建资源
PUT    /api/v1/patients/{id}     # 更新资源
DELETE /api/v1/patients/{id}     # 删除资源

# 嵌套资源
GET    /api/v1/patients/{patientId}/medical-cases  # kebab-case
```

### 2.2 项目结构规范

#### 2.2.1 解决方案结构
```
LYBT.sln
├── src/
│   ├── Server/
│   │   ├── Core/
│   │   │   ├── LYBT.Entities/           # 领域实体
│   │   │   └── LYBT.Infrastructure/     # 基础设施
│   │   ├── Modules/
│   │   │   ├── LYBT.Module.Auth/
│   │   │   ├── LYBT.Module.Patients/
│   │   │   └── ...
│   │   └── Services/
│   │       └── LYBT.WebAPI/             # API入口
│   ├── Client/
│   │   └── Desktop/
│   │       ├── Core/
│   │       ├── Infrastructure/
│   │       ├── Modules/
│   │       └── Shell/
│   └── Shared/
│       ├── LYBT.Shared.Models/          # DTO定义
│       ├── LYBT.Shared.Interfaces/      # 接口定义
│       └── LYBT.Shared.Utilities/       # 工具类
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
└── docs/
```

#### 2.2.2 模块内部结构
```
LYBT.Module.Patients/
├── Controllers/           # API控制器
│   └── PatientsController.cs
├── Services/              # 业务服务
│   ├── IPatientService.cs
│   ├── PatientQueryService.cs
│   └── PatientBusinessService.cs
├── Repositories/          # 数据访问
│   ├── IPatientRepository.cs
│   └── PatientRepository.cs
├── Validators/            # 验证器
│   └── PatientValidator.cs
├── Mapping/               # AutoMapper配置
│   └── PatientMappingProfile.cs
└── PatientsModule.cs      # 模块注册
```

### 2.3 代码规范

#### 2.3.1 类设计规范
```csharp
// 1. 一个文件一个类
// 2. 类不超过500行
// 3. 方法不超过50行
// 4. 参数不超过5个

public class PatientService : IPatientService
{
    // 依赖注入的字段放在最前
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatientService> _logger;
    private readonly IMapper _mapper;
    
    // 构造函数
    public PatientService(
        IPatientRepository repository,
        ILogger<PatientService> logger,
        IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }
    
    // 公有方法
    public async Task<ServiceResult<PatientDto>> CreateAsync(
        PatientCreateDto dto)
    {
        // 实现
    }
    
    // 私有方法放在最后
    private void ValidatePatient(Patient patient)
    {
        // 验证逻辑
    }
}
```

#### 2.3.2 异步编程规范
```csharp
// 1. 异步方法必须返回Task或Task<T>
// 2. 异步方法名必须以Async结尾
// 3. 不要使用async void（事件处理器除外）
// 4. 使用ConfigureAwait(false)（UI层除外）

public async Task<Patient> GetPatientAsync(Guid id)
{
    // 正确：使用await
    var patient = await _repository.GetByIdAsync(id)
        .ConfigureAwait(false);
    
    // 错误：不要使用.Result或.Wait()
    // var patient = _repository.GetByIdAsync(id).Result;
    
    return patient;
}
```

#### 2.3.3 异常处理规范
```csharp
// 1. 使用特定异常类型
public class DomainException : Exception { }
public class ValidationException : Exception { }
public class NotFoundException : Exception { }

// 2. 不要吞掉异常
try
{
    await _repository.SaveAsync(entity);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "保存实体失败: {EntityId}", entity.Id);
    throw new DataException("保存失败", ex);
}

// 3. 使用全局异常处理器
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

### 2.4 数据访问规范

#### 2.4.1 Repository模式
```csharp
// 基础Repository接口
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}

// 具体Repository接口
public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByIdNumberAsync(string idNumber);
    Task<IEnumerable<Patient>> SearchByPinyinAsync(string pinyin);
}

// 实现
public class PatientRepository : Repository<Patient>, IPatientRepository
{
    public async Task<Patient?> GetByIdNumberAsync(string idNumber)
    {
        return await Query()
            .FirstOrDefaultAsync(p => p.IdNumber == idNumber);
    }
}
```

#### 2.4.2 查询优化
```csharp
// 1. 使用投影减少数据传输
var patients = await _context.Patients
    .Where(p => !p.IsDeleted)
    .Select(p => new PatientListDto
    {
        Id = p.Id,
        Name = p.Name,
        PhoneNumber = p.PhoneNumber
    })
    .ToListAsync();

// 2. 使用AsNoTracking提高只读查询性能
var patient = await _context.Patients
    .AsNoTracking()
    .FirstOrDefaultAsync(p => p.Id == id);

// 3. 使用Include避免N+1问题
var medicalCase = await _context.MedicalCases
    .Include(m => m.Patient)
    .Include(m => m.Consultation)
    .Include(m => m.Prescription)
        .ThenInclude(p => p.Items)
    .FirstOrDefaultAsync(m => m.Id == id);

// 4. 分页查询
var pagedResult = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## 三、API设计标准

### 3.1 RESTful规范

#### 3.1.1 HTTP方法语义
| 方法 | 语义 | 幂等 | 安全 |
|------|------|------|------|
| GET | 查询资源 | ✅ | ✅ |
| POST | 创建资源 | ❌ | ❌ |
| PUT | 完整更新 | ✅ | ❌ |
| PATCH | 部分更新 | ✅ | ❌ |
| DELETE | 删除资源 | ✅ | ❌ |

#### 3.1.2 状态码规范
| 状态码 | 含义 | 使用场景 |
|--------|------|----------|
| 200 | 成功 | GET/PUT/PATCH成功 |
| 201 | 已创建 | POST成功创建资源 |
| 204 | 无内容 | DELETE成功 |
| 400 | 请求错误 | 参数验证失败 |
| 401 | 未认证 | 未登录或Token无效 |
| 403 | 禁止访问 | 无权限 |
| 404 | 未找到 | 资源不存在 |
| 409 | 冲突 | 业务规则冲突 |
| 500 | 服务器错误 | 未处理异常 |

### 3.2 请求响应规范

#### 3.2.1 请求格式
```json
// 创建请求
POST /api/v1/patients
Content-Type: application/json
Authorization: Bearer {token}

{
    "name": "张三",
    "phoneNumber": "13800138000",
    "idNumber": "110101199001011234",
    "address": "北京市朝阳区"
}

// 查询请求
GET /api/v1/patients?page=1&pageSize=20&keyword=张
```

#### 3.2.2 响应格式
```json
// 成功响应
{
    "success": true,
    "code": 200,
    "message": "操作成功",
    "data": {
        "id": "550e8400-e29b-41d4-a716-446655440000",
        "name": "张三"
    },
    "timestamp": "2025-09-28T10:00:00Z"
}

// 错误响应
{
    "success": false,
    "code": 400,
    "message": "参数验证失败",
    "errors": {
        "phoneNumber": ["手机号格式不正确"],
        "idNumber": ["身份证号已存在"]
    },
    "timestamp": "2025-09-28T10:00:00Z"
}

// 分页响应
{
    "success": true,
    "code": 200,
    "data": {
        "items": [...],
        "totalCount": 100,
        "pageNumber": 1,
        "pageSize": 20,
        "totalPages": 5
    }
}
```

### 3.3 API版本管理

```csharp
// 1. URL路径版本（推荐）
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatientsController : ControllerBase { }

// 2. 配置版本策略
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// 3. 版本弃用
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
public class PatientsController : ControllerBase { }
```

## 四、安全规范

### 4.1 认证授权

#### 4.1.1 JWT配置
```csharp
// JWT选项
public class JwtOptions
{
    public string Secret { get; set; }      // 至少32字符
    public string Issuer { get; set; }      // 发行者
    public string Audience { get; set; }    // 受众
    public int ExpireMinutes { get; set; } = 480;  // 8小时
    public int RefreshExpireDays { get; set; } = 7;
}

// Token生成
public string GenerateToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    
    var token = new JwtSecurityToken(
        issuer: _jwtOptions.Issuer,
        audience: _jwtOptions.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes),
        signingCredentials: credentials
    );
    
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

#### 4.1.2 权限控制
```csharp
// 角色授权
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase { }

// 策略授权
[Authorize(Policy = "CanModifyPatient")]
public async Task<IActionResult> UpdatePatient(Guid id) { }

// 策略定义
services.AddAuthorization(options =>
{
    options.AddPolicy("CanModifyPatient", policy =>
        policy.RequireAssertion(context =>
        {
            var user = context.User;
            var isAdmin = user.IsInRole("Admin");
            var isOwner = user.HasClaim("PatientOwner", "true");
            return isAdmin || isOwner;
        }));
});
```

### 4.2 数据安全

#### 4.2.1 密码安全
```csharp
// 密码哈希
public class PasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }
    
    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

// 密码策略
public class PasswordPolicy
{
    public int MinLength { get; set; } = 6;
    public bool RequireDigit { get; set; } = true;
    public bool RequireUpper { get; set; } = true;
    public bool RequireLower { get; set; } = true;
    public bool RequireSpecial { get; set; } = false;
}
```

#### 4.2.2 数据脱敏
```csharp
public static class DataMasking
{
    // 手机号脱敏：138****1234
    public static string MaskPhoneNumber(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length != 11)
            return phone;
        
        return $"{phone[..3]}****{phone[7..]}";
    }
    
    // 身份证脱敏：110***********1234
    public static string MaskIdNumber(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length != 18)
            return idNumber;
        
        return $"{idNumber[..3]}***********{idNumber[14..]}";
    }
}
```

### 4.3 输入验证

```csharp
// FluentValidation示例
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不能为空")
            .Length(2, 20).WithMessage("姓名长度必须在2-20个字符之间");
        
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
        
        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("身份证号不能为空")
            .Matches(@"^\d{17}[\dXx]$").WithMessage("身份证号格式不正确")
            .Must(BeValidIdNumber).WithMessage("身份证号无效");
    }
    
    private bool BeValidIdNumber(string idNumber)
    {
        // 身份证号校验逻辑
        return IdNumberValidator.Validate(idNumber);
    }
}
```

## 五、性能优化标准

### 5.1 数据库优化

#### 5.1.1 索引策略
```sql
-- 主键索引（自动创建）
PRIMARY KEY (Id)

-- 唯一索引
CREATE UNIQUE INDEX UX_Patients_IdNumber ON Patients(IdNumber)

-- 查询索引
CREATE INDEX IX_Patients_PhoneNumber ON Patients(PhoneNumber)
CREATE INDEX IX_Patients_PinyinCode ON Patients(PinyinCode)

-- 复合索引
CREATE INDEX IX_MedicalCases_PatientId_CreatedAt 
ON MedicalCases(PatientId, CreatedAt DESC)

-- 包含列索引
CREATE INDEX IX_Patients_Name_Include_Phone 
ON Patients(Name) INCLUDE (PhoneNumber)
```

#### 5.1.2 查询优化
```csharp
// 1. 避免N+1问题
var patients = await _context.Patients
    .Include(p => p.MedicalCases)
        .ThenInclude(m => m.Consultation)
    .ToListAsync();

// 2. 使用分页
var result = await query
    .OrderBy(p => p.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// 3. 只查询需要的字段
var summary = await _context.Patients
    .Select(p => new 
    { 
        p.Id, 
        p.Name, 
        CaseCount = p.MedicalCases.Count() 
    })
    .ToListAsync();
```

### 5.2 缓存策略

#### 5.2.1 缓存层级
```csharp
// L1: 进程内缓存
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // 限制缓存项数
});

// L2: 分布式缓存（预留）
// services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = "localhost:6379";
// });

// 缓存服务封装
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiry);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}
```

#### 5.2.2 缓存策略
```csharp
public class CacheStrategy
{
    // 缓存时间配置
    public static readonly TimeSpan ShortCache = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MediumCache = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan LongCache = TimeSpan.FromMinutes(30);
    
    // 缓存键生成
    public static string GetPatientKey(Guid id) => $"patient:{id}";
    public static string GetHerbListKey() => "herbs:list";
    public static string GetUserPermKey(Guid userId) => $"user:perm:{userId}";
    
    // 缓存失效策略
    public static async Task InvalidatePatientCache(
        ICacheService cache, 
        Guid patientId)
    {
        await cache.RemoveAsync(GetPatientKey(patientId));
        await cache.RemoveByPrefixAsync("patients:list:");
    }
}
```

## 六、日志规范

### 6.1 日志级别

| 级别 | 使用场景 | 示例 |
|------|----------|------|
| **Fatal** | 系统崩溃 | 数据库不可用 |
| **Error** | 异常错误 | 未处理异常 |
| **Warning** | 警告信息 | 性能问题、重试 |
| **Information** | 业务事件 | 用户登录、创建订单 |
| **Debug** | 调试信息 | SQL语句、详细流程 |
| **Verbose** | 详细跟踪 | 方法进入/退出 |

### 6.2 结构化日志

```csharp
// Serilog配置
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/lybt-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// 使用示例
_logger.LogInformation(
    "用户 {UserId} 创建了患者 {PatientId}，姓名：{PatientName}",
    userId, patientId, patientName);

_logger.LogError(ex, 
    "保存患者 {PatientId} 失败",
    patientId);
```

### 6.3 审计日志

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string TableName { get; set; }
    public string EntityId { get; set; }
    public string Action { get; set; }  // Create/Update/Delete
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
}

// EF拦截器实现审计
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var context = eventData.Context;
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged);
        
        foreach (var entry in entries)
        {
            var auditLog = CreateAuditLog(entry);
            context.Set<AuditLog>().Add(auditLog);
        }
        
        return base.SavingChangesAsync(eventData, result);
    }
}
```

## 七、测试标准

### 7.1 测试层级

| 层级 | 占比 | 目标 | 工具 |
|------|------|------|------|
| 单元测试 | 70% | 业务逻辑 | xUnit, Moq |
| 集成测试 | 20% | API端点 | TestServer |
| E2E测试 | 10% | 用户流程 | Selenium |

### 7.2 单元测试规范

```csharp
// 命名规范：方法名_场景_期望结果
[Fact]
public async Task CreatePatient_WithValidData_ShouldReturnSuccess()
{
    // Arrange - 准备数据
    var dto = new PatientCreateDto
    {
        Name = "测试患者",
        PhoneNumber = "13800138000",
        IdNumber = "110101199001011234",
        Address = "测试地址"
    };
    
    _mockRepository
        .Setup(x => x.AddAsync(It.IsAny<Patient>()))
        .ReturnsAsync((Patient p) => p);
    
    // Act - 执行操作
    var result = await _service.CreateAsync(dto);
    
    // Assert - 验证结果
    result.Should().NotBeNull();
    result.IsSuccess.Should().BeTrue();
    result.Data.Name.Should().Be(dto.Name);
    
    _mockRepository.Verify(
        x => x.AddAsync(It.IsAny<Patient>()), 
        Times.Once);
}
```

### 7.3 集成测试规范

```csharp
public class PatientControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public PatientControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 替换真实数据库为内存数据库
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });
        
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task GetPatients_ShouldReturnPagedResult()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", GetTestToken());
        
        // Act
        var response = await _client.GetAsync("/api/v1/patients");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<PatientDto>>>(content);
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }
}
```

## 八、部署标准

### 8.1 环境配置

#### 8.1.1 配置文件层级
```
appsettings.json              # 基础配置
appsettings.Development.json  # 开发环境
appsettings.Staging.json      # 测试环境
appsettings.Production.json   # 生产环境
```

#### 8.1.2 敏感信息管理
```csharp
// 开发环境：User Secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."

// 生产环境：环境变量
Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "...");

// Azure Key Vault（预留）
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{vaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 8.2 发布配置

```xml
<!-- 发布配置文件 -->
<Project>
  <PropertyGroup>
    <PublishProtocol>FileSystem</PublishProtocol>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
    <PublishSingleFile>false</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
  </PropertyGroup>
</Project>
```

### 8.3 健康检查

```csharp
// 健康检查配置
services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck("cache", () =>
    {
        var cache = serviceProvider.GetService<IMemoryCache>();
        return cache != null 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy();
    });

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

## 九、文档标准

### 9.1 代码注释

```csharp
/// <summary>
/// 创建患者档案
/// </summary>
/// <param name="dto">患者创建信息</param>
/// <returns>创建成功的患者信息</returns>
/// <exception cref="ValidationException">参数验证失败</exception>
/// <exception cref="DuplicateException">身份证号重复</exception>
public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
{
    // 业务逻辑注释：解释为什么，而不是做什么
    // 检查身份证号唯一性（业务要求）
    var exists = await _repository.ExistsByIdNumberAsync(dto.IdNumber);
    if (exists)
    {
        throw new DuplicateException("身份证号已存在");
    }
    
    // TODO: 添加患者照片上传功能
    // FIXME: 修复拼音码生成的多音字问题
    // HACK: 临时解决方案，等待第三方库更新
}
```

### 9.2 API文档

```csharp
/// <summary>
/// 患者管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    /// <summary>
    /// 获取患者列表
    /// </summary>
    /// <param name="page">页码（从1开始）</param>
    /// <param name="pageSize">每页数量（默认20）</param>
    /// <param name="keyword">搜索关键词（姓名/拼音码/手机号）</param>
    /// <returns>患者分页列表</returns>
    /// <response code="200">返回患者列表</response>
    /// <response code="401">未授权</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PatientListDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetPatients(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null)
    {
        // 实现
    }
}
```

## 十、版本管理标准

### 10.1 Git分支策略

```
main/master     # 生产分支
  ├── develop   # 开发分支
  ├── feature/* # 功能分支
  ├── bugfix/*  # 缺陷修复
  └── hotfix/*  # 紧急修复
```

### 10.2 提交规范

```
<type>(<scope>): <subject>

<body>

<footer>
```

类型：
- feat: 新功能
- fix: 缺陷修复
- docs: 文档更新
- style: 代码格式
- refactor: 重构
- test: 测试
- chore: 构建/工具

示例：
```
feat(patients): 添加Excel批量导入功能

- 支持2000条患者数据导入
- 自动验证身份证号唯一性
- 生成导入报告

Closes #123
```

### 10.3 版本号规范

```
主版本.次版本.修订号
MAJOR.MINOR.PATCH

1.0.0 - 初始发布
1.1.0 - 新增功能
1.1.1 - 缺陷修复
2.0.0 - 不兼容更新
```

## 附录A：检查清单

### 代码审查清单

- [ ] 命名是否符合规范？
- [ ] 是否有适当的注释？
- [ ] 是否处理了所有异常？
- [ ] 是否有单元测试？
- [ ] 是否有性能问题？
- [ ] 是否有安全问题？
- [ ] 是否符合SOLID原则？
- [ ] 是否有重复代码？

### 发布前检查清单

- [ ] 所有测试通过？
- [ ] 代码审查完成？
- [ ] 文档已更新？
- [ ] 配置文件正确？
- [ ] 数据库迁移脚本？
- [ ] 性能测试通过？
- [ ] 安全扫描通过？
- [ ] 版本号已更新？

## 附录B：工具清单

### 开发工具

| 工具 | 用途 | 版本 |
|------|------|------|
| Visual Studio | IDE | 2022 |
| VS Code | 轻量编辑器 | Latest |
| SQL Server Management Studio | 数据库管理 | 19.0 |
| Postman | API测试 | Latest |
| Git | 版本控制 | 2.40+ |

### NuGet包清单

```xml
<!-- 后端核心包 -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />

<!-- 前端核心包 -->
<PackageReference Include="Prism.DryIoc" Version="9.0.271-pre" />
<PackageReference Include="MaterialDesignThemes" Version="5.0.0" />
<PackageReference Include="Refit" Version="7.0.0" />

<!-- 测试包 -->
<PackageReference Include="xunit" Version="2.6.6" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

---

**文档维护**：
| 版本 | 日期 | 修订内容 |
|------|------|----------|
| v1.0 | 2025-09-28 | 初始版本 |
| v2.0 | 2025-09-28 | 完善所有技术标准 |