# Server端开发指南

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**维护团队**：后端开发组  

## 🎯 Server端开发导航

凌隐宝堂中医诊所管理系统Server端采用**ASP.NET Core三层架构**，严格遵循Controller + Service + Repository的分层模式，确保代码的可维护性和扩展性。

### 📋 Server端技术栈

| 技术 | 版本 | 用途 | 说明 |
|------|------|------|------|
| **.NET** | 8.0 | 运行时 | 最新的LTS版本，性能优异 |
| **ASP.NET Core** | 8.0 | Web框架 | RESTful API开发 |
| **Entity Framework Core** | 8.0 | ORM框架 | 数据库操作和映射 |
| **SQL Server** | 2019+ | 数据库 | 主数据库 |
| **Dapper** | 2.0 | 微ORM | 高性能数据访问 |
| **AutoMapper** | 12.0 | 对象映射 | DTO与实体转换 |
| **FluentValidation** | 11.0 | 数据验证 | 业务规则验证 |
| **Serilog** | 3.0 | 日志框架 | 结构化日志记录 |
| **Swagger/OpenAPI** | 6.5 | API文档 | 自动生成API文档 |
| **NUnit** | 3.13 | 单元测试 | 测试框架 |

## 🏗️ Server端架构设计

### 三层架构模式
```
LYBT.Server (ASP.NET Core Web API)
├── Controllers/          # 控制器层 - API接口
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── PatientsController.cs
│   └── ...
├── Services/             # 服务层 - 业务逻辑
│   ├── Interfaces/       # 服务接口
│   ├── AuthService.cs
│   ├── UserService.cs
│   ├── PatientService.cs
│   └── ...
├── Repositories/         # 仓储层 - 数据访问
│   ├── Interfaces/       # 仓储接口
│   ├── PatientRepository.cs
│   ├── MedicalCaseRepository.cs
│   └── ...
├── Infrastructure/       # 基础设施
│   ├── Data/            # 数据库配置
│   ├── Caching/         # 缓存服务
│   ├── Logging/         # 日志配置
│   ├── Security/        # 安全配置
│   └── Validation/      # 验证配置
├── Models/              # 数据模型
│   ├── Entities/        # 实体模型
│   ├── DTOs/           # 数据传输对象
│   ├── Requests/       # 请求模型
│   └── Responses/      # 响应模型
└── Configuration/       # 配置文件
    ├── appsettings.json
    ├── appsettings.Development.json
    └── appsettings.Production.json
```

## 🔧 开发环境配置

### 1. 项目启动配置
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 配置数据库
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 配置AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 配置FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 配置缓存
builder.Services.AddMemoryCache();

// 配置日志
builder.Services.AddSerilog();

