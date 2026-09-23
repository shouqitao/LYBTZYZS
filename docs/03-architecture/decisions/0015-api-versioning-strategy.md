# ADR-0015: API 版本控制策略
> 版本: v1.1 | 日期: 2026-08-20（v1.1 2026-09-24：新增「v2 切换清单」——F-07 版本化准备）

**状态**: Accepted
**日期**: 2026-06-28
**来源**: 01-system-overview.md API 版本控制策略节

## 背景

系统提供 RESTful API 供 Desktop 客户端和未来 Web 客户端调用。随着功能迭代和客户端版本分化，需要明确的 API 版本控制策略以保证向后兼容性。

## 决策

采用 **URL Path Versioning** (`/api/v{version}/`) 作为 API 版本化方案。

### 核心规则

| 规则 | 约束 | 说明 |
|------|------|------|
| 版本化方式 | URL Path | `/api/v1/` 前缀，`[ApiVersion("1")]` |
| 版本号格式 | 整数递增 | v1 → v2 → v3，无 v1.1 等子版本 |
| 触发条件 | 破坏性变更 | 删除/重命名端点、改变必需字段、改变语义 |
| 共存期 | ≥6 个月 | v1 和 v2 同时可用，客户端逐步迁移 |
| 弃用流程 | 标记 → 过渡 → 移除 | 先 `[Obsolete]` + 6 个月过渡期，再物理删除 |
| 新端点 | 允许在当前版本 | 非破坏性新功能直接加到当前版本，不递增版本号 |

### 弃用端点处理

1. 代码中标记 `[Obsolete("Use /api/v{new}/... instead. Deprecated since YYYY-MM-DD")]`
2. 文档中保留端点说明，标注 `[Deprecated]` + 替代方案
3. 监控弃用端点调用频率（通过审计日志）
4. 6 个月过渡期后移除

### 本地模式版本同步

本地 LocalWebAPI 与远程 WebAPI 使用相同版本号，但本地模式端点是远程的子集（约 80% 覆盖率）。版本变更时：
- 两端同步更新到相同版本
- 本地模式缺失的端点（如 Sync 相关）不纳入本地 API 契约

## 理由

- **简单性**: URL Path 是最直观的版本化方式，客户端和开发者都能一眼识别版本
- **兼容性**: 多版本共存期保证客户端平滑迁移
- **可测试性**: 每个版本可独立测试，不受其他版本影响
- **文档友好**: API 参考文档按版本组织，易于维护

## 后果

- URL 路径略长（`/api/v1/auth/login` vs `/api/auth/login`）
- 需要维护多版本端点代码（共存期内）
- 本地模式需与远程模式保持版本同步（由 ADR-0010 保障）

## v2 切换清单（F-07，2026-09-24 准备就绪）

**触发条件**：出现破坏性变更（删除/重命名端点、改变必需字段或语义）。非破坏性新功能直接加到 v1，不递增版本号。

**服务端（已准备，切换时按此执行）**

1. **版本常量**：`src/Server/Core/LYBT.Infrastructure/Constants/ApiVersionConstants.cs` 已预留 `V2 = "2"`（保留值——当前无任何控制器声明）。
2. **仅改受影响的控制器**：给发生破坏性变更的控制器加 `[ApiVersion("2")]`，并让路由同时匹配两个版本（`[ApiVersion("1")]` + `[ApiVersion("2")]`，路由保持 `api/v{version:apiVersion}/…`；v2 语义变化通过 action 内按 `RequestedApiVersion` 分流或拆分为两个 action）。未受影响的控制器继续只声明 v1。
3. **弃用流程**：v1 端点标 `[Obsolete("Use /api/v2/... instead. Deprecated since YYYY-MM-DD")]`，保留 ≥6 个月过渡期后再物理删除。
4. **Swagger 无需改动**：文档与 SwaggerUI 端点由 `IApiVersionDescriptionProvider` 驱动（`ConfigureSwaggerOptions` + `UnifiedMiddlewareConfiguration.ConfigureSwaggerMiddleware`）——发现到 v2 后自动多出 `/swagger/v2/swagger.json` 与对应 UI 端点。
5. **守卫测试同步**：`tests/LYBT.Tests.Server/Unit/WebAPI/ApiVersioningGuardTests.cs` 的 `DeclaredApiVersions_ComeFromApiVersionConstants` 断言「V2 尚未被任何控制器声明」——切 v2 时该断言会失败，属**有意触发**：更新该断言即代表 v2 正式启用（同时更新本清单状态）。
6. **本地模式同步**（ADR-0010）：LocalWebAPI 控制器路由与远程同版本号，缺失端点不纳入本地契约。

