# 双模式架构（Remote WebAPI + LocalWebAPI）
> 版本: v8.2 | 日期: 2026-09-22

> **N1 决策（2026-06-28，用户确认）**：**v1.0 远程库与本地库数据孤立，不互通**。本地模式定位为"远程故障应急降级"，断网期录入的数据事后手动补录或可丢。**Sync（数据同步）整体延期至 v2.0**，详见 [17-sync-protocol.md](17-sync-protocol.md)（v2.0 设计参考）。

## 概述

系统通过 URL 驱动的方式自动选择连接目标，不需要手动选择"模式"。

| URL 类型 | 客户端实现 | 目标服务 | 数据库 | 适用场景 |
| ---------- | ----------- | --------- | -------- | ---------- |
| **127.0.0.1:5300 / localhost:5300** | HttpClientApiClient | 嵌入式 Kestrel（端口 **5300**，见 `EmbeddedLocalWebApiService.cs:17` + `appsettings.json:OfflineMode:LocalApiBaseUrl`） | SQL Server (本地) | 单用户离线 |

用户通过状态栏的"连接设置"弹出面板输入 URL，`SwitchingApiClient` 代理自动路由到对应的底层实现。Repository 层完全无感知。

---

## 设计理由

> 完整决策记录见 [ADR-0009: URL 驱动双模式架构](decisions/0009-url-driven-dual-mode.md)。

### 问题背景

中医诊所管理系统需要同时满足两种部署场景：

1. **多终端联网** — 诊所配备 1-5 台终端（前台+医生），通过局域网连接共享数据
2. **单终端离线** — 偏远地区或网络不稳定环境，需要完全离线工作

### 核心设计选择

| 决策点 | 选择 | 替代方案 | 选择理由 |
| -------- | ------ | ---------- | ---------- |
| 本地数据库 | SQL Server LocalDB | — | 与远程 SQL Server 方言完全一致，消除跨数据库 LINQ 行为差异 |
| 本地 API 宿主 | 嵌入式 Kestrel（进程内） | 独立 Windows Service | 单进程部署，无需管理外部服务，适合无 IT 运维的小诊所 |
| 模式切换机制 | URL 驱动（localhost 判断） | ConnectionMode 枚举 + DI 重建 | 零配置切换，用户改 URL 即可，消除运行时状态机竞态 |
| Repository 统一层 | SwitchingApiClient 代理 | 直接注入 DbContext | HTTP 中间件管线（认证/授权/异常/日志）完整复用 |
| 认证复用 | 两端均用 JWT Bearer Token | 本地跳过认证 | Authorization Policy/Claims/中间件完整生效 |
| Controller 分离 | 两套独立 Controller | 共享 Controller 项目 | 双端 Controller 均复用同一 Service/Handler 层（ADR-0010 统一服务层），仅宿主/认证细节不同 |
| 本地认证简化 | 1年长效 Token，无 Refresh | 完整 Refresh Token 流程 | 本地单用户 + Mutex 单实例，简化认证降低复杂度 |

### 设计权衡

**接受的代价**：

| 代价 | 理由/缓解 |
| ------ | ---------- |
| 本地模式 HTTP 序列化开销 | localhost 回环延迟 <1ms，小诊所数据量（~5000 医案/年）下不可感知 |
| 两套 Controller 代码 | Controller 仅做参数校验 + 调用 Service，核心业务规则在共享层（Entities/Validators/DTOs） |
| 本地 JWT 固定密钥 | 本地单用户场景，Mutex 保证单实例，安全风险可控；后续可 DPAPI 外部化 |
| 端点覆盖需手动对齐 | 当前 ~96%（112 remote vs 107 local，不含 Sync），差异集中在 MedicalCases 查询端点与 Reports 趋势端点（2026-08-08 核对；2026-09-22 B-06 备份/恢复 8 端点双端对齐） |

**获得的收益**：

