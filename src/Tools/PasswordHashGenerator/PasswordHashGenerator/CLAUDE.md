# PasswordHashGenerator 代码知识

使用 BCrypt 算法生成密码哈希值的命令行工具，依赖 LYBT.Shared.Utilities.Security.PasswordHelper。

## 代码文件结构

```
├── Program.cs        # 命令行参数解析 + 密码哈希生成/验证
```

### Program.cs
**Program** | 解析命令行参数，调用 PasswordHelper 生成 BCrypt 哈希，验证哈希正确性，输出 SQL 更新语句

| 方法 | 说明 |
|------|------|
| Main(args) | 入口方法，解析 --password / --role / --show-config / --show-help 参数，调用 GeneratePasswordHash |
| ShowHelp() | 显示命令行用法和示例 |
| GetDefaultAdminPassword() | 优先从 appsettings.json 读取 `Lybt:DefaultPasswords:SysAdminPassword`，回退到硬编码默认密码 |
| ShowConfiguration() | 调用 PasswordHelper.GetConfiguration() 显示 BCrypt 工作因子等配置 |
| GeneratePasswordHash(password, role) | 调用 PasswordHelper.HashPassword 生成哈希，PasswordHelper.VerifyPassword 验证，输出 SQL 语句和临时密码示例 |

## 命令行参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `--password <密码>` | 要哈希的密码 | 从配置或硬编码读取 |
| `--role <角色>` | UserRole 枚举值 | Doctor |
| `--show-config` | 显示 PasswordHelper 配置 | - |
| `--show-help` / `-h` | 显示帮助 | - |

## 外部依赖

| 依赖 | 用途 |
|------|------|
| LYBT.Shared.Utilities.Security.PasswordHelper | BCrypt 哈希生成/验证核心逻辑 |
| LYBT.Shared.Models.Enums.UserRole | 用户角色枚举 |
| Microsoft.Extensions.Configuration | 读取 appsettings.json 配置 |

## 死代码与废弃标记

(无)

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 硬编码回退密码 | GetDefaultAdminPassword 中的 fallbackPassword | 仅在 appsettings.json 缺失时使用，开发工具可接受 |
| 返回值表示成功/失败 | Main 返回 int (0=成功, 1=失败) | 可用于脚本化调用时检测执行状态 |
| 密码明文输出到控制台 | GeneratePasswordHash 输出原始密码和 SQL 语句 | 仅限开发环境使用，不应在生产环境运行 |