**客户端面（切换时需同步，本次未改动客户端代码）**

| 位置 | 现状 | v2 切换时 |
|------|------|-----------|
| `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/*.cs` | **12 个 Refit 接口**硬编码 `/api/v1/...`（IAuthApi、IBackupApi、IConfigurationApi、IDeployApi、IDiagnosticsApi、IFormulaApi、IHerbApi、IMedicalCaseApi、IPatientApi、IRegistrationApi、IReportsApi、IUserApi） | 受影响端点的路由字符串改为 v2（或按接口拆 v1/v2 两套） |
| `Foundation/Http/AuthorizationMessageHandler.cs`（匿名端点列表） | 含 `/api/v1/auth/login`、`/api/v1/auth/refresh` | 追加 v2 匿名路径，否则 v2 登录请求会被附加 Token 逻辑影响 |
| `Foundation/Http/CachingHttpMessageHandler.cs`（缓存键前缀 `"/api/v1/"`，L157 附近） | 仅 v1 路径参与缓存 | 前缀改为按版本推导，否则 v2 GET 请求不被缓存/失效 |
| `Foundation/Caching/DesktopCacheManager.cs` + `DesktopCacheKeyRegistry.cs` | 失效前缀 `GET:/api/v1/{domain}` | 同步支持 v2 前缀 |
| `Foundation/HealthCheck/ApiHealthCheckService.cs`（健康检查 URL） | `{baseUrl}/api/v1/health` | 保留 v1（健康检查非破坏性，v1 共存期内无需改） |
| `Foundation/Http/TokenRefreshHandler.cs` + `Clients/IdentityHttpApiClient.cs` | 刷新走 `/api/v1/auth/refresh` | 与 IAuthApi 同步改 v2（若认证契约变更） |
| LocalWebAPI 控制器（14 个，`[Route("api/v1/...")]`） | 与远程同版本 | 与远程同步（ADR-0010） |

**明确不做的事（设计决策）**

- **不提供 header/query/媒体类型版本协商**：`ApiVersionReader` 只有 `UrlSegmentApiVersionReader`（`ApiServiceCollectionExtensions.RegisterApiServices`），版本只能来自 URL 路径段。
- **不加版本协商中间件**：版本完全由 URL 路径段决定，不引入「未指定版本 → 静默改选默认版本」的兜底路由。路由模板本身要求 `v{n}` 段存在（`api/v{version:apiVersion}/…`），因此裸 `/api/x` 不匹配任何版本化路由。
- `AssumeDefaultVersionWhenUnspecified = true` 保留框架默认语义（服务于版本中立端点与 `CreatedAtAction` 链接生成等边界情形），**不构成对裸路径的兜底**。
- 因此**不存在** `Api-Version` 头、`api-version` 查询参数等入口；任何引入它们的改动都违反本 ADR。

## 关联

- [ADR-0009: URL 驱动双模式](0009-url-driven-dual-mode.md) — SwitchingApiClient 依赖版本化 URL
- [ADR-0010: LocalWebAPI 统一服务层](0010-localwebapi-unified-service-layer.md) — 本地/远程 API 契约同步
- [04-api-reference/](../../04-api-reference/README.md) — API 端点文档，按版本组织

## 关联 US

- 全部 141 个 US 的 API 端点均受此版本控制策略约束
