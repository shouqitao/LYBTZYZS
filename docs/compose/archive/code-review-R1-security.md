# R1 安全性审查报告

**审查范围**: `src/Server/` 全部代码 + `src/Shared/` 安全相关代码
**审查日期**: 2026-08-21
**审查人**: Hermes Agent (AI)

---

## 发现的问题

| # | 文件:行号 | 严重度 | 问题描述 | 修复建议 |
|---|----------|--------|---------|----------|
| 1 | `src/Tools/PasswordHashGenerator/appsettings.json:3-5` | 🔴 高 | **硬编码密码提交到仓库**: `SysAdminPassword: "DevPass123!"`, `NewUserPassword: "Lybt2025@TempPass#"` 明文写入配置文件并提交到 Git。即使这是开发工具，密码也不应出现在版本控制中。 | 从 Git 历史中清除；改用 `user-secrets` 或环境变量注入；在 `.gitignore` 中排除敏感配置 |
| 2 | `src/Server/Services/LYBT.WebAPI/config/appsettings.json:45-48` | 🟡 中 | **开发默认密码硬编码**: `DefaultPasswords` 节中 `DevP@ssw0rd!` 作为三个角色的默认密码写死在配置文件中。若生产部署遗漏 `user-secrets` 或环境变量覆盖，系统将使用这些已知密码。 | ① 在启动时若检测到 `DefaultPasswords` 为默认值且环境为 Production 则拒绝启动；② 添加注释警告 |
| 3 | `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs:72-87` | 🟡 中 | **登出端点无需认证**: `LogoutAsync` 标记 `[AllowAnonymous]`，任何人可通过提供 RefreshToken 撤销任意会话。虽然 Token 本身需要有效，但攻击者可利用此接口配合 Token 窃取实现会话撤销（Denial of Service）。 | 将 `[AllowAnonymous]` 改为 `[Authorize]`，要求认证用户才能登出；或至少验证调用者身份与 Token 所属用户一致 |
| 4 | `src/Server/Modules/LYBT.Module.Identity/Application/Commands/LoginCommandHandler.cs:186` | 🟡 中 | **Access Token 同时作为 Refresh Token**: `RefreshToken = token` 将访问令牌直接作为刷新令牌返回。注释说明这是设计决策，但意味着 Access Token 泄露后攻击者可同时获取 Refresh 能力，大幅延长攻击窗口。 | 实现独立的 Refresh Token（建议 opaque token + 独立签名密钥），或至少实现 Refresh Token 轮换机制 |
| 5 | `src/Server/Core/LYBT.Infrastructure/Services/DeployService.cs:18-40` | 🟡 中 | **文件上传无大小限制**: `SaveUpdatePackageAsync` 仅验证 `.zip` 扩展名，未设置最大文件大小限制。攻击者可上传超大文件导致磁盘耗尽（DoS）。 | ① 在 Controller 层添加 `[RequestSizeLimit]`（如 100MB）；② 在 Service 层添加 `content.Length` 上限检查 |
| 6 | `src/Server/Services/LYBT.WebAPI/HealthCheck/SqlServerHealthCheck.cs:79-81` | 🟡 中 | **健康检查泄露 SQL 错误信息**: `HealthCheckResult.Unhealthy($"SQL Server连接失败: {ex.Message}")` 将原始 SQL 异常消息返回给调用者，可能泄露数据库类型、版本、连接信息。此端点虽需认证（`/api/v1/health/details`），但错误详情仍不应暴露。 | 返回通用错误消息（如"数据库连接失败"），将详细错误仅写入服务端日志 |
| 7 | `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/SystemExceptionHandler.cs:50-61` | 🟡 中 | **开发环境泄露完整异常堆栈**: 在 `IsDevelopment()` 时返回 `exceptionType`、`stackTrace`、`exception.Message` 到响应体。虽然仅限开发环境，但若配置错误将 `ASPNETCORE_ENVIRONMENT` 设为 `Development` 运行于生产，将泄露内部实现细节。 | ① 在启动时检测 Production 环境下绝不泄露堆栈；② 使用 `#if DEBUG` 编译指令替代运行时环境判断 |
| 8 | `src/Shared/LYBT.Shared.Models/Validators/Users/UserInputDtoValidator.cs:39` vs `src/Shared/LYBT.Shared.Models/Utilities/Security/PasswordPolicyValidator.cs:19` | 🟡 中 | **密码最小长度不一致**: DTO 验证器允许密码最小 6 位（`Length(6, 128)`），但 Identity 配置和 `PasswordPolicyValidator` 要求最小 8 位。通过 DTO 验证的 6-7 位密码可能在某些路径绕过 Identity 策略。 | 统一密码最小长度为 8 位（对齐 `PasswordPolicyValidator.Policy.MinLength`） |
| 9 | `src/Server/Modules/LYBT.Module.Identity/Application/Commands/AutoLoginCommandHandler.cs:22-35` | 🟡 中 | **自动登录令牌无额外绑定**: Auto-Login Token 仅验证 JWT 签名和有效期，未绑定 IP/设备指纹。长期令牌（RememberMe 可达 30 天）被盗后可从任意位置使用。 | ① 在 Auto-Login Token 中嵌入 IP 哈希或设备指纹；② 验证时比对当前请求的 IP/UA |
| 10 | `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:184-189` | 🟡 中 | **Swagger 在非生产环境暴露**: `!app.Environment.IsProduction()` 时启用 Swagger UI，且 `/swagger` 路径 CSP 放宽（`unsafe-eval`）。在测试/staging 环境中 Swagger 暴露全部 API 文档，降低攻击门槛。 | ① 将 Swagger 默认关闭，仅通过显式配置 `Swagger:Enabled=true` 启用；② 为 Swagger 端点添加认证要求 |
| 11 | `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:54-55` | 🟡 中 | **异常处理兜底泄露异常类型**: `UnifiedMiddlewareConfiguration` 的 Fallback 异常处理器在开发环境返回 `$"[DEV] {exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}"`，直接拼接异常堆栈到响应体。 | 仅在 `#if DEBUG` 编译时返回详细信息，生产二进制即使配置为 Development 也不泄露 |

