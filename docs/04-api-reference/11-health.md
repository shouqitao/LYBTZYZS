# 健康检查 API

> Controller: `HealthController` | 路由前缀: `/api/v1/health` | 默认权限: `[Authorize]`

## 概述

提供服务端健康状态检查功能，用于负载均衡器探活、监控系统集成和运维排查。基础检查匿名访问，详细检查需认证。

> **注意**: 健康检查端点使用 `Success()` 辅助方法包装 `ApiResponse<T>` 信封；详细检查使用 `ApiResponse<object>.CreateSuccess()` 直接包装。所有响应均为 `ApiResponse<T>` 格式。

---

## GET /health

基础健康检查，快速探活端点。

- **权限**: 匿名 (`[AllowAnonymous]`)

**成功响应** (200) `ApiResponse<object>` — `data`:

```json
{
  "status": "Healthy",
  "timestamp": "2026-06-25T14:00:00Z"
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

**成功响应** (200) `ApiResponse<object>` — `data`:

```json
{
  "message": "pong",
  "timestamp": "2026-06-25T14:00:00Z"
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

**成功响应** (200) `ApiResponse<object>` — `data`:

```json
{
  "status": "Healthy",
  "timestamp": "2026-06-25T14:00:00Z",
  "database": {
    "status": "Healthy",
    "duration": 45,
    "provider": "Microsoft.EntityFrameworkCore.SqlServer",
    "pendingMigrations": 0,
    "serverVersion": "Microsoft SQL Server 2022 (RTM-GDR3-GDR3-KB5035432) - 16.0.4135.4"
  }
}
```

**降级响应** (503) `data`:

```json
{
  "status": "Degraded",
  "timestamp": "2026-06-25T14:00:00Z",
  "database": {
    "status": "Degraded",
    "duration": 5023,
    "provider": "Microsoft.EntityFrameworkCore.SqlServer",
    "pendingMigrations": 2,
    "serverVersion": null
  }
}
```

**curl 示例：**

```bash
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
| 503 | 数据库连接异常或超时 |
| 401 | — | 通用错误码见 [README](README.md#通用-http-状态码) |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 标注使用简化响应格式 (非 ApiResponse 信封) |
| 2026-06-25 | v1.2 | 修正为 `ApiResponse<T>` 信封格式（与源码一致）；补充 curl 示例、完整 JSON 响应、错误码表 |
| 2026-06-28 | v1.3 | 文档结构优化批次1：JSON 示例去 ApiResponse 外壳只留 data；错误响应 JSON 块合并到错误码表；通用状态码引用 README |