| 收益 | 说明 |
| ------ | ------ |
| 业务代码 100% 复用 | ViewModel/Service/Repository 零改动，完全无感知当前模式 |
| HTTP 管线完整复用 | 认证、授权、异常处理、日志、CorrelationId 两端全部生效 |
| 数据库行为一致 | 两端均为 SQL Server，LINQ 查询/排序/日期/NULL 处理行为完全一致 |
| 部署极简 | Desktop 单进程，LocalWebAPI 随主进程自动启停 |
| 模式切换无感 | URL 变更 → SwitchingApiClient 自动路由，无需重启或重新登录 |

---

## WebAPI vs LocalWebAPI 完整对比

### 共同点

| 维度 | 说明 |
| ------ | ------ |
| **Repository 接口** | 完全相同 — 6 个 `IXxxRepository` 接口定义在 `Contracts/Repositories/` |
| **DTO 契约** | 完全相同 — `src/Shared/LYBT.Shared.Models/Contracts/` |
| **实体模型** | 完全相同 — `src/Shared/LYBT.Entities/`，AppDbContext 复用所有 `IEntityTypeConfiguration` |
| **业务规则** | Validators、BusinessRules 完全共享 |
| **认证机制** | 两端均使用 JWT Bearer Token + 相同 Claims Schema |
| **授权策略** | 相同的 7 个 Policy（`AdminBusinessOnly` / `DoctorOnly` / `DoctorOrAdmin` / `AdminOrSuperAdmin` / `SysAdminOnly` / `DoctorOrReceptionist`（P1-6 Batch D 已扩为含 Admin/SuperAdmin） / `DoctorOrAdminOrReceptionist`，见 `PolicyConstants`）——双端 Registrations 策略已对齐（P1-29 Batch D：Create=`DoctorOrReceptionist`、StartVisit=`DoctorOnly`、Cancel=`ReceptionistOnly`，`LocalWebApiPatternTests.Should_Have_Same_Auth_Policy_As_Remote` 守卫） |
| **EF Core 过滤器** | `IsDeleted` 软删除全局过滤器两端均生效 |
| **异常处理** | 异常路径统一 ProblemDetails（X-3 + R-2：Server UnifiedMiddlewareConfiguration / Local SharedHost）；控制器业务失败两端均 ApiResponse |

### 不同点（含认证/DbContext/功能限制合并）

