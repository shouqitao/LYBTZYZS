# 变量值域与默认值
> 版本: v1.0 | 日期: 2026-08-20

## JWT 配置

| 配置项 | 类型 | 默认值 | 值域 | 说明 |
|--------|------|--------|------|------|
| `Jwt:SecretKey` | string | — | ≥32 字符 | JWT 签名密钥，生产必须修改 |
| `Jwt:Issuer` | string | `LYBT.WebAPI` | 任意字符串 | 签发者标识（appsettings.json 实际值） |
| `Jwt:Audience` | string | `LYBT.Client` | 任意字符串 | 接收者标识（appsettings.json 实际值） |
| `Jwt:AccessTokenExpirationMinutes` | int | `480`（base） | 5-1440（v2.0） | AccessToken 有效期（分钟）。从 `JwtOptions` 配置读取（`JwtService.cs:110`）：base 480/Dev·Test 60/Prod 30。环境覆盖见 `appsettings.{Environment}.json` |

## 安全配置

| 配置项 | 类型 | 默认值 | 值域 | 说明 |
|--------|------|--------|------|------|
| `Security:AccountLockout:Enabled` | bool | `true` | true/false | 是否启用账户锁定（设计扩展，未在 appsettings 暴露） |
| `Security:AccountLockout:MaxFailedCount` | int | `5` | 3-20 | 最大失败次数（设计扩展，未在 appsettings 暴露） |
| `Security:AccountLockout:LockoutMinutes` | int | `15` | 5-60 | 锁定时长（分钟）（设计扩展，未在 appsettings 暴露） |
| `Security:RateLimiting:Login:PermitLimit` | int | `5` | 3-20 | 登录限流（次/分钟） |

## 默认密码

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `DefaultPasswords:AdminPassword` | `Admin@123456` | Admin 初始密码，首次登录强制修改 |
| `DefaultPasswords:SysAdminPassword` | `SysAdmin@2026!` | Sysadmin 初始密码 |

## 会话配置

| 配置项 | 类型 | 默认值 | 值域 | 说明 |
|--------|------|--------|------|------|
| `Session:TimeoutMinutes` | int | `120` | 5-480 | Server WebAPI 会话超时（分钟） |
| `ClientSession:InactivityTimeoutMinutes` | int | `30` | 5-120 | Desktop Shell 不活动超时（分钟，appsettings.json 实际键） |
| `ClientSession:WarningBeforeTimeoutMinutes` | int | `2` | 0-10 | 超时前预警（分钟）——大于 0 时 Shell 在剩余时间进入该窗口弹出会话超时提醒（「续期」/「退出」）；0 = 关闭预警（2026-09-23 由 0 改为 2） |

## 数据库配置

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `ConnectionStrings:DefaultConnection` | — | 远程 SQL Server 连接串 |
| `DatabaseOptions:ConnectionString` | — | LocalDB 连接串 |
| `DatabaseOptions:CommandTimeout` | `30` | 命令超时（秒） |

## 功能开关

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `FeatureToggles:OverwriteConflicts` | `true` | 同步冲突时覆盖本地 |
| `FeatureToggles:DuplicateHerbMergeStrategy` | `Skip` | 药材重复策略（Skip/Update/Error） |

## 日志配置

| 配置项 | 默认值 | 值域 | 说明 |
|--------|--------|------|------|
| `Serilog:MinimumLevel:Default` | `Information` | Verbose-Off | 日志级别 |
| `Logging:LogLevel:Default` | `Warning` | Trace-None | 框架日志级别 |
| `Logging:Cleanup:Enabled` | `true` | bool | 数据库 SystemLog 清理开关（`LogCleanupService`） |
| `Logging:Cleanup:RetentionDays` | `90` | 1-365 | 保留天数（Error/Fatal 永久保留） |
| `Logging:Cleanup:CleanupIntervalHours` | `24` | 1-168 | 清理间隔 |
| `Logging:Cleanup:InitialDelayMinutes` | `5` | 1-60 | 首次执行延迟 |
| `Logging:Cleanup:BatchSize` | `1000` | 100-10000 | 每批删除条数 |
| `Logging:Archive:Enabled` | `true` | bool | 文件日志归档开关（`LogArchiveService`，F-08） |
| `Logging:Archive:ArchiveAfterDays` | `7` | 1-365 | 仅归档该天数之前的文件 |
| `Logging:Archive:ArchiveDirectory` | `archive` | 相对路径 | 归档子目录（相对 `LogDirectory`） |
| `Logging:Archive:DeleteSourceAfterArchive` | `true` | bool | 归档成功后删除源文件 |
| `Logging:Archive:IntervalHours` | `24` | 1-168 | 归档间隔 |
| `Logging:Archive:InitialDelayMinutes` | `10` | 0-60 | 首次执行延迟 |
| `Logging:Archive:FilePrefixes` | `["lybt-web-api","bootstrap"]` | 非空数组 | 参与归档的日志文件名前缀 |
| `Logging:Archive:LogDirectory` | `logs` | 路径 | 日志目录（相对路径按进程工作目录解析，与 Serilog File sink 同基准） |

> 归档仅处理**已结束月份**且超过 `ArchiveAfterDays` 天的文件；当前月与仍在写入的文件跳过。节名是 `Logging`（不是 `Lybt:Logging`）。
