# 🔐 认证模块 (Auth)

## 📦 模块定位

- **层级**：Server端 + Client端
- **类型**：基础支撑模块（认证授权）
- **职责**：提供系统身份认证、权限管理、令牌管理和会话管理功能。采用双轨认证机制，支持普通用户认证（Users表）和超级管理员认证（AdminSecrets表物理隔离），确保系统安全性和用户体验的平衡。

## 🎯 功能概述

认证模块是系统的基础模块，负责用户身份验证和访问控制。通过双轨认证机制、JWT无状态令牌和RefreshToken刷新机制，实现安全可靠的身份管理。Client端采用MVVM架构，提供友好的登录界面、API健康检查和凭证存储功能。

### 核心价值

- **双轨认证架构**：超级管理员与普通用户物理隔离，提升安全性
- **JWT无状态认证**：AccessToken + RefreshToken机制，支持令牌刷新和撤销
- **会话管理**：记录用户登录设备、IP地址、过期时间，支持多端登录控制
- **密码安全**：BCrypt哈希算法（工作因子12），强密码策略
- **用户体验优化**：记住密码、API健康检查、角色自动导航

## 🏗️ 模块架构

### Server端架构（LYBT.Module.Auth）

```
LYBT.Module.Auth/
├── AuthModule.cs                       # 模块依赖注入注册
│   └── AddAuthModule()                 # 依赖注入配置(仓储+服务+验证器)
├── Interfaces/                         # 模块接口定义
│   └── IAuthRepository.cs              # 认证仓储接口
├── Services/                           # 业务逻辑实现
│   ├── AuthService.cs                  # 认证服务(9个核心方法)
│   │   ├── IsSuperAdminCredentials()   # 超级管理员凭证验证
│   │   ├── VerifyCredentialsAsync()    # 普通用户凭证验证
│   │   ├── LoginAsync()                # 统一登录入口(双轨认证)
│   │   ├── LogoutAsync()               # 用户登出(撤销所有会话)
│   │   ├── RefreshTokenAsync()         # 刷新访问令牌
│   │   ├── ValidateTokenAsync()        # 验证JWT令牌
│   │   ├── RevokeTokenAsync()          # 撤销RefreshToken
│   │   ├── GetSessionInfoAsync()       # 获取会话信息
│   │   └── ChangeSysAdminPasswordAsync() # 修改超管密码
│   └── JwtService.cs                   # JWT令牌生成与验证
│       ├── GenerateToken()             # 生成AccessToken
│       ├── ValidateToken()             # 验证JWT令牌
│       └── GetClaimsFromToken()        # 解析JWT Claims
├── Repositories/                       # 数据仓储实现
│   └── AuthRepository.cs               # 认证仓储(会话管理、密钥查询)
├── Validators/                         # FluentValidation验证器
│   ├── LoginRequestValidator.cs        # 登录请求验证
│   └── ChangePasswordRequestValidator.cs # 修改密码请求验证
└── Mapping/                            # AutoMapper映射配置
    └── AuthMappingProfile.cs           # Entity ↔ DTO映射规则
```

**依赖关系**：
- **依赖项目**：LYBT.Entities（UserModel、AuthSessionModel、AdminSecretModel）、LYBT.Infrastructure（AppDbContext）、LYBT.Shared.Models（AuthDto）
- **被依赖项目**：所有业务模块（Users、Patients、MedicalCases等）、LYBT.WebAPI（AuthController）

### Client端架构（LYBT.Desktop.Auth）

