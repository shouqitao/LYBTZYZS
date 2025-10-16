# 共享开发指南

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**维护团队**：架构组、全栈开发组  

## 🎯 共享开发导航

凌隐宝堂中医诊所管理系统共享层是连接Server端和Client端的桥梁，提供跨层共享的基础设施、数据模型、业务接口和技术标准。共享层的开发质量直接影响整个系统的稳定性和一致性。

### 📋 共享层技术栈

| 技术 | 版本 | 用途 | 说明 |
|------|------|------|------|
| **.NET** | 8.0 | 运行时 | 跨平台运行时 |
| **Entity Framework Core** | 8.0 | ORM框架 | 数据库操作 |
| **AutoMapper** | 12.0 | 对象映射 | DTO与实体转换 |
| **FluentValidation** | 11.0 | 数据验证 | 业务规则验证 |
| **Serilog** | 3.0 | 日志框架 | 结构化日志记录 |
| **Dapper** | 2.0 | 微ORM | 高性能数据访问 |
| **Newtonsoft.Json** | 13.0 | JSON序列化 | 数据序列化 |

## 🏗️ 共享层架构设计

### 核心组件结构
```
LYBT.Shared (共享库)
├── Models/              # 数据模型层
│   ├── Entities/        # 业务实体
│   ├── DTOs/           # 数据传输对象
│   ├── Requests/       # 请求模型
│   ├── Responses/      # 响应模型
│   └── ViewModels/     # 视图模型
├── Interfaces/          # 接口定义层
│   ├── Services/       # 业务服务接口
│   ├── Repositories/   # 仓储接口
│   ├── Infrastructure/ # 基础设施接口
│   └── Common/         # 通用接口
├── Infrastructure/      # 基础设施层
│   ├── Data/           # 数据访问组件
│   ├── Caching/        # 缓存组件
│   ├── Logging/        # 日志组件
│   ├── Security/       # 安全组件
│   ├── Validation/     # 验证组件
│   └── Http/           # HTTP组件
├── Utilities/           # 工具类层
│   ├── Extensions/     # 扩展方法
│   ├── Helpers/        # 辅助类
│   ├── Converters/     # 转换器
│   └── Formatters/     # 格式化器
├── Constants/           # 常量定义层
│   ├── SystemConstants.cs
│   ├── BusinessConstants.cs
│   └── ValidationConstants.cs
└── Enums/              # 枚举类型层
    ├── Gender.cs
    ├── MedicalCaseStatus.cs
    ├── PrescriptionStatus.cs
    └── Permission.cs
```

## 📝 数据模型开发

### 1. 实体模型开发
```csharp
// Models/Entities/BaseEntity.cs
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// Models/Entities/Patient.cs
[Table("Patients")]
public class Patient : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; }

    [Required]
    [StringLength(10)]
    public string Gender { get; set; }

    [Required]
    public DateTime BirthDate { get; set; }

    [Required]
    [StringLength(11)]
    [Phone]
    public string Phone { get; set; }

    [StringLength(200)]
    public string Address { get; set; }

    [StringLength(18)]
    [RegularExpression(@"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$")]
    public string IdCard { get; set; }

    [StringLength(1000)]
    public string MedicalHistory { get; set; }

    [StringLength(500)]
    public string Allergies { get; set; }

    [StringLength(500)]
    public string Notes { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = PatientStatus.Active;

    // 导航属性
    public virtual ICollection<MedicalCase> MedicalCases { get; set; }
    public virtual ICollection<Prescription> Prescriptions { get; set; }

    // 计算属性
    [NotMapped]
    public int Age => DateTime.Today.Year - BirthDate.Year - 
        (BirthDate.Date > DateTime.Today.AddYears(-DateTime.Today.Year - BirthDate.Year) ? 1 : 0);

    [NotMapped]
    public string AgeText => $"{Age}岁";

    [NotMapped]
    public string MaskedPhone => Phone?.Length > 7 ? 
        Phone.Substring(0, 3) + "****" + Phone.Substring(7) : Phone;

    [NotMapped]
    public string MaskedIdCard => IdCard?.Length > 8 ? 
        IdCard.Substring(0, 4) + "**********" + IdCard.Substring(IdCard.Length - 4) : IdCard;
}
```

