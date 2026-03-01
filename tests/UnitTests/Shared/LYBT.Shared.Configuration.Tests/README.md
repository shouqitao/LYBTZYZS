# LYBT.Shared.Configuration.Tests

> LYBT.Shared.Configuration 配置扩展与选项验证的单元测试

## 项目定位

- **层级**: UnitTests / Shared
- **被测模块**: LYBT.Shared.Configuration
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| ServerConfigurationExtensionsTests.cs | ServerConfigurationExtensions | ~N |
| ConfigurationLoadingTests.cs | 配置加载逻辑 | ~N |
| ValidateOnStartTests.cs | 启动时配置验证 | ~N |
| ApiClientOptionsTests.cs | ApiClientOptions | ~N |
| JwtOptionsTests.cs | JwtOptions | ~N |
| JwtOptionsValidatorTests.cs | JwtOptionsValidator | ~N |

## 运行方式

```bash
dotnet test tests/UnitTests/Shared/LYBT.Shared.Configuration.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Shared.Configuration
- 目标框架: net8.0

## 更新记录

- 2026-03-01: 创建 README