| 维度 | Remote WebAPI | LocalWebAPI | 设计理由 |
| ------ | -------------- | ------------- | ---------- |
| **宿主进程** | 独立 ASP.NET Core 服务 | WPF 进程内嵌 Kestrel（动态端口） | 单进程部署 |
| **URL 前缀** | `/api/v1/`（含版本段） | `/api/v1/`（含版本段，与远程一致） | 实现已收敛（2026-08-08 修正，原文档声称本地无版本段已过时） |
| **序列化** | camelCase（`AddControllers().AddJsonOptions`） | PascalCase（默认） | 历史 Token 兼容 |
| **数据库连接** | 远程 SQL Server（共享） | 本地 SQL Server LocalDB（每机独立） | 数据隔离 |
| **数据库名** | LYBTDB | LYBTDB_Local | — |
| **数据库迁移** | EF Core 迁移 | `MigrateAsync()` + 双种子（IdentitySeedData + LocalWebApiSeedData） | 与远程同一迁移链（2026-08-08 修正，原文档声称 EnsureCreated 已过时） |
| **AccessToken 有效期** | 配置驱动（base 480/Dev·Test 60/Prod 30 分钟） | 1 年 | 本地无 Token 泄露风险 |
| **RefreshToken** | 支持（滑动续期 + Token Family 防重放） | 不支持 | 本地单用户，无需续期 |
| **JWT 签名密钥** | 配置文件 (appsettings.json) | 固定常量 (`LYBT-LocalWebAPI-Secret-Key-2024`) | 本地无需运维管理 |
| **SecurityAuditLog** | 记录（登录/登出/刷新/锁定） | 不记录 | 本地无审计合规需求 |
| **Rate Limiting** | 5次/60s 登录 + 100次/min API（均按来源 IP 分区） | `LocalLogin` 5次/60s + `ApiCalls` 100次/min（均按来源 IP 分区，2026-09-16 对齐远程维度；原为全局固定窗口且无 `ApiCalls`） | 本地登录防爆破 |
| **客户端传输链** | 经私有 `IHttpClientFactory` 的具名客户端 `RemoteApi`：`LoggingHttpHandler` → `CachingHttpMessageHandler` → `AuthorizationMessageHandler` → `TokenRefreshHandler` → `SocketsHttpHandler` | 具名客户端 `LocalApi`：`CachingHttpMessageHandler` → `AuthorizationMessageHandler` → `SocketsHttpHandler`（**不含** `TokenRefreshHandler`：本地续期由 Shell `TokenLifecycleService` 显式驱动，避免误发远程刷新） | 两端同一「携带 Bearer」契约；本地端点同受 `[Authorize]` 保护。**2026-09-16 第二批**：桌面主容器为 Prism.DryIoc，无法直接 `AddHttpClient`，故由 `DesktopHttpTransportFactory` 以私有 `ServiceCollection` 构建传输层工厂后注入 DryIoc（[ADR-0028](decisions/0028-desktop-http-resilience.md)）；handler 由工厂池化管理（`SetHandlerLifetime`/`PooledConnectionLifetime` = 2min），幂等请求带 3 次指数退避重试（POST 不重试）。此前本地传输无 `AuthorizationMessageHandler` 且返回共享 `HttpClient`，导致受保护端点恒 401、第二次调用 `ObjectDisposedException`（2026-09-16 第一批修复） |
| **响应缓存** | 进程内 `IMemoryCache` + 传输层读写一体（[ADR-0029](decisions/0029-desktop-response-cache.md)） | 同左（双端共用同一条 handler 链） | GET 2xx 缓存（键 `GET:{path}{query}#{userId}`，目录类 TTL 5min / 事务类 30s，条目显式 `Size=1`）；非 GET 2xx 按域前缀失效（含 registrations ↔ medicalcases 跨域依赖） |
| **默认授权（FallbackPolicy）** | `RequireAuthenticatedUser` | `RequireAuthenticatedUser`（2026-09-16 补齐，此前缺失为 fail-open） | 双端默认拒绝，匿名端点须显式 `[AllowAnonymous]` |
| **CORS** | 配置允许桌面端 origin | 不配置（同源） | localhost 无跨域 |
| **Sync 端点** | 6 个（作为 Sync Server） | 无（本地是唯一数据源） | 本地无需与自己同步 |
| **打印日志** | `POST /print-completed` 写入 `MedicalCasePrintLog` | 不记录 | 本地无服务端审计 |
| **多用户并发** | 支持（乐观锁 + 事务隔离） | 单用户（Mutex 防多开） | 本地无需并发控制 |
| **DI 架构** | 完整 3-layer（Controller→Service→Repository→DbContext） | 同一 3-layer（Controller→ISender/I*Service 全复用 Server Service/Handler 层，ADR-0010） | 双轨真共享（2026-08-08 修正，原文档声称本地 Controller→DbContext 直连已过时） |
| **配置来源** | appsettings.json + 环境变量 | 嵌入式配置（代码内） | 本地无运维管理 |
| **端点数** | ~104 | ~99 | 2026-08-08 A-17 修复后核对（不含 Sync；MedicalCases/Reports/Configuration 有缺口） |
| **健康检查** | DB 连接 + 版本 + 延迟 | DB 连接 + 磁盘空间 | 本地关注磁盘 |

### LocalWebAPI 独有端点

