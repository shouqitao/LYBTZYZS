# DEEP-005: API设计最佳实践

## 概述

凌隐宝堂中医诊所管理系统的API设计遵循RESTful架构原则，结合医疗行业的特殊需求，提供安全、高效、易用的Web API接口。本文档基于实际项目经验，详细阐述API设计的核心理念、架构模式、安全策略和性能优化方案，确保API接口的可靠性、可维护性和扩展性。

## API设计原则

### 1. 核心设计理念

#### 1.1 RESTful架构原则
- **资源导向**：每个URL代表一个资源，使用名词而非动词
- **统一接口**：使用标准的HTTP方法（GET, POST, PUT, DELETE）
- **无状态**：每个请求包含完整的上下文信息
- **分层系统**：客户端无需知道是否直接连接到最终服务器

#### 1.2 医疗行业特殊考量
- **数据安全性**：患者隐私保护，符合HIPAA等法规要求
- **操作审计**：关键操作必须记录详细的审计日志
- **数据完整性**：确保医疗数据的准确性和一致性
- **实时性要求**：处方计算、库存更新等需要实时响应

### 2. API命名规范

#### 2.1 URL命名规则
```csharp
// 资源命名使用复数形式
/api/patients           // 患者资源
/api/doctors            // 医生资源
/api/medical-cases      // 医案资源（使用连字符）
/api/prescriptions      // 处方资源
/api/herbs              // 药材资源

// 嵌套资源
/api/patients/{id}/medical-cases           // 患者的医案
/api/medical-cases/{id}/prescriptions     // 医案的处方
/api/prescriptions/{id}/items             // 处方的明细项

// 操作性资源（动词+名词）
/api/prescriptions/{id}/calculate-price    // 计算处方价格
/api/herbs/check-inventory                 // 检查库存
```

#### 2.2 HTTP方法使用规范
```csharp
[HttpGet]
[Route("{id}")]                          // GET /api/patients/{id}
public async Task<ActionResult<PatientDto>> GetPatient(int id)

[HttpPost]
[Route("")]                              // POST /api/patients
public async Task<ActionResult<PatientDto>> CreatePatient([FromBody] CreatePatientRequest request)

[HttpPut]
[Route("{id}")]                          // PUT /api/patients/{id}
public async Task<ActionResult<PatientDto>> UpdatePatient(int id, [FromBody] UpdatePatientRequest request)

[HttpDelete]
[Route("{id}")]                          // DELETE /api/patients/{id}
public async Task<ActionResult> DeletePatient(int id)

[HttpPost]
[Route("{id}/deactivate")]              // POST /api/patients/{id}/deactivate
public async Task<ActionResult> DeactivatePatient(int id)
```

## 请求响应设计

### 1. 统一响应格式

#### 1.1 成功响应结构
```csharp
// 标准响应格式
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
}

// 分页响应格式
public class PagedResponse<T> : ApiResponse<List<T>>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

// 使用示例
[HttpGet]
public async Task<ActionResult<PagedResponse<PatientDto>>> GetPatients([FromQuery] PatientQueryParameters parameters)
{
    var result = await _patientService.GetPatientsAsync(parameters);

    var response = new PagedResponse<PatientDto>
    {
        Success = true,
        Data = result.Items,
        Page = result.Page,
        PageSize = result.PageSize,
        TotalCount = result.TotalCount,
        TotalPages = result.TotalPages,
        HasNextPage = result.HasNextPage,
        HasPreviousPage = result.HasPreviousPage,
        Message = "获取患者列表成功",
        Timestamp = DateTime.UtcNow,
        RequestId = HttpContext.TraceIdentifier
    };

    return Ok(response);
}
```

