# 服务器硬化问题清单与解决方案（凌隐宝堂中医诊所 / LYBT.Server.sln）

- 日期：2025-09-21
- 适用范围：`LYBT.Server.sln`（WebAPI、Core、Modules、Shared）
- 关联文档：
  - 初始报告：`docs/governance/server-hardening-report-20250921.md`
  - 复核报告：`docs/governance/server-hardening-recheck-20250921.md`

——

## 一、问题清单与现状

1) 生产环境暴露 Swagger
- 表现：在所有环境启用 `UseSwagger/UseSwaggerUI` 导致生产可被扫描
- 影响：潜在信息泄露、接口枚举
- 位置：
  - 实际管线：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`
  - 遗留扩展：`src/Server/Services/LYBT.WebAPI/Extensions/Application/MiddlewareConfigurationExtensions.cs`
  - 版本化示例：`src/Server/Services/LYBT.WebAPI/Extensions/ApiVersioningConfiguration.cs`
- 状态：已修复（生产禁用；仅非生产启用）

2) 安全响应头未接线
- 表现：虽有 `SecurityOptions.SecurityHeaders`，但未落地到响应
- 影响：CSP、XFO、CTO、Referrer、Permissions 等策略未生效
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`
- 状态：已修复（从配置读取并写入响应头）

3) 性能中间件已注册未启用
- 表现：`AddResponseCompression/ResponseCaching/OutputCache` 未在管道启用
- 影响：吞吐与带宽优化未生效
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`
- 状态：已修复（`UsePerformanceOptimizations()` 已接入）

4) 速率限制未接线
- 表现：有安全选项与约束需要，但无全局/端点接线
- 影响：撞库、暴力破解与异常高频请求缺乏防护
- 位置：
  - 注册：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs`
  - 管道：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`
  - 登录端点：`src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- 状态：已修复（全局 + 登录策略已落地；默认值适配≤20人内部使用）

5) JSON 编码默认“放宽”
- 表现：使用 `UnsafeRelaxedJsonEscaping`，在少数场景扩大 XSS 风险面
- 影响：若 JSON 被直接内联到 HTML，安全性下降
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs`
- 状态：已修复（默认安全编码；提供 `WebApiOptions:Json:UnsafeRelaxedEscaping` 开关）

6) 配置校验路径重复
- 表现：基础设施扩展与 WebAPI 内部都做“生产配置强校验”
- 影响：校验规则可能分叉、维护成本提升
- 位置：
  - 基础设施：`src/Server/Core/LYBT.Infrastructure/Configuration/Extensions/EnvironmentAwareValidation.cs`
  - WebAPI 本地：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs`
- 状态：待处理（建议统一入口）

7) 健康检查内含原始 SQL
- 表现：`HealthController` 使用 `SqlQueryRaw` 查询 Users/Patients 计数
- 影响：在差异化环境中可能脆弱；details 过多内部信息
- 位置：`src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`
- 状态：待处理（建议收敛或加鉴权/限环境）

