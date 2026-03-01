# LYBT.Module.Users.Tests

> 用户模块单元测试，覆盖用户账户管理、密码策略及权限分配等服务层业务逻辑。

## 项目定位

- **层级**: UnitTests / Server / Modules
- **被测模块**: LYBT.Module.Users
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Services/UserServiceTests.cs` | `UserService` | ~34 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Users.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.Users
- LYBT.Infrastructure
- LYBT.Entities

## 更新记录

- 2026-03-01: 创建 README
