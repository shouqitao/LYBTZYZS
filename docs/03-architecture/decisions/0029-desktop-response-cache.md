# ADR: 桌面 GET 响应缓存策略（进程内 + 传输层读写一体）

> 状态: Accepted | 日期: 2026-09-16 | 相关: 设计 04（`docs/compose/specs/architecture-optimization-2026-09-16/design-04-cache-strategy.md`）、架构审查 L3「缓存策略」

## 背景

- `IMemoryCache` 已注册（`SizeLimit=1000`/`CompactionPercentage=0.25`），`DesktopCacheManager` 已提供 5 个域的前缀失效 + Prism 失效事件。
- 但**全仓 0 处 `IMemoryCache.Set/GetOrCreate`** —— 缓存没有生产者，每次失效都是空转（且 `RemoveByPrefix` 走反射枚举 `MemoryCache` 私有集合，实现变更时静默返回空集合）。
- 失效调用点 100% 散落在 ViewModel，写路径（仓储/Service）完全不参与 → 一旦补上缓存，非 MasterDetail 路径的写操作不会失效。
- 若按原意图补 `Set` 而不设 `Size`，会命中 `MemoryCache` 的 `SizeLimit` 校验直接抛 `InvalidOperationException`。

## 决策

**定为「进程内 `IMemoryCache` + 传输层读写一体」**（不引入分布式缓存：桌面为单机单用户）。

1. **生产与失效同时下沉到传输层**：新增 `CachingHttpMessageHandler`（`DelegatingHandler`），同时挂到 `RemoteApi` 与 `LocalApi` 具名客户端：
   - **读**：GET 2xx → 缓存（键 `GET:{AbsolutePath}{Query}#{userId}`，条目显式 `Size = 1`）。
   - **写**：非 GET 2xx → 按路径域前缀失效（含跨域依赖表：`registrations ↔ medicalcases`），覆盖**全部**写路径，不再依赖调用方自觉。
2. **TTL 分级**：目录类（`herbs`/`formulas`）5 分钟；事务类（其余）30 秒。
3. **用户域隔离**：键尾附 `#{userId}`（取自 `ITokenStorageService.GetLoginResponse()`），避免换用户后串读；`anonymous` 覆盖未登录态。
4. **不缓存**：`/export`、`/import-template`、`/health`、`/download`（大体积二进制/无意义）。
5. **去反射**：新增 `DesktopCacheKeyRegistry` —— 写入时登记键、经 `MemoryCacheEntryOptions.RegisterPostEvictionCallback` 在逐出时同步注销，前缀失效只遍历登记表。`DesktopCacheManager` 改用它；服务端仍用 `Shared` 的反射实现（本次未纳入范围，见报告）。
6. **补齐域**：`IDesktopCacheManager` 增加 `InvalidateRegistrationCaches` / `InvalidateReportCaches` / `InvalidateAll`；`CacheDomain` 增加 `Registrations` / `Reports`。

## 后果

- **正向**：目录类读取（药材/验方）在高频浏览下不再反复打后端；写路径失效全覆盖；缓存条目按用户隔离；前缀失效不再依赖私有反射。
- **代价与已知窗口**：事务类域存在最长 **30 秒**的陈旧窗口（写操作经传输层即时失效，但绕过传输层的直连写不在覆盖范围——当前无此类路径）。
- **风险**：缓存键以 URL 为准，不同权限视图若共用同一 URL 则共享缓存（当前端点按角色返回不同数据时 URL 相同 —— 已通过用户域后缀隔离到「同一用户」粒度；跨角色切换必然伴随重新登录 → 用户域变化 → 自然隔离）。
