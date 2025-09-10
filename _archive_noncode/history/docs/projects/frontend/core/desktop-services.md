# Desktop.Services Project (桌面服务层项目)

## 📋 项目概述

### 项目定位
**Desktop.Services** 是凌隐宝堂中医诊所系统的**前端业务服务层项目**，提供前端应用与后端API的交互服务、业务逻辑封装和数据转换处理。作为前端业务层的核心，连接底层基础设施服务和上层UI组件，实现前后端数据交互的标准化和业务逻辑的统一管理。

### 核心价值
- 🔗 **API集成**: 标准化的后端API调用和响应处理
- 🎯 **业务封装**: 前端业务逻辑的集中管理和封装
- 🔄 **数据转换**: DTO与ViewModel之间的数据映射转换
- 📝 **缓存管理**: 前端数据缓存策略和生命周期管理
- 🔐 **权限控制**: 前端权限验证和用户操作控制
- 📊 **状态管理**: 全局应用状态和业务状态管理
- 🎨 **UI服务**: 为UI层提供专业化的业务服务接口

### 技术定位 (v1.0)
```
UI层 (Modules, Views, ViewModels)
    ↑ 调用
LYBT.Desktop.Services (业务服务层) ← 本项目
    ↑ 依赖
Desktop.Infrastructure (基础设施) + Shared.Models (共享模型)
```

## 🏗️ 技术架构

### 核心技术栈
```csharp
// 基础技术栈
- .NET 8.0-windows
- AutoMapper 12.0.1 (对象映射)
- Prism.Core 9.0.537 (MVVM支持)
- Microsoft.Extensions.DependencyInjection (依赖注入)
- Microsoft.Extensions.Logging (日志系统)
- Refit 7.0.0 (HTTP API客户端)

// 项目引用
<ProjectReference Include="..\Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
<ProjectReference Include="..\Core\LYBT.Desktop.Core.csproj" />
<ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
<ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
```

### 项目结构架构
```
src/Client/Desktop/Services/
├── ApiServices/                # API服务接口
│   ├── IAuthApi.cs
│   ├── IUserApi.cs
│   ├── IPatientApi.cs
│   ├── IMedicalCaseApi.cs
│   ├── IConsultationApi.cs
│   ├── IPrescriptionApi.cs
│   ├── IHerbApi.cs
│   └── IFormulaApi.cs
├── BusinessServices/           # 业务服务实现
│   ├── AuthService.cs
│   ├── UserService.cs
│   ├── PatientService.cs
│   ├── MedicalCaseService.cs
│   ├── ConsultationService.cs
│   ├── PrescriptionService.cs
│   ├── HerbService.cs
│   └── FormulaService.cs
├── CacheServices/              # 缓存服务
│   ├── ICacheService.cs
│   ├── MemoryCacheService.cs
│   └── CacheKeys.cs
├── ValidationServices/         # 验证服务
│   ├── IValidationService.cs
│   └── ValidationService.cs
├── MappingProfiles/           # AutoMapper配置
│   ├── AuthMappingProfile.cs
│   ├── UserMappingProfile.cs
│   ├── PatientMappingProfile.cs
│   └── CommonMappingProfile.cs
├── Models/                    # 前端模型
│   ├── ViewModels/           # 视图模型
│   ├── ServiceModels/        # 服务模型
│   └── RequestModels/        # 请求模型
├── Extensions/               # 扩展方法
│   ├── ServiceResultExtensions.cs
│   ├── ApiResponseExtensions.cs
│   └── MappingExtensions.cs
└── Constants/               # 常量定义
    ├── CacheConstants.cs
    ├── ValidationConstants.cs
    └── ServiceConstants.cs
```

## 🔌 API服务接口

