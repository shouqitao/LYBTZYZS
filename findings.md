# Findings: Sprint 7 - 权限矩阵修复与测试优化收尾

> **更新日期**: 2026-03-16
> **计划文件**: `~/.claude/plans/compressed-greeting-clover.md`

---

## Sprint 7 任务来源

本 Sprint 整合了以下历史计划中**未完成**的任务：

| 来源计划 | 日期 | 整合任务 |
|----------|------|----------|
| Permission Matrix Defect Remediation | 03-10 | D-4, I-2, G-9, G-11 (4个高优先级问题) |
| Test-Driven Implementation | 03-11 | TDD 验证方式 (应用到本 Sprint) |
| Integration Test Performance | 03-12 | 批量迁移、基准测试 |

**已归档/完成的计划** (不纳入本 Sprint):
- Desktop 重构优化 (03-14~03-15) ✅ 已完成
- Test Architecture Refactoring (03-12) ✅ 核心已完成

---

## 历史发现归档

### Desktop 层架构分析 (2026-03-14)

**详细分析**: 见 `docs/plans/archive/desktop-refactoring-2026-03-15/findings_completed.md`

**关键成果**:
- 识别 25 个架构问题并按严重性分类
- 完成 4 个 Phase 的重构优化
- 测试覆盖率从 <20% 提升到 80%+

---

## 计划实施状态分析 (2026-03-16)

### 📋 分析方法论

通过以下方式确认各计划实施状态:
1. 读取计划文档原文
2. 检查 git 提交记录 (2026-03-10 至 2026-03-16)
3. 验证代码库中相关文件存在性
4. 对比计划中的 Task 列表与实际完成度

---

### 1️⃣ Server Test PRD-Driven Refactoring (2026-03-10)

**计划目标**: 重写 Server 测试为 PRD-Driven，6 个并行 Collection fixtures，~3x 加速

**实际状态**: 部分实施 ⚠️

| 计划组件 | 实际状态 | 证据 |
|----------|----------|------|
| ServerFixture unseal | ✅ | `ServerFixture.cs` 已为 public |
| 6 Domain Fixtures | ✅ | AuthUsers, ClinicalData, HerbFormula, SystemOps |
| TestDataBuilders | 📝 | 未找到独立 Builder 类 |
| BusinessAssertions | 📝 | 未找到自定义断言类 |
| PRD-Driven 测试 | 📝 | US_*_MustHaveTests 存在但覆盖率待确认 |

**结论**: 基础设施已搭建，测试数据构建器和业务断言尚未完全实施。

---

### 2️⃣ Permission Matrix Defect Remediation (2026-03-10)

**计划目标**: 修复 role-permission-matrix.md v1.1 发现的 11 个问题

**实际状态**: 设计完成，代码实施未开始 ❌

| 问题编号 | 类别 | 优先级 | 代码变更状态 | 测试状态 |
|----------|------|--------|--------------|----------|
| D-4 | 逻辑缺陷 | HIGH | ❌ 未开始 | ❌ 未开始 |
| D-5 | 逻辑缺陷 | MEDIUM | ❌ 未开始 | ❌ 未开始 |
| D-6 | 逻辑缺陷 | LOW | ❌ 未开始 | ❌ 未开始 |
| I-1 | PRD不一致 | MEDIUM | ❌ 未开始 | ❌ 未开始 |
| I-2 | PRD不一致 | HIGH | ❌ 未开始 | ❌ 未开始 |
| G-7 ~ G-12 | 设计不足 | MIXED | ❌ 未开始 | ❌ 未开始 |

**关键阻塞点**:
- `IsLocked` 动态计算逻辑未实现
- Registration 回退流程代码未实现
- 医生禁用前置校验未实现

**结论**: 这是一份详细的设计文档，但代码实施工作尚未开始。

---

### 3️⃣ Test-Driven Implementation Plan (2026-03-11)

**计划目标**: 通过 TDD 确保权限矩阵缺陷修复和 Journey Test 重构

**实际状态**: 计划中，未开始实施 ❌

| Phase | 任务数 | 实施状态 |
|-------|--------|----------|
| Phase 1: 权限矩阵缺陷验证 | 5 | ❌ 未开始 |
| Phase 2: Journey Test 重构 | 10 | ❌ 未开始 |
| Phase 3: PRD 驱动测试对齐 | 8 模块 | ❌ 未开始 |
| Phase 4: 验证与完成 | 3 | ❌ 未开始 |

**依赖关系**: 本计划依赖于 Permission Matrix Defect Remediation 的实施。

**结论**: 这是一个执行计划，等待前置设计文档的实施。

---

### 4️⃣ Test Architecture Refactoring (2026-03-12)

**计划目标**: 创建 LYBT.Tests.Server.Unit 项目，分层测试架构

**实际状态**: 核心实施完成 ✅

| Task | 计划文件 | 实际状态 | git 提交 |
|------|----------|----------|----------|
| Create Unit Test Project | Task 1 | ✅ | c5f52e2db |
| Migrate PasswordHelper Tests | Task 2 | ✅ | 已迁移 |
| Migrate Logging Tests | Task 3 | ✅ | 已迁移 |
| Migrate Validator Tests | Task 4 | ✅ | 已迁移 |
| Migrate Exception Tests | Task 5 | ✅ | 已迁移 |
| Migrate Entity Tests | Task 6 | 📝 | 部分完成 |
| Migrate Config Tests | Task 7 | 📝 | 部分完成 |
| Cleanup Directories | Task 8 | ✅ | 已完成 |
| Optimize Collections | Task 9 | ✅ | 8→4 已合并 |
| Add runsettings | Task 10 | 📝 | 未确认 |

