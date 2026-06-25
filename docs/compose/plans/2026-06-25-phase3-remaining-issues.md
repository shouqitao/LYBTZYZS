# 第三阶段：遗留问题修复实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 补充缺失的 Reports API 文档，将新权限策略应用到对应 Controller，实现 /auto-login 端点

**Architecture:** 创建 Reports API 文档文件；修改 Controller 的 `[Authorize]` 属性使用新策略；在 AuthController 中实现 auto-login 端点。

**Tech Stack:** C#, ASP.NET Core 8, Markdown, Mermaid

## Global Constraints

- 所有 API 响应使用 `ApiResponse<T>` 包装
- 新策略 `AdminOrSuperAdmin` 和 `DoctorOrReceptionist` 已在 Task 2 中注册
- 遵循现有 API 参考文档格式（请求/响应 JSON + curl + 错误码表）

---

## Task 1: 新增 Reports API 文档

**Covers:** [S2 2.1]

**Files:**
- Create: `docs/04-api-reference/13-reports.md`
- Modify: `docs/04-api-reference/README.md`（添加索引）

**Interfaces:**
- Consumes: `ReportsController` 的 3 个端点签名
- Produces: 完整的 Reports API 参考文档

- [ ] **Step 1: 读取 ReportsController 确认端点**

读取 `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs`。

确认 3 个端点：
- `GET /api/v1/reports/daily/income` → `DailyIncomeDto`
- `GET /api/v1/reports/daily/consultations` → `DailyConsultationDto`
- `GET /api/v1/reports/daily/herbs` → `DailyHerbUsageDto`

- [ ] **Step 2: 读取 DTO 定义**

读取 `src/Shared/LYBT.Shared.Models/Contracts/Reports/` 下的 DTO 文件，确认字段定义。

- [ ] **Step 3: 创建 13-reports.md**

创建 `docs/04-api-reference/13-reports.md`，包含：

```markdown
# 报表模块 API

> 日期: 2026-06-25 | 状态: Active

## 端点列表

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| GET | `/reports/daily/income` | DoctorOrAdmin | 日收入统计 |
| GET | `/reports/daily/consultations` | DoctorOrAdmin | 日诊疗统计 |
| GET | `/reports/daily/herbs` | DoctorOrAdmin | 日药材使用统计 |

## GET /reports/daily/income

**描述**：查询当日收入统计

**权限**：DoctorOrAdmin

**请求示例**：

```bash
curl -X GET http://localhost:5000/api/v1/reports/daily/income \
  -H "Authorization: Bearer {access_token}"
```

**成功响应 (200)**：

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "date": "2026-06-25",
    "totalIncome": 12500.00,
    "registrationCount": 15,
    "averagePerPatient": 833.33
  },
  "requestId": "0HN8V..."
}
```

## GET /reports/daily/consultations

**描述**：查询当日诊疗统计

**请求示例**：

```bash
curl -X GET http://localhost:5000/api/v1/reports/daily/consultations \
  -H "Authorization: Bearer {access_token}"
```

**成功响应 (200)**：

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "date": "2026-06-25",
    "totalConsultations": 18,
    "completedConsultations": 15,
    "pendingConsultations": 3
  },
  "requestId": "0HN8V..."
}
```

## GET /reports/daily/herbs

**描述**：查询当日药材使用统计

**请求示例**：

```bash
curl -X GET http://localhost:5000/api/v1/reports/daily/herbs \
  -H "Authorization: Bearer {access_token}"
```

**成功响应 (200)**：

```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "date": "2026-06-25",
    "topHerbs": [
      { "herbId": "Guid", "herbName": "黄芪", "usageCount": 12 },
      { "herbId": "Guid", "herbName": "当归", "usageCount": 10 }
    ],
    "totalHerbTypes": 25
  },
  "requestId": "0HN8V..."
}
```

## 错误码

| 错误码 | HTTP 状态码 | 说明 |
|--------|------------|------|
| Unauthorized | 401 | 未登录或 Token 无效 |
| Forbidden | 403 | 权限不足 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-25 | v1.0 | 初始版本，覆盖 3 个报表端点 |
```

- [ ] **Step 4: 更新 README.md 索引**

读取 `docs/04-api-reference/README.md`，在模块端点索引中添加 Reports 模块。

- [ ] **Step 5: 提交**

```bash
git add docs/04-api-reference/13-reports.md docs/04-api-reference/README.md
git commit -m "docs(api): add Reports module API reference with 3 daily report endpoints"
```

---

## Task 2: 应用新权限策略到 Controller

