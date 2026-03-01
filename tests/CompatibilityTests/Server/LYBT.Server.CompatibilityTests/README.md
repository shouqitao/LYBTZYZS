# LYBT.Server.CompatibilityTests

> API 兼容性测试，验证 API 端点向后兼容

## 项目定位

- **层级**: CompatibilityTests
- **被测模块**: LYBT.WebAPI, LYBT.Infrastructure
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| ApiCompatibilityTests.cs | API 端点兼容性 | ~N |

## 运行方式

```bash
dotnet test tests/CompatibilityTests/Server/LYBT.Server.CompatibilityTests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.WebAPI
- LYBT.Infrastructure
- 目标框架: net8.0

## 更新记录

- 2026-03-01: 创建 README