```
LYBT.Desktop.Auth/
├── ViewModels/                         # MVVM视图模型层(1个)
│   └── LoginViewModel.cs               # 登录视图模型(9属性+7方法)
│       ├── 属性(9): Username, Password, RememberMe, RememberPassword,
│       │           HasSavedPassword, ApiStatus, ApiStatusMessage,
│       │           HasMessage, LoginCommand
│       └── 方法(7): 构造函数, CheckApiHealthAsyncSafe,
│                   LoadSavedCredentialsAsync, CheckApiHealthAsync,
│                   CanExecuteLogin, ExecuteLoginAsync, NavigateBasedOnRole
├── Views/                              # WPF视图层(4个)
│   ├── LoginView.xaml                  # 登录视图(作为UserControl嵌入)
│   ├── LoginView.xaml.cs               # LoginView代码后置
│   ├── LoginWindow.xaml                # 登录窗口(独立Window)
│   └── LoginWindow.xaml.cs             # LoginWindow代码后置
└── AuthenticationModule.cs             # Prism模块定义(2个方法)
    ├── OnInitialized()                 # 模块初始化
    └── RegisterTypes()                 # 类型注册(Views + ViewModels)
```

**依赖关系**：
- **依赖服务**：IAuthenticationService、ITokenStorageService、ISecureCredentialStorage、IApiHealthCheckService、IUsernameStorage（来自LYBT.Desktop.Foundation）
- **UI依赖**：MaterialDesignThemes 5.1.x（Material Design组件）、Prism.DryIoc 8.x（MVVM框架）
- **Shell集成**：Shell项目根据登录状态决定显示LoginWindow或MainWindow

## 🔧 核心功能

### 1. 双轨认证机制（Server端）

**设计理念**：超级管理员与普通用户完全隔离，提升安全性。

**第一轨：超级管理员认证（AdminSecrets表）**：
- 专用表`AdminSecrets`物理隔离（不在Users表中）
- 配置文件定义超级管理员用户名列表（`appsettings.json`）
- BCrypt密码哈希验证
- **特殊限制**：超级管理员无RefreshToken（每次登录都需要输入密码）

```csharp
// AuthService.cs - 超级管理员凭证验证
public bool IsSuperAdminCredentials(string username, string password)
{
    // 从配置读取超级管理员用户名列表
    var adminUsernames = _configuration
        .GetSection("Authentication:SuperAdmin:Usernames")
        .Get<List<string>>() ?? new List<string>();

    // 用户名不在超级管理员列表中
    if (!adminUsernames.Contains(username, StringComparer.OrdinalIgnoreCase))
    {
        return false;
    }

    // 从AdminSecrets表验证密码
    var adminSecret = _dbContext.AdminSecrets
        .FirstOrDefault(a => a.Username.ToLower() == username.ToLower());

    if (adminSecret == null) return false;

    // BCrypt密码验证
    return BCrypt.Net.BCrypt.Verify(password, adminSecret.PasswordHash);
}
```

**第二轨：普通用户认证（Users表）**：
- 标准用户表`Users`存储普通用户信息
- 支持Role角色（Admin、Doctor）
- 用户状态检查（Active、Inactive、Locked）
- **RefreshToken机制**：7天有效期，支持令牌刷新和撤销

```csharp
// AuthService.cs - 普通用户凭证验证
public async Task<UserModel?> VerifyCredentialsAsync(string username, string password)
{
    // 检查是否为超级管理员用户名(禁止普通用户使用)
    if (IsSuperAdminUsername(username))
    {
        _logger.LogWarning($"尝试使用保留用户名登录: {username}");
        return null;
    }

    // 从Users表查询用户
    var user = await _userRepository.GetByUsernameAsync(username);
    if (user == null) return null;

    // 检查用户状态
    if (user.Status != UserStatus.Active)
    {
        _logger.LogWarning($"用户状态异常: {username}, 状态: {user.Status}");
        return null;
    }

    // BCrypt密码验证
    if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
    {
        _logger.LogWarning($"密码验证失败: {username}");
        return null;
    }

    return user;
}
```

