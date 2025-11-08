# 密码重置工具

**Issue #1908**: 增强密码重置工具，支持sysadmin和普通用户密码重置

---

## 📋 功能概述

本工具用于重置LYBTZYZS系统中的用户密码，支持：

1. **SysAdmin账户** - 管理员账户（存储在AdminSecrets表）
2. **普通用户账户** - 医生、护士等用户（存储在Users表）

### 密码哈希算法

使用 **BCrypt** 算法（与AuthService一致）：
- **Workfactor**: 11
- **算法**: BCrypt.Net-Next
- **兼容性**: 与系统认证服务完全兼容

---

## 🚀 使用方法

### 方法1: 交互式模式（推荐）

适合不熟悉命令行的用户，工具会逐步提示输入：

```bash
cd D:\source\repos\LYBTZYZS
dotnet run --project scripts/ResetPassword/ResetPassword.csproj
```

**交互流程**:
```
===== LYBTZYZS 密码重置工具 =====
Issue #1908: 支持sysadmin和普通用户密码重置

请选择账户类型:
  1. SysAdmin (管理员账户)
  2. User (普通用户)
请输入选项 (1/2): 1

请输入新密码: [输入新密码]
请再次输入新密码: [确认密码]

操作配置:
  账户类型: SysAdmin (管理员)
  用户名: sysadmin
  新密码: ******

确认执行密码重置? (y/n): y

✓ 密码哈希已生成 (BCrypt workfactor=11)
✓ 数据库连接成功
找到SysAdmin账户:
  ID: 00000000-0000-0000-0000-000000000001
  旧哈希: $2a$11$va/3K149qeu9...

✓ SysAdmin密码已更新

✓ 密码重置成功!

登录信息:
  用户名: sysadmin
  新密码: [你输入的密码]

===== 完成 =====
```

---

### 方法2: 命令行模式（快速）

适合熟悉命令行的用户，直接指定参数：

#### 重置SysAdmin密码

```bash
cd D:\source\repos\LYBTZYZS
dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t sysadmin -p "NewSecurePass123!"
```

#### 重置普通用户密码

```bash
cd D:\source\repos\LYBTZYZS
dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t user -u doctor1 -p "NewPass123!"
```

#### 指定数据库连接字符串

```bash
dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- \
  -t sysadmin \
  -p "NewSecurePass123!" \
  -c "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true"
```

---

## 📝 命令行参数

| 参数 | 简写 | 说明 | 必填 | 示例 |
|-----|------|------|------|------|
| `--type` | `-t` | 账户类型: `sysadmin` 或 `user` | 否* | `-t sysadmin` |
| `--username` | `-u` | 用户名（仅普通用户需要） | 条件* | `-u doctor1` |
| `--password` | `-p` | 新密码 | 否* | `-p "Pass123!"` |
| `--connection` | `-c` | 数据库连接字符串 | 否 | `-c "Server=...;Database=..."` |
| `--help` | `-h` | 显示帮助信息 | 否 | `-h` |

**说明**:
- `*` 如果不提供参数，工具会进入交互式模式
- `username` 仅在 `type=user` 时需要
- 默认连接字符串: `Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true`

---

## 💡 使用场景

### 场景1: 忘记SysAdmin密码

**问题**: 忘记管理员密码，无法登录系统

**解决方案**:
```bash
# 进入项目目录
cd D:\source\repos\LYBTZYZS

# 运行工具（交互式）
dotnet run --project scripts/ResetPassword/ResetPassword.csproj

# 选择: 1. SysAdmin
# 输入新密码
# 确认操作

# 使用新密码登录系统
```

---

### 场景2: 批量重置用户密码

**问题**: 需要重置多个用户的密码到统一临时密码

**解决方案**:
```bash
# 重置doctor1
dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t user -u doctor1 -p "TempPass2025!"

# 重置doctor2
dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t user -u doctor2 -p "TempPass2025!"

# 重置nurse1
dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t user -u nurse1 -p "TempPass2025!"
```

---

### 场景3: 安全加固 - 强制密码更新

**问题**: 系统安全升级，需要将所有用户密码更新为符合新安全策略的密码

**解决方案**:
```bash
# 批量脚本（PowerShell示例）
$users = @("doctor1", "doctor2", "nurse1")
foreach ($user in $users) {
    $password = "NewSecure$(Get-Random -Min 1000 -Max 9999)!"
    dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t user -u $user -p $password
    Write-Host "用户 $user 密码已重置为: $password"
}
```

---

## 🔐 密码安全建议

### 推荐密码格式

**SysAdmin密码**（管理员账户）:
- 长度: ≥20个字符
- 复杂度: 大写+小写+数字+特殊字符
- 示例: `LybtAdmin2025@SecurePass!`

**普通用户密码**（首次登录临时密码）:
- 长度: ≥12个字符
- 复杂度: 大写+小写+数字+特殊字符
- 示例: `Pass123!` (首次登录后强制修改)

### 密码策略

系统密码验证规则（参考代码实现）:
- 最小长度: 6个字符
- 建议长度: 12-20个字符
- 必须包含: 字母和数字
- 推荐包含: 特殊字符

