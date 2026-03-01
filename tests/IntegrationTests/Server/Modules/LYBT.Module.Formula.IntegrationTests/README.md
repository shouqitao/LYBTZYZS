# LYBT.Module.Formula.IntegrationTests

> 验方模块服务层集成测试，使用 SQLite InMemory 数据库验证 FormulaService 业务逻辑

## 项目定位

- **层级**: IntegrationTests
- **被测模块**: LYBT.Module.Formula
- **状态**: Active

## 测试文件

| 文件 | 被测类/端点 |
|------|------------|
| FormulaServiceIntegrationTests | FormulaService 业务逻辑全路径 |

## 运行方式

```bash
dotnet test tests/IntegrationTests/Server/Modules/LYBT.Module.Formula.IntegrationTests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.Formula, LYBT.Infrastructure, LYBT.Entities
- Microsoft.EntityFrameworkCore.Sqlite (InMemory 模式)
- 目标框架: net8.0

## 更新记录

- 2026-03-01: 创建 README