**统一登录入口（自动判断双轨认证）**：
```csharp
// AuthService.cs - 统一登录入口
public async Task<LoginResponse> LoginAsync(LoginRequest request)
{
    // 先尝试超级管理员认证(第一轨)
    if (IsSuperAdminCredentials(request.Username, request.Password))
    {
        // 超级管理员登录
        var adminToken = _jwtService.GenerateToken(
            userId: Guid.Empty,
            username: request.Username,
            role: UserRole.Admin,
            isSuperAdmin: true
        );

        return new LoginResponse
        {
            AccessToken = adminToken,
            RefreshToken = null,  // 超级管理员无RefreshToken
            UserId = Guid.Empty,
            Username = request.Username,
            Role = UserRole.Admin,
            IsSuperAdmin = true,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
    }

    // 再尝试普通用户认证(第二轨)
    var user = await VerifyCredentialsAsync(request.Username, request.Password);
    if (user == null)
    {
        throw new UnauthorizedException("用户名或密码错误");
    }

    // 生成JWT AccessToken + RefreshToken
    var accessToken = _jwtService.GenerateToken(
        userId: user.Id,
        username: user.Username,
        role: user.Role,
        isSuperAdmin: false
    );
    var refreshToken = Guid.NewGuid().ToString("N");

    // 创建会话记录
    var session = new AuthSessionModel
    {
        UserId = user.Id,
        RefreshToken = refreshToken,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        DeviceInfo = request.DeviceInfo,
        IpAddress = request.IpAddress
    };
    await _dbContext.AuthSessions.AddAsync(session);
    await _dbContext.SaveChangesAsync();

    return new LoginResponse
    {
        AccessToken = accessToken,
        RefreshToken = refreshToken,
        UserId = user.Id,
        Username = user.Username,
        Role = user.Role,
        IsSuperAdmin = false,
        ExpiresAt = DateTime.UtcNow.AddHours(2)
    };
}
```

### 2. JWT令牌机制（Server端）

**AccessToken（无状态令牌，2小时有效期）**：
- JWT结构：Header（签名算法）+ Payload（用户信息）+ Signature（签名）
- Claims包含：UserId、Username、Role、IsSuperAdmin、Expiry
- 签名密钥：从配置文件读取（`appsettings.json`）

```csharp
// JwtService.cs - 生成JWT AccessToken
public string GenerateToken(Guid userId, string username, UserRole role, bool isSuperAdmin)
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, username),
        new Claim(ClaimTypes.Role, role.ToString()),
        new Claim("IsSuperAdmin", isSuperAdmin.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**RefreshToken机制（有状态令牌，7天有效期）**：
- RefreshToken存储在`AuthSessions`表
- 支持令牌刷新（无需重新输入密码）
- 支持令牌撤销（登出或安全事件）
- **注意**：超级管理员无RefreshToken（提升安全性）

```csharp
// AuthService.cs - 刷新访问令牌
public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
{
    // 查询RefreshToken会话
    var session = await _dbContext.AuthSessions
        .Include(s => s.User)
        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    if (session == null)
    {
        throw new UnauthorizedException("无效的RefreshToken");
    }

    // 检查是否过期
    if (session.ExpiresAt < DateTime.UtcNow)
    {
        throw new UnauthorizedException("RefreshToken已过期");
    }

    // 检查是否被撤销
    if (session.RevokedAt.HasValue)
    {
        throw new UnauthorizedException("RefreshToken已被撤销");
    }

    // 生成新的AccessToken
    var newAccessToken = _jwtService.GenerateToken(
        userId: session.UserId,
        username: session.User.Username,
        role: session.User.Role,
        isSuperAdmin: false
    );

    // 更新会话最后使用时间
    session.LastUsedAt = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();

    return new LoginResponse
    {
        AccessToken = newAccessToken,
        RefreshToken = refreshToken,  // RefreshToken不变
        UserId = session.UserId,
        Username = session.User.Username,
        Role = session.User.Role,
        IsSuperAdmin = false,
        ExpiresAt = DateTime.UtcNow.AddHours(2)
    };
}

// AuthService.cs - 撤销RefreshToken（登出或安全事件）
public async Task RevokeTokenAsync(string refreshToken)
{
    var session = await _dbContext.AuthSessions
        .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    if (session == null) return;

    // 标记为已撤销
    session.RevokedAt = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();

    _logger.LogInformation($"RefreshToken已撤销: {refreshToken}");
}

