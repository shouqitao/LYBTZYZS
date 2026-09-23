# 运维文档

> **用户速览**：给运维人员看的。部署、配置、监控、备份都在这里。
>
> **权威文档在哪**：部署 → [01-deployment.md](01-deployment.md)；配置项 → [02-configuration.md](02-configuration.md)。完整查询指南见 [docs/README.md](../README.md#ai-查询指南)。

## 部署架构

```
[Desktop Client (WPF)]
     │
     ├── 远程 ──→ [LYBT.WebAPI] ──→ [SQL Server]
     │              (Kestrel :5000)
     │
     └── 本地 ──→ [LocalWebAPI] ──→ [LocalDB]
                    (Kestrel :5300)
```

## 文档索引

| # | 文档 | 一句话说明 |
|---|------|-----------|
| 01 | [部署指南](01-deployment.md) | 服务端+客户端+数据库部署（含 Windows 补充与 Newman 验证） |
| 02 | [配置管理](02-configuration.md) | 完整配置项说明 |
| 03 | [故障排查](03-troubleshooting.md) | 错误日志定位与修复 |
| 04 | [开发环境规格](04-development-environment-spec.md) | 环境要求 |
| 05 | [API 测试](05-api-tests.md) | 测试用例 |
| 06 | [备份恢复](06-backup-recovery.md) | 备份策略+灾难恢复 |
| 07 | [监控告警](07-monitoring-alerting.md) | 监控指标+告警规则 |
| 08 | [部署回滚](08-deployment-rollback.md) | 回滚策略+决策矩阵 |
| 09 | [变量与密钥](09-variables-secrets.md) | 配置变量清单 |
| 10 | [变量值域](10-variables-value-ranges.md) | 取值范围+默认值 |
| 11 | [服务器配置参考](11-server-config-reference.md) | 生产环境速查 |
| 12 | [Desktop 发布流程](12-desktop-release.md) | Velopack 打包与自动更新 |

## 日志系统

| 目标 | 级别 | 说明 |
|------|------|------|
| Console | Info+ | 开发调试 |
| File | Info+ | 本地日志（30天轮转 + 按月归档 zip） |
| SQL Server | Warn+ | 数据库持久化 |

日志路径：`logs/lybt-web-api-{date}.log`

## 健康检查

| 端点 | 权限 | 说明 |
|------|------|------|
| `GET /health` | 匿名 | 探活 |
| `GET /health/database` | 匿名 | 数据库检查（仅 Server） |
| `GET /api/v1/health` | 匿名 | 业务健康检查 |
| `GET /api/v1/health/ping` | 匿名 | Ping/Pong |
| `GET /api/v1/health/details` | 已认证 | 详细检查 |
