# ADR-0009: URL 驱动双模式架构 (Remote WebAPI + LocalWebAPI)

**状态**: Accepted
**日期**: 2026-06-13
**取代**: ADR-0002 (双模式架构 — SQLite + 策略模式)
**关联**: [05-dual-mode.md](../05-dual-mode.md)

## 背景

中医诊所管理系统需要同时满足两种部署场景：

1. **多终端联网** — 诊所配备 1-5 台终端（前台+医生），通过局域网连接共享数据
2. **单终端离线** — 偏远地区或网络不稳定环境，需要完全离线工作

ADR-0002 采用 SQLite + 策略模式 + 运行时 ConnectionMode 切换的方案。该方案在实践中暴露了以下问题：

| 问题 | 影响 |
|------|------|
| SQLite 与 SQL Server 方言差异 | 同一 LINQ 查询在不同数据库行为不一致（排序、日期、NULL 处理） |
| 两套独立的数据访问实现 | LocalDataSource 与 RemoteDataSource 代码重复，维护成本高 |
| 运行时模式切换复杂 | ConnectionMode 枚举 + DI 重建 + 状态同步，引入多个竞态条件 |
| 本地认证完全独立 | 无法复用 JWT/Claims/Authorization Policy 架构 |

## 考虑的替代方案

### 方案 A: 直接注入 DbContext（无 HTTP 层）

本地模式绕过 HTTP，Desktop ViewModel 直接注入 `AppDbContext`。

| 优势 | 劣势 |
|------|------|
| 性能最优（无 HTTP 序列化开销） | Repository 接口分裂：`IXxxRepository` 有 HTTP 和 EF 两套实现 |
| 代码简单直接 | Authorization Policy/Filter 无法复用（它们是 HTTP 中间件） |
| | EF Core 跟踪行为与 HTTP 无状态模式不一致 |
| | 本地模式需重新实现认证/授权/异常处理/日志管线 |

**否决理由**: HTTP 中间件管线（认证、授权、异常处理、日志、CorrelationId）是架构核心资产。直接注入 DbContext 会绕过这些管线，导致本地模式安全/审计能力缺失，且需要维护两套业务逻辑路径。

### 方案 B: 独立本地服务进程

本地模式启动一个独立的控制台/Windows Service 作为 LocalWebAPI。

| 优势 | 劣势 |
|------|------|
| 进程隔离，稳定性好 | 用户需管理两个进程（Desktop + Service） |
| 可复用 Server WebAPI 代码 | 安装/卸载复杂度增加 |
| | 进程间通信仍有 HTTP 开销 |

**否决理由**: 小诊所无 IT 运维能力。双进程管理增加部署复杂度和故障点，违背"单机即用"的设计目标。

### 方案 C: 共享 Controller 项目（选中方案的变体）

将 Server Controllers 和 LocalWebAPI Controllers 合并为一个共享项目，通过条件编译或配置区分。

| 优势 | 劣势 |
|------|------|
| 消除 Controller 代码重复 | Controller 依赖差异大（Server 用 Repository + Service，Local 用 DbContext 直连） |
| | DI 注册路径不同（Server: 3-layer DI; Local: 精简 DI） |
| | 条件编译增加维护复杂度 |

**否决理由**: Server 和 LocalWebAPI 的依赖链不同（Server 有完整 Service/Repository/CQRS 层，Local 直接 Controller→DbContext）。强行合并引入条件分支，比保持两套独立但清晰的 Controller 更难维护。

## 决策

**采用 URL 驱动的嵌入式 LocalWebAPI 方案。**

### 核心设计

```
Desktop (WPF/Prism)
  ├── Repository 层（统一接口，零感知模式）
  │     └── SwitchingApiClient : IApiClient (代理)
  │           ├── 非 localhost → RefitApiClient → Server WebAPI → 远程 SQL Server
  │           └── localhost    → HttpClientApiClient → LocalWebAPI (Kestrel) → 本地 LocalDB
```

### 关键设计选择

| 决策点 | 选择 | 理由 |
|--------|------|------|
| 本地数据库 | SQL Server LocalDB | 与远程 SQL Server 方言一致，消除跨数据库行为差异 |
| 本地 API 宿主 | 嵌入式 Kestrel（进程内） | 单进程部署，无需管理外部服务 |
| 模式切换机制 | URL 驱动（localhost 判断） | 零配置切换，用户改 URL 即可 |
| Repository 统一层 | SwitchingApiClient 代理 | Repository 层完全无感知，业务代码零改动 |
| 认证复用 | 两端均用 JWT Bearer Token | Authorization Policy/Claims/中间件管线完整复用 |

### 设计权衡

**接受的代价**:

| 代价 | 缓解措施 |
|------|----------|
| 本地模式有 HTTP 序列化开销 | localhost 回环延迟 <1ms，小诊所数据量下不可感知 |
| 两套 Controller 代码 | Controller 逻辑精简，核心业务规则在共享层（Entities/Validators/DTOs） |
| LocalWebAPI 认证简化（无 Refresh） | 本地单用户场景，1 年长效 Token + Mutex 单实例，安全风险可控 |

**获得的收益**:

| 收益 | 说明 |
|------|------|
| 业务代码 100% 复用 | ViewModel/Service/Repository 零改动 |
| HTTP 管线完整复用 | 认证、授权、异常处理、日志、CorrelationId 全部生效 |
| 数据库行为一致 | 两端均为 SQL Server，LINQ 查询行为完全一致 |
| 部署极简 | Desktop 单进程，LocalWebAPI 自动启停 |
| 模式切换无感 | URL 变更 → SwitchingApiClient 自动路由，无需重启 |

## 后果

### 正面

- Repository 接口统一：6 个 `IXxxRepository` 接口 + `SwitchingApiClient` 代理，新增实体只需扩接口
- 测试简化：Server 测试用真实 SQL Server，Desktop 测试用 LocalWebAPI，不再需要 mock 数据层
- 维护成本降低：消除 LocalDataSource/RemoteDataSource 双实现，消除 ConnectionMode 状态机

### 负面

- LocalWebAPI Controller 与 Server Controller 存在逻辑重复（虽依赖链不同）
- 本地 JWT 固定密钥（`LYBT-LocalWebAPI-Secret-Key-2024`）在代码中硬编码
- 端点覆盖需手动保持一致（当前 ~100%，差异 8 个本地独有端点）

### 后续演进方向

1. **Controller 共享**：若 LocalWebAPI Controller 增多，可考虑提取共享业务逻辑到 Shared 项目
2. **本地密钥外部化**：将固定密钥改为 DPAPI 加密的机器级密钥
3. **端点对齐自动化**：添加编译时检查确保两端端点覆盖一致
