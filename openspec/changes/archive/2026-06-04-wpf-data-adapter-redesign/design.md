## Context

当前 WPF 桌面端数据对接层存在以下问题：

1. **两套 HTTP 客户端**：Remote 模式使用 **Refit**（接口 + 属性驱动），LocalWebAPI 模式使用**原生 HttpClient**（手动序列化/反序列化），代码风格不统一
2. **HttpXxxRepository 重复代码**：LocalWebAPI 的每个 HttpXxxRepository 都重复编写 HttpClient 调用、JSON 序列化、错误处理逻辑
3. **HttpClient 生命周期不一致**：Remote 模式通过 Refit 的 `RestService.For<T>()` 管理 HttpClient，LocalWebAPI 模式在每个 HttpXxxRepository 构造函数中手动 new
4. **Token 注入分散**：Refit 通过 `DelegatingHandler` 注入，LocalWebAPI 通过 `HttpRequestMessage.Headers.Add()` 手动添加
5. **错误处理不统一**：Refit 抛 `ApiException`，HttpClient 返回 `HttpResponseMessage`，错误处理逻辑分散在各 Repository 中

当前架构（来自 `docs/03-architecture/dual-mode.md`）：
```
Remote:  ViewModel → Repository (Refit) → Server WebAPI → DB
Local:   ViewModel → HttpXxxRepository (HttpClient) → Embedded Kestrel → DB
```

## Goals / Non-Goals

**Goals:**
- 创建 `IApiClient` 统一接口，封装所有业务 API 的操作
- 引入 `IHttpClientFactory` 统一管理 HttpClient 生命周期
- 消除 HttpXxxRepository 中的重复代码，统一为通过 IApiClient 调用
- 统一 JWT Token 注入到 DelegatingHandler 管道
- 统一错误处理为 `ServiceResult<T>` 模式
- 通过 `appsettings.json` 配置驱动模式切换，不改变 Repository 层代码

**Non-Goals:**
- 不修改 Server WebAPI 或 LocalWebAPI 的控制器逻辑
- 不改变 Repository 接口定义（`IPatientRepository` 等保持不变）
- 不涉及数据同步（Sync）模块的重构
- 不改变 ViewModel 层代码

## Decisions

### Decision 1: IApiClient 按业务模块拆分接口

**选择**：`IApiClient` 作为顶层聚合接口，内部按业务模块拆分为子接口（`IApiClient.Patients`, `IApiClient.Herbs` 等）

**替代方案**：
- ❌ 单一巨型接口：所有端点在一个接口中，违反接口隔离原则
- ❌ 不拆分（保持现状）：继续 Refit 接口和 HttpClient 分离

**理由**：按业务模块拆分子接口，每个子接口对应一个业务领域，与 Repository 接口一一对应，便于理解和维护。

### Decision 2: Refit 实现 + HttpClient 实现共享 IApiClient 接口

**选择**：Remote 模式使用 **Refit** 实现 IApiClient 的各子接口（通过 `[Get]`, `[Post]` 等属性），LocalWebAPI 模式使用原生 **HttpClient Wrapper** 实现相同接口

**替代方案**：
- ❌ 全部改为 Refit：LocalWebAPI 也可以使用 Refit，但增加了不必要的依赖
- ❌ 全部改为原生 HttpClient：失去 Refit 的类型安全和声明式 API 优势

**理由**：Refit 在 Remote 模式上已经稳定运行，无需替换。LocalWebAPI 使用 HttpClient Wrapper 更轻量，且两边共享相同的 `IApiClient` 接口，实现"接口统一、实现可插拔"。

### Decision 3: IHttpClientFactory 统一管理

**选择**：两种模式都通过 `IHttpClientFactory` 创建和管理 HttpClient，通过 Typed Client 或 Named Client 模式注入

**替代方案**：
- ❌ 继续手动 new HttpClient：端口耗尽、生命周期问题
- ❌ 全局静态 HttpClient：无法针对不同模式配置不同的 BaseAddress

**理由**：`IHttpClientFactory` 是 .NET 推荐的最佳实践，自动处理连接池管理、DNS 刷新、配置流水线。

### Decision 4: DelegatingHandler 管道统一注入 Token

**选择**：创建 `AuthDelegatingHandler`，在 HttpClient 管道中自动添加 JWT Bearer Token

**替代方案**：
- ❌ 在 Repository 层手动添加 Header：代码分散，容易遗漏
- ❌ 在 IApiClient 实现中手动添加：和 DelegatingHandler 逻辑重复

