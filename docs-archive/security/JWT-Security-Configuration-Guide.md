# JWT安全配置指南

## 概述
本文档详细说明凌隐宝堂中医诊所管理系统的JWT安全配置要求和最佳实践。

## 安全配置清单

### ✅ 已完成的安全加固
- [x] **强密钥生成**：使用88字符的Base64编码密钥（替换原有的弱密钥）
- [x] **Token过期时间优化**：
  - AccessToken: 30分钟（原480分钟）
  - RefreshToken: 7天（原30天）
- [x] **RefreshToken机制**：实现了持久化的RefreshToken管理
- [x] **Token黑名单机制**：支持Token撤销和黑名单管理
- [x] **环境变量配置**：敏感信息通过环境变量管理

### 🔐 密钥管理

#### 生成强密钥
```powershell
# PowerShell命令生成64字节Base64密钥
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
```

#### 密钥存储要求
- **开发环境**：可以使用appsettings.json（仅限开发）
- **生产环境**：必须使用环境变量或密钥管理服务
- **密钥长度**：至少32字符，建议64字符以上
- **密钥轮换**：每90天轮换一次

### 📋 配置文件说明

#### appsettings.json（开发环境）
```json
{
  "JwtOptions": {
    "Secret": "开发环境密钥（至少32字符）",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "ExpireMinutes": 30,
    "RememberMeExpireMinutes": 10080,
    "ClockSkewSeconds": 300
  }
}
```

#### appsettings.Production.json（生产环境）
```json
{
  "JwtOptions": {
    "Secret": "${JWT_SECRET}",  // 从环境变量读取
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "ExpireMinutes": 30,
    "RememberMeExpireMinutes": 10080,
    "ClockSkewSeconds": 300
  }
}
```

### 🛡️ Token生命周期管理

| Token类型 | 过期时间 | 用途 | 存储位置 |
|---------|---------|------|---------|
| AccessToken | 30分钟 | API访问授权 | 客户端内存 |
| RefreshToken | 7天 | 刷新AccessToken | 数据库+客户端安全存储 |

### 🚫 Token撤销机制

#### 黑名单触发场景
1. 用户主动登出
2. 密码更改
3. 账户锁定
4. 安全威胁检测
5. 管理员手动撤销

#### 黑名单清理策略
- 自动清理已过期的黑名单记录
- 保留30天的审计日志

### 🔒 安全最佳实践

#### 客户端存储
- **AccessToken**：仅存储在内存中，避免localStorage/sessionStorage
- **RefreshToken**：使用httpOnly、secure、sameSite的cookie存储

#### 传输安全
- 强制使用HTTPS
- 配置HSTS头部
- 启用证书固定（Certificate Pinning）

#### 防护措施
- 实施速率限制（登录：5次/分钟）
- 监控异常Token使用模式
- 记录所有认证相关事件
- 实施IP地址白名单（可选）

### 📊 监控和审计

#### 监控指标
- Token颁发频率
- 刷新Token使用率
- 失败认证尝试
- 黑名单Token数量

#### 审计日志
记录以下事件：
- Token生成
- Token刷新
- Token撤销
- 认证失败
- 账户锁定

### 🚀 部署检查清单

- [ ] 生产环境使用强密钥（88字符以上）
- [ ] 密钥通过环境变量配置
- [ ] HTTPS已启用并强制
- [ ] 数据库迁移已执行（RefreshTokens和BlacklistedTokens表）
- [ ] 日志系统已配置
- [ ] 监控告警已设置
- [ ] 定期密钥轮换计划已制定

### 📚 相关文档
- [OWASP JWT安全指南](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [RFC 7519 - JSON Web Token](https://datatracker.ietf.org/doc/html/rfc7519)
- [环境变量配置示例](.env.example)

### 🆘 故障排除

#### 常见问题
1. **"JWT密钥太短"错误**
   - 确保密钥至少32字符
   - 使用提供的PowerShell命令生成新密钥

2. **Token验证失败**
   - 检查时钟同步（ClockSkew设置）
   - 验证Issuer和Audience配置
   - 确认密钥一致性

3. **RefreshToken无效**
   - 检查数据库连接
   - 验证RefreshToken未过期
   - 确认Token未被撤销

### 📝 更新日志
- 2025-09-26：初始JWT安全加固实施
  - 更新密钥强度
  - 缩短Token过期时间
  - 实现RefreshToken机制
  - 添加Token黑名单功能