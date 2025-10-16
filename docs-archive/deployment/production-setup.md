# 生产环境配置指南

**最后更新**: 2025-09-30  
**适用版本**: LYBT v1.0+

## 📋 目录

- [前提条件](#前提条件)
- [配置步骤](#配置步骤)
- [快速开始](#快速开始)
- [验证配置](#验证配置)
- [常见问题](#常见问题)
- [安全建议](#安全建议)

---

## 前提条件

在配置生产环境之前，请确保已安装以下组件：

- ✅ **.NET 8.0 Runtime** 或更高版本
- ✅ **SQL Server 2019+** 或兼容数据库
- ✅ **Windows Server 2019+** 或 **Linux**（Ubuntu 20.04+）
- ✅ **管理员权限**（用于设置环境变量）

---

## 配置步骤

### 步骤 1：准备配置信息

生产环境需要配置以下 **7 个关键配置项**：

| 配置项 | 优先级 | 说明 |
|--------|--------|------|
| 数据库连接字符串 | ⚠️ **Critical** | SQL Server 连接信息 |
| JWT 签名密钥 | ⚠️ **Critical** | 至少 32 字符的随机密钥 |
| 管理员默认密码 | 📌 Important | 系统管理员初始密码 |
| 新用户默认密码 | 📌 Important | 新建用户的初始密码 |
| 管理员用户名 | 📌 Important | 系统管理员登录名 |
| 管理员邮箱 | 📌 Important | 系统管理员邮箱地址 |
| 允许的主机名 | 💡 Optional | CORS 允许的域名 |

详细配置项说明请参考 [环境变量配置参考](./environment-variables.md)。

---

### 步骤 2：设置环境变量

#### Windows 方式

使用 PowerShell **以管理员身份**运行：

```powershell
# 1. 数据库连接字符串（Critical）
setx ConnectionStrings__DefaultConnection "Server=YOUR_SQL_SERVER;Database=LYBTDB;User Id=lybt_app;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True" /M

# 2. JWT 签名密钥（Critical）- 建议自动生成
$bytes = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
setx Lybt__Authentication__Jwt__SecretKey $jwtKey /M
Write-Host "生成的 JWT 密钥: $jwtKey" -ForegroundColor Green

# 3. 管理员默认密码（Important）
setx Lybt__Authentication__DefaultPasswords__SysAdminPassword "Admin@123456" /M

# 4. 新用户默认密码（Important）
setx Lybt__Authentication__DefaultPasswords__NewUserPassword "User@123456" /M

# 5. 管理员用户名（Important）
setx Lybt__Business__SystemAdmin__Username "admin" /M

# 6. 管理员邮箱（Important）
setx Lybt__Business__SystemAdmin__Email "admin@yourdomain.com" /M

# 7. 允许的主机名（Optional）
setx AllowedHosts "yourdomain.com;*.yourdomain.com" /M
```

**⚠️ 重要提示**：
- 使用 `/M` 参数设置**系统级**环境变量（需要管理员权限）
- 设置后需要**重启应用程序**才能生效
- 环境变量修改后，当前 PowerShell 会话不会自动更新

#### Linux 方式

编辑 `/etc/environment` 或创建 systemd 服务配置：

```bash
# 方式 1: 编辑 /etc/environment（需要 sudo）
sudo nano /etc/environment

# 添加以下内容：
ConnectionStrings__DefaultConnection="Server=YOUR_SQL_SERVER;Database=LYBTDB;User Id=lybt_app;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True"
Lybt__Authentication__Jwt__SecretKey="YOUR_GENERATED_JWT_KEY"
Lybt__Authentication__DefaultPasswords__SysAdminPassword="Admin@123456"
Lybt__Authentication__DefaultPasswords__NewUserPassword="User@123456"
Lybt__Business__SystemAdmin__Username="admin"
Lybt__Business__SystemAdmin__Email="admin@yourdomain.com"
AllowedHosts="yourdomain.com;*.yourdomain.com"

# 方式 2: systemd 服务配置（推荐）
sudo nano /etc/systemd/system/lybt-webapi.service

# 添加环境变量到 [Service] 部分：
[Service]
Environment="ConnectionStrings__DefaultConnection=Server=..."
Environment="Lybt__Authentication__Jwt__SecretKey=..."
# ... 其他配置

# 重新加载 systemd 配置
sudo systemctl daemon-reload
sudo systemctl restart lybt-webapi
```

**生成 JWT 密钥（Linux）**：

```bash
# 使用 OpenSSL 生成 32 字节随机密钥
openssl rand -base64 32
```

---

### 步骤 3：验证配置

在启动应用前，使用验证脚本检查配置：

```powershell
# Windows PowerShell
.\scripts\validate-production-config.ps1
```

```bash
# Linux（如果有 PowerShell）
pwsh ./scripts/validate-production-config.ps1
```

**预期输出**（配置正确时）：

```
✅ 数据库连接字符串
✅ JWT 签名密钥
✅ 管理员默认密码
✅ 新用户默认密码
✅ 管理员用户名
✅ 管理员邮箱
✅ 允许的主机名

验证结果:
  错误: 0
  警告: 0
```

---

### 步骤 4：启动应用

```powershell
# 设置环境为 Production
$env:ASPNETCORE_ENVIRONMENT="Production"

# 启动应用
dotnet LYBT.WebAPI.dll
```

**成功启动标志**：

```
[INF] ✅ Production 配置验证通过
[INF] Now listening on: https://localhost:5001
[INF] Application started. Press Ctrl+C to shut down.
```

**配置错误示例**：

```
╔═══════════════════════════════════════════════════════════╗
║  ❌ Production 配置验证失败                               ║
╚═══════════════════════════════════════════════════════════╝

发现 2 个配置错误：

⚠️ CRITICAL 错误（必须修复）:

  [1] 数据库连接字符串
      配置路径: ConnectionStrings:DefaultConnection
      环境变量: ConnectionStrings__DefaultConnection
      问题: 配置值包含占位符: #{DATABASE_CONNECTION_STRING}#
      示例: Server=localhost;Database=LYBTDB;...
      修复方法（Windows）:
      setx ConnectionStrings__DefaultConnection "<your-value>"
      修复方法（Linux）:
      export ConnectionStrings__DefaultConnection="<your-value>"

───────────────────────────────────────────────────────────
📖 详细配置指南: docs/deployment/production-setup.md
🔧 验证脚本: .\scripts\validate-production-config.ps1
```

---

## 快速开始

如果您希望快速测试 Production 配置（不推荐用于实际生产）：

```powershell
# 最小化配置（仅设置 Critical 项）
setx ConnectionStrings__DefaultConnection "Server=localhost;Database=LYBTDB;Integrated Security=True;TrustServerCertificate=True" /M
setx Lybt__Authentication__Jwt__SecretKey "ThisIsATestKeyWithAtLeast32Characters1234567890" /M

# 重启 PowerShell 会话后启动
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run --project src/Server/Services/LYBT.WebAPI
```

---

## 验证配置

### 方法 1：使用验证脚本

```powershell
.\scripts\validate-production-config.ps1 -Verbose
```

### 方法 2：手动检查

```powershell
# 查看已设置的环境变量
[Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Machine")
[Environment]::GetEnvironmentVariable("Lybt__Authentication__Jwt__SecretKey", "Machine")
# ... 检查其他配置项
```

### 方法 3：启动应用验证

应用启动时会自动验证配置，如果配置不正确会：

1. ❌ **启动失败**（退出码 1）
2. 📋 **显示详细错误消息**（包含缺失项和修复命令）
3. 📝 **记录到日志文件**

---

## 常见问题

### Q1: 设置环境变量后应用仍然报错

**原因**: 环境变量修改后，当前进程不会自动更新。

**解决方案**:
1. 关闭所有 PowerShell/CMD 窗口
2. 重新打开 PowerShell **以管理员身份**
3. 验证环境变量已生效：`$env:ConnectionStrings__DefaultConnection`
4. 启动应用

### Q2: 如何生成安全的 JWT 密钥

**Windows PowerShell**:
```powershell
$bytes = [byte[]]::new(32)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

**Linux**:
```bash
openssl rand -base64 32
```

### Q3: 数据库连接失败

**检查清单**:
- ✅ SQL Server 服务是否运行
- ✅ 连接字符串中的服务器地址/端口是否正确
- ✅ 数据库用户权限是否足够（需要读写权限）
- ✅ 防火墙是否允许连接
- ✅ `TrustServerCertificate=True` 是否已设置（如果使用自签名证书）

### Q4: 如何修改已设置的环境变量

```powershell
# Windows（管理员权限）
setx VARIABLE_NAME "new_value" /M

# Linux
sudo nano /etc/environment
# 或
sudo systemctl edit lybt-webapi
```

修改后需要重启应用程序。

### Q5: 可以在配置文件中直接设置密钥吗？

❌ **强烈不建议**！

- 配置文件可能被误提交到 Git
- 配置文件备份可能泄露敏感信息
- 环境变量更符合 12-Factor App 最佳实践
- 容器化部署时环境变量更易管理

---

## 安全建议

### 1. 密钥强度

- ✅ JWT 密钥至少 **32 字符**（256 位）
- ✅ 使用加密安全的随机数生成器
- ✅ 避免使用可预测的字符串

### 2. 数据库安全

- ✅ 使用**独立的应用数据库账户**（非 sa）
- ✅ 仅授予必要权限（读写表，禁止 DDL）
- ✅ 启用 SQL Server 加密连接
- ✅ 定期轮换数据库密码

### 3. 环境变量保护

- ✅ 使用**系统级**环境变量（`/M` 参数）
- ✅ 限制服务器访问权限
- ✅ 不要在日志中记录环境变量值
- ✅ 定期审计环境变量配置

### 4. 定期维护

- ✅ **每 90 天**轮换 JWT 密钥
- ✅ **首次登录后**强制修改默认密码
- ✅ **定期备份**环境变量配置（加密存储）
- ✅ **审计日志**检查异常访问

---

## 相关文档

- 📖 [环境变量配置参考](./environment-variables.md) - 所有配置项的详细说明
- 🔒 [安全检查清单](./security-checklist.md) - 部署前安全检查项
- 🔧 [问题排查指南](./troubleshooting.md) - 常见问题解决方案
- 🐳 [Docker 部署指南](./docker-deployment.md) - 容器化部署说明（Phase 3）

---

## 支持与反馈

如有问题或建议，请：

1. 查阅 [问题排查指南](./troubleshooting.md)
2. 在 GitHub 创建 Issue
3. 联系系统管理员

---

**📝 文档版本**: 1.0  
**🔄 最后更新**: 2025-09-30  
**✍️ 维护者**: LYBT Team