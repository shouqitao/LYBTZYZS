# 共享架构指南

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**对应代码层**：LYBT.Shared  

## 🏗️ 共享架构设计

凌隐宝堂中医诊所共享架构是连接Server端和Client端的桥梁，提供跨层共享的基础设施、数据模型、业务接口和技术标准。

```
LYBT.Shared (共享层)
├── Models/             # 数据模型和实体
├── Interfaces/         # 业务接口定义
├── Infrastructure/     # 基础设施组件
├── Utilities/          # 工具类和扩展
├── Constants/          # 常量定义
└── Enums/             # 枚举类型
```

## 📐 核心组件详解

### 1. Models - 数据模型层
**职责**：定义业务实体、数据传输对象、验证规则

**核心组件**：
- `Entities/` - 业务实体模型
- `DTOs/` - 数据传输对象
- `Requests/` - 请求模型
- `Responses/` - 响应模型
- `ViewModels/` - 视图模型

**代码示例**：
```csharp
// Models/Entities/Patient.cs
public class Patient : BaseEntity
{
    public string Name { get; set; }
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string IdCard { get; set; }
    public string MedicalHistory { get; set; }
    public string Allergies { get; set; }
    public string Notes { get; set; }
    
    // 导航属性
    public virtual ICollection<MedicalCase> MedicalCases { get; set; }
    public virtual ICollection<Prescription> Prescriptions { get; set; }
    
    // 计算属性
    public int Age => DateTime.Today.Year - BirthDate.Year;
    public string AgeText => $"{Age}岁";
}

// Models/DTOs/PatientDto.cs
public class PatientDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public int Age { get; set; }
    public int MedicalCaseCount { get; set; }
    public int PrescriptionCount { get; set; }
    public string LastVisitDate { get; set; }
}

// Models/Requests/PatientCreateRequest.cs
public class PatientCreateRequest
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "性别不能为空")]
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

// Models/Responses/ApiResponse.cs
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int Code { get; set; }
    public DateTime Timestamp { get; set; }
    
    public static ApiResponse<T> Success(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Code = 200,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public static ApiResponse<T> Error(string message, int code = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Code = code,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

### 2. Interfaces - 接口定义层
**职责**：定义业务服务接口、仓储接口、通用接口

**核心组件**：
- `Services/` - 业务服务接口
- `Repositories/` - 仓储接口
- `Common/` - 通用接口

**代码示例**：
```csharp
// Interfaces/Services/IPatientService.cs
public interface IPatientService
{
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(int id);
    Task<ApiResponse<IEnumerable<PatientDto>>> GetPatientsAsync(int pageIndex = 1, int pageSize = 20);
    Task<ApiResponse<IEnumerable<PatientDto>>> SearchPatientsAsync(string keyword);
    Task<ApiResponse<PatientDto>> CreatePatientAsync(PatientCreateRequest request);
    Task<ApiResponse<PatientDto>> UpdatePatientAsync(int id, PatientUpdateRequest request);
    Task<ApiResponse<bool>> DeletePatientAsync(int id);
    Task<ApiResponse<bool>> CheckPhoneExistsAsync(string phone, int? excludeId = null);
    Task<ApiResponse<bool>> CheckIdCardExistsAsync(string idCard, int? excludeId = null);
}

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
    Task<IEnumerable<Patient>> GetByNameAsync(string name);
    Task<IEnumerable<Patient>> GetPatientsWithMedicalCasesAsync(int pageIndex = 1, int pageSize = 20);
    Task<bool> IsPhoneExistsAsync(string phone, int? excludeId = null);
    Task<bool> IsIdCardExistsAsync(string idCard, int? excludeId = null);
}

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
}
```

### 3. Infrastructure - 基础设施层
**职责**：提供通用基础设施组件、数据访问、缓存、日志等

**核心组件**：
- `Data/` - 数据访问组件
- `Caching/` - 缓存组件
- `Logging/` - 日志组件
- `Security/` - 安全组件
- `Validation/` - 验证组件

**代码示例**：
```csharp
// Infrastructure/Data/RepositoryBase.cs
public abstract class RepositoryBase<T> : IRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;
    
    protected RepositoryBase(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public virtual async Task<T> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }
    
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }
    
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }
    
    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }
    
    public virtual async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
    
    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
    
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }
}

