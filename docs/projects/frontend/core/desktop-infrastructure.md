# Desktop.Infrastructure Project (桌面基础设施项目)

## 📋 项目概述

### 项目定位
**Desktop.Infrastructure** 是凌隐宝堂中医诊所系统的**前端基础设施项目**，提供整个桌面应用的核心基础服务、依赖注入容器配置、事件聚合、配置管理和基础设施抽象。作为前端架构的基础支撑层，为所有上层项目提供统一的基础服务和架构支撑。

### 核心价值
- 🏗️ **架构基础**: Prism模块化和依赖注入容器配置
- 🔧 **基础服务**: 提供通用基础服务抽象和实现
- 📡 **事件系统**: 基于Prism EventAggregator的事件通信
- ⚙️ **配置管理**: 统一的配置加载、存储和管理
- 🔐 **安全服务**: 客户端认证、令牌管理和安全策略
- 📊 **日志系统**: 结构化日志记录和异常处理
- 🎯 **抽象接口**: 为业务层提供清晰的服务接口定义

### 技术定位 (v1.0)
```
所有上层项目 (Core, Services, Modules, Shell)
    ↑ 依赖
LYBT.Desktop.Infrastructure (基础设施层) ← 本项目
    ↑ 依赖
.NET 8.0 + Prism.DryIoc 9.0.537 + Microsoft.Extensions
```

## 🏗️ 技术架构

### 核心技术栈
```csharp
// 基础技术栈
- .NET 8.0-windows
- Prism.DryIoc 9.0.537 (IoC容器 + 模块化)
- Microsoft.Extensions.Configuration (配置系统)
- Microsoft.Extensions.Logging (日志系统)
- Microsoft.Extensions.Hosting (主机服务)
- Microsoft.Extensions.Http (HTTP客户端)
- System.Text.Json (JSON序列化)

// 项目引用
<ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
<ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
<ProjectReference Include="..\..\Shared\LYBT.Shared.Utilities\LYBT.Shared.Utilities.csproj" />
```

### 项目结构架构
```
src/Client/Desktop/Infrastructure/
├── Configuration/              # 配置管理
│   ├── IConfigurationService.cs
│   ├── ConfigurationService.cs
│   ├── AppSettings.cs
│   └── ConfigurationExtensions.cs
├── Services/                   # 基础服务
│   ├── Authentication/         # 认证服务
│   │   ├── IAuthenticationService.cs
│   │   ├── AuthenticationService.cs
│   │   └── TokenManager.cs
│   ├── Dialog/                 # 对话框服务
│   │   ├── IDialogService.cs
│   │   └── DialogService.cs
│   ├── Http/                   # HTTP服务
│   │   ├── IApiClient.cs
│   │   └── ApiClient.cs
│   ├── Navigation/             # 导航服务
│   │   ├── INavigationService.cs
│   │   └── NavigationService.cs
│   └── Storage/                # 存储服务
│       ├── IStorageService.cs
│       └── LocalStorageService.cs
├── Events/                     # 事件定义
│   ├── UserEvents.cs
│   ├── NavigationEvents.cs
│   └── SystemEvents.cs
├── Exceptions/                 # 异常处理
│   ├── InfrastructureException.cs
│   ├── ApiException.cs
│   └── GlobalExceptionHandler.cs
├── Extensions/                 # 扩展方法
│   ├── ServiceCollectionExtensions.cs
│   ├── ContainerExtensions.cs
│   └── LoggingExtensions.cs
├── Constants/                  # 常量定义
│   ├── ApiConstants.cs
│   ├── ConfigurationKeys.cs
│   └── EventNames.cs
└── Models/                     # 基础模型
    ├── ApiResponse.cs
    ├── PagedResult.cs
    └── ServiceResult.cs
```

## 🔧 核心服务规范

