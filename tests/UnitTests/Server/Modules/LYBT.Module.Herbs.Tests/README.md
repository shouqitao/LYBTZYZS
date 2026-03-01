# LYBT.Module.Herbs.Tests

> 中药材模块单元测试，覆盖药材仓储数据访问层与服务层的查询、增删改业务逻辑。

## 项目定位

- **层级**: UnitTests / Server / Modules
- **被测模块**: LYBT.Module.Herbs
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Repositories/HerbRepositoryTests.cs` | `HerbRepository` | ~22 |
| `Services/HerbServiceTests.cs` | `HerbService` | ~30 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Herbs.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.Herbs
- LYBT.Infrastructure
- LYBT.Entities
- Microsoft.EntityFrameworkCore.InMemory

## 更新记录

- 2026-03-01: 创建 README