### 1. 认证API服务 (IAuthApi)
```csharp
// 认证API接口定义
[Headers("Content-Type: application/json")]
public interface IAuthApi
{
    [Post("/api/v1/auth/login")]
    Task<ApiResponse<LoginResponseDto>> LoginAsync([Body] LoginRequestDto request);
    
    [Post("/api/v1/auth/logout")]
    Task<ApiResponse> LogoutAsync();
    
    [Post("/api/v1/auth/refresh")]
    Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync([Body] RefreshTokenRequestDto request);
    
    [Post("/api/v1/auth/change-password")]
    Task<ApiResponse> ChangePasswordAsync([Body] ChangePasswordRequestDto request);
    
    [Get("/api/v1/auth/profile")]
    Task<ApiResponse<UserProfileDto>> GetCurrentUserProfileAsync();
    
    [Put("/api/v1/auth/profile")]
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync([Body] UpdateProfileRequestDto request);
}
```

### 2. 用户API服务 (IUserApi)
```csharp
// 用户API接口定义
[Headers("Content-Type: application/json")]
public interface IUserApi
{
    [Get("/api/v1/users")]
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
        [Query] int page = 1, 
        [Query] int pageSize = 50);
    
    [Get("/api/v1/users/search")]
    Task<ApiResponse<PagedResult<UserDto>>> SearchUsersAsync([Query] UserSearchRequestDto request);
    
    [Get("/api/v1/users/{id}")]
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
    
    [Post("/api/v1/users")]
    Task<ApiResponse<UserDto>> CreateUserAsync([Body] UserCreateRequestDto request);
    
    [Put("/api/v1/users/{id}")]
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, [Body] UserUpdateRequestDto request);
    
    [Delete("/api/v1/users/{id}")]
    Task<ApiResponse> DeleteUserAsync(Guid id);
    
    [Post("/api/v1/users/batch-update-status")]
    Task<ApiResponse> BatchUpdateUserStatusAsync([Body] BatchUpdateStatusRequestDto request);
    
    [Get("/api/v1/users/statistics")]
    Task<ApiResponse<UserStatisticsDto>> GetUserStatisticsAsync();
}
```

### 3. 患者API服务 (IPatientApi)
```csharp
// 患者API接口定义
[Headers("Content-Type: application/json")]
public interface IPatientApi
{
    [Get("/api/v1/patients")]
    Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
        [Query] int page = 1, 
        [Query] int pageSize = 50);
    
    [Get("/api/v1/patients/search")]
    Task<ApiResponse<PagedResult<PatientDto>>> SearchPatientsAsync([Query] PatientSearchRequestDto request);
    
    [Get("/api/v1/patients/{id}")]
    Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);
    
    [Post("/api/v1/patients")]
    Task<ApiResponse<PatientDto>> CreatePatientAsync([Body] PatientCreateRequestDto request);
    
    [Put("/api/v1/patients/{id}")]
    Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, [Body] PatientUpdateRequestDto request);
    
    [Delete("/api/v1/patients/{id}")]
    Task<ApiResponse> DeletePatientAsync(Guid id);
    
    [Get("/api/v1/patients/{id}/medical-history")]
    Task<ApiResponse<List<MedicalHistoryDto>>> GetPatientMedicalHistoryAsync(Guid id);
    
    [Post("/api/v1/patients/import")]
    Task<ApiResponse<ImportResultDto>> ImportPatientsAsync([Body] ImportRequestDto request);
    
    [Get("/api/v1/patients/export")]
    Task<ApiResponse<ExportResultDto>> ExportPatientsAsync([Query] PatientExportRequestDto request);
}
```

## 🏢 业务服务实现