### 2. DTO模型开发
```csharp
// Models/DTOs/PatientDto.cs
public class PatientDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Gender { get; set; }

    public DateTime BirthDate { get; set; }

    public string Phone { get; set; }

    public string Address { get; set; }

    public string IdCard { get; set; }

    public string MedicalHistory { get; set; }

    public string Allergies { get; set; }

    public string Notes { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // 计算属性
    public int Age { get; set; }

    public string AgeText { get; set; }

    public string MaskedPhone { get; set; }

    public string MaskedIdCard { get; set; }

    // 统计信息
    public int MedicalCaseCount { get; set; }

    public int PrescriptionCount { get; set; }

    public DateTime? LastVisitDate { get; set; }

    public decimal TotalAmount { get; set; }
}
```

### 3. 请求模型开发
```csharp
// Models/Requests/PatientCreateRequest.cs
public class PatientCreateRequest
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name { get; set; }

    [Required(ErrorMessage = "性别不能为空")]
    [StringLength(10, ErrorMessage = "性别长度不能超过10个字符")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "出生日期不能为空")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "手机号必须是11位")]
    public string Phone { get; set; }

    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    public string Address { get; set; }

    [StringLength(18, MinimumLength = 18, ErrorMessage = "身份证号必须是18位")]
    [RegularExpression(@"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$", 
                     ErrorMessage = "身份证号格式不正确")]
    public string IdCard { get; set; }

    [StringLength(1000, ErrorMessage = "病史长度不能超过1000个字符")]
    public string MedicalHistory { get; set; }

    [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
    public string Allergies { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string Notes { get; set; }
}

// Models/Requests/PatientUpdateRequest.cs
public class PatientUpdateRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name { get; set; }

    [Required(ErrorMessage = "性别不能为空")]
    [StringLength(10, ErrorMessage = "性别长度不能超过10个字符")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "出生日期不能为空")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "手机号必须是11位")]
    public string Phone { get; set; }

    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    public string Address { get; set; }

    [StringLength(18, MinimumLength = 18, ErrorMessage = "身份证号必须是18位")]
    [RegularExpression(@"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$", 
                     ErrorMessage = "身份证号格式不正确")]
    public string IdCard { get; set; }

    [StringLength(1000, ErrorMessage = "病史长度不能超过1000个字符")]
    public string MedicalHistory { get; set; }

    [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
    public string Allergies { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string Notes { get; set; }
}
```

### 4. 响应模型开发
```csharp
// Models/Responses/ApiResponse.cs
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int Code { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Success(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Code = 200
        };
    }

    public static ApiResponse<T> Error(string message, int code = 400, List<string> errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Code = code,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse<T> ValidationError(List<ValidationError> validationErrors)
    {
        var errors = validationErrors.Select(e => e.ErrorMessage).ToList();
        return new ApiResponse<T>
        {
            Success = false,
            Message = "数据验证失败",
            Code = 422,
            Errors = errors
        };
    }
}

// Models/Responses/PagedResult.cs
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    public static PagedResult<T> Create(IEnumerable<T> data, int pageIndex, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResult<T>
        {
            Data = data,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = pageIndex > 1,
            HasNextPage = pageIndex < totalPages
        };
    }
}
```

## 🔌 接口定义开发

### 1. 服务接口定义
```csharp
// Interfaces/Services/IPatientService.cs
public interface IPatientService
{
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(int id);
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(int pageIndex = 1, int pageSize = 20, string keyword = null);
    Task<ApiResponse<IEnumerable<PatientDto>>> SearchPatientsAsync(string keyword);
    Task<ApiResponse<PatientDto>> CreatePatientAsync(PatientCreateRequest request);
    Task<ApiResponse<PatientDto>> UpdatePatientAsync(int id, PatientUpdateRequest request);
    Task<ApiResponse<bool>> DeletePatientAsync(int id);
    Task<ApiResponse<bool>> CheckPhoneExistsAsync(string phone, int? excludeId = null);
    Task<ApiResponse<bool>> CheckIdCardExistsAsync(string idCard, int? excludeId = null);
    Task<ApiResponse<byte[]>> ExportPatientsAsync(string keyword = null);
    Task<ApiResponse<bool>> ImportPatientsAsync(byte[] fileData);
}

// Interfaces/Services/IAuthService.cs
public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ApiResponse<bool>> LogoutAsync(int userId);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync();
    Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);
}

// Interfaces/Services/IConsultationService.cs
public interface IConsultationService
{
    Task<ApiResponse<ConsultationDto>> GetConsultationByIdAsync(int id);
    Task<ApiResponse<PagedResult<ConsultationDto>>> GetConsultationsAsync(int patientId, int pageIndex = 1, int pageSize = 20);
    Task<ApiResponse<ConsultationDto>> CreateConsultationAsync(ConsultationCreateRequest request);
    Task<ApiResponse<ConsultationDto>> UpdateConsultationAsync(int id, ConsultationUpdateRequest request);
    Task<ApiResponse<bool>> DeleteConsultationAsync(int id);
    Task<ApiResponse<IEnumerable<ConsultationDto>>> GetConsultationsByPatientIdAsync(int patientId);
    Task<ApiResponse<ConsultationDto>> GetLatestConsultationByPatientIdAsync(int patientId);
}
```

