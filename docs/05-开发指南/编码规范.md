# 编码标准

## 目录

1. [概述](#概述)
2. [C# 编码规范](#csharp-编码规范)
3. [命名约定](#命名约定)
4. [代码组织](#代码组织)
5. [编程实践](#编程实践)
6. [API 设计规范](#api-设计规范)
7. [数据访问规范](#数据访问规范)
8. [异常处理](#异常处理)
9. [日志记录](#日志记录)
10. [测试规范](#测试规范)
11. [安全编码](#安全编码)
12. [性能优化](#性能优化)
13. [代码审查](#代码审查)

## 概述

本文档定义了凌隐宝堂中医诊所诊疗系统的编码标准和最佳实践。所有开发人员都应遵循这些规范，以确保代码的一致性、可读性和可维护性。

### 核心原则

1. **清晰性优于巧妙性**：代码应该易于理解
2. **一致性**：整个项目保持统一的编码风格
3. **简单性**：避免过度设计，保持简单
4. **可测试性**：代码应该易于测试
5. **可维护性**：考虑未来的维护需求

## C# 编码规范

### 1. 基本格式

#### 缩进和空格

```csharp
// 使用 4 个空格进行缩进，不使用 Tab
public class PatientService
{
    private readonly IPatientRepository _repository;
    
    public PatientService(IPatientRepository repository)
    {
        _repository = repository;
    }
}
```

#### 大括号

```csharp
// 大括号独占一行（Allman style）
if (condition)
{
    // 代码块
}
else
{
    // 代码块
}

// 单行语句也要使用大括号
if (condition)
{
    return true;
}
```

#### 行长度

- 每行代码不超过 120 个字符
- 超长的行应该合理换行

```csharp
// 长参数列表换行
public async Task<ApiResponse<PatientDetailDto>> RegisterPatientAsync(
    string name,
    string idNumber,
    DateTime birthDate,
    string phoneNumber,
    string address)
{
    // 方法实现
}
```

### 2. 语言特性使用

#### 使用 var

```csharp
// 当类型明显时使用 var
var patient = new Patient();
var patients = new List<Patient>();

// 类型不明显时使用显式类型
IPatientService service = serviceFactory.CreatePatientService();
Dictionary<string, object> config = GetConfiguration();
```

#### 使用 async/await

```csharp
// 异步方法命名以 Async 结尾
public async Task<Patient> GetPatientByIdAsync(Guid id)
{
    return await _repository.GetByIdAsync(id);
}

// 避免不必要的 async
public Task<Patient> GetPatientByIdAsync(Guid id)
{
    // 如果只是返回 Task，不需要 async/await
    return _repository.GetByIdAsync(id);
}
```

#### 使用 LINQ

```csharp
// 使用方法语法
var activePatients = patients
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .ToList();

// 复杂查询使用查询语法
var patientRecords = from p in patients
                     join r in records on p.Id equals r.PatientId
                     where p.IsActive
                     select new { Patient = p, Record = r };
```

### 3. 注释规范

#### XML 文档注释

```csharp
/// <summary>
/// 根据ID获取患者信息
/// </summary>
/// <param name="id">患者ID</param>
/// <returns>患者详细信息</returns>
/// <exception cref="NotFoundException">当患者不存在时抛出</exception>
public async Task<PatientDetailDto> GetPatientByIdAsync(Guid id)
{
    // 实现
}
```

#### 代码注释

```csharp
public async Task ProcessRegistrationAsync(RegistrationDto dto)
{
    // 验证患者信息
    var patient = await ValidatePatientAsync(dto.PatientId);
    
    // 检查医生排班
    var schedule = await CheckDoctorScheduleAsync(dto.DoctorId, dto.AppointmentTime);
    
    // TODO: 添加短信通知功能
    // FIXME: 处理并发预约的问题
    
    // 创建挂号记录
    await CreateRegistrationAsync(patient, schedule);
}
```

## 命名约定

### 1. 通用规则

| 类型 | 命名规则 | 示例 |
|------|---------|------|
| 类 | PascalCase | `PatientService` |
| 接口 | I + PascalCase | `IPatientService` |
| 方法 | PascalCase | `GetPatientById` |
| 属性 | PascalCase | `PatientName` |
| 参数 | camelCase | `patientId` |
| 局部变量 | camelCase | `localVariable` |
| 常量 | UPPER_CASE | `MAX_RETRY_COUNT` |
| 私有字段 | _camelCase | `_repository` |

### 2. 特定命名规范

#### 异步方法

```csharp
// 异步方法以 Async 结尾
public async Task<Patient> GetPatientByIdAsync(Guid id)
public async Task SavePatientAsync(Patient patient)
```

#### 布尔属性和方法

```csharp
// 使用 Is、Has、Can 等前缀
public bool IsActive { get; set; }
public bool HasAllergies { get; set; }
public bool CanEdit() { return true; }
```

#### 集合

```csharp
// 使用复数形式
public List<Patient> Patients { get; set; }
public IEnumerable<Doctor> Doctors { get; set; }
```

#### DTOs

```csharp
// 数据传输对象使用 Dto 后缀
public class PatientDto { }
public class PatientCreateDto { }
public class PatientEditDto { }
public class PatientDetailDto { }
```

## 代码组织

### 1. 文件组织

```csharp
// 每个文件只包含一个公共类型
// 文件名与类型名称相同

// PatientService.cs
public class PatientService : IPatientService
{
    // 类实现
}
```

### 2. 类成员组织

```csharp
public class PatientService
{
    // 1. 常量
    private const int MAX_NAME_LENGTH = 100;
    
    // 2. 静态字段
    private static readonly ILogger _logger;
    
    // 3. 私有字段
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    
    // 4. 构造函数
    public PatientService(IPatientRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    
    // 5. 属性
    public string ServiceName { get; }
    
    // 6. 公共方法
    public async Task<PatientDto> GetPatientAsync(Guid id)
    {
        // 实现
    }
    
    // 7. 保护方法
    protected virtual void ValidatePatient(Patient patient)
    {
        // 实现
    }
    
    // 8. 私有方法
    private bool IsValidIdNumber(string idNumber)
    {
        // 实现
    }
    
    // 9. 嵌套类型
    private class PatientValidator
    {
        // 实现
    }
}
```

### 3. 命名空间组织

```csharp
// 系统命名空间
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// 第三方命名空间
using AutoMapper;
using Microsoft.EntityFrameworkCore;

// 项目命名空间
using LYBT.Models.Entities;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts;
```

## 编程实践

### 1. SOLID 原则

#### 单一职责原则（SRP）

```csharp
// 好的示例：每个类只有一个职责
public class PatientService : IPatientService
{
    // 只负责患者业务逻辑
}

public class PatientValidator
{
    // 只负责患者数据验证
}

public class PatientRepository : IPatientRepository
{
    // 只负责患者数据访问
}
```

#### 开闭原则（OCP）

```csharp
// 使用抽象和接口实现扩展
public interface INotificationService
{
    Task SendNotificationAsync(string message);
}

public class SmsNotificationService : INotificationService
{
    public async Task SendNotificationAsync(string message)
    {
        // SMS 实现
    }
}

public class EmailNotificationService : INotificationService
{
    public async Task SendNotificationAsync(string message)
    {
        // Email 实现
    }
}
```

### 2. 依赖注入

```csharp
// 使用构造函数注入
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientService> _logger;
    
    public PatientService(
        IPatientRepository repository,
        IMapper mapper,
        ILogger<PatientService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### 3. 防御性编程

```csharp
public async Task<PatientDto> GetPatientByIdAsync(Guid id)
{
    // 参数验证
    if (id == Guid.Empty)
    {
        throw new ArgumentException("Patient ID cannot be empty", nameof(id));
    }
    
    // 空值检查
    var patient = await _repository.GetByIdAsync(id);
    if (patient == null)
    {
        throw new NotFoundException($"Patient with ID {id} not found");
    }
    
    return _mapper.Map<PatientDto>(patient);
}
```

## API 设计规范

### 1. RESTful 原则

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class PatientsController : BaseController
{
    // GET api/v1/patients
    [HttpGet]
    public async Task<IActionResult> GetAllPatients([FromQuery] PageRequest request)
    {
        var result = await _patientService.GetPatientsAsync(request);
        return ApiResponse(result);
    }
    
    // GET api/v1/patients/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPatientById(Guid id)
    {
        var result = await _patientService.GetPatientByIdAsync(id);
        return ApiResponse(result);
    }
    
    // POST api/v1/patients
    [HttpPost]
    public async Task<IActionResult> CreatePatient([FromBody] PatientCreateDto dto)
    {
        var result = await _patientService.CreatePatientAsync(dto);
        return CreatedAtAction(nameof(GetPatientById), new { id = result.Id }, result);
    }
    
    // PUT api/v1/patients/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePatient(Guid id, [FromBody] PatientEditDto dto)
    {
        dto.Id = id;
        var result = await _patientService.UpdatePatientAsync(dto);
        return ApiResponse(result);
    }
    
    // DELETE api/v1/patients/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        await _patientService.DeletePatientAsync(id);
        return NoContent();
    }
}
```

### 2. API 响应格式

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public string ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### 3. 版本控制

```csharp
// URL 版本控制
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class PatientsController : BaseController
{
    // v1 实现
}

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
public class PatientsV2Controller : BaseController
{
    // v2 实现
}
```

## 数据访问规范

### 1. Repository 模式

```csharp
public interface IPatientRepository : IBaseRepository<Patient>
{
    Task<IEnumerable<Patient>> GetActivePatientAsync();
    Task<Patient> GetPatientWithRecordsAsync(Guid id);
}

public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) : base(context)
    {
    }
    
    public async Task<IEnumerable<Patient>> GetActivePatientAsync()
    {
        return await _context.Patients
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
    
    public async Task<Patient> GetPatientWithRecordsAsync(Guid id)
    {
        return await _context.Patients
            .Include(p => p.MedicalRecords)
            .Include(p => p.Allergies)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
```

### 2. Entity Framework 最佳实践

```csharp
// 使用 AsNoTracking 提高查询性能
public async Task<IEnumerable<PatientDto>> GetPatientsForDisplayAsync()
{
    var patients = await _context.Patients
        .AsNoTracking()
        .Where(p => p.IsActive)
        .Select(p => new PatientDto
        {
            Id = p.Id,
            Name = p.Name,
            PhoneNumber = p.PhoneNumber
        })
        .ToListAsync();
        
    return patients;
}

// 批量操作使用事务
public async Task UpdatePatientsAsync(IEnumerable<Patient> patients)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        _context.UpdateRange(patients);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 3. 查询优化

```csharp
// 使用投影减少数据传输
var patientSummaries = await _context.Patients
    .Where(p => p.IsActive)
    .Select(p => new
    {
        p.Id,
        p.Name,
        RecordCount = p.MedicalRecords.Count()
    })
    .ToListAsync();

// 使用 Include 避免 N+1 查询
var patientsWithRecords = await _context.Patients
    .Include(p => p.MedicalRecords)
        .ThenInclude(r => r.Prescriptions)
    .Where(p => p.IsActive)
    .ToListAsync();
```

## 异常处理

### 1. 异常类型

```csharp
// 业务异常
public class BusinessException : Exception
{
    public string ErrorCode { get; }
    
    public BusinessException(string message, string errorCode = null) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

// 未找到异常
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) 
        : base(message, "NOT_FOUND")
    {
    }
}

// 验证异常
public class ValidationException : BusinessException
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(Dictionary<string, string[]> errors) 
        : base("Validation failed", "VALIDATION_FAILED")
    {
        Errors = errors;
    }
}
```

### 2. 异常处理模式

```csharp
public async Task<PatientDto> UpdatePatientAsync(PatientEditDto dto)
{
    try
    {
        // 验证
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        // 业务逻辑
        var patient = await _repository.GetByIdAsync(dto.Id);
        if (patient == null)
        {
            throw new NotFoundException($"Patient {dto.Id} not found");
        }
        
        // 更新
        _mapper.Map(dto, patient);
        await _repository.UpdateAsync(patient);
        
        return _mapper.Map<PatientDto>(patient);
    }
    catch (BusinessException)
    {
        // 业务异常直接抛出
        throw;
    }
    catch (Exception ex)
    {
        // 记录未预期的异常
        _logger.LogError(ex, "Error updating patient {PatientId}", dto.Id);
        throw new BusinessException("An error occurred while updating patient");
    }
}
```

### 3. 全局异常处理

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
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
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Timestamp = DateTime.UtcNow
        };
        
        switch (exception)
        {
            case ValidationException validationEx:
                response.ErrorCode = "VALIDATION_ERROR";
                response.Message = validationEx.Message;
                response.Data = validationEx.Errors;
                context.Response.StatusCode = 400;
                break;
                
            case NotFoundException notFoundEx:
                response.ErrorCode = "NOT_FOUND";
                response.Message = notFoundEx.Message;
                context.Response.StatusCode = 404;
                break;
                
            case BusinessException businessEx:
                response.ErrorCode = businessEx.ErrorCode ?? "BUSINESS_ERROR";
                response.Message = businessEx.Message;
                context.Response.StatusCode = 400;
                break;
                
            default:
                _logger.LogError(exception, "Unhandled exception occurred");
                response.ErrorCode = "INTERNAL_ERROR";
                response.Message = "An error occurred while processing your request";
                context.Response.StatusCode = 500;
                break;
        }
        
        await context.Response.WriteAsJsonAsync(response);
    }
}
```

## 日志记录

### 1. 日志级别使用

```csharp
public class PatientService
{
    private readonly ILogger<PatientService> _logger;
    
    public async Task<PatientDto> GetPatientByIdAsync(Guid id)
    {
        // Debug: 开发调试信息
        _logger.LogDebug("Getting patient with ID: {PatientId}", id);
        
        try
        {
            var patient = await _repository.GetByIdAsync(id);
            
            if (patient == null)
            {
                // Warning: 业务异常但可以处理
                _logger.LogWarning("Patient not found: {PatientId}", id);
                throw new NotFoundException($"Patient {id} not found");
            }
            
            // Information: 重要业务事件
            _logger.LogInformation("Patient retrieved successfully: {PatientId}", id);
            
            return _mapper.Map<PatientDto>(patient);
        }
        catch (Exception ex)
        {
            // Error: 异常和错误
            _logger.LogError(ex, "Error retrieving patient: {PatientId}", id);
            throw;
        }
    }
}
```

### 2. 结构化日志

```csharp
// 使用结构化日志而不是字符串拼接
// 好的示例
_logger.LogInformation("User {UserId} logged in at {LoginTime}", userId, DateTime.Now);

// 不好的示例
_logger.LogInformation($"User {userId} logged in at {DateTime.Now}");

// 记录复杂对象
_logger.LogInformation("Patient registered: {@Patient}", new
{
    patient.Id,
    patient.Name,
    patient.PhoneNumber,
    RegisterTime = DateTime.Now
});
```

### 3. 性能日志

```csharp
public async Task<IEnumerable<PatientDto>> SearchPatientsAsync(string keyword)
{
    using (_logger.BeginScope("SearchPatients"))
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var results = await _repository.SearchAsync(keyword);
            
            _logger.LogInformation(
                "Patient search completed. Keyword: {Keyword}, ResultCount: {Count}, Duration: {Duration}ms",
                keyword, results.Count(), stopwatch.ElapsedMilliseconds);
                
            return _mapper.Map<IEnumerable<PatientDto>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Patient search failed. Keyword: {Keyword}, Duration: {Duration}ms",
                keyword, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## 测试规范

### 1. 单元测试

```csharp
[TestClass]
public class PatientServiceTests
{
    private Mock<IPatientRepository> _repositoryMock;
    private Mock<IMapper> _mapperMock;
    private Mock<ILogger<PatientService>> _loggerMock;
    private PatientService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<PatientService>>();
        
        _service = new PatientService(
            _repositoryMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }
    
    [TestMethod]
    public async Task GetPatientById_WhenPatientExists_ReturnsPatientDto()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, Name = "Test Patient" };
        var patientDto = new PatientDto { Id = patientId, Name = "Test Patient" };
        
        _repositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(patient);
        _mapperMock.Setup(m => m.Map<PatientDto>(patient))
            .Returns(patientDto);
        
        // Act
        var result = await _service.GetPatientByIdAsync(patientId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(patientId, result.Id);
        Assert.AreEqual("Test Patient", result.Name);
        
        _repositoryMock.Verify(r => r.GetByIdAsync(patientId), Times.Once);
        _mapperMock.Verify(m => m.Map<PatientDto>(patient), Times.Once);
    }
    
    [TestMethod]
    [ExpectedException(typeof(NotFoundException))]
    public async Task GetPatientById_WhenPatientNotExists_ThrowsNotFoundException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        
        _repositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync((Patient)null);
        
        // Act
        await _service.GetPatientByIdAsync(patientId);
        
        // Assert - Exception expected
    }
}
```

### 2. 集成测试

```csharp
[TestClass]
public class PatientControllerIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CreatePatient_ValidData_ReturnsCreatedResult()
    {
        // Arrange
        var client = _factory.CreateClient();
        var createDto = new PatientCreateDto
        {
            Name = "张三",
            IdNumber = "110101199001011234",
            PhoneNumber = "13800138000",
            BirthDate = new DateTime(1990, 1, 1)
        };
        
        // Act
        var response = await client.PostAsJsonAsync("/api/v1/patients", createDto);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PatientDto>>();
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual("张三", result.Data.Name);
        
        // 验证数据库
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var patient = await context.Patients.FindAsync(result.Data.Id);
        Assert.IsNotNull(patient);
        Assert.AreEqual("张三", patient.Name);
    }
}
```

### 3. 测试命名规范

```csharp
// 测试方法命名：被测方法_场景_预期结果
[TestMethod]
public void CalculateAge_BirthdayNotReached_ReturnsCorrectAge() { }

[TestMethod]
public void ValidateIdNumber_InvalidFormat_ReturnsFalse() { }

[TestMethod]
public async Task SavePatient_DuplicateIdNumber_ThrowsBusinessException() { }
```

## 安全编码

### 1. 输入验证

```csharp
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不能为空")
            .MaximumLength(50).WithMessage("姓名长度不能超过50个字符")
            .Matches(@"^[\u4e00-\u9fa5a-zA-Z\s]+$").WithMessage("姓名只能包含中文、英文和空格");
            
        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("身份证号不能为空")
            .Matches(@"^\d{17}[\dXx]$").WithMessage("身份证号格式不正确");
            
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
            
        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("出生日期不能为空")
            .LessThan(DateTime.Today).WithMessage("出生日期必须小于今天");
    }
}
```

### 2. SQL 注入防护

```csharp
// 使用参数化查询
public async Task<IEnumerable<Patient>> SearchPatientsAsync(string keyword)
{
    // 好的示例：使用 LINQ
    return await _context.Patients
        .Where(p => p.Name.Contains(keyword) || p.PhoneNumber.Contains(keyword))
        .ToListAsync();
    
    // 好的示例：使用参数化 SQL
    var sql = "SELECT * FROM Patients WHERE Name LIKE @keyword OR PhoneNumber LIKE @keyword";
    var parameter = new SqlParameter("@keyword", $"%{keyword}%");
    return await _context.Patients.FromSqlRaw(sql, parameter).ToListAsync();
    
    // 不好的示例：字符串拼接
    // var sql = $"SELECT * FROM Patients WHERE Name LIKE '%{keyword}%'";
    // return await _context.Patients.FromSqlRaw(sql).ToListAsync();
}
```

### 3. 敏感数据处理

```csharp
// 不记录敏感信息
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    
    [JsonIgnore] // 不序列化
    public string IdNumber { get; set; }
    
    [JsonPropertyName("idNumber")]
    public string MaskedIdNumber => IdNumber?.Length > 4 
        ? $"{IdNumber.Substring(0, 4)}****{IdNumber.Substring(IdNumber.Length - 4)}" 
        : "****";
}

// 日志脱敏
_logger.LogInformation("User login: {Username}", username);
// 不要记录密码
// _logger.LogInformation($"User login: {username}, password: {password}");
```

### 4. 权限验证

```csharp
[Authorize(Roles = "Admin,Doctor")]
public class PatientController : BaseController
{
    [HttpGet("{id}")]
    [Authorize(Policy = "PatientReadPolicy")]
    public async Task<IActionResult> GetPatient(Guid id)
    {
        // 检查数据权限
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!await _authService.CanAccessPatient(currentUserId, id))
        {
            return Forbid();
        }
        
        var patient = await _patientService.GetPatientByIdAsync(id);
        return Ok(patient);
    }
}
```

## 性能优化

### 1. 异步编程

```csharp
// 使用异步方法提高并发性能
public async Task<DashboardDto> GetDashboardDataAsync()
{
    // 并行执行多个异步操作
    var patientCountTask = _patientRepository.CountAsync();
    var todayRegistrationsTask = _registrationRepository.GetTodayCountAsync();
    var pendingBillsTask = _billingRepository.GetPendingCountAsync();
    
    await Task.WhenAll(patientCountTask, todayRegistrationsTask, pendingBillsTask);
    
    return new DashboardDto
    {
        PatientCount = await patientCountTask,
        TodayRegistrations = await todayRegistrationsTask,
        PendingBills = await pendingBillsTask
    };
}
```

### 2. 缓存使用

```csharp
public class CachedPatientService : IPatientService
{
    private readonly IPatientService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedPatientService> _logger;
    
    public async Task<PatientDto> GetPatientByIdAsync(Guid id)
    {
        var cacheKey = $"patient_{id}";
        
        if (_cache.TryGetValue<PatientDto>(cacheKey, out var cachedPatient))
        {
            _logger.LogDebug("Patient retrieved from cache: {PatientId}", id);
            return cachedPatient;
        }
        
        var patient = await _innerService.GetPatientByIdAsync(id);
        
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
            
        _cache.Set(cacheKey, patient, cacheOptions);
        
        return patient;
    }
}
```

### 3. 数据库优化

```csharp
// 分页查询
public async Task<PagedResult<PatientDto>> GetPatientsAsync(PageRequest request)
{
    var query = _context.Patients
        .Where(p => p.IsActive)
        .AsNoTracking();
    
    // 计算总数
    var totalCount = await query.CountAsync();
    
    // 分页查询
    var patients = await query
        .OrderBy(p => p.Name)
        .Skip((request.PageNumber - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(p => new PatientDto
        {
            Id = p.Id,
            Name = p.Name,
            PhoneNumber = p.PhoneNumber
        })
        .ToListAsync();
    
    return new PagedResult<PatientDto>
    {
        Items = patients,
        TotalCount = totalCount,
        PageNumber = request.PageNumber,
        PageSize = request.PageSize
    };
}
```

## 代码审查

### 1. 代码审查清单

- [ ] 代码是否遵循命名规范？
- [ ] 是否有适当的注释和文档？
- [ ] 错误处理是否完善？
- [ ] 是否有单元测试覆盖？
- [ ] 是否存在潜在的性能问题？
- [ ] 是否有安全漏洞？
- [ ] 代码是否易于理解和维护？
- [ ] 是否遵循 SOLID 原则？
- [ ] 日志记录是否充分？
- [ ] 是否有重复代码？

### 2. 代码审查流程

1. **自我审查**：提交前自行检查代码
2. **自动化检查**：运行代码分析工具
3. **同行评审**：至少一名团队成员审查
4. **反馈处理**：及时响应和修改
5. **合并批准**：通过审查后合并

### 3. 代码审查工具

- **StyleCop**: C# 代码风格检查
- **SonarQube**: 代码质量分析
- **ReSharper**: 代码分析和重构
- **Visual Studio Code Analysis**: 内置代码分析

## 工具配置

### 1. EditorConfig

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx}]
indent_style = space
indent_size = 4

[*.{json,xml,yml,yaml}]
indent_style = space
indent_size = 2
```

### 2. 代码分析规则

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

## 总结

遵循这些编码标准将帮助我们：

1. 提高代码质量和一致性
2. 减少错误和技术债务
3. 提升团队协作效率
4. 简化代码维护和扩展
5. 确保系统的安全性和性能

所有团队成员都应该熟悉并遵循这些标准。在实际开发中，如果遇到标准中未涵盖的情况，应该与团队讨论并更新本文档。