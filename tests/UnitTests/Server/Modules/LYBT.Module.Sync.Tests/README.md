# LYBT.Module.Sync.Tests

> 同步模块单元测试，覆盖数据校验和计算（ChecksumHelper）及双模式数据同步服务（SyncService）。

## 项目定位

- **层级**: UnitTests / Server / Modules
- **被测模块**: LYBT.Module.Sync
- **状态**: Active

## 测试文件

| 文件 | 被测类 | 测试数 |
|------|--------|--------|
| `Services/ChecksumHelperTests.cs` | `ChecksumHelper` | ~35 |
| `Services/SyncServiceTests.cs` | `SyncService` | ~28 |

## 运行方式

```bash
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Sync.Tests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Module.Sync
- LYBT.Infrastructure
- LYBT.Entities

## 更新记录

- 2026-03-01: 创建 README
