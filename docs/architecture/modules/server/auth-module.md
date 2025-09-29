# Auth模块设计 - Server端

## 📋 模块概述
**职责**：用户认证、JWT令牌管理、登录登出、密码管理
**命名空间**：`LYBT.Module.Auth`
**API路径**：`/api/v1/auth/*`

## 🏗️ 架构设计

### 分层结构
```
├── Controllers/           # HTTP控制器
│   └── AuthController.cs
├── Services/             # 业务服务
│   ├── AuthService.cs
│   ├── JwtService.cs
│   └── EnhancedJwtService.cs
├── Interfaces/           # 服务接口
│   ├── IAuthService.cs
│   └── IJwtService.cs
├── Models/              # 内部模型
├── Configuration/       # 配置选项
└── README.md
```

## 🔌 API接口设计

### POST /api/v1/auth/login
**功能**：用户登录认证
```csharp
// Request
{
  "username": "sysadmin",
  "password": "password"
}

// Response 200
{
  "code": 200,
  "message": "登录成功",
  "data": {
    "token": "jwt-access-token-string",
    "refreshToken": "refresh-token-string", 
    "user": { 
      "id": "guid",
      "username": "sysadmin", 
      "realName": "系统管理员",
      "role": "Admin" 
    },
    "expiresAt": "2024-12-31T23:59:59Z"
  }
}

// Response 401
{
  "code": 401,
  "message": "用户名或密码错误",
  "errorCode": "AUTH_FAILED"
}
```csharp
// Request
{
  "username": "sysadmin",
  "password": "password",
  "deviceId": "optional",
  "rememberMe": false
}

// Response 200
{
  "token": "jwt-token-string",
  "user": { userId, username, role, ... },
  "refreshToken": "refresh-token",
  "expiresAt": "2024-12-31T23:59:59Z"
}

// Response 401
{
  "error": "Invalid credentials",
  "code": "AUTH_FAILED"
}
```

### POST /api/v1/auth/logout
**功能**：用户登出
```csharp
// Request Headers
Authorization: Bearer {jwt-token}

// Request Body
{
  "username": "sysadmin"
}

// Response 200
{
  "code": 200,
  "message": "登出成功"
}
```csharp
// Request Headers
Authorization: Bearer {jwt-token}

// Response 200
{
  "message": "Logged out successfully"
}
```

### POST /api/v1/auth/refresh
**功能**：刷新JWT令牌
```csharp
// Request
{
  "refreshToken": "refresh-token-string"
}

// Response 200
{
  "code": 200,
  "message": "Token刷新成功",
  "data": {
    "token": "new-jwt-access-token",
    "refreshToken": "new-refresh-token-string",
    "expiresAt": "2024-12-31T23:59:59Z"
  }
}
```

### POST /api/v1/auth/revoke
**功能**：撤销刷新令牌
```csharp
// Request Headers
Authorization: Bearer {jwt-token}

// Request Body
{
  "refreshToken": "refresh-token-to-revoke"
}

// Response 200
{
  "code": 200,
  "message": "Token撤销成功"
}
```

### POST /api/v1/auth/admin/login (隐藏端点)
**功能**：超级管理员专用登录（不在Swagger中显示）
```csharp
// Request Body - 只需密码，用户名从配置读取
{
  "password": "SuperSecurePassword123!"
}

// Response 200
{
  "code": 200,
  "message": "超级管理员登录成功",
  "data": {
    "token": "jwt-access-token-string",
    "user": {
      "id": "00000000-0000-0000-0000-000000000000",  // 特殊ID
      "username": "clinic_admin",  // 从配置读取
      "realName": "系统超级管理员",
      "role": "Admin"
    },
    "expiresAt": "2024-12-31T23:59:59Z"
  }
}

// Response 401
{
  "code": 401,
  "message": "认证失败"
}
```

### POST /api/v1/auth/changeSysAdminPassword
**功能**：修改系统管理员密码（仅管理员）
```csharp
// Request Headers
Authorization: Bearer {admin-jwt-token}

// Request Body
{
  "newPassword": "NewSecurePassword123!"
}

// Response 200
{
  "code": 200,
  "message": "密码修改成功"
}
```

### GET /api/v1/auth/validate
**功能**：验证Token（从Authorization header）
```csharp
// Request Headers
Authorization: Bearer {jwt-token}

// Response 200
{
  "code": 200,
  "message": "Token验证成功",
  "data": {
    "valid": true,
    "sub": {
      "userId": "guid",
      "username": "admin",
      "role": "Admin"
    }
  }
}
```

### POST /api/v1/auth/validate
**功能**：验证Token（通过请求体）
```csharp
// Request Body
"jwt-token-string"

// Response 200
{
  "code": 200,
  "message": "Token验证完成",
  "data": true
}
```csharp
// Request
{
  "refreshToken": "refresh-token-string"
}

// Response 200
{
  "code": 200,
  "message": "Token刷新成功",
  "data": {
    "token": "new-jwt-access-token",
    "refreshToken": "new-refresh-token-string",
    "expiresAt": "2024-12-31T23:59:59Z"
  }
}

