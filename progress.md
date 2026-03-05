# 测试体系重构 - Progress

## Session: 2026-03-05

### Phase: BRAINSTORM -- complete

#### Actions
1. 全面审计 3 个测试项目 (Server 1017 + Desktop 494 + Arch 68)
2. Desktop 逐文件分析: 47 files, 143 Substitute.For, 194 mock-heavy tests
3. 调研 WPF/MVVM 测试最佳实践 (5 patterns from web research)
4. 探索双模式架构: Remote (Refit+HTTP) vs Local (SQLite)
5. 评估 WebApplicationFactory vs 外部进程: 选择 in-process (覆盖 99% 功能正确性)
6. 确认需求调整: 本地模式从 SQLite 迁移到 SQL Server LocalDB
7. 确认新建 LYBT.Tests.Integration 项目 (Desktop+Server 联合验证)
8. 设计三阶段路线: Phase 0 (迁移) -> Phase 2 (联通) -> Phase 3 (本地)
9. 写入设计文档: docs/plans/2026-03-05-desktop-test-simplification-design.md
10. 更新三文件

### Phase: PLAN -- complete

#### Actions
1. 调研本地模式需求变更: SYNC-D02 将废弃整个 DataSource 层
2. 确认: Phase 0 取消, 全部本地模式测试删除 (等 SYNC-D02 后再补)
3. 确认: Desktop+WebAPI 联通测试与本地 DB 选择完全解耦
4. 并行代理收集 24 个测试文件详情 (测试数/mock 使用/分类)
5. 并行代理收集 ServerFixture/WebAPI/Refit 接口信息
6. 并行代理收集 EndToEnd 测试结构和 DesktopFixture 分析
7. 编写 16-task 实施计划: docs/plans/2026-03-05-test-restructuring-plan.md
8. 更新三文件

### Phase: EXECUTE -- complete

#### Wave 1 (并行)
**Agent A (Integration 脚手架, Tasks 1+2):**
- 创建 LYBT.Tests.Integration 项目 + IntegrationFixture + IntegrationTestBase
- 修正计划中 6 处偏差: 命名空间、属性名、DB 连接方式等
- BUILD SUCCEEDED

**Agent B (删除 + 清理, Tasks 8+9+10):**
- 删除 29 个文件 (20 local mode + 9 mock-heavy)
- 移除 5 个未使用 NuGet/项目依赖
- 创建 WpfTestHelper 替代 DesktopFixture.InitializeWpf()
- 269 tests, 0 failed

#### Wave 2 (并行)
**Agent C (Integration 测试, Tasks 3-7):**
- 创建 5 个 Flow 测试文件: Auth(6) + Patient(5) + Herb(4) + Formula(4) + MedicalCase(8)
- 27 tests, 27 passed
- 发现: Refit DELETE 204 问题, Clone 端点未实现

**Agent D (纯逻辑重写, Tasks 12-15):**
- 创建 3 个新测试文件: ConsultationEditor(6) + PrescriptionEditor(10) + CardReader(11)
- 补充 2 个现有文件: WorkspaceState(+3) + ChangeTracker(+4)
- Desktop 269→307 tests (net +38), 0 failed

### Phase: VERIFY -- complete

#### 全量测试结果

| Project | Before | After | Delta |
|---------|--------|-------|-------|
| Server | 1017 | 1017 | 0 |
| Integration | 0 | 27 | +27 |
| Desktop | 494 | 307 | -187 |
| Architecture | 68 | 76 | +8 |
| **Total** | **1579** | **1427** | **-152** |

所有 1427 tests 全绿, 0 failed, 0 skipped (Arch 有 1 skip)。