// Infrastructure/Caching/MemoryCacheService.cs
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    
    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<T> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out T value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value;
        }
        
        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry.Value;
        }
        
        _cache.Set(key, value, options);
        _logger.LogDebug("Cache set for key: {Key}", key);
    }
    
    public async Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache removed for key: {Key}", key);
    }
    
    public async Task<bool> ExistsAsync(string key)
    {
        return _cache.TryGetValue(key, out _);
    }
    
    public async Task ClearAsync()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
        _logger.LogDebug("Cache cleared");
    }
}

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
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator == null)
        {
            _logger.LogWarning("No validator found for type: {Type}", typeof(T).Name);
            return ValidationResult.Success;
        }
        
        var result = validator.Validate(entity);
        return ConvertToValidationResult(result);
    }
    
    public ValidationResult ValidateProperty<T, TProperty>(T entity, Expression<Func<T, TProperty>> property)
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
    
    public async Task<ValidationResult> ValidateAsync<T>(T entity)
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator == null)
        {
            return ValidationResult.Success;
        }
        
        var result = await validator.ValidateAsync(entity);
        return ConvertToValidationResult(result);
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

### 4. Utilities - 工具类层
**职责**：提供通用工具类、扩展方法、辅助函数

**核心组件**：
- `Extensions/` - 扩展方法
- `Helpers/` - 辅助类
- `Converters/` - 转换器
- `Formatters/` - 格式化器

**代码示例**：
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
        return PinyinHelper.GetPinyin(chineseText);
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
}

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

// Utilities/Converters/EnumConverter.cs
public static class EnumConverter
{
    public static string ToDescriptionString(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
    
    public static T FromDescriptionString<T>(string description) where T : Enum
    {
        var type = typeof(T);
        var fields = type.GetFields();
        
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute?.Description == description)
            {
                return (T)Enum.Parse(type, field.Name);
            }
        }
        
        throw new ArgumentException($"No enum value with description '{description}' found in {type.Name}");
    }
}
```

### 5. Constants - 常量定义层
**职责**：定义系统常量、配置常量、业务常量

**代码示例**：
```csharp
// Constants/SystemConstants.cs
public static class SystemConstants
{
    public const string APPLICATION_NAME = "凌隐宝堂中医诊所管理系统";
    public const string APPLICATION_VERSION = "5.0.0";
    public const string DEFAULT_CULTURE = "zh-CN";
    public const string DEFAULT_TIMEZONE = "Asia/Shanghai";
    
    // 缓存键
    public static class CacheKeys
    {
        public const string PATIENT_LIST = "patient:list";
        public const string PATIENT_DETAIL = "patient:detail:";
        public const string HERB_LIST = "herb:list";
        public const string FORMULA_LIST = "formula:list";
        public const string USER_PERMISSIONS = "user:permissions:";
        public const string USER_INFO = "user:info:";
    }
    
    // 缓存过期时间
    public static class CacheExpiry
    {
        public static readonly TimeSpan SHORT = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan MEDIUM = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan LONG = TimeSpan.FromHours(2);
        public static readonly TimeSpan DAILY = TimeSpan.FromDays(1);
    }
    
    // 分页默认值
    public static class Pagination
    {
        public const int DEFAULT_PAGE_SIZE = 20;
        public const int MAX_PAGE_SIZE = 100;
        public const int DEFAULT_PAGE_INDEX = 1;
    }
    
    // 文件上传限制
    public static class FileUpload
    {
        public const long MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
        public const string[] ALLOWED_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
    }
}

// Constants/BusinessConstants.cs
public static class BusinessConstants
{
    // 患者状态
    public static class PatientStatus
    {
        public const string ACTIVE = "Active";
        public const string INACTIVE = "Inactive";
        public const string DECEASED = "Deceased";
    }
    
