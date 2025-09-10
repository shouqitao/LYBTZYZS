# LYBT.Module.Auth 项目文档

## 📋 项目概述

**LYBT.Module.Auth**是凌隐宝堂中医诊所系统的身份认证与授权核心模块，负责用户身份验证、JWT令牌管理、角色权限控制和会话管理。作为整个系统安全的基石，Auth模块确保只有经过认证和授权的用户才能访问相应的业务功能。

### 项目职责
- **身份认证**: 用户登录验证、密码校验和会话管理
- **JWT令牌管理**: Access Token和Refresh Token的生成、验证和刷新
- **角色权限控制**: 基于角色的访问控制(RBAC)，支持Admin和Doctor角色
- **会话安全**: 登录状态管理、令牌过期处理和安全登出
- **密码安全**: BCrypt哈希加密、密码策略和首次登录强制修改
- **安全审计**: 登录日志记录、失败尝试监控和异常行为追踪

### 在系统中的位置
Auth模块作为安全网关，位于所有业务模块之前，为整个系统提供统一的认证授权服务。它与Infrastructure的JWT认证框架深度集成，为WebAPI的所有控制器提供安全保障。

### 关键业务价值
- **系统安全**: 确保系统访问的安全性和合规性
- **用户体验**: 提供流畅的登录体验和会话管理
- **权限管理**: 精确控制不同角色的功能访问权限
- **审计合规**: 完整的认证日志满足医疗行业合规要求

## 🏗️ 技术架构

### 项目架构设计
Auth模块采用UltraThink双层架构标准：

```
AuthService (纯委托层)
    ├── AuthQueryService (查询专业层)
    │   ├── 获取用户认证状态查询
    │   ├── 会话历史和审计日志查询
    │   └── 权限验证和角色检查查询
    └── AuthBusinessService (业务逻辑层)
        ├── 用户登录认证流程
        ├── JWT令牌生成和刷新
        ├── 密码验证和修改
        └── 会话管理和登出处理
```

### 核心技术栈
- **.NET 8.0**: 现代C#语言特性和高性能运行时
- **BCrypt.Net**: 安全的密码哈希加密库
- **System.IdentityModel.Tokens.Jwt**: JWT令牌处理
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT认证中间件
- **AutoMapper**: 实体和DTO对象映射
- **Microsoft.Extensions.Logging**: 结构化日志记录
- **Microsoft.Extensions.Caching.Memory**: 会话缓存管理

### 依赖项目列表
**直接依赖**:
- `LYBT.Infrastructure` - JWT认证框架和数据访问支持
- `LYBT.Entities` - UserModel和AdminSecretModel实体
- `LYBT.Shared.Models` - 认证相关DTO定义
- `LYBT.Shared.Interfaces` - 认证服务接口契约
- `LYBT.Shared.Utilities` - 密码处理和验证工具

**被依赖项目**:
- `LYBT.WebAPI` - 控制器层调用认证服务
- 所有业务模块 - 通过JWT中间件获得用户身份信息

### 设计模式采用
- **Service Pattern**: 双层服务架构，职责清晰分离
- **Strategy Pattern**: 不同认证策略的灵活实现
- **Factory Pattern**: JWT令牌生成工厂
- **Repository Pattern**: 通过Infrastructure访问用户数据
- **Decorator Pattern**: 认证结果的增强和装饰

## 🎯 功能规范

### 必须实现的功能清单

#### 1. 核心认证功能
- ✅ **用户登录**: 用户名/密码验证，支持管理员和医生登录
- ✅ **JWT令牌生成**: 包含用户信息和角色的安全令牌
- ✅ **令牌验证**: 中间件自动验证请求令牌有效性
- ✅ **令牌刷新**: RefreshToken机制延长会话有效期
- ✅ **安全登出**: 令牌失效和会话清理
- ✅ **首次登录**: 强制修改初始密码的安全流程

#### 2. 角色权限管理
- ✅ **角色定义**: Admin（管理员）和Doctor（医生）两种角色
- ✅ **权限控制**: 基于角色的API访问控制
- ✅ **权限验证**: 运行时权限检查和拒绝处理
- ✅ **角色切换**: 支持多角色用户的权限切换（如适用）

