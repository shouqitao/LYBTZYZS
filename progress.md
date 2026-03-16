# Progress: Sprint 7 - 权限矩阵修复与测试优化收尾

> **更新日期**: 2026-03-16
> **状态**: 🔄 Sprint 7 已规划，等待开始执行
> **计划文件**: `~/.claude/plans/compressed-greeting-clover.md`

---

## Sprint 7 概览

### 目标
整合 03-10 ~ 03-12 期间未完成的工作，完成高优先级权限矩阵缺陷修复和测试性能优化收尾。

### 时间线
- **Day 0**: Phase 0 - 遗留问题修复 (MedicalCase-Registration 关联)
- **Day 1-3**: Phase 1 - 权限矩阵缺陷修复 (4个高优先级问题)
- **Day 4**: Phase 2 - 测试性能优化收尾 (批量迁移、基准测试)
- **Day 5**: Phase 3 - 验证与文档 (全量测试、归档)

### Sprint 任务清单

**Phase 0: 遗留问题修复 (已完成)**
- [x] Task 0.1: MedicalCaseInputDto 添加 RegistrationId
- [x] Task 0.2: MedicalCaseCommandService 实现自动回填
- [x] Task 0.3: 添加验证测试 `CreateCase_WithRegistrationId_LinksRegistration`
- [x] Task 0.4: 更新 Client 端调用 `CreateMedicalCaseAsync(Guid patientId, Guid? registrationId = null)`

**Phase 1: 权限矩阵缺陷修复**
- [x] Task 1.1: D-4 Receptionist 医案可见性
- [ ] Task 1.2: I-2 "当天可编辑"锁定逻辑
- [ ] Task 1.3: G-9 Registration 回退流程
- [ ] Task 1.4: G-11 医生禁用后挂号处理

**Phase 2: 测试性能优化收尾**
- [ ] Task 2.1: 批量迁移 MustHave 测试
- [ ] Task 2.2: 执行性能基准测试

**Phase 3: 验证与文档**
- [ ] Task 3.1: 全量测试运行
- [ ] Task 3.2: 归档计划文件

---

## 历史归档

### Desktop 层重构优化 (2026-03-14 ~ 2026-03-15)

**全部完成** ✅

- Phase 1: 紧急修复 (5/5 任务)
- Phase 2: 测试覆盖 (5/5 任务，新增 104 个测试)
- Phase 3: UI 规范化 (5/5 任务)
- Phase 4: 架构完善 (主体完成)

**详细记录**: 见 `docs/plans/archive/desktop-refactoring-2026-03-15/progress_completed.md`

---

## 当前会话 (2026-03-16)

### Task 1.1 完成 - D-4 Receptionist 医案可见性

**实施内容**:
1. **DTO 更新**: `RegistrationListDto` 添加 `HasMedicalCase` 计算属性
   - 文件: `src/Shared/LYBT.Shared.Models/Contracts/Registration/RegistrationListDto.cs`
   - 实现: `public bool HasMedicalCase => MedicalCaseId.HasValue;`

2. **测试基础设施**: 添加 Receptionist 角色支持
   - `ServerFixture`: 添加 Receptionist 用户种子数据
   - `IntegrationTestBase`: 添加 `LoginAsReceptionistAsync()` 方法

3. **D-4 验证测试** (2个):
   - `D4_Receptionist_CanAccessQueue_WithMedicalCaseHint`: 验证 Receptionist 可访问队列
   - `D4_Registration_HasMedicalCase_ComputedCorrectly`: 验证计算属性逻辑正确

**测试运行结果**:
```
测试总数: 2
     通过数: 2
     失败数: 0
```

---

## 当前会话 (2026-03-16 续)

### Phase 1 计划完成 - 权限矩阵缺陷修复

**创建实施计划**: `docs/plans/2026-03-16-phase1-permission-matrix-fixes-plan.md`

**包含任务**:
| 任务 | 问题 | 设计要点 |
|------|------|---------|
| Task 1 | I-2 "当天可编辑" | IsLocked动态计算 + 角色分层权限检查 |
| Task 2 | G-9 Registration回退 | 取消分流: Receptionist回退/Doctor闭环 |
| Task 3 | G-11 医生禁用检查 | 禁用前置校验Waiting挂号数量 |
| Task 4 | 全量测试 | 验证所有修复不破坏现有功能 |

---

### Phase 0 完成 - MedicalCase-Registration 关联修复

