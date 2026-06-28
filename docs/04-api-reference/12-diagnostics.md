# 诊断工具 API

> Controller: `DiagnosticsController` | 路由前缀: `/api/v1/diagnostics` | 默认权限: 远程 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]`; 本地 `[Authorize]` (任意已登录用户)

## 概述

提供运行时日志级别动态调整功能，用于生产环境问题排查。远程模式仅 Admin/SuperAdmin 可访问；本地模式任意已登录用户可访问。调试模式有最大时长限制 (120 分钟)，到期自动恢复默认级别。

> **注意**: 远程模块使用 `[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]` 策略授权。本地模式使用类级 `[Authorize]`（任意已登录用户）。

---

## GET /diagnostics/logging/status

获取当前日志级别状态。

- **权限**: Admin / SuperAdmin

**成功响应** (200) `ApiResponse<object>` — `data`:

```json
{
  "currentLevel": "Information",
  "defaultLevel": "Information",
  "isDebugModeActive": false,
  "debugModeStartedAt": null,
  "debugModeExpiresAt": null,
  "remainingMinutes": null
}
```

**调试模式激活时** — `data`:

```json
{
  "currentLevel": "Debug",
  "defaultLevel": "Information",
  "isDebugModeActive": true,
  "debugModeStartedAt": "2026-06-25T14:00:00Z",
  "debugModeExpiresAt": "2026-06-25T14:30:00Z",
  "remainingMinutes": 30
}
```

**curl 示例：**

```bash
curl -X GET http://localhost:5000/api/v1/diagnostics/logging/status \
  -H "Authorization: Bearer $TOKEN"
```

---

## POST /diagnostics/logging/debug/enable

启用临时调试模式。到期自动恢复默认日志级别。

- **权限**: Admin / SuperAdmin

**请求体** `EnableDebugModeRequest`:

```json
{
  "level": "Debug",
  "durationMinutes": 30
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `level` | string | 否 | 目标级别 (Verbose/Debug/Information)，默认 Debug |
| `durationMinutes` | int | 否 | 持续时间 (1-120分钟)，默认 30，上限 120 |

**成功响应** (200) `ApiResponse<object>` — `data`:

```json
{
  "message": "调试模式已启用",
  "previousLevel": "Information",
  "currentLevel": "Debug",
  "startedAt": "2026-06-25T14:00:00Z",
  "expiresAt": "2026-06-25T14:30:00Z",
  "durationMinutes": 30
}
```

**curl 示例：**

```bash
# 默认启用 30 分钟 Debug
curl -X POST http://localhost:5000/api/v1/diagnostics/logging/debug/enable \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"level":"Debug","durationMinutes":30}'

# 启用 Verbose 级别，持续 60 分钟
curl -X POST http://localhost:5000/api/v1/diagnostics/logging/debug/enable \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"level":"Verbose","durationMinutes":60}'

# 使用默认参数（Debug 级别，30 分钟）
curl -X POST http://localhost:5000/api/v1/diagnostics/logging/debug/enable \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

---

## POST /diagnostics/logging/debug/disable

手动禁用调试模式，恢复默认日志级别。

- **权限**: Admin / SuperAdmin
- **请求体**: 无

**成功响应** (200) `ApiResponse<object>` — `data`:

```json
{
  "message": "调试模式已禁用，已恢复默认日志级别",
  "previousLevel": "Debug",
  "currentLevel": "Information"
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/diagnostics/logging/debug/disable \
  -H "Authorization: Bearer $TOKEN"
```

---

## POST /diagnostics/logging/level

直接设置日志级别 (持久生效，直到重启或再次设置)。

- **权限**: Admin / SuperAdmin

**请求体** `SetLoggingLevelRequest`:

```json
{
  "level": "Debug"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `level` | string | 是 | 目标级别 (Verbose/Debug/Information/Warning/Error/Fatal) |

**成功响应** (200) `ApiResponse<object>` — `data`:

```json
{
  "message": "日志级别已更新",
  "previousLevel": "Information",
  "currentLevel": "Debug"
}
```

**curl 示例：**

```bash
# 设置为 Warning 级别
curl -X POST http://localhost:5000/api/v1/diagnostics/logging/level \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"level":"Warning"}'

# 设置为 Debug 级别
curl -X POST http://localhost:5000/api/v1/diagnostics/logging/level \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"level":"Debug"}'
```

---

## 错误码

| HTTP 状态码 | 错误信息 | 场景 |
|------------|---------|------|
| 400 | `日志级别不能为空` | `level` 为空 |
| 400 | `无效的日志级别` | `level` 非合法枚举值（合法值：Verbose/Debug/Information/Warning/Error/Fatal） |
| 401/403 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 标注使用基于角色授权 (非策略授权) |
| 2026-06-25 | v1.2 | 补充全部端点的 curl 示例、`ApiResponse<T>` 信封完整 JSON 示例、无效/空级别错误响应、错误码表 |
| 2026-06-28 | v1.1 | 文档对齐代码：权限策略 AdminOnly→AdminOrSuperAdmin（对齐 DiagnosticsController.cs:19） |
| 2026-06-28 | v1.3 | 文档结构优化批次1：JSON 示例去 ApiResponse 外壳只留 data；错误响应 JSON 块合并到错误码表；通用状态码引用 README |