#### 3. 密码安全管理
- ✅ **密码加密**: BCrypt哈希+盐值安全存储
- ✅ **密码验证**: 登录时的密码正确性验证
- ✅ **密码修改**: 用户主动修改密码功能
- ✅ **密码策略**: 最小长度、复杂度要求验证
- ✅ **初始密码**: 新用户首次登录强制修改密码

#### 4. 会话管理功能
- ✅ **会话创建**: 登录成功后创建用户会话
- ✅ **会话维持**: Access Token 8小时有效期管理
- ✅ **会话延长**: Refresh Token 30天自动刷新
- ✅ **会话失效**: 登出或超时后的会话清理
- ✅ **并发会话**: 支持同一用户多设备登录管理

#### 5. 安全审计功能
- ✅ **登录日志**: 成功登录的时间、IP、设备记录
- ✅ **失败记录**: 登录失败次数和原因追踪
- ✅ **异常监控**: 多次失败尝试和可疑行为检测
- ✅ **权限审计**: 权限验证失败的详细日志记录

### 接口定义规范

#### IAuthService主服务接口
```csharp
public interface IAuthService
{
    Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ServiceResult<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<ServiceResult<bool>> LogoutAsync(LogoutRequestDto request);
    Task<ServiceResult<bool>> ChangePasswordAsync(ChangePasswordDto dto);
    Task<ServiceResult<UserProfileDto>> GetCurrentUserAsync(Guid userId);
    Task<ServiceResult<bool>> ValidateTokenAsync(string token);
}
```

#### IAuthQueryService查询服务接口
```csharp
public interface IAuthQueryService
{
    Task<ServiceResult<bool>> IsTokenValidAsync(string token);
    Task<ServiceResult<UserAuthInfoDto>> GetUserAuthInfoAsync(Guid userId);
    Task<ServiceResult<List<LoginHistoryDto>>> GetLoginHistoryAsync(Guid userId, int pageNumber, int pageSize);
    Task<ServiceResult<AuthStatisticsDto>> GetAuthStatisticsAsync(DateTime startDate, DateTime endDate);
    Task<ServiceResult<bool>> HasPermissionAsync(Guid userId, string permission);
}
```

#### IAuthBusinessService业务服务接口
```csharp
public interface IAuthBusinessService
{
    Task<ServiceResult<LoginResponseDto>> ProcessLoginAsync(LoginRequestDto request);
    Task<ServiceResult<RefreshTokenResponseDto>> ProcessTokenRefreshAsync(RefreshTokenRequestDto request);
    Task<ServiceResult<bool>> ProcessLogoutAsync(LogoutRequestDto request);
    Task<ServiceResult<bool>> ProcessPasswordChangeAsync(ChangePasswordDto dto);
    Task<ServiceResult<bool>> ProcessFirstLoginPasswordChangeAsync(FirstLoginPasswordChangeDto dto);
}
```

### 数据模型定义

#### LoginRequestDto登录请求
```csharp
public class LoginRequestDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; } = false;
    
    public string? ClientInfo { get; set; }
    public string? IpAddress { get; set; }
}
```

#### LoginResponseDto登录响应
```csharp
public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = null!;
    public bool IsFirstLogin { get; set; }
    public List<string> Permissions { get; set; } = new();
}
```

#### UserProfileDto用户资料
```csharp
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime LastLoginTime { get; set; }
    public bool IsActive { get; set; }
}
```

