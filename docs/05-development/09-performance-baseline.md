# 性能基准测试报告

## 概述

本文档记录 v1.0 性能基准测试结果，对齐 `docs/02-requirements/17-nfr.md` 中的 NFR-PERF-001~004 指标。

**测试日期**: 2026-03-10
**测试环境**: 本地开发机 (Windows 10, SQL Server LocalDB)
**数据集**: 5000 患者 + 200 药材 + 25000 医案 (含 15000 处方, ~112500 处方药材项)
**测试框架**: xUnit + WebApplicationFactory + 真实 SQL Server (非 mock)
**统计方法**: 每项测试执行 20 次, 取 P95 (第 95 百分位)

---

## 测试结果

### API 响应时间 (NFR-PERF-001)

| 编号 | 操作类型 | NFR 目标 (P95) | 实测 P95 | 实测 Avg | 实测 Max | 状态 |
|------|----------|---------------|----------|----------|----------|------|
| NFR-PERF-001a | 简单查询 (GET /patients/{id}) | < 500ms | 4ms | 2ms | 25ms | PASS |
| NFR-PERF-001b | 列表查询 (GET /patients?keyword=) | < 1000ms | 28ms | 23ms | 44ms | PASS |
| NFR-PERF-001c | 聚合保存 (POST /medicalcases) | < 2000ms | 52ms | 51ms | 269ms | PASS |
| NFR-PERF-001d | 医案列表 (GET /medicalcases?page=) | < 1000ms | 31ms | 22ms | 256ms | PASS |

### 并发能力 (NFR-PERF-004)

| 指标 | NFR 目标 | 实测结果 | 状态 |
|------|----------|----------|------|
| 3 并发用户, 每用户 10 请求 | >= 95% 成功 | 30/30 (100%) | PASS |
| 并发响应 P95 | < 1000ms | 18ms | PASS |
| 并发响应 Avg | - | 7ms | - |

---

## 分析

### 远超预期的原因

实测 P95 远低于 NFR 阈值 (4ms vs 500ms, 28ms vs 1000ms)。主要原因:

1. **InProcess 测试**: WebApplicationFactory 在进程内运行, 无网络开销
2. **LocalDB**: 测试使用本地 SQL Server, I/O 延迟极低
3. **数据量符合预期**: 5000 患者 + 25000 医案在 SQL Server 的处理范围内

### 生产环境预期差异

| 因素 | 测试环境 | 生产环境 | 预期影响 |
|------|---------|---------|---------|
| 网络 | InProcess (0ms) | 局域网 (1-5ms) | +5ms |
| 数据库 | LocalDB (SSD) | SQL Server (网络) | +10-50ms |
| 序列化 | 包含 | 包含 | 0 |
| 认证 | JWT Bearer | JWT Bearer | 0 |
| OutputCache | 未命中 (每次 Respawn) | 可能命中 | -50%~0 |

**保守估计**: 生产环境 P95 约为实测值的 3-5 倍, 仍远低于 NFR 阈值。

---

## 数据播种统计

| 实体 | 数量 | 播种耗时 |
|------|------|---------|
| 药材 (Herb) | 200 | ~1s |
| 患者 (Patient) | 5000 | ~5s |
| 医案 (MedicalCase) | 25000 | ~40s |
| 诊断 (Consultation) | 25000 | 含在医案中 |
| 处方 (Prescription) | 15000 (60%) | 含在医案中 |
| 处方药材项 (PrescriptionItem) | ~112500 | 含在医案中 |
| **总计** | **~82700** | **~46s** |

---

## 测试清单

### CI 友好测试 (小数据量, 每次运行)

| 测试 | seed | 阈值 |
|------|------|------|
| Patients_PaginationQuery_ShouldRespondWithin500ms | 20 | 500ms |
| Patients_SearchByKeyword_ShouldRespondWithin800ms | 20 | 800ms |
| Patients_ConcurrentRequests_ShouldHandleLoad | 20 | 80% 成功 |
| MedicalCase_DetailWithRelations_ShouldRespondWithin1s | 1 | 1000ms |

### NFR 基准测试 (大数据量, [Trait("Category", "Performance")])

| 测试 | 数据集 | NFR 编号 |
|------|--------|---------|
| NFR_SimpleQuery_P95ShouldBeLessThan500ms | 标准 | NFR-PERF-001a |
| NFR_ListQuery_P95ShouldBeLessThan1s | 标准 | NFR-PERF-001b |
| NFR_AggregateSave_P95ShouldBeLessThan2s | 标准 | NFR-PERF-001c |
| NFR_MedicalCaseList_P95ShouldBeLessThan1s | 标准 | NFR-PERF-001b ext |
| NFR_ConcurrentLoad_ShouldHandleMultipleUsers | 标准 | NFR-PERF-004 |

### 运行命令

```bash
# CI 友好测试 (快速, ~20s)
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~PerformanceTests&Category!=Performance"

# NFR 基准测试 (完整, ~4min)
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~PerformanceTests&Category=Performance"

# 全部性能测试
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~PerformanceTests"

# 查看详细 P95 输出
dotnet test tests/LYBT.Tests.Server/ --filter "Category=Performance" --logger "console;verbosity=detailed"
```

---

## NFR 验收状态

| NFR 编号 | 描述 | 验收标准 | 状态 |
|----------|------|---------|------|
| NFR-PERF-001a | API 简单查询 | P95 < 500ms (标准数据量) | PASS |
| NFR-PERF-001b | API 列表查询 | P95 < 1s (标准数据量) | PASS |
| NFR-PERF-001c | API 聚合保存 | P95 < 2s (标准数据量) | PASS |
| NFR-PERF-001d | API 批量导入 | P95 < 5s | 未测 (需文件上传) |
| NFR-PERF-002a | Desktop 冷启动 | < 5s | UAT 手动验收 |
| NFR-PERF-002b | Desktop 页面切换 | < 1s | UAT 手动验收 |
| NFR-PERF-003 | 客户端内存 | < 200MB | UAT 手动验收 |
| NFR-PERF-004 | 并发 1-3 用户 | 95% 成功率 | PASS (100%) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-10 | v1.0 | 初始基准测试: 9 个测试 (4 CI + 5 NFR), 全部通过 |