| 模块 | 端点 | 方法 | 说明 |
| ------ | ------ | ------ | ------ |
| Formulas | `/api/v1/formulas/{id}/clone` | POST | 克隆验方（含药材组成） |
| Patients | `/api/v1/patients/by-id-number/{idNumber}` | GET | 按身份证号查询患者 |
| MedicalCases | `/api/v1/medicalcases/by-status/{status}` | GET | 按状态查询医案 |
| Diagnostics | `/api/v1/diagnostics/db-info` | GET | 数据库连接信息 + 磁盘空间 |
| Diagnostics | `/api/v1/diagnostics/logs/recent` | GET | 最近日志条目 |

> `GET /api/v1/medicalcases/pending` 原列于本表，2026-09-16 已在 Remote `MedicalCasesController` 补齐对位实现（Refit 契约 `IMedicalCaseApi.GetPendingCasesAsync` 与 `ConnectionModeService` 的探测此前在远程模式 404）——**不再是 Local 独有**。

> **纠错（D8）**: 原文档另列 `formulas/categories` 与 `patients/by-phone` 为本地独有端点 — 代码全仓不存在，属虚构条目，已移除。

**设计说明**: 这些端点满足本地单用户场景的便捷需求（如快速克隆验方、身份证号查询、系统诊断），不要求远程模式实现。这些查询在远程模式由 Repository 客户端过滤完成。

### 本地模式功能限制

部分对服务端有强依赖的功能在本地模式下不可用：

| 功能 | 原因 | 行为 |
| ------ | ------ | ------ |
| Token 刷新 | 本地使用 1 年长效 Token | RefreshToken 端点返回 501 |
| SecurityAuditLog | 本地无审计合规需求 | 查询返回空结果 |
| 自动登录令牌 | 依赖远程中心化存储 | 端点返回 501 |
| 用户同步 | 用户数据不参与同步 | 手动维护 |
| 打印日志 | 本地未实现 print-completed 端点（2026-08-08 修正，原文档声称「端点返回空结果」过时） | 端点不存在 → 404 |

---

## URL 驱动切换

```mermaid
graph TB
    subgraph UI["UI 层"]
        StatusBar["状态栏 — 显示当前连接"]
        Popup["连接设置弹出面板"]
    end

    subgraph CS["连接配置"]
        CSService["IConnectionSettingsService<br/>URL 持久化 + IsLocal 判断"]
    end

    subgraph Proxy["代理层"]
        Switch["SwitchingApiClient : IApiClient"]
        Switch -->|"localhost?"| LocalImpl["HttpClientApiClient"]
        Switch -->|"其他地址"| RemoteImpl["RefitApiClient"]
    end

    subgraph REPO["Repository 层（不变）"]
        PatientRepo["PatientRepository"]
        HerbRepo["HerbRepository"]
    end

    UI --> CSService
    CSService --> Switch
    PatientRepo --> Switch
    HerbRepo --> Switch
    LocalImpl --> LocalWebAPI["嵌入式 Kestrel"]
    RemoteImpl --> ServerAPI["Server WebAPI"]
```

### 模式切换流程

> 对应 [ADR-0009: URL 驱动双模式架构](decisions/0009-url-driven-dual-mode.md) —— **URL 改即生效，无显式切换动作**。

```mermaid
flowchart TD
    A[用户在连接设置面板修改 URL] --> B[ConnectionSettingsService 持久化新 URL]
    B --> C[SwitchingApiClient 下次属性访问时读取 CurrentUrl]
    C --> D{localhost / 127.0.0.1?}
    D -->|是| E[路由到 HttpClientApiClient → 嵌入式 Kestrel :5300]
    D -->|否| F[路由到 RefitApiClient → 远程 WebAPI :5000]
    E --> G[Repository 层零感知，业务继续]
    F --> G
```text

**关键特性**（ADR-0009）：