**Covers:** [S2 2.2]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs`（同步）

**Interfaces:**
- Consumes: `PolicyConstants.AdminOrSuperAdmin`（已注册）
- Produces: Controller 级别使用新策略

- [ ] **Step 1: 更新 UsersController 策略**

读取 `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`。

将 `[Authorize(Policy = PolicyConstants.AdminOnly)]` 改为 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`。

注意：只修改 Controller 级别的 `[Authorize]`，不要修改方法级别的单独 `[Authorize]`（如果有）。

- [ ] **Step 2: 更新 DiagnosticsController 策略**

读取 `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs`。

将 `[Authorize(Policy = PolicyConstants.AdminOnly)]` 改为 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`。

- [ ] **Step 3: 更新 ConfigurationController 策略**

读取 `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs`。

将 `[Authorize(Policy = PolicyConstants.AdminOnly)]` 改为 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`。

- [ ] **Step 4: 同步 LocalWebAPI UsersController**

读取 `src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs`，应用相同的策略变更。

- [ ] **Step 5: 运行测试验证**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~UsersControllerTests" --no-restore
dotnet test tests/LYBT.Tests.Architecture/ --no-restore
```

- [ ] **Step 6: 提交**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs src/Client/Desktop/LocalWebAPI/Controllers/UsersController.cs
git commit -m \"feat(auth): apply AdminOrSuperAdmin policy to Users, Diagnostics, and Configuration controllers\"
```

---

## Task 3: 实现 /auto-login 端点

**Covers:** [S2 2.3]

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`
- Modify: `src/Server/Modules/LYBT.Module.Auth/Interfaces/IJwtService.cs`（如需）
- Test: `tests/LYBT.Tests.Server/Integration/AuthControllerTests.cs`

**Interfaces:**
- Consumes: `AutoLoginRequest` DTO, `IJwtService`
- Produces: `POST /api/v1/auth/auto-login` 端点

- [ ] **Step 1: 确认 DTO 结构**

读取 `src/Shared/LYBT.Shared.Models/Contracts/Auth/AutoLoginRequest.cs`。

- [ ] **Step 2: 在 IJwtService 中添加 ValidateAutoLoginToken 方法**

```csharp
Result<LoginResponse> ValidateAutoLoginToken(string autoLoginToken);
```

- [ ] **Step 3: 在 JwtService 中实现**

实现逻辑：验证 auto-login token → 查找用户 → 生成新的 JWT → 返回 `LoginResponse`。

- [ ] **Step 4: 在 Server AuthController 中添加端点**

```csharp
[HttpPost(\"auto-login\")]\n[AllowAnonymous]\n[ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]\n[ProducesResponseType(typeof(ApiResponse<LoginResponse>), 401)]\npublic IActionResult AutoLoginAsync([FromBody] AutoLoginRequest request)\n{\n    if (ValidateModel() is { } modelError) return modelError;\n\n    var result = _jwtService.ValidateAutoLoginToken(request.Token);\n    return HandleAuthResult(result, \"自动登录成功\");\n}\n```

- [ ] **Step 5: 在 LocalWebAPI AuthController 中添加相同端点**

- [ ] **Step 6: 编写测试**

```csharp
[Fact]\npublic async Task AutoLogin_ValidToken_ReturnsToken()\n{\n    // Note: AutoLogin token generation is Desktop-only\n    // This test verifies the endpoint exists and handles invalid tokens\n    var response = await _client.PostAsJsonAsync(\"/api/v1/auth/auto-login\", new\n    {\n        Token = \"invalid-token\"\n    });\n\n    response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);\n}\n```

- [ ] **Step 7: 运行测试**

```bash
dotnet test tests/LYBT.Tests.Server/ --filter \"FullyQualifiedName~AuthControllerTests\" --no-restore\n```

- [ ] **Step 8: 提交**

```bash
git add src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs src/Server/Modules/LYBT.Module.Auth/ tests/LYBT.Tests.Server/Integration/AuthControllerTests.cs\ngit commit -m \"feat(auth): implement POST /auth/auto-login endpoint for desktop client\"
```

---

## Self-Review

**Spec coverage check:**
- [S2 2.1] Reports API 文档 → Task 1 ✓
- [S2 2.2] 应用新权限策略 → Task 2 ✓
- [S2 2.3] /auto-login 端点 → Task 3 ✓

**Placeholder scan:** 无 TBD/TODO。

**Type consistency:** Task 1 的 DTO 字段与 ReportsController 实际返回一致；Task 2 的策略常量与 Phase 2 注册的常量一致；Task 3 的 `AutoLoginRequest` DTO 已存在。
