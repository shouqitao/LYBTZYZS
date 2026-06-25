# 诊断工具 API

> Controller: `DiagnosticsController` | 路由前缀: `/api/v1/diagnostics` | 默认权限: 远程 `[Authorize(Policy = PolicyConstants.AdminOnly)]`; 本地 `[Authorize]` (任意已登录用户)

## 概述

提供运行时日志级别动态调整功能，用于生产环境问题排查。远程模式仅 Admin/SuperAdmin 可访问；本地模式任意已登录用户可访问。调试模式有最大时长限制 (120 分钟)，到期自动恢复默认级别。

> **注意**: 远程模块使用 `[Authorize(Policy = PolicyConstants.AdminOnly)]` 策略授权。本地模式使用类级 `[Authorize]`（任意已登录用户）。

---

## GET /diagnostics/logging/status

获取当前日志级别状态。

- **权限**: Admin / SuperAdmin

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "currentLevel": "Information",
    "defaultLevel": "Information",
    "isDebugModeActive": false,
    "debugModeStartedAt": null,
    "debugModeExpiresAt": null,
    "remainingMinutes": null
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9N..."
}
```

**调试模式激活时**:

```json
{
  "success": true,
  "message": null,
  "data": {
    "currentLevel": "Debug",
    "defaultLevel": "Information",
    "isDebugModeActive": true,
    "debugModeStartedAt": "2026-06-25T14:00:00Z",
    "debugModeExpiresAt": "2026-06-25T14:30:00Z",
    "remainingMinutes": 30
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9N..."
}
```

**curl 示例：**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')

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

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "message": "调试模式已启用",
    "previousLevel": "Information",
    "currentLevel": "Debug",
    "startedAt": "2026-06-25T14:00:00Z",
    "expiresAt": "2026-06-25T14:30:00Z",
    "durationMinutes": 30
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9P..."
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

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "message": "调试模式已禁用，已恢复默认日志级别",
    "previousLevel": "Debug",
    "currentLevel": "Information"
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9Q..."
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

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "message": "日志级别已更新",
    "previousLevel": "Information",
    "currentLevel": "Debug"
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9R..."
}
```

**空级别错误响应** (400):

```json
{
  "error": "日志级别不能为空"
}
```

**无效级别错误响应** (400):

```json
{
  "error": "无效的日志级别",
  "validLevels": ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"]
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

| HTTP 状态码 | 说明 |
|------------|------|
| 200 | 操作成功 |
| 400 | 日志级别为空或无效 |
| 401 | 未认证 |
| 403 | 非 Admin/SuperAdmin 角色 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 标注使用基于角色授权 (非策略授权) |
| 2026-06-25 | v1.2 | 补充全部端点的 curl 示例、`ApiResponse<T>` 信封完整 JSON 示例、无效/空级别错误响应、错误码表 |