    // 医案状态
    public static class MedicalCaseStatus
    {
        public const string NEW = "New";
        public const string IN_PROGRESS = "InProgress";
        public const string COMPLETED = "Completed";
        public const string CANCELLED = "Cancelled";
    }
    
    // 处方状态
    public static class PrescriptionStatus
    {
        public const string DRAFT = "Draft";
        public const string CONFIRMED = "Confirmed";
        public const string DISPENSED = "Dispensed";
        public const string COMPLETED = "Completed";
        public const string CANCELLED = "Cancelled";
    }
    
    // 诊疗类型
    public static class ConsultationTypes
    {
        public const string FIRST_VISIT = "FirstVisit";
        public const string FOLLOW_UP = "FollowUp";
        public const string EMERGENCY = "Emergency";
    }
    
    // 支付方式
    public static class PaymentMethods
    {
        public const string CASH = "Cash";
        public const string CREDIT_CARD = "CreditCard";
        public const string ALIPAY = "Alipay";
        public const string WECHAT_PAY = "WechatPay";
        public const string INSURANCE = "Insurance";
    }
}
```

### 6. Enums - 枚举类型层
**职责**：定义业务枚举类型、系统枚举类型

**代码示例**：
```csharp
// Enums/Gender.cs
public enum Gender
{
    [Description("男")]
    Male = 1,
    
    [Description("女")]
    Female = 2
}

// Enums/MedicalCaseStatus.cs
public enum MedicalCaseStatus
{
    [Description("新建")]
    New = 1,
    
    [Description("进行中")]
    InProgress = 2,
    
    [Description("已完成")]
    Completed = 3,
    
    [Description("已取消")]
    Cancelled = 4
}

// Enums/PrescriptionStatus.cs
public enum PrescriptionStatus
{
    [Description("草稿")]
    Draft = 1,
    
    [Description("已确认")]
    Confirmed = 2,
    
    [Description("已发药")]
    Dispensed = 3,
    
    [Description("已完成")]
    Completed = 4,
    
    [Description("已取消")]
    Cancelled = 5
}

// Enums/ConsultationType.cs
public enum ConsultationType
{
    [Description("初诊")]
    FirstVisit = 1,
    
    [Description("复诊")]
    FollowUp = 2,
    
    [Description("急诊")]
    Emergency = 3
}

// Enums/PaymentMethod.cs
public enum PaymentMethod
{
    [Description("现金")]
    Cash = 1,
    
    [Description("银行卡")]
    CreditCard = 2,
    
    [Description("支付宝")]
    Alipay = 3,
    
    [Description("微信支付")]
    WechatPay = 4,
    
    [Description("医保")]
    Insurance = 5
}

// Enums/Permission.cs
public enum Permission
{
    [Description("患者管理")]
    PatientManage = 1,
    
    [Description("医案管理")]
    MedicalCaseManage = 2,
    
    [Description("诊疗管理")]
    ConsultationManage = 3,
    
    [Description("处方管理")]
    PrescriptionManage = 4,
    
    [Description("药材管理")]
    HerbManage = 5,
    
    [Description("验方管理")]
    FormulaManage = 6,
    
    [Description("用户管理")]
    UserManage = 7,
    
    [Description("系统管理")]
    SystemManage = 8
}
```

## 🔧 跨层数据传输

### 1. 统一响应格式
```csharp
// Models/Responses/ApiResult.cs
public class ApiResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int Code { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> Errors { get; set; }
    
