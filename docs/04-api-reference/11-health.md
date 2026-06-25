# 健康检查 API

> Controller: `HealthController` | 路由前缀: `/api/v1/health` | 默认权限: `[Authorize]`

## 概述

提供服务端健康状态检查功能，用于负载均衡器探活、监控系统集成和运维排查。基础检查匿名访问，详细检查需认证。

> **注意**: 健康检查端点使用 `Success()` 辅助方法包装 `ApiResponse<T>` 信封；详细检查使用 `ApiResponse<object>.CreateSuccess()` 直接包装。所有响应均为 `ApiResponse<T>` 格式。

---

## GET /health

基础健康检查，快速探活端点。

- **权限**: 匿名 (`[AllowAnonymous]`)

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "status": "Healthy",
    "timestamp": "2026-06-25T14:00:00Z"
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9K..."
}
```

**curl 示例：**

```bash
# 无需认证，直接探活
curl -X GET http://localhost:5000/api/v1/health
```

---

## GET /health/ping

Ping/Pong 端点，最轻量的探活检查。

- **权限**: 匿名 (`[AllowAnonymous]`)

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "message": "pong",
    "timestamp": "2026-06-25T14:00:00Z"
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9L..."
}
```

**curl 示例：**

```bash
# 无需认证，Ping 端点
curl -X GET http://localhost:5000/api/v1/health/ping
```

---

## GET /health/details

详细健康检查，包含数据库连接状态。

- **权限**: 已认证 (`[Authorize]`)

**成功响应** (200) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "status": "Healthy",
    "timestamp": "2026-06-25T14:00:00Z",
    "database": {
      "status": "Healthy",
      "duration": 45,
      "provider": "Microsoft.EntityFrameworkCore.SqlServer",
      "pendingMigrations": 0,
      "serverVersion": "Microsoft SQL Server 2022 (RTM-GDR3-GDR3-KB5035432) - 16.0.4135.4"
    }
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9M..."
}
```

**降级响应** (503) `ApiResponse<object>`:

```json
{
  "success": true,
  "message": null,
  "data": {
    "status": "Degraded",
    "timestamp": "2026-06-25T14:00:00Z",
    "database": {
      "status": "Degraded",
      "duration": 5023,
      "provider": "Microsoft.EntityFrameworkCore.SqlServer",
      "pendingMigrations": 2,
      "serverVersion": null
    }
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9M..."
}
```

**curl 示例：**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')

# 需要认证，查看详细健康状态
curl -X GET http://localhost:5000/api/v1/health/details \
  -H "Authorization: Bearer $TOKEN"
```

**状态值说明**:

| 状态 | HTTP | 说明 |
|------|------|------|
| Healthy | 200 | 所有组件正常 |
| Degraded | 503 | 数据库连接异常或超时 |
| Unhealthy | 503 | 严重错误 |

**错误码**:

| HTTP 状态码 | 说明 |
|------------|------|
| 200 | 健康检查通过 |
| 401 | 未认证（仅 /health/details） |
| 503 | 数据库连接异常或超时 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 标注使用简化响应格式 (非 ApiResponse 信封) |
| 2026-06-25 | v1.2 | 修正为 `ApiResponse<T>` 信封格式（与源码一致）；补充 curl 示例、完整 JSON 响应、错误码表 |