### 1. 认证服务 (AuthenticationService)
```csharp
// 认证服务接口
public interface IAuthenticationService
{
    Task<bool> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<bool> RefreshTokenAsync();
    bool IsAuthenticated { get; }
    UserInfo CurrentUser { get; }
    string AccessToken { get; }
    event EventHandler<UserInfo> UserChanged;
    event EventHandler<bool> AuthenticationStateChanged;
}

// 认证服务实现
public class AuthenticationService : IAuthenticationService
{
    private readonly IApiClient _apiClient;
    private readonly ITokenManager _tokenManager;
    private readonly IConfigurationService _configurationService;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<AuthenticationService> _logger;
    
    private UserInfo _currentUser;
    private Timer _tokenRefreshTimer;
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && 
                                  _tokenManager.IsTokenValid(AccessToken);
    
    public UserInfo CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser != value)
            {
                _currentUser = value;
                UserChanged?.Invoke(this, value);
            }
        }
    }
    
    public string AccessToken => _tokenManager.GetAccessToken();
    
    public event EventHandler<UserInfo> UserChanged;
    public event EventHandler<bool> AuthenticationStateChanged;
    
    public AuthenticationService(
        IApiClient apiClient,
        ITokenManager tokenManager,
        IConfigurationService configurationService,
        IEventAggregator eventAggregator,
        ILogger<AuthenticationService> logger)
    {
        _apiClient = apiClient;
        _tokenManager = tokenManager;
        _configurationService = configurationService;
        _eventAggregator = eventAggregator;
        _logger = logger;
        
        InitializeTokenRefreshTimer();
    }
    
    public async Task<bool> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("开始用户登录: {Username}", request.Username);
            
            var response = await _apiClient.PostAsync<LoginResponse>("/api/v1/auth/login", request);
            
            if (response.Success && response.Data != null)
            {
                // 存储令牌
                await _tokenManager.StoreTokensAsync(response.Data.AccessToken, response.Data.RefreshToken);
                
                // 设置当前用户
                CurrentUser = response.Data.User;
                
                // 发布登录事件
                _eventAggregator.GetEvent<UserLoggedInEvent>().Publish(CurrentUser);
                
                // 触发认证状态变更事件
                AuthenticationStateChanged?.Invoke(this, true);
                
                _logger.LogInformation("用户登录成功: {Username}", CurrentUser.Username);
                return true;
            }
            
            _logger.LogWarning("用户登录失败: {Username}, 错误: {Error}", 
                request.Username, response.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录过程中发生异常: {Username}", request.Username);
            throw new AuthenticationException("登录失败", ex);
        }
    }
    
    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("开始用户注销");
            
            // 调用注销API
            if (!string.IsNullOrEmpty(AccessToken))
            {
                try
                {
                    await _apiClient.PostAsync("/api/v1/auth/logout", null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "调用注销API失败，继续本地注销");
                }
            }
            
            // 清除本地令牌和用户信息
            await _tokenManager.ClearTokensAsync();
            CurrentUser = null;
            
            // 停止令牌刷新定时器
            _tokenRefreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            // 发布注销事件
            _eventAggregator.GetEvent<UserLoggedOutEvent>().Publish();
            
            // 触发认证状态变更事件
            AuthenticationStateChanged?.Invoke(this, false);
            
            _logger.LogInformation("用户注销成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注销过程中发生异常");
            throw;
        }
    }
    
    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = _tokenManager.GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("刷新令牌为空，无法刷新访问令牌");
                return false;
            }
            
            var request = new RefreshTokenRequest { RefreshToken = refreshToken };
            var response = await _apiClient.PostAsync<LoginResponse>("/api/v1/auth/refresh", request);
            
            if (response.Success && response.Data != null)
            {
                await _tokenManager.StoreTokensAsync(response.Data.AccessToken, response.Data.RefreshToken);
                _logger.LogDebug("访问令牌刷新成功");
                return true;
            }
            
            _logger.LogWarning("访问令牌刷新失败: {Error}", response.ErrorMessage);
            
            // 刷新失败，执行注销
            await LogoutAsync();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新访问令牌时发生异常");
            await LogoutAsync();
            return false;
        }
    }
    
    private void InitializeTokenRefreshTimer()
    {
        // 每5分钟检查一次令牌是否需要刷新
        _tokenRefreshTimer = new Timer(async _ =>
        {
            if (IsAuthenticated && _tokenManager.ShouldRefreshToken(AccessToken))
            {
                await RefreshTokenAsync();
            }
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    }
}
```

