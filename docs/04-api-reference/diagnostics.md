# 诊断工具 API

> Controller: `DiagnosticsController` | 路由前缀: `/api/v1/diagnostics` | 默认权限: `[Authorize(Roles = "SuperAdmin")]`

## 概述

提供运行时日志级别动态调整功能，用于生产环境问题排查。仅 SuperAdmin 角色可访问。调试模式有最大时长限制 (120 分钟)，到期自动恢复默认级别。

---

## GET /diagnostics/logging/status

获取当前日志级别状态。

- **权限**: SuperAdmin

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "currentLevel": "Debug",
    "defaultLevel": "Information",
    "isDebugModeActive": true,
    "debugModeStartedAt": "2026-02-10T12:00:00Z",
    "debugModeExpiresAt": "2026-02-10T12:30:00Z",
    "remainingMinutes": 25
  }
}
```

---

## POST /diagnostics/logging/debug/enable

启用临时调试模式。到期自动恢复默认日志级别。

- **权限**: SuperAdmin

**请求体**:

```json
{
  "level": "Debug",          // 可选，目标级别 (Verbose/Debug/Information)，默认 Debug
  "durationMinutes": 30      // 可选，持续时间 (1-120分钟)，默认 30
}
```

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "message": "调试模式已启用",
    "previousLevel": "Information",
    "currentLevel": "Debug",
    "startedAt": "2026-02-10T12:00:00Z",
    "expiresAt": "2026-02-10T12:30:00Z",
    "durationMinutes": 30
  }
}
```

---

## POST /diagnostics/logging/debug/disable

手动禁用调试模式，恢复默认日志级别。

- **权限**: SuperAdmin

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "message": "调试模式已禁用，已恢复默认日志级别",
    "previousLevel": "Debug",
    "currentLevel": "Information"
  }
}
```

---

## POST /diagnostics/logging/level

直接设置日志级别 (持久生效，直到重启或再次设置)。

- **权限**: SuperAdmin

**请求体**:

```json
{
  "level": "Debug"    // 必填，目标级别 (Verbose/Debug/Information/Warning/Error/Fatal)
}
```

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "message": "日志级别已更新",
    "previousLevel": "Information",
    "currentLevel": "Debug"
  }
}
```

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