### 2. 仓储接口定义
```csharp
// Interfaces/Repositories/IRepository.cs
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
}

// Interfaces/Repositories/IPatientRepository.cs
public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient> GetByPhoneAsync(string phone);
    Task<Patient> GetByIdCardAsync(string idCard);
    Task<Patient> GetByNameAsync(string name);
    Task<IEnumerable<Patient>> GetPagedAsync(int pageIndex, int pageSize, string keyword = null);
    Task<int> CountAsync(string keyword = null);
    Task<bool> IsPhoneExistsAsync(string phone, int? excludeId = null);
    Task<bool> IsIdCardExistsAsync(string idCard, int? excludeId = null);
    Task<bool> HasMedicalCasesAsync(int patientId);
    Task<bool> HasPrescriptionsAsync(int patientId);
    Task<Patient> GetWithDetailsAsync(int id);
}

// Interfaces/Repositories/IConsultationRepository.cs
public interface IConsultationRepository : IRepository<Consultation>
{
    Task<IEnumerable<Consultation>> GetByPatientIdAsync(int patientId);
    Task<Consultation> GetLatestByPatientIdAsync(int patientId);
    Task<IEnumerable<Consultation>> GetPagedAsync(int pageIndex, int pageSize, int? patientId = null);
    Task<int> CountAsync(int? patientId = null);
    Task<Consultation> GetWithDetailsAsync(int id);
}
```

### 3. 通用接口定义
```csharp
// Interfaces/Common/IValidationService.cs
public interface IValidationService
{
    ValidationResult Validate<T>(T entity);
    ValidationResult ValidateProperty<T, TProperty>(T entity, Expression<Func<T, TProperty>> property);
    Task<ValidationResult> ValidateAsync<T>(T entity);
}

// Interfaces/Common/ICacheService.cs
public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task ClearAsync();
    Task<IEnumerable<string>> GetKeysAsync(string pattern = null);
}

// Interfaces/Common/ITokenService.cs
public interface ITokenService
{
    Task<string> GetAccessTokenAsync();
    Task<string> GetRefreshTokenAsync();
    Task<UserDto> GetCurrentUserAsync();
    Task SaveTokensAsync(string accessToken, string refreshToken, UserDto user);
    Task ClearTokensAsync();
    Task<bool> IsTokenExpiredAsync();
}

// Interfaces/Common/IEventAggregator.cs
public interface IEventAggregator
{
    void Publish<T>(T eventData) where T : class;
    void Subscribe<T>(Action<T> handler) where T : class;
    void Unsubscribe<T>(Action<T> handler) where T : class;
}
```

## 🏗️ 基础设施开发

### 1. 数据访问组件
```csharp
// Infrastructure/Data/RepositoryBase.cs
public abstract class RepositoryBase<T> : IRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<RepositoryBase<T>> _logger;

    protected RepositoryBase(DbContext context, ILogger<RepositoryBase<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public virtual async Task<T> GetByIdAsync(int id)
    {
        try
        {
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID获取实体失败，ID: {Id}, 类型: {Type}", id, typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _dbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有实体失败，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据条件查找实体失败，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加实体失败，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新实体失败，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task DeleteAsync(T entity)
    {
        try
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除实体失败，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync()
    {
        try
        {
            return await _dbSet.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "统计实体数量失败，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            return await _dbSet.CountAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据条件统计实体数量失败，类型: {Type}", typeof(T).Name);
            throw;
        }
    }
}
```