// 配置JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### 2. 数据库配置
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LYBT_Clinic;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "LYBT-Clinic",
    "Audience": "LYBT-Clinic-Users",
    "Key": "YourSecretKeyHereMustBeAtLeast32CharactersLong!",
    "ExpireMinutes": 120,
    "RefreshTokenExpireDays": 7
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
```

## 📝 API开发规范

### 1. 控制器开发
```csharp
// Controllers/PatientsController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService patientService, ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    /// <summary>
    /// 获取患者分页列表
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="keyword">搜索关键词</param>
    /// <returns>患者分页列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResult<PagedResult<PatientDto>>>> GetPatients(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string keyword = null)
    {
        try
        {
            var result = await _patientService.GetPatientsAsync(pageIndex, pageSize, keyword);
            return Ok(ApiResult<PagedResult<PatientDto>>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者列表失败");
            return StatusCode(500, ApiResult<PagedResult<PatientDto>>.Error("服务器内部错误"));
        }
    }

    /// <summary>
    /// 根据ID获取患者详情
    /// </summary>
    /// <param name="id">患者ID</param>
    /// <returns>患者详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResult<PatientDto>>> GetPatient(int id)
    {
        try
        {
            var result = await _patientService.GetPatientByIdAsync(id);
            if (result == null)
            {
                return NotFound(ApiResult<PatientDto>.Error("患者不存在"));
            }
            return Ok(ApiResult<PatientDto>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者详情失败，ID: {Id}", id);
            return StatusCode(500, ApiResult<PatientDto>.Error("服务器内部错误"));
        }
    }

    /// <summary>
    /// 创建新患者
    /// </summary>
    /// <param name="request">患者创建请求</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<ApiResult<PatientDto>>> CreatePatient([FromBody] PatientCreateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResult<PatientDto>.Error("数据验证失败"));
            }

            var result = await _patientService.CreatePatientAsync(request);
            return CreatedAtAction(nameof(GetPatient), new { id = result.Id }, ApiResult<PatientDto>.Success(result));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResult<PatientDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建患者失败");
            return StatusCode(500, ApiResult<PatientDto>.Error("服务器内部错误"));
        }
    }

    /// <summary>
    /// 更新患者信息
    /// </summary>
    /// <param name="id">患者ID</param>
    /// <param name="request">患者更新请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<ApiResult<PatientDto>>> UpdatePatient(int id, [FromBody] PatientUpdateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResult<PatientDto>.Error("数据验证失败"));
            }

            var result = await _patientService.UpdatePatientAsync(id, request);
            if (result == null)
            {
                return NotFound(ApiResult<PatientDto>.Error("患者不存在"));
            }
            return Ok(ApiResult<PatientDto>.Success(result));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResult<PatientDto>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者失败，ID: {Id}", id);
            return StatusCode(500, ApiResult<PatientDto>.Error("服务器内部错误"));
        }
    }

    /// <summary>
    /// 删除患者
    /// </summary>
    /// <param name="id">患者ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResult<bool>>> DeletePatient(int id)
    {
        try
        {
            var result = await _patientService.DeletePatientAsync(id);
            if (!result)
            {
                return NotFound(ApiResult<bool>.Error("患者不存在"));
            }
            return Ok(ApiResult<bool>.Success(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除患者失败，ID: {Id}", id);
            return StatusCode(500, ApiResult<bool>.Error("服务器内部错误"));
        }
    }
}
```

### 2. 服务层开发
```csharp
// Services/PatientService.cs
public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<PatientCreateRequest> _createValidator;
    private readonly IValidator<PatientUpdateRequest> _updateValidator;
    // Issue #1754: 已移除ICacheService，如需缓存请直接注入IMemoryCache
    // private readonly IMemoryCache _cache;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IPatientRepository patientRepository,
        IMapper mapper,
        IValidator<PatientCreateRequest> createValidator,
        IValidator<PatientUpdateRequest> updateValidator,
        // Issue #1754: 如需缓存，直接注入IMemoryCache
        // IMemoryCache cache,
        ILogger<PatientService> logger)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        // _cache = cache;
        _logger = logger;
    }

    public async Task<PatientDto> GetPatientByIdAsync(int id)
    {
        // Issue #1754: 已移除ICacheService，如需缓存可使用IMemoryCache
        // var cacheKey = $"patient:{id}";
        // var cached = _cache.TryGetValue(cacheKey, out PatientDto cachedDto) ? cachedDto : null;
        // if (cached != null) return cached;

        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
        {
            return null;
        }

        var patientDto = _mapper.Map<PatientDto>(patient);

        // Issue #1754: 如需缓存
        // _cache.Set(cacheKey, patientDto, TimeSpan.FromMinutes(30));

        return patientDto;
    }

    public async Task<PagedResult<PatientDto>> GetPatientsAsync(int pageIndex, int pageSize, string keyword = null)
    {
        // Issue #1754: 已移除ICacheService
        // var cacheKey = $"patients:{pageIndex}:{pageSize}:{keyword}";
        // var cached = _cache.TryGetValue(cacheKey, out PagedResult<PatientDto> cachedResult) ? cachedResult : null;
        // if (cached != null) return cached;

        var totalCount = await _patientRepository.CountAsync(keyword);
        var patients = await _patientRepository.GetPagedAsync(pageIndex, pageSize, keyword);
        var patientDtos = _mapper.Map<List<PatientDto>>(patients);

        var result = PagedResult<PatientDto>.Create(patientDtos, pageIndex, pageSize, totalCount);

        // Issue #1754: 如需缓存
        // _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));

        return result;
    }

    public async Task<PatientDto> CreatePatientAsync(PatientCreateRequest request)
    {
        // 验证请求数据
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 检查手机号是否已存在
        if (await _patientRepository.IsPhoneExistsAsync(request.Phone))
        {
            throw new ValidationException("手机号已存在");
        }

        // 检查身份证号是否已存在
        if (!string.IsNullOrEmpty(request.IdCard) && 
            await _patientRepository.IsIdCardExistsAsync(request.IdCard))
        {
            throw new ValidationException("身份证号已存在");
        }

        var patient = _mapper.Map<Patient>(request);
        patient.CreatedAt = DateTime.UtcNow;
        patient.Status = PatientStatus.Active;

        var createdPatient = await _patientRepository.AddAsync(patient);
        var patientDto = _mapper.Map<PatientDto>(createdPatient);

        // 清除相关缓存
        await _cacheService.RemoveAsync("patients:*");

        _logger.LogInformation("创建患者成功，ID: {Id}, 姓名: {Name}", patientDto.Id, patientDto.Name);
        return patientDto;
    }

    public async Task<PatientDto> UpdatePatientAsync(int id, PatientUpdateRequest request)
    {
        // 验证请求数据
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
        {
            return null;
        }

        // 检查手机号是否已被其他患者使用
        if (await _patientRepository.IsPhoneExistsAsync(request.Phone, id))
        {
            throw new ValidationException("手机号已被其他患者使用");
        }

        // 检查身份证号是否已被其他患者使用
        if (!string.IsNullOrEmpty(request.IdCard) && 
            await _patientRepository.IsIdCardExistsAsync(request.IdCard, id))
        {
            throw new ValidationException("身份证号已被其他患者使用");
        }

        _mapper.Map(request, patient);
        patient.UpdatedAt = DateTime.UtcNow;

        var updatedPatient = await _patientRepository.UpdateAsync(patient);
        var patientDto = _mapper.Map<PatientDto>(updatedPatient);

        // 清除相关缓存
        await _cacheService.RemoveAsync($"patient:{id}");
        await _cacheService.RemoveAsync("patients:*");

        _logger.LogInformation("更新患者成功，ID: {Id}, 姓名: {Name}", patientDto.Id, patientDto.Name);
        return patientDto;
    }

    public async Task<bool> DeletePatientAsync(int id)
    {
        var patient = await _patientRepository.GetByIdAsync(id);
        if (patient == null)
        {
            return false;
        }

        // 检查是否有关联的医案或处方
        var hasMedicalCases = await _patientRepository.HasMedicalCasesAsync(id);
        var hasPrescriptions = await _patientRepository.HasPrescriptionsAsync(id);
        
        if (hasMedicalCases || hasPrescriptions)
        {
            throw new ValidationException("患者存在关联的医案或处方，无法删除");
        }

        await _patientRepository.DeleteAsync(patient);

        // 清除相关缓存
        await _cacheService.RemoveAsync($"patient:{id}");
        await _cacheService.RemoveAsync("patients:*");

        _logger.LogInformation("删除患者成功，ID: {Id}", id);
        return true;
    }
}
```

### 3. 仓储层开发
```csharp
// Repositories/PatientRepository.cs
public class PatientRepository : RepositoryBase<Patient>, IPatientRepository
{
    private readonly ApplicationDbContext _context;

