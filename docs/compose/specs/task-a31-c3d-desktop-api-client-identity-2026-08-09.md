# 任务 A-31-C3d：Desktop IApiClient Identity 侧合并

> 派发对象：Mimo Code
> 版本：v1.0 | 日期：2026-08-09
> 依据：Server 端 C-3a 已完成（Auth+Users→Identity），Desktop API 客户端需同步

## 任务

将 Desktop 侧 `IApiClientAuth` + `IApiClientUsers` 合并为 `IApiClientIdentity`，与 Server 端对齐。

## 现状

- `IApiClient`（主接口）→ `Auth` 属性（IApiClientAuth）+ `Users` 属性（IApiClientUsers）
- `IApiClientAuth`：6 方法（Login/AutoLogin/Logout/Refresh/ValidateToken/HealthCheck）
- `IApiClientUsers`：14 方法（CRUD+Password+Batch+Profile）
- 实现类：`AuthApiClient` + `UserApiClient`
- Auth 侧调用：通过 `ILoginCoordinator` 抽象（0 直接 `IApiClient.Auth` 调用）
- Users 侧调用：`UserRepository` 有 9 处直接 `_apiClient.Users.*` 调用

## 动作

1. **合并接口**：`IApiClientAuth` + `IApiClientUsers` → `IApiClientIdentity`（20 方法）
2. **更新 IApiClient**：`Auth` + `Users` → `Identity` 属性
3. **合并实现**：`AuthApiClient` + `UserApiClient` → `IdentityApiClient`
4. **更新消费方**：`UserRepository` 9 处 `_apiClient.Users.*` → `_apiClient.Identity.*`
5. **更新 DI 注册**：`AddApiClient` 中 Auth/Users 注册合并
6. **删除旧文件**：`IApiClientAuth.cs` + `IApiClientUsers.cs` + `AuthApiClient.cs` + `UserApiClient.cs`
7. **更新 IApiClient 主接口**：`Auth` + `Users` → `Identity`

## 关键约束

- **路由保持**：`/api/v1/auth/*` + `/api/v1/users/*` 不变（Server 端 IdentityController 双路由）
- **LoginCoordinator 不动**：Auth 侧已通过 ILoginCoordinator 抽象，0 直接调用
- **0 错误 0 警告**：`dotnet build LYBTZYZS.sln --no-incremental`
- **架构测试**：`dotnet test tests/LYBT.Tests.Architecture/` 全绿

## 产出

- 1 个 commit + push
- 报告：`docs/compose/reports/a31-c3d-desktop-api-client-identity-merge.md`
