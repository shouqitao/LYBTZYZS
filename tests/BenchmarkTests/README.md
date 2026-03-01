# 基准测试 (BenchmarkTests)

> BenchmarkDotNet 驱动的性能基准测试 | 量化对比不同实现方案

## 项目列表

| 项目 | 测试内容 | 测试文件数 |
|------|----------|-----------|
| LYBT.QueryLayer.Benchmarks | 查询层性能基准 (Repository 缓存对比、批量操作 N+1 vs Batch 对比) | 3 |

## 测试场景

**BatchOperationsBenchmark (活跃)** -- SQLite 内存数据库，对比 N+1 循环模式和 EF Core ExecuteUpdate 批量模式在不同 BatchSize (5/10/20/50) 下的删除和状态更新性能。

**ReadRepositoryBenchmark (暂停)** -- Repository 缓存层对比测试，因基类重构处于 `#if false` 禁用状态。

## 运行方式

基准测试是控制台应用 (OutputType=Exe)，需在 Release 模式下运行:

```bash
dotnet run -c Release --project tests/BenchmarkTests/LYBT.QueryLayer.Benchmarks/
```

## 依赖

- LYBT.Entities, LYBT.Infrastructure, LYBT.Module.Users, LYBT.Shared.Models
- BenchmarkDotNet (NuGet)

## 注意事项

- 必须使用 Release 模式运行，Debug 模式结果不可靠
- ReadRepositoryBenchmark 因基类重构处于 `#if false` 禁用状态，需配合基类更新后启用

## 更新记录

- 2026-03-01: 创建 README 文档
