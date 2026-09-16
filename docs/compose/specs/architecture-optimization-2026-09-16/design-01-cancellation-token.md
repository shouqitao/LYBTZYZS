# 设计 01：CancellationToken 全链传播（桌面客户端）

> 状态：待实施 ｜ 日期：2026-09-16 ｜ 来源：`docs/compose/reports/frontend-architecture-audit-2026-09-16.md` §四 / L2-04 / L3 取消传播

## 1. 问题描述（实测）

| 层 | `CancellationToken` 现状 | 证据 |
|---|---|---|
| 桌面契约 `Contracts/ApiClient/` | **0 处**（10 个子接口，共 108 个 `Task` 方法） | `grep -rn CancellationToken Contracts/ApiClient/ \| wc -l` = 0 |
| 桌面 Refit `Contracts/Api/` | **0 处**（11 个接口，共 100 个 `Task` 方法） | 同上 |
| `IEntityApiSegment<,,>` | 5 个方法全无 | `Contracts/ApiClient/IEntityApiSegment.cs` |
| `EntityApiClientRepositoryBase` | 形参声明了 `ct`，**调用 `Api.*` 时丢弃** | `GetPagedAsync`/`GetByIdAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync` |
| `ApiClientRepositoryBase` | `ExecuteImportAsync` 收 `ct` 但函数体内 0 引用；`ExecuteBatchDeleteAsync` 连形参都没有 | 同上 |
| 模块仓储 | 61 处 `ct` 形参（声明齐全） | `Modules/*/Repositories/*.cs` |
| 服务端 Handler/Service | 已有 `ct` 并逐层透传（合规） | `CancelRegistrationCommandHandler` 等 |

**后果**：桌面侧「取消」是假契约——VM 关闭/切换模块/登出后，in-flight HTTP 请求继续占用连接并把结果回写到已失效的 VM 状态；仓储公开的 `ct` 形参无任何效果。

## 2. 设计方案

**唯一约定**：所有异步公共方法末尾追加 `CancellationToken ct = default`（有默认值 → 不破坏既有调用点）。

- 契约层（`IApiClient*`、Refit `I*Api`）：末尾 `CancellationToken ct = default`。Refit 原生识别末尾 `CancellationToken` 作为请求取消令牌（`[Refit.Body]` 之外的末位参数）。
- `IEntityApiSegment<,,>`：5 个方法补 `ct`；其 DIM 转发实现（`IApiClientFormulas` 等）同步。
- 适配器（`HttpApiClientBase` 派生 10 个 + Refit 直通 10 个）：透传到 `GetAndWrapAsync`/`PostAndWrapAsync`/`SendAsync`（这些早已支持 `ct`）与 Refit 调用。
- 仓储基类 → 模块仓储 → 桌面 Service：逐层透传。
- **不改** `SwitchingApiClient` 的「模式切换不取消在途请求」既定决策（`SwitchingApiClient.cs` 头注释 P2-15-5）；本次只让**调用方**能传 `ct`。

## 3. 影响范围

`Contracts/ApiClient/`(10)、`Contracts/Api/`(11)、`Foundation/Http/Clients/`(20)、`Foundation/Http/`(3)、`Foundation/Repositories/`(3)、`Modules/*/Repositories/`(7)、桌面 Service（`CrudServiceBase` 及各模块 Service）、调用方 VM（按需传 `ct`）。
预计签名变更 ≈ 210 处，**全部编译器可捕获**。

## 4. 风险评估

| 风险 | 等级 | 缓解 |
|---|---|---|
| Refit 契约破坏（`ct` 未置于末位 → 被当作请求参数） | 中 | 机械加在末位；构建后跑 `Refit` 生成期即可暴露 |
| `ct` 传入后 `OperationCanceledException` 冒泡到 VM | 中 | 调用方仅在「确定要取消」处传 `ct`；其余仍可省略（默认 `default`） |
| 大量文件并发修改冲突 | 中 | 按层分片、冻结签名契约后并行 |

## 5. 实施步骤

1. 契约层补 `ct`（`Contracts/ApiClient/` + `Contracts/Api/`）。
2. 适配器层透传（`Foundation/Http/Clients/`）。
3. 仓储基类 + 模块仓储透传。
4. 桌面 Service 透传；编译修复调用方。
5. `dotnet build --no-incremental` + 定向测试。