// AuthService.cs - 用户登出（撤销所有会话）
public async Task LogoutAsync(Guid userId)
{
    var sessions = await _dbContext.AuthSessions
        .Where(s => s.UserId == userId && !s.RevokedAt.HasValue)
        .ToListAsync();

    foreach (var session in sessions)
    {
        session.RevokedAt = DateTime.UtcNow;
    }

    await _dbContext.SaveChangesAsync();
    _logger.LogInformation($"用户{userId}的所有会话已撤销");
}
```

### 3. 会话管理（Server端）

**AuthSessionModel表结构**：
| 字段 | 类型 | 说明 |
|-----|------|------|
| Id | Guid | 主键 |
| UserId | Guid | 用户ID（外键） |
| RefreshToken | string | RefreshToken令牌 |
| ExpiresAt | DateTime | 过期时间（7天） |
| RevokedAt | DateTime? | 撤销时间（NULL表示未撤销） |
| DeviceInfo | string | 设备信息（如"Windows 11 Desktop"） |
| IpAddress | string | IP地址 |
| LastUsedAt | DateTime | 最后使用时间 |
| CreatedAt | DateTime | 创建时间 |

**会话管理功能**：
- **多端登录控制**：一个用户可以有多个会话（多设备登录）
- **会话查询**：根据UserId查询所有活跃会话
- **会话撤销**：登出时撤销所有会话，或单独撤销某个会话
- **会话清理**：定期清理过期会话（后台任务）

```csharp
// AuthService.cs - 获取会话信息
public async Task<List<AuthSessionDto>> GetSessionInfoAsync(Guid userId)
{
    var sessions = await _dbContext.AuthSessions
        .Where(s => s.UserId == userId && !s.RevokedAt.HasValue)
        .OrderByDescending(s => s.LastUsedAt)
        .ToListAsync();

    return _mapper.Map<List<AuthSessionDto>>(sessions);
}
```

### 4. 登录流程（Client端）

**完整登录流程（8个步骤）**：
```csharp
// LoginViewModel.cs - 执行登录逻辑
private async Task ExecuteLoginAsync()
{
    try
    {
        IsBusy = true;
        ClearMessage();

        // Step 1: 验证输入
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            SetErrorMessage("用户名和密码不能为空");
            return;
        }

        // Step 2: 调用认证服务登录
        var result = await _authService.LoginAsync(new LoginRequest
        {
            Username = Username.Trim(),
            Password = Password
        });

        if (!result.IsSuccess)
        {
            SetErrorMessage(result.ErrorMessage ?? "登录失败，请检查用户名和密码");
            return;
        }

        // Step 3: 保存JWT令牌
        await _tokenStorage.SaveTokenAsync(result.Data.Token);

        // Step 4: 保存凭证（如果用户勾选）
        if (RememberMe)
        {
            _usernameStorage.SaveUsername(Username);
        }
        else
        {
            _usernameStorage.ClearUsername();
        }

        if (RememberPassword)
        {
            _credentialStorage.SavePassword(Username, Password);
        }
        else
        {
            _credentialStorage.DeletePassword(Username);
        }

        // Step 5: 发布用户登录事件（通知其他模块）
        EventAggregator.GetEvent<UserLoggedInEvent>().Publish(result.Data.User);

        // Step 6: 记录登录成功日志
        _logger.LogInformation("用户 {Username} 登录成功，角色: {Role}",
            Username, result.Data.User.Role);

        // Step 7: 基于用户角色导航到对应模块
        NavigateBasedOnRole(result.Data.User.Role);

        // Step 8: 关闭登录窗口（如果是独立窗口）
        if (Application.Current.MainWindow is LoginWindow loginWindow)
        {
            loginWindow.Close();
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "登录过程中发生异常");
        SetErrorMessage("登录失败: " + ex.Message);
    }
    finally
    {
        IsBusy = false;
    }
}