    public static ApiResult<T> Success(T data, string message = "操作成功")
    {
        return new ApiResult<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Code = 200,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public static ApiResult<T> Error(string message, int code = 400, List<string> errors = null)
    {
        return new ApiResult<T>
        {
            Success = false,
            Message = message,
            Code = code,
            Timestamp = DateTime.UtcNow,
            Errors = errors ?? new List<string>()
        };
    }
    
    public static ApiResult<T> ValidationError(List<ValidationError> validationErrors)
    {
        var errors = validationErrors.Select(e => e.ErrorMessage).ToList();
        return new ApiResult<T>
        {
            Success = false,
            Message = "数据验证失败",
            Code = 422,
            Timestamp = DateTime.UtcNow,
            Errors = errors
        };
    }
}
```

### 2. 分页响应格式
```csharp
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

### 3. 统一异常处理
```csharp
// Infrastructure/Exceptions/BusinessException.cs
public class BusinessException : Exception
{
    public int ErrorCode { get; }
    public string ErrorDetails { get; }
    
    public BusinessException(string message, int errorCode = 400, string errorDetails = null)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
    
    public BusinessException(string message, Exception innerException, int errorCode = 400, string errorDetails = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
}

// Infrastructure/Exceptions/ValidationException.cs
public class ValidationException : BusinessException
{
    public List<ValidationError> ValidationErrors { get; }
    
    public ValidationException(List<ValidationError> validationErrors)
        : base("数据验证失败", 422)
    {
        ValidationErrors = validationErrors;
    }
}

// Infrastructure/Exceptions/NotFoundException.cs
public class NotFoundException : BusinessException
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id {id} not found", 404)
    {
    }
    
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}
```

## 🔗 技术决策记录 (ADR)

### ADR-001: 使用FluentValidation进行数据验证
**状态**: 已接受  
**日期**: 2025-10-15  

**决策**：使用FluentValidation库进行数据验证，而不是DataAnnotations。

**理由**：
- 更灵活的验证规则定义
- 更好的性能
- 支持复杂的验证逻辑
- 更清晰的错误消息

**后果**：
- 需要额外的依赖
- 验证规则需要单独维护
- 学习成本较高

### ADR-002: 使用AutoMapper进行对象映射
**状态**: 已接受  
**日期**: 2025-10-15  

**决策**：使用AutoMapper库进行对象映射，而不是手动映射。

**理由**：
- 减少样板代码
- 提高开发效率
- 减少映射错误
- 支持复杂映射逻辑

**后果**：
- 运行时性能开销
- 需要配置映射规则
- 调试复杂度增加

### ADR-003: 使用MediatR实现命令查询分离
**状态**: 已拒绝  
**日期**: 2025-10-15  

**决策**：不使用MediatR，保持传统的服务层架构。

**理由**：
- 项目规模相对较小
- 避免过度设计
- 减少学习成本
- 保持代码简洁

**后果**：
- 代码耦合度可能较高
- 扩展性受限
- 测试复杂度较高

## 📋 最佳实践

### 1. 命名约定
- **接口**: 以I开头，如IPatientService
- **实现类**: 以具体名称开头，如PatientService
- **实体类**: 使用业务名词，如Patient、MedicalCase
- **DTO类**: 以Dto结尾，如PatientDto
- **请求类**: 以Request结尾，如PatientCreateRequest
- **响应类**: 以Response结尾，如PatientResponse

### 2. 代码组织
- **单一职责**: 每个类只负责一个功能
- **开闭原则**: 对扩展开放，对修改封闭
- **依赖倒置**: 依赖抽象，不依赖具体实现
- **接口隔离**: 使用小而专一的接口

### 3. 性能优化
- **延迟加载**: 使用延迟加载减少内存占用
- **缓存策略**: 合理使用缓存提高性能
- **异步编程**: I/O操作使用async/await
- **批量操作**: 减少数据库访问次数

### 4. 安全考虑
- **输入验证**: 所有输入都必须验证
- **SQL注入防护**: 使用参数化查询
- **敏感数据**: 敏感数据加密存储
- **权限控制**: 实现细粒度权限控制

## 🔗 相关文档

- **[架构总览](../README.md)** - 三层对齐架构设计原理
- **[Server端架构](../server/README.md)** - 服务端三层架构实现
- **[Client端架构](../client/README.md)** - WPF五层架构实现
- **[共享开发指南](../../development/shared/README.md)** - 共享层开发规范
- **[模块设计指南](../module-design-guide.md)** - 业务模块化设计标准

---

**文档维护**：架构组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核