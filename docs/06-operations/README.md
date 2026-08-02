# 运维文档

> **用户速览**：给运维人员看的。部署、配置、监控、备份都在这里。

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
| 01 | [部署指南](01-deployment.md) | 服务端+客户端+数据库部署 |
| 02 | [配置管理](02-configuration.md) | 完整配置项说明 |
| 03 | [WebAPI 部署摘要](03-webapi-deployment-summary.md) | 快速部署参考 |
| 04 | [Windows 部署](04-windows-deployment.md) | Windows 环境部署 |
| 05 | [开发环境规格](05-development-environment-spec.md) | 环境要求 |
| 06 | [API 测试](06-api-tests.md) | 测试用例 |
| 07 | [备份恢复](07-backup-recovery.md) | 备份策略+灾难恢复 |
| 08 | [监控告警](08-monitoring-alerting.md) | 监控指标+告警规则 |
| 09 | [部署回滚](09-deployment-rollback.md) | 回滚策略+决策矩阵 |
| 10 | [变量与密钥](10-variables-secrets.md) | 配置变量清单 |
| 11 | [变量值域](11-variables-value-ranges.md) | 取值范围+默认值 |
| 12 | [部署流程](12-deployment-flow.md) | 远程/本地部署步骤 |
| 13 | [服务器配置参考](13-server-config-reference.md) | 生产环境速查 |
| 14 | [测试环境](14-deployment-test-environment.md) | 测试环境配置 |

## 日志系统

| 目标 | 级别 | 说明 |
|------|------|------|
| Console | Info+ | 开发调试 |
| File | Info+ | 本地日志（30天轮转） |
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
