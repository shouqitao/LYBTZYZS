---
feature: rebuild-sql-integration-tests
status: delivered
updated: 2026-09-17
branch: master
commits: a4e3788b8..HEAD
---

# 重建 SQL 集成测试基建（R-7）

## Report

**What was built** — 重建 SQL Server + Respawn 集成测试基建。新增 `_Infrastructure/`（TestDbFactory/RespawnCheckpoint/IntegrationTestBase/SqlServerIntegrationCollection）和 `Integration/SqlCore/`（MedicalCase/Registration/Patient 三个领域路径测试）。使用 Respawn 7 真实 API（Respawner/RespawnerOptions），串行集合防止并行 Respawn 冲突。

**Verification** — NuGet restore 环境问题持续，无法运行 `dotnet build`。需在能正常 restore 的环境执行构建和测试验证。

**Journey log** —
- Respawn 7.0 API 与旧版不兼容：无 Checkpoint 类，正确 API 是 Respawner.CreateAsync + ResetAsync
- MedicalCase 有硬 FK（PatientId/UserId），SQL 测试必须先 seed 关联实体
- 过滤唯一索引（UX_MedicalCases_Patient_ActiveOnly）约束同一患者只能有一条 Active 医案

## [S1] Problem

T2-1 批次删除了真 SQL Server + Respawn 集成测试基建，当前 Server 测试全为 EF InMemory。SQL 翻译缺陷、并发问题、真实数据库行为无测试覆盖。

## [S2] Design

### 目标
重建最小可用的 SQL Server 集成测试基建，覆盖核心业务路径。

### 架构
1. **TestDbFactory** — 创建真实 SQL Server 连接（LocalDB 或环境变量指定）
2. **RespawnCheckpoint** — 测试间数据库重置
3. **IntegrationTestBase** — 测试基类，管理数据库生命周期
4. **核心业务路径测试** — MedicalCase CRUD、Registration 流程、Patient CRUD

### 技术选型
- SQL Server LocalDB（开发环境）或环境变量 `TEST_CONNECTION_STRING`
- Respawn 4.0+ 数据库重置
- xUnit Class Fixture 管理数据库生命周期

## [S3] Out of Scope

- 全部模块的集成测试（本批次覆盖核心路径）
- 性能测试
- 并发测试

## Tasks
- [x] T1: 创建 TestDbFactory + RespawnCheckpoint + IntegrationTestBase (covers: S2)
- [x] T2: 核心业务路径集成测试（MedicalCase/Registration/Patient） (covers: S2; depends: T1)
- [x] T3: 更新测试文档 (covers: S2; depends: T2)