**实施内容**:
1. **DTO 更新**: `MedicalCaseInputDto` 添加 `RegistrationId` 可选属性
   - 文件: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseInputDto.cs`

2. **Server端逻辑**: `MedicalCaseCommandService.CreateFromInput` 实现自动回填
   - 文件: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
   - 创建医案时自动更新 `Registration.MedicalCaseId`

3. **Client端更新**: `CreateMedicalCaseAsync` 添加可选 `registrationId` 参数
   - 接口: `IMedicalCaseCommandService`
   - 实现: `MedicalCaseService`

4. **验证测试**: 添加 `CreateCase_WithRegistrationId_LinksRegistration` 测试
   - 文件: `tests/LYBT.Tests.Server/Features/US_MedicalCase_MustHaveTests.cs`
   - 测试构建器: `MedicalCaseBuilder.WithRegistration()`

**修改文件**:
```
src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseInputDto.cs
src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs
src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IMedicalCaseCommandService.cs
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs
tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/MedicalCaseBuilder.cs
tests/LYBT.Tests.Server/Features/US_MedicalCase_MustHaveTests.cs
```
- 当前 `HasMedicalCase` 始终为 false (因为 MedicalCaseId 未被填充)
- 需要后续实现 Registration-MedicalCase 关联逻辑

### 任务梳理完成

**已完成**:
1. ✅ 归档 Desktop 重构优化所有相关文件
2. ✅ 按时间顺序分析 5 个历史计划文件
3. ✅ 通过 git 提交记录验证实际实施状态
4. ✅ 验证代码库中相关文件存在性
5. ✅ 更新 task_plan.md / findings.md / progress.md

**分析的计划文件** (按时间从远到近):

| 序号 | 计划文件 | 日期 | 实施状态 |
|------|----------|------|----------|
| 1 | Server Test PRD-Driven Refactoring | 03-10 | 部分实施 (~60%) |
| 2 | Permission Matrix Defect Remediation | 03-10 | 设计完成，未实施 (0%) |
| 3 | Test-Driven Implementation | 03-11 | 计划中，未实施 (0%) |
| 4 | Test Architecture Refactoring | 03-12 | 核心完成 (~80%) |
| 5 | Integration Test Performance | 03-12 | 核心完成 (~70%) |

---

## 关键发现

### ✅ 已完成的成果

1. **LYBT.Tests.Server.Unit 项目已建立**
   - 位置: `tests/LYBT.Tests.Server.Unit/`
   - 内容: PasswordHelper, Logging, Validator, Exception, Entity 测试
   - 提交: c5f52e2db

2. **性能优化基础设施就绪**
   - SharedTestContext (共享 WebApplicationFactory)
   - TransactionalIntegrationTestBase (事务隔离替代 Respawn)
   - Transactional Collection 定义 (AuthUsersFast 等)
   - 提交: 0bb164407, 0c4f4463b, 9590b9264

3. **测试 Collection 优化**
   - 从 8 个合并为 4 个 (AuthUsers, ClinicalData, HerbFormula, SystemOps)
   - 提交: b0def4e1d

### ⏳ 待完成的工作

1. **Permission Matrix 缺陷修复** (11 个问题)
   - D-4: Receptionist 医案可见性
   - I-2: "当天可编辑"锁定逻辑
   - G-9: Registration 回退流程
   - G-11: 医生禁用处理

2. **测试性能优化收尾**
   - 批量迁移剩余测试类到 Transactional 模型
   - 执行性能基准测试验证 < 30 秒目标

---

## 下一步行动建议

### 选项 1: 继续 Permission Matrix 缺陷修复
- 实施 D-4 Receptionist 医案可见性 (API + 测试)
- 实施 I-2 "当天可编辑"锁定逻辑
- 实施 G-9 Registration 回退流程
- 预计工作量: 3-5 天

### 选项 2: 继续测试性能优化
- 批量迁移 MustHave 测试到 Transactional 模型
- 迁移 UserJourney 测试
- 执行性能基准测试
- 预计工作量: 2-3 天

### 选项 3: 创建新的 Sprint 计划
- 基于当前状态规划新的迭代
- 结合 Permission Matrix 修复和性能优化收尾
- 预计工作量: 1 周 Sprint

---

## 验证命令备忘

```bash
# 验证 Server.Unit 测试
dotnet test tests/LYBT.Tests.Server.Unit/ -v n

# 验证 SharedTestContext 存在
ls tests/LYBT.Tests.Server/_Infrastructure/SharedTestContext.cs

# 验证 TransactionalIntegrationTestBase 存在
ls tests/LYBT.Tests.Server/_Infrastructure/TransactionalIntegrationTestBase.cs

# 运行 Server 集成测试
dotnet test tests/LYBT.Tests.Server/ -v n

# 查看相关提交
git log --oneline --since="2026-03-10" --grep="test\|performance\|transactional"
```

---

## References

- 当前任务计划: `./task_plan.md`
- 分析发现: `./findings.md`
- 历史归档: `docs/plans/archive/desktop-refactoring-2026-03-15/`
