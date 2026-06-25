# 文档审计发现的代码问题修复实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复文档审计过程中发现的 3 个代码层面问题：AuthController 响应格式不一致、权限策略定义缺失、AuthController 缺少 refresh 端点

**Architecture:** 修改 AuthController 使所有响应使用 `ApiResponse<T>` 包装；在 PolicyConstants 中补充缺失的权限策略；为 AuthController 添加 `/refresh` 端点。

**Tech Stack:** C#, ASP.NET Core 8, Identity, JWT

## Global Constraints

- 所有 API 响应必须使用 `ApiResponse<T>` 或 `ApiResponse` 包装
- 不破坏现有 API 兼容性
- 遵循 BaseApiController 的响应方法模式（`Success()`, `Error()`, `HandleAuthResult()` 等）
- 新增代码必须有对应测试

---

## Task 1: 修复 AuthController 登录失败响应格式

**Covers:** [S2 2.1]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:61,65,124,129,135,155`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`（对应修改）
- Test: `tests/LYBT.Tests.Server/Integration/AuthControllerTests.cs`（新增验证测试）

**Interfaces:**
- Consumes: `BaseApiController` 的 `HandleAuthResult<T>()` 方法
- Produces: AuthController 所有失败响应统一使用 `ApiResponse<T>` 包装

- [ ] **Step 1: 读取当前 AuthController 确认问题点**

读取 `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`，确认以下问题行：
- 第 61 行：`return Unauthorized(new { message = "用户名或密码错误" });`
- 第 65 行：`return Unauthorized(new { message = "用户名或密码错误" });`
- 第 124 行：`return Unauthorized(new { valid = false, message = "Missing Authorization header", errorCode = "TokenInvalid" });`
- 第 129 行：`return Unauthorized(new { valid = false, message = "Invalid Authorization header format", errorCode = "TokenInvalid" });`
- 第 135 行：`return Unauthorized(new { valid = false, message = "Missing token in Authorization header", errorCode = "TokenInvalid" });`
- 第 155 行：`return Unauthorized(new { valid = false, message = "Token is invalid", errorCode = "ERR-10202" });`

- [ ] **Step 2: 修复 Login 方法的失败响应**

将第 61 行和第 65 行的：
```csharp
return Unauthorized(new { message = "用户名或密码错误" });
```
改为：
```csharp
return HandleAuthResult(Result<LoginResponse>.Fail("用户名或密码错误", ModuleErrorCode.AuthInvalidCredentials), "登录失败");
```

- [ ] **Step 3: 修复 ValidateToken 方法的失败响应**

将第 124、129、135、155 行的裸 `Unauthorized(new { ... })` 改为使用 `HandleAuthResult` 或直接构建 `ApiResponse`：

```csharp
// 第 124 行 - Missing header
var response = ApiResponse<object>.CreateFail("Missing Authorization header");
response.RequestId = GetRequestId();
response.Errors = new { code = "TokenInvalid" };
return Unauthorized(response);

// 第 129 行 - Invalid format
var response = ApiResponse<object>.CreateFail("Invalid Authorization header format");
response.RequestId = GetRequestId();
response.Errors = new { code = "TokenInvalid" };
return Unauthorized(response);

// 第 135 行 - Missing token
var response = ApiResponse<object>.CreateFail("Missing token in Authorization header");
response.RequestId = GetRequestId();
response.Errors = new { code = "TokenInvalid" };
return Unauthorized(response);

// 第 155 行 - Invalid token
var response = ApiResponse<object>.CreateFail("Token is invalid");
response.RequestId = GetRequestId();
response.Errors = new { code = "ERR-10202" };
return Unauthorized(response);
```

- [ ] **Step 4: 同步修改 LocalWebAPI 的 AuthController**

读取 `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`，应用相同的响应格式修复。

- [ ] **Step 5: 编写验证测试**

在 `tests/LYBT.Tests.Server/Integration/AuthControllerTests.cs` 中添加测试：

```csharp
[Fact]
public async Task Login_InvalidCredentials_ReturnsApiResponseFormat()
{
    var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        UserName = "nonexistent",
        Password = "wrong"
    });

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    var content = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
    content.ShouldNotBeNull();
    content.Success.ShouldBeFalse();
    content.Message.ShouldContain("用户名或密码错误");
    content.RequestId.ShouldNotBeNullOrWhiteSpace();
}
```

- [ ] **Step 6: 运行测试验证**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~AuthControllerTests" --no-restore
```

- [ ] **Step 7: 提交**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs tests/LYBT.Tests.Server/Integration/AuthControllerTests.cs
git commit -m "fix(auth): standardize AuthController error responses to use ApiResponse<T> envelope"
```

---

## Task 2: 补充 PolicyConstants 缺失的权限策略

**Covers:** [S2 2.3]

**Files:**
- Modify: `src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs`
- Test: `tests/LYBT.Tests.Architecture/`（验证策略注册）