### 2. 令牌管理器 (TokenManager)
```csharp
// 令牌管理接口
public interface ITokenManager
{
    Task StoreTokensAsync(string accessToken, string refreshToken);
    Task ClearTokensAsync();
    string GetAccessToken();
    string GetRefreshToken();
    bool IsTokenValid(string token);
    bool ShouldRefreshToken(string token);
    DateTime? GetTokenExpiration(string token);
}

// 令牌管理实现
public class TokenManager : ITokenManager
{
    private readonly IStorageService _storageService;
    private readonly ILogger<TokenManager> _logger;
    
    private const string ACCESS_TOKEN_KEY = "access_token";
    private const string REFRESH_TOKEN_KEY = "refresh_token";
    private const int REFRESH_THRESHOLD_MINUTES = 10; // 提前10分钟刷新
    
    public TokenManager(IStorageService storageService, ILogger<TokenManager> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }
    
    public async Task StoreTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            await _storageService.SetSecureAsync(ACCESS_TOKEN_KEY, accessToken);
            await _storageService.SetSecureAsync(REFRESH_TOKEN_KEY, refreshToken);
            
            _logger.LogDebug("令牌存储成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "存储令牌时发生异常");
            throw;
        }
    }
    
    public async Task ClearTokensAsync()
    {
        try
        {
            await _storageService.RemoveAsync(ACCESS_TOKEN_KEY);
            await _storageService.RemoveAsync(REFRESH_TOKEN_KEY);
            
            _logger.LogDebug("令牌清除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除令牌时发生异常");
            throw;
        }
    }
    
    public string GetAccessToken()
    {
        try
        {
            return _storageService.GetSecure(ACCESS_TOKEN_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取访问令牌时发生异常");
            return null;
        }
    }
    
    public string GetRefreshToken()
    {
        try
        {
            return _storageService.GetSecure(REFRESH_TOKEN_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取刷新令牌时发生异常");
            return null;
        }
    }
    
    public bool IsTokenValid(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;
            
        try
        {
            var expiration = GetTokenExpiration(token);
            return expiration.HasValue && expiration.Value > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "验证令牌时发生异常");
            return false;
        }
    }
    
    public bool ShouldRefreshToken(string token)
    {
        var expiration = GetTokenExpiration(token);
        if (!expiration.HasValue)
            return false;
            
        return expiration.Value <= DateTime.UtcNow.AddMinutes(REFRESH_THRESHOLD_MINUTES);
    }
    
    public DateTime? GetTokenExpiration(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;
            
        try
        {
            var handler = new JsonWebTokenHandler();
            var jsonToken = handler.ReadJsonWebToken(token);
            return jsonToken.ValidTo;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析令牌过期时间时发生异常");
            return null;
        }
    }
}
```

### 3. API客户端 (ApiClient)
```csharp
// API客户端接口
public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data);
    Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data);
    Task<ApiResponse<T>> DeleteAsync<T>(string endpoint);
    Task<ApiResponse> PostAsync(string endpoint, object data);
    Task<ApiResponse> PutAsync(string endpoint, object data);
    Task<ApiResponse> DeleteAsync(string endpoint);
}

// API客户端实现
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenManager _tokenManager;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public ApiClient(HttpClient httpClient, ITokenManager tokenManager, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _tokenManager = tokenManager;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
    
    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        return await SendRequestAsync<T>(HttpMethod.Get, endpoint);
    }
    
    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        return await SendRequestAsync<T>(HttpMethod.Post, endpoint, data);
    }
    
    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        return await SendRequestAsync<T>(HttpMethod.Put, endpoint, data);
    }
    
    public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
    {
        return await SendRequestAsync<T>(HttpMethod.Delete, endpoint);
    }
    
    public async Task<ApiResponse> PostAsync(string endpoint, object data)
    {
        return await SendRequestAsync(HttpMethod.Post, endpoint, data);
    }
    
    public async Task<ApiResponse> PutAsync(string endpoint, object data)
    {
        return await SendRequestAsync(HttpMethod.Put, endpoint, data);
    }
    
    public async Task<ApiResponse> DeleteAsync(string endpoint)
    {
        return await SendRequestAsync(HttpMethod.Delete, endpoint);
    }
    
    private async Task<ApiResponse<T>> SendRequestAsync<T>(HttpMethod method, string endpoint, object data = null)
    {
        try
        {
            var request = CreateHttpRequest(method, endpoint, data);
            
            _logger.LogDebug("发送API请求: {Method} {Endpoint}", method, endpoint);
            
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(responseContent, _jsonOptions);
                _logger.LogDebug("API请求成功: {Method} {Endpoint}", method, endpoint);
                return result;
            }
            else
            {
                _logger.LogWarning("API请求失败: {Method} {Endpoint}, 状态码: {StatusCode}, 响应: {Response}",
                    method, endpoint, response.StatusCode, responseContent);
                    
                return ApiResponse<T>.Failure($"请求失败: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "网络请求异常: {Method} {Endpoint}", method, endpoint);
            return ApiResponse<T>.Failure("网络连接异常，请检查网络连接");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "请求超时: {Method} {Endpoint}", method, endpoint);
            return ApiResponse<T>.Failure("请求超时，请稍后重试");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API请求发生异常: {Method} {Endpoint}", method, endpoint);
            return ApiResponse<T>.Failure($"请求异常: {ex.Message}");
        }
    }
    
    private async Task<ApiResponse> SendRequestAsync(HttpMethod method, string endpoint, object data = null)
    {
        var response = await SendRequestAsync<object>(method, endpoint, data);
        return new ApiResponse
        {
            Success = response.Success,
            Message = response.Message,
            ErrorMessage = response.ErrorMessage,
            Timestamp = response.Timestamp,
            RequestId = response.RequestId
        };
    }
    
    private HttpRequestMessage CreateHttpRequest(HttpMethod method, string endpoint, object data = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
        
        // 添加认证头
        var token = _tokenManager.GetAccessToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        // 添加请求内容
        if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        
        // 添加通用头
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-Client-Type", "Desktop");
        request.Headers.Add("X-Client-Version", "1.0.0");
        
        return request;
    }
}
```