#### ChangePasswordDto密码修改
```csharp
public class ChangePasswordDto
{
    [Required(ErrorMessage = "用户ID不能为空")]
    public Guid UserId { get; set; }
    
    [Required(ErrorMessage = "当前密码不能为空")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "新密码长度必须在8-100个字符之间")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "新密码必须包含大小写字母、数字和特殊字符")]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("NewPassword", ErrorMessage = "确认密码与新密码不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### 业务规则约束
1. **登录规则**: 用户名和密码必须匹配数据库记录，支持大小写敏感
2. **令牌规则**: Access Token 8小时有效，Refresh Token 30天有效
3. **密码规则**: 最少8位，必须包含大小写字母、数字和特殊字符
4. **角色规则**: Admin可访问所有功能，Doctor仅限业务功能
5. **会话规则**: 同一用户可多设备登录，但令牌独立管理
6. **安全规则**: 连续5次登录失败锁定账户15分钟
7. **审计规则**: 所有认证操作必须记录详细日志

## 📋 开发规范

### 代码结构要求
```
src/Server/Modules/LYBT.Module.Auth/
├── Services/
│   ├── AuthQueryService.cs          # 查询专业层
│   ├── AuthBusinessService.cs       # 业务逻辑层
│   └── AuthService.cs               # 纯委托层
├── Controllers/
│   └── AuthController.cs            # API控制器
├── DTOs/
│   ├── LoginRequestDto.cs           # 登录请求DTO
│   ├── LoginResponseDto.cs          # 登录响应DTO
│   ├── ChangePasswordDto.cs         # 密码修改DTO
│   └── UserProfileDto.cs            # 用户资料DTO
├── Validators/
│   ├── LoginRequestValidator.cs     # 登录请求验证器
│   └── PasswordPolicyValidator.cs   # 密码策略验证器
├── Mapping/
│   └── AuthMappingProfile.cs        # AutoMapper配置
├── Exceptions/
│   ├── AuthenticationException.cs   # 认证异常
│   └── AuthorizationException.cs    # 授权异常
└── AuthModule.cs                    # 模块依赖注入注册
```

### UltraThink双层架构实现

#### AuthService主服务(纯委托)
```csharp
public class AuthService : IAuthService
{
    private readonly IAuthQueryService _queryService;
    private readonly IAuthBusinessService _businessService;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(IAuthQueryService queryService, 
                      IAuthBusinessService businessService,
                      ILogger<AuthService> logger)
    {
        _queryService = queryService;
        _businessService = businessService;
        _logger = logger;
    }
    
    public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto request)
        => await _businessService.ProcessLoginAsync(request);
    
    public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        => await _queryService.IsTokenValidAsync(token);
    
    public async Task<ServiceResult<UserProfileDto>> GetCurrentUserAsync(Guid userId)
        => await _queryService.GetUserAuthInfoAsync(userId);
        
    // 其他方法类似的纯委托实现...
}
```

#### AuthQueryService查询专业层
```csharp
public class AuthQueryService : IAuthQueryService
{
    private readonly IRepository<UserModel> _userRepository;
    private readonly IJwtHelper _jwtHelper;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthQueryService> _logger;
    
    public async Task<ServiceResult<bool>> IsTokenValidAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return ServiceResult<bool>.Failure("令牌不能为空");
            
            var isValid = _jwtHelper.ValidateToken(token);
            return ServiceResult<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "令牌验证失败: {Token}", token);
            return ServiceResult<bool>.Failure("令牌验证失败");
        }
    }
    
    public async Task<ServiceResult<UserAuthInfoDto>> GetUserAuthInfoAsync(Guid userId)
    {
        try
        {
            var cacheKey = $"user_auth_info_{userId}";
            if (_cache.TryGetValue(cacheKey, out UserAuthInfoDto? cachedInfo))
                return ServiceResult<UserAuthInfoDto>.Success(cachedInfo!);
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ServiceResult<UserAuthInfoDto>.Failure("用户不存在");
            
            var authInfo = _mapper.Map<UserAuthInfoDto>(user);
            _cache.Set(cacheKey, authInfo, TimeSpan.FromMinutes(10));
            
            return ServiceResult<UserAuthInfoDto>.Success(authInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户认证信息失败: {UserId}", userId);
            return ServiceResult<UserAuthInfoDto>.Failure("获取用户信息失败");
        }
    }
}
```

#### AuthBusinessService业务逻辑层
```csharp
public class AuthBusinessService : IAuthBusinessService
{
    private readonly IRepository<UserModel> _userRepository;
    private readonly IRepository<AdminSecretModel> _adminRepository;
    private readonly IJwtHelper _jwtHelper;
    private readonly IPasswordHelper _passwordHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthBusinessService> _logger;
    