### 2. 缓存组件
```csharp
// Infrastructure/Caching/MemoryCacheService.cs
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out T value))
            {
                _logger.LogDebug("缓存命中，键: {Key}", key);
                return value;
            }

            _logger.LogDebug("缓存未命中，键: {Key}", key);
            return default;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var options = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiry.Value;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            _cache.Set(key, value, options);
            _logger.LogDebug("缓存设置成功，键: {Key}", key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveAsync(string key)
    {
        await _semaphore.WaitAsync();
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("缓存删除成功，键: {Key}", key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _cache.TryGetValue(key, out _);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
            _logger.LogDebug("缓存清空成功");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string pattern = null)
    {
        // MemoryCache不支持获取所有键，这里返回空集合
        // 在实际项目中，可以使用Redis等其他缓存系统
        return await Task.FromResult(Enumerable.Empty<string>());
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
```

### 3. 验证组件
```csharp
// Infrastructure/Validation/FluentValidationService.cs
public class FluentValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FluentValidationService> _logger;

    public FluentValidationService(IServiceProvider serviceProvider, ILogger<FluentValidationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ValidationResult Validate<T>(T entity)
    {
        try
        {
            var validator = _serviceProvider.GetService<IValidator<T>>();
            if (validator == null)
            {
                _logger.LogWarning("未找到类型 {Type} 的验证器", typeof(T).Name);
                return ValidationResult.Success;
            }

            var result = validator.Validate(entity);
            return ConvertToValidationResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证过程中发生错误，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    public ValidationResult ValidateProperty<T, TProperty>(T entity, Expression<Func<T, TProperty>> property)
    {
        try
        {
            var validator = _serviceProvider.GetService<IValidator<T>>();
            if (validator == null)
            {
                return ValidationResult.Success;
            }

            var result = validator.Validate(entity, options => 
                options.IncludeProperties(property.GetMemberName()));
            return ConvertToValidationResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "属性验证过程中发生错误，类型: {Type}, 属性: {Property}", 
                typeof(T).Name, property.GetMemberName());
            throw;
        }
    }

    public async Task<ValidationResult> ValidateAsync<T>(T entity)
    {
        try
        {
            var validator = _serviceProvider.GetService<IValidator<T>>();
            if (validator == null)
            {
                _logger.LogWarning("未找到类型 {Type} 的验证器", typeof(T).Name);
                return ValidationResult.Success;
            }

            var result = await validator.ValidateAsync(entity);
            return ConvertToValidationResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步验证过程中发生错误，类型: {Type}", typeof(T).Name);
            throw;
        }
    }

    private static ValidationResult ConvertToValidationResult(ValidationResult result)
    {
        if (result.IsValid)
        {
            return ValidationResult.Success;
        }

        var errors = result.Errors.Select(e => new ValidationError
        {
            PropertyName = e.PropertyName,
            ErrorMessage = e.ErrorMessage,
            AttemptedValue = e.AttemptedValue
        }).ToList();

        return ValidationResult.Failure(errors);
    }
}
```

## 🛠️ 工具类开发

### 1. 扩展方法
```csharp
// Utilities/Extensions/StringExtensions.cs
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string value)
    {
        return string.IsNullOrEmpty(value);
    }

    public static bool IsNullOrWhiteSpace(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static string SafeTrim(this string value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static string MaskPhone(this string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7)
            return phone;

        return phone.Substring(0, 3) + "****" + phone.Substring(7);
    }

    public static string MaskIdCard(this string idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard) || idCard.Length < 8)
            return idCard;

        return idCard.Substring(0, 4) + "**********" + idCard.Substring(idCard.Length - 4);
    }

    public static string ToPinyin(this string chineseText)
    {
        // 实现中文转拼音的逻辑
        try
        {
            var pinyin = new PinyinHelper();
            return pinyin.GetPinyin(chineseText);
        }
        catch
        {
            return chineseText;
        }
    }

    public static bool IsValidPhone(this string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        return Regex.IsMatch(phone, @"^1[3-9]\d{9}$");
    }

    public static bool IsValidIdCard(this string idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard))
            return false;

        return Regex.IsMatch(idCard, @"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$");
    }
}

// Utilities/Extensions/DateTimeExtensions.cs
public static class DateTimeExtensions
{
    public static int CalculateAge(this DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    public static string ToAgeString(this DateTime birthDate)
    {
        var age = birthDate.CalculateAge();
        return $"{age}岁";
    }

    public static string ToChineseDateString(this DateTime date)
    {
        return date.ToString("yyyy年MM月dd日");
    }

    public static string ToChineseDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy年MM月dd日 HH:mm");
    }

    public static bool IsWeekend(this DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    public static DateTime GetFirstDayOfMonth(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    public static DateTime GetLastDayOfMonth(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
    }

    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "刚刚";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}分钟前";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}小时前";
        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays}天前";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)}个月前";
        
        return $"{(int)(timeSpan.TotalDays / 365)}年前";
    }
}
```

