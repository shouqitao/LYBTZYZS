# ADR-0002: 双模式架构 (远程 + 本地)

> **⚠️ 已废弃**: 本文档描述的 SQLite 本地模式已被 SQL Server LocalDB + 嵌入式 LocalWebAPI (Kestrel) 替代。原始决策中的策略模式 + ConnectionMode 运行时切换已在 2026-05 架构简化中移除。当前架构详见 [dual-mode.md](../dual-mode.md)。

**状态**: 已取代 (Superseded)
**日期**: 2026-02-01
**取代说明**: 原始决策中的策略模式 + ConnectionMode 运行时切换已在 2026-05 架构简化中移除。当前架构采用 Remote (Refit HTTP) + LocalWebAPI (嵌入式 Kestrel) 双模式，无运行时切换。详见 [dual-mode.md](../dual-mode.md)。

## 背景

中医诊所可能面临网络不稳定的场景，需要支持离线诊疗。同时系统也需要支持多用户在线模式。

## 决策

采用策略模式实现双模式:
- **远程模式**: WPF -> HTTP API -> SQL Server (多用户、在线)
- **本地模式**: WPF -> LocalDbContext -> SQLite (单用户、离线)

### 核心机制
- 定义统一 IDataSource 接口
- RemoteXxxDataSource 和 LocalXxxDataSource 实现相同接口
- 通过 appsettings.json `ConnectionMode` 配置，启动时 DI 注册切换
- 业务层代码完全无感知模式差异

### 本地认证
- LocalAuthService 提供 BCrypt 本地验证
- 不依赖 JWT Token

### 数据同步
- SyncService 提供双向同步 (SHA256 Checksum 比对)
- 支持 Herb、Patient、Formula 实体同步
- 冲突需用户手动解决

## 已确定的决策

以下事项已基于代码逆向分析确定:

1. **本地模式功能矩阵**: 全模块完整支持 (6 个 LocalDataSource 100% 方法覆盖)
2. **同步冲突解决**: 手动逐条选择 (SyncConflictDialog)
3. **MedicalCase 同步**: v1.0 不支持，后续版本规划
4. **User 同步**: v1.0 不支持，初始化时下载
5. **SQLite 加密**: v1.0 不加密，依赖 OS 权限

详见 `docs/plans/2026-02-10-requirements-deepening-design.md`

## 后续演变

| 日期 | 变更 |
|------|------|
| 2026-02-01 | 初始实现，支持手动切换 |
| 2026-02-04 | 增加 SyncService 双向同步 |
| 2026-03-08 | SQLite 迁移至 SQL Server |
| 2026-03-09 | Sprint 6: 实现运行时切换 (SYNC-D03) |
| 2026-05-01 | **架构简化**: 移除 ConnectionMode 运行时切换、移除遗留 Local 仓储、统一为 Remote + LocalWebAPI |
