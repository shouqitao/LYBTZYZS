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