### 4. 配置服务 (ConfigurationService)
```csharp
// 配置服务接口
public interface IConfigurationService
{
    T GetValue<T>(string key, T defaultValue = default);
    Task SetValueAsync<T>(string key, T value);
    Task<bool> RemoveAsync(string key);
    Task SaveAsync();
    Task LoadAsync();
    event EventHandler<string> ConfigurationChanged;
}

// 配置服务实现
public class ConfigurationService : IConfigurationService
{
    private readonly IStorageService _storageService;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly Dictionary<string, object> _configurationCache;
    private readonly object _lockObject = new();
    
    private const string CONFIGURATION_KEY = "app_configuration";
    
    public event EventHandler<string> ConfigurationChanged;
    
    public ConfigurationService(IStorageService storageService, ILogger<ConfigurationService> logger)
    {
        _storageService = storageService;
        _logger = logger;
        _configurationCache = new Dictionary<string, object>();
    }
    
    public T GetValue<T>(string key, T defaultValue = default)
    {
        if (string.IsNullOrEmpty(key))
            return defaultValue;
        
        lock (_lockObject)
        {
            if (_configurationCache.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "转换配置值类型失败: {Key}, {Value}", key, value);
                    return defaultValue;
                }
            }
        }
        
        return defaultValue;
    }
    
    public async Task SetValueAsync<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
            return;
        
        lock (_lockObject)
        {
            _configurationCache[key] = value;
        }
        
        await SaveAsync();
        
        ConfigurationChanged?.Invoke(this, key);
        _logger.LogDebug("配置值已更新: {Key} = {Value}", key, value);
    }
    
    public async Task<bool> RemoveAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;
        
        bool removed;
        lock (_lockObject)
        {
            removed = _configurationCache.Remove(key);
        }
        
        if (removed)
        {
            await SaveAsync();
            ConfigurationChanged?.Invoke(this, key);
            _logger.LogDebug("配置值已删除: {Key}", key);
        }
        
        return removed;
    }
    
    public async Task SaveAsync()
    {
        try
        {
            Dictionary<string, object> configToSave;
            lock (_lockObject)
            {
                configToSave = new Dictionary<string, object>(_configurationCache);
            }
            
            var json = JsonSerializer.Serialize(configToSave, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            await _storageService.SetAsync(CONFIGURATION_KEY, json);
            _logger.LogDebug("配置已保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置时发生异常");
            throw;
        }
    }
    
    public async Task LoadAsync()
    {
        try
        {
            var json = await _storageService.GetAsync(CONFIGURATION_KEY);
            if (!string.IsNullOrEmpty(json))
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                lock (_lockObject)
                {
                    _configurationCache.Clear();
                    foreach (var kvp in config)
                    {
                        _configurationCache[kvp.Key] = JsonElementToObject(kvp.Value);
                    }
                }
                
                _logger.LogDebug("配置已加载，共 {Count} 个配置项", config.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置时发生异常");
            throw;
        }
    }
    
    private object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
```