---

## 🛠️ BCrypt哈希生成示例

### 明文密码 → BCrypt哈希

**明文**: `LybtAdmin2025@SecurePass!`
**BCrypt哈希**: `$2a$11$afPwqPi6lpQr22fqoaRol.u9ktXMg.nVftjMBfGvpot.gs2NAlaT2`

**重要**:
- BCrypt每次生成的哈希值不同（盐是随机生成的）
- 但所有哈希值都可以验证同一个明文密码
- 示例哈希仅供参考，实际运行时会生成不同的哈希

### 验证哈希

使用 `scripts/BcryptGenerator` 工具验证：

```bash
cd D:\source\repos\LYBTZYZS
dotnet run --project scripts/BcryptGenerator/BcryptGenerator.csproj
```

输出:
```
=== BCrypt密码哈希生成器 ===

SysAdmin账号:
  用户名: sysadmin
  密码: LybtAdmin2025@SecurePass!
  BCrypt哈希: $2a$11$afPwqPi6lpQr22fqoaRol.u9ktXMg.nVftjMBfGvpot.gs2NAlaT2
  验证结果: ✓ 成功
```

---

## 📊 数据库表结构

### AdminSecrets表（SysAdmin）

```sql
CREATE TABLE AdminSecrets (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PasswordHash NVARCHAR(500) NOT NULL
)

-- SysAdmin固定ID
-- '00000000-0000-0000-0000-000000000001'
```

### Users表（普通用户）

```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    Email NVARCHAR(100),
    RealName NVARCHAR(50),
    Status INT,
    IsDeleted BIT
)
```

---

## ⚠️ 注意事项

### 安全警告

1. **生产环境谨慎操作**
   - 密码重置会立即生效
   - 用户需要使用新密码登录
   - 建议在维护窗口执行

2. **审计日志**
   - 当前版本不自动记录审计日志
   - 建议手动记录操作（谁、何时、重置了哪个账户）

3. **数据库备份**
   - 执行前建议备份数据库
   - 避免误操作导致无法恢复

### 使用限制

1. **仅本地数据库**
   - 默认连接字符串为本地数据库
   - 远程数据库需指定 `--connection` 参数

2. **需要数据库权限**
   - 工具需要对AdminSecrets和Users表的UPDATE权限
   - 确保数据库用户有足够权限

3. **不支持批量操作**
   - 当前版本每次只能重置一个账户
   - 批量操作需编写脚本循环调用

---

## 🧪 测试验证

### 验证步骤

1. **重置密码**
   ```bash
   dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t sysadmin -p "TestPass123!"
   ```

2. **启动应用**
   - 启动LYBT.Desktop应用
   - 启动LYBT.WebAPI服务

3. **登录测试**
   - 使用新密码登录
   - 确认认证成功

4. **恢复原密码**（可选）
   ```bash
   dotnet run --project scripts/ResetPassword/ResetPassword.csproj -- -t sysadmin -p "LybtAdmin2025@SecurePass!"
   ```

---

## 🔧 故障排除

### 问题1: 数据库连接失败

**错误信息**:
```
✗ 错误: A network-related or instance-specific error occurred...
```

**解决方案**:
1. 检查SQL Server服务是否启动
2. 检查数据库名称是否正确（LYBTDB）
3. 检查Windows身份验证是否可用
4. 尝试指定完整连接字符串

### 问题2: 未找到用户

**错误信息**:
```
✗ 错误: 未找到用户: doctor1
```

**解决方案**:
1. 检查用户名拼写是否正确
2. 确认用户是否已删除（IsDeleted=1）
3. 查询数据库确认用户存在:
   ```sql
   SELECT UserName, IsDeleted FROM Users WHERE UserName = 'doctor1'
   ```

### 问题3: BCrypt哈希验证失败

**错误信息**:
```
✓ 密码重置成功!
[但登录时提示密码错误]
```

**解决方案**:
1. 检查Workfactor是否为11（与AuthService一致）
2. 重新生成哈希并更新数据库
3. 查看AuthService日志确认验证逻辑

---

## 📚 相关资源

### 代码文件

- **工具实现**: `scripts/ResetPassword/Program.cs`
- **BCrypt生成器**: `scripts/BcryptGenerator/Program.cs`
- **认证服务**: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- **用户服务**: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

### 数据库配置

- **AdminSecrets配置**: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/AdminSecretConfiguration.cs`
- **Users配置**: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/UserConfiguration.cs`

### GitHub Issues

- **Issue #1908**: 增强密码重置工具 - 支持sysadmin账户
- **Epic #1886**: 密码修改功能完善
- **Issue #1907**: Token改为内存存储

---

## 🤝 贡献

如有问题或改进建议，请提交GitHub Issue:
- **仓库**: https://github.com/shouqitao/LYBTZYZS
- **Issue模板**: 选择 `Enhancement` 或 `Bug Report`

---

**最后更新**: 2025-11-08
**版本**: 1.0.0
**维护者**: Claude Code
