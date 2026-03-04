# Testing Trophy Redesign

## Goal
重新设计测试架构为 Testing Trophy 模式: 消灭 mock，以集成测试为核心，确保所有业务逻辑测试真实反映代码实现。

## Current Status: VERIFY complete, ready for git commit

## Documents
- Design: docs/plans/2026-03-03-testing-trophy-redesign-design.md
- Plan: docs/plans/2026-03-03-testing-trophy-redesign-plan.md

## Phases

### Phase 1: 基础设施搭建
Status: complete

- [x] 1.1: 创建 LYBT.Tests.Server 项目 (csproj + 目录结构 + sln)
- [x] 1.2: 实现 ITestDatabaseProvider + LocalSqlServerProvider
- [x] 1.3: 实现 ServerFixture (Respawn + 真实登录 + WAF)
- [x] 1.4: 实现 IntegrationTestBase (每测试 Respawn 重置)
- [x] 1.5: 烟雾测试 3/3 pass (真实登录验证)

### Phase 2: 服务端测试迁移
Status: complete
Plan: docs/plans/2026-03-03-phase2-server-test-migration-plan.md

- [x] 2.1: Auth (25 tests, 3 HIGH issues fixed)
- [x] 2.2: Users (32 tests, 2 spec issues fixed)
- [x] 2.3: Patients (24 tests)
- [x] 2.4: MedicalCases (50 tests, 3 files)
- [x] 2.5: Herbs (36 tests)
- [x] 2.6: Formulas + remaining (95 tests, 8 files)
- [x] 2.7: RateLimiting (1 test, separate fixture)
- [x] 2.8: Pure logic (915 tests, 45 files)
- [x] 2.9: IntegrationTestBase helpers (3 shared helpers, 12 files refactored)
- [x] 2.10: Full verification (1185 tests pass, 0 NSubstitute)

### Phase 3: Desktop 测试重构
Status: complete

- [x] 3.1: 创建 LYBT.Tests.Desktop 项目 + DesktopFixture (SQLite + 真实 Repository)
- [x] 3.2: 迁移纯逻辑 + DataSource 测试 (386 tests, 28 files)
- [x] 3.3: 迁移 ViewModel + E2E 测试 (329 tests, 31 files)
- [x] 3.4: 删除旧 Desktop 测试项目 (Desktop.Unit + Desktop.Integration)

### Phase 4: 清理 + 防护
Status: complete

- [x] 4.1: 删除旧测试项目 (5 -> 3: Unit, Server.Integration, TestConfiguration 已删除)
- [x] 4.2: 精简 TestConfiguration (已删除, 基础设施内嵌新项目)
- [x] 4.3: 新增 AntiMockRuleTests (Server 零 mock 架构防护)
- [x] 4.4: 更新 CLAUDE.md 和项目文档 (7 个文件更新)
- [x] 4.5: 全量验证 (2021 tests pass: Server 1185 + Desktop 760 + Architecture 76)

## Decisions

| Decision | Rationale |
|----------|-----------|
| Testing Trophy (方案 B) | 结构性消除 mock 偏差，而非战术修补 |
| 5 -> 3 项目合并 | Unit/Integration 界限在 Trophy 模式下模糊，减少维护 |
| 非 Docker (本地 SQL Server) | 环境无 Docker，预留 Testcontainers 接口 |
| Respawn 替代 EnsureDeleted | 每测试隔离 + 50x 性能提升 |
| 真实登录替代硬编码 JWT | 消除认证路径偏差 |
| Mock 白名单 (仅 WPF 边界接口) | WPF Runtime 无法避免，但严格限制范围 |

## Errors Encountered
(none)
