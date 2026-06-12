---
type: concept
title: API 标准化响应信封
tags: [api, architecture, json, standard]
related: [problem-details-rfc7807, error-handling, mccee-error-code-standard, correlation-id-tracking]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/04-api-reference/README.md", "docs/06-operations/newman-report.html"]
---
# API 标准化响应信封

## 定义

**API 标准化响应信封**（API Response Envelope）是凌隐宝堂系统 RESTful API 采用的统一 JSON 响应结构。无论请求成功与否，API 均返回包含 `success`, `message`, `data` (或 `errors`), `timestamp` 及 `requestId` 字段的对象，旨在为客户端提供一致的数据解析体验与业务状态判断逻辑。

## 标准结构示例

### 基础成功响应
```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "errors": null,
  "timestamp": "2026-03-28T15:58:44Z",
  "requestId": "0HN8V..."
}
```

### 基础失败响应
```json
{
  "success": false,
  "message": "错误描述",
  "data": null,
  "errors": [ ... ],
  "timestamp": "2026-03-28T15:58:44Z",
  "requestId": "0HN8V..."
}
```

### 分页响应
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [ ... ],
    "totalCount": 100,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "errors": null,
  "timestamp": "2026-03-28T15:58:44Z",
  "requestId": "0HN8V..."
}
```

## 关键字段说明

* **success**: 布尔值，指示操作是否成功。
* **message**: 人类可读的消息，成功时为操作提示，失败时为错误描述。
* **data**: 业务数据载体，失败时通常为 `null`。
* **errors**: 错误详情数组或对象，成功时通常为 `null`。
* **timestamp**: 响应生成的时间戳（ISO 8601 格式）。
* **requestId**: 唯一请求标识符，用于链路追踪和日志排查，对应 [[correlation-id-tracking]]。

## 现状与规范差异

系统早期架构文档中曾提及 [[problem-details-rfc7807]] (RFC 7807 Problem Details)。根据 [[4-docs--13-06-operations--13-newman-report--dpflh1|Newman 测试报告 (2026-03-28)]] 的分析，当前系统在实际运行中存在**响应格式双轨制**的问题：

### 合规路径（自定义信封）
* **业务响应**：统一使用自定义信封 (`success`/`data`)。例如 Auth 模块（登录、登出、刷新令牌）严格遵循此结构，即使在 401 错误时也返回 `{"success": false, "errors": {...}}`。大多数业务模块（如 Herbs, Formulas）的 200 OK 响应均包含完整信封。
* **设计初衷**：为前端提供一致的数据解析逻辑，特别是对于分页数据和业务状态判断。

### 违规/例外路径
* **Problem Details (RFC 7807)**：在模型验证失败（400）、资源未找到（404）或方法不允许（405）时，ASP.NET Core 默认中间件会直接返回 `application/problem+json` 格式，缺少 `success` 字段。
* **运维接口**：Health 和 Diagnostics 模块直接返回轻量级业务对象（如 `{ "status": "Healthy" }`），未进行信封包装。
* **二进制响应**：导出接口返回 Excel 二进制流，完全脱离 JSON 信封规范。
* **基础设施错误**：在某些底层异常或网关层面，可能仍会返回符合 RFC 7807 标准的错误结构。

## 影响

这种不一致性导致自动化测试脚本中的通用断言（`ApiResponse structure is valid`）在错误场景下大规模失败，增加了维护成本并掩盖了真实的业务逻辑错误。

## 改进方向

* **方案 A**：修改全局异常处理中间件，捕获 `ProblemDetails` 并将其转换为标准的 `ApiResponse` 格式，实现全链路统一。
* **方案 B**：调整测试框架，使其能够根据 HTTP 状态码或 `Content-Type` 动态切换断言策略（兼容 RFC 7807 和自定义信封）。