### 5. 对话框服务 (DialogService)
```csharp
// 对话框服务接口
public interface IDialogService
{
    void ShowInformation(string message, string title = "信息");
    void ShowWarning(string message, string title = "警告");
    void ShowError(string message, string title = "错误");
    Task<bool> ShowConfirmationAsync(string message, string title = "确认");
    Task<string> ShowInputDialogAsync(string prompt, string title = "输入", string defaultValue = "");
    Task<T> ShowDialogAsync<T>(string dialogName, DialogParameters parameters = null);
}

// 对话框服务实现
public class DialogService : IDialogService
{
    private readonly IContainerProvider _containerProvider;
    private readonly ILogger<DialogService> _logger;
    
    public DialogService(IContainerProvider containerProvider, ILogger<DialogService> logger)
    {
        _containerProvider = containerProvider;
        _logger = logger;
    }
    
    public void ShowInformation(string message, string title = "信息")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }
    
    public void ShowWarning(string message, string title = "警告")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }
    
    public void ShowError(string message, string title = "错误")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
    
    public Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        return Task.Run(() =>
        {
            var result = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialogResult = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
                result = dialogResult == MessageBoxResult.Yes;
            });
            return result;
        });
    }
    
    public Task<string> ShowInputDialogAsync(string prompt, string title = "输入", string defaultValue = "")
    {
        return Task.Run(() =>
        {
            string result = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 这里应该显示自定义输入对话框
                // 为了简化，使用Microsoft.VisualBasic.Interaction.InputBox
                // 实际项目中应该创建自定义的输入对话框
                result = Microsoft.VisualBasic.Interaction.InputBox(prompt, title, defaultValue);
            });
            return string.IsNullOrEmpty(result) ? null : result;
        });
    }
    
    public async Task<T> ShowDialogAsync<T>(string dialogName, DialogParameters parameters = null)
    {
        try
        {
            // 这里应该根据dialogName解析并显示对应的对话框
            // 返回对话框的结果
            await Task.CompletedTask;
            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示对话框时发生异常: {DialogName}", dialogName);
            throw;
        }
    }
}
```

## 🚀 事件系统架构

### 事件定义
```csharp
// 用户相关事件
public class UserLoggedInEvent : PubSubEvent<UserInfo> { }
public class UserLoggedOutEvent : PubSubEvent { }
public class UserUpdatedEvent : PubSubEvent<UserInfo> { }

// 导航相关事件
public class NavigationRequestedEvent : PubSubEvent<NavigationRequest> { }
public class NavigationCompletedEvent : PubSubEvent<string> { }

// 系统相关事件
public class ApplicationInitializedEvent : PubSubEvent { }
public class ApplicationShuttingDownEvent : PubSubEvent { }
public class ConfigurationChangedEvent : PubSubEvent<string> { }
public class ThemeChangedEvent : PubSubEvent<string> { }

// 数据相关事件
public class DataLoadedEvent<T> : PubSubEvent<T> { }
public class DataSavedEvent<T> : PubSubEvent<T> { }
public class DataDeletedEvent : PubSubEvent<Guid> { }

// 错误相关事件
public class ErrorOccurredEvent : PubSubEvent<ErrorInfo> { }
public class ValidationErrorEvent : PubSubEvent<ValidationResult> { }
```