- **无切换动作**：用户改 URL → 下次 API 调用自动走新目标，无需重启或重新登录
- **无运行时状态机**：`SwitchingApiClient` 是无状态代理，每次属性访问实时判断
- **无数据迁移**：v1.0 两库孤立（N1 决策），URL 切换不触发任何数据同步

## 统一 IApiClient 抽象

所有 Repository 依赖统一的 `IApiClient` 接口，不再区分"远程实现"和"本地实现"。`SwitchingApiClient` 代理根据当前 URL 自动路由。

| 接口 | IApiClient 子接口 | 说明 |
| ------ | ------------------ | ------ |
| IPatientRepository | IApiClient.Patients | CRUD + 批量操作 |
| IHerbRepository | IApiClient.Herbs | CRUD + 批量操作 + 分类查询 |
| IFormulaRepository | IApiClient.Formulas | CRUD + 克隆 + 批量操作 + 分类查询 |
| IMedicalCaseRepository | IApiClient.MedicalCases | CRUD + 状态流转 + 处方 |
| IUserRepository | IApiClient.Identity | CRUD + 密码管理 + 批量操作 |
| IRegistrationRepository | IApiClient.Registrations | CRUD + 队列管理 |

DI 注册在 `UnifiedApiClientExtensions.cs` 中完成：始终注册 `SwitchingApiClient` 为 `IApiClient` Singleton。P1-30 Batch D 2026-08-21 收敛：`Contracts/Api/IApi*`（Refit 生成，`internal`）为远程/本地共享契约；`Contracts/ApiClient/IApiClient*` 聚合为单一 `IApiClient` 门面（含 10 子段 `IApiClientPatients` 等作聚合属性），`ViewModel` 仅依赖 `IApiClient.*`，不直连 Refit 接口。

### SwitchingApiClient 代理

```text
SwitchingApiClient : IApiClient
  ├── _remoteApi (RefitApiClient)     ← 非 localhost 时使用
  └── _localApi  (HttpClientApiClient) ← localhost/127.0.0.1 时使用
```

- 每次属性访问（如 `_apiClient.Patients`）实时读取 `IConnectionSettingsService.CurrentUrl`
- URL 变更时自动重建底层客户端
- Repository 层零改动

## 端点覆盖率

| 模块 | Remote 端点 | Local 端点 | 覆盖率 | 差异说明 |
| ------ | ------------ | ----------- | ------- | ---------- |
| Auth | 5 | 5 | 100% | 两端一致（13b 5 端点） |
| Users | 14 | 14 | 100% | 均继承 BaseUsersController |
| Patients | 12 | 12 | 100% | A-17 补全本地 CRUD override 后对齐 |
| Herbs | 13 | 13 | 100% | — |
| Formulas | 13 | 14 | 108% | Local 多 clone |
| MedicalCases | 21 | 13 | 62% | Local 缺 search/print-completed/permissions/audit-logs/consultations/prescriptions 等查询端点 |
| Registrations | 7 | 9 | 129% | Local 多便捷查询 |
| Reports | 8 | 3 | 38% | Local 仅 3 个 daily 聚合端点（趋势/绩效/排行/流量未实现） |
| Sync | 6 | 0 | — | 🧲 v2.0（N1 决策，v1.0 两库孤立） |
| Diagnostics | 4 | 7 | 175% | Local 多 db-info, logs/recent, version |
| Configuration | 5 | 4 | 80% | Local 少批量 PUT |
| Deploy | 2 | 2 | 100% | — |
| Backup | 8 | 8 | 100% | 双端共享 `BaseBackupController` + `IBackupService`（远程 SQL Server / 本地 LocalDB 同一引擎，B-06） |
| Health | 3 | 3 | 100% | — |
| **总计** | **~112** | **~107** | — | 2026-08-08 A-17 修复后核对 + 2026-09-22 B-06 备份/恢复双端 8 端点（不含 Sync；原表 113/112 为文档虚构，D8 修正） |

---

## LocalWebAPI 架构

### 进程内嵌 Kestrel

