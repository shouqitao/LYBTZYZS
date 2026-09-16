# 设计 04：缓存层定案（桌面客户端）

> 状态：待实施 ｜ 日期：2026-09-16 ｜ 来源：审计报告 L3 缓存策略

## 1. 问题描述（实测）

| 项 | 现状 | 证据 |
|---|---|---|
| 缓存容器 | `IMemoryCache` 已注册：`SizeLimit=1000`、`CompactionPercentage=0.25` | `Shell/Extensions/ServiceCollectionExtensions.cs` |
| 缓存生产者 | **无**：全仓 0 处 `IMemoryCache.Set/GetOrCreate` | grep 仅命中注册处 |
| 失效入口 | `DesktopCacheManager` 5 个方法按 `GET:/api/v1/{域}` **前缀**清理 + 发 Prism `CacheEvents.InvalidatedEvent` | `Foundation/Caching/DesktopCacheManager.cs` |
| 前缀实现 | `RemoveByPrefix` 反射 `MemoryCache.EntriesCollection` 私有属性枚举全部 key | `Shared/.../ServiceCollection/CacheExtensions.cs` |
| 失效调用点 | 100% 在 ViewModel（MasterDetail 各页 + UserEditor） | `*MasterDetailViewModel` |
| 缺失域 | `IDesktopCacheManager` 只有 5 个域方法；`CacheDomain` 有 6 个（含 `All`）；**无 Registrations / Reports** | `Contracts/Events/CacheEvents.cs` |

**后果**：每次失效 = 一次空转 + 一次反射全表枚举；且一旦按原始意图补上 GET 缓存，因 `MemoryCache` 配置了 `SizeLimit` 而 `Set` 未提供 `Size`，会**当场抛 `InvalidOperationException`**（Microsoft.Extensions.Caching.Memory 的既定行为）。

## 2. 设计方案

**定为「进程内 `IMemoryCache` + 显式写后失效」**（不引入分布式缓存——桌面为单机单用户，跨进程共享无场景）。

> **实施修正（2026-09-16）**：初稿把缓存生产者放在 `HttpApiClientBase.GetAndWrapAsync`、把失效下沉到仓储写模板。落地时改为**传输层 `DelegatingHandler`**（`CachingHttpMessageHandler`），原因有二：
> ① 在 `HttpApiClientBase` 加依赖需要改 20 个适配器构造器（与并行切片冲突，且把缓存耦合进 HTTP helper）；
> ② 仓储写模板只覆盖「经仓储的写」，而 handler 挂在链上可覆盖**全部**出站请求（含 Service/VM 直调子接口的路径），失效面更完整。
> 键仍保持 `GET:{path}{query}` 前缀约定（`RemoveByPrefix` 语义不变），并追加 `#{userId}` 用户域后缀。

1. **读路径生产者**：新增 `CachingHttpMessageHandler`（挂 `RemoteApi` 与 `LocalApi` 具名客户端）对 GET 做缓存：
   - 键：`GET:{AbsolutePath}{Query}#{userId}`（与既有失效前缀约定一致；用户域后缀防跨用户串读）。
   - 每项**必须** `Size = 1`，满足 `SizeLimit`。
   - TTL 分级：目录类（herbs/formulas）5 min；事务类 30 s。
   - 不缓存：`/export`、`/import-template`、`/health`、`/download`。
2. **写路径集中失效**：同一 handler 在非 GET 2xx 后按路径域前缀失效，含跨域依赖表（`registrations ↔ medicalcases`）。
3. **补齐域**：`IDesktopCacheManager` 增加 `InvalidateRegistrationCaches()` / `InvalidateReportCaches()` / `InvalidateAll()`；`CacheDomain` 增加 `Registrations` / `Reports`。
4. **去反射**：新增 `DesktopCacheKeyRegistry`（写入时登记 + 逐出回调注销），`DesktopCacheManager` 改用它；服务端仍用 `Shared` 的反射实现（本次未纳入范围）。

## 3. 影响范围

`Foundation/Caching/**`、`Contracts/Services/IDesktopCacheManager.cs`、`Foundation/Http/HttpApiClientBase.cs`、`Foundation/Repositories/**`、`Shared/.../CacheExtensions.cs`、`Shell/Extensions/ServiceCollectionExtensions.cs`、VM 中的失效调用点。

## 4. 风险评估

| 风险 | 等级 | 缓解 |
|---|---|---|
| 缓存陈旧（失效遗漏） | **高** | ① TTL 兜底；② 失效下沉到写模板（覆盖全部写路径）而非 VM；③ 为「写后读回」加集成测试 |
| `SizeLimit` 未设 `Size` 抛异常 | 中 | 统一入口设置 `Size`，并加单测断言「缓存条目可写入」 |
| 反射替换引入新失效缺口 | 中 | 键登记与 `Set` 同点写入，不依赖缓存内部状态 |
| 缓存掩盖并发/权限差异（不同角色看到同一列表） | 中 | 键纳入用户/角色维度，或对权限敏感端点不缓存 |

## 5. 实施步骤

1. 出 ADR（桌面缓存策略：进程内 + 写后失效 + 键登记）。
2. `IDesktopCacheManager` 补域 + 键登记实现。
3. 读路径生产者（GET + TTL + Size）。
4. 写模板集中失效。
5. build + 定向集成测试（写后读回、TTL 过期）。
