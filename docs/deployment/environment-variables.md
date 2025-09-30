# 环境变量配置参考

**最后更新**: 2025-09-30  
**适用版本**: LYBT v1.0+

本文档提供生产环境所有环境变量的详细配置参考。

---

## 📋 配置项总览

| # | 环境变量名 | 配置路径 | 优先级 | 用途 |
|---|-----------|---------|--------|------|
| 1 | `ConnectionStrings__DefaultConnection` | ConnectionStrings:DefaultConnection | ⚠️ Critical | 数据库连接字符串 |
| 2 | `Lybt__Authentication__Jwt__SecretKey` | Lybt:Authentication:Jwt:SecretKey | ⚠️ Critical | JWT 签名密钥 |
| 3 | `Lybt__Authentication__DefaultPasswords__SysAdminPassword` | Lybt:Authentication:DefaultPasswords:SysAdminPassword | 📌 Important | 管理员默认密码 |
| 4 | `Lybt__Authentication__DefaultPasswords__NewUserPassword` | Lybt:Authentication:DefaultPasswords:NewUserPassword | 📌 Important | 新用户默认密码 |
| 5 | `Lybt__Business__SystemAdmin__Username` | Lybt:Business:SystemAdmin:Username | 📌 Important | 管理员用户名 |
| 6 | `Lybt__Business__SystemAdmin__Email` | Lybt:Business:SystemAdmin:Email | 📌 Important | 管理员邮箱 |
| 7 | `AllowedHosts` | AllowedHosts | 💡 Optional | 允许的主机名 |

---

## 🔐 Critical 配置（必须设置）

### 1. 数据库连接字符串

**环境变量**: `ConnectionStrings__DefaultConnection`  
**配置路径**: `ConnectionStrings:DefaultConnection`  
**优先级**: ⚠️ **Critical**

#### 用途
SQL Server 数据库连接字符串，应用程序所有数据访问的核心配置。

#### 格式示例

**Windows 集成认证**:
```
Server=localhost;Database=LYBTDB;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

**SQL Server 认证（推荐生产环境）**:
```
Server=prod-sql.company.com,1433;Database=LYBTDB;User Id=lybt_app;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=60
```

**Azure SQL Database**:
```
Server=tcp:yourserver.database.windows.net,1433;Database=LYBTDB;User Id=lybt_app@yourserver;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60
```

#### 配置说明

| 参数 | 必需 | 说明 |
|------|------|------|
| `Server` | ✅ | 服务器地址和端口（默认 1433） |
| `Database` | ✅ | 数据库名称（必须为 LYBTDB） |
| `User Id` | 条件 | SQL 认证用户名（非集成认证时必需） |
| `Password` | 条件 | SQL 认证密码（非集成认证时必需） |
| `Integrated Security` | 条件 | Windows 集成认证（True/False） |
| `TrustServerCertificate` | ✅ | 信任服务器证书（自签名证书时设为 True） |
| `MultipleActiveResultSets` | 推荐 | 启用 MARS（建议设为 True） |
| `Connection Timeout` | 可选 | 连接超时（秒，默认 15） |
| `Encrypt` | 推荐 | 启用加密连接（生产环境建议 True） |

#### 设置方法

**Windows**:
```powershell
setx ConnectionStrings__DefaultConnection "Server=localhost;Database=LYBTDB;Integrated Security=True;TrustServerCertificate=True" /M
```

**Linux**:
```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=LYBTDB;User Id=lybt_app;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

#### 安全建议

- ✅ 使用独立的应用数据库账户（非 sa）
- ✅ 仅授予必要权限（db_datareader + db_datawriter）
- ✅ 定期轮换密码（建议每 90 天）
- ✅ 启用加密连接（`Encrypt=True`）
- ✅ 限制连接来源 IP

---

### 2. JWT 签名密钥

**环境变量**: `Lybt__Authentication__Jwt__SecretKey`  
**配置路径**: `Lybt:Authentication:Jwt:SecretKey`  
**优先级**: ⚠️ **Critical**

#### 用途
用于签名和验证 JWT 令牌的密钥，直接影响系统安全性。

#### 要求

- ✅ 至少 **32 字符**（256 位）
- ✅ 使用加密安全的随机数生成
- ✅ 包含大小写字母、数字、特殊字符
- ❌ 不可使用可预测的字符串

#### 生成方法

**Windows PowerShell**:
```powershell
$bytes = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
Write-Host "生成的密钥: $jwtKey"
```

**Linux**:
```bash
openssl rand -base64 32
```

#### 示例
```
gF8xK2pLmNqR7tUwV9yZ1bC3dE4fG5hI6jK7lM8nO9pQ0rS1tU2vW3xY4zA5B6c=
```

#### 设置方法

**Windows**:
```powershell
setx Lybt__Authentication__Jwt__SecretKey "YOUR_GENERATED_KEY" /M
```

**Linux**:
```bash
export Lybt__Authentication__Jwt__SecretKey="YOUR_GENERATED_KEY"
```

#### 安全建议

- ✅ 每 **90 天**轮换一次密钥
- ✅ 不要在日志中记录密钥
- ✅ 备份时加密存储
- ✅ 限制环境变量访问权限

---

## 📌 Important 配置（强烈建议设置）

### 3. 系统管理员默认密码

**环境变量**: `Lybt__Authentication__DefaultPasswords__SysAdminPassword`  
**配置路径**: `Lybt:Authentication:DefaultPasswords:SysAdminPassword`  
**优先级**: 📌 **Important**

#### 用途
系统管理员账户的初始密码（首次创建时使用）。

#### 密码要求（Production）

- 最小长度: 12 字符
- 必须包含: 大写字母 + 小写字母 + 数字 + 特殊字符

