# 测试驱动开发 - 发现记录

> **创建日期**: 2026-03-11
> **最后更新**: 2026-03-11

---

## 需求理解

**核心目标**:
1. 代码实现与设计文档没有误差
2. 功能完全按照设计完成
3. 高效完成开发任务

**当前项目状态**:
- Sprint 6 已完成 (双模式/D2 诊所设置/D3 草稿水印/C2 照片加密)
- 正在进行：权限矩阵缺陷修复 (11 个问题，D-4~G-12)
- 测试架构：3 项目，Testing Trophy (~2021 tests)
  - Server: 1185 tests (真实 SQL Server + Respawn，零 mock)
  - Desktop: 760 tests (SQLite InMemory + 真实 Repository)
  - Architecture: 78 tests (架构防护 + AntiMockRules)

---

## 调研结果

### 现有 Journey Tests 文件清单

| 文件 | Collection | 状态 |
|------|-----------|------|
| AuthJourneyTests.cs | Auth | 需补全负面场景 |
| BootstrapJourneyTests.cs | User | 已完成 |
| AdminSetupJourneyTests.cs | User | 已完成 |
| FirstVisitJourneyTests.cs | Clinical | 已完成 |
| ReturnVisitJourneyTests.cs | Clinical | 已完成 |
| MedicalCaseEditJourneyTests.cs | Clinical | 需补全边界条件 |
| PatientManagementJourneyTests.cs | Clinical | 已完成 |
| HerbFormulaManagementJourneyTests.cs | HerbFormula | 已完成 |
| DoctorClinicalJourneyTests.cs | Clinical | 待删除 (冗余) |
| CrossNarrativeValidationTests.cs | Clinical | 已完成 |
| BatchOperationsJourneyTests.cs | HerbFormula | 已完成 |

**缺失文件**:
- ReceptionistJourneyTests.cs (D-4 验证)
- RegistrationJourneyTests.cs (G-9 验证)
- DoctorDisableJourneyTests.cs (G-11 验证)

### 测试基础设施

**基类**: `JourneyTestBase<TFixture>`
- 提供 helper 方法：`LoginAsAdminAsync()`, `LoginAsDoctorAsync()`, `PostAsync<T>`, `PutAsync<T>`, `GetAsync<T>`, `ReadErrorAsync()`
- 单测试方法包含完整用户旅程

**Fixtures**:
- `ServerFixture` - 基础测试服务器
- `AuthFixture`, `UserFixture`, `ClinicalFixture`, `HerbFormulaFixture`, `SyncFixture`, `InfraFixture` - 域名分类

**Collections** (并行执行):
- Auth, User, Clinical, HerbFormula, Sync, Infra (6 Collection 并行)

---

## 技术决策

| 决策 | 说明 | 来源 |
|------|------|------|
| TDD 原则 | RED→GREEN→REFACTOR | 技能要求 |
| Journey Test 优先 | Layer A 优先验证端到端流程 | 测试策略 |
| 删除冗余测试 | DoctorClinicalJourneyTests 与 FirstVisit 重叠 | 效率优化 |
| Layer B 暂缓 | Features/ 下 111 测试移到 _Deferred/ | 优先级调整 |
| 测试命名规范 | `US_AUTH_001_Login_WithValidCredentials_ShouldReturnToken` | PRD 对齐 |

---

## 风险与缓解

| 风险 | 缓解措施 |
|------|---------|
| 测试执行时间长 | 6 Collection 并行，目标<5 分钟 |
| 测试数据库污染 | Respawn 清理 + 独立 Database 每 Collection |
| Mock 过度使用 | AntiMockRuleTests 架构测试强制 Server 零 mock |
| PRD 变更不同步 | 测试命名包含 US 编号 |

---

## 待确认事项

1. 优先级：Phase 1 (权限矩阵缺陷) vs Phase 2 (Journey Test 重构)
2. LoginAsReceptionistAsync 方法是否存在 (需检查 ServerFixture)
3. 04:00 边界条件的 IsLocked 逻辑是否已实现
