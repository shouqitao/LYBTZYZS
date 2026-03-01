# LYBT.Module.Formula.Tests

> 验方模块单元测试，覆盖验方的查询、创建、更新及业务规则校验逻辑。

## 项目定位

- **层级**: UnitTests / Server / Modules
- **被测模块**: LYBT.Module.Formula
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Services/FormulaServiceTests.cs` | `FormulaService` | ~28 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Formula.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.Formula
- LYBT.Infrastructure
- LYBT.Entities

## 更新记录

- 2026-03-01: 创建 README
