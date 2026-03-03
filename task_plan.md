# Architecture & Test Deep Audit Plan

## Goal
诊断"测试全绿但运行时失败"的根因，重建测试置信度，合并双测试层级，同时完成架构改进。

## Current Status: ALL PHASES COMPLETE (审计全部完成)

---

## Audit Scores (Updated)

| Dimension | Score | Key Findings |
|-----------|-------|-------------|
| Architecture Compliance | 4.8/5 | 零循环依赖，严格分层 |
| Dead Code Hygiene | 4.5/5 | 近乎零死代码 |
| Code Quality (SOLID) | 8/10 | OCP/DIP 优秀 |
| **Test Confidence** | **9/10** | **Phase 1+2 完成: 2370 tests, 25->5 项目, 所有迁移测试全绿** |
| Test Structure | 9/10 | 5 个测试项目 (Unit/Desktop.Unit/Server.Integration/Desktop.Integration/Architecture) |
| **Overall** | **9/10** | **架构优秀，测试体系完成合并重建** |

---

## Phases

### Phase 0: 运行时登录修复 (URGENT)
Status: complete

- [x] Task 0.1: 验证启动路径
- [x] Task 0.2: 确认密码一致性
- [x] Task 0.3: 同步测试种子

### Phase 1: 测试置信度重建 (P0 - 核心)
Status: complete

- [x] Task 1.1: 重写 LoginViewModelTests (27 个真实测试)
- [x] Task 1.2: DatabaseInitializationService 单元测试 (8 个)
- [x] Task 1.3: 用户不存在场景 (已存在)
- [x] Task 1.4: 用户被禁用场景 (Login_DisabledUser_Returns403)
- [x] Task 1.5: 密码 Hash 为空场景 (Login_UserWithEmptyPasswordHash_Returns401)
- [x] Task 1.6: Desktop 集成测试评估 -- Mock IAuthApi 是正确架构决策

### Phase 2: 双测试层级合并 (P1)
Status: complete

**目标**: 25 -> 10 个测试项目, 两边取长补短
**设计文档**: `docs/plans/2026-03-02-test-merge-design.md`

#### Phase 2a: Server 集成测试统一 (高风险)

- [x] Task 2.1: 增强 WebApiFixture (COMPLETE)
  - 添加 `CreateJsonContent<T>()` / `ParseResponseContent<T>()` 辅助方法
  - 文件: `tests/LYBT.Tests.Server.Integration/Fixtures/WebApiFixture.cs`

- [x] Task 2.2: 迁移 WebAPI.IntegrationTests (COMPLETE - 258/258 全绿)
  - 12 个无重叠文件: 直接迁移 + URL/格式修复
  - 7 个重叠文件: 逐个比对去重, 69 个 B 独有测试合并
  - 22 个失败测试: 状态码修正/权限客户端/数据隔离/业务语义修复

- [x] Task 2.6: 合并小型专项测试 (COMPLETE - 266/266 全绿)
  - CompatibilityTests: 8 -> 2 独有测试迁移 (ApiResponseContractTests)
  - Formula.IntegrationTests: 5 -> 3 独有测试迁移 (FormulaServiceIntegrationTests)
  - 修复: EF Core 8 OPENJSON 兼容性 (内存返回 vs DB 回查)
  - 文件: Compatibility/ApiResponseContractTests.cs, Formulas/FormulaServiceIntegrationTests.cs

#### Phase 2b: Server 单元测试统一 (中风险)
Status: complete

- [x] Task 2.3+2.4: Server 单元测试统一 (COMPLETE - 1302/1302 全绿)
  - 49 文件迁移到 Tests.Unit/ (上一会话完成)
  - 69 个过期测试修复 (接口演进但测试未同步): Validators 16 + Auth 7 + Formula 11 + Herbs 12 + Users 13 + MedicalCase 6 + Patients 4
  - 1 个 flaky 测试修复 (JwtServiceTests.ValidateToken_WithTamperedToken)
  - 最终: 1302 passed, 0 failed (原 592 + 迁移 710)