### 1. 认证业务服务 (AuthService)
```csharp
// 认证业务服务接口
public interface IAuthService
{
    Task<ServiceResult<LoginResult>> LoginAsync(LoginRequest request);
    Task<ServiceResult> LogoutAsync();
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ServiceResult<UserProfile>> GetCurrentUserProfileAsync();
    Task<ServiceResult<UserProfile>> UpdateProfileAsync(UpdateProfileRequest request);
    bool IsAuthenticated { get; }
    UserInfo CurrentUser { get; }
    event EventHandler<UserInfo> UserChanged;
}

// 认证业务服务实现
public class AuthService : IAuthService
{
    private readonly IAuthApi _authApi;
    private readonly IAuthenticationService _authenticationService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    
    public bool IsAuthenticated => _authenticationService.IsAuthenticated;
    public UserInfo CurrentUser => _authenticationService.CurrentUser;
    
    public event EventHandler<UserInfo> UserChanged
    {
        add => _authenticationService.UserChanged += value;
        remove => _authenticationService.UserChanged -= value;
    }
    
    public AuthService(
        IAuthApi authApi,
        IAuthenticationService authenticationService,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<AuthService> logger)
    {
        _authApi = authApi;
        _authenticationService = authenticationService;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<ServiceResult<LoginResult>> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("开始登录请求: {Username}", request.Username);
            
            // 验证请求参数
            var validation = ValidateLoginRequest(request);
            if (!validation.IsValid)
                return ServiceResult<LoginResult>.Failure(validation.ErrorMessage);
            
            // 转换为API请求模型
            var apiRequest = _mapper.Map<LoginRequestDto>(request);
            
            // 调用认证服务
            var loginResult = await _authenticationService.LoginAsync(apiRequest);
            
            if (loginResult)
            {
                // 获取用户资料
                var profileResult = await GetCurrentUserProfileAsync();
                var userProfile = profileResult.Success ? profileResult.Data : null;
                
                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("user_*");
                
                var result = new LoginResult
                {
                    User = CurrentUser,
                    Profile = userProfile,
                    LoginTime = DateTime.Now
                };
                
                _logger.LogInformation("用户登录成功: {Username}", request.Username);
                return ServiceResult<LoginResult>.Success(result);
            }
            else
            {
                _logger.LogWarning("用户登录失败: {Username}", request.Username);
                return ServiceResult<LoginResult>.Failure("用户名或密码错误");
            }
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "登录API调用异常: {Username}", request.Username);
            return ServiceResult<LoginResult>.Failure($"登录失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程发生异常: {Username}", request.Username);
            return ServiceResult<LoginResult>.Failure("登录过程中发生错误，请稍后重试");
        }
    }
    
    public async Task<ServiceResult> LogoutAsync()
    {
        try
        {
            _logger.LogInformation("开始注销当前用户");
            
            await _authenticationService.LogoutAsync();
            
            // 清除所有缓存
            await _cacheService.ClearAsync();
            
            _logger.LogInformation("用户注销成功");
            return ServiceResult.Success("注销成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注销过程发生异常");
            return ServiceResult.Failure("注销失败，请稍后重试");
        }
    }
    
    public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            _logger.LogInformation("开始修改密码");
            
            // 验证请求参数
            var validation = ValidateChangePasswordRequest(request);
            if (!validation.IsValid)
                return ServiceResult.Failure(validation.ErrorMessage);
            
            var apiRequest = _mapper.Map<ChangePasswordRequestDto>(request);
            var response = await _authApi.ChangePasswordAsync(apiRequest);
            
            if (response.Success)
            {
                _logger.LogInformation("密码修改成功");
                return ServiceResult.Success("密码修改成功");
            }
            else
            {
                _logger.LogWarning("密码修改失败: {Error}", response.ErrorMessage);
                return ServiceResult.Failure(response.ErrorMessage);
            }
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "修改密码API调用异常");
            return ServiceResult.Failure($"修改密码失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修改密码过程发生异常");
            return ServiceResult.Failure("修改密码过程中发生错误，请稍后重试");
        }
    }
    
    public async Task<ServiceResult<UserProfile>> GetCurrentUserProfileAsync()
    {
        try
        {
            // 检查缓存
            var cacheKey = $"user_profile_{CurrentUser?.Id}";
            var cachedProfile = await _cacheService.GetAsync<UserProfile>(cacheKey);
            if (cachedProfile != null)
            {
                return ServiceResult<UserProfile>.Success(cachedProfile);
            }
            
            var response = await _authApi.GetCurrentUserProfileAsync();
            
            if (response.Success && response.Data != null)
            {
                var profile = _mapper.Map<UserProfile>(response.Data);
                
                // 缓存用户资料，有效期1小时
                await _cacheService.SetAsync(cacheKey, profile, TimeSpan.FromHours(1));
                
                return ServiceResult<UserProfile>.Success(profile);
            }
            else
            {
                return ServiceResult<UserProfile>.Failure(response.ErrorMessage);
            }
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "获取用户资料API调用异常");
            return ServiceResult<UserProfile>.Failure($"获取用户资料失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户资料过程发生异常");
            return ServiceResult<UserProfile>.Failure("获取用户资料失败，请稍后重试");
        }
    }
    
    private ValidationResult ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return ValidationResult.Failure("用户名不能为空");
            
        if (string.IsNullOrWhiteSpace(request.Password))
            return ValidationResult.Failure("密码不能为空");
            
        if (request.Username.Length < 3 || request.Username.Length > 50)
            return ValidationResult.Failure("用户名长度必须在3-50个字符之间");
            
        if (request.Password.Length < 6)
            return ValidationResult.Failure("密码长度不能少于6个字符");
            
        return ValidationResult.Success();
    }
    
    private ValidationResult ValidateChangePasswordRequest(ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OldPassword))
            return ValidationResult.Failure("原密码不能为空");
            
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return ValidationResult.Failure("新密码不能为空");
            
        if (request.NewPassword.Length < 6)
            return ValidationResult.Failure("新密码长度不能少于6个字符");
            
        if (request.OldPassword == request.NewPassword)
            return ValidationResult.Failure("新密码不能与原密码相同");
            
        return ValidationResult.Success();
    }
}
```