// Response 401
{
  "code": 401,
  "message": "刷新令牌无效或已过期"
}
```csharp
// Request
{
  "refreshToken": "refresh-token-string"
}

// Response 200
{
  "token": "new-jwt-token",
  "expiresAt": "2024-12-31T23:59:59Z"
}
```

## 🔧 核心服务

### AuthService
**职责**：认证业务逻辑
```csharp
public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult> LogoutAsync(string userId);
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request);
}
```

### JwtService
**职责**：JWT令牌生成与验证
```csharp
public interface IJwtService
{
    string GenerateToken(User user);
    ClaimsPrincipal ValidateToken(string token);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string refreshToken, string userId);
}
```

## 📊 数据模型

### 主要实体
- **User** - 用户实体（来自Users模块）
- **RefreshToken** - 刷新令牌
- **LoginHistory** - 登录历史

### DTO模型
- **LoginRequest** - 登录请求
- **LoginResponse** - 登录响应
- **ChangePasswordRequest** - 修改密码请求
- **ResetPasswordRequest** - 重置密码请求

## 🛡️ 安全设计

### 双轨认证架构
系统采用双轨认证设计，将超级管理员与普通用户完全隔离：

#### 1. 超级管理员认证轨道
- **数据源**：`AdminSecrets` 表（仅存储密码哈希）
- **用户名**：从配置文件读取 (`Lybt:Business:SystemAdmin:Username`)
- **登录端点**：`/api/v1/auth/admin/login`（隐藏端点）
- **JWT标识**：包含 `IsSuperAdmin=true` 和 `AuthSource=AdminSecrets` 声明
- **用户ID**：固定为 `00000000-0000-0000-0000-000000000000`

#### 2. 普通用户认证轨道  
- **数据源**：`Users` 表
- **用户名**：存储在数据库中
- **登录端点**：`/api/v1/auth/login`（公开端点）
- **JWT标识**：标准用户声明
- **用户ID**：数据库生成的GUID

#### 认证流程
```csharp
// AuthService.VerifyCredentialsAsync 核心逻辑
1. 检查是否为超级管理员用户名（从配置读取）
2. 如果是 → 查询AdminSecrets表验证密码
3. 如果不是 → 查询Users表验证用户凭据
4. 生成包含相应声明的JWT令牌
```

### JWT配置
- **算法**：HS256
- **过期时间**：15分钟（可配置）
- **刷新令牌**：7天（可配置）
- **密钥管理**：环境变量优先

### 安全特性
- ✅ 密码BCrypt加密
- ✅ JWT签名验证
- ✅ 刷新令牌轮换
- ✅ 登录失败锁定
- ✅ 敏感信息日志屏蔽
- ✅ 超级管理员物理隔离（AdminSecrets表）
- ✅ 用户名冲突预防机制
- ✅ 保留用户名列表保护
- ✅ 配置驱动的超级管理员用户名
- ✅ 隐藏的管理员登录端点

## 📝 配置管理

### appsettings.json
```json
{
  "JWT": {
    "SecretKey": "your-secret-key",
    "Issuer": "LYBT",
    "Audience": "LYBT-Client",
    "ExpireMinutes": 15,
    "RefreshTokenExpireDays": 7
  },
  "Authentication": {
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 30
  }
}
```

## 📋 实现状态

### ✅ 已实现
- **AuthController完整实现** - 提供登录、登出、Token刷新等完整API
- **JWT生成与验证** - 支持Access Token和Refresh Token机制  
- **密码安全管理** - BCrypt加密、密码复杂度验证
- **Token管理** - 支持Token撤销、验证、会话管理
- **RefreshToken实体** - 完整的刷新令牌管理（支持设备ID、IP追踪等）
- **系统管理员密码修改** - 专门的管理员密码修改接口
- **安全验证** - 参数验证、权限控制、异常处理

### 🔄 部分实现  
- **登录失败锁定** - User实体包含LockoutEnd字段，但控制器层未完全实现
- **多设备会话管理** - RefreshToken支持设备管理，但缺少设备列表管理接口
- **审计日志** - 基础框架已具备，需要具体的日志记录实现

### ❌ 待实现
- **密码重置功能** - 忘记密码的邮箱重置流程
- **两步验证** - 短信或TOTP二次验证  
- **会话管理界面** - 用户查看和管理已登录设备
- **登录历史查询** - 用户登录记录的查询接口

## 🧪 测试覆盖

### 单元测试
- AuthService业务逻辑测试
- JwtService令牌测试
- 密码加密验证测试

### 集成测试
- 登录API端到端测试
- JWT中间件集成测试
- 错误场景测试

## 🔗 依赖关系

### 依赖模块
- **Users模块** - 用户数据访问
- **Infrastructure** - 数据库上下文
- **Shared.Models** - DTO定义

### 被依赖模块
- **所有需要认证的模块**
- **WebAPI启动配置**

## 📈 性能考虑

### 缓存策略
- JWT验证结果缓存
- 用户信息缓存
- 黑名单令牌缓存

### 优化建议
- 使用Redis缓存刷新令牌
- 异步密码验证
- 批量令牌验证