    public async Task<ServiceResult<LoginResponseDto>> ProcessLoginAsync(LoginRequestDto request)
    {
        try
        {
            _logger.LogInformation("处理用户登录请求: {Username}", request.Username);
            
            // 1. 查找用户
            var user = await FindUserAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("登录失败 - 用户不存在: {Username}", request.Username);
                return ServiceResult<LoginResponseDto>.Failure("用户名或密码错误");
            }
            
            // 2. 验证密码
            if (!_passwordHelper.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("登录失败 - 密码错误: {Username}", request.Username);
                await RecordLoginFailureAsync(user.Id, "密码错误");
                return ServiceResult<LoginResponseDto>.Failure("用户名或密码错误");
            }
            
            // 3. 检查账户状态
            if (user.Status != CommonStatus.Active)
            {
                _logger.LogWarning("登录失败 - 账户已禁用: {Username}", request.Username);
                return ServiceResult<LoginResponseDto>.Failure("账户已被禁用");
            }
            
            // 4. 生成令牌
            var accessToken = _jwtHelper.GenerateToken(user);
            var refreshToken = _jwtHelper.GenerateRefreshToken();
            
            // 5. 更新最后登录时间
            await UpdateLastLoginTimeAsync(user.Id);
            
            // 6. 记录登录成功
            await RecordLoginSuccessAsync(user.Id, request.IpAddress, request.ClientInfo);
            
            // 7. 构建响应
            var response = new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(8),
                User = _mapper.Map<UserProfileDto>(user),
                IsFirstLogin = user.IsFirstLogin,
                Permissions = GetUserPermissions(user.Role)
            };
            
            _logger.LogInformation("用户登录成功: {Username}, UserId: {UserId}", request.Username, user.Id);
            return ServiceResult<LoginResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理登录请求异常: {Username}", request.Username);
            return ServiceResult<LoginResponseDto>.Failure("登录处理失败，请稍后重试");
        }
    }
    
    private async Task<UserModel?> FindUserAsync(string username)
    {
        // 首先尝试从普通用户表查找
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        
        if (user != null) return user;
        
        // 如果是超级管理员，从AdminSecrets表查找
        var adminSecrets = await _adminRepository.GetAllAsync();
        var adminSecret = adminSecrets.FirstOrDefault(a => a.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
        
        if (adminSecret != null)
        {
            // 将AdminSecret转换为UserModel
            return new UserModel
            {
                Id = adminSecret.Id,
                Username = adminSecret.UserName,
                PasswordHash = adminSecret.PasswordHash,
                FullName = "超级管理员",
                Email = "admin@lybt.com",
                Role = UserRole.Admin,
                Status = CommonStatus.Active,
                IsFirstLogin = false
            };
        }
        
        return null;
    }
    
    private List<string> GetUserPermissions(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => new List<string>
            {
                "users:read", "users:write", "users:delete",
                "patients:read", "patients:write", "patients:delete",
                "system:monitor", "system:configure"
            },
            UserRole.Doctor => new List<string>
            {
                "patients:read", "patients:write",
                "medicalcase:read", "medicalcase:write",
                "consultation:read", "consultation:write",
                "prescription:read", "prescription:write"
            },
            _ => new List<string>()
        };
    }
}
```

### 命名规范
- **服务类**: PascalCase + Service后缀 (AuthService, AuthQueryService)
- **DTO类**: PascalCase + Dto后缀 (LoginRequestDto, UserProfileDto)
- **异常类**: PascalCase + Exception后缀 (AuthenticationException)
- **接口**: I前缀 + PascalCase (IAuthService, IAuthQueryService)
- **方法**: PascalCase，异步方法Async后缀
- **常量**: UPPER_SNAKE_CASE (MAX_LOGIN_ATTEMPTS)

### 质量标准
- **安全性**: 所有密码使用BCrypt哈希，敏感信息不记录日志
- **异常处理**: 认证失败不暴露具体原因，统一返回模糊错误信息
- **日志记录**: 详细记录认证过程，但不包含敏感信息
- **性能要求**: 登录响应时间<1秒，令牌验证<100ms
- **缓存策略**: 用户信息缓存10分钟，令牌状态缓存5分钟
- **并发安全**: 支持多线程并发认证请求

### 测试要求
- **单元测试覆盖率**: >90%，特别是核心认证逻辑
- **集成测试**: 完整的登录流程和令牌刷新流程
- **安全测试**: SQL注入、密码暴力破解、令牌伪造等攻击测试
- **性能测试**: 并发登录请求和高频令牌验证测试

## 🔌 集成接口

### 对外提供的接口

#### 1. RESTful API接口
```http
# 用户登录
POST /api/v1/auth/login
Content-Type: application/json
{
    "username": "doctor01",
    "password": "SecurePassword123!",
    "rememberMe": true
}

