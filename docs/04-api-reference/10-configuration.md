# 系统配置 API

> Controller: `ConfigurationController` | 路由前缀: `/api/v1/configuration` | 默认权限: `[Authorize(Policy = PolicyConstants.AdminOnly)]`

## 概述

提供系统配置读取与生产环境配置验证功能。仅 Admin 和 SuperAdmin 可访问。GetConfiguration 返回安全、非敏感的配置项；GetValue 按 key 查询单个配置值；ValidateProduction 验证生产环境配置是否完整合规。

> **注意**: 本模块使用 `[Authorize(Policy = PolicyConstants.AdminOnly)]` 策略授权。

---

## GET /configuration

获取系统配置项集合。

- **权限**: Admin / SuperAdmin (`[Authorize(Policy = PolicyConstants.AdminOnly)]`)

**成功响应** (200): `ApiResponse<Dictionary<string, string?>>`

```json
{
  "success": true,
  "message": "配置获取成功",
  "data": {
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
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9H..."
}
```

**curl 示例：**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')

curl -X GET http://localhost:5000/api/v1/configuration \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP | 说明 |
|------|------|
| 200 | 成功返回配置字典 |
| 401 | 未认证 |
| 403 | 非 Admin/SuperAdmin 角色 |

---

## GET /configuration/{key}

获取单个配置项的值。

- **权限**: Admin / SuperAdmin (`[Authorize(Policy = PolicyConstants.AdminOnly)]`)

**路径参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| `key` | string | 配置项名称 (如 `App:Name`) |

**成功响应** (200): `ApiResponse<string?>`

```json
{
  "success": true,
  "message": "配置项获取成功",
  "data": "凌隐宝堂中医诊所管理系统",
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9I..."
}
```

**配置项不存在时**（data 为 null）:

```json
{
  "success": true,
  "message": "配置项获取成功",
  "data": null,
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9I..."
}
```

**参数错误响应** (422):

```json
{
  "success": false,
  "message": "配置项名称不能为空",
  "data": null,
  "errors": ["key 不能为空"],
  "timestamp": 1750873200,
  "requestId": "0HN9I..."
}
```

**curl 示例：**

```bash
# 获取应用名称
curl -X GET http://localhost:5000/api/v1/configuration/App%3AName \
  -H "Authorization: Bearer $TOKEN"

# 获取数据库连接字符串
curl -X GET http://localhost:5000/api/v1/configuration/ConnectionStrings%3ADefaultConnection \
  -H "Authorization: Bearer $TOKEN"

# 获取 JWT 过期时间
curl -X GET http://localhost:5000/api/v1/configuration/Jwt%3AExpireMinutes \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP | 说明 |
|------|------|
| 200 | 成功返回配置值 (可为 null) |
| 401 | 未认证 |
| 403 | 非 Admin/SuperAdmin 角色 |
| 422 | key 为空 |

---

## POST /configuration/validate

验证生产环境配置是否完整合规。检查必要的配置项是否已设置，用于部署前验证。

- **权限**: Admin / SuperAdmin (`[Authorize(Policy = PolicyConstants.AdminOnly)]`)

**成功响应** (200): `ApiResponse<object>`

```json
{
  "success": true,
  "message": "生产环境配置验证通过",
  "data": {
    "isValid": true,
    "checkedKeys": [
      "Jwt:SecretKey",
      "ConnectionStrings:DefaultConnection",
      "App:Name"
    ],
    "missingKeys": [],
    "warnings": []
  },
  "errors": null,
  "timestamp": 1750873200,
  "requestId": "0HN9J..."
}
```

**验证失败响应** (422):

```json
{
  "success": false,
  "message": "生产环境配置验证失败",
  "data": {
    "isValid": false,
    "checkedKeys": [
      "Jwt:SecretKey",
      "ConnectionStrings:DefaultConnection",
      "App:Name"
    ],
    "missingKeys": [
      "Jwt:SecretKey",
      "ConnectionStrings:DefaultConnection"
    ],
    "warnings": [
      "默认密码未修改，生产环境请更换"
    ]
  },
  "errors": ["缺少必要的生产环境配置项"],
  "timestamp": 1750873200,
  "requestId": "0HN9J..."
}
```

**curl 示例：**

```bash
curl -X POST http://localhost:5000/api/v1/configuration/validate \
  -H "Authorization: Bearer $TOKEN"
```

**错误码**:

| HTTP | 说明 |
|------|------|
| 200 | 验证通过 |
| 401 | 未认证 |
| 403 | 非 Admin/SuperAdmin 角色 |
| 422 | 配置验证失败 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
| 2026-06-12 | v1.1 | 添加 DTO 类型名到响应; 标注使用基于角色授权 (非策略授权) |
| 2026-06-25 | v1.2 | 补充全部端点的 curl 示例、`ApiResponse<T>` 信封完整 JSON 示例、真实配置键值 |
