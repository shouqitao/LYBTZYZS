# 性能测试 (PerformanceTests)

> EF Core CRUD 操作性能基准 | 批量操作性能验证

## 项目列表

| 项目 | 测试内容 | 测试文件数 |
|------|----------|-----------|
| Server/LYBT.Server.PerformanceTests | Server 端模块 CRUD 和批量操作性能基准 | 3 |

## 测试场景

**ServerPerformanceTests** -- 使用 InMemory 数据库测试 Users/Patients/Herbs 的分页查询 (P95 < 500ms)、单条创建 (P95 < 300ms)、批量导入 1000 条 (< 10s)。

**BatchOperationPerformanceTests** -- 批量操作基准 (Epic #2016): AddRangeAsync 1000 条 (< 5s)、DeleteRangeAsync 实体/ID 集合 1000 条 (< 5s)、GetPagedAsync 10000 条数据分页查询 (< 1s)。

## 运行方式

性能测试是 BenchmarkDotNet 控制台应用，需在 Release 模式下运行:

```bash
dotnet run -c Release --project tests/PerformanceTests/Server/LYBT.Server.PerformanceTests/
```

## 依赖

- LYBT.Infrastructure, LYBT.Entities, LYBT.Module.Users/Patients/Herbs
- LYBT.Tests.Configuration (共享测试基础设施)
- BenchmarkDotNet (NuGet)

## 注意事项

- 必须使用 Release 模式运行，Debug 模式结果不可靠
- 性能阈值 (P95 < 500ms 等) 基于 InMemory 数据库，实际 SQL Server 性能会有差异

## 更新记录

- 2026-03-01: 创建 README 文档
