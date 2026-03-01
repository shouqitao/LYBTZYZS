# LYBT.Module.MedicalCase.Tests

> 医案模块单元测试，覆盖医案的命令操作、查询检索与状态流转三个服务层切面。

## 项目定位

- **层级**: UnitTests / Server / Modules
- **被测模块**: LYBT.Module.MedicalCase
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Services/MedicalCaseCommandServiceTests.cs` | `MedicalCaseCommandService` | ~10 |
| `Services/MedicalCaseQueryServiceTests.cs` | `MedicalCaseQueryService` | ~10 |
| `Services/MedicalCaseStateServiceTests.cs` | `MedicalCaseStateService` | ~13 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.MedicalCase
- LYBT.Infrastructure
- LYBT.Entities

## 更新记录

- 2026-03-01: 创建 README