**理由**：DelegatingHandler 是 ASP.NET Core 标准管道模式，一次配置全局生效。

### Decision 5: 配置驱动模式切换

**选择**：在 `appsettings.json` 中增加 `ApiMode` 配置（`"Remote"` 或 `"LocalWebAPI"`），DI 在启动时根据配置注册对应的 IApiClient 实现

**替代方案**：
- ❌ 运行时切换：复杂度高，需要监听配置变更并重建 HttpClient
- ❌ 编译时切换（条件编译）：无法在同一构建产物中支持两种模式

**理由**：启动时根据配置一次性注册，简单可靠。如需切换模式，重启应用即可（符合当前设计 SYNC-D03）。

## Risks / Trade-offs

- **[风险]** Refit 和 HttpClient Wrapper 的实现差异可能导致行为不一致 → **缓解**：通过集成测试覆盖两种模式的相同场景
- **[风险]** DelegatingHandler 的 Token 刷新逻辑复杂度增加 → **缓解**：复用现有的 `ITokenRefreshService`，仅将注入点从 Repository 层上移到 Handler
- **[风险]** IApiClient 子接口数量多（7+ 业务模块），接口定义工作量较大 → **缓解**：从现有 Refit 接口（`Contracts/IApi` 目录）迁移，增量重构
- **[权衡]** 增加了抽象层级的复杂度（IApiClient → 子接口 → 实现）→ **收益**：消除了两套客户端的维护成本，统一了错误处理和认证流程

## Migration Plan

1. 创建 `IApiClient` 接口及子接口定义
2. 创建 Refit 实现（迁移现有 Refit 接口到 IApiClient）
3. 创建 LocalWebAPI HttpClient Wrapper 实现
4. 创建 `AuthDelegatingHandler` 和 `IHttpClientFactory` 配置
5. 重构所有 Remote Repository 从直接依赖 Refit 接口改为依赖 IApiClient
6. 重构所有 HttpXxxRepository 从手动 HttpClient 改为依赖 IApiClient
7. 更新 DI 注册（`DataSourceRegistrationExtensions`），根据 `ApiMode` 配置注册
8. 删除旧的独立 Refit 接口依赖和散落的 HttpClient 管理代码
9. 运行完整测试套件验证两种模式

## Resolved Questions

### Q1: IApiClient 子接口放哪里？

**决定：`LYBT.Desktop.Contracts/ApiClient/`**

- `Contracts` 项目的职责就是接口定义（IApi、IRepository、IService），IApiClient 是 IApi 的上层抽象，天然属于这里
- 避免项目数量膨胀（当前已有 16 个项目），独立项目没有实际隔离价值
- 子接口直接替换现有 `Contracts/IApi/` 中的 Refit 接口，迁移路径最短

```
LYBT.Desktop.Contracts/
├── IApi/                         ← 旧（迁移完成后删除）
├── ApiClient/                    ← 新
│   ├── IApiClient.cs
│   ├── IAuthApi.cs
│   ├── IPatientApi.cs
│   └── ...
└── Repositories/
```

### Q2: LocalWebAPI 是否需要 Token 刷新？

**决定：不需要，DelegatingHandler 做分支处理**

- TBD-01 已明确：本地模式使用 1 年有效期 Token，不支持 Refresh Token 轮换
- `LocalJwtConfig` 签发的 Token 在 1 年内可无限使用，实际场景中不可能过期
- DelegatingHandler 通过 `IApiModeProvider` 判断当前模式，LocalWebAPI 下收到 401 直接触发重新登录而非刷新
- Remote 模式保持原有刷新逻辑不变

```csharp
// AuthDelegatingHandler 分支逻辑
if (_modeProvider.Mode == ApiMode.LocalWebAPI)
    return await HandleLocalAuthFailure(response);  // 直接重新登录
return await HandleRemoteAuthFailure(response);      // 尝试刷新 → 失败再重新登录
```

### Q3: ApiMode 是否需要 UI 切换入口？

**决定：不需要切换入口，加状态栏模式指示器**

- SYNC-D03 已移除运行时模式切换，这是有意的架构简化
- 运行时切换涉及 HttpClient 重建、DI 变更、Kestrel 启停，复杂度极高
- 用户不会频繁切换模式（诊所要么联网用远程，要么断网用本地）
- 在主窗口状态栏添加只读模式指示器（`[● 远程模式]` / `[● 本地模式]`），方便确认当前状态