# 响应
{
    "success": true,
    "data": {
        "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "refreshToken": "refresh_token_here",
        "expiresAt": "2025-09-01T18:30:00Z",
        "user": {
            "id": "123e4567-e89b-12d3-a456-426614174000",
            "username": "doctor01",
            "fullName": "张医生",
            "role": "Doctor"
        },
        "isFirstLogin": false
    }
}
```

```http
# 刷新令牌
POST /api/v1/auth/refresh
{
    "refreshToken": "refresh_token_here"
}

# 修改密码
PUT /api/v1/auth/password
Authorization: Bearer <access_token>
{
    "currentPassword": "OldPassword123!",
    "newPassword": "NewPassword123!",
    "confirmPassword": "NewPassword123!"
}

# 获取当前用户信息
GET /api/v1/auth/profile
Authorization: Bearer <access_token>

# 用户登出
POST /api/v1/auth/logout
Authorization: Bearer <access_token>
{
    "refreshToken": "refresh_token_here"
}
```

#### 2. 内部服务接口
```csharp
// 其他业务模块可以通过依赖注入使用
public class SomeBusinessService
{
    private readonly IAuthService _authService;
    
    public async Task<bool> CheckUserPermission(Guid userId, string permission)
    {
        var result = await _authService.HasPermissionAsync(userId, permission);
        return result.IsSuccess && result.Data;
    }
}
```

### 依赖的外部接口
- **IJwtHelper**: Infrastructure提供的JWT令牌处理服务
- **IRepository<T>**: Infrastructure提供的数据访问接口
- **IPasswordHelper**: Shared.Utilities提供的密码处理工具
- **IMemoryCache**: .NET提供的内存缓存服务
- **ILogger<T>**: .NET提供的日志记录服务

### 数据传输格式

#### JWT令牌载荷格式
```json
{
    "sub": "123e4567-e89b-12d3-a456-426614174000",
    "username": "doctor01", 
    "email": "doctor01@lybt.com",
    "role": "Doctor",
    "fullname": "张医生",
    "exp": 1693555200,
    "iat": 1693526400,
    "iss": "LYBT.WebAPI",
    "aud": "LYBT.Client"
}
```

#### 认证状态响应格式
```json
{
    "isAuthenticated": true,
    "user": {
        "id": "guid",
        "username": "string",
        "role": "Admin|Doctor",
        "permissions": ["permission1", "permission2"]
    },
    "session": {
        "expiresAt": "datetime",
        "issuedAt": "datetime",
        "lastActivity": "datetime"
    }
}
```

### 错误处理规范
- **401 Unauthorized**: 令牌无效、过期或未提供
- **403 Forbidden**: 权限不足，角色不满足要求
- **400 Bad Request**: 登录参数无效或密码不符合策略
- **429 Too Many Requests**: 登录尝试过于频繁，触发限流
- **500 Internal Server Error**: 认证服务内部错误

## ⚙️ 配置管理

### 配置项定义

#### 认证相关配置
```json
{
  "JwtOptions": {
    "Key": "YourSuperSecureKeyHere_MustBe256BitsOrMore",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "ExpireMinutes": 480,
    "RefreshTokenExpireDays": 30
  },
  "AuthOptions": {
    "MaxLoginAttempts": 5,
    "LockoutDurationMinutes": 15,
    "PasswordRequiredLength": 8,
    "RequireUppercase": true,
    "RequireLowercase": true,
    "RequireDigit": true,
    "RequireSpecialCharacter": true,
    "SessionTimeoutMinutes": 480,
    "RefreshTokenLifespanDays": 30,
    "EnableConcurrentSessions": true,
    "MaxConcurrentSessions": 3
  },
  "SecurityOptions": {
    "EnableLoginAuditLog": true,
    "EnableFailureTracking": true,
    "EnableIpWhitelist": false,
    "AllowedIpRanges": [],
    "EnableBruteForceProtection": true,
    "PasswordHashWorkFactor": 12
  }
}
```

### 环境变量要求
```bash
# JWT配置（生产环境必须覆盖）
JWTOPTIONS__KEY="ProductionSecureKey256BitsMinimum_ChangeThisInProduction"
JWTOPTIONS__EXPIREMINUTES=480
JWTOPTIONS__REFRESHTOKENEXPIREDAYS=30