### 2. 用户业务服务 (UserService)
```csharp
// 用户业务服务接口
public interface IUserService
{
    Task<ServiceResult<PagedResult<UserViewModel>>> GetUsersAsync(int page = 1, int pageSize = 50);
    Task<ServiceResult<PagedResult<UserViewModel>>> SearchUsersAsync(UserSearchRequest request);
    Task<ServiceResult<UserViewModel>> GetUserByIdAsync(Guid id);
    Task<ServiceResult<UserViewModel>> CreateUserAsync(UserCreateRequest request);
    Task<ServiceResult<UserViewModel>> UpdateUserAsync(Guid id, UserUpdateRequest request);
    Task<ServiceResult> DeleteUserAsync(Guid id);
    Task<ServiceResult> BatchUpdateStatusAsync(BatchUpdateStatusRequest request);
    Task<ServiceResult<UserStatistics>> GetUserStatisticsAsync();
}

// 用户业务服务实现
public class UserService : IUserService
{
    private readonly IUserApi _userApi;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
    
    public UserService(
        IUserApi userApi,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userApi = userApi;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<ServiceResult<PagedResult<UserViewModel>>> GetUsersAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            _logger.LogDebug("获取用户列表: 第{Page}页，每页{PageSize}条", page, pageSize);
            
            // 检查缓存
            var cacheKey = $"users_page_{page}_{pageSize}";
            var cachedResult = await _cacheService.GetAsync<PagedResult<UserViewModel>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("从缓存返回用户列表");
                return ServiceResult<PagedResult<UserViewModel>>.Success(cachedResult);
            }
            
            var response = await _userApi.GetUsersAsync(page, pageSize);
            
            if (response.Success && response.Data != null)
            {
                var result = new PagedResult<UserViewModel>
                {
                    Items = _mapper.Map<List<UserViewModel>>(response.Data.Items),
                    TotalCount = response.Data.TotalCount,
                    Page = response.Data.Page,
                    PageSize = response.Data.PageSize,
                    TotalPages = response.Data.TotalPages
                };
                
                // 缓存结果
                await _cacheService.SetAsync(cacheKey, result, _cacheExpiry);
                
                _logger.LogDebug("获取用户列表成功，共{Total}条记录", result.TotalCount);
                return ServiceResult<PagedResult<UserViewModel>>.Success(result);
            }
            else
            {
                _logger.LogWarning("获取用户列表失败: {Error}", response.ErrorMessage);
                return ServiceResult<PagedResult<UserViewModel>>.Failure(response.ErrorMessage);
            }
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "获取用户列表API调用异常");
            return ServiceResult<PagedResult<UserViewModel>>.Failure($"获取用户列表失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表过程发生异常");
            return ServiceResult<PagedResult<UserViewModel>>.Failure("获取用户列表失败，请稍后重试");
        }
    }
    
    public async Task<ServiceResult<UserViewModel>> CreateUserAsync(UserCreateRequest request)
    {
        try
        {
            _logger.LogInformation("开始创建用户: {Username}", request.Username);
            
            // 验证请求参数
            var validation = ValidateUserCreateRequest(request);
            if (!validation.IsValid)
                return ServiceResult<UserViewModel>.Failure(validation.ErrorMessage);
            
            var apiRequest = _mapper.Map<UserCreateRequestDto>(request);
            var response = await _userApi.CreateUserAsync(apiRequest);
            
            if (response.Success && response.Data != null)
            {
                var result = _mapper.Map<UserViewModel>(response.Data);
                
                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("users_*");
                
                _logger.LogInformation("用户创建成功: {Username}", request.Username);
                return ServiceResult<UserViewModel>.Success(result);
            }
            else
            {
                _logger.LogWarning("用户创建失败: {Username}, 错误: {Error}", request.Username, response.ErrorMessage);
                return ServiceResult<UserViewModel>.Failure(response.ErrorMessage);
            }
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "创建用户API调用异常: {Username}", request.Username);
            return ServiceResult<UserViewModel>.Failure($"创建用户失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户过程发生异常: {Username}", request.Username);
            return ServiceResult<UserViewModel>.Failure("创建用户过程中发生错误，请稍后重试");
        }
    }
    
    public async Task<ServiceResult> DeleteUserAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("开始删除用户: {UserId}", id);
            
            var response = await _userApi.DeleteUserAsync(id);
            
            if (response.Success)
            {
                // 清除相关缓存
                await _cacheService.RemoveByPatternAsync("users_*");
                await _cacheService.RemoveAsync($"user_{id}");
                
                _logger.LogInformation("用户删除成功: {UserId}", id);
                return ServiceResult.Success("用户删除成功");
            }
            else
            {
                _logger.LogWarning("用户删除失败: {UserId}, 错误: {Error}", id, response.ErrorMessage);
                return ServiceResult.Failure(response.ErrorMessage);
            }
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "删除用户API调用异常: {UserId}", id);
            return ServiceResult.Failure($"删除用户失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户过程发生异常: {UserId}", id);
            return ServiceResult.Failure("删除用户过程中发生错误，请稍后重试");
        }
    }
    
    private ValidationResult ValidateUserCreateRequest(UserCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return ValidationResult.Failure("用户名不能为空");
            
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return ValidationResult.Failure("显示名称不能为空");
            
        if (request.Username.Length < 3 || request.Username.Length > 50)
            return ValidationResult.Failure("用户名长度必须在3-50个字符之间");
            
        if (!string.IsNullOrEmpty(request.Email) && !IsValidEmail(request.Email))
            return ValidationResult.Failure("邮箱格式不正确");
            
        return ValidationResult.Success();
    }
    
    private bool IsValidEmail(string email)
    {
        try
        {
            var mailAddress = new System.Net.Mail.MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

## 🗄️ 缓存服务架构

### 内存缓存服务 (MemoryCacheService)
```csharp
// 缓存服务接口
public interface ICacheService
{
    Task<T> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task ClearAsync();
    bool Exists(string key);
}