**Interfaces:**
- Consumes: 现有 `PolicyConstants.AdminOnly` 和 `PolicyConstants.DoctorOrAdmin`
- Produces: 新增 `PolicyConstants.AdminOrSuperAdmin` 和 `PolicyConstants.DoctorOrReceptionist`

- [ ] **Step 1: 在 PolicyConstants 中添加新策略常量**

读取 `src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs`。

添加：
```csharp
public static class PolicyConstants
{
    public const string AdminOnly = "AdminOnly";
    public const string DoctorOrAdmin = "DoctorOrAdmin";
    public const string AdminOrSuperAdmin = "AdminOrSuperAdmin";
    public const string DoctorOrReceptionist = "DoctorOrReceptionist";
}
```

- [ ] **Step 2: 在 WebAPI 中注册新策略**

读取 `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs`。

在 `AddPolicy` 调用区域添加：
```csharp
options.AddPolicy(PolicyConstants.AdminOrSuperAdmin, policy =>
    policy.RequireRole(RoleConstants.Admin, RoleConstants.SuperAdmin));

options.AddPolicy(PolicyConstants.DoctorOrReceptionist, policy =>
    policy.RequireRole(RoleConstants.Doctor, RoleConstants.Receptionist));
```

- [ ] **Step 3: 在 LocalWebAPI 中注册新策略**

读取 `src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs`，添加相同的策略注册。

- [ ] **Step 4: 运行架构测试验证**

```bash
dotnet test tests/LYBT.Tests.Architecture/ --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Server/Core/LYBT.Infrastructure/Constants/PolicyConstants.cs src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs src/Client/Desktop/LocalWebAPI/Auth/LocalJwtConfig.cs
git commit -m "feat(auth): add AdminOrSuperAdmin and DoctorOrReceptionist authorization policies"
```

---

## Task 3: 为 AuthController 添加 /refresh 端点

**Covers:** [S2 2.2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Interfaces/IJwtService.cs`（如需添加方法）
- Modify: `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`（如需实现方法）
- Test: `tests/LYBT.Tests.Server/Integration/AuthControllerTests.cs`

**Interfaces:**
- Consumes: `IJwtService.ValidateToken()`, `IJwtService.GenerateToken()`
- Produces: `POST /api/v1/auth/refresh` 端点

- [ ] **Step 1: 确认 DTO 已存在**

读取 `src/Shared/LYBT.Shared.Models/Contracts/Auth/RefreshTokenRequest.cs`，确认 DTO 结构。

- [ ] **Step 2: 在 IJwtService 中添加 RefreshToken 方法签名**

如果 `IJwtService` 没有 `RefreshToken` 方法，添加：
```csharp
Result<LoginResponse> RefreshToken(string expiredToken);
```

- [ ] **Step 3: 在 JwtService 中实现 RefreshToken**

实现逻辑：验证旧 token → 生成新 token → 返回新的 `LoginResponse`。

- [ ] **Step 4: 在 AuthController 中添加 /refresh 端点**

```csharp
[HttpPost("refresh")]
[AllowAnonymous]
[ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
[ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]
public IActionResult RefreshTokenAsync([FromBody] RefreshTokenRequest request)
{
    if (ValidateModel() is { } modelError) return modelError;

    var result = _jwtService.RefreshToken(request.Token);
    return HandleAuthResult(result, "Token刷新成功");
}
```

- [ ] **Step 5: 在 LocalWebAPI 的 AuthController 中添加相同端点**

- [ ] **Step 6: 编写测试**

```csharp
[Fact]
public async Task RefreshToken_ValidToken_ReturnsNewToken()
{
    // 先登录获取 token
    var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        UserName = "admin",
        Password = "Admin@123456"
    });
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
    var oldToken = loginContent.Data.Token;

    // 刷新 token
    var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
    {
        Token = oldToken
    });

    refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    var content = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
    content.Success.ShouldBeTrue();
    content.Data.Token.ShouldNotBe(oldToken);
}
```

- [ ] **Step 7: 运行测试验证**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~AuthControllerTests" --no-restore
```

- [ ] **Step 8: 提交**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs src/Server/Modules/LYBT.Module.Auth/ tests/LYBT.Tests.Server/Integration/AuthControllerTests.cs
git commit -m "feat(auth): implement POST /auth/refresh endpoint for token renewal"
```

---

## Self-Review

**Spec coverage check:**
- [S2 2.1] AuthController 响应格式修复 → Task 1 ✓
- [S2 2.2] AuthController /refresh 端点 → Task 3 ✓
- [S2 2.3] 权限策略统一 → Task 2 ✓
- [S2 2.4] 文档交叉引用清理 → 已在第一阶段完成 ✓

**Placeholder scan:** 无 TBD/TODO。

**Type consistency:** Task 1 使用 `HandleAuthResult<T>()` 与 BaseApiController 一致；Task 2 的策略常量与现有 `AdminOnly`/`DoctorOrAdmin` 命名风格一致；Task 3 的 `RefreshToken` 方法签名与 `IJwtService` 接口风格一致。