8) CORS 占位存在但未用
- 表现：存在 `RegisterCorsServices` 与调用，但方法体为空
- 影响：造成“似乎已启用 CORS”的误解
- 位置：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs`
- 状态：待处理（建议移除或加配置开关）

9) 遗留扩展未清理
- 表现：示例/旧扩展与统一实现并存，容易误用
- 位置：
  - `src/Server/Services/LYBT.WebAPI/Extensions/Application/MiddlewareConfigurationExtensions.cs`
  - `src/Server/Services/LYBT.WebAPI/Extensions/ApiVersioningConfiguration.cs`
- 状态：待处理（建议 `[Obsolete(error: true)]` 或移除）

10) 品牌与命名统一
- 表现：对外文案已统一为“凌隐宝堂中医诊所”，但 JWT Issuer/Audience 仍为 `LYBT/LYBT-Client`
- 影响：改动 Issuer/Audience 将影响现有 Token 校验
- 位置：配置项 `JwtOptions`
- 状态：可选（按需要安排迁移窗口）

——

## 二、已落地的修复方案（摘要）

- 生产禁用 Swagger（仅非生产启用）
  - 统一中间件：`UnifiedMiddlewareConfiguration.ConfigureSwaggerMiddleware()`

- 安全响应头按配置应用
  - 统一中间件：`SecurityHeadersMiddleware.ConfigureSecurityHeadersFromOptions()`
  - 读取 `SecurityOptions.SecurityHeaders` 并设置响应头

- 性能中间件启用
  - 管道启用：`app.UsePerformanceOptimizations()`（压缩/响应缓存/输出缓存）

- 速率限制接线（内部≤20人）
  - 全局：每用户 Token 120/分钟（未登录按 IP），队列 60
  - 登录：每 IP 30/分钟（内网 1000/分钟），队列 20/200
  - 接线：`ConfigureRateLimiting()` + `[EnableRateLimiting("Login")]`

- JSON 编码默认安全
  - 默认 `JavaScriptEncoder.Default`
  - 开关：`WebApiOptions:Json:UnsafeRelaxedEscaping`（默认 false）

——

## 三、剩余问题的解决方案设计

1) 统一配置校验入口（P1）
- 目标：仅保留基础设施侧 `EnvironmentAwareValidation` 的一处校验入口
- 方案：
  - 删除/迁移 WebAPI 内部的 `ProductionConfigValidationFilter` 与私有 `AddEnvironmentAwareValidation`；
  - 在 WebAPI 只调用基础设施扩展（单一入口）。
- 验收：生产启动时若缺少关键配置，能 fail-fast；两个实现不再并存。

2) 健康检查精简或限权（P2）
- 目标：降低脆弱性与信息暴露
- 方案：
  - 将 `SqlQueryRaw` 检查改为 `Database.CanConnectAsync()` + 迁移状态；
  - `details` 接口仅对管理员或非生产开放（加 `[Authorize(Policy="AdminPolicy")]` 或环境判断）。
- 验收：功能等效或更稳健，生产环境无多余内部信息泄露。

3) CORS 显式化（P2）
- 目标：消除误导，保持行为清晰
- 方案（二选一）：
  - 彻底移除 `RegisterCorsServices` 调用与方法；
  - 仅在存在 `Security.Cors` 配置段时启用并提示日志。
- 验收：默认无跨域；需要时可按配置显式启用。

4) 清理遗留扩展（P2）
- 目标：避免误用旧扩展导致生产暴露或重复配置
- 方案：
  - 对示例/旧扩展添加 `[Obsolete("示例/勿用于生产", true)]`；
  - 或直接移除该文件；
  - 在 README 标明统一入口：`UnifiedMiddlewareConfiguration` / `UnifiedServiceRegistration`。
- 验收：构建失败能快速提示误用；无重复装配路径。

5) 品牌彻底统一（可选）
- 目标：将 Issuer/Audience 统一为“凌隐宝堂/凌隐宝堂客户端”等
- 方案：
  - 规划运维窗口，先在客户端配置可接受多 Issuer/Audience；
  - 服务器侧并行接受新旧 Issuer/Audience 一段时间；
  - 全量切换后移除旧值。
- 验收：切换过程中 Token 验证不中断；日志可见迁移成功率。

——

## 四、任务排期与责任建议

- P1（统一配置校验入口）
  - 工作量：0.5 天
  - 负责人：后端负责人

- P2（健康检查、CORS 显式化、遗留扩展清理）
  - 工作量：1 天
  - 负责人：后端负责人

- 可选（品牌统一 Issuer/Audience）
  - 工作量：0.5–1 天（含联调）
  - 负责人：后端 + 客户端

——

## 五、验证与回归

- 构建：`dotnet build LYBT.Server.sln -c Release`（0 错误）
- 运行（开发）：`dotnet run --project src/Server/Services/LYBT.WebAPI`
- 验证项：
  - 非生产可访问 `/swagger`；生产 404/403
  - 响应头包含 CSP/XFO/CTO/Referrer/Permissions-Policy
  - 速率限制触发 429 并记录日志
  - JSON 默认安全编码；开关放宽时可见差异

> 如需我继续落地 P1/P2 的“统一与清理”改动，可直接确认，我将按最小影响提交。