// LoginViewModel.cs - 基于用户角色导航
private void NavigateBasedOnRole(UserRole role)
{
    switch (role)
    {
        case UserRole.Admin:
            RegionManager.RequestNavigate("ContentRegion", "UsersView");
            break;
        case UserRole.Doctor:
            RegionManager.RequestNavigate("ContentRegion", "PatientsView");
            break;
        default:
            _logger.LogWarning("未知的用户角色: {Role}", role);
            SetErrorMessage("用户角色配置错误，请联系管理员");
            break;
    }
}
```

### 5. API健康检查（Client端）

**启动时自动健康检查**：
```csharp
// LoginViewModel.cs - API健康检查
private async Task CheckApiHealthAsync()
{
    try
    {
        _logger.LogInformation("开始检查API健康状态...");

        var result = await _apiHealthCheck.CheckHealthAsync();

        if (result.IsSuccess)
        {
            var healthStatus = result.Data;
            ApiStatus = healthStatus.Status;

            switch (healthStatus.Status)
            {
                case ApiHealthStatus.Healthy:
                    ApiStatusMessage = "✅ API连接正常";
                    _logger.LogInformation("API健康检查成功");
                    break;
                case ApiHealthStatus.Degraded:
                    ApiStatusMessage = "⚠️ API连接不稳定";
                    _logger.LogWarning("API处于降级状态");
                    break;
                case ApiHealthStatus.Unhealthy:
                    ApiStatusMessage = "❌ API连接失败";
                    _logger.LogError("API健康检查失败");
                    break;
            }
        }
        else
        {
            ApiStatus = ApiHealthStatus.Unhealthy;
            ApiStatusMessage = $"❌ 无法连接到服务器: {result.ErrorMessage}";
            _logger.LogError("API健康检查失败: {Error}", result.ErrorMessage);
        }
    }
    catch (Exception ex)
    {
        ApiStatus = ApiHealthStatus.Unhealthy;
        ApiStatusMessage = $"❌ 健康检查异常: {ex.Message}";
        _logger.LogError(ex, "API健康检查过程中发生异常");
    }
}

