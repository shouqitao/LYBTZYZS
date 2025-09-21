# 服务器硬化复核报告（凌隐宝堂中医诊所 / LYBT.Server.sln）

- 日期：2025-09-21
- 依据：docs/governance/server-hardening-report-20250921.md（上轮治理报告）
- 复核范围：WebAPI 装配、配置/选项、安全头、速率限制、Swagger 暴露、JSON 编码、CORS、健康检查、遗留扩展

—

## 复核结论

- P0 风险已消除：
  - 生产禁用 Swagger：已在统一中间件中按环境启用，仅非生产可用。
  - 安全响应头：已按 SecurityOptions 应用（CSP/Frame/CTO/Referrer/Permissions-Policy）。
  - 性能中间件：已注册并在管道中启用（压缩/响应缓存/输出缓存）。
  - 速率限制：已接线（全局 + 登录端点策略），满足内部≤20人并发使用场景。
  - JSON 编码：默认使用安全编码（可经 WebApiOptions.Json.UnsafeRelaxedEscaping 开关放宽）。

- P1/P2 建议性问题仍存在但不阻塞：
  - 复核保留项见“仍存在的问题与建议”。

—

## 变更核对（与上轮报告对照）

- 生产 Swagger 暴露（已解决）
  - 位置：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:60
  - 行为：仅在非生产调用 `UseSwagger/UseSwaggerUI`。

- 安全响应头未应用（已解决）
  - 位置：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:110
  - 行为：从 `SecurityOptions.SecurityHeaders` 读取并写入响应头。

- 性能中间件未启用（已解决）
  - 位置：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:45
  - 行为：`app.UsePerformanceOptimizations()` 已接入管道。

- 速率限制未接线（已解决）
  - 注册：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs:282
  - 中间件：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs:42
  - 登录端点标注：src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs:26（`[EnableRateLimiting("Login")]`）
  - 默认策略：
    - 全局：每用户 Token 120/分钟（未登录按 IP），队列 60
    - 登录：每 IP 30/分钟（内网 1000/分钟），队列 20/200

- JSON 编码放宽默认（已修复）
  - 位置：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs:372
  - 行为：默认 `JavaScriptEncoder.Default`；配置 `WebApiOptions:Json:UnsafeRelaxedEscaping=true` 可放宽。

—

## 仍存在的问题与建议

1) 配置校验路径重复（建议统一）
- 位置：
  - 基础设施扩展：src/Server/Core/LYBT.Infrastructure/Configuration/Extensions/EnvironmentAwareValidation.cs
  - WebAPI 本地：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs（`AddEnvironmentAwareValidation(...)` 私有实现 + `ProductionConfigValidationFilter`）
- 风险：两处并存易分散认知，后续规则变更难以一致。
- 建议（P1）：统一使用基础设施扩展，移除 WebAPI 本地私有校验，或将过滤器迁移至基础设施并在此处调用单一入口。

2) 健康检查包含原始 SQL（建议收敛）
- 位置：src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs
- 风险：在特定权限/对象名差异的环境中可能脆弱；过于详细的检查建议仅对管理员或非生产开放。
- 建议（P2）：改为轻量 EF 能力检查，或对 `details` 接口加鉴权/环境限制。

3) CORS 空实现但被调用（建议显式化）
- 位置：src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs（`RegisterCorsServices`）
- 现状：占位实现 + 调用；已在注释中说明无跨域需求。
- 建议（P2）：
  - 要么删除该调用与占位；
  - 要么改为仅在存在 `Security.Cors` 配置时启用（更显式）。

4) 遗留扩展未清理（建议归档/移除）
- 位置：
  - src/Server/Services/LYBT.WebAPI/Extensions/Application/MiddlewareConfigurationExtensions.cs（含无条件安全头/Swagger 示例）
  - src/Server/Services/LYBT.WebAPI/Extensions/ApiVersioningConfiguration.cs（提供 `UseVersionedSwagger()`，内部无条件 `UseSwagger()`；当前未被调用）
- 风险：新同事可能误用旧扩展导致生产暴露 Swagger 或重复配置。
- 建议（P2）：为上述未用扩展添加 `[Obsolete(error: true)]` 标注或移除，减少误用面；若保留示例性质，请显著注释“示例/勿用于生产”。

5) 品牌与命名
- 已将对外文案调整为中文（“凌隐宝堂中医诊所”）。Issuer/Audience 仍使用默认 `LYBT/LYBT-Client`，避免破坏既有 Token 验证。
- 建议（P2）：若需要彻底品牌统一，可在运维窗口内更换 Issuer/Audience 并同步客户端配置；变更需配合 Token 迁移方案。

—

## 建议执行计划（仅非功能性）

- P1（统一配置校验入口）
  - 统一到基础设施扩展；删除 WebAPI 本地私有 `AddEnvironmentAwareValidation` 与 `ProductionConfigValidationFilter`（或迁移至基础设施）。

- P2（清理与一致性）
  - 健康检查精简或加鉴权（仅管理员/非生产可见 details）。
  - CORS 占位显式化（移除调用或加配置开关）。
  - 为遗留扩展加 `[Obsolete(error: true)]` 或移除；保留示例需加粗“勿用于生产”。
  - 如需品牌彻底统一，安排 Issuer/Audience 迁移窗口。

—

## 复核清单（抽样）

- 生产 Swagger：仅非生产可访问（已验证）。
- 响应头：含 CSP/X-Frame-Options/CTO/Referrer/Permissions-Policy（已验证）。
- 压缩/缓存：`content-encoding` 出现于压缩响应；OutputCache 可观测（建议联调验证）。
- 速率限制：登录过频返回 429，全局阈值生效（可用压测验证）。
- JSON 编码：默认安全，若打开放宽开关，需记录审计并限于受控环境。

—

如需，我可以继续按上述 P1/P2 建议提交最小化变更，或将速率限制白名单/阈值参数化到 `appsettings.Security.json` 并提供示例配置。