### 2. 辅助类
```csharp
// Utilities/Helpers/IdGeneratorHelper.cs
public static class IdGeneratorHelper
{
    private static readonly ConcurrentDictionary<string, long> _counters = new();

    public static string GeneratePatientId()
    {
        var prefix = $"P{DateTime.Now:yyyyMMdd}";
        var counter = _counters.AddOrUpdate(prefix, 1, (_, v) => v + 1);
        return $"{prefix}{counter:D4}";
    }

    public static string GenerateMedicalCaseId()
    {
        var prefix = $"M{DateTime.Now:yyyyMMdd}";
        var counter = _counters.AddOrUpdate(prefix, 1, (_, v) => v + 1);
        return $"{prefix}{counter:D4}";
    }

    public static string GeneratePrescriptionId()
    {
        var prefix = $"R{DateTime.Now:yyyyMMdd}";
        var counter = _counters.AddOrUpdate(prefix, 1, (_, v) => v + 1);
        return $"{prefix}{counter:D4}";
    }

    public static string GenerateConsultationId()
    {
        var prefix = $"C{DateTime.Now:yyyyMMdd}";
        var counter = _counters.AddOrUpdate(prefix, 1, (_, v) => v + 1);
        return $"{prefix}{counter:D4}";
    }
}

// Utilities/Helpers/FileHelper.cs
public static class FileHelper
{
    public static async Task<byte[]> ReadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        return await File.ReadAllBytesAsync(filePath);
    }

    public static async Task SaveFileAsync(string filePath, byte[] data)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(filePath, data);
    }

    public static string GetFileExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    public static bool IsAllowedExtension(string fileName, string[] allowedExtensions)
    {
        var extension = GetFileExtension(fileName);
        return allowedExtensions.Contains(extension);
    }

    public static string GenerateUniqueFileName(string originalFileName, string directory = null)
    {
        var extension = GetFileExtension(originalFileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        
        if (!string.IsNullOrEmpty(directory))
        {
            return Path.Combine(directory, fileName);
        }
        
        return fileName;
    }
}
```

## 🧪 单元测试

### 1. 模型测试
```csharp
// Tests/Models/PatientTests.cs
[TestFixture]
public class PatientTests
{
    [Test]
    public void Patient_WhenCreated_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var patient = new Patient();

        // Assert
        Assert.That(patient.Status, Is.EqualTo(PatientStatus.Active));
        Assert.That(patient.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
        Assert.That(patient.IsDeleted, Is.False);
    }

    [Test]
    public void Age_WhenBirthDateProvided_ShouldCalculateCorrectly()
    {
        // Arrange
        var patient = new Patient
        {
            BirthDate = new DateTime(1990, 5, 15)
        };
        var today = new DateTime(2025, 10, 15);

        // Act
        var age = patient.Age;

        // Assert
        Assert.That(age, Is.EqualTo(35));
    }

    [Test]
    public void MaskedPhone_WhenPhoneProvided_ShouldMaskCorrectly()
    {
        // Arrange
        var patient = new Patient
        {
            Phone = "13800138000"
        };

        // Act
        var maskedPhone = patient.MaskedPhone;

        // Assert
        Assert.That(maskedPhone, Is.EqualTo("138****8000"));
    }

    [Test]
    public void MaskedIdCard_WhenIdCardProvided_ShouldMaskCorrectly()
    {
        // Arrange
        var patient = new Patient
        {
            IdCard = "110101199005153456"
        };

        // Act
        var maskedIdCard = patient.MaskedIdCard;

        // Assert
        Assert.That(maskedIdCard, Is.EqualTo("1101**********3456"));
    }
}
```

