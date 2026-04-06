# PasswordHashGenerator

> BCrypt 密码哈希生成与验证工具，用于生成可直接写入数据库的密码哈希值

## 用途

基于项目统一的 `PasswordHelper` 工具类，生成 BCrypt 格式的密码哈希值。主要功能:

- 对指定密码生成 BCrypt 哈希，输出可用于 SQL UPDATE 的语句
- 自动验证生成的哈希值正确性
- 支持从 appsettings.json 读取默认管理员密码
- 支持查看当前 BCrypt 配置 (工作因子、重新哈希策略等)
- 生成临时密码示例

## 使用方式

```
dotnet run --project src/Tools/PasswordHashGenerator/PasswordHashGenerator/
```

命令行参数:

| 参数 | 说明 |
|------|------|
| --password <密码> | 要哈希的密码 (不提供则使用默认管理员密码) |
| --role <角色> | 用户角色，默认 Doctor，可选 Admin |
| --show-config | 显示当前 PasswordHelper 配置信息 |
| --show-help, -h | 显示帮助信息 |

## 依赖

- LYBT.Shared.Utilities (PasswordHelper, BCrypt 封装)
- LYBT.Shared.Models (UserRole 枚举)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 按 README 规范创建文档 |

## 开发笔记

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