// 启动时安全的健康检查（不阻塞UI）
private async Task CheckApiHealthAsyncSafe()
{
    try
    {
        await CheckApiHealthAsync();
    }
    catch
    {
        // 吞掉异常，避免阻塞UI初始化
    }
}
```

### 6. 记住密码功能（Client端）

**DPAPI加密存储密码**：
```csharp
// LoginViewModel.cs - 加载保存的凭证
private async Task LoadSavedCredentialsAsync()
{
    try
    {
        // Step 1: 加载用户名
        var savedUsername = _usernameStorage.GetSavedUsername();
        if (!string.IsNullOrWhiteSpace(savedUsername))
        {
            Username = savedUsername;
            RememberMe = true;
            _logger.LogInformation("加载保存的用户名: {Username}", savedUsername);
        }

        // Step 2: 加载密码
        if (!string.IsNullOrWhiteSpace(Username))
        {
            var savedPassword = _credentialStorage.GetPassword(Username);
            if (!string.IsNullOrWhiteSpace(savedPassword))
            {
                Password = savedPassword;
                RememberPassword = true;
                HasSavedPassword = true;
                _logger.LogInformation("加载保存的密码（用户: {Username}）", Username);
            }
            else
            {
                HasSavedPassword = false;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载保存的凭证失败");
        HasSavedPassword = false;
    }
}

// RememberPassword属性变化时保存/删除密码
public bool RememberPassword
{
    get => _rememberPassword;
    set
    {
        if (SetProperty(ref _rememberPassword, value))
        {
            if (value && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
                // 保存密码（DPAPI加密）
                _credentialStorage.SavePassword(Username, Password);
                _logger.LogInformation("密码已保存（用户: {Username}）", Username);
            }
            else if (!value && !string.IsNullOrWhiteSpace(Username))
            {
                // 删除密码
                _credentialStorage.DeletePassword(Username);
                _logger.LogInformation("密码已删除（用户: {Username}）", Username);
            }
        }
    }
}
```

## 📋 业务规则

### 1. 双轨认证规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **AUTH-R01** | 超级管理员用户名必须在配置文件白名单中 | AuthService.IsSuperAdminCredentials |
| **AUTH-R02** | 超级管理员禁止创建RefreshToken（每次登录都需要输入密码） | AuthService.LoginAsync |
| **AUTH-R03** | 普通用户禁止使用超级管理员保留用户名 | AuthService.VerifyCredentialsAsync |
| **AUTH-R04** | 普通用户状态必须为Active才能登录 | AuthService.VerifyCredentialsAsync |

### 2. 密码安全规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **AUTH-R05** | 密码必须使用BCrypt哈希（工作因子12） | AuthService（Server端） |
| **AUTH-R06** | 密码存储必须使用DPAPI加密（Client端） | ISecureCredentialStorage（Client端） |
| **AUTH-R07** | 登录失败不暴露敏感信息（统一返回"用户名或密码错误"） | AuthService.LoginAsync |
| **AUTH-R08** | 密码字段不记录到日志 | 所有日志记录点 |

### 3. 令牌管理规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **AUTH-R09** | AccessToken有效期2小时（不可配置） | JwtService.GenerateToken |
| **AUTH-R10** | RefreshToken有效期7天（不可配置） | AuthService.LoginAsync |
| **AUTH-R11** | RefreshToken必须存储在AuthSessions表 | AuthService.LoginAsync |
| **AUTH-R12** | 令牌撤销后不可恢复（软删除标记RevokedAt） | AuthService.RevokeTokenAsync |
| **AUTH-R13** | 用户登出撤销所有活跃会话 | AuthService.LogoutAsync |

### 4. 会话管理规则

| 规则编号 | 规则描述 | 实现位置 |
|---------|---------|---------|
| **AUTH-R14** | 每个会话必须记录DeviceInfo和IpAddress | AuthService.LoginAsync |
| **AUTH-R15** | 会话过期时间为RefreshToken创建时间+7天 | AuthService.LoginAsync |
| **AUTH-R16** | 用户可以有多个活跃会话（支持多端登录） | AuthSessions表设计 |
| **AUTH-R17** | 会话使用时更新LastUsedAt时间戳 | AuthService.RefreshTokenAsync |

## 🔌 API 端点

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `AuthController` 对外暴露。

- **API路由前缀**: `/api/v1/auth`

**主要端点**：

| 端点 | 方法 | 功能描述 | 请求体 | 响应体 |
|-----|------|---------|--------|--------|
| `/api/v1/auth/login` | POST | 用户登录（双轨认证） | LoginRequest | LoginResponse |
| `/api/v1/auth/logout` | POST | 用户登出（撤销所有会话） | - | - |
| `/api/v1/auth/refresh` | POST | 刷新AccessToken | RefreshTokenRequest | LoginResponse |
| `/api/v1/auth/validate` | GET | 验证JWT令牌有效性 | - | ValidationResponse |
| `/api/v1/auth/revoke` | POST | 撤销RefreshToken | RevokeTokenRequest | - |
| `/api/v1/auth/sessions` | GET | 获取当前用户所有会话 | - | List<AuthSessionDto> |
| `/api/v1/auth/change-password` | POST | 修改密码 | ChangePasswordRequest | - |
| `/api/v1/auth/change-sysadmin-password` | POST | 修改超管密码 | ChangeSysAdminPasswordRequest | - |

**DTO定义示例**：

```csharp
// LoginRequest DTO
public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, ErrorMessage = "用户名长度不能超过50字符")]
    public string Username { get; set; }

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100字符之间")]
    public string Password { get; set; }

    public string? DeviceInfo { get; set; }  // 设备信息（可选）
    public string? IpAddress { get; set; }   // IP地址（可选，由Server端自动获取）
}

