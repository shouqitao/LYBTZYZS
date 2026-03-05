# 测试体系重构

## Goal
化繁为简，确保核心流程端到端跑通。删除假绿测试和即将废弃的本地模式测试。

## Design Document
docs/plans/2026-03-05-desktop-test-simplification-design.md

## Implementation Plan
docs/plans/2026-03-05-test-restructuring-plan.md

## Decisions
| Decision | Rationale |
|----------|-----------|
| 取消 Phase 0 (本地 DB 迁移) | 等 SYNC-D02 (Sprint 4) 统一处理，避免重复劳动 |
| 删除全部本地模式测试 (~131) | DataSource 层 + DesktopFixture 在 SYNC-D02 后整体废弃 |
| 删除全部 mock-heavy 测试 (~109) | 验证 Received() 不能捕获真实 bug |
| 新建 LYBT.Tests.Integration | 职责分离: Integration 测远程联通, Desktop 测纯逻辑 |
| 远程模式与本地模式解耦 | Desktop+WebAPI 联通测试不受本地 DB 选择影响 |
| Mock 白名单制 | 仅 mock 硬件/Shell/HTTP，其余真实实现 |

## Phases

### Phase 1: WebAPI 正确性
Status: complete
- [x] Server 1017 tests 全绿

### Phase 2: Desktop + API 联通 (新建 LYBT.Tests.Integration)
Status: complete
- [x] Task 1: 创建项目脚手架 (.csproj + GlobalUsings + sln)
- [x] Task 2: IntegrationFixture (WebApplicationFactory + Refit + Respawn)
- [x] Task 3: AuthFlowTests (6 tests)
- [x] Task 4: PatientFlowTests (5 tests)
- [x] Task 5: HerbFlowTests (4 tests)
- [x] Task 6: FormulaFlowTests (4 tests)
- [x] Task 7: MedicalCaseFlowTests (8 tests)

### Phase 3: Desktop 精简
Status: complete
- [x] Task 8: 删除全部本地模式测试 (20 files, ~131 tests)
- [x] Task 9: 删除 mock-heavy ViewModel/PureLogic 测试 (9 files, ~109 tests)
- [x] Task 10: 清理 .csproj (移除 5 个未使用依赖) + 创建 WpfTestHelper
- [x] Task 11: 确认剩余测试 269 passed
- [x] Task 12: ConsultationEditorPureTests (6 tests)
- [x] Task 13: PrescriptionEditorPureTests (10 tests)
- [x] Task 14: CardReaderPureTests (11 tests)
- [x] Task 15: WorkspaceState (+3) + ChangeTracker (+4) 补充测试

### Phase 4: 全量验证
Status: complete
- [x] Server 1017 + Integration 27 + Desktop 307 + Arch 76 = 1427 全绿

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| 3 files referenced DesktopFixture.InitializeWpf() | 1 | 创建 WpfTestHelper 轻量替代 |
| Refit DELETE 204 无法反序列化 ApiResponse | 1 | MedicalCase 删除使用原始 HttpClient |
| Clone 端点 Server 未实现 | 1 | FormulaFlowTests 替换为 ToggleStatus 测试 |
