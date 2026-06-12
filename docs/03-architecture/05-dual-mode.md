# URL 驱动连接切换架构

## 概述

系统通过 URL 驱动的方式自动选择连接目标，不需要手动选择"模式"。

| URL 类型 | 客户端实现 | 目标服务 | 数据库 | 适用场景 |
|----------|-----------|---------|--------|----------|
| **非 localhost** | RefitApiClient | Server WebAPI | SQL Server (远程) | 多用户联网环境 |
| **127.0.0.1 / localhost** | HttpClientApiClient | 嵌入式 Kestrel | SQL Server (本地) | 单用户离线 |

用户通过状态栏的"连接设置"弹出面板输入 URL，`SwitchingApiClient` 代理自动路由到对应的底层实现。Repository 层完全无感知。

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

## 架构图

```mermaid
graph TB
    subgraph VM["ViewModel 层"]
        BL["ViewModel / Service"]
    end

    subgraph REPO["Repository 层"]
        IRepo["IXxxRepository 接口<br>(Contracts/Repositories/)"]
    end

    BL --> IRepo

    subgraph Remote["远程模式"]
        RemoteRepo["XxxRepository<br>(Refit HTTP 客户端)"]
        API["WebAPI Server"]
        SQL["SQL Server"]
        RemoteRepo --> API --> SQL
    end

    subgraph LocalWebAPI["嵌入式本地 WebAPI 模式"]
        HttpRepo["HttpXxxRepository<br>(HTTP Proxy)"]
        Kestrel["LocalWebApiHost<br>(嵌入式 Kestrel)"]
        SQL2["SQL Server"]
        HttpRepo -->|"http://127.0.0.1:{port}"| Kestrel
        Kestrel --> Controllers["LocalWebAPI Controllers"]
        Controllers --> DbContext["LocalWebApiDbContext"]
        DbContext --> SQL2
    end

    IRepo --> RemoteRepo
    IRepo --> HttpRepo
```

## 统一 IApiClient 抽象

所有 Repository 依赖统一的 `IApiClient` 接口，不再区分"远程实现"和"本地实现"。`SwitchingApiClient` 代理根据当前 URL 自动路由。

| 接口 | IApiClient 子接口 | 说明 |
|------|------------------|------|
| IPatientRepository | IApiClient.Patients | CRUD + 批量操作 |
| IHerbRepository | IApiClient.Herbs | CRUD + 批量操作 + 分类查询 |
| IFormulaRepository | IApiClient.Formulas | CRUD + 克隆 + 批量操作 + 分类查询 |
| IMedicalCaseRepository | IApiClient.MedicalCases | CRUD + 状态流转 + 处方 |
| IUserRepository | IApiClient.Users | CRUD + 密码管理 + 批量操作 |
| IRegistrationRepository | IApiClient.Registrations | CRUD + 队列管理 |

DI 注册在 `UnifiedApiClientExtensions.cs` 中完成：始终注册 `SwitchingApiClient` 为 `IApiClient` Singleton。

### SwitchingApiClient 代理

```
SwitchingApiClient : IApiClient
  ├── _remoteApi (RefitApiClient)     ← 非 localhost 时使用
  └── _localApi  (HttpClientApiClient) ← localhost/127.0.0.1 时使用
```

- 每次属性访问（如 `_apiClient.Patients`）实时读取 `IConnectionSettingsService.CurrentUrl`
- URL 变更时自动重建底层客户端
- Repository 层零改动

## 本地模式功能覆盖率

| 模块 | 远程端点 (Remote) | 本地端点 (LocalWebAPI) | 覆盖率 |
|------|-----------------|----------------------|-------|
| Auth | 5 | 5 | 100% |
| Users | 14 | 14 | 100% |
| Patients | 11 | 11 | 100% |
| Herbs | 17 | 17 | 100% |
| Formulas | 15 | 15 | 100% |
| MedicalCases | 19 | 19 | 100% |
| Registrations | 7 | 7 | 100% |
| Sync | 6 | 6 | 100% |
| Diagnostics | 4 | 4 | 100% |
| Configuration | 1 | 1 | 100% |
| Health | 3 | 3 | 100% |
| **总计** | **~102** | **~102** | **~100%** |

### 本地模式独有端点

部分端点仅在 LocalWebAPI 模式下可用，用于增强离线使用体验：

| 模块 | 端点 | 方法 | 说明 |
|------|------|------|------|
| Formulas | /api/formulas/{id}/clone | POST | 克隆验方（含药材组成） |
| Formulas | /api/formulas/categories | GET | 获取验方分类列表 |
| Patients | /api/patients/by-id-number/{idNumber} | GET | 按身份证号查询患者 |
| MedicalCases | /api/medicalcases/pending | GET | 获取待处理医案（无处方） |
| MedicalCases | /api/medicalcases/by-status/{status} | GET | 按状态查询医案 |
| Diagnostics | /api/diagnostics/db-info | GET | 数据库连接信息 |
| Diagnostics | /api/diagnostics/version | GET | 程序集版本信息 |
| Diagnostics | /api/diagnostics/logs/recent | GET | 最近日志 |

**设计说明**: 这些端点满足本地单用户场景下的便捷需求（如快速克隆验方、身份证号查询、系统诊断），不要求远程模式实现。

### 限制与排除 (TBD-01)

