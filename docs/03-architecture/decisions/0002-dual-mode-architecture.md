# ADR-0002: 双模式架构 (远程 + 本地)

**状态**: 已采纳
**日期**: 2026-02-01

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
3. **MedicalCase 同步**: v1.0 不支持，v2.0 规划
4. **User 同步**: v1.0 不支持，初始化时下载
5. **SQLite 加密**: v1.0 不加密，依赖 OS 权限

详见 `docs/plans/2026-02-10-requirements-deepening-design.md`

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-02-01 | 初始实现，支持手动切换 |
| 2026-02-04 | 增加 SyncService 双向同步 |