LocalWebAPI 是运行在 WPF Desktop 进程内的 ASP.NET Core Kestrel 实例，不是独立服务。

```text
LYBT.Desktop.Shell.exe (WPF 主进程)
  ├── WPF UI 线程 (Dispatcher)
  ├── Kestrel 后台线程 (LocalWebApiHost)
  │     └── http://127.0.0.1:{动态端口}/api/v1/...
  │           ├── Controllers (12 个)
  │           ├── AppDbContext (SQL Server LocalDB，复用 Server 实体配置)
  │           └── JWT 认证中间件 (简化版)
  └── Mutex (防多开)
```

**动态端口发现**: 启动时 Kestrel 绑定端口 0（OS 分配），实际端口写入 `IConnectionSettingsService`，SwitchingApiClient 据此路由。

**生命周期**: LocalWebAPI 随 Desktop 主进程启动/停止，无需独立管理。

### DbContext 架构

`AppDbContext`（LYBT.Infrastructure）双端共用，本地模式复用 Server 端所有 `IEntityTypeConfiguration`：

```csharp
// AppDbContext 复用 Server 端所有 IEntityTypeConfiguration
modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
```text

实体配置完全相同（共享程序集）；查询过滤器（IsDeleted 全局过滤器）完全相同。差异仅在连接字符串（LocalDB `(localdb)\MSSQLLocalDB`）和数据库名（LYBTDB_Local），迁移方式（`MigrateAsync()` + 双种子，与远程同一迁移链）。

> **注（2026-08-08 A-18 P1-7 修正）**: 原文档声称 `LocalWebApiDbContext`（独立 DbContext + EnsureCreated）已过时 — 本地实际使用 `AppDbContext` + `MigrateAsync()`（`LocalWebApiProgram.cs:47-48,139-141`）。`LYBT.Desktop.LocalData` 项目从未建立（仅 `LYBT.Desktop.Infrastructure/LocalData/Context/LocalDbContext.cs` 存在且生产零引用，休眠状态）。

> **注**: 原独立文档 `localwebapi/overview.md`、`localwebapi/authentication.md`、`localwebapi/api-endpoints.md` 的内容已合并到本文档。原文件保留作为详细参考。

---

## 同步架构（v2.0）

> 🧲 **v2.0 规划** — Sync 整体属 v2.0（N1 决策）。v1.0 远程与本地数据孤立，无同步能力。
>
> **完整同步协议规范**（Checksum 算法、元数据模型、序列化格式、依赖顺序、错误恢复、MedicalCase 聚合同步）已外移至 [17-sync-protocol.md](17-sync-protocol.md)。

---

## 已知实现缺口（I-6, 2026-08-19）

> **范围**：本节显式注记双模式文档与代码的已知差异，避免被后续轮次误判为新发现遗留。

| 功能 | 设计态（文档） | 代码现状 | 说明 |
|------|----------------|----------|------|
| **FirstRun 向导 5 步** | `desktop-ui-requirements.md §1.3` P0：改密→诊所信息→连接模式→创建 Admin→完成 | `FirstRunSetupView.xaml` 仅基础框架（Steps 1-2 可用，3-5 待完善）；`desktop-ui-requirements §七-1` 已标 ⚠️ | **B4 决策 I-4：推迟到 v2.0**（仅 Sysadmin 且可手动配置，v1.0 优先核心诊疗；`13-traceability-matrix` SHELL-011 已标 `🧲 v2.0`） |
| **Reports 趋势/绩效** | `desktop-ui-requirements §2.4` P1：收入/就诊/药材趋势 + 绩效排行 | `Reports` 仅 `daily/income/consultations/herbs` 3 端点（`ReportsController` 8→3）；`LocalWebAPI` 同裁剪（`05-dual-mode §端点覆盖率` 38% 已注） | **B4 决策 I-3：推迟到 v2.0**（`ReportsHomeView` 仅消费 3/8 日统计，5 趋势端点属锦上添花；`13-traceability-matrix` REPORT-004 已标 `🧲 v2.0`） |

