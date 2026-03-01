# LYBT.WebAPI.Tests

> WebAPI 宿主层单元测试，覆盖授权处理器、诊断控制器、数据库注册扩展及中间件异常处理管道。

## 项目定位

- **层级**: UnitTests / Server / WebAPI
- **被测模块**: LYBT.WebAPI
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Authorization/MedicalCaseAuthorizationHandlerTests.cs` | `MedicalCaseAuthorizationHandler` | ~10 |
| `Controllers/DiagnosticsControllerTests.cs` | `DiagnosticsController` | ~16 |
| `Extensions/DatabaseServiceCollectionExtensionsTests.cs` | `DatabaseServiceCollectionExtensions` | ~2 |
| `Middleware/BusinessExceptionHandlerTests.cs` | `BusinessExceptionHandler` | ~5 |
| `Middleware/CorrelationIdMiddlewareTests.cs` | `CorrelationIdMiddleware` | ~6 |
| `Middleware/SystemExceptionHandlerTests.cs` | `SystemExceptionHandler` | ~5 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/WebAPI/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.WebAPI
- LYBT.Infrastructure
- LYBT.Entities
- Microsoft.AspNetCore.TestHost

## 更新记录

- 2026-03-01: 创建 README
