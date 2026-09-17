---
feature: rebuild-sql-integration-tests
status: in-progress
updated: 2026-09-17
branch: master
commits: a4e3788b8..HEAD
---

# 重建 SQL 集成测试基建（R-7）

## Report

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
- [ ] T1: 创建 TestDbFactory + RespawnCheckpoint + IntegrationTestBase (covers: S2)
- [ ] T2: 核心业务路径集成测试（MedicalCase/Registration/Patient） (covers: S2; depends: T1)
- [ ] T3: 更新测试文档 (covers: S2; depends: T2)