    public PatientRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<bool> IsPhoneExistsAsync(string phone, int? excludeId = null)
    {
        var query = _context.Patients.Where(p => p.Phone == phone);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> IsIdCardExistsAsync(string idCard, int? excludeId = null)
    {
        if (string.IsNullOrEmpty(idCard))
        {
            return false;
        }

        var query = _context.Patients.Where(p => p.IdCard == idCard);
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<IEnumerable<Patient>> GetPagedAsync(int pageIndex, int pageSize, string keyword = null)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(p => 
                p.Name.Contains(keyword) || 
                p.Phone.Contains(keyword) ||
                p.IdCard.Contains(keyword));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string keyword = null)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(p => 
                p.Name.Contains(keyword) || 
                p.Phone.Contains(keyword) ||
                p.IdCard.Contains(keyword));
        }

        return await query.CountAsync();
    }

    public async Task<bool> HasMedicalCasesAsync(int patientId)
    {
        return await _context.MedicalCases.AnyAsync(mc => mc.PatientId == patientId);
    }

    public async Task<bool> HasPrescriptionsAsync(int patientId)
    {
        return await _context.Prescriptions.AnyAsync(p => p.PatientId == patientId);
    }

    public async Task<Patient> GetWithDetailsAsync(int id)
    {
        return await _context.Patients
            .Include(p => p.MedicalCases)
            .Include(p => p.Prescriptions)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
```

## 🔒 身份认证与授权

### 1. JWT认证配置
```csharp
// Services/JwtService.cs
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateAccessToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("permissions", string.Join(",", user.Permissions))
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"])),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}
```

### 2. 双轨认证实现
```csharp
// Services/AuthService.cs
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminSecretRepository _adminSecretRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IAdminSecretRepository adminSecretRepository,
        IJwtService jwtService,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _adminSecretRepository = adminSecretRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            // 普通用户认证
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user != null && _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Success)
            {
                var accessToken = _jwtService.GenerateAccessToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                
                // 保存刷新令牌
                await _userRepository.UpdateRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));
                
                _logger.LogInformation("用户登录成功，邮箱: {Email}", request.Email);
                return AuthResult.Success(new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    User = _mapper.Map<UserDto>(user)
                });
            }

            // 超级管理员认证
            var adminSecret = await _adminSecretRepository.GetBySecretAsync(request.Password);
            if (adminSecret != null && adminSecret.IsActive)
            {
                var adminUser = new User
                {
                    Id = -1,
                    Email = "admin@lybt.com",
                    Name = "超级管理员",
                    Role = "SuperAdmin",
                    Permissions = new List<string> { "*" }
                };

                var accessToken = _jwtService.GenerateAccessToken(adminUser);
                var refreshToken = _jwtService.GenerateRefreshToken();
                
                _logger.LogInformation("超级管理员登录成功");
                return AuthResult.Success(new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    User = _mapper.Map<UserDto>(adminUser)
                });
            }

            _logger.LogWarning("登录失败，邮箱: {Email}", request.Email);
            return AuthResult.Failure("用户名或密码错误");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程中发生错误");
            return AuthResult.Failure("登录失败，请稍后重试");
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
            var userId = int.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "0");
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return AuthResult.Failure("无效的刷新令牌");
            }

            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            
            await _userRepository.UpdateRefreshTokenAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(7));
            
            return AuthResult.Success(new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                User = _mapper.Map<UserDto>(user)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌过程中发生错误");
            return AuthResult.Failure("刷新令牌失败");
        }
    }

    public async Task<bool> LogoutAsync(int userId)
    {
        try
        {
            await _userRepository.UpdateRefreshTokenAsync(userId, null, null);
            _logger.LogInformation("用户登出成功，ID: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出过程中发生错误");
            return false;
        }
    }
}
```

## 🧪 单元测试

### 1. 服务层测试
```csharp
// Tests/Services/PatientServiceTests.cs
[TestFixture]
public class PatientServiceTests
{
    private Mock<IPatientRepository> _mockPatientRepository;
    private Mock<IMapper> _mockMapper;
    private Mock<IValidator<PatientCreateRequest>> _mockCreateValidator;
    private Mock<IValidator<PatientUpdateRequest>> _mockUpdateValidator;
    // Issue #1754: 已移除ICacheService
    // 如需缓存测试，使用 Mock<IMemoryCache>
    // private Mock<IMemoryCache> _mockCache;
    private Mock<ILogger<PatientService>> _mockLogger;
    private PatientService _patientService;