// 内存缓存服务实现
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, bool> _cacheKeys;
    private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(30);
    
    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _cacheKeys = new ConcurrentDictionary<string, bool>();
    }
    
    public Task<T> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return Task.FromResult<T>(null);
            
            var value = _memoryCache.Get<T>(key);
            
            if (value != null)
                _logger.LogDebug("缓存命中: {Key}", key);
            else
                _logger.LogDebug("缓存未命中: {Key}", key);
            
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存数据失败: {Key}", key);
            return Task.FromResult<T>(null);
        }
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            if (string.IsNullOrEmpty(key) || value == null)
                return Task.CompletedTask;
            
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry,
                SlidingExpiration = TimeSpan.FromMinutes(5), // 滑动过期时间
                Priority = CacheItemPriority.Normal
            };
            
            // 注册过期回调
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _cacheKeys.TryRemove(key.ToString(), out _);
                _logger.LogDebug("缓存项已过期: {Key}, 原因: {Reason}", key, reason);
            });
            
            _memoryCache.Set(key, value, options);
            _cacheKeys.TryAdd(key, true);
            
            _logger.LogDebug("缓存数据成功: {Key}, 过期时间: {Expiry}", key, expiry ?? _defaultExpiry);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置缓存数据失败: {Key}", key);
            return Task.CompletedTask;
        }
    }
    
    public Task RemoveAsync(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;
            
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            
            _logger.LogDebug("移除缓存数据: {Key}", key);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除缓存数据失败: {Key}", key);
            return Task.CompletedTask;
        }
    }
    
    public Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            if (string.IsNullOrEmpty(pattern))
                return Task.CompletedTask;
            
            var keysToRemove = _cacheKeys.Keys
                .Where(key => key.Contains(pattern.Replace("*", "")))
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }
            
            _logger.LogDebug("按模式移除缓存数据: {Pattern}, 移除数量: {Count}", pattern, keysToRemove.Count);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按模式移除缓存数据失败: {Pattern}", pattern);
            return Task.CompletedTask;
        }
    }
    
    public Task ClearAsync()
    {
        try
        {
            var keysToRemove = _cacheKeys.Keys.ToList();
            
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
            }
            
            _cacheKeys.Clear();
            
            _logger.LogInformation("清除所有缓存数据，清除数量: {Count}", keysToRemove.Count);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除缓存数据失败");
            return Task.CompletedTask;
        }
    }
    
    public bool Exists(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
                return false;
            
            return _memoryCache.TryGetValue(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查缓存是否存在失败: {Key}", key);
            return false;
        }
    }
}
```

## 🎯 AutoMapper映射配置

### 用户映射配置
```csharp
// 用户映射配置
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // DTO to ViewModel mappings
        CreateMap<UserDto, UserViewModel>()
            .ForMember(dest => dest.DisplayRole, opt => opt.MapFrom(src => GetDisplayRole(src.Role)))
            .ForMember(dest => dest.StatusText, opt => opt.MapFrom(src => GetStatusText(src.Status)))
            .ForMember(dest => dest.CreateTimeText, opt => opt.MapFrom(src => src.CreateTime.ToString("yyyy-MM-dd HH:mm")))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == UserStatus.Active));
        
        // Request to DTO mappings
        CreateMap<UserCreateRequest, UserCreateRequestDto>();
        
        CreateMap<UserUpdateRequest, UserUpdateRequestDto>();
        
        CreateMap<UserSearchRequest, UserSearchRequestDto>()
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page <= 0 ? 1 : src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize <= 0 ? 50 : Math.Min(src.PageSize, 200)));
        
        // Statistics mappings
        CreateMap<UserStatisticsDto, UserStatistics>()
            .ForMember(dest => dest.ActivePercentage, opt => opt.MapFrom(src => 
                src.TotalUsers > 0 ? (src.ActiveUsers * 100.0 / src.TotalUsers) : 0));
    }
    
    private string GetDisplayRole(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "系统管理员",
            UserRole.Doctor => "医生",
            UserRole.Nurse => "护士",
            UserRole.Receptionist => "接待员",
            _ => "未知角色"
        };
    }
    
    private string GetStatusText(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "正常",
            UserStatus.Inactive => "停用",
            UserStatus.Locked => "锁定",
            _ => "未知状态"
        };
    }
}