---

### P07 例外：LocalWebAPI 统一服务层白名单（ADR-0010/0023）

架构规则 P07 要求 Server 模块间零引用；`LYBT.LocalWebAPI` 为**唯一例外**（见 ADR-0010 统一服务层、ADR-0023 白名单正名）。允许直接引用 `LYBT.Entities`/`LYBT.Infrastructure` 及 6 个 Server 模块（`Identity/Catalog/Patients/MedicalCases/Registrations/Reports`）以实现双模式行为 100% 复用。其余模块间仍零引用，`tests/LYBT.Tests.Architecture/ArchTests.P07` 显式豁免 LocalWebAPI（代码注释）。

## 架构决策记录

- [ADR-0023: LocalWebAPI 白名单（P07 例外正名）](decisions/0023-localwebapi-whitelist.md) — P07 唯一例外显式白名单
- [ADR-0010: LocalWebAPI 统一服务层](decisions/0010-localwebapi-unified-service-layer.md) — 保留复用，不解耦
- [ADR-0009: URL 驱动双模式架构](decisions/0009-url-driven-dual-mode.md) — 当前决策：嵌入式 Kestrel + URL 驱动切换 + SQL Server LocalDB
- [ADR-0002: 双模式架构](decisions/0002-dual-mode-architecture.md) — 历史决策（已被 ADR-0009 取代）
- [ADR-0028: 桌面 HTTP 传输层池化与弹性](decisions/0028-desktop-http-resilience.md) — IHttpClientFactory + Polly（2026-09-16）
- [ADR-0029: 桌面 GET 响应缓存策略](decisions/0029-desktop-response-cache.md) — 进程内 IMemoryCache + 传输层读写一体（2026-09-16）

---

## 变更记录

| 日期 | 版本 | 变更内容 |
| ------ | ------ | ---------- |
| 2026-09-22 | v8.2 | **B-06 备份/恢复双端对齐**：端点覆盖表新增 `Backup` 行（8/8 = 100%，双端共享 `BaseBackupController` + `IBackupService`——远程对 SQL Server、本地对 LocalDB 同一引擎），总计 ~104/~99 → ~112/~107；「端点覆盖需手动对齐」代价行同步（~95% → ~96%） | B-06 交付：备份/恢复不再 Desktop 专用，双端同路由 8 端点 |
| 2026-08-08 | v8.1 | **13 项文档偏差修正（A-18 P1-7，D5-D10）**：URL 前缀统一 `/api/v1/`；迁移方式 EnsureCreated→MigrateAsync+双种子；DI 架构「Controller→DbContext 直连」→「复用 Server Service/Handler 层（ADR-0010）」；端点覆盖表按代码实际重写（103 vs 99，删虚构 categories/by-phone 端点）；打印日志行为「返回空结果」→「404（端点不存在）」；Rate Limiting 本地 5/60s；实体位置 `src/Shared/LYBT.Entities/`；DbContext 章节 LocalWebApiDbContext→AppDbContext |
| 2026-06-28 | v8.0 | **spec S3 批次2 提炼（712→~360 行）**：同步架构 + 同步协议规范（Checksum/元数据/序列化/依赖顺序/错误恢复/MedicalCase 聚合同步/模块级决策）整体外移至 [17-sync-protocol.md](17-sync-protocol.md)；WebAPI vs LocalWebAPI 对比矩阵 + 本地认证架构 + DbContext 架构 + 本地模式限制 4 表合 1；N1 横幅简化为链接指向 sync-protocol。变更历史见 git log。 |
| 2026-06-28 | v7.2 | N1 决策对齐：顶部加 N1 横幅；端口统一 5300；模式切换流程图重写为 ADR-0009「URL 改即生效」语义；Policy 数量 2→4 对齐 PolicyConstants。 |
