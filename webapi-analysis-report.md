# LYBT.WebAPI 代码分析与处理方案

## 总览
- 目标：识别无用代码、简化方法与配置、降低样板与重复，统一启动/中间件与版本策略。
- 范围：`src/Server/Services/LYBT.WebAPI`（Program、Extensions、Controllers、Middleware、Config）。

## 主要发现（问题与影响）
- 未使用/可移除
  - `Middleware/SecurityHeadersMiddleware.cs` 未在管道中启用；如无启用计划可删除。
  - `Config/memo.json` 未被任何代码引用，可删除。
- 重复/冲突配置
  - 业务模块重复注册（多余）：`Extensions/UnifiedServiceRegistration.cs:257`–`264` 同时注册单模块与 `AddAllModules()`。
  - 生产配置校验包含 CORS，但项目已移除 CORS：`Extensions/UnifiedServiceRegistration.cs:482`–`487` 始终可能失败。
  - 启动时写入磁盘报告（生产环境）：`Extensions/UnifiedServiceRegistration.cs:491`–`505`，容器/云环境通常无写权限，不建议。
- 启动/中间件顺序与冗余
  - `UseRouting()` 被隐藏在子方法里（`ConfigureAuthenticationMiddleware`），建议在 `ConfigureAllMiddleware` 顶层只调用一次，提升可读性与可控性。
  - Program 配置源重复添加环境变量（一次在前置配置，一次在 `builder.Configuration`），可保留一处。
- 控制器简化空间
  - `UsersController` 手工模型校验可交给 `[ApiController]`（自动 400），减少样板；未使用变量（如 `operatorRole`）可移除。
- API 版本与 Swagger
  - 同时开启 Query/Header/Segment 三种版本读取（不必要）：仅 Segment 已满足路由策略，建议简化。

## 处理方案（最小可实施）
- 删除/停用
  - 删除 `Config/memo.json`；如短期不启用安全头，删除 `Middleware/SecurityHeadersMiddleware.cs` 或在生产启用 `app.UseSecurityHeaders()`。
- 合并/精简
  - 业务模块注册：删除 `AddUsersModuleServices()` 与 `AddAuthModule()` 重复调用，只保留 `AddAllModules()`（`Extensions/UnifiedServiceRegistration.cs:257`–`264`）。
  - 移除 CORS 校验与磁盘写报告：`Extensions/UnifiedServiceRegistration.cs:482`–`505`。
  - Program 仅保留一处 `AddEnvironmentVariables()`；将 `UseRouting()` 提升到 `ConfigureAllMiddleware` 顶层统一调用一次。
  - `UsersController`：移除手工 `ValidateModel*`，删除未使用变量读写，统一使用 `HandleServiceResult` 入口。
  - API 版本读取器仅保留 `UrlSegmentApiVersionReader`。

## 风险与验证
- 风险：服务注册变更需确认无缺失；Swagger SchemaId 自定义逻辑保留，若简化需回归验证。
- 验证：
  - 构建与启动（Development/Production）；
  - Swagger UI/鉴权（JWT）/版本路由（`/api/v1/...`）；
  - 健康检查 `GET /api/v1/health` 与 `details`；
  - 用户接口全链路（列表/详情/创建/更新/切换状态）。

## 后续可选优化
- 全局异常与模型验证通过过滤器/ProblemDetails 统一；
- 将生产配置校验改为“日志+启动失败”，不做磁盘写入；
- 为启动/注册/中间件写集成测试，保障重构后的顺序正确。