    [SetUp]
    public void Setup()
    {
        _mockPatientRepository = new Mock<IPatientRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockCreateValidator = new Mock<IValidator<PatientCreateRequest>>();
        _mockUpdateValidator = new Mock<IValidator<PatientUpdateRequest>>();
        // Issue #1754: 如需缓存测试
        // _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<PatientService>>();

        _patientService = new PatientService(
            _mockPatientRepository.Object,
            _mockMapper.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object,
            // Issue #1754: 如需缓存
            // _mockCache.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task CreatePatientAsync_WithValidRequest_ShouldCreatePatient()
    {
        // Arrange
        var request = new PatientCreateRequest
        {
            Name = "张三",
            Gender = "男",
            BirthDate = new DateTime(1990, 1, 1),
            Phone = "13800138000",
            Address = "北京市朝阳区"
        };

        var validationResult = new ValidationResult();
        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockPatientRepository.Setup(x => x.IsPhoneExistsAsync(request.Phone))
            .ReturnsAsync(false);

        _mockPatientRepository.Setup(x => x.IsIdCardExistsAsync(request.IdCard))
            .ReturnsAsync(false);

        var patient = new Patient { Id = 1, Name = request.Name };
        _mockPatientRepository.Setup(x => x.AddAsync(It.IsAny<Patient>()))
            .ReturnsAsync(patient);

        var patientDto = new PatientDto { Id = 1, Name = request.Name };
        _mockMapper.Setup(x => x.Map<Patient>(request))
            .Returns(patient);
        _mockMapper.Setup(x => x.Map<PatientDto>(patient))
            .Returns(patientDto);

        // Act
        var result = await _patientService.CreatePatientAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("张三", result.Name);

        _mockPatientRepository.Verify(x => x.AddAsync(It.IsAny<Patient>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync("patients:*"), Times.Once);
    }

    [Test]
    public async Task CreatePatientAsync_WithDuplicatePhone_ShouldThrowValidationException()
    {
        // Arrange
        var request = new PatientCreateRequest
        {
            Name = "张三",
            Phone = "13800138000"
        };

        var validationResult = new ValidationResult();
        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockPatientRepository.Setup(x => x.IsPhoneExistsAsync(request.Phone))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _patientService.CreatePatientAsync(request));
        Assert.AreEqual("手机号已存在", ex.Message);
    }
}
```

### 2. 控制器测试
```csharp
// Tests/Controllers/PatientsControllerTests.cs
[TestFixture]
public class PatientsControllerTests
{
    private Mock<IPatientService> _mockPatientService;
    private Mock<ILogger<PatientsController>> _mockLogger;
    private PatientsController _controller;

    [SetUp]
    public void Setup()
    {
        _mockPatientService = new Mock<IPatientService>();
        _mockLogger = new Mock<ILogger<PatientsController>>();
        _controller = new PatientsController(_mockPatientService.Object, _mockLogger.Object);
    }

    [Test]
    public async Task GetPatient_WithValidId_ShouldReturnPatient()
    {
        // Arrange
        var patientId = 1;
        var patientDto = new PatientDto { Id = patientId, Name = "张三" };
        _mockPatientService.Setup(x => x.GetPatientByIdAsync(patientId))
            .ReturnsAsync(patientDto);

        // Act
        var result = await _controller.GetPatient(patientId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        Assert.AreEqual(200, okResult.StatusCode);

        var apiResult = okResult.Value as ApiResult<PatientDto>;
        Assert.IsNotNull(apiResult);
        Assert.IsTrue(apiResult.Success);
        Assert.AreEqual(patientDto.Name, apiResult.Data.Name);
    }

    [Test]
    public async Task CreatePatient_WithValidRequest_ShouldCreatePatient()
    {
        // Arrange
        var request = new PatientCreateRequest
        {
            Name = "张三",
            Gender = "男",
            BirthDate = new DateTime(1990, 1, 1),
            Phone = "13800138000"
        };

        var patientDto = new PatientDto { Id = 1, Name = request.Name };
        _mockPatientService.Setup(x => x.CreatePatientAsync(request))
            .ReturnsAsync(patientDto);

        // Act
        var result = await _controller.CreatePatient(request);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.IsNotNull(createdResult);
        Assert.AreEqual(201, createdResult.StatusCode);

        var apiResult = createdResult.Value as ApiResult<PatientDto>;
        Assert.IsNotNull(apiResult);
        Assert.IsTrue(apiResult.Success);
        Assert.AreEqual(patientDto.Name, apiResult.Data.Name);
    }
}
```

## 📊 性能优化

### 1. 数据库优化
```csharp
// 优化查询
public async Task<PagedResult<PatientDto>> GetPatientsOptimizedAsync(int pageIndex, int pageSize, string keyword = null)
{
    // 使用AsNoTracking提高查询性能
    var query = _context.Patients.AsNoTracking().AsQueryable();

    if (!string.IsNullOrEmpty(keyword))
    {
        query = query.Where(p => 
            EF.Functions.Like(p.Name, $"%{keyword}%") || 
            EF.Functions.Like(p.Phone, $"%{keyword}%"));
    }

    // 使用Select只查询需要的字段
    var result = await query
        .Select(p => new PatientDto
        {
            Id = p.Id,
            Name = p.Name,
            Gender = p.Gender,
            BirthDate = p.BirthDate,
            Phone = p.Phone,
            Address = p.Address,
            Age = DateTime.Today.Year - p.BirthDate.Year,
            CreatedAt = p.CreatedAt
        })
        .OrderByDescending(p => p.CreatedAt)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var totalCount = await query.CountAsync();
    return PagedResult<PatientDto>.Create(result, pageIndex, pageSize, totalCount);
}
```

### 2. 缓存策略
```csharp
// 分层缓存
public async Task<PatientDto> GetPatientWithCacheAsync(int id)
{
    var cacheKey = $"patient:{id}";
    
    // L1: 内存缓存
    var cached = await _cacheService.GetAsync<PatientDto>(cacheKey);
    if (cached != null)
    {
        return cached;
    }

    // L2: 数据库查询
    var patient = await _patientRepository.GetByIdAsync(id);
    if (patient == null)
    {
        return null;
    }

    var patientDto = _mapper.Map<PatientDto>(patient);
    
    // 设置缓存，30分钟过期
    await _cacheService.SetAsync(cacheKey, patientDto, TimeSpan.FromMinutes(30));
    
    return patientDto;
}
```

## 🚀 部署配置

### 1. Docker配置
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LYBT.Server/LYBT.Server.csproj", "LYBT.Server/"]
COPY ["LYBT.Shared/LYBT.Shared.csproj", "LYBT.Shared/"]
RUN dotnet restore "LYBT.Server/LYBT.Server.csproj"
COPY . .
WORKDIR "/src/LYBT.Server"
RUN dotnet build "LYBT.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LYBT.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LYBT.Server.dll"]
```

### 2. 环境变量配置
```bash
# 开发环境
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Server=localhost;Database=LYBT_Clinic_Dev;Trusted_Connection=true"
export Jwt__Key="YourSecretKeyHereMustBeAtLeast32CharactersLong!"
export Jwt__Issuer="LYBT-Clinic-Dev"
export Jwt__Audience="LYBT-Clinic-Users-Dev"

# 生产环境
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=LYBT_Clinic;User Id=appuser;Password=yourpassword"
export Jwt__Key="ProductionSecretKeyHereMustBeAtLeast32CharactersLong!"
export Jwt__Issuer="LYBT-Clinic-Prod"
export Jwt__Audience="LYBT-Clinic-Users-Prod"
```

## 🔗 相关文档

- **[架构总览](../../architecture/README.md)** - 三层对齐架构设计原理
- **[Server端架构](../../architecture/server/README.md)** - 服务端三层架构实现
- **[开发指南总览](../README.md)** - 开发规范和流程指导
- **[API文档](../../api/README.md)** - API接口详细文档
- **[测试指南](../shared/testing-guide.md)** - 单元测试和集成测试指南

---

**文档维护**：后端开发组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核