#### 1.2 错误响应结构
```csharp
// 错误响应格式
public class ErrorResponse
{
    public bool Success { get; set; } = false;
    public string Code { get; set; }
    public string Message { get; set; }
    public List<string> Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
    public string Path { get; set; }
}

// 错误代码定义
public static class ErrorCodes
{
    // 通用错误 (1000-1999)
    public const string INVALID_REQUEST = "1001";
    public const string UNAUTHORIZED = "1002";
    public const string FORBIDDEN = "1003";
    public const string NOT_FOUND = "1004";
    public const string VALIDATION_ERROR = "1005";

    // 业务错误 (2000-2999)
    public const string PATIENT_NOT_FOUND = "2001";
    public const string DUPLICATE_PATIENT = "2002";
    public const string INVALID_MEDICAL_CASE = "2003";
    public const string PRESCRIPTION_CALCULATION_ERROR = "2004";
    public const string INSUFFICIENT_INVENTORY = "2005";

    // 系统错误 (5000-5999)
    public const string DATABASE_ERROR = "5001";
    public const string EXTERNAL_SERVICE_ERROR = "5002";
    public const string INTERNAL_SERVER_ERROR = "5003";
}

// 全局异常处理中间件
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

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

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var response = new ErrorResponse
        {
            RequestId = context.TraceIdentifier,
            Path = context.Request.Path,
            Timestamp = DateTime.UtcNow
        };

        switch (ex)
        {
            case ValidationException validationEx:
                response.Code = ErrorCodes.VALIDATION_ERROR;
                response.Message = "请求验证失败";
                response.Details = validationEx.Errors.Select(e => e.ErrorMessage).ToList();
                context.Response.StatusCode = 400;
                break;

            case NotFoundException notFoundEx:
                response.Code = ErrorCodes.NOT_FOUND;
                response.Message = notFoundEx.Message;
                context.Response.StatusCode = 404;
                break;

            case BusinessException businessEx:
                response.Code = businessEx.ErrorCode;
                response.Message = businessEx.Message;
                context.Response.StatusCode = 400;
                break;

            default:
                _logger.LogError(ex, "未处理的异常: {RequestId}", context.TraceIdentifier);
                response.Code = ErrorCodes.INTERNAL_SERVER_ERROR;
                response.Message = "服务器内部错误";
                context.Response.StatusCode = 500;
                break;
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### 2. 请求验证设计

#### 2.1 模型验证
```csharp
// 创建患者请求模型
public class CreatePatientRequest
{
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(100, ErrorMessage = "姓名长度不能超过100个字符")]
    public string Name { get; set; }

    [Required(ErrorMessage = "性别不能为空")]
    [RegularExpression("^(男|女)$", ErrorMessage = "性别必须是'男'或'女'")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "出生日期不能为空")]
    [DataType(DataType.Date)]
    [CustomDateRange(ErrorMessage = "出生日期无效")]
    public DateTime DateOfBirth { get; set; }

    [Phone(ErrorMessage = "电话号码格式不正确")]
    [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
    public string PhoneNumber { get; set; }

    [RegularExpression(@"^\d{17}[\dX]$", ErrorMessage = "身份证号码格式不正确")]
    public string IdentificationNumber { get; set; }

    [StringLength(500, ErrorMessage = "地址长度不能超过500个字符")]
    public string Address { get; set; }

    [StringLength(100, ErrorMessage = "紧急联系人长度不能超过100个字符")]
    public string EmergencyContact { get; set; }

    [Phone(ErrorMessage = "紧急联系电话格式不正确")]
    public string EmergencyPhone { get; set; }
}

// 自定义验证属性
public class CustomDateRangeAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is DateTime date)
        {
            var today = DateTime.Today;
            var minDate = today.AddYears(-120); // 最小120岁
            var maxDate = today; // 最大今天出生

            if (date < minDate || date > maxDate)
            {
                return new ValidationResult($"出生日期必须在{minDate:yyyy-MM-dd}到{maxDate:yyyy-MM-dd}之间");
            }
        }

        return ValidationResult.Success;
    }
}
```

#### 2.2 业务规则验证
```csharp
// 处方计算请求验证
public class PrescriptionCalculationRequest
{
    [Required]
    public int PatientId { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "处方至少包含一味药")]
    public List<PrescriptionItemRequest> Items { get; set; }

    [DataType(DataType.Date)]
    public DateTime PrescriptionDate { get; set; } = DateTime.Today;
}