### 事件管理器
```csharp
// 事件管理器接口
public interface IEventManager
{
    void Publish<T>(T eventData) where T : PubSubEvent, new();
    void Publish<T, TPayload>(TPayload payload) where T : PubSubEvent<TPayload>, new();
    void Subscribe<T>(Action callback) where T : PubSubEvent, new();
    void Subscribe<T, TPayload>(Action<TPayload> callback) where T : PubSubEvent<TPayload>, new();
    void Unsubscribe<T>(Action callback) where T : PubSubEvent, new();
    void Unsubscribe<T, TPayload>(Action<TPayload> callback) where T : PubSubEvent<TPayload>, new();
}

// 事件管理器实现
public class EventManager : IEventManager
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<EventManager> _logger;
    
    public EventManager(IEventAggregator eventAggregator, ILogger<EventManager> logger)
    {
        _eventAggregator = eventAggregator;
        _logger = logger;
    }
    
    public void Publish<T>(T eventData) where T : PubSubEvent, new()
    {
        try
        {
            _eventAggregator.GetEvent<T>().Publish();
            _logger.LogDebug("事件已发布: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布事件时发生异常: {EventType}", typeof(T).Name);
        }
    }
    
    public void Publish<T, TPayload>(TPayload payload) where T : PubSubEvent<TPayload>, new()
    {
        try
        {
            _eventAggregator.GetEvent<T>().Publish(payload);
            _logger.LogDebug("事件已发布: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布事件时发生异常: {EventType}", typeof(T).Name);
        }
    }
    
    public void Subscribe<T>(Action callback) where T : PubSubEvent, new()
    {
        try
        {
            _eventAggregator.GetEvent<T>().Subscribe(callback);
            _logger.LogDebug("事件订阅成功: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅事件时发生异常: {EventType}", typeof(T).Name);
        }
    }
    
    public void Subscribe<T, TPayload>(Action<TPayload> callback) where T : PubSubEvent<TPayload>, new()
    {
        try
        {
            _eventAggregator.GetEvent<T>().Subscribe(callback);
            _logger.LogDebug("事件订阅成功: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订阅事件时发生异常: {EventType}", typeof(T).Name);
        }
    }
    
    public void Unsubscribe<T>(Action callback) where T : PubSubEvent, new()
    {
        try
        {
            _eventAggregator.GetEvent<T>().Unsubscribe(callback);
            _logger.LogDebug("事件取消订阅: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅事件时发生异常: {EventType}", typeof(T).Name);
        }
    }
    
    public void Unsubscribe<T, TPayload>(Action<TPayload> callback) where T : PubSubEvent<TPayload>, new()
    {
        try
        {
            _eventAggregator.GetEvent<T>().Unsubscribe(callback);
            _logger.LogDebug("事件取消订阅: {EventType}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消订阅事件时发生异常: {EventType}", typeof(T).Name);
        }
    }
}
```

## 🔧 依赖注入配置

### 服务注册扩展
```csharp
// 基础设施服务注册
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDesktopInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // 配置HTTP客户端
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            var baseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl");
            if (!string.IsNullOrEmpty(baseUrl))
            {
                client.BaseAddress = new Uri(baseUrl);
            }
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // 注册基础服务
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IStorageService, LocalStorageService>();
        services.AddScoped<ITokenManager, TokenManager>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IEventManager, EventManager>();
        
        // 注册日志服务
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        return services;
    }
    
    // Container注册扩展
    public static IContainerRegistry RegisterInfrastructureServices(
        this IContainerRegistry containerRegistry)
    {
        // 注册Prism相关服务
        containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
        
        // 注册基础设施服务
        containerRegistry.Register<IConfigurationService, ConfigurationService>();
        containerRegistry.Register<IStorageService, LocalStorageService>();
        containerRegistry.Register<ITokenManager, TokenManager>();
        containerRegistry.Register<IAuthenticationService, AuthenticationService>();
        containerRegistry.Register<IDialogService, DialogService>();
        containerRegistry.Register<INavigationService, NavigationService>();
        containerRegistry.Register<IEventManager, EventManager>();
        
        return containerRegistry;
    }
}
```

## ⚙️ 配置管理

### 应用设置模型
```csharp
// 应用配置模型
public class AppSettings
{
    public ApiSettings ApiSettings { get; set; } = new();
    public AuthenticationSettings AuthenticationSettings { get; set; } = new();
    public LoggingSettings LoggingSettings { get; set; } = new();
    public UISettings UISettings { get; set; } = new();
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://localhost:7001";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public bool EnableLogging { get; set; } = true;
}

public class AuthenticationSettings
{
    public int TokenRefreshThresholdMinutes { get; set; } = 10;
    public bool RememberLogin { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 480; // 8小时
}

public class LoggingSettings
{
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    public bool EnableFileLogging { get; set; } = true;
    public string LogFilePath { get; set; } = "Logs";
    public int MaxLogFileSizeMB { get; set; } = 10;
    public int MaxLogFileCount { get; set; } = 5;
}

public class UISettings
{
    public string Theme { get; set; } = "Light";
    public string Language { get; set; } = "zh-CN";
    public int PageSize { get; set; } = 50;
    public bool ShowAnimations { get; set; } = true;
    public double WindowOpacity { get; set; } = 1.0;
}
```