# 认证配置
AUTHOPTIONS__MAXLOGINATTEMPTS=5
AUTHOPTIONS__LOCKOUTDURATIONMINUTES=15
AUTHOPTIONS__ENABLECONCURRENTSESSIONS=true

# 安全配置
SECURITYOPTIONS__ENABLELOGINAUDITLOG=true
SECURITYOPTIONS__ENABLEBRUTEFORCEPROTECTION=true
SECURITYOPTIONS__PASSWORDHASHWORKFACTOR=12

# 超级管理员配置（首次初始化）
ADMIN_DEFAULT_USERNAME="sysadmin"
ADMIN_DEFAULT_PASSWORD="Admin@123456"
```

### 部署配置说明
1. **开发环境**: 使用默认配置，JWT密钥可以是测试密钥
2. **测试环境**: 使用生产级密钥，但可以降低安全要求以便测试
3. **生产环境**: 必须使用环境变量覆盖所有敏感配置
4. **高可用部署**: 考虑使用分布式缓存共享会话状态

## 🧪 测试规范

### 单元测试要求

#### 认证业务逻辑测试
```csharp
public class AuthBusinessServiceTests : IDisposable
{
    private readonly Mock<IRepository<UserModel>> _mockUserRepository;
    private readonly Mock<IJwtHelper> _mockJwtHelper;
    private readonly Mock<IPasswordHelper> _mockPasswordHelper;
    private readonly AuthBusinessService _service;
    
    public AuthBusinessServiceTests()
    {
        _mockUserRepository = new Mock<IRepository<UserModel>>();
        _mockJwtHelper = new Mock<IJwtHelper>();
        _mockPasswordHelper = new Mock<IPasswordHelper>();
        
        var mapper = CreateMapper();
        var logger = Mock.Of<ILogger<AuthBusinessService>>();
        
        _service = new AuthBusinessService(
            _mockUserRepository.Object,
            _mockJwtHelper.Object,
            _mockPasswordHelper.Object,
            mapper,
            logger);
    }
    
    [Fact]
    public async Task ProcessLoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "TestPass123!"
        };
        
        var user = new UserModel
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            PasswordHash = "hashed_password",
            Status = CommonStatus.Active
        };
        
        _mockUserRepository.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<UserModel> { user });
        _mockPasswordHelper.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
                          .Returns(true);
        _mockJwtHelper.Setup(j => j.GenerateToken(user))
                     .Returns("valid_jwt_token");
        
        // Act
        var result = await _service.ProcessLoginAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.User.Username.Should().Be("testuser");
    }
    
    [Fact]
    public async Task ProcessLoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "wrongpassword"
        };
        
        var user = new UserModel
        {
            Username = "testuser",
            PasswordHash = "hashed_password",
            Status = CommonStatus.Active
        };
        
        _mockUserRepository.Setup(r => r.GetAllAsync())
                          .ReturnsAsync(new List<UserModel> { user });
        _mockPasswordHelper.Setup(p => p.VerifyPassword(request.Password, user.PasswordHash))
                          .Returns(false);
        
        // Act
        var result = await _service.ProcessLoginAsync(request);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名或密码错误");
    }
    
    [Theory]
    [InlineData(null, "密码不能为空")]
    [InlineData("", "密码不能为空")]
    [InlineData("123", "密码长度必须在8-100个字符之间")]
    [InlineData("password", "密码必须包含大小写字母、数字和特殊字符")]
    public async Task ProcessPasswordChangeAsync_InvalidPassword_ReturnsValidationError(
        string newPassword, string expectedError)
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "OldPass123!",
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };
        
        // Act
        var result = await _service.ProcessPasswordChangeAsync(dto);
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain(expectedError);
    }
}
```

### 集成测试要求

#### 认证API集成测试
```csharp
public class AuthApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public AuthApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task POST_Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "sysadmin",
            Password = "Admin@123456",
            RememberMe = false
        };
        
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<LoginResponseDto>>(responseContent);
        
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.AccessToken.Should().NotBeNullOrEmpty();
        apiResponse.Data.RefreshToken.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Should().NotBeNull();
    }
    
    [Fact]
    public async Task GET_Profile_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        // Act
        var response = await _client.GetAsync("/api/v1/auth/profile");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserProfileDto>>(responseContent);
        
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data!.Username.Should().NotBeNullOrEmpty();
    }
}
```

### 安全测试要求

#### 认证安全测试
```csharp
public class AuthSecurityTests
{
    [Fact]
    public async Task Login_MultipleFailedAttempts_TriggersLockout()
    {
        // 测试多次登录失败是否触发账户锁定
    }
    
