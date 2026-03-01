# LYBT.Infrastructure.Tests

> 服务端基础设施层单元测试，覆盖通用服务基类、仓储基类、JSON序列化与跨模块查询服务。

## 项目定位

- **层级**: UnitTests / Server / Core
- **被测模块**: LYBT.Infrastructure
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `BaseServiceTests.cs` | `BaseService` | ~12 |
| `Repositories/BaseRepositoryTests.cs` | `BaseRepository` | ~65 |
| `Serialization/SensitiveDataJsonConverterTests.cs` | `SensitiveDataJsonConverter` | ~4 |
| `Services/CrossModuleQueryServiceTests.cs` | `CrossModuleQueryService` | ~13 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Core/LYBT.Infrastructure.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Infrastructure
- LYBT.Entities
- LYBT.Shared.Models
- Microsoft.EntityFrameworkCore.InMemory

## 更新记录

- 2026-03-01: 创建 README
