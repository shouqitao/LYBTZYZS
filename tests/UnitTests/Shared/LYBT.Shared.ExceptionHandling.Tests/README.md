# LYBT.Shared.ExceptionHandling.Tests

> LYBT.Shared.ExceptionHandling 异常类型与错误码体系的单元测试

## 项目定位

- **层级**: UnitTests / Shared
- **被测模块**: LYBT.Shared.ExceptionHandling
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| ErrorCodeTests.cs | ErrorCode | ~N |
| AppExceptionTests.cs | AppException | ~N |
| BusinessExceptionTests.cs | BusinessException | ~N |
| ProblemDetailsFactoryTests.cs | ProblemDetailsFactory | ~N |

## 运行方式

```bash
dotnet test tests/UnitTests/Shared/LYBT.Shared.ExceptionHandling.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Shared.ExceptionHandling
- LYBT.Shared.Primitives
- 目标框架: net8.0

## 更新记录

- 2026-03-01: 创建 README