// 认证映射配置
public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<LoginRequest, LoginRequestDto>();
        
        CreateMap<ChangePasswordRequest, ChangePasswordRequestDto>();
        
        CreateMap<UpdateProfileRequest, UpdateProfileRequestDto>();
        
        CreateMap<UserProfileDto, UserProfile>()
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => 
                string.IsNullOrEmpty(src.Avatar) ? "/Assets/Images/default-avatar.png" : src.Avatar));
    }
}

// 通用映射配置
public class CommonMappingProfile : Profile
{
    public CommonMappingProfile()
    {
        // PagedResult mappings
        CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
        
        // 时间格式化
        CreateMap<DateTime, string>().ConvertUsing(dt => dt.ToString("yyyy-MM-dd HH:mm:ss"));
        CreateMap<DateTime?, string>().ConvertUsing(dt => dt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "");
        
        // 枚举到字符串映射
        CreateMap<UserRole, string>().ConvertUsing(role => role.ToString());
        CreateMap<UserStatus, string>().ConvertUsing(status => status.ToString());
    }
}
```

## 🔗 服务扩展方法

### ServiceResult扩展方法
```csharp
// ServiceResult扩展方法
public static class ServiceResultExtensions
{
    // 转换ServiceResult到不同类型
    public static ServiceResult<TTarget> Map<TSource, TTarget>(
        this ServiceResult<TSource> source, 
        Func<TSource, TTarget> mapper)
    {
        if (source.Success && source.Data != null)
        {
            try
            {
                var mappedData = mapper(source.Data);
                return ServiceResult<TTarget>.Success(mappedData);
            }
            catch (Exception ex)
            {
                return ServiceResult<TTarget>.Failure($"数据转换失败: {ex.Message}");
            }
        }
        
        return ServiceResult<TTarget>.Failure(source.ErrorMessage);
    }
    