    [Fact]
    public async Task JWT_ExpiredToken_ReturnsUnauthorized()
    {
        // 测试过期令牌是否正确拒绝
    }
    
    [Fact]
    public async Task JWT_TamperedToken_ReturnsUnauthorized()
    {
        // 测试篡改令牌是否被检测
    }
    
    [Fact]
    public void Password_WeakPassword_FailsValidation()
    {
        // 测试弱密码验证
    }
    
    [Fact]
    public async Task Login_SQLInjectionAttempt_SafelyHandled()
    {
        // 测试SQL注入防护
    }
}
```

### 测试覆盖率目标
- **核心认证逻辑**: 100%覆盖率
- **密码安全功能**: >95%覆盖率
- **令牌管理功能**: >90%覆盖率
- **API端点**: >85%覆盖率
- **异常处理**: >80%覆盖率

## 🚀 部署说明

### 构建要求
- **.NET 8.0 SDK**: 编译Auth模块
- **BCrypt.Net依赖**: 确保密码哈希库正确安装
- **JWT库依赖**: 确保令牌处理库版本兼容

### 部署步骤

#### 1. 模块部署验证
```bash
# 验证Auth模块编译
dotnet build LYBT.Module.Auth.csproj

# 验证服务注册
dotnet run --project LYBT.WebAPI
curl http://localhost:5000/api/v1/auth/login -d '{"username":"test","password":"test"}'
```

#### 2. 超级管理员初始化
```bash
# 确保数据库包含默认管理员
dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI

# 验证默认管理员可以登录
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"Admin@123456"}'
```

#### 3. JWT配置验证
```bash
# 验证JWT密钥配置正确
dotnet run --project LYBT.WebAPI --environment Production

# 测试令牌生成和验证
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/v1/auth/profile
```

### 环境依赖
- **JWT密钥**: 生产环境必须使用256位以上安全密钥
- **数据库访问**: 需要UserModel和AdminSecretModel表的读写权限
- **缓存服务**: 需要IMemoryCache或分布式缓存服务
- **日志系统**: 需要配置认证日志的存储和轮转

### 运行监控

#### 认证性能监控
```http
# 登录成功率监控
GET /api/v1/monitoring/auth/success-rate?period=1h

# 令牌验证性能
GET /api/v1/monitoring/auth/token-validation-metrics

# 认证异常监控
GET /api/v1/monitoring/auth/exceptions?severity=error
```

#### 安全审计监控
```http
# 登录失败统计
GET /api/v1/monitoring/auth/failed-logins?period=24h

# 可疑登录活动
GET /api/v1/monitoring/auth/suspicious-activities

# 权限验证失败
GET /api/v1/monitoring/auth/authorization-failures
```

## 📚 相关文档

### 相关项目文档链接
- [LYBT.Infrastructure项目文档](../core/infrastructure.md) - JWT认证框架和基础设施
- [LYBT.Shared.Utilities项目文档](../../shared/shared-utilities.md) - 密码处理和验证工具
- [用户管理模块文档](./users.md) - 用户实体和角色管理

### API文档链接
- [认证API完整规范](../../../api/auth-api.md) - REST API接口详细定义
- [JWT令牌规范](../../../security/jwt-token-specification.md) - 令牌格式和验证标准
- [权限系统设计](../../../security/permission-system.md) - RBAC权限模型

### 技术规范引用
- [UltraThink双层架构标准](../../../ultrathink/ultrathink-comprehensive-refactoring-complete-20250831.md) - 架构设计规范
- [密码安全最佳实践](../../../security/password-security-guide.md) - 密码策略和加密标准
- [认证安全指南](../../../security/authentication-security.md) - 认证系统安全实施
- [审计日志规范](../../../development/audit-logging-standard.md) - 安全审计日志标准

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**最后更新**: 2025-09-01  
**维护者**: UltraThink项目组  
**审核状态**: ✅ 已审核通过