**验证结果**:
```
$ ls tests/LYBT.Tests.Server.Unit/
Entities/  LYBT.Tests.Server.Unit.csproj  Shared/  Usings.cs  Utilities/  Validators/  bin/  obj/

$ ls tests/LYBT.Tests.Server.Unit/Shared/
Auth/  Configuration/  ExceptionHandling/  Logging/
```

**结论**: 单元测试项目已成功建立，核心测试类已迁移，剩余部分实体和配置测试可继续完善。

---

### 5️⃣ Integration Test Performance Optimization (2026-03-12)

**计划目标**: 将集成测试从 102 秒降至 30 秒以内

**实际状态**: 核心基础设施完成 ✅，批量迁移待续 📝

| Task | 计划内容 | 实际状态 | git 提交 |
|------|----------|----------|----------|
| SharedTestContext | Task 1 | ✅ | d35d054f9 |
| TransactionalIntegrationTestBase | Task 2 | ✅ | 0bb164407 |
| Transactional Collections | Task 3 | ✅ | 0c4f4463b |
| Migrate One Test Class | Task 4 | ✅ | 9590b9264 |
| Performance Benchmark | Task 5 | 📝 | 未执行正式对比 |
| Batch Migration | Task 6 | 📝 | 部分完成 |
| UserJourney Migration | Task 7 | 📝 | 未开始 |
| Final Verification | Task 8 | 📝 | 未执行 |

**关键提交确认**:
- `0bb164407` - feat(tests): add TransactionalIntegrationTestBase with transaction isolation
- `0c4f4463b` - feat(tests): add transactional collection definitions for fast tests
- `9590b9264` - perf(tests): migrate US_Auth_MustHaveTests to transactional model
- `b0def4e1d` - perf(tests): enable parallel execution within collections

**验证结果**:
```
$ ls tests/LYBT.Tests.Server/_Infrastructure/SharedTestContext.cs
SharedTestContext exists
```

**结论**: 性能优化基础设施已就绪，SharedTestContext 和 TransactionalIntegrationTestBase 已可用。剩余工作为批量迁移现有测试类并验证性能提升。

---

## 总结

### 📊 计划实施统计

| 计划文件 | 日期 | 实施状态 | 完成度 |
|----------|------|----------|--------|
| Server Test PRD-Driven | 03-10 | 部分实施 | ~60% |
| Permission Matrix Defect | 03-10 | 设计完成 | 0% |
| Test-Driven Implementation | 03-11 | 计划中 | 0% |
| Test Architecture | 03-12 | 核心完成 | ~80% |
| Integration Test Perf | 03-12 | 核心完成 | ~70% |

### D-4 实施发现 (2026-03-16)

**已完成**:
- `RegistrationListDto` 添加 `HasMedicalCase` 计算属性
- Receptionist 可以访问 `/api/v1/registrations/queue` (PatientAccess 策略)
- 2 个测试验证 D-4 修复

**遗留问题**: MedicalCase-Registration 关联未实现
- 设计文档 D6 要求: "MedicalCase 创建 | Registration.MedicalCaseId 填入"
- 当前实现: 创建 MedicalCase 时不更新 Registration.MedicalCaseId
- 影响: `HasMedicalCase` 始终为 false (MedicalCaseId 始终为 null)
- 建议: 在 MedicalCaseCommandService.CreateFromInputDtoAsync 中添加 Registration 关联逻辑

### 🔴 待实施高优先级任务

1. **Permission Matrix 缺陷修复** (11 个问题)
   - ✅ D-4: Receptionist 医案可见性 (已完成，有遗留关联问题)
   - I-2: "当天可编辑"锁定逻辑
   - G-9: Registration 回退流程
   - G-11: 医生禁用处理

2. **测试性能优化收尾**
   - 批量迁移测试到 Transactional 模型
   - 执行性能基准测试

### ✅ Phase 0 完成: MedicalCase-Registration 关联 (2026-03-16)

**设计文档 D6 要求**: MedicalCase 创建时自动更新 Registration.MedicalCaseId

**实施方案**:
| 组件 | 变更 | 文件 |
|------|------|------|
| DTO | 添加 `RegistrationId` 可选属性 | `MedicalCaseInputDto.cs` |
| Server | 创建时自动回填关联 | `MedicalCaseCommandService.cs` |
| Client API | 接口添加可选参数 | `IMedicalCaseCommandService.cs` |
| Client Impl | 实现传递 RegistrationId | `MedicalCaseService.cs` |
| Mapper | 忽略 RegistrationId 映射 | `MedicalCaseDetailModelMapper.cs` |
| Test Builder | 添加 `WithRegistration()` 方法 | `MedicalCaseBuilder.cs` |
| Test | 验证关联逻辑测试 | `US_MedicalCase_MustHaveTests.cs` |

**使用方式**:
```csharp
// 从前台挂号创建医案时传入 RegistrationId
var result = await _medicalCaseService.CreateMedicalCaseAsync(patientId, registrationId);
```

### 🟢 已完成成果

1. Desktop 重构优化 (Phase 1-4) ✅
2. Server.Unit 测试项目 ✅
3. SharedTestContext 基础设施 ✅
4. TransactionalIntegrationTestBase ✅
5. 测试 Collection 优化 (8→4) ✅

---

## References

- 计划文档: `docs/plans/2026-03-10-*` ~ `docs/plans/2026-03-12-*`
- 归档目录: `docs/plans/archive/`
- 代码验证: `tests/LYBT.Tests.Server.Unit/`, `tests/LYBT.Tests.Server/_Infrastructure/`