### 配置文件示例
```json
{
  "AppSettings": {
    "ApiSettings": {
      "BaseUrl": "https://localhost:7001",
      "TimeoutSeconds": 30,
      "RetryCount": 3,
      "EnableLogging": true
    },
    "AuthenticationSettings": {
      "TokenRefreshThresholdMinutes": 10,
      "RememberLogin": true,
      "SessionTimeoutMinutes": 480
    },
    "LoggingSettings": {
      "MinimumLevel": "Information",
      "EnableFileLogging": true,
      "LogFilePath": "Logs",
      "MaxLogFileSizeMB": 10,
      "MaxLogFileCount": 5
    },
    "UISettings": {
      "Theme": "Light",
      "Language": "zh-CN",
      "PageSize": 50,
      "ShowAnimations": true,
      "WindowOpacity": 1.0
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning",
      "LYBT": "Debug"
    }
  }
}
```

## 🚀 异常处理

### 全局异常处理器
```csharp
// 全局异常处理器
public class GlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IEventManager _eventManager;
    private readonly IDialogService _dialogService;
    
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IEventManager eventManager,
        IDialogService dialogService)
    {
        _logger = logger;
        _eventManager = eventManager;
        _dialogService = dialogService;
        
        // 订阅全局异常事件
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        HandleException(exception, "未处理的应用程序异常", e.IsTerminating);
    }
    
    private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleException(e.Exception.GetBaseException(), "未处理的任务异常", false);
        e.SetObserved(); // 标记异常已处理
    }
    
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        HandleException(e.Exception, "UI线程异常", false);
        e.Handled = true; // 标记异常已处理，防止应用程序崩溃
    }
    
    private void HandleException(Exception exception, string context, bool isTerminating)
    {
        try
        {
            _logger.LogError(exception, "全局异常处理: {Context}, 是否终止: {IsTerminating}", context, isTerminating);
            
            // 发布错误事件
            _eventManager.Publish<ErrorOccurredEvent, ErrorInfo>(new ErrorInfo
            {
                Exception = exception,
                Context = context,
                Timestamp = DateTime.Now,
                IsTerminating = isTerminating
            });
            
            // 显示用户友好的错误消息
            if (!isTerminating)
            {
                var userMessage = GetUserFriendlyMessage(exception);
                _dialogService.ShowError(userMessage, "系统错误");
            }
        }
        catch (Exception handlerException)
        {
            // 异常处理器本身发生异常，记录到事件日志或输出到调试器
            System.Diagnostics.Debug.WriteLine($"异常处理器发生异常: {handlerException}");
        }
    }
    
    private string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => "网络连接异常，请检查网络连接后重试",
            TimeoutException => "请求超时，请稍后重试",
            UnauthorizedAccessException => "您没有执行此操作的权限",
            AuthenticationException => "身份验证失败，请重新登录",
            ApiException apiEx => apiEx.Message,
            _ => "系统发生未知错误，请联系技术支持"
        };
    }
}

// 错误信息模型
public class ErrorInfo
{
    public Exception Exception { get; set; }
    public string Context { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsTerminating { get; set; }
}
```

## 🧪 测试规范

### 服务单元测试
```csharp
[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IApiClient> _mockApiClient;
    private Mock<ITokenManager> _mockTokenManager;
    private Mock<IConfigurationService> _mockConfigurationService;
    private Mock<IEventAggregator> _mockEventAggregator;
    private Mock<ILogger<AuthenticationService>> _mockLogger;
    private AuthenticationService _authenticationService;
    
    [SetUp]
    public void SetUp()
    {
        _mockApiClient = new Mock<IApiClient>();
        _mockTokenManager = new Mock<ITokenManager>();
        _mockConfigurationService = new Mock<IConfigurationService>();
        _mockEventAggregator = new Mock<IEventAggregator>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        
        _authenticationService = new AuthenticationService(
            _mockApiClient.Object,
            _mockTokenManager.Object,
            _mockConfigurationService.Object,
            _mockEventAggregator.Object,
            _mockLogger.Object);
    }
    
    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var request = new LoginRequest { Username = "test", Password = "password" };
        var response = ApiResponse<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            User = new UserInfo { Username = "test", DisplayName = "Test User" }
        });
        
        _mockApiClient.Setup(x => x.PostAsync<LoginResponse>("/api/v1/auth/login", request))
                     .ReturnsAsync(response);
        
        // Act
        var result = await _authenticationService.LoginAsync(request);
        
        // Assert
        Assert.That(result, Is.True);
        Assert.That(_authenticationService.CurrentUser?.Username, Is.EqualTo("test"));
        _mockTokenManager.Verify(x => x.StoreTokensAsync("access-token", "refresh-token"), Times.Once);
    }
    
    [Test]
    public async Task LoginAsync_InvalidCredentials_ReturnsFalse()
    {
        // Arrange
        var request = new LoginRequest { Username = "test", Password = "wrong-password" };
        var response = ApiResponse<LoginResponse>.Failure("Invalid credentials");
        
        _mockApiClient.Setup(x => x.PostAsync<LoginResponse>("/api/v1/auth/login", request))
                     .ReturnsAsync(response);
        
        // Act
        var result = await _authenticationService.LoginAsync(request);
        
        // Assert
        Assert.That(result, Is.False);
        Assert.That(_authenticationService.CurrentUser, Is.Null);
        _mockTokenManager.Verify(x => x.StoreTokensAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
```

