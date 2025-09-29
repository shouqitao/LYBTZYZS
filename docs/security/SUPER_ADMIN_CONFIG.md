# 超级管理员安全配置指南

## 架构设计

### 1. 数据隔离
- **AdminSecrets表**：仅存储密码哈希，不存储用户名
- **Users表**：存储普通用户和管理员，与超级管理员完全隔离
- **安全优势**：即使SQL注入攻击也无法获取超级管理员用户名

## 配置说明

### 1. 超级管理员用户名配置
超级管理员用户名通过配置文件指定，支持多环境灵活配置：

```json
// appsettings.json (默认配置)
{
  "Lybt": {
    "Business": {
      "SystemAdmin": {
        "Username": "clinic_admin",  // 可自定义修改
        "Email": "admin@lybt.com"
      }
    }
  }
}
```

### 2. 环境特定配置
不同环境可以使用不同的超级管理员用户名：

```json
// appsettings.Development.json
{
  "Lybt": {
    "Business": {
      "SystemAdmin": {
        "Username": "dev_admin"  // 开发环境用户名
      }
    }
  }
}

// appsettings.Production.json
{
  "Lybt": {
    "Business": {
      "SystemAdmin": {
        "Username": "prod_secure_admin_2025"  // 生产环境用户名
      }
    }
  }
}
```

### 3. 环境变量覆盖
支持通过环境变量动态修改：

```bash
# Windows PowerShell
$env:Lybt__Business__SystemAdmin__Username="custom_admin"

# Linux/Mac
export Lybt__Business__SystemAdmin__Username="custom_admin"

# Docker
docker run -e Lybt__Business__SystemAdmin__Username="docker_admin" ...
```

## 安全机制

### 1. 用户名冲突预防
系统自动防止创建与超级管理员相同的普通用户：

- **创建用户时检查**：UserService.CreateUserAsync
- **更新用户时检查**：UserService.UpdateUserAsync
- **保留用户名列表**：admin, administrator, root, system, superadmin, sysadmin

### 2. 认证流程
```
1. 用户登录 → AuthService.VerifyCredentialsAsync
2. 检查是否为超级管理员用户名（从配置读取）
3. 如果是 → 验证AdminSecrets表中的密码哈希
4. 如果不是 → 验证Users表中的用户凭据
```

### 3. 专用登录端点
- **普通用户**：`POST /api/v1/auth/login`
- **超级管理员**：`POST /api/v1/auth/admin/login` (隐藏端点)
  - 仅需提供密码
  - 用户名从配置自动读取
  - 不在Swagger文档中显示

## JWT令牌标识

超级管理员的JWT包含特殊声明：
```json
{
  "sub": "00000000-0000-0000-0000-000000000000",  // 特殊ID
  "name": "clinic_admin",  // 配置中的用户名
  "role": "Admin",
  "IsSuperAdmin": "true",  // 超级管理员标识
  "AuthSource": "AdminSecrets"  // 认证来源
}
```

## 最佳实践

### 1. 生产环境建议
- 使用复杂的、不易猜测的超级管理员用户名
- 定期更换超级管理员密码
- 启用审计日志记录所有超级管理员操作
- 限制超级管理员登录IP（通过防火墙或应用层）

### 2. 配置示例（高安全性）
```json
{
  "Lybt": {
    "Business": {
      "SystemAdmin": {
        "Username": "lybt_sa_2025_q1_prod",  // 包含时间戳的复杂用户名
        "Email": "security@lybt.com"
      }
    }
  }
}
```

### 3. 监控建议
- 监控所有超级管理员登录尝试
- 设置失败登录次数阈值告警
- 记录所有配置更改
- 定期审查超级管理员操作日志

## 故障排除

### 问题：无法登录超级管理员
1. 检查配置文件中的Username设置
2. 确认AdminSecrets表中有种子数据
3. 验证密码哈希是否正确
4. 查看日志中的认证错误信息

### 问题：普通用户与超级管理员用户名冲突
- 系统会自动阻止创建
- 返回错误："用户名为系统保留用户名"
- 解决方案：选择其他用户名或修改配置

## 代码位置参考
- 认证服务：`src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- 用户服务：`src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`
- 认证控制器：`src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`
- 数据模型：`src/Server/Core/LYBT.Entities/Users/AdminSecretModel.cs`