# 健康检查 API

> Controller: `HealthController` | 路由前缀: `/api/v1/health` | 默认权限: `[Authorize]`

## 概述

提供服务端健康状态检查功能，用于负载均衡器探活、监控系统集成和运维排查。基础检查匿名访问，详细检查需认证。

> **注意**: 健康检查端点使用简化的响应格式 (扁平 JSON)，不使用标准的 `ApiResponse<T>` 信封。

---

## GET /health

基础健康检查，快速探活端点。

- **权限**: 匿名 (`[AllowAnonymous]`)

**成功响应** (200):

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-10T12:00:00Z"
}
```

---

## GET /health/ping

Ping/Pong 端点，最轻量的探活检查。

- **权限**: 匿名 (`[AllowAnonymous]`)

**成功响应** (200):

```json
{
  "message": "pong",
  "timestamp": "2026-02-10T12:00:00Z"
}
```

---

## GET /health/details

详细健康检查，包含数据库连接状态。

- **权限**: 已认证 (`[Authorize]`)

**成功响应** (200):

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-10T12:00:00Z",
  "database": {
    "status": "Healthy",
    "duration": 45
  }
}
```

**降级响应** (503):

```json
{
  "status": "Degraded",
  "timestamp": "2026-02-10T12:00:00Z",
  "database": {
    "status": "Degraded",
    "duration": 120
  }
}
```

**状态值说明**:

| 状态 | HTTP | 说明 |
|------|------|------|
| Healthy | 200 | 所有组件正常 |
| Degraded | 503 | 数据库连接异常或超时 |
| Unhealthy | 503 | 严重错误 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 标注使用简化响应格式 (非 ApiResponse 信封) |
