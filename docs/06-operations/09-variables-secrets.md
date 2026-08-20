# Variables — 配置与密钥

## Configuration Variables

| Name | Used By | Scope | Source | Risk |
|------|---------|-------|--------|------|
| `ConnectionStrings:DefaultConnection` | Server | Server | appsettings.json | 中 — DB 连接串 |
| `Jwt:SecretKey` | Server | Server | appsettings.json | **高 — JWT 签名密钥** |
| `Jwt:Issuer` | Server | Server | appsettings.json | 低 |
| `Jwt:Audience` | Server | Server | appsettings.json | 低 |
| `Jwt:AccessTokenExpirationMinutes` | Server | Server | 代码硬编码（非 appsettings） | 低 — Token 有效期，当前硬编码 60 分钟，可配属 v2.0 |
| `Security:AccountLockout:MaxFailedCount` | Server | Server | 设计扩展（未在 appsettings 暴露） | 低 — 锁定阈值 |
| `Security:AccountLockout:LockoutMinutes` | Server | Server | 设计扩展（未在 appsettings 暴露） | 低 — 锁定时长 |
| `DefaultPasswords:AdminPassword` | Server | Server | appsettings.json | **高 — 默认密码** |
| `DefaultPasswords:SysAdminPassword` | Server | Server | appsettings.json | **高 — 默认密码** |
| `ClinicSettings:*` | Server | Server | appsettings.json | 中 — 诊所信息 |
| `FeatureToggles:*` | Server | Server | appsettings.json | 低 — 功能开关 |

## Secrets (Must Not Be Bundled Client-Side)

| Secret | Location | Rotation |
|--------|----------|----------|
| JWT SecretKey | Server appsettings.json | 手动，影响所有在线用户 |
| DB Connection String | Server appsettings.json | 手动 |
| Default Passwords | Server appsettings.json | 首次部署后修改 |

**Desktop 端无密钥**：Desktop 通过 Refit HTTP 调用 Server API，不直接持有任何密钥。Token 存储在内存中，进程退出自动清除。

## Pre-Go-Live Checklist

- [ ] 修改 `DefaultPasswords:AdminPassword` 和 `DefaultPasswords:SysAdminPassword`
- [ ] 生成新的 `Jwt:SecretKey`（至少 32 字符随机字符串）
- [ ] 确认 `ConnectionStrings:DefaultConnection` 指向生产数据库
- [ ] 确认 HTTPS 配置（生产环境强制 HTTPS）
- [ ] 确认 `SecurityOptions.AccountLockout` 阈值合理
- [ ] 确认日志级别（生产环境 Warning+）
- [ ] 确认备份策略（数据库 + 配置文件）

## Environment Modes

| 模式 | 端口 | 数据库 | 说明 |
|------|:---:|--------|------|
| 远程 | 5000 | SQL Server (LYBTDB_Dev) | 生产/开发环境 |
| 本地 | 5300 | LocalDB (LYBTDesktop) | 嵌入式 LocalWebAPI（生产实际值） |
| 测试 | 随机 | InMemory / LocalDB | 单元/集成测试 |

> LocalWebAPI **独立调试端口**为 5290（`LocalWebAPI/Program.cs`），**仅独立运行时生效，嵌入模式不监听**。嵌入模式统一走 5300。