// LoginResponse DTO
public class LoginResponse
{
    public string AccessToken { get; set; }     // JWT AccessToken（2小时有效）
    public string? RefreshToken { get; set; }   // RefreshToken（7天有效，超管无）
    public Guid UserId { get; set; }            // 用户ID
    public string Username { get; set; }        // 用户名
    public UserRole Role { get; set; }          // 用户角色（Admin/Doctor）
    public bool IsSuperAdmin { get; set; }      // 是否超级管理员
    public DateTime ExpiresAt { get; set; }     // AccessToken过期时间
}

// AuthSessionDto DTO
public class AuthSessionDto
{
    public Guid Id { get; set; }
    public string DeviceInfo { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }  // 是否活跃（未过期且未撤销）
}
```

**完整API定义**请参考 `IAuthService` 接口和 `AuthController` 的实现。

## 🎯 设计原则

### Server端设计原则（6条）

#### 1. 双轨认证物理隔离
- **原则**：超级管理员与普通用户完全隔离，不共享数据表
- **实现**：AdminSecrets表 + Users表分离设计
- **价值**：提升系统安全性，防止权限提升攻击

#### 2. JWT无状态 + RefreshToken有状态
- **原则**：AccessToken无状态验证，RefreshToken有状态管理
- **实现**：JWT签名验证 + AuthSessions表存储RefreshToken
- **价值**：性能与安全性平衡（AccessToken快速验证，RefreshToken可撤销）

#### 3. 会话追踪与多端登录支持
- **原则**：记录每个登录会话的设备信息和IP地址
- **实现**：AuthSessions表记录DeviceInfo、IpAddress、LastUsedAt
- **价值**：支持多端登录、会话管理、异常登录检测

#### 4. 密码安全优先
- **原则**：所有密码必须BCrypt哈希（工作因子12），禁止明文存储
- **实现**：BCrypt.Net库统一处理密码哈希和验证
- **价值**：防止密码泄露（即使数据库被攻破）

#### 5. 日志安全与审计
- **原则**：记录所有认证事件，但不记录敏感信息（密码）
- **实现**：ILogger记录登录成功/失败、令牌刷新、会话撤销
- **价值**：安全审计、异常登录追踪

#### 6. 异步优先与性能优化
- **原则**：所有I/O操作异步化（数据库查询、JWT生成）
- **实现**：async/await模式、Task异步方法
- **价值**：提升并发性能，避免阻塞线程池

### Client端设计原则（6条）

#### 1. MVVM架构严格遵循
- **原则**：视图与业务逻辑完全分离，所有UI状态和操作通过ViewModel暴露
- **实现**：LoginView.xaml仅包含XAML标记和数据绑定，LoginViewModel包含所有登录逻辑
- **价值**：可测试性、可维护性、UI与逻辑解耦

#### 2. 安全性优先
- **密码存储安全**：使用Windows DPAPI加密存储密码（ISecureCredentialStorage）
- **JWT令牌管理**：令牌存储在内存，应用关闭时自动清除
- **日志记录**：记录登录成功/失败事件（不记录密码）

#### 3. 用户体验优化
- **启动时体验**：自动执行API健康检查，显示连接状态（✅ 正常 / ⚠️ 不稳定 / ❌ 失败）
- **自动填充**：自动加载保存的用户名和密码（如果用户曾勾选记住）
- **友好提示**：友好的错误提示（"用户名和密码不能为空" 而非 "Validation Failed"）
- **角色导航**：登录成功后自动导航到对应模块（Admin→用户管理，Doctor→患者管理）

#### 4. 事件驱动通信
- **原则**：使用Prism EventAggregator模式解耦模块间通信
- **实现**：登录成功后发布`UserLoggedInEvent`，其他模块订阅事件更新状态
- **价值**：模块解耦、松散耦合、可扩展性

#### 5. 异步优先与防阻塞
- **所有I/O操作异步化**：ExecuteLoginAsync、CheckApiHealthAsync、LoadSavedCredentialsAsync
- **防阻塞策略**：构造函数中使用`_ = CheckApiHealthAsyncSafe()`异步触发（不阻塞构造完成）
- **IsBusy状态**：控制登录按钮可用性（防止重复点击）

#### 6. 依赖注入与可测试性
- **构造函数注入**：所有依赖服务通过构造函数注入（IAuthenticationService, ITokenStorageService等）
- **避免ServiceLocator**：不使用ServiceLocator反模式
- **单元测试友好**：便于Mock依赖服务进行单元测试

## 🛠 技术栈

### Server端技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| **.NET 8** | 8.0 | 基础框架 |
| **Entity Framework Core** | 8.0 | 数据持久化（通过Repository模式） |
| **BCrypt.Net** | 0.1.0 | 密码哈希和验证 |
| **System.IdentityModel.Tokens.Jwt** | 7.x | JWT令牌生成和验证 |
| **FluentValidation** | 11.x | DTO数据验证框架 |
| **AutoMapper** | 13.x | Entity与DTO之间的自动映射 |
| **Microsoft.Extensions.DependencyInjection** | 8.0.x | 依赖注入容器 |

### Client端技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| **WPF** | .NET 8 | 桌面UI框架 |
| **Prism.DryIoc** | 8.x | MVVM框架和模块化 |
| **MaterialDesignThemes** | 5.1.x | Material Design UI组件 |
| **System.Security.Cryptography** | .NET 8 | DPAPI加密凭证存储 |
| **Microsoft.Extensions.Logging** | 8.0.x | 日志记录 |

## 🚀 快速开始

此模块是类库，作为Server端服务（LYBT.WebAPI）和Client端应用（LYBT.Desktop.Shell）的一部分被引用和托管。无法独立运行。

### Server端集成

```csharp
// Startup.cs (LYBT.WebAPI)
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册Auth模块(自动注册仓储+服务+验证器)
        services.AddAuthModule();

        // 配置JWT认证
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"])
                    )
                };
            });

        // 配置授权策略
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("DoctorOnly", policy => policy.RequireRole("Doctor"));
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        // 启用认证和授权中间件
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
```

### Client端集成

```csharp
// App.xaml.cs (LYBT.Desktop.Shell)
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Auth模块(优先级最高，应用启动时加载)
    moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);
}

