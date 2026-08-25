# VM 交互 Phase 1 验证报告：认证 + 连接（2026-08-13）

> 任务书 `.hermes-task-vm-interaction.md` Phase 1（5 个 Auth VM——登录→Token→自动刷新→服务器配置→首次运行全链路）。
> **结论：5 个 VM 已由历史批次（B1/B2/SHELL-018/QUICKVISIT 等）完整实现真实 API 接线——本次验证确认无代码缺口，任务书「骨架」假设不成立。**

## 5 个 VM 实现证据

| VM | 行数 | 实现状态 | 真实 API 链路 |
|----|------|---------|--------------|
| LoginViewModel | 360 | ✅ 完整 | `LoginCommand → ILoginCoordinator.LoginAsync → IAuthenticationService.LoginAsync → IApiClientIdentity.LoginAsync (POST /api/v1/auth/login)`；Token 存 `ITokenStorageService`；导航/状态机/首次运行向导/防重入全接 |
| LoginCredentialsViewModel | 190 | ✅ 完整 | `IUsernameStorageService`（用户名/RememberMe）+ `ICredentialVault`（Windows 凭据库——HasSavedPassword/GetPassword）——Load/Save 全实现 |
| ServerConfigViewModel | ~140 | ✅ 完整 | `ConnectionTestViewModelBase.TestConnection → IConnectionModeService.TestRemoteConnectionAsync`（真实 HTTP /health 探测）+ `IConnectionSettingsService.SetUrlAsync` 持久化 + 双模式切换 |
| FirstRunSetupViewModel | ~126 | ✅ 完整 | 同基类（连接测试 + 模式选择）；**SysAdmin 创建走服务器侧 SetupToken 架构**（`SystemAdmin:InitialSetupToken`——Desktop 无权限调用——任务书设想不适用，见决策） |
| ConnectionStatusViewModel | 318 | ✅ 完整 | `IApplicationStateService.IsApiHealthy`（真实健康状态）+ `IConnectionModeService.CheckRemoteAvailableAsync/SetModeAsync`（真实探测 + B2 守卫） |

## 全链路核查

| 环节 | 实现 | 位置 |
|------|------|------|
| 登录（POST /api/v1/auth/login） | ✅ | AuthenticationService.LoginAsync → `_apiClient.Identity.LoginAsync` |
| Token 存储 | ✅ | `ITokenStorageService.SaveAuthenticationAsync`（**Session 存储——Issue #1907：医疗系统不支持 RememberMe 持久化**） |
| **自动刷新** | ✅ | `TokenRefreshHandler`（RefitApiClient HTTP 管道：HttpClientHandler → TokenRefreshHandler → AuthorizationMessageHandler——401 拦截自动刷新） |
| 自动登录（AutoToken） | ✅ 服务层 | `AuthenticationService.LoginWithAutoTokenAsync`（225 行——服务就绪；**VM 层未接线**——Session token 不跨进程 + 医疗系统安全决策，自动登录不启用——见决策） |
| 服务器配置 | ✅ | ConnectionModeService.TestRemoteConnectionAsync（真实 HTTP 探测）/ SetModeAsync（B2 守卫 ERR-70506） |
| 首次运行 | ✅ | FirstRun 向导（连接测试→模式选择→标记文件 first_run_done.flag） |
| RememberMe | ✅ | vault（Windows 凭据库——本地加密） |

## 架构决策记录（与任务书设想的差异）

1. **自动登录（IsAutoLogin）不接线**：`TokenStorageService` 注释 Issue #1907——医疗系统不支持 RememberMe 持久化（token 仅 Session——进程内存）；自动登录需跨进程凭据——安全决策不启用。VM 的 `IsAutoLogin` 保留为 UI 开关（注释标注「预留」）。
2. **FirstRunSetup 不创建 SysAdmin**：架构上 SysAdmin 创建在**服务器侧**（`IdentitySeedData` + `SystemAdmin:InitialSetupToken` 初始化令牌——权限/安全）；Desktop 首次运行只做**连接配置**（测试/保存/模式选择）。任务书「FirstRun 创建 SysAdmin」不适用当前架构。
3. **连通性测试用 /health 而非 /auth/validate**：validate 需登录 token（未登录不可用）；health 无认证轻量探测——正确选择。

## 验证

- `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-incremental`：**0 警告 0 错误**
- 5 个 VM **零 TODO/NotImplemented/空实现**（grep 验证）
- 关键方法体抽查：SaveAndEnableAsync / SaveRemoteAsync / TestConnectionAsync / ExecuteLoginAsync / LoadSavedCredentialsAsync / LoadApiStatusAsync / DetectConnectionModeAsync 全为真实服务调用（非空壳）

## 结论

Phase 1 实质工作（真实 API 接线）已交付——**本次为验证 + 归档**。Phase 2（User/Patient CRUD VM）任务书假设「骨架」可能同样不成立（历史批次已实现 MasterDetail VM）——届时先核查再实施。