#### 示例
```
Admin@2025SecurePassword
```

#### 设置方法

**Windows**:
```powershell
setx Lybt__Authentication__DefaultPasswords__SysAdminPassword "Admin@2025SecurePassword" /M
```

**Linux**:
```bash
export Lybt__Authentication__DefaultPasswords__SysAdminPassword="Admin@2025SecurePassword"
```

#### 安全建议

- ✅ 首次登录后**立即修改**
- ✅ 不要使用常见密码
- ✅ 启用首次登录强制修改密码（`ForceChangeOnFirstLogin: true`）

---

### 4. 新用户默认密码

**环境变量**: `Lybt__Authentication__DefaultPasswords__NewUserPassword`  
**配置路径**: `Lybt:Authentication:DefaultPasswords:NewUserPassword`  
**优先级**: 📌 **Important**

#### 用途
新建用户账户的初始密码（由管理员创建用户时使用）。

#### 密码要求（同管理员密码）

- 最小长度: 12 字符
- 必须包含: 大写字母 + 小写字母 + 数字 + 特殊字符

#### 示例
```
User@2025InitialPassword
```

#### 设置方法

**Windows**:
```powershell
setx Lybt__Authentication__DefaultPasswords__NewUserPassword "User@2025InitialPassword" /M
```

**Linux**:
```bash
export Lybt__Authentication__DefaultPasswords__NewUserPassword="User@2025InitialPassword"
```

#### 安全建议

- ✅ 与管理员密码使用不同的值
- ✅ 启用首次登录强制修改密码

---

### 5. 系统管理员用户名

**环境变量**: `Lybt__Business__SystemAdmin__Username`  
**配置路径**: `Lybt:Business:SystemAdmin:Username`  
**优先级**: 📌 **Important**

#### 用途
系统管理员账户的登录用户名。

#### 要求

- 长度: 3-50 字符
- 仅包含: 字母、数字、下划线

#### 示例
```
admin
sysadmin
administrator
```

#### 设置方法

**Windows**:
```powershell
setx Lybt__Business__SystemAdmin__Username "admin" /M
```

**Linux**:
```bash
export Lybt__Business__SystemAdmin__Username="admin"
```

#### 安全建议

- ✅ 避免使用 "admin"、"root" 等常见名称
- ✅ 使用组织内部命名规范

---

### 6. 系统管理员邮箱

**环境变量**: `Lybt__Business__SystemAdmin__Email`  
**配置路径**: `Lybt:Business:SystemAdmin:Email`  
**优先级**: 📌 **Important**

#### 用途
系统管理员的邮箱地址（用于通知、密码重置等）。

#### 格式要求

- 标准邮箱格式: `user@domain.com`
- 必须有效且可接收邮件

#### 示例
```
admin@company.com
sysadmin@yourdomain.com
```

#### 设置方法

**Windows**:
```powershell
setx Lybt__Business__SystemAdmin__Email "admin@company.com" /M
```

**Linux**:
```bash
export Lybt__Business__SystemAdmin__Email="admin@company.com"
```

#### 验证

应用启动时会验证邮箱格式（正则表达式：`^[^@]+@[^@]+\.[^@]+$`）。

---

## 💡 Optional 配置（可选）

### 7. 允许的主机名

**环境变量**: `AllowedHosts`  
**配置路径**: `AllowedHosts`  
**优先级**: 💡 **Optional**

#### 用途
ASP.NET Core 主机过滤中间件使用，限制可访问的域名（防止主机头攻击）。

#### 格式

多个主机名用分号 `;` 分隔。

#### 示例

**单个域名**:
```
example.com
```

**多个域名**:
```
example.com;www.example.com
```

**通配符**:
```
*.example.com;example.com
```

**允许所有（不推荐生产环境）**:
```
*
```

#### 设置方法

**Windows**:
```powershell
setx AllowedHosts "example.com;*.example.com" /M
```

**Linux**:
```bash
export AllowedHosts="example.com;*.example.com"
```

#### 默认值

如果未设置，默认为 `*`（允许所有主机）。

#### 安全建议

- ✅ 生产环境应明确指定允许的域名
- ✅ 使用通配符时需谨慎
- ❌ 避免使用 `*`（允许所有）

---

## 🔧 验证配置

### 使用验证脚本

```powershell
.\scripts\validate-production-config.ps1
```

### 手动验证

**Windows**:
```powershell
# 查看系统级环境变量
[Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Machine")
[Environment]::GetEnvironmentVariable("Lybt__Authentication__Jwt__SecretKey", "Machine")
```

**Linux**:
```bash
# 查看当前会话环境变量
echo $ConnectionStrings__DefaultConnection
echo $Lybt__Authentication__Jwt__SecretKey
```

---

## 🌍 环境变量优先级

.NET 配置系统按以下优先级加载（后者覆盖前者）：

1. `appsettings.json`（基础配置）
2. `appsettings.Production.json`（环境特定配置）
3. **环境变量**（最高优先级） ⭐

因此，环境变量会覆盖配置文件中的同名配置项。

---

## 📝 配置映射规则

JSON 配置路径与环境变量名称的映射规则：

| JSON 路径 | 环境变量名 |
|-----------|-----------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Lybt:Authentication:Jwt:SecretKey` | `Lybt__Authentication__Jwt__SecretKey` |

**规则**: 将冒号 `:` 替换为双下划线 `__`

---

## 相关文档

- 📖 [生产环境配置指南](./production-setup.md) - 快速开始指南
- 🔒 [安全检查清单](./security-checklist.md) - 部署前安全检查
- 🔧 [问题排查指南](./troubleshooting.md) - 常见问题解决

---

**📝 文档版本**: 1.0  
**🔄 最后更新**: 2025-09-30  
**✍️ 维护者**: LYBT Team