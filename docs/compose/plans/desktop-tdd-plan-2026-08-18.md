# Desktop TDD 逐层完善计划（L4 → L0）

## 策略

基于蓝图 §3.5 Desktop 分层规则，从 HTTP Client 层（L4）开始，逐层向上完善测试覆盖。每层遵循 **RED → GREEN → REFACTOR** TDD 循环。

**基准文档**：
- `docs/compose/reports/desktop-l4-layer-analysis-2026-08-18.md`（L4 层分析）
- `docs/03-architecture/14-structure-design-blueprint.md` §3.5（Desktop 分层规则）
- `docs/03-architecture/05-dual-mode.md`（双模式架构）

## 层级定义

| 层 | 职责 | 测试类型 |
|----|------|---------|
| **L4** | HTTP Client 基础设施 | Unit（Mock HttpClient） |
| **L3** | SwitchingApiClient 路由 | Unit（Mock IApiClient） |
| **L2** | Repository HTTP 代理 | Unit（Mock IApiClient） |
| **L1** | Service 业务逻辑 | Unit（Mock Repository） |
| **L0** | ViewModel + View | Unit（Mock Service）+ 手动 UI 验证 |

## Phase 1: L4 层测试（当前优先）

### Batch 1: HttpApiClientBase（基础 HTTP 操作）
**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpApiClientBase.cs`
**测试**：`tests/LYBT.Tests.Desktop/Unit/Http/HttpApiClientBaseTests.cs`

| 测试用例 | 类型 |
|---------|------|
| SendAsync GET 返回 2xx 时正常响应 | Happy path |
| SendAsync POST 带 body 正确发送 JSON | Happy path |
| SendAsync PUT/DELETE 正常工作 | Happy path |
| SendAsync 非 2xx 抛 HttpRequestException | Error |
| SendAsync 不支持的 HTTP 方法抛 ArgumentException | Error |
| DeserializeEnvelopeAsync 信封格式正确解包 | Happy path |
| DeserializeEnvelopeAsync 裸 T 格式兼容 | Edge case |
| DeserializeEnvelopeAsync 空 JSON 返回空 ApiResponse | Edge case |
| BuildPagedUrl 基础分页 | Happy path |
| BuildPagedUrl 带过滤参数 | Happy path |
| BuildPagedUrl 空过滤参数跳过 | Edge case |
| ToJsonContent 生成正确 Content-Type | Happy path |

### Batch 2: AuthorizationMessageHandler（Token 注入）
**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/AuthorizationMessageHandler.cs`
**测试**：`tests/LYBT.Tests.Desktop/Unit/Http/AuthorizationMessageHandlerTests.cs`

| 测试用例 | 类型 |
|---------|------|
| 有 Token 时注入 Authorization header | Happy path |
| 无 Token 时不注入 header | Edge case |
| 匿名端点 /health 跳过 Token 检查 | Happy path |
| 匿名端点 /api/v1/auth/login 跳过 Token | Happy path |
| 匿名端点 /api/v1/auth/refresh 跳过 Token | Happy path |
| 非匿名端点正确注入 Token | Happy path |
| Token 存储抛异常时传播异常 | Error |

### Batch 3: TokenRefreshHandler（自动刷新）
**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/TokenRefreshHandler.cs`
**测试**：`tests/LYBT.Tests.Desktop/Unit/Http/TokenRefreshHandlerTests.cs`

| 测试用例 | 类型 |
|---------|------|
| Token 未过期时放行请求 | Happy path |
| Token 即将过期（<5min）触发刷新 | Happy path |
| 刷新成功后更新 Token 存储 | Happy path |
| 用户不活跃时跳过刷新 | Business rule |
| 并发请求不重复刷新（SemaphoreSlim） | Concurrency |
| 刷新失败重试 3 次（指数退避） | Error recovery |
| 刷新失败时发布 EventAggregator 事件 | Integration |
| RefreshToken 不存在时返回 NotLoggedIn | Error |
| AutoLogin 降级触发条件 | Business rule |
| AutoLogin 成功后返回新 Token | Happy path |
| AutoLogin 失败时清除凭据 | Error recovery |

### Batch 4: SwitchingApiClient（路由）
**文件**：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/SwitchingApiClient.cs`
**测试**：`tests/LYBT.Tests.Desktop/Unit/Http/SwitchingApiClientTests.cs`

| 测试用例 | 类型 |
|---------|------|
| IsLocal URL 返回 HttpClientApiClient | Happy path |
| 非 IsLocal URL 返回 RefitApiClient | Happy path |
| URL 变更后销毁旧 client、创建新 client | Lifecycle |
| Thread-safe 并发访问（双重检查锁定） | Concurrency |
| Dispose 正确清理当前 client | Lifecycle |

### Batch 5: 集成验证
**测试**：`tests/LYBT.Tests.Desktop/Integration/Http/L4IntegrationTests.cs`

| 测试用例 | 类型 |
|---------|------|
| Handler 链正确组装顺序 | Integration |
| LocalWebAPI 端到端（连接 + 认证 + CRUD） | E2E |
| Remote WebAPI 端到端（连接 + 认证 + CRUD） | E2E |

## Phase 2: L3-L0 层测试（后续）

| 层 | 批次 | 范围 |
|----|------|------|
| L3 SwitchingApiClient | 已含在 L4 Batch 4 | — |
| L2 Repository | Batch 6-7 | Patient/Herb/Formula/MedicalCase/Registration/User Repository |
| L1 Service | Batch 8-9 | AuthService/ConnectionModeService/TokenLifecycleService |
| L0 ViewModel | Batch 10-11 | LoginViewModel/ConnectionStatusViewModel |

## 执行顺序

```
1. 写测试（RED）→ 预期当前代码行为（或暴露 bug）
2. 修复/完善代码（GREEN）→ 测试通过
3. 重构代码 → 测试仍通过
4. 提交 + 推送
5. 进入下一批次
```

## 约束

- 每批次独立提交，不跨批次修改
- 测试必须使用 Mock/Stub，不依赖真实 WebAPI（Unit 测试）
- 集成测试需要 WebAPI 运行（单独运行）
- 测试放在 `tests/LYBT.Tests.Desktop/Unit/Http/` 目录
- 代码修改委派 pi 执行
