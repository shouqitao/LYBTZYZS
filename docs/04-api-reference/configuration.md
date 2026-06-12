# 系统配置 API

> Controller: `ConfigurationController` | 路由前缀: `/api/v1/configuration` | 默认权限: `[Authorize(Roles = "Admin")]`

## 概述

提供系统配置读取与生产环境配置验证功能。仅 Admin 角色可访问。GetConfiguration 返回安全、非敏感的配置项；GetValue 按 key 查询单个配置值；ValidateProduction 验证生产环境配置是否完整合规。

---

## GET /configuration

获取系统配置项集合。

- **权限**: Admin

**成功响应** (200):

```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "App:Name": "LYBTZYZS",
    "App:Version": "1.0.0",
    "App:Environment": "Production"
  }
}
```

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

**成功响应** (200):

```json
{
  "success": true,
  "message": "操作成功",
  "data": "LYBTZYZS"
}
```

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

**成功响应** (200):

```json
{
  "success": true,
  "message": "操作成功"
}
```

**验证失败响应** (422):

```json
{
  "success": false,
  "message": "缺少必要配置项: ConnectionStrings:DefaultConnection"
}
```

**状态码**:

| HTTP | 说明 |
|------|------|
| 200 | 验证通过 |
| 401 | 未认证 |
| 403 | 非 Admin 角色 |
| 422 | 配置验证失败 |

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