    // 异步转换
    public static async Task<ServiceResult<TTarget>> MapAsync<TSource, TTarget>(
        this ServiceResult<TSource> source, 
        Func<TSource, Task<TTarget>> mapper)
    {
        if (source.Success && source.Data != null)
        {
            try
            {
                var mappedData = await mapper(source.Data);
                return ServiceResult<TTarget>.Success(mappedData);
            }
            catch (Exception ex)
            {
                return ServiceResult<TTarget>.Failure($"数据转换失败: {ex.Message}");
            }
        }
        
        return ServiceResult<TTarget>.Failure(source.ErrorMessage);
    }
    
    // 批量操作结果合并
    public static ServiceResult<List<T>> Combine<T>(this IEnumerable<ServiceResult<T>> results)
    {
        var resultsList = results.ToList();
        var failures = resultsList.Where(r => !r.Success).ToList();
        
        if (failures.Any())
        {
            var errorMessages = failures.Select(f => f.ErrorMessage);
            return ServiceResult<List<T>>.Failure(string.Join("; ", errorMessages));
        }
        
        var successData = resultsList.Where(r => r.Success && r.Data != null)
                                   .Select(r => r.Data)
                                   .ToList();
        
        return ServiceResult<List<T>>.Success(successData);
    }
    
    // 条件执行
    public static async Task<ServiceResult<T>> ExecuteIfAsync<T>(
        this ServiceResult<T> source,
        Func<T, bool> condition,
        Func<T, Task<ServiceResult<T>>> action)
    {
        if (!source.Success || source.Data == null)
            return source;
        
        if (condition(source.Data))
        {
            return await action(source.Data);
        }
        
        return source;
    }
}

// API响应扩展方法
public static class ApiResponseExtensions
{
    // API响应转ServiceResult
    public static ServiceResult<T> ToServiceResult<T>(this ApiResponse<T> apiResponse)
    {
        if (apiResponse.Success)
        {
            return ServiceResult<T>.Success(apiResponse.Data);
        }
        else
        {
            return ServiceResult<T>.Failure(apiResponse.ErrorMessage ?? "API调用失败");
        }
    }
    
