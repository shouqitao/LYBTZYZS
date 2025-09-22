# HttpClientFactory 迁移指南

## 概述

本文档说明如何从旧的 `Core.Http.HttpClientFactory` 迁移到新的 `UnifiedApiClientManager` 架构。

## 架构变更

### 旧架构（已废弃）
- **位置**: `src/Client/Desktop/Core/Http/HttpClientFactory.cs`
- **类型**: 实例化的工厂类，实现 `IHttpClientFactory` 接口
- **问题**:
  - 每个模块独立管理 HttpClient 实例
  - 缺乏统一的 API 客户端管理
  - 重复的配置代码

### 新架构（推荐）

#### 1. API 调用场景
- **使用**: `Infrastructure.Api.UnifiedApiClientManager`
- **位置**: `src/Client/Desktop/Infrastructure/Api/UnifiedApiClientManager.cs`
- **优势**:
  - 统一管理所有 8 个业务模块的 API 客户端
  - 集成 Refit 类型安全 API 调用
  - 自动处理认证令牌

#### 2. 基础 HttpClient 创建
- **使用**: `Infrastructure.HttpClientFactory`（静态工厂类）
- **位置**: `src/Client/Desktop/Infrastructure/HttpClientFactory.cs`
- **用途**: 为 `UnifiedApiClientManager` 提供基础 HttpClient 实例

## 迁移步骤

### 步骤 1: 更新依赖注入

**旧代码**:
```csharp
// 注册 IHttpClientFactory
containerRegistry.RegisterSingleton<IHttpClientFactory, HttpClientFactory>();

// 在服务中注入
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task CallApi()
    {
        var client = _httpClientFactory.CreateClient("api");
        // 手动调用 API
    }
}
```

**新代码**:
```csharp
// 注册已在 ServiceCollectionExtensions 中完成
// 直接注入 UnifiedApiClientManager 或具体的 API 接口

public class MyService
{
    private readonly IUnifiedApiClientManager _apiManager;
    // 或者直接注入具体的 API
    private readonly IUserApi _userApi;

    public MyService(IUnifiedApiClientManager apiManager, IUserApi userApi)
    {
        _apiManager = apiManager;
        _userApi = userApi;
    }

    public async Task CallApi()
    {
        // 使用类型安全的 Refit API
        var users = await _userApi.GetAllUsersAsync();
        // 或者
        var patients = await _apiManager.PatientApi.GetPatientsAsync();
    }
}
```

### 步骤 2: 更新认证处理

**旧代码**:
```csharp
var client = _httpClientFactory.CreateClient("authenticated");
AuthenticationHandler.SetBearerToken(token);
```

**新代码**:
```csharp
// 通过 UnifiedApiClientManager 设置令牌
_apiManager.SetAuthorizationToken(token);
```

### 步骤 3: 更新 API 调用

**旧代码**:
```csharp
var client = _httpClientFactory.CreateClient("api");
var json = JsonSerializer.Serialize(data);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync("api/v1/users", content);
var result = await response.Content.ReadAsStringAsync();
var user = JsonSerializer.Deserialize<User>(result);
```

**新代码**:
```csharp
// 使用类型安全的 Refit API
var user = await _apiManager.UserApi.CreateUserAsync(data);
```

## 注意事项

1. **不要混用**: 避免同时使用旧的 `Core.Http.HttpClientFactory` 和新的 `UnifiedApiClientManager`

2. **保留的类**: `Infrastructure.HttpClientFactory`（静态工厂）仍然保留，用于创建基础 HttpClient 实例

3. **废弃警告**: 使用旧 API 会产生编译警告，提示使用新 API

4. **测试更新**: 更新单元测试，使用新的 API 接口进行模拟

## 示例：完整迁移案例

### Auth 模块迁移示例

**旧实现**:
```csharp
public class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        var client = _httpClientFactory.CreateClient("api");
        var loginData = new { username, password };
        var json = JsonSerializer.Serialize(loginData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("api/v1/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResult>(result);
        }

        throw new Exception("Login failed");
    }
}
```

**新实现**:
```csharp
public class AuthBusinessService : IAuthBusinessService
{
    private readonly IAuthApi _authApi;

    public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto request)
    {
        try
        {
            var response = await _authApi.LoginAsync(request);
            return ServiceResult<LoginResponseDto>.Success(response);
        }
        catch (ApiException ex)
        {
            return ServiceResult<LoginResponseDto>.Failure($"登录失败: {ex.Message}");
        }
    }
}
```

## 迁移检查清单

- [ ] 移除对 `Core.Http.IHttpClientFactory` 的所有引用
- [ ] 更新依赖注入配置，使用 `IUnifiedApiClientManager`
- [ ] 替换手动 HTTP 调用为 Refit API 调用
- [ ] 更新认证令牌设置方法
- [ ] 更新异常处理，使用 `ApiException`
- [ ] 运行测试确保功能正常
- [ ] 清理未使用的 using 语句

## 支持

如有问题，请参考：
- `UnifiedApiClientManager` 源代码和注释
- 各模块的 API 接口定义（`IUserApi`, `IPatientApi` 等）
- 单元测试示例