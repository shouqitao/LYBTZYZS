# 服务器硬化与架构复盘报告（LYBT.Server.sln）

- 项目：LYBT 中医诊疗系统（Server 侧）
- 日期：2025-09-21
- 范围：`LYBT.Server.sln`（WebAPI、Core、Modules、Shared）
- 关联基线：`docs/governance/server-hardening-plan.md`

—

## 执行结论

- 当前功能实现基本稳健，但存在数项“未落地/未接线”的硬化配置与遗留扩展并存问题，容易造成认知偏差与安全策略失效。
- 需优先处理“生产暴露 Swagger”“安全头未应用”“性能中间件已注册未启用”“缺少速率限制接线”4 个硬化缺口。均为非功能性改动，但可能影响外部可见行为（需确认开关策略）。
- 架构分层（WebAPI -> Modules -> Infrastructure/Entities）清晰，架构测试齐全；建议收敛重复/过时扩展，避免“双轨并存”。

—

## 评审方法

- 代码走查：入口与装配层（Program、Extensions/*）、安全与配置（Options/*、appsettings.*）、中间件与控制器、核心数据访问（AppDbContext）。
- 架构测试：`tests/Architecture/` 规则审阅，聚焦路由规范、禁止框架、分层依赖。

—

## 关键发现与风险

1) 生产暴露 Swagger（需确认）
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:31`、`:49`
- 现状：`app.UseSwagger(); app.UseSwaggerUI();` 在所有环境启用，未限制生产访问，也未加保护策略。
- 风险：在生产环境暴露 API 面，易被扫描与枚举；不属于功能，但外部可见，建议仅非生产启用，或受鉴权保护。

2) 安全头未实际应用（未接线）
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/Application/MiddlewareConfigurationExtensions.cs:84` 提供 `ConfigureSecurityHeaders` 扩展；`src/Server/Core/LYBT.Infrastructure/Configuration/Options/SecurityOptions.cs` 定义 `SecurityHeaders`；`src/Server/Services/LYBT.WebAPI/appsettings.Security.json` 已配置 CSP 等。
- 现状：统一中间件装配未调用此扩展，未将 `SecurityOptions` 的策略下发到响应头。
- 风险：安全策略“有配置无落地”，与生产前置校验不一致。

3) 性能中间件已注册未启用
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/PerformanceOptimization.cs`
- 现状：服务注册了 ResponseCompression/ResponseCaching/OutputCache，但 `ConfigureAllMiddleware` 未调用 `app.UsePerformanceOptimizations()`。
- 风险：配置与运行不一致；压缩/缓存/输出缓存策略未生效。

4) 速率限制未接线（需确认策略）
- 位置：`SecurityOptions.RateLimit` 已定义（`src/Server/Core/LYBT.Infrastructure/Configuration/Options/SecurityOptions.cs`），`appsettings.Security.json` 已给出规则。
- 现状：未见 `AddRateLimiter/UseRateLimiter` 接线与策略映射（如登录接口、通用路由策略）。
- 风险：撞库/爆破与滥用防护能力不足。为非功能改动但具外部影响，需策略确认（白名单/豁免路由等）。

5) JSON 编码使用 UnsafeRelaxed（需评估）
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs` 中 `AddControllers().AddJsonOptions` 设置 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。
- 风险：API 默认无需 HTML 上下文嵌入，使用默认编码更保守；建议在确需兼容场景下以选项开关控制。

6) CORS 空实现但被调用
- 位置：`RegisterCorsServices` 方法体为空，但在统一注册流程中被调用。
- 风险：造成“以为已启用 CORS”的错觉；若未来引入 Web 前端将产生混淆。建议显式注释或移除调用。

7) 配置校验重复路径并存
- 位置：`Infrastructure.Configuration.Extensions.EnvironmentAwareValidation` 与 `UnifiedServiceRegistration` 私有 `AddEnvironmentAwareValidation(...)` 并存。
- 风险：两套入口容易分叉；建议统一到基础设施扩展一处，以减少歧义。

8) 健康检查内含原始 SQL
- 位置：`src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs` 使用 `Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM ...")`。
- 风险：在特定部署中可能受权限/对象名影响；建议替换为轻量 EF 检测或限定仅管理员可见的详细检查。

9) 遗留扩展未清理
- 位置：`Extensions/Application/MiddlewareConfigurationExtensions.cs` 等与统一版并存；`ServiceCollection/AuthenticationExtensions.cs` 已标记 Obsolete 但仍在仓库。
- 风险：新同事易误用旧扩展；建议归档/移除或在 README 标注“勿用”。

—

## 架构现状亮点

- 分层清晰：WebAPI 控制器经由 Modules（业务/查询/仓储）访问 Infrastructure/Entities；`[Authorize]` 覆盖业务控制器，`AuthController` 与 `HealthController` 合理放开。
- 统一异常：`AddProblemDetails` + `GlobalExceptionHandler` 一致返回 ProblemDetails，控制器基类提供统一响应封装。
- 配置治理：生产前置校验（连接串、JWT 长度、密码策略、敏感日志开关）到位。
- 测试护栏：`tests/Architecture/ArchTests.cs` 约束跨层依赖、路由前缀与禁用框架，契合治理目标。

—

## 修复要求计划（不新增业务功能）

优先级定义：
- P0 阻塞性安全/合规风险（需您确认后执行）
- P1 高优先硬化（默认无外部可见变更或可灰度开关）
- P2 清理与一致性（无行为变化）

P0（需确认）
- 生产禁用 Swagger 或加保护
  - 变更：在 `ConfigureSwaggerMiddleware` 中仅在 `!app.Environment.IsProduction()` 启用；或新增仅管理员可见的鉴权策略。
  - 影响：生产访问 `/swagger` 行为变化。
  - 验收：生产环境访问 Swagger 返回 404/403；非生产仍可用。
- 接入速率限制（登录/通用）
  - 变更：`services.AddRateLimiter(...)` + `app.UseRateLimiter()`，将 `Security.RateLimit` 规则映射到命名策略；对 `POST /api/v1/auth/login` 设置更严格规则。
  - 影响：异常高频请求将被 429；需提供白名单/自检绕过策略。
  - 验收：基准/压力测试验证 429 行为与日志观测。

P1
- 应用安全响应头策略
  - 变更：在统一中间件中读取 `SecurityOptions.SecurityHeaders`，设置 `CSP/X-Frame-Options/X-Content-Type-Options/Referrer-Policy/Permissions-Policy`。
  - 影响：仅 HTTP 头部变化；与现有配置一致。
  - 验收：生产响应头含期望字段，CSP 有效。
- 启用已注册的性能中间件
  - 变更：在 `ConfigureAllMiddleware` 中调用 `app.UsePerformanceOptimizations()`；如需可通过 `WebApiOptions.Performance` 开关控制。
  - 影响：压缩/缓存生效；潜在带宽与吞吐改善。
  - 验收：响应头含 `content-encoding`；缓存命中率可观测。
- 收敛配置校验入口
  - 变更：统一使用 `Infrastructure.Configuration.Extensions.EnvironmentAwareValidation`，移除 `UnifiedServiceRegistration` 内部私有重复实现。
  - 影响：装配路径更单一，无行为变化。
  - 验收：生产启动校验仍覆盖既有规则。
- JSON 编码回退为默认（可开关）
  - 变更：将 `UnsafeRelaxedJsonEscaping` 改为默认编码；如有兼容需求以 `WebApiOptions` 增加显式开关。
  - 影响：极少数包含特殊字符的历史客户端需评估；默认更安全。
  - 验收：关键接口回归通过，XSS 犯错面降低。

P2
- 移除/归档遗留扩展
  - 变更：删除或 `Obsolete(error: true)` 标注未使用扩展：`Extensions/Application/MiddlewareConfigurationExtensions.cs`、`Extensions/ServiceCollection/AuthenticationExtensions.cs`。
  - 影响：减少误用；不改行为。
  - 验收：全量编译通过；无引用处。
- 明确 CORS 空实现
  - 变更：在 `RegisterCorsServices` 处加入注释并返回；或删除调用。
  - 影响：认知一致；不改行为。
- 健康检查优化
  - 变更：将原始 SQL 改为轻量 EF/可选关闭详细子检查；保持 GET /details 仅非生产或受鉴权。
  - 影响：更稳健；行为可通过环境开关控制。

—

## 变更位置建议（参考）

- 中间件装配：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`
- 性能接线：`src/Server/Services/LYBT.WebAPI/Extensions/PerformanceOptimization.cs`
- 安全头策略读取：`src/Server/Core/LYBT.Infrastructure/Configuration/Options/SecurityOptions.cs` + 统一装配层
- 速率限制：统一注册层 `UnifiedServiceRegistration` + Program/中间件
- JSON 编码：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs`
- 健康检查：`src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`
- 遗留扩展清理：`src/Server/Services/LYBT.WebAPI/Extensions/Application/*`、`src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollection/AuthenticationExtensions.cs`

—

## 回归与风险评估

- Swagger 生产禁用/受限：外部可见行为变化，需与运维/测试确认窗口与访问路径替代方案（如内网文档）。
- 速率限制：需考虑网关/反代后的真实 IP（信任代理）与白名单；登录页面需要良好提示与日志审计。
- 压缩/缓存：与下游代理缓存策略协调；对实时性要求高的接口应打标签禁用缓存。
- JSON 编码：如历史客户端依赖“放宽转义”，需灰度开关并提供迁移指引。

—

## 验收标准（抽样）

- 生产环境：
  - 访问 `/swagger` 返回 404/403；响应头含 CSP/X-Frame-Options 等。
  - 压缩开启（`content-encoding: br/gzip`），速率限制在压力场景下返回 429 并记录日志。
- 开发/测试环境：
  - Swagger 可用；健康检查 details 可访问。
  - 架构测试（`tests/Architecture/`）保持通过；单元/集成测试无新增失败。

—

## 需要确认的决策点

- 是否在生产完全禁用 Swagger，或仅限管理员访问？
- 速率限制的具体策略（通用/登录/按 IP 或 Token），以及白名单范围？
- JSON 编码是否需要临时兼容开关？默认是否回退安全编码？

—

报告到此。如需我按上述计划提交对应变更（以最小影响、开关控制为原则），请确认 P0 项的具体策略；其余 P1/P2 我可直接按建议执行并提交。