    // 无泛型版本
    public static ServiceResult ToServiceResult(this ApiResponse apiResponse)
    {
        if (apiResponse.Success)
        {
            return ServiceResult.Success(apiResponse.Message);
        }
        else
        {
            return ServiceResult.Failure(apiResponse.ErrorMessage ?? "API调用失败");
        }
    }
}
```

## 🔧 依赖注入配置

### 服务注册
```csharp
// 服务注册扩展
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册API客户端
        var baseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl");
        
        services.AddRefitClient<IAuthApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
                
        services.AddRefitClient<IUserApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
                
        services.AddRefitClient<IPatientApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
                
        services.AddRefitClient<IMedicalCaseApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
                
        services.AddRefitClient<IConsultationApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
                
        services.AddRefitClient<IPrescriptionApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
                
        services.AddRefitClient<IHerbApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
                
        services.AddRefitClient<IFormulaApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));
        
        // 注册业务服务
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IMedicalCaseService, MedicalCaseService>();
        services.AddScoped<IConsultationService, ConsultationService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IHerbService, HerbService>();
        services.AddScoped<IFormulaService, FormulaService>();
        
        // 注册缓存服务
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        
        // 注册验证服务
        services.AddScoped<IValidationService, ValidationService>();
        
        // 注册AutoMapper
        services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
        
        return services;
    }
}
```

## 🧪 测试规范

### 业务服务测试
```csharp
[TestFixture]
public class AuthServiceTests
{
    private Mock<IAuthApi> _mockAuthApi;
    private Mock<IAuthenticationService> _mockAuthenticationService;
    private Mock<ICacheService> _mockCacheService;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<AuthService>> _mockLogger;
    private AuthService _authService;
    
    [SetUp]
    public void SetUp()
    {
        _mockAuthApi = new Mock<IAuthApi>();
        _mockAuthenticationService = new Mock<IAuthenticationService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        
        _authService = new AuthService(
            _mockAuthApi.Object,
            _mockAuthenticationService.Object,
            _mockCacheService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var request = new LoginRequest { Username = "test", Password = "password" };
        var apiRequest = new LoginRequestDto { Username = "test", Password = "password" };
        var userInfo = new UserInfo { Username = "test", DisplayName = "Test User" };
        
        _mockMapper.Setup(x => x.Map<LoginRequestDto>(request)).Returns(apiRequest);
        _mockAuthenticationService.Setup(x => x.LoginAsync(apiRequest)).ReturnsAsync(true);
        _mockAuthenticationService.SetupGet(x => x.CurrentUser).Returns(userInfo);
        
        // Act
        var result = await _authService.LoginAsync(request);
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.User, Is.EqualTo(userInfo));
    }
    
    [Test]
    public async Task LoginAsync_EmptyUsername_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest { Username = "", Password = "password" };
        
        // Act
        var result = await _authService.LoginAsync(request);
        
        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("用户名不能为空"));
    }
}
```

## 🚀 构建和部署

### 项目文件配置
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    
    <AssemblyTitle>LYBT Desktop Services</AssemblyTitle>
    <AssemblyDescription>凌隐宝堂桌面应用业务服务层</AssemblyDescription>
    <AssemblyVersion>1.0.0</AssemblyVersion>
    <FileVersion>1.0.0</FileVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AutoMapper" Version="12.0.1" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
    <PackageReference Include="Prism.Core" Version="9.0.537" />
    <PackageReference Include="Refit" Version="7.0.0" />
    <PackageReference Include="Refit.HttpClientFactory" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
    <ProjectReference Include="..\Core\LYBT.Desktop.Core.csproj" />
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
  </ItemGroup>

</Project>
```

## 📚 相关文档

### 架构文档
- [前端业务服务层设计](../../architecture/frontend-business-service-layer.md)
- [API客户端架构设计](../../architecture/api-client-architecture.md)
- [缓存策略设计](../../architecture/caching-strategy-design.md)

### 开发指南
- [Refit API客户端使用指南](../../development/refit-api-client-guide.md)
- [AutoMapper映射配置指南](../../development/automapper-configuration-guide.md)
- [服务层开发规范](../../development/service-layer-development-standards.md)
- [业务验证实现指南](../../development/business-validation-guide.md)

### 测试指南
- [业务服务测试规范](../../testing/business-service-testing-standards.md)
- [API客户端测试指南](../../testing/api-client-testing-guide.md)
- [缓存服务测试实践](../../testing/cache-service-testing-practices.md)

### 部署文档
- [服务层部署配置](../../deployment/service-layer-deployment.md)
- [API客户端配置管理](../../deployment/api-client-configuration.md)
- [性能监控和调优](../../deployment/performance-monitoring-tuning.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 前端开发组