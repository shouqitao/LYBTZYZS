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
- AuthController完整实现
- JWT生成与验证
- 登录登出流程
- 密码加密验证
- 错误处理机制

### 🔄 待优化
- 登录失败锁定机制
- 审计日志记录
- 多设备会话管理

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