public class PrescriptionItemRequest
{
    [Required(ErrorMessage = "药材ID不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "药材ID必须大于0")]
    public int HerbId { get; set; }

    [Required(ErrorMessage = "数量不能为空")]
    [Range(0.1, 1000, ErrorMessage = "数量必须在0.1到1000之间")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "单位不能为空")]
    [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
    public string Unit { get; set; }
}

// 业务验证服务
public class PrescriptionValidationService
{
    public async Task<ValidationResult> ValidatePrescriptionAsync(PrescriptionCalculationRequest request)
    {
        var errors = new List<string>();

        // 验证药材库存
        foreach (var item in request.Items)
        {
            var herb = await _herbService.GetHerbAsync(item.HerbId);
            if (herb == null)
            {
                errors.Add($"药材ID {item.HerbId} 不存在");
                continue;
            }

            if (herb.CurrentStock < item.Quantity)
            {
                errors.Add($"药材 {herb.Name} 库存不足，当前库存: {herb.CurrentStock}{herb.Unit}，需要: {item.Quantity}{item.Unit}");
            }
        }

        // 验证配伍禁忌
        var incompatibilityResult = await CheckHerbIncompatibilityAsync(request.Items);
        if (!incompatibilityResult.IsValid)
        {
            errors.AddRange(incompatibilityResult.Warnings);
        }

        // 验证剂量合理性
        var dosageValidation = await ValidateDosageAsync(request);
        if (!dosageValidation.IsValid)
        {
            errors.AddRange(dosageValidation.Warnings);
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    private async Task<IncompatibilityResult> CheckHerbIncompatibilityAsync(List<PrescriptionItemRequest> items)
    {
        // 实现中药配伍禁忌检查逻辑
        // 例如：人参、莱菔子相畏
        var warnings = new List<string>();
        var herbNames = items.Select(i => i.HerbName).ToList();

        if (herbNames.Contains("人参") && herbNames.Contains("莱菔子"))
        {
            warnings.Add("警告：人参与莱菔子相畏，不建议同时使用");
        }

        // 更多配伍禁忌检查...

        return new IncompatibilityResult
        {
            IsValid = !warnings.Any(),
            Warnings = warnings
        };
    }
}
```

## 认证授权设计

### 1. JWT令牌认证

#### 1.1 令牌生成和验证
```csharp
// JWT服务
public class JwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings, ILogger<JwtTokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("sub", user.Id.ToString()),
            new Claim("name", user.Username),
            new Claim("role", user.Role),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_jwtSettings.AccessTokenExpiration),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation("为用户 {UserId} 生成访问令牌", user.Id);
        return tokenString;
    }

    public string GenerateRefreshToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("token_type", "refresh"),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_jwtSettings.RefreshTokenExpiration),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "令牌验证失败");
            return null;
        }
    }
}
```

#### 1.2 认证控制器
```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;

    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
        {
            _logger.LogWarning("登录失败: 用户名或密码错误 - {Username}", request.Username);
            return Unauthorized(new ErrorResponse
            {
                Code = ErrorCodes.UNAUTHORIZED,
                Message = "用户名或密码错误"
            });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("登录失败: 用户已被禁用 - {UserId}", user.Id);
            return Unauthorized(new ErrorResponse
            {
                Code = ErrorCodes.FORBIDDEN,
                Message = "用户账户已被禁用"
            });
        }

        // 生成令牌
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user);

        // 更新最后登录时间
        await _authService.UpdateLastLoginAsync(user.Id);

        // 记录登录日志
        _logger.LogInformation("用户登录成功 - UserId: {UserId}, Username: {Username}, IP: {IP}",
            user.Id, user.Username, HttpContext.Connection.RemoteIpAddress);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = (int)TimeSpan.FromHours(2).TotalSeconds,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        };

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = response,
            Message = "登录成功"
        });
    }

    [HttpPost]
    [Route("refresh")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var principal = _jwtTokenService.ValidateToken(request.RefreshToken);
            if (principal == null)
            {
                return Unauthorized(new ErrorResponse
                {
                    Code = ErrorCodes.UNAUTHORIZED,
                    Message = "无效的刷新令牌"
                });
            }

            var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new ErrorResponse
                {
                    Code = ErrorCodes.UNAUTHORIZED,
                    Message = "用户不存在或已被禁用"
                });
            }

            // 生成新的访问令牌
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);

            var response = new RefreshTokenResponse
            {
                AccessToken = newAccessToken,
                ExpiresIn = (int)TimeSpan.FromHours(2).TotalSeconds
            };

            return Ok(new ApiResponse<RefreshTokenResponse>
            {
                Success = true,
                Data = response,
                Message = "令牌刷新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "令牌刷新失败");
            return BadRequest(new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "令牌刷新失败"
            });
        }
    }
}
```

### 2. 双轨认证机制

#### 2.1 超级管理员认证
```csharp
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    [HttpPost]
    [Route("authenticate")]
    public async Task<ActionResult<AdminAuthResponse>> AuthenticateWithSecret([FromBody] AdminAuthRequest request)
    {
        _logger.LogWarning("超级管理员认证尝试 - Secret: {SecretHash}",
            ComputeHash(request.SecretKey));

        var adminSecret = await _adminService.ValidateSecretAsync(request.SecretKey);
        if (adminSecret == null)
        {
            _logger.LogWarning("超级管理员认证失败 - 无效的密钥");
            return Unauthorized(new ErrorResponse
            {
                Code = ErrorCodes.UNAUTHORIZED,
                Message = "无效的超级管理员密钥"
            });
        }

        if (!adminSecret.IsActive)
        {
            _logger.LogWarning("超级管理员认证失败 - 密钥已禁用");
            return Unauthorized(new ErrorResponse
            {
                Code = ErrorCodes.FORBIDDEN,
                Message = "超级管理员密钥已被禁用"
            });
        }

        if (adminSecret.ExpiryDate.HasValue && adminSecret.ExpiryDate < DateTime.UtcNow)
        {
            _logger.LogWarning("超级管理员认证失败 - 密钥已过期");
            return Unauthorized(new ErrorResponse
            {
                Code = ErrorCodes.FORBIDDEN,
                Message = "超级管理员密钥已过期"
            });
        }

        // 生成特殊的超级管理员令牌
        var adminToken = GenerateAdminToken(adminSecret);

        _logger.LogInformation("超级管理员认证成功 - SecretId: {SecretId}", adminSecret.Id);

        var response = new AdminAuthResponse
        {
            AdminToken = adminToken,
            Permissions = new[] { "system.admin", "user.manage", "data.export", "system.config" },
            ExpiresIn = (int)TimeSpan.FromHours(24).TotalSeconds
        };

        return Ok(new ApiResponse<AdminAuthResponse>
        {
            Success = true,
            Data = response,
            Message = "超级管理员认证成功"
        });
    }

    private string GenerateAdminToken(AdminSecret adminSecret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin"),
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim("admin_secret_id", adminSecret.Id.ToString()),
            new Claim("token_type", "admin"),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## 业务API设计模式

### 1. 复杂计算API

#### 1.1 处方价格计算
```csharp
[ApiController]
[Route("api/prescriptions")]
public class PrescriptionCalculationController : ControllerBase
{
    private readonly IPrescriptionCalculationService _calculationService;
    private readonly ILogger<PrescriptionCalculationController> _logger;

    [HttpPost]
    [Route("calculate")]
    public async Task<ActionResult<PrescriptionCalculationResult>> CalculatePrice(
        [FromBody] PrescriptionCalculationRequest request)
    {
        _logger.LogInformation("开始计算处方价格 - PatientId: {PatientId}, DoctorId: {DoctorId}",
            request.PatientId, request.DoctorId);

        try
        {
            // 验证请求
            var validationResult = await _calculationService.ValidateRequestAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.VALIDATION_ERROR,
                    Message = "请求验证失败",
                    Details = validationResult.Errors
                });
            }

            // 执行计算
            var result = await _calculationService.CalculatePrescriptionAsync(request);

            // 记录计算日志
            _logger.LogInformation("处方价格计算完成 - PatientId: {PatientId}, TotalAmount: {TotalAmount}",
                request.PatientId, result.TotalAmount);

            return Ok(new ApiResponse<PrescriptionCalculationResult>
            {
                Success = true,
                Data = result,
                Message = "处方价格计算成功"
            });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "处方价格计算失败 - PatientId: {PatientId}", request.PatientId);
            return BadRequest(new ErrorResponse
            {
                Code = ex.ErrorCode,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处方价格计算异常 - PatientId: {PatientId}", request.PatientId);
            return StatusCode(500, new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "处方价格计算失败"
            });
        }
    }

    [HttpPost]
    [Route("calculate-batch")]
    public async Task<ActionResult<List<PrescriptionCalculationResult>>> CalculateBatchPrice(
        [FromBody] List<PrescriptionCalculationRequest> requests)
    {
        if (requests.Count > 100)
        {
            return BadRequest(new ErrorResponse
            {
                Code = ErrorCodes.VALIDATION_ERROR,
                Message = "批量计算最多支持100个处方"
            });
        }

        _logger.LogInformation("开始批量计算处方价格 - Count: {Count}", requests.Count);

        try
        {
            var results = await _calculationService.CalculateMultiplePrescriptionsAsync(requests);

            var successCount = results.Count(r => r.ErrorMessage == null);
            var failureCount = results.Count - successCount;

            _logger.LogInformation("批量处方价格计算完成 - 成功: {SuccessCount}, 失败: {FailureCount}",
                successCount, failureCount);

            return Ok(new ApiResponse<List<PrescriptionCalculationResult>>
            {
                Success = true,
                Data = results,
                Message = $"批量计算完成，成功: {successCount}，失败: {failureCount}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量处方价格计算异常");
            return StatusCode(500, new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "批量计算失败"
            });
        }
    }
}
```

#### 1.2 库存管理API
```csharp
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    [HttpPost]
    [Route("update")]
    public async Task<ActionResult<InventoryUpdateResult>> UpdateInventory(
        [FromBody] UpdateInventoryRequest request)
    {
        _logger.LogInformation("更新库存 - HerbId: {HerbId}, QuantityChange: {QuantityChange}, Type: {TransactionType}",
            request.HerbId, request.QuantityChange, request.TransactionType);

        try
        {
            // 验证库存更新
            var validation = await _inventoryService.ValidateInventoryUpdateAsync(request);
            if (!validation.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = ErrorCodes.VALIDATION_ERROR,
                    Message = validation.ErrorMessage
                });
            }

            // 执行库存更新
            var result = await _inventoryService.UpdateInventoryAsync(request);

            // 检查低库存警告
            if (result.IsLowStock)
            {
                _logger.LogWarning("库存不足警告 - HerbId: {HerbId}, CurrentStock: {CurrentStock}, MinStock: {MinStock}",
                    request.HerbId, result.NewStockLevel, result.MinStockLevel);

                // 发送低库存通知
                await _notificationService.SendLowStockAlertAsync(result.HerbId, result.NewStockLevel);
            }

            return Ok(new ApiResponse<InventoryUpdateResult>
            {
                Success = true,
                Data = result,
                Message = "库存更新成功"
            });
        }
        catch (InsufficientInventoryException ex)
        {
            _logger.LogWarning(ex, "库存不足 - HerbId: {HerbId}", request.HerbId);
            return BadRequest(new ErrorResponse
            {
                Code = ErrorCodes.INSUFFICIENT_INVENTORY,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存更新异常 - HerbId: {HerbId}", request.HerbId);
            return StatusCode(500, new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "库存更新失败"
            });
        }
    }

    [HttpGet]
    [Route("low-stock")]
    public async Task<ActionResult<LowStockReport>> GetLowStockReport(
        [FromQuery] double thresholdPercent = 0.2)
    {
        try
        {
            var report = await _inventoryService.GetLowStockReportAsync(thresholdPercent);

            _logger.LogInformation("获取低库存报告 - 阈值: {ThresholdPercent}%, 低库存项数: {Count}",
                thresholdPercent * 100, report.LowStockItems.Count);

            return Ok(new ApiResponse<LowStockReport>
            {
                Success = true,
                Data = report,
                Message = "低库存报告获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取低库存报告失败");
            return StatusCode(500, new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "获取低库存报告失败"
            });
        }
    }
}
```

### 2. 搜索和过滤API

#### 2.1 高级搜索接口
```csharp
[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    [HttpGet]
    [Route("patients")]
    public async Task<ActionResult<PagedResponse<PatientSearchResult>>> SearchPatients(
        [FromQuery] PatientSearchParameters parameters)
    {
        _logger.LogInformation("搜索患者 - Keyword: {Keyword}, Filters: {Filters}",
            parameters.Keyword, parameters.GetFilterSummary());

        try
        {
            var result = await _searchService.SearchPatientsAsync(parameters);

            var response = new PagedResponse<PatientSearchResult>
            {
                Success = true,
                Data = result.Items,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                HasNextPage = result.HasNextPage,
                HasPreviousPage = result.HasPreviousPage,
                Message = $"找到 {result.TotalCount} 个匹配的患者",
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            };

            // 记录搜索统计
            _logger.LogInformation("患者搜索完成 - 返回 {Count} 个结果，总计数 {TotalCount}",
                result.Items.Count, result.TotalCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者搜索失败 - Parameters: {Parameters}", parameters);
            return StatusCode(500, new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "搜索失败"
            });
        }
    }

    [HttpGet]
    [Route("suggestions")]
    public async Task<ActionResult<SearchSuggestionsResponse>> GetSuggestions(
        [FromQuery] string query,
        [FromQuery] string type = "patient",
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return BadRequest(new ErrorResponse
            {
                Code = ErrorCodes.VALIDATION_ERROR,
                Message = "搜索关键词至少需要2个字符"
            });
        }

        try
        {
            var suggestions = await _searchService.GetSuggestionsAsync(query, type, limit);

            return Ok(new ApiResponse<SearchSuggestionsResponse>
            {
                Success = true,
                Data = new SearchSuggestionsResponse
                {
                    Query = query,
                    Type = type,
                    Suggestions = suggestions
                },
                Message = "搜索建议获取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取搜索建议失败 - Query: {Query}, Type: {Type}", query, type);
            return StatusCode(500, new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = "获取搜索建议失败"
            });
        }
    }
}

// 搜索参数模型
public class PatientSearchParameters
{
    public string Keyword { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public string PhoneNumber { get; set; }
    public string IdentificationNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedDate";
    public string SortOrder { get; set; } = "desc";

    public string GetFilterSummary()
    {
        var filters = new List<string>();

        if (!string.IsNullOrEmpty(Name)) filters.Add($"姓名:{Name}");
        if (!string.IsNullOrEmpty(Gender)) filters.Add($"性别:{Gender}");
        if (!string.IsNullOrEmpty(PhoneNumber)) filters.Add($"电话:{PhoneNumber}");
        if (!string.IsNullOrEmpty(IdentificationNumber)) filters.Add($"身份证:{IdentificationNumber}");
        if (StartDate.HasValue) filters.Add($"开始日期:{StartDate.Value:yyyy-MM-dd}");
        if (EndDate.HasValue) filters.Add($"结束日期:{EndDate.Value:yyyy-MM-dd}");

        return filters.Any() ? string.Join(", ", filters) : "无过滤条件";
    }
}
```

## 性能优化策略

### 1. 响应缓存

#### 1.1 内存缓存配置
```csharp
[ApiController]
[Route("api/herbs")]
public class HerbsController : ControllerBase
{
    private readonly IHerbService _herbService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HerbsController> _logger;

    [HttpGet]
    [Route("")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)] // 5分钟缓存
    public async Task<ActionResult<List<HerbDto>>> GetHerbs()
    {
        const string cacheKey = "herbs:all";

        if (_cache.TryGetValue(cacheKey, out List<HerbDto> cachedHerbs))
        {
            _logger.LogDebug("从缓存获取药材列表");
            return Ok(cachedHerbs);
        }

        var herbs = await _herbService.GetActiveHerbsAsync();

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, herbs, cacheOptions);

        _logger.LogInformation("从数据库加载药材列表 - 数量: {Count}", herbs.Count);
        return Ok(herbs);
    }

    [HttpGet]
    [Route("{id}/price")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)] // 1分钟缓存
    public async Task<ActionResult<HerbPriceDto>> GetHerbPrice(int id)
    {
        var cacheKey = $"herb:price:{id}";

        if (_cache.TryGetValue(cacheKey, out HerbPriceDto cachedPrice))
        {
            return Ok(cachedPrice);
        }

        var price = await _herbService.GetCurrentPriceAsync(id);
        if (price == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = ErrorCodes.NOT_FOUND,
                Message = "药材不存在"
            });
        }

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
        };

        _cache.Set(cacheKey, price, cacheOptions);
        return Ok(price);
    }

    [HttpPost]
    [Route("clear-cache")]
    [Authorize(Roles = "Admin")]
    public ActionResult ClearHerbCache()
    {
        _cache.Remove("herbs:all");
        _logger.LogInformation("药材缓存已清除");

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "缓存清除成功"
        });
    }
}
```

#### 1.2 分布式缓存（Redis替代方案）
```csharp
// 由于项目禁止使用Redis，这里提供内存缓存的分布式替代方案
public class DistributedMemoryCache : IDistributedCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<DistributedMemoryCache> _logger;

    public DistributedMemoryCache(IMemoryCache memoryCache, ILogger<DistributedMemoryCache> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public byte[] Get(string key)
    {
        _memoryCache.TryGetValue(key, out byte[] value);
        return value;
    }

    public Task<byte[]> GetAsync(string key)
    {
        return Task.FromResult(Get(key));
    }

    public void Refresh(string key)
    {
        // 内存缓存不需要刷新
    }

    public Task RefreshAsync(string key)
    {
        Refresh(key);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _memoryCache.Remove(key);
    }

    public Task RemoveAsync(string key)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options.SlidingExpiration,
            Priority = CacheItemPriority.Normal
        };

        _memoryCache.Set(key, value, memoryCacheOptions);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }
}
```

### 2. 异步处理和限流

#### 2.1 异步操作API
```csharp
[ApiController]
[Route("api/async")]
public class AsyncOperationsController : ControllerBase
{
    private readonly IBackgroundTaskService _backgroundTaskService;
    private readonly ILogger<AsyncOperationsController> _logger;

    [HttpPost]
    [Route("export-patients")]
    public async Task<ActionResult<AsyncTaskResponse>> ExportPatients(
        [FromBody] ExportPatientsRequest request)
    {
        var taskId = Guid.NewGuid().ToString();

        _logger.LogInformation("启动患者数据导出任务 - TaskId: {TaskId}, Filters: {Filters}",
            taskId, request.GetFilterSummary());

        // 创建后台任务
        var task = _backgroundTaskService.QueueTask(new ExportPatientsTask
        {
            TaskId = taskId,
            Request = request,
            CreatedBy = GetCurrentUserId(),
            CreatedDate = DateTime.UtcNow
        });

        var response = new AsyncTaskResponse
        {
            TaskId = taskId,
            Status = "Queued",
            EstimatedDuration = "2-5分钟",
            CreatedDate = DateTime.UtcNow
        };

        return Accepted(new ApiResponse<AsyncTaskResponse>
        {
            Success = true,
            Data = response,
            Message = "导出任务已启动"
        });
    }

    [HttpGet]
    [Route("tasks/{taskId}/status")]
    public async Task<ActionResult<TaskStatusResponse>> GetTaskStatus(string taskId)
    {
        var status = await _backgroundTaskService.GetTaskStatusAsync(taskId);
        if (status == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = ErrorCodes.NOT_FOUND,
                Message = "任务不存在"
            });
        }

        return Ok(new ApiResponse<TaskStatusResponse>
        {
            Success = true,
            Data = status,
            Message = "任务状态获取成功"
        });
    }

    [HttpGet]
    [Route("tasks/{taskId}/result")]
    public async Task<ActionResult> GetTaskResult(string taskId)
    {
        var result = await _backgroundTaskService.GetTaskResultAsync(taskId);
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = ErrorCodes.NOT_FOUND,
                Message = "任务结果不存在"
            });
        }

        if (result.Status == "Completed")
        {
            var fileBytes = Convert.FromBase64String(result.FileData);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"患者数据导出_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }
        else if (result.Status == "Failed")
        {
            return BadRequest(new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_SERVER_ERROR,
                Message = $"任务执行失败: {result.ErrorMessage}"
            });
        }
        else
        {
            return BadRequest(new ErrorResponse
            {
                Code = ErrorCodes.VALIDATION_ERROR,
                Message = "任务尚未完成"
            });
        }
    }
}
```

#### 2.2 API限流中间件
```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly Dictionary<string, (DateTime LastAccess, int RequestCount)> _requestCounts;
    private readonly object _lock = new object();

    // 配置：每分钟最多100个请求
    private const int MaxRequestsPerMinute = 100;
    private const int WindowSizeMinutes = 1;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _requestCounts = new Dictionary<string, (DateTime, int)>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (_requestCounts.ContainsKey(clientId))
            {
                var (lastAccess, requestCount) = _requestCounts[clientId];

                // 检查是否在时间窗口内
                if (now - lastAccess < TimeSpan.FromMinutes(WindowSizeMinutes))
                {
                    // 在时间窗口内，增加请求计数
                    if (requestCount >= MaxRequestsPerMinute)
                    {
                        _logger.LogWarning("API限流触发 - ClientId: {ClientId}, 请求计数: {RequestCount}",
                            clientId, requestCount);

                        context.Response.StatusCode = 429; // Too Many Requests
                        context.Response.ContentType = "application/json";

                        var errorResponse = new ErrorResponse
                        {
                            Code = "RATE_LIMIT_EXCEEDED",
                            Message = "请求过于频繁，请稍后再试",
                            Timestamp = now,
                            RequestId = context.TraceIdentifier
                        };

                        return;
                    }

                    _requestCounts[clientId] = (lastAccess, requestCount + 1);
                }
                else
                {
                    // 超出时间窗口，重置计数
                    _requestCounts[clientId] = (now, 1);
                }
            }
            else
            {
                // 新客户端，初始化计数
                _requestCounts[clientId] = (now, 1);
            }
        }

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        // 优先使用用户ID（如果已认证）
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // 其次使用IP地址
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(remoteIp))
        {
            return $"ip:{remoteIp}";
        }

        // 最后使用默认标识
        return "anonymous";
    }
}
```

## API文档和版本控制

### 1. Swagger/OpenAPI配置

#### 1.1 Swagger配置
```csharp
// Program.cs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "凌隐宝堂中医诊所管理系统 API",
        Version = "v1",
        Description = "凌隐宝堂中医诊所管理系统Web API接口文档",
        Contact = new OpenApiContact
        {
            Name = "凌隐宝堂技术支持",
            Email = "support@lybt.com"
        }
    });

    // 包含XML注释
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // 添加JWT认证
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 添加超级管理员认证
    c.AddSecurityDefinition("AdminToken", new OpenApiSecurityScheme
    {
        Description = "Admin Token for super administrator access",
        Name = "X-Admin-Token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "AdminToken"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "凌隐宝堂 API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "凌隐宝堂中医诊所管理系统 API文档";
    });
}
```

#### 1.2 API文档注释
```csharp
/// <summary>
/// 患者管理控制器
/// </summary>
[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    /// <summary>
    /// 获取患者列表
    /// </summary>
    /// <param name="parameters">查询参数</param>
    /// <returns>患者列表分页结果</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">请求参数无效</response>
    /// <response code="401">未授权访问</response>
    /// <response code="500">服务器内部错误</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PatientDto>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PagedResponse<PatientDto>>> GetPatients(
        [FromQuery] PatientQueryParameters parameters)
    {
        // 实现...
    }

    /// <summary>
    /// 创建新患者
    /// </summary>
    /// <param name="request">患者创建请求</param>
    /// <returns>创建的患者信息</returns>
    /// <response code="201">创建成功</response>
    /// <response code="400">请求参数无效或患者已存在</response>
    /// <response code="401">未授权访问</response>
    /// <response code="500">服务器内部错误</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PatientDto>> CreatePatient(
        [FromBody] CreatePatientRequest request)
    {
        // 实现...
    }
}
```

### 2. API版本控制

#### 2.1 版本控制实现
```csharp
// 版本控制服务
public interface IApiVersionService
{
    string GetCurrentVersion();
    bool IsVersionSupported(string version);
    string GetDeprecatedMessage(string version);
}

public class ApiVersionService : IApiVersionService
{
    private readonly Dictionary<string, ApiVersionInfo> _versions;

    public ApiVersionService()
    {
        _versions = new Dictionary<string, ApiVersionInfo>
        {
            ["v1"] = new ApiVersionInfo
            {
                Version = "v1",
                ReleaseDate = new DateTime(2024, 1, 1),
                IsDeprecated = false,
                DeprecationDate = null,
                SunsetDate = null,
                SupportedUntil = null
            }
        };
    }

    public string GetCurrentVersion() => "v1";

    public bool IsVersionSupported(string version)
    {
        return _versions.ContainsKey(version.ToLowerInvariant());
    }

    public string GetDeprecatedMessage(string version)
    {
        if (_versions.TryGetValue(version.ToLowerInvariant(), out var versionInfo))
        {
            if (versionInfo.IsDeprecated)
            {
                return $"API版本 {version} 已废弃，请使用最新版本。废弃日期: {versionInfo.DeprecationDate:yyyy-MM-dd}";
            }
        }
        return null;
    }
}

// 版本控制中间件
public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiVersionService _versionService;
    private readonly ILogger<ApiVersionMiddleware> _logger;

    public ApiVersionMiddleware(RequestDelegate next, IApiVersionService versionService, ILogger<ApiVersionMiddleware> logger)
    {
        _next = next;
        _versionService = versionService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestedVersion = GetRequestedVersion(context);

        if (!string.IsNullOrEmpty(requestedVersion) && !_versionService.IsVersionSupported(requestedVersion))
        {
            _logger.LogWarning("不支持的API版本 - Version: {Version}, Path: {Path}", requestedVersion, context.Request.Path);

            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Code = "UNSUPPORTED_API_VERSION",
                Message = $"不支持的API版本: {requestedVersion}",
                Details = new[] { $"支持的版本: {_versionService.GetCurrentVersion()}" },
                Timestamp = DateTime.UtcNow,
                RequestId = context.TraceIdentifier,
                Path = context.Request.Path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        // 检查废弃版本警告
        if (!string.IsNullOrEmpty(requestedVersion))
        {
            var deprecationMessage = _versionService.GetDeprecatedMessage(requestedVersion);
            if (!string.IsNullOrEmpty(deprecationMessage))
            {
                context.Response.Headers.Add("X-API-Deprecation-Warning", deprecationMessage);
                _logger.LogWarning("使用已废弃的API版本 - Version: {Version}, Warning: {Warning}",
                    requestedVersion, deprecationMessage);
            }
        }

        context.Response.Headers.Add("API-Version", _versionService.GetCurrentVersion());

        await _next(context);
    }

    private string GetRequestedVersion(HttpContext context)
    {
        // 从URL路径获取版本
        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path) && path.StartsWith("/api/"))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && segments[1].StartsWith("v"))
            {
                return segments[1];
            }
        }

        // 从查询参数获取版本
        if (context.Request.Query.TryGetValue("version", out var versionValue))
        {
            return versionValue;
        }

        // 从请求头获取版本
        if (context.Request.Headers.TryGetValue("API-Version", out var headerVersion))
        {
            return headerVersion;
        }

        return null;
    }
}
```

## API设计检查清单

### 设计原则
- [ ] 遵循RESTful架构原则
- [ ] 使用名词而非动词命名资源
- [ ] 正确使用HTTP方法
- [ ] 设计无状态API接口
- [ ] 实现统一的错误处理机制

### 请求响应
- [ ] 使用标准HTTP状态码
- [ ] 设计统一的响应格式
- [ ] 实现分页查询机制
- [ ] 添加请求参数验证
- [ ] 包含必要的响应头信息

### 安全认证
- [ ] 实现JWT令牌认证
- [ ] 配置双轨认证机制
- [ ] 添加请求限流保护
- [ ] 实现API访问日志
- [ ] 配置HTTPS安全传输

### 性能优化
- [ ] 实现响应缓存策略
- [ ] 优化数据库查询性能
- [ ] 添加异步处理能力
- [ ] 实现批量操作接口
- [ ] 配置API限流机制

### 文档维护
- [ ] 配置Swagger/OpenAPI文档
- [ ] 添加详细的API注释
- [ ] 实现API版本控制
- [ ] 提供示例请求和响应
- [ ] 保持文档与代码同步

### 测试验证
- [ ] 编写API单元测试
- [ ] 实现集成测试覆盖
- [ ] 添加性能测试验证
- [ ] 测试异常处理机制
- [ ] 验证安全防护措施

通过这套完整的API设计最佳实践，凌隐宝堂中医诊所管理系统能够提供安全、高效、易维护的Web API接口，满足医疗行业的特殊需求和技术标准。