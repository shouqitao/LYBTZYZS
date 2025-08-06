# Auth模块实现总结

## 模块概述
Auth模块负责系统的身份认证和授权功能，是整个系统的安全基础。

## 已完成功能

### 1. JWT令牌管理 ✅
**文件**: `LYBT.Infrastructure/Authentication/JwtAuthenticationService.cs`
- 生成JWT令牌（支持记住我功能）
- 验证JWT令牌
- 刷新令牌机制
- 从令牌提取用户信息

**配置特点**：
- 普通登录：8小时有效期
- 记住我：30天有效期
- 使用HMAC-SHA256签名算法

### 2. 登录功能 ✅
**文件**: `LYBT.WebAPI/Controllers/AuthController.cs`
- 支持普通用户登录
- 支持SysAdmin特殊账户登录
- 返回JWT令牌和用户信息
- 记录登录日志

### 3. 登出功能 ✅
**文件**: `AuthController.cs` - `Logout`方法
- 记录登出日志
- 清理服务端状态（如需要）

### 4. 令牌刷新 ✅
**文件**: `AuthController.cs` - `RefreshToken`方法
- 验证现有令牌
- 生成新令牌
- 返回刷新时间

### 5. 密码修改 ✅
**文件**: `AuthController.cs` - `ChangePassword`方法
- 支持SysAdmin密码修改
- 验证旧密码
- 更新密码哈希

### 6. 登录失败次数限制 ✅ 【新增】
**文件**: `LYBT.Module.Auth/Services/LoginAttemptService.cs`
- 跟踪登录失败次数
- 3次失败后锁定15分钟
- 自动解锁机制
- 登录成功后清除失败记录

**防护特性**：
- 基于用户名的锁定（不区分大小写）
- 使用内存缓存提高性能
- 支持获取剩余锁定时间
- 防暴力破解攻击

### 7. 获取当前用户 ✅
**文件**: `AuthController.cs` - `GetCurrentUser`方法
- 从JWT令牌解析用户信息
- 返回用户基本信息和角色

## 技术实现亮点

### 1. 分层架构
```
Controller层（AuthController）
    ↓
Service层（AuthService）
    ↓
Repository层（AuthRepository）
    ↓
Infrastructure层（JwtAuthenticationService）
```

### 2. 依赖注入配置
```csharp
// ServiceCollectionExtension.cs
services.AddScoped<IAuthRepository, AuthRepository>();
services.AddScoped<SysAdminHandler>();
services.AddSingleton<ILoginAttemptService, LoginAttemptService>();
services.AddScoped<IAuthService, AuthService>();
```

### 3. 安全特性
- 密码使用BCrypt哈希存储
- JWT令牌包含用户ID、用户名、角色
- 支持基于角色的授权
- 防暴力破解保护

## API接口清单

| 端点 | 方法 | 说明 | 认证要求 |
|-----|-----|------|---------|
| `/api/v1/auth/login` | POST | 用户登录 | 无 |
| `/api/v1/auth/logout` | POST | 用户登出 | 需要 |
| `/api/v1/auth/refresh-token` | POST | 刷新令牌 | 需要 |
| `/api/v1/auth/change-password` | POST | 修改密码 | 需要 |
| `/api/v1/auth/current-user` | GET | 获取当前用户 | 需要 |

## 请求/响应示例

### 登录请求
```json
POST /api/v1/auth/login
{
    "username": "admin",
    "password": "Admin@123456",
    "rememberMe": false
}
```

### 登录响应
```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
        "id": "uuid",
        "username": "admin",
        "realName": "管理员",
        "role": "Admin",
        "email": "admin@example.com",
        "phoneNumber": "13800138000",
        "isActive": true
    }
}
```

### 登录失败（账户锁定）
```json
{
    "title": "认证失败",
    "detail": "账户已被锁定，请15分钟后再试",
    "status": 401
}
```

## 配置项

### JWT配置（appsettings.json）
```json
{
    "Jwt": {
        "Secret": "your-secret-key-at-least-32-characters",
        "Issuer": "LYBT.WebAPI",
        "Audience": "LYBT.Client",
        "ExpireMinutes": 480,        // 8小时
        "RememberMeExpireMinutes": 43200  // 30天
    }
}
```

### 登录保护配置
- 最大失败次数：3次
- 锁定时长：15分钟
- 锁定范围：基于用户名

## 使用示例

### 前端集成
```javascript
// 登录
const response = await fetch('/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        username: 'admin',
        password: 'Admin@123456',
        rememberMe: false
    })
});

const data = await response.json();
if (response.ok) {
    // 保存令牌
    localStorage.setItem('token', data.token);
    // 保存用户信息
    localStorage.setItem('user', JSON.stringify(data.user));
}

// 后续请求携带令牌
fetch('/api/v1/patients', {
    headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
    }
});
```

## 安全建议

1. **生产环境配置**
   - 使用强密钥（至少32字符）
   - 启用HTTPS
   - 配置CORS策略

2. **密码策略**
   - 强制复杂密码
   - 定期更换密码
   - 避免使用默认密码

3. **监控建议**
   - 监控失败登录尝试
   - 记录所有认证事件
   - 设置异常登录告警

## 待优化项

1. **双因素认证**（2FA）
2. **单点登录**（SSO）
3. **OAuth2.0集成**
4. **会话管理增强**
5. **审计日志完善**

## 测试覆盖

- [x] 正常登录测试
- [x] 密码错误测试
- [x] 账户锁定测试
- [x] 令牌刷新测试
- [x] 密码修改测试
- [ ] 并发登录测试
- [ ] 令牌过期测试
- [ ] 性能压力测试

## 总结

Auth模块已完成所有基础功能，包括：
- JWT令牌认证
- 登录/登出管理
- 密码安全管理
- 防暴力破解保护

该模块为系统提供了可靠的身份认证基础，满足中小型诊所的安全需求。