### 集成测试
```csharp
[TestFixture]
public class InfrastructureIntegrationTests
{
    private IServiceProvider _serviceProvider;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();
        
        services.AddDesktopInfrastructure(configuration);
        _serviceProvider = services.BuildServiceProvider();
    }
    
    [Test]
    public void ServiceRegistration_AllServicesCanBeResolved()
    {
        // Assert that all required services can be resolved
        Assert.DoesNotThrow(() => _serviceProvider.GetRequiredService<IConfigurationService>());
        Assert.DoesNotThrow(() => _serviceProvider.GetRequiredService<IStorageService>());
        Assert.DoesNotThrow(() => _serviceProvider.GetRequiredService<ITokenManager>());
        Assert.DoesNotThrow(() => _serviceProvider.GetRequiredService<IAuthenticationService>());
        Assert.DoesNotThrow(() => _serviceProvider.GetRequiredService<IDialogService>());
    }
    
    [Test]
    public async Task ConfigurationService_SaveAndLoad_WorksCorrectly()
    {
        // Arrange
        var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
        const string key = "test-key";
        const string value = "test-value";
        
        // Act
        await configService.SetValueAsync(key, value);
        var retrievedValue = configService.GetValue<string>(key);
        
        // Assert
        Assert.That(retrievedValue, Is.EqualTo(value));
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
    
    <!-- 程序集信息 -->
    <AssemblyTitle>LYBT Desktop Infrastructure</AssemblyTitle>
    <AssemblyDescription>凌隐宝堂桌面应用基础设施服务</AssemblyDescription>
    <AssemblyVersion>1.0.0</AssemblyVersion>
    <FileVersion>1.0.0</FileVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Prism.DryIoc" Version="9.0.537" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />
    <PackageReference Include="Microsoft.VisualBasic" Version="10.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
    <ProjectReference Include="..\..\Shared\LYBT.Shared.Utilities\LYBT.Shared.Utilities.csproj" />
  </ItemGroup>

</Project>
```

## 📚 相关文档

### 架构文档
- [前端架构设计标准](../../architecture/frontend-architecture-standards.md)
- [依赖注入容器配置指南](../../architecture/dependency-injection-configuration.md)
- [事件驱动架构设计](../../architecture/event-driven-architecture.md)

### 开发指南
- [基础设施服务开发规范](../../development/infrastructure-service-standards.md)
- [认证服务集成指南](../../development/authentication-service-integration.md)
- [配置管理最佳实践](../../development/configuration-management-best-practices.md)
- [异常处理策略](../../development/exception-handling-strategies.md)

### 测试指南
- [基础设施测试规范](../../testing/infrastructure-testing-standards.md)
- [Mock服务测试指南](../../testing/mock-service-testing-guide.md)
- [集成测试实践](../../testing/integration-testing-practices.md)

### 部署文档
- [基础设施部署指南](../../deployment/infrastructure-deployment-guide.md)
- [配置文件管理](../../deployment/configuration-file-management.md)
- [日志配置和监控](../../deployment/logging-configuration-monitoring.md)

---

**文档版本**: v1.0.0  
**创建日期**: 2025-01-09  
**最后更新**: 2025-01-09  
**维护团队**: 前端开发组