部分对服务端有强依赖的功能在本地模式下不可用，调用时返回 `501 Not Implemented` 或空结果：
- **Token 刷新**: 本地模式使用长效 Token (1年)，不支持 Refresh Token 轮换。
- **审计日志查询**: 本地暂不持久化系统级审计日志。
- **自动登录令牌同步**: 自动登录依赖远程中心化存储。
- **用户同步**: 用户数据不参与双向同步，仅手动维护。

---

## LocalWebAPI 架构

详见 [localwebapi/overview.md](localwebapi/overview.md)。

---

## 同步架构

### 同步流程

```mermaid
sequenceDiagram
    participant User as 用户
    participant VM as SyncViewModel
    participant Sync as SyncService
    participant Local as LocalDbContext
    participant API as ISyncApi

    User->>VM: 点击"检查差异"
    VM->>Sync: CheckDifferencesAsync()
    Sync->>Local: 获取本地元数据
    Sync->>API: GetMetadataAsync()
    Sync-->>VM: 差异列表 (LocalOnly/ServerOnly/Conflict)

    User->>VM: 选择同步项目，解决冲突
    VM->>Sync: ExecuteSyncAsync()

    Sync->>API: UploadAsync() (本地 -> 服务端)
    Sync->>API: DownloadAsync() (服务端 -> 本地)
    Sync->>Local: 保存下载数据
    Sync-->>VM: SyncExecutionResult
```

### SyncService 核心操作

| 操作 | 说明 |
|------|------|
| CheckDifferencesAsync | 比对本地与服务端元数据，分类为 LocalOnly/ServerOnly/Conflict |
| UploadAsync | 序列化本地实体为 JSON，上传到服务端 |
| DownloadAsync | 从服务端下载实体 JSON，存入本地数据库 |
| ExecuteSyncAsync | 完整同步流程: 处理上传列表 + 下载列表 + 冲突解决 |

### 同步依赖顺序

| 顺序 | 下载 (Server->Local) | 上传 (Local->Server) | 原因 |
|------|---------------------|---------------------|------|
| 1 | Herb | Herb | Formula 子项引用 HerbId |
| 2 | Patient | Patient | MedicalCase 引用 PatientId |
| 3 | Formula | Formula | 依赖 Herb 已存在 |
| (未实现) | MedicalCase | MedicalCase | 聚合级，依赖 Patient + Herb |

### 支持同步的实体类型

| 实体 | 同步支持 |
|------|----------|
| Herb | 支持 |
| Patient | 支持 |
| Formula | 支持 (含 FormulaHerbItems) |
| MedicalCase | 仅同步 Completed 状态 (SYNC-D01)。聚合级原子同步 |
| User | v1.0 不支持 |

### 冲突解决

| 实体类型 | 策略 | 说明 |
|---------|------|------|
| Herb / Patient / Formula | Server Wins | 自动覆盖 |
| MedicalCase | 手动选择 | 保留冲突对比 UI |

---

## 架构决策记录

- [ADR-0002: 双模式架构](decisions/0002-dual-mode-architecture.md) — 远程 + 本地双模式架构演进历史 (原始 SQLite 策略已被 LocalWebAPI 替代)

### 模块级决策

| 编号 | 决策 | 状态 | 说明 |
|------|------|------|------|
| SYNC-D01 | MedicalCase 同步范围 | 已确认 | 仅同步 Completed 状态 |
| SYNC-D02 | 统一本地/远程数据路径 | **已实施** | 废除 DataSource 抽象层，使用 Repository 双实现 |
| SYNC-D03 | 运行时模式切换 | **已移除** | 被 URL 驱动连接切换替代 (URL-CONN-01) |
| URL-CONN-01 | URL 驱动连接切换 | **已实施** | SwitchingApiClient 代理 + IConnectionSettingsService，用户通过 UI 输入 URL 即时切换 |
| SYNC-D04 | 冲突解决策略 | 已确认 | 简单实体 Server Wins; MedicalCase 手动选择 |
| LOCALWEB-01 | 使用 SQL Server 作为本地数据库 | 已实施 | 与远程模式保持一致，简化数据同步 |
| LOCALWEB-02 | 嵌入 Kestrel 在 WPF 进程内 | 已实施 | 避免独立进程管理复杂度 |
| LOCALWEB-03 | 简化 JWT 认证 (无刷新) | 已实施 | 本地模式无需复杂认证流 |
| LOCALWEB-04 | HTTP Proxy Repository 模式 | 已实施 | 统一 Repository 接口 |
| TBD-01 | 本地模式功能受限范围 | 已确定 | 不可用项: 自动登录/Token刷新/审计日志查询/User同步/服务端API导入导出 |

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-22 | v2.0 | 架构演进: 新增 SYNC-D01~D04 决策 |
| 2026-03-08 | v2.1 | LocalDB 迁移: SQLite -> SQL Server |
| 2026-03-09 | v2.2 | v1.0-rc 状态同步 |
| 2026-03-09 | v3.0 | **Sprint 6 完成**: SYNC-D02 DataSource 废除 + SYNC-D03 运行时切换已实施 |
| 2026-07-01 | v4.0 | **LocalWebAPI 覆盖率提升**: 覆盖率从 ~78% 提升至 ~100% |
| 2026-05-01 | v5.0 | **架构简化**: 移除运行时模式切换 (ConnectionMode)、移除遗留 Local 仓储、统一为 Remote + LocalWebAPI 双模式 |
| 2026-06-08 | v6.0 | **URL 驱动连接切换**: 废弃 ApiMode 枚举，引入 SwitchingApiClient 代理 + IConnectionSettingsService，用户通过 UI 输入 URL 即时切换 |