#### Phase 2c: Desktop 集成测试统一 (中风险)
Status: complete

- [x] Task 2.5: 合并 LYBT.Desktop.IntegrationTests (COMPLETE - 95/95 全绿)
  - 84 tests (B) → 合并到 24 tests (A)，去重 13，净增 71
  - 16 文件迁移: Foundation(3) + LocalMode(3) + EndToEnd(10)
  - 5 个缺失 DI 注册修复 (IPatientStatusHandler 等)
  - 最终: 95 passed, 0 failed (原 24 + 净增 71)

#### Phase 2d: 清理收尾 (低风险)
Status: complete

- [x] Task 2.7: 更新 LYBT.All.sln 移除 19 个废弃项目 + 删除 5 个目录 (COMPLETE)
  - `dotnet sln remove` 移除 19 个 Structure B 项目 (13 UnitTests + 3 IntegrationTests + 1 Compatibility + 1 Performance + 1 Benchmark)
  - 删除 5 个目录: IntegrationTests/(44 .cs), UnitTests/(121 .cs), CompatibilityTests/(4 .cs), PerformanceTests/(9 .cs), BenchmarkTests/(6 .cs)
  - 保留解决方案文件夹 IntegrationTests + UnitTests (包含活跃项目)
- [x] Task 2.8: 清理空架构测试方法 (NO-OP - 无空方法或占位符)

#### 执行依赖

```
2.1 -> 2.2 -> 2.6
              |
      2.3 + 2.4 (可并行)
              |
         2.5 (独立)
              |
      2.7 -> 2.8
```

### Phase 3: 架构快速修复 (P0)
Status: complete

- [x] Task 3.1: CardReader + LocalData 已在 SLN
- [x] Task 3.2: Authorization Handler XML 注释清理
- [x] Task 3.3: PrescriptionPrintService 裸 catch 修复

### Phase 4: DRY 改进 (P1)
Status: complete

- [x] Task 4.1: 提取魔法常量到集中常量类 (RoleConstants + PolicyConstants + HttpHeaderConstants) -- COMPLETE
  - 3 新建常量类 + 12 文件替换引用
- [x] Task 4.2: 创建 Guard 工具类 -- SKIPPED (YAGNI: 70+ null 检查模式各异，统一反增复杂度)

### Phase 5: API 一致性 (P2)
Status: complete

- [x] Task 5.1: MedicalCaseWorkspaceViewModel Handler 提取 -- SKIPPED (前期已尝试提取 NavigationHandler 并回退)
- [x] Task 5.2: HTTP 状态码统一 -- COMPLETE (2 处 UnprocessableEntity -> BusinessFail)
- [x] Task 5.3: 日志级别标准化 -- COMPLETE (7 文件 15+ 处修复)

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| 测试修复优先于架构重构 | 测试不可信导致任何重构都无法验证正确性 |
| 25 -> 10 项目，两边取长补短 | 实际重复仅约 20 个测试，大多是互补关系 |
| WebApiFixture 作为统一基础设施 | Drop+Migrate 更稳健，多角色客户端更灵活，LYBT_Test 隔离更安全 |
| 统一种子用户逻辑 | 测试种子必须复用生产路径 |
| 去重保留断言更丰富版本 | 两边取长补短原则 |

---

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| UseSqlite + MigrateAsync 冲突 | 1 | 改用 UseInMemoryDatabase |
| PasswordHash NOT NULL 约束 | 1 | 改为空字符串测试 |

## Corrections from BRAINSTORM

| 原诊断 | 实际情况 |
|--------|---------|
| "8 处裸 catch" | 仅 1 处裸 catch |
| "CardReader/LocalData 未加入 SLN" | 已在 SLN |
| "4 个未使用 Authorization Handler" | Handler 已删除，仅注释残留 |
| Task 1.3 "缺少用户不存在场景" | 已存在 |
