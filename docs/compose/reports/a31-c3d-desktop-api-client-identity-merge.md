# A-31-C3d：Desktop IApiClient Identity 侧合并报告

> **任务**：`docs/compose/specs/task-a31-c3d-desktop-api-client-identity-2026-08-09.md`
> **执行**：Mimo Code | **日期**：2026-08-09 | **分支**：master
> **依据**：Server 端 C-3a 已完成（AuthController+UsersController→IdentityController，双路由），Desktop API 客户端需同步对齐

---

## 1. 执行摘要

将 Desktop 侧 `IApiClientAuth`（6 方法）+ `IApiClientUsers`（14 方法）合并为 `IApiClientIdentity`（20 方法），与 Server 端 `IdentityController` 对齐。主接口 `IApiClient` 的 `Auth`/`Users` 属性合并为 `Identity`；Refit 实现 `AuthApiClient`+`UserApiClient`→`IdentityApiClient`，HttpClient 实现 `AuthHttpApiClient`+`UsersHttpApiClient`→`IdentityHttpApiClient`；消费方 `UserRepository` 9 处 + 4 个注入 `IApiClientAuth` 的安全服务同步更新；删除 6 个旧文件。验证：build 0 错误 0 警告、架构测试 86/86、受影响 Desktop 单测 32/32。

| 项 | 内容 | Commit |
|----|------|--------|
| 代码 | 接口合并 + 实现合并 + 消费方更新 + 删旧文件 + 测试同步 | 本次代码 commit |
| 文档 | 本报告 + Contracts README + 03-patterns/05-dual-mode/15-integration-plan + 总账状态同步 | 本次文档 commit |

## 2. 合并执行

**新建文件（3）**：

| 文件 | 职责 |
|------|------|
| `Contracts/ApiClient/IApiClientIdentity.cs` | 20 方法 = Auth 6（Login/AutoLogin/Logout/Refresh/Validate/HealthCheck）+ Users 14（CRUD+Password+Batch+Profile+Local-only）；继承 `IEntityApiSegment<UserListDto,UserDetailDto,UserInputDto>` 并保留 DIM 转发（UserRepository 基类依赖） |
| `Foundation/Http/Clients/IdentityApiClient.cs` | Refit 适配器：包装 `IAuthApi` + `IUserApi`；Local-only 4 方法抛 `NotSupportedException` |
| `Foundation/Http/Clients/IdentityHttpApiClient.cs` | HttpClient 适配器：`/api/v1/auth/*` + `/api/v1/users/*` 路由保持 |

**主接口更新**：`IApiClient.Auth` + `IApiClient.Users` → `IApiClient.Identity`；三实现（`RefitApiClient`/`HttpClientApiClient`/`SwitchingApiClient`）同步。

**消费方更新**：

| 文件 | 改动 |
|------|------|
| `UserRepository.cs` | 9 处 `_apiClient.Users.*` → `_apiClient.Identity.*` + 基类构造参数 `apiClient.Users`→`apiClient.Identity` |
| `AuthenticationService.cs` | 注入 `IApiClientAuth` → `IApiClientIdentity` |
| `LogoutService.cs` | 同上 |
| `TokenLifecycleService.cs` | 同上 |
| `AuthHealthService.cs`（Admin/Sysadmin） | 同上 |
| 测试 `AuthEventPublishingTests`/`LogoutServiceTests`/`HttpClientApiClientEnvelopeTests`/`SwitchingApiClientTests` | `IApiClientAuth`/`.Auth`/`.Users` → `IApiClientIdentity`/`.Identity` |

**删除旧文件（6）**：`IApiClientAuth.cs` + `IApiClientUsers.cs` + `AuthApiClient.cs` + `UserApiClient.cs` + `AuthHttpApiClient.cs` + `UsersHttpApiClient.cs`。

## 3. DI 注册说明

Desktop 容器仅注册 `IApiClient` 单例（`SwitchingApiClient`，`UnifiedApiClientExtensions.AddUnifiedApiClient`），`IApiClientAuth`/`IApiClientUsers` 子接口本身**从未显式注册**——4 个安全服务（AuthenticationService 等）的构造注入在 DryIoc 惰性解析下未触发（登录链路经 `ILoginCoordinator`→`IAuthenticationService` 抽象）。合并后注入 `IApiClientIdentity` 行为等价，无新增注册需求；`UserRepository` 经 `IApiClient.Identity`（基类 `IEntityApiSegment` 契约）解析。

## 4. 验证结果（真实输出）

| 验证项 | 结果 |
|--------|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | **0 错误 0 警告**（全解决方案 31 项目） |
| `dotnet test tests/LYBT.Tests.Architecture/` | **86/86 全绿**（DP10 规则按 `IApiClient*` 前缀自动覆盖新接口） |
| `dotnet test tests/LYBT.Tests.Desktop/ --filter "AuthEventPublishingTests\|LogoutServiceTests\|HttpClientApiClientEnvelopeTests\|SwitchingApiClientTests"` | **32/32 通过** |

## 5. 关键约束达成

| 约束 | 达成 |
|------|------|
| 路由 `/api/v1/auth/*` + `/api/v1/users/*` 不变 | ✅ `IdentityHttpApiClient`/`IdentityApiClient` 保留双路由（Server IdentityController 双路由未动） |
| LoginCoordinator 不动（Auth 侧已抽象） | ✅ 未触碰 `LoginCoordinator`/`ILoginCoordinator`；仅更新其下层注入 `IApiClientAuth` 的 4 个服务 |
| 0 错误 0 警告 | ✅ |
| 架构测试全绿 | ✅ 86/86 |

## 6. 文档更新

- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/README.md`：`Auth`/`Users` 属性表 → `Identity`；`IApiClientAuth`/`IApiClientUsers` 两节 → `IApiClientIdentity`（20 方法表）
- `docs/05-development/03-patterns.md`：SwitchingApiClient 示例代码 `Auth` → `Identity`
- `docs/03-architecture/05-dual-mode.md`：`IApiClient.Users` → `IApiClient.Identity`
- `docs/03-architecture/15-solution-integration-plan.md`：C-3a 落地行 Desktop 侧标注 C-3d 完成
- `docs/03-architecture/13-project-master-plan.md`：A-31 行追加 C-3d 完成记录
- `tests/LYBT.Tests.Architecture/DesktopLayerArchTests.cs`：DP10 注释示例名更新

## 7. 遗留/后续

- `AuthenticationIntegrationTests`（NSubstitute 代理 `IAuthApi` internal 接口失败）为 HEAD 存量环境问题（C-3a/C-7 已记录），本次未触碰
- Server 侧 `BaseUsersController` 的 `/api/v1/users/*` 与 `/api/v1/auth/*` 双路由在 C-3a 已收敛，本批 Desktop 对齐后双端 IApiClient/Controller 命名完全一致

---

*报告完成。全部验证基于真实 build/test 输出。*
