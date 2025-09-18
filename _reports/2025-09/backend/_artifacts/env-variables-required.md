# LYBT.WebAPI 生产环境必需环境变量清单

**生成时间**: 2025-09-18  
**适用版本**: P4-Server Release  

## 必需环境变量

### 1. 加密相关
- **ENCRYPTION_KEY**: 数据加密密钥
  - **最小长度**: 32字符
  - **建议**: 使用随机生成的强密钥
  - **示例**: `openssl rand -base64 32`

- **JWT_SECRET**: JWT令牌签名密钥
  - **最小长度**: 32字符
  - **重要性**: 关键安全组件，泄露将导致认证体系失效
  - **建议**: 定期轮换，使用高强度随机密钥

### 2. 默认密码
- **ADMIN_DEFAULT_PASSWORD**: 系统管理员默认密码
  - **最小长度**: 8字符
  - **用途**: sysadmin用户初始化密码
  - **建议**: 包含大小写字母、数字和特殊字符

- **USER_DEFAULT_PASSWORD**: 新用户默认密码
  - **最小长度**: 8字符
  - **用途**: 新建用户的临时密码
  - **建议**: 强制用户首次登录后修改

## 配置验证

### 手动验证
```powershell
# 运行验证脚本
.\scripts\deploy\verify-env.ps1

# 检查特定变量
$env:JWT_SECRET
$env:ADMIN_DEFAULT_PASSWORD
```

### 部署前检查清单
- [ ] 所有必需环境变量已设置
- [ ] JWT_SECRET长度≥32字符且不包含"Development"
- [ ] 密码符合强度要求
- [ ] 环境变量已从配置文件占位符${VAR}正确替换

## 安全注意事项

### 密钥管理
- 使用密钥管理服务(如Azure Key Vault, AWS Secrets Manager)
- 避免在代码、日志中硬编码敏感信息
- 实施定期密钥轮换策略

### 访问控制
- 限制环境变量访问权限
- 定期审计密钥使用情况
- 监控异常认证活动

## 示例配置脚本
```bash
# Linux/Docker环境
export ENCRYPTION_KEY="$(openssl rand -base64 32)"
export JWT_SECRET="$(openssl rand -base64 32)" 
export ADMIN_DEFAULT_PASSWORD="SecureAdmin@123!"
export USER_DEFAULT_PASSWORD="DefaultUser@456!"

# Windows环境
$env:ENCRYPTION_KEY = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:JWT_SECRET = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:ADMIN_DEFAULT_PASSWORD = "SecureAdmin@123!"
$env:USER_DEFAULT_PASSWORD = "DefaultUser@456!"
```

---
*此文档是P4-Server发布就绪流程的一部分*