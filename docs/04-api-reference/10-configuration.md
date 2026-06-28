# 系统配置 API

> Controller: `ConfigurationController` | 路由前缀: `/api/v1/configuration` | 默认权限: `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`

## 概述

提供系统配置读取与生产环境配置验证功能。仅 Admin 和 SuperAdmin 可访问。GetConfiguration 返回安全、非敏感的配置项；GetValue 按 key 查询单个配置值；ValidateProduction 验证生产环境配置是否完整合规。

> **响应信封**：所有响应为 `ApiResponse<T>`，字段定义与通用错误码见 [README](README.md)。下文成功响应示例仅展示 `data` 内容。

---

## GET /configuration

获取系统配置项集合。

- **权限**: Admin / SuperAdmin (`[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`)

**成功响应** (200): `ApiResponse<Dictionary<string, string?>>`

```json
{
  "App:Name": "凌隐宝堂中医诊所管理系统",
  "App:Version": "1.0.0",
  "Jwt:SecretKey": "***",
  "Jwt:Issuer": "LYBTZYZS",
  "Jwt:Audience": "LYBTZYZS-Client",
  "Jwt:ExpireMinutes": "60",
  "ConnectionStrings:DefaultConnection": "Server=localhost;Database=LYBTDB_Dev;Trusted_Connection=True;",
  "Logging:LogLevel:Default": "Information",
  "Logging:LogLevel:Microsoft": "Warning",
  "DefaultPasswords:Admin": "***",
  "DefaultPasswords:SysAdmin": "***"
}
```

**curl 示例：**

