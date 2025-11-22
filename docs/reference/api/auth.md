# Auth API参考文档 (Auth API Reference)

> **信息导向**: 精确的认证授权API接口技术文档
> **适合人群**: 开发者、API集成人员、技术负责人
> **使用方式**: 精确查询、接口对接、技术实现

## 🔌 API概览

### 基本信息
- **API版本**: v1
- **基础路径**: `/api/v1/auth`
- **认证方式**: JWT Bearer Token
- **内容类型**: `application/json`
- **字符编码**: UTF-8

### 安全特性
- JWT无状态认证
- 刷新令牌机制
- 登录限流保护
- 请求签名验证
- CORS跨域支持

## 🔐 认证端点 (Authentication Endpoints)

### 用户登录

#### 端点信息
```http
POST /api/v1/auth/login
```

#### 请求头
```
Content-Type: application/json
X-Requested-With: XMLHttpRequest
```

#### 请求体
```json
{
  "userName": "string",
  "password": "string",
  "rememberMe": "boolean"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 | 约束 |
|--------|------|------|------|------|
| userName | string | 是 | 用户名 | 3-50字符，字母数字下划线 |
| password | string | 是 | 密码 | 8-128字符，符合密码策略 |
| rememberMe | boolean | 否 | 记住登录状态 | 默认false |

#### 响应格式
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcy1pcy1hLXJlZnJlc2gtdG9rZW4=",
    "expiresIn": 900,
    "user": {
      "id": "00000000-0000-0000-0000-000000000001",
      "userName": "admin",
      "displayName": "系统管理员",
      "role": "SuperAdmin",
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00Z"
    }
  }
}
```

**响应数据说明**
| 字段名 | 类型 | 描述 |
|--------|------|------|
| token | string | JWT访问令牌，15分钟有效 |
| refreshToken | string | 刷新令牌，7天有效 |
| expiresIn | number | 访问令牌过期时间（秒） |
| user | object | 用户基本信息 |

#### HTTP状态码
| 状态码 | 描述 | 场景 |
|--------|------|------|
| 200 | OK | 登录成功 |
| 400 | Bad Request | 请求参数错误 |
| 401 | Unauthorized | 用户名或密码错误 |
| 429 | Too Many Requests | 登录次数超限 |
| 500 | Internal Server Error | 服务器内部错误 |

#### 错误响应格式
```json
{
  "success": false,
  "message": "用户名或密码错误",
  "errors": [
    {
      "code": "INVALID_CREDENTIALS",
      "message": "提供的用户名或密码不正确",
      "field": "password"
    }
  ],
  "timestamp": "2025-01-01T12:00:00Z"
}
```

### 用户登出

