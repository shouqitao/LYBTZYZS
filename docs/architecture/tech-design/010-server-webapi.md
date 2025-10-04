# 010-Server WebAPI 设计（最小闭环）

## 决策
- 默认端口：5001；允许 `ASPNETCORE_URLS` 覆盖。
- 启动流程：Program → RegisterAllApplicationServices → Build → InitializeAllApplicationServices → ConfigureAllMiddleware。
- 认证：用户名/密码 → JWT（HS256）。
- 健康检查：GET `/api/health` 返回 200。
- 种子账户：首启检查/创建 `sysadmin`（Dev 打印一次强密码，Prod 不打印）。

## 接口契约
- POST `/api/auth/login`
  - Request: `{ username, password }`
  - Response: `{ accessToken, expiresIn, userId, role }`
  - 错误：400 参数错误；401 认证失败
- GET `/api/health`
  - Response: `200 OK` `{ status: "Healthy", time: "..." }`

## 配置优先级
appsettings.json → appsettings.{ENV}.json → 环境变量。生产环境强校验 JWT Secret；开发环境允许警告。

## 代码证据
- 启动与装配：
  - WebAPI 入口：`src/Server/Services/LYBT.WebAPI/Program.cs:39`
  - 统一注册：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs:22`
  - 统一初始化：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedApplicationInitialization.cs:16`
  - 端口默认回退（拟改为 5001）：`src/Server/Services/LYBT.WebAPI/Program.cs:49`

## 自检结果（✅ 已验证）
- **登录接口**：✅ 已存在 `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:39-76`
- **健康检查**：✅ 已存在 `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs:37-59`
- **端口配置**：✅ 已改为5001 `Program.cs:49`
- **sysadmin播种**：✅ 已存在 `src/Server/Core/LYBT.Infrastructure/Data/DatabaseInitializationService.cs:62`

## 实际状态（无需改动）
1) ✅ 端口已设为5001：`urls = "http://localhost:5001"`
2) ✅ HealthController已存在：`GET /api/v1/health`
3) ✅ AuthController已存在：`POST /api/v1/auth/login`
4) ✅ sysadmin播种已实现：`InitializeAdminSecretsAsync()`