---

## 无问题的关键文件

以下文件经审查未发现安全问题：

- **SQL 注入防护**: `LogCleanupService.cs:84` — 使用 `ExecuteSqlRawAsync` + `SqlParameter` 参数化查询，无 SQL 注入风险
- **认证/授权**: `AuthenticationServiceCollectionExtensions.cs` — JWT 配置完整（ValidateIssuer/Audience/Lifetime/SigningKey），FallbackPolicy 强制认证
- **授权策略**: `PolicyConstants.cs` + 各 Controller `[Authorize]` — 角色隔离完善，`UserHierarchyGuard` 实现一级管一级
- **安全响应头**: `SecurityHeadersMiddleware.cs` — CSP/X-Frame-Options/HSTS/X-Content-Type-Options 全面覆盖
- **敏感数据脱敏**: `SensitiveDataMasker.cs` + `SensitiveDataDestructuringPolicy.cs` — 密码/Token/连接字符串自动脱敏
- **密码安全**: `PasswordHelper.cs` — 使用 `RandomNumberGenerator` 密码学安全随机数
- **配置写入白名单**: `ConfigurationWritePolicy.cs` — 禁止运行时修改 ConnectionStrings/Jwt:SecretKey/DefaultPasswords
- **账户锁定**: `LoginCommandHandler.cs` — 支持失败计数 + 账户锁定 + 安全审计日志
- **CORS**: `ApiServiceCollectionExtensions.cs` — 指定来源白名单，非通配符
- **CSRF**: JWT Bearer Token 认证天然防 CSRF（Token 在 Authorization 头中，不受浏览器自动发送）
- **输入验证**: FluentValidation 验证器覆盖用户名/密码/手机号/邮箱等字段

---

## 总结

- 🔴 **高风险**: 1 个（#1 硬编码密码提交仓库）
- 🟡 **中风险**: 10 个（#2-#11）
- ✅ **通过**: 11 个关键安全领域

### 整体评价

项目安全架构设计较为完善：
- JWT 认证配置严格（ValidateIssuer/Audience/Lifetime/SigningKey 全部启用）
- FallbackPolicy 默认要求所有端点认证
- 角色隔离和层级管理（UserHierarchyGuard）设计合理
- 安全响应头（CSP/HSTS/X-Frame-Options）全面覆盖
- 敏感数据脱敏体系（SensitiveDataMasker + Serilog DestructuringPolicy）完善
- 安全审计日志记录登录成功/失败事件

主要风险集中在：① 配置文件中的硬编码密码（#1, #2）；② 认证流程设计（#3 登出无认证, #4 Access=Refresh, #9 Auto-Login 无绑定）；③ 信息泄露（#6 健康检查, #7 异常堆栈, #10 Swagger）。
