# 系统配置 API

> Controller: `ConfigurationController` | 路由前缀: `/api/v1/configuration` | 默认权限: `[Authorize(Roles = "Admin")]`

## 概述

提供系统配置读取与生产环境配置验证功能。仅 Admin 角色可访问。GetConfiguration 返回安全、非敏感的配置项；GetValue 按 key 查询单个配置值；ValidateProduction 验证生产环境配置是否完整合规。

> **注意**: 本模块使用 `[Authorize(Roles = "Admin")]` 基于角色的授权，而非其他模块使用的 `[Authorize(Policy = "...")]` 策略授权。

---

## GET /configuration

获取系统配置项集合。

- **权限**: Admin

**成功响应** (200): `ApiResponse<Dictionary<string, string?>>`

**状态码**:

| HTTP | 说明 |
|------|------|
| 200 | 成功返回配置字典 |
| 401 | 未认证 |
| 403 | 非 Admin 角色 |

---

## GET /configuration/{key}

获取单个配置项的值。

- **权限**: Admin

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| key | string | 配置项名称 (如 `App:Name`) |

**成功响应** (200): `ApiResponse<string?>`

**参数错误响应** (422):

```json
{
  "success": false,
  "message": "配置项名称不能为空"
}
```

**状态码**:

| HTTP | 说明 |
|------|------|
| 200 | 成功返回配置值 (可为 null) |
| 401 | 未认证 |
| 403 | 非 Admin 角色 |
| 422 | key 为空 |

---

## POST /configuration/validate

验证生产环境配置是否完整合规。检查必要的配置项是否已设置，用于部署前验证。

- **权限**: Admin

**成功响应** (200): `ApiResponse<object>`

**验证失败响应** (422): `ApiResponse` (message 含缺少的配置项)

**状态码**:

| HTTP | 说明 |
|------|------|
| 200 | 验证通过 |
| 401 | 未认证 |
| 403 | 非 Admin 角色 |
| 422 | 配置验证失败 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 添加 DTO 类型名到响应; 标注使用基于角色授权 (非策略授权) |