### 2. 扩展方法测试
```csharp
// Tests/Extensions/StringExtensionsTests.cs
[TestFixture]
public class StringExtensionsTests
{
    [Test]
    public void IsNullOrEmpty_WhenStringIsNull_ShouldReturnTrue()
    {
        // Arrange
        string value = null;

        // Act
        var result = value.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrEmpty_WhenStringIsEmpty_ShouldReturnTrue()
    {
        // Arrange
        var value = "";

        // Act
        var result = value.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNullOrEmpty_WhenStringHasValue_ShouldReturnFalse()
    {
        // Arrange
        var value = "test";

        // Act
        var result = value.IsNullOrEmpty();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void MaskPhone_WhenPhoneIsValid_ShouldMaskCorrectly()
    {
        // Arrange
        var phone = "13800138000";

        // Act
        var result = phone.MaskPhone();

        // Assert
        Assert.That(result, Is.EqualTo("138****8000"));
    }

    [Test]
    public void IsValidPhone_WhenPhoneIsValid_ShouldReturnTrue()
    {
        // Arrange
        var phone = "13800138000";

        // Act
        var result = phone.IsValidPhone();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsValidPhone_WhenPhoneIsInvalid_ShouldReturnFalse()
    {
        // Arrange
        var phone = "12345678901";

        // Act
        var result = phone.IsValidPhone();

        // Assert
        Assert.That(result, Is.False);
    }
}
```

## 📊 性能优化

### 1. 对象映射优化
```csharp
// Infrastructure/Mapping/MappingProfile.cs
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Patient映射
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.BirthDate)))
            .ForMember(dest => dest.AgeText, opt => opt.MapFrom(src => $"{CalculateAge(src.BirthDate)}岁"))
            .ForMember(dest => dest.MaskedPhone, opt => opt.MapFrom(src => MaskPhone(src.Phone)))
            .ForMember(dest => dest.MaskedIdCard, opt => opt.MapFrom(src => MaskIdCard(src.IdCard)))
            .ForMember(dest => dest.MedicalCaseCount, opt => opt.Ignore())
            .ForMember(dest => dest.PrescriptionCount, opt => opt.Ignore())
            .ForMember(dest => dest.LastVisitDate, opt => opt.Ignore())
            .ForMember(dest => dest.TotalAmount, opt => opt.Ignore());

        CreateMap<PatientCreateRequest, Patient>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

        CreateMap<PatientUpdateRequest, Patient>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

        // 其他实体映射...
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7)
            return phone;
        return phone.Substring(0, 3) + "****" + phone.Substring(7);
    }

    private static string MaskIdCard(string idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard) || idCard.Length < 8)
            return idCard;
        return idCard.Substring(0, 4) + "**********" + idCard.Substring(idCard.Length - 4);
    }
}
```

### 2. 验证优化
```csharp
// Infrastructure/Validation/PatientValidator.cs
public class PatientCreateValidator : AbstractValidator<PatientCreateRequest>
{
    public PatientCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("姓名长度不能超过50个字符")
            .Matches(@"^[\u4e00-\u9fa5]+$").WithMessage("姓名只能包含中文字符");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("性别不能为空")
            .Must(BeValidGender).WithMessage("性别必须是男或女");

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("出生日期不能为空")
            .Must(BeValidBirthDate).WithMessage("出生日期不能是未来日期，且不能早于100年前");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");

        RuleFor(x => x.IdCard)
            .Matches(@"^[1-9]\d{5}(18|19|20)\d{2}((0[1-9])|(1[0-2]))(([0-2][1-9])|10|20|30|31)\d{3}[0-9Xx]$")
            .When(x => !string.IsNullOrEmpty(x.IdCard))
            .WithMessage("身份证号格式不正确");

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("地址长度不能超过200个字符");

        RuleFor(x => x.MedicalHistory)
            .MaximumLength(1000).WithMessage("病史长度不能超过1000个字符");

        RuleFor(x => x.Allergies)
            .MaximumLength(500).WithMessage("过敏史长度不能超过500个字符");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("备注长度不能超过500个字符");
    }

    private bool BeValidGender(string gender)
    {
        return gender == "男" || gender == "女";
    }

    private bool BeValidBirthDate(DateTime birthDate)
    {
        var today = DateTime.Today;
        var minDate = today.AddYears(-100);
        return birthDate <= today && birthDate >= minDate;
    }
}
```

## 🔗 相关文档

- **[架构总览](../../architecture/README.md)** - 三层对齐架构设计原理
- **[共享架构](../../architecture/shared/README.md)** - 跨层组件和标准
- **[开发指南总览](../README.md)** - 开发规范和流程指导
- **[Server端开发指南](../server/README.md)** - 后端开发规范和实践
- **[Client端开发指南](../client/README.md)** - WPF客户端开发指南

---

**文档维护**：架构组、全栈开发组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核