protected override Window CreateShell()
{
    // 如果未登录，显示登录窗口
    var authService = Container.Resolve<IAuthenticationService>();
    if (!authService.IsLoggedInAsync().Result)
    {
        return Container.Resolve<LoginWindow>();
    }

    // 已登录，显示主窗口
    return Container.Resolve<MainWindow>();
}
```

## 📚 相关文档

### 模块文档
- **[双轨认证机制设计](dual-track-auth.md)** *(待创建)* - 详细的双轨认证架构设计文档
- **[JWT令牌管理指南](jwt-management.md)** *(待创建)* - JWT和RefreshToken机制详解
- **[会话管理详解](session-management.md)** *(待创建)* - AuthSessions表设计和会话管理策略

### 开发指南
- **[Server端开发指南](../../../development/server/auth-development.md)** *(待创建)* - Server端AuthService开发和测试指南
- **[Client端开发指南](../../../development/client/auth-development.md)** *(待创建)* - Client端LoginViewModel开发和测试指南

### API文档
- **[Auth API完整文档](../../../api/auth-api.md)** *(待创建)* - 完整的API端点定义、请求/响应示例、错误码说明

### 架构设计
- **[Server端架构设计](../../../architecture/server/auth-design.md)** *(待创建)* - Server端架构决策和设计模式
- **[Client端架构设计](../../../architecture/client/auth-design.md)** *(待创建)* - Client端MVVM架构和设计原则

---

**最后更新**：2025-10-29
**维护负责**：Server端开发组 + Client端开发组