```bash
curl -X GET http://localhost:5000/api/v1/configuration \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 说明 |
|------------|------|
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## GET /configuration/{key}

获取单个配置项的值。

- **权限**: Admin / SuperAdmin (`[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`)

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `key` | string | 配置项名称 (如 `App:Name`) |

**成功响应** (200): `ApiResponse<string?>`

```json
"凌隐宝堂中医诊所管理系统"
```

配置项不存在时 `data` 为 `null`。

**curl 示例：**

```bash
curl -X GET http://localhost:5000/api/v1/configuration/App%3AName \
  -H "Authorization: Bearer $TOKEN"

# 获取数据库连接字符串
curl -X GET http://localhost:5000/api/v1/configuration/ConnectionStrings%3ADefaultConnection \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 说明 |
|------------|------|
| 422 | key 为空 |
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /configuration/validate

验证生产环境配置是否完整合规。检查必要的配置项是否已设置，用于部署前验证。

- **权限**: Admin / SuperAdmin (`[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`)

**成功响应** (200): `ApiResponse<object>`

```json
{
  "isValid": true,
  "checkedKeys": [
    "Jwt:SecretKey",
    "ConnectionStrings:DefaultConnection",
    "App:Name"
  ],
  "missingKeys": [],
  "warnings": []
}
```

验证失败时返回 422（`data.isValid` 为 `false`，并填充 `missingKeys` / `warnings`）。

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/configuration/validate \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP 状态码 | 说明 |
|------------|------|
| 422 | 配置验证失败 |
| 401/403/404 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## PUT /configuration/{section} 🧲 v1.0 待实现

> 🚧 **v1.0 待实现**（[ADR-0014](../03-architecture/decisions/0014-sysadmin-config-dual-mode.md)）。当前 `ConfigurationController` 仅提供只读 GET；本端点及白/黑名单、写回、延迟重启属待实施范围。

更新单个配置节（仅业务参数，敏感配置返回 403）。sysadmin 在远程模式配置中心修改服务端业务参数时调用。

- **权限**: SysAdminOnly（🧲 新策略，仅 `IsSysAdmin=true`，见 ADR-0014）

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `section` | string | 配置节名（必须在白名单内） |

**PUT 白名单（业务/运维参数，可改）**:
- `Session`（TimeoutMinutes / AllowConcurrentSessions / SlidingExpiration）
- `Security.RateLimiting`（各限额 / 窗口 / 白名单 IP）
- `ClinicSettings`（诊所名 / 营业时间 / 挂号费 / 分页 / 药材角色序）
- `FeatureToggles`（各功能开关）
- `SystemAdmin.SessionTimeoutMinutes`（仅会话超时；`AutoCreateOnStartup` / `AllowAutoCreateInProduction` 不可改）
- `MemoryCache`（SizeLimit / Expiration 等）
- `Serilog:MinimumLevel`（复用 Diagnostics API 热更新）

**PUT 黑名单（敏感/基础设施，返回 403）**:
- `Jwt.SecretKey`、`Jwt.AccessTokenExpirationMinutes`（Token 策略改需重部署）
- `ConnectionStrings`
- `DefaultPasswords`
- `Kestrel`（端口/限制）
- `SystemAdmin.AllowAutoCreateInProduction`、`InitialSetupToken`（生产门控）
- `Database`（连接池/迁移策略）

**请求体**: 该节的完整 JSON 对象（合并写回服务端 `appsettings.json`，写回前备份原文件 `appsettings.json.bak.{timestamp}`）。

**成功响应** (200): `ApiResponse<object>`

```json
{
  "applied": true,
  "restartRequired": true,
  "effectiveMode": "restart"
}
```

**错误码**:

| HTTP 状态码 | 说明 |
|------------|------|
| 403 | 节名在黑名单（敏感/基础设施） |
| 404 | 节名不存在 |
| 422 | 请求体校验失败 |
| 401/400 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## POST /configuration/restart 🧲 v1.0 待实现

> 🚧 **v1.0 待实现**（[ADR-0014](../03-architecture/decisions/0014-sysadmin-config-dual-mode.md)）。延迟重启端点属待实施范围。

延迟 30 秒重启服务端 WebAPI 进程，用于配置变更后生效。sysadmin 在配置中心点击「应用并重启」时触发（二次确认后）。

- **权限**: SysAdminOnly（🧲 新策略，仅 `IsSysAdmin=true`）
- **限频**: 每小时最多 3 次（防误操作/DoS）

**流程**:
1. sysadmin 改配置 → 系统提示「需重启生效」
2. sysadmin 点「应用并重启」→ 二次确认对话框（显示将重启 + 倒计时）
3. `POST /configuration/restart` → 服务端延迟 30 秒执行 `IHostApplicationLifetime.StopApplication()`
4. Windows Service / 进程管理器（配置恢复策略）自动拉起 WebAPI
5. 重启完成 → sysadmin 端轮询健康检查确认恢复

**防护**: 权限（sysadmin only）；审计（每次重启写审计日志，D1 SecurityAuditLog：操作人/时间/原因）；限频（每小时 ≤3 次）；优雅（延迟 30 秒给在途请求完成时间，返回响应后再生效）；通知（重启前通过状态端点/SignalR 告知在线用户）。

**成功响应** (200): `ApiResponse<object>`

```json
{
  "scheduledAt": "2026-06-28T10:00:30Z",
  "delaySeconds": 30
}
```

**错误码**:

| HTTP 状态码 | 说明 |
|------------|------|
| 429 | 触发限频（每小时 ≤3 次） |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## GET 脱敏说明 🧲 v1.0 待实现

> 🚧 **v1.0 待实现**（ADR-0014）。当前 GET 端点返回原始值；脱敏展示属待实施范围。

GET `/configuration` 与 `/configuration/{section}` 响应中敏感字段将掩码展示（与 PUT 黑名单对应）：

| 字段 | 脱敏效果 |
|------|---------|
| `Jwt.SecretKey` | `***` |
| `ConnectionStrings:*` | `Server=***`（保留键名，值掩码） |
| `DefaultPasswords:*` | `***` |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 添加 DTO 类型名到响应; 标注使用基于角色授权 (非策略授权) |
| 2026-06-25 | v1.2 | 补充全部端点的 curl 示例、`ApiResponse<T>` 信封完整 JSON 示例、真实配置键值 |
| 2026-06-28 | v1.1 | 文档对齐代码：权限策略 AdminOnly→AdminOrSuperAdmin（对齐 ConfigurationController.cs:14） |
| 2026-06-28 | v1.3 | 新增 PUT /configuration/{section}（白/黑名单）、POST /configuration/restart（延迟重启）、GET 脱敏说明（均 🧲 v1.0 待实现，ADR-0014） |
| 2026-06-28 | v1.4 | 文档结构优化批次1：JSON 示例去 ApiResponse 外壳只留 data；删除「通用响应格式」节（README 已集中化）；curl 引用 README TOKEN |
