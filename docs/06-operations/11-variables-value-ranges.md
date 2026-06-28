# 变量值域与默认值

## JWT 配置

| 配置项 | 类型 | 默认值 | 值域 | 说明 |
|--------|------|--------|------|------|
| `Jwt:SecretKey` | string | — | ≥32 字符 | JWT 签名密钥，生产必须修改 |
| `Jwt:Issuer` | string | `LYBT.WebAPI` | 任意字符串 | 签发者标识（appsettings.json 实际值） |
| `Jwt:Audience` | string | `LYBT.Client` | 任意字符串 | 接收者标识（appsettings.json 实际值） |
| `Jwt:AccessTokenExpirationMinutes` | int | `60`（硬编码） | 5-1440（v2.0） | AccessToken 有效期（分钟）。当前由 `AuthController.cs` 硬编码 `AddMinutes(60)`，**未在 appsettings 暴露**，可配置化属 v2.0 待办 |

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
| `ClientSession:WarningBeforeTimeoutMinutes` | int | `0` | 0-10 | 超时前预警（分钟） |

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