#### 端点信息
```http
POST /api/v1/auth/logout
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "refreshToken": "string"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| refreshToken | string | 是 | 要撤销的刷新令牌 |

#### 响应格式
```json
{
  "success": true,
  "message": "登出成功",
  "data": null
}
```

#### HTTP状态码
| 状态码 | 描述 | 场景 |
|--------|------|------|
| 200 | OK | 登出成功 |
| 400 | Bad Request | 刷新令牌无效 |
| 401 | Unauthorized | 访问令牌无效 |
| 500 | Internal Server Error | 服务器内部错误 |

### 刷新访问令牌

#### 端点信息
```http
POST /api/v1/auth/refresh-token
```

#### 请求体
```json
{
  "refreshToken": "string"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| refreshToken | string | 是 | 有效的刷新令牌 |

#### 响应格式
```json
{
  "success": true,
  "message": "令牌刷新成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 900
  }
}
```

#### HTTP状态码
| 状态码 | 描述 | 场景 |
|--------|------|------|
| 200 | OK | 令牌刷新成功 |
| 400 | Bad Request | 刷新令牌格式错误 |
| 401 | Unauthorized | 刷新令牌无效或过期 |
| 500 | Internal Server Error | 服务器内部错误 |

### 撤销令牌

#### 端点信息
```http
POST /api/v1/auth/revoke-token
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "refreshToken": "string",
  "revokeReason": "string"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| refreshToken | string | 是 | 要撤销的刷新令牌 |
| revokeReason | string | 否 | 撤销原因 |

#### 响应格式
```json
{
  "success": true,
  "message": "令牌撤销成功",
  "data": null
}
```

### 验证令牌

#### 端点信息
```http
POST /api/v1/auth/validate-token
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "token": "string"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| token | string | 是 | 要验证的JWT令牌 |

#### 响应格式
```json
{
  "success": true,
  "message": "令牌有效",
  "data": {
    "isValid": true,
    "userId": "00000000-0000-0000-0000-000000000001",
    "userName": "admin",
    "role": "SuperAdmin",
    "expiresAt": "2025-01-01T12:15:00Z"
  }
}
```

### 修改密码

#### 端点信息
```http
POST /api/v1/auth/change-password
Authorization: Bearer <access_token>
```

#### 请求体
```json
{
  "currentPassword": "string",
  "newPassword": "string",
  "confirmPassword": "string"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| currentPassword | string | 是 | 当前密码 |
| newPassword | string | 是 | 新密码 |
| confirmPassword | string | 是 | 确认新密码 |

#### 密码策略要求
- 最小长度：8个字符
- 包含大写字母：A-Z
- 包含小写字母：a-z
- 包含数字：0-9
- 包含特殊字符：!@#$%^&*()_+-=[]{}|;:,.<>

#### 响应格式
```json
{
  "success": true,
  "message": "密码修改成功",
  "data": null
}
```

## 👤 管理员专用端点

### 超级管理员登录

#### 端点信息
```http
POST /api/v1/auth/super-admin-login
```

#### 请求体
```json
{
  "adminKey": "string",
  "userName": "string",
  "password": "string"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| adminKey | string | 是 | 超级管理员密钥 |
| userName | string | 是 | 超级管理员用户名 |
| password | string | 是 | 超级管理员密码 |

### 修改系统管理员密码

#### 端点信息
```http
POST /api/v1/auth/change-sysadmin-password
Authorization: Bearer <access_token>
```

**权限要求**: SuperAdmin角色

#### 请求体
```json
{
  "targetUserId": "string",
  "newPassword": "string",
  "forceChange": "boolean"
}
```

**请求参数说明**
| 参数名 | 类型 | 必填 | 描述 |
|--------|------|------|------|
| targetUserId | string | 是 | 目标用户ID |
| newPassword | string | 是 | 新密码 |
| forceChange | boolean | 否 | 是否强制用户下次登录时修改密码 |

## 🔒 权限控制

### 角色权限矩阵

| 端点 | SuperAdmin | Admin | Doctor | Nurse | 未认证 |
|------|------------|-------|-------|-------|--------|
| POST /login | ✗ | ✗ | ✗ | ✗ | ✅ |
| POST /logout | ✅ | ✅ | ✅ | ✅ | ✗ |
| POST /refresh-token | ✗ | ✗ | ✗ | ✗ | ✅ |
| POST /revoke-token | ✅ | ✅ | ✅ | ✅ | ✗ |
| POST /validate-token | ✅ | ✅ | ✅ | ✅ | ✅ |
| POST /change-password | ✅ | ✅ | ✅ | ✅ | ✗ |
| POST /super-admin-login | ✗ | ✗ | ✗ | ✗ | ✅ |
| POST /change-sysadmin-password | ✅ | ✗ | ✗ | ✗ | ✗ |

### JWT令牌声明

```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "name": "admin",
  "role": "SuperAdmin",
  "iat": 1640995200,
  "exp": 1640996100,
  "iss": "LYBTZYZS",
  "aud": "LYBTZYZS-Users",
  "jti": "unique-token-id"
}
```

**声明说明**
| 声明名 | 描述 | 示例 |
|--------|------|------|
| sub | 用户ID | 00000000-0000-0000-0000-000000000001 |
| name | 用户名 | admin |
| role | 用户角色 | SuperAdmin |
| iat | 令牌签发时间 | 1640995200 |
| exp | 令牌过期时间 | 1640996100 |
| iss | 签发者 | LYBTZYZS |
| aud | 受众 | LYBTZYZS-Users |
| jti | 令牌ID | unique-token-id |

## 🛡️ 安全机制

### 登录限流
- **限制策略**: 同一IP每分钟最多5次登录尝试
- **锁定策略**: 同一用户名失败3次后锁定30分钟
- **实现方式**: ASP.NET Core Rate Limiting

### 密码安全
- **哈希算法**: BCrypt
- **盐值强度**: 2^12 = 4096轮
- **最小长度**: 8字符
- **复杂度要求**: 大小写字母+数字+特殊字符

### Token安全
- **签名算法**: HMAC-SHA256
- **访问令牌有效期**: 15分钟
- **刷新令牌有效期**: 7天
- **Token撤销**: 支持主动撤销和黑名单机制

### 审计日志
记录所有安全相关事件：
- 登录成功/失败
- 登出操作
- 密码修改
- 权限变更
- Token撤销

## 🔧 配置参数

### JWT配置 (appsettings.json)
```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-must-be-long-enough-32-chars-minimum",
    "Issuer": "LYBTZYZS",
    "Audience": "LYBTZYZS-Users",
    "AccessTokenExpiration": 15,
    "RefreshTokenExpiration": 168,
    "ClockSkew": 5
  }
}
```

**配置说明**
| 参数名 | 类型 | 默认值 | 描述 |
|--------|------|--------|------|
| SecretKey | string | 必填 | JWT签名密钥，至少32字符 |
| Issuer | string | LYBTZYZS | JWT签发者 |
| Audience | string | LYBTZYZS-Users | JWT受众 |
| AccessTokenExpiration | number | 15 | 访问令牌有效期（分钟） |
| RefreshTokenExpiration | number | 168 | 刷新令牌有效期（小时） |
| ClockSkew | number | 5 | 时钟偏差容忍度（分钟） |

### 安全配置
```json
{
  "SecuritySettings": {
    "MaxFailedLoginAttempts": 3,
    "AccountLockoutDuration": 30,
    "PasswordMinLength": 8,
    "RequirePasswordComplexity": true,
    "EnableSecurityAudit": true
  }
}
```

## 🔄 API版本控制

### 版本策略
- **当前版本**: v1
- **版本格式**: URL路径版本控制 (`/api/v{version}`)
- **向后兼容**: 保持向后兼容性
- **弃用通知**: 提前3个月通知API弃用

### 版本变更记录
| 版本 | 发布日期 | 主要变更 | 兼容性 |
|------|----------|----------|--------|
| v1.0 | 2025-01-01 | 初始版本 | - |
| v1.1 | 计划中 | 添加多因素认证 | 向后兼容 |

## 📊 监控和日志

### 健康检查
```http
GET /api/v1/auth/health
```

**响应格式**
```json
{
  "status": "Healthy",
  "timestamp": "2025-01-01T12:00:00Z",
  "checks": {
    "database": "Healthy",
    "jwt": "Healthy",
    "cache": "Healthy"
  }
}
```

### 日志级别
- **Trace**: 详细的Token处理信息
- **Debug**: 认证流程调试信息
- **Information**: 关键操作记录
- **Warning**: 安全相关警告
- **Error**: 认证错误和异常

### 性能指标
- 登录响应时间（目标：<200ms）
- Token验证时间（目标：<50ms）
- 数据库查询时间（目标：<100ms）
- 并发登录处理能力（目标：100/秒）

## 🧪 测试工具

### Postman集合
```json
{
  "info": {
    "name": "LYBTZYZS Auth API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Login",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "body": {
          "mode": "raw",
          "raw": "{\n  \"userName\": \"admin\",\n  \"password\": \"password123\",\n  \"rememberMe\": true\n}"
        },
        "url": {
          "raw": "{{base_url}}/api/v1/auth/login",
          "host": ["{{base_url}}"],
          "path": ["api", "v1", "auth", "login"]
        }
      }
    }
  ]
}
```

### 自动化测试示例
```csharp
[Test]
public async Task Login_ValidCredentials_ReturnsToken()
{
    // Arrange
    var loginRequest = new LoginRequest
    {
        UserName = "testuser",
        Password = "TestPassword123!",
        RememberMe = false
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    result.Success.Should().BeTrue();
    result.Data.Token.Should().NotBeEmpty();
    result.Data.RefreshToken.Should().NotBeEmpty();
}
```

## 🔗 相关资源

### API文档
- [Users API参考文档](users.md)
- [安全配置指南](../configuration/authentication.md)
- [权限管理规范](../business-rules/rbac.md)

### 技术规范
- [REST API设计规范](../technical-specs/rest-api.md)
- [JWT实现详解](../explanation/technology/jwt-implementation.md)
- [安全最佳实践](../technical-specs/security.md)

### 外部资源
- [JWT官方网站](https://jwt.io/)
- [OWASP认证指南](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [ASP.NET Core认证文档](https://docs.microsoft.com/aspnet/core/security/authentication/)

---

**文档类型**: Reference API
**API版本**: v1.0
**更新时间**: 2025-11-22
**维护团队**: 架构组 + API团队
**测试覆盖**: 100%