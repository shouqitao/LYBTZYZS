# 安全配置管理系统

本系统实现了全面的安全配置管理，包括密码策略、CORS配置、安全头、JWT配置验证等功能。

## 核心组件

### 1. SecurityOptions配置类

位置：`LYBT.Infrastructure.Options.SecurityOptions`

包含以下配置模块：
- **HTTPS配置**：强制HTTPS、HSTS设置
- **CORS配置**：跨域资源共享策略
- **安全头配置**：CSP、X-Frame-Options等
- **密码策略**：密码复杂度要求
- **API限流**：防止暴力攻击
- **环境安全**：生产环境安全配置

### 2. 安全中间件

#### SecurityHeadersMiddleware
自动添加安全头到所有HTTP响应：
```csharp
app.UseSecurityHeaders();
```

### 3. 安全服务

#### IPasswordValidationService
密码强度验证和生成：
```csharp
var result = await _passwordValidator.ValidatePasswordAsync(password, username);
var securePassword = _passwordValidator.GenerateSecurePassword(16);
```

#### ISecurityConfigurationValidator
安全配置验证：
```csharp
var validation = await _securityValidator.ValidateConfigurationAsync();
```

## 配置文件

### 基本配置
在 `appsettings.json` 或 `appsettings.Security.json` 中：

```json
{
  "Security": {
    "Https": {
      "RequireHttps": true,
      "HstsMaxAgeDays": 365,
      "HstsIncludeSubdomains": true,
      "HstsPreload": true
    },
    "Cors": {
      "AllowedOrigins": [
        "https://localhost:5001",
        "https://yourdomain.com"
      ],
      "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowedHeaders": ["Content-Type", "Authorization"],
      "AllowCredentials": true,
      "PreflightMaxAge": 3600
    },
    "SecurityHeaders": {
      "ContentSecurityPolicy": "default-src 'self'; script-src 'self'",
      "XFrameOptions": "DENY",
      "XContentTypeOptions": "nosniff"
    },
    "PasswordPolicy": {
      "MinLength": 12,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialChar": true,
      "PasswordExpiryDays": 90
    }
  }
}
```

### 环境变量
敏感信息使用环境变量：
```json
{
  "JwtOptions": {
    "Secret": "${JWT_SECRET}"
  },
  "SysAdminOptions": {
    "DefaultPassword": "${ADMIN_DEFAULT_PASSWORD}"
  }
}
```

## API接口

### SecurityController
提供安全管理API（需要Admin权限）：

#### 验证安全配置
```http
GET /api/v1/security/configuration/validation
```

#### 验证密码强度
```http
POST /api/v1/security/password/validation
Content-Type: application/json

{
  "password": "MyP@ssw0rd123",
  "username": "user123"
}
```

#### 生成安全密码
```http
POST /api/v1/security/password/generate
Content-Type: application/json

{
  "length": 16
}
```

#### 获取安全摘要
```http
GET /api/v1/security/configuration/summary
```

## 安全特性

### 1. 自动安全头
- **Content-Security-Policy**：防止XSS攻击
- **X-Frame-Options**：防止点击劫持
- **X-Content-Type-Options**：防止MIME嗅探
- **Referrer-Policy**：控制引用信息泄露
- **Permissions-Policy**：限制浏览器API使用

### 2. CORS安全策略
- 开发环境：宽松配置，支持本地开发
- 生产环境：严格配置，只允许指定域名

### 3. 密码安全
- 可配置的复杂度要求
- 禁止常见弱密码模式
- 键盘模式检测
- 密码历史记录
- 自动密码过期

### 4. JWT安全
- 密钥强度验证
- 过期时间检查
- 配置安全性审核

### 5. 环境安全
- 生产环境严格配置验证
- 敏感信息隐藏
- 错误信息保护

## 启动时安全验证

系统启动时自动执行安全配置验证：

```
✅ 安全配置验证通过
⚠️ 安全警告: JWT过期时间过长，建议设置为更短的时间以提高安全性
❌ 安全配置错误: 生产环境必须配置具体的CORS源
```

## 最佳实践

### 1. 生产环境配置
- 使用环境变量存储敏感信息
- 配置具体的AllowedHosts
- 启用HTTPS重定向和HSTS
- 设置严格的CORS策略

### 2. 密码管理
- 强制用户首次登录修改默认密码
- 定期密码更换提醒
- 使用生成的安全密码

### 3. 监控和审计
- 启用安全事件日志记录
- 定期检查安全配置
- 监控异常登录尝试

### 4. 开发环境
- 使用开发环境专用配置
- 避免在开发中使用生产密钥
- 定期更新安全配置

## 故障排除

### 常见问题

1. **CORS错误**
   - 检查AllowedOrigins配置
   - 确认请求头设置正确
   - 验证预检请求处理

2. **JWT验证失败**
   - 检查密钥配置
   - 验证过期时间设置
   - 确认时钟偏移配置

3. **安全头不生效**
   - 确认中间件注册顺序
   - 检查配置文件格式
   - 验证环境变量设置

### 调试模式
开发环境可启用详细日志：
```json
{
  "Logging": {
    "LogLevel": {
      "LYBT.WebAPI.Middleware.SecurityHeadersMiddleware": "Debug",
      "LYBT.WebAPI.Services.SecurityConfigurationValidator": "Debug"
    }
  }
}
```

## 更新和维护

### 配置更新
1. 更新配置文件
2. 重启应用程序
3. 验证安全配置状态
4. 测试相关功能

### 定期审核
- 每季度审核安全配置
- 检查密码策略有效性
- 更新安全头配置
- 验证CORS策略

## 相关文档
- [开发规范](./开发规范.md)
- [API响应标准](./API响应标准.md)
- [默认密码文档](./development/DEFAULT_PASSWORDS.md)