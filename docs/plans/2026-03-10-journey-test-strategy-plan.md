# Journey Test Strategy: "From Zero to Production" Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 建立一套"从无到有"的 Journey Test 体系，按真实业务顺序验证 WebAPI 完整功能链，确保系统可上线。

**Architecture:** 以 User Story Map 的 4 个 Narrative 为骨架，按"系统启动 -> 用户创建 -> 基础数据 -> 首诊 -> 复诊 -> 管理运维"的时间线编排测试。每个 Journey Test 是一部"微型电影"，包含正常路径 + 关键异常路径。现有基础设施 (ServerFixture + DomainFixtures + Respawn) 保持不变。

**Tech Stack:** .NET 8, xUnit 2.x, FluentAssertions, SQL Server LocalDB, WebApplicationFactory, Respawn

---

## 现状审计

### 测试分布 (Server 项目, 887 tests total)

| 层级 | 目录 | 测试数 | 定位 | 决策 |
|------|------|--------|------|------|
| PureLogic | `PureLogic/` | 750 | 纯逻辑单元测试 (Validator/Model/Config) | **KEEP** - 不动 |
| Journey | `UserJourneys/` | 23 | 业务链路测试 (Layer A) | **重构** - 本计划核心 |
| Feature | `Features/` | 111 | 单功能验证 (Layer B) | **暂移除** - 后续按需补充 |
| RateLimiting | `RateLimiting/` | 3 | 基础设施 | **KEEP** - 不动 |

### 现有 Journey Tests 审计

| 文件 | Collection | Facts | 覆盖内容 | 状态 |
|------|-----------|-------|----------|------|
| AuthJourneyTests | Auth | 1 | 登录/Token/刷新/登出/匿名拒绝 | 保留, 补充负面场景 |
| BootstrapJourneyTests | Users | 1 | SysAdmin创建3角色+药材+验方+权限 | 保留, 拆分职责 |
| AdminSetupJourneyTests | Users | 1 | Admin管理用户 | 保留 |
| HerbFormulaManagementJourneyTests | HerbFormula | 1 | 药材CRUD+验方CRUD | 保留 |
| PatientManagementJourneyTests | Clinical | 1 | 患者CRUD+搜索 | 保留 |
| FirstVisitJourneyTests | Clinical | 5 | 首诊完整流程+4异常 | **核心**, 保留 |
| ReturnVisitJourneyTests | Clinical | 2 | 复诊+编辑原因 | 保留 |
| DoctorClinicalJourneyTests | Clinical | 1 | 医生看诊全流程 | **与FirstVisit重叠**, 合并或删除 |
| MedicalCaseEditJourneyTests | Clinical | 1 | 医案编辑场景 | 保留 |
| BatchOperationsJourneyTests | Users | 1 | 批量操作 | 保留 |
| CrossNarrativeValidationTests | Clinical | 8 | 跨叙事验证 (X5-X12) | 保留 |

### 冗余分析

1. **DoctorClinicalJourneyTests** vs **FirstVisitJourneyTests**: 高度重叠 (创建患者->创建医案->诊断->处方->完成)。FirstVisit 更完整 (含挂号流程)。建议删除 DoctorClinicalJourneyTests。

2. **Features/ 下的 US_*_MustHaveTests (111 tests)**: 属于 Layer B (单功能验证)。用户决策"先做 Layer A, Layer B 后补"。建议暂时从项目中排除 (不删除文件，移到 `_Deferred/` 目录)。

---

## Journey 故事线设计

按"从无到有"时间线编排，映射到 User Story Map Narrative:

```
Chapter 0: Auth & Security        [Narrative 4 基础] -> AuthJourneyTests
Chapter 1: System Bootstrap       [Narrative 4 启动] -> BootstrapJourneyTests
Chapter 2: Admin Setup            [Narrative 4 管理] -> AdminSetupJourneyTests
Chapter 3: Master Data (药材验方)  [Narrative 3]      -> HerbFormulaManagementJourneyTests
Chapter 4: Patient Management     [Narrative 1 前置] -> PatientManagementJourneyTests
Chapter 5: First Visit (首诊)     [Narrative 1]      -> FirstVisitJourneyTests
Chapter 6: Return Visit (复诊)    [Narrative 2]      -> ReturnVisitJourneyTests
Chapter 7: Medical Case Edit      [跨Narrative]      -> MedicalCaseEditJourneyTests
Chapter 8: Cross-Narrative Guard  [跨Narrative]      -> CrossNarrativeValidationTests
```

### 每章覆盖的 User Story (Must Have)

| Chapter | 正常路径覆盖 US | 异常路径覆盖 |
|---------|-----------------|-------------|
| 0 Auth | US-AUTH-001(登录), US-AUTH-002(登出), US-AUTH-005(Token验证), US-AUTH-008(刷新), US-AUTH-009(密码错误), US-AUTH-010(匿名拒绝) | 密码错误401, 用户不存在401, 账号禁用403, Token过期401 |
| 1 Bootstrap | US-USER-001(创建用户) x3角色, 角色权限验证 | 重复用户名, Doctor不能创建用户 |
| 2 Admin Setup | US-USER-002(列表), US-USER-003(详情), US-USER-004(更新), US-USER-005(删除) | 删除自己被拒, Admin不能创建SuperAdmin |
| 3 Master Data | US-HERB-001~005, US-FORM-001~006 | 重复药材名, 删除被引用药材 |
| 4 Patient | US-PAT-001~004 | 重复身份证号, 搜索无结果 |
| 5 First Visit | US-REG-001~006, US-MC-001~007, US-MC-013 | BR-001重复活跃医案, BR-003空诊断阻止完成, 取消挂号 |
| 6 Return Visit | US-MC-009(历史), US-MC-010(搜索), US-MC-018(复制处方) | 完成后编辑需原因 |
| 7 Case Edit | US-MC-008(编辑), US-MC-011(模式切换), US-MC-014/015(挂起/恢复) | 挂起医案不能完成 |
| 8 Cross Guard | 跨模块联动验证 | X5患者禁用阻止创建, X6引用保护, X7长会话Token刷新, X10禁用药材不可用 |

---

## Implementation Phases

### Phase 1: 清理与整合 (Clean & Consolidate)

**目标**: 移除冗余，建立干净的起点。

#### Task 1.1: 删除 DoctorClinicalJourneyTests (冗余)

**Files:**
- Delete: `tests/LYBT.Tests.Server/UserJourneys/DoctorClinicalJourneyTests.cs`

**理由**: 与 FirstVisitJourneyTests 高度重叠。FirstVisit 包含完整的挂号流程更贴近真实场景。

**验证**: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~UserJourneys" --no-build` 仍然通过。

#### Task 1.2: 移除 Features/ 测试 (Layer B 暂缓)

**Files:**
- Move: `tests/LYBT.Tests.Server/Features/` -> `tests/LYBT.Tests.Server/_Deferred/Features/`

**理由**: Layer B 测试后续按需补充。不删除文件，仅移到 `_Deferred/` 避免编译。

**注意**: 需要同步更新 `.csproj` 排除 `_Deferred/` 目录编译:
```xml
<ItemGroup>
  <Compile Remove="_Deferred\**" />
</ItemGroup>
```

**验证**: `dotnet build tests/LYBT.Tests.Server/` 编译通过，Features 测试不再计数。

#### Task 1.3: 移除 _Infrastructure/BusinessAssertions.cs 和相关未使用文件

**Files:**
- Review: `tests/LYBT.Tests.Server/_Infrastructure/BusinessAssertions.cs`
- Review: `tests/LYBT.Tests.Server/_Infrastructure/TestDataBuilders/`

如果这些文件仅被 Features/ 测试引用，一起移到 `_Deferred/`。

**验证**: 编译通过，无孤立引用。

---

### Phase 2: 补全 Auth Journey (Chapter 0)

**目标**: AuthJourneyTests 补充关键负面场景，覆盖所有 Auth Must Have US。

#### Task 2.1: 补充 Auth 负面场景

**File:** `tests/LYBT.Tests.Server/UserJourneys/AuthJourneyTests.cs`

**当前覆盖**:
- [x] 正确密码登录返回 Token
- [x] Token 验证
- [x] 错误密码返回 401
- [x] Token 刷新
- [x] 登出
- [x] 匿名访问拒绝

**需补充**:
- [ ] 不存在的用户名登录 -> 401
- [ ] 已禁用用户登录 -> 401 或 403
- [ ] SysAdmin 登录验证 (US-AUTH-012)
- [ ] 空用户名/密码 -> 400 (Validator 拦截)

**新增测试方法**:
```csharp
[Fact]
public async Task Auth_NegativePaths_Journey()
{
    await ResetForJourneyAsync();

    // 1. Non-existent user -> 401
    // 2. Empty username -> 400
    // 3. SysAdmin login -> OK
    // 4. Disable user via admin, then login -> fail
}
```

**验证**: `dotnet test --filter "AuthJourneyTests"` 通过。

---

### Phase 3: 补全 Bootstrap Journey (Chapter 1)

**目标**: BootstrapJourneyTests 补充创建用户的负面场景。

#### Task 3.1: 补充 Bootstrap 负面场景

**File:** `tests/LYBT.Tests.Server/UserJourneys/BootstrapJourneyTests.cs`

**需补充**:
- [ ] 重复用户名创建 -> 400
- [ ] 必填字段缺失 -> 400 (Validator)
- [ ] Doctor 角色不能创建用户 -> 403 (已在 Step 5 验证)

可以新增一个 `[Fact] Bootstrap_NegativePaths()` 或追加到现有 journey 中。

---

### Phase 4: 补全 Master Data Journey (Chapter 3)

**目标**: HerbFormulaManagementJourneyTests 补充药材验方的完整 CRUD + 负面场景。

#### Task 4.1: 审查并补充药材验方 Journey

**File:** `tests/LYBT.Tests.Server/UserJourneys/HerbFormulaManagementJourneyTests.cs`

**需确认覆盖**:
- [ ] 药材 CRUD 完整 (创建/读取/更新/删除)
- [ ] 验方 CRUD 完整
- [ ] 药材搜索 (名称/拼音码)
- [ ] 验方启用/禁用
- [ ] 删除被验方引用的药材 -> 被阻止或提示

---

### Phase 5: 补全 Patient Journey (Chapter 4)

**目标**: PatientManagementJourneyTests 补充患者管理完整链路。

#### Task 5.1: 审查并补充患者管理 Journey

**File:** `tests/LYBT.Tests.Server/UserJourneys/PatientManagementJourneyTests.cs`

**需确认覆盖**:
- [ ] 创建患者 (US-PAT-001)
- [ ] 搜索患者 (US-PAT-002) -- 姓名、拼音码
- [ ] 查看详情 (US-PAT-003)
- [ ] 更新信息 (US-PAT-004)
- [ ] 负面: 重复身份证号

---

### Phase 6: 审查 First Visit & Return Visit (Chapter 5-6)

**目标**: 确认现有 FirstVisitJourneyTests + ReturnVisitJourneyTests 覆盖完整。

#### Task 6.1: 审查 FirstVisitJourneyTests

**当前覆盖**:
- [x] 正常路径: 患者登记 -> 挂号 -> 看诊 -> 诊断 -> 处方 -> 完成
- [x] BR-001: 重复活跃医案
- [x] BR-003: 空诊断阻止完成
- [x] BR-003: 无处方决定阻止完成
- [x] 取消挂号

**可能需补充**:
- [ ] US-MC-016: 验方导入到处方 (Should Have, 可延后)
- [ ] 挂号状态自动跟随医案完成 (US-REG-005)

#### Task 6.2: 审查 ReturnVisitJourneyTests

**当前覆盖**:
- [x] 搜索患者 -> 查看历史 -> 创建新案 -> 完成
- [x] 完成后编辑需原因

**可能需补充**:
- [ ] 复制历史处方 (US-MC-018, Should Have)

---

### Phase 7: 审查 Cross-Narrative (Chapter 8)

**目标**: 确认 CrossNarrativeValidationTests 覆盖关键跨模块验证。

#### Task 7.1: 审查 CrossNarrativeValidationTests

**当前覆盖 (8 facts)**:
- [x] X5: 患者禁用阻止创建医案
- [x] X6: 药材引用保护
- [x] X7: Token 刷新
- [x] X8: 健康检查
- [x] X9: 患者禁用阻止挂号
- [x] X10: 禁用药材不可用于处方
- [x] X11: 并发编号竞态
- [x] X12: print-completed 端点探测

**状态**: 覆盖较完整，无需大幅修改。

---

### Phase 8: 运行全量验证

#### Task 8.1: 全量测试

```bash
# 编译
dotnet build tests/LYBT.Tests.Server/

# 运行全部 Journey Tests
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~UserJourneys" -v normal

# 运行全部测试 (PureLogic + Journey + RateLimiting)
dotnet test tests/LYBT.Tests.Server/ -v normal
```

**预期结果**:
- PureLogic: ~750 passed
- UserJourneys: ~25-30 passed (补充后)
- RateLimiting: ~3 passed
- Features: 0 (已移到 _Deferred/)

#### Task 8.2: 更新文档

- 更新 CLAUDE.md 中的测试数量
- 更新 testing-architecture.md 记忆

---

## 优先级排序

| 优先级 | Phase | 任务 | 预计耗时 |
|--------|-------|------|---------|
| P0 | Phase 1 | 清理冗余 (Task 1.1-1.3) | 15 min |
| P1 | Phase 2 | Auth 负面场景 | 20 min |
| P2 | Phase 3 | Bootstrap 负面场景 | 15 min |
| P2 | Phase 4 | 药材验方审查补充 | 20 min |
| P2 | Phase 5 | 患者管理审查补充 | 15 min |
| P3 | Phase 6 | 首诊/复诊审查 | 10 min |
| P3 | Phase 7 | 跨叙事审查 | 10 min |
| P4 | Phase 8 | 全量验证 + 文档 | 15 min |

**总预计: ~2 小时**

---

## Journey Test 覆盖的 Must Have US 追溯矩阵

| US 编号 | 描述 | 覆盖 Journey | 状态 |
|---------|------|-------------|------|
| US-AUTH-001 | 用户登录 | AuthJourney | Covered |
| US-AUTH-002 | 用户登出 | AuthJourney | Covered |
| US-AUTH-005 | Token 验证 | AuthJourney | Covered |
| US-AUTH-008 | Token 刷新 | AuthJourney | Covered |
| US-AUTH-009 | 安全审计 | AuthJourney (负面路径) | To Add |
| US-AUTH-010 | 超管登录 | AuthJourney | Covered |
| US-AUTH-012 | 超管首次登录 | BootstrapJourney | Covered |
| US-USER-001 | 创建用户 | BootstrapJourney | Covered |
| US-USER-002 | 用户列表 | AdminSetupJourney | Covered |
| US-USER-003 | 用户详情 | AdminSetupJourney | Covered |
| US-USER-004 | 更新用户 | AdminSetupJourney | Covered |
| US-USER-005 | 删除用户 | AdminSetupJourney | Covered |
| US-PAT-001 | 创建患者 | FirstVisitJourney | Covered |
| US-PAT-002 | 搜索患者 | ReturnVisitJourney | Covered |
| US-PAT-003 | 患者详情 | PatientManagement | Covered |
| US-PAT-004 | 更新患者 | PatientManagement | To Verify |
| US-HERB-001 | 创建药材 | BootstrapJourney | Covered |
| US-HERB-002 | 药材列表 | HerbFormulaJourney | Covered |
| US-HERB-003 | 药材详情 | HerbFormulaJourney | To Verify |
| US-HERB-004 | 更新药材 | HerbFormulaJourney | To Verify |
| US-HERB-005 | 删除药材 | HerbFormulaJourney | To Verify |
| US-FORM-001 | 创建验方 | BootstrapJourney | Covered |
| US-FORM-002 | 验方列表 | HerbFormulaJourney | To Verify |
| US-FORM-003 | 验方详情 | BootstrapJourney | Covered |
| US-FORM-004 | 更新验方 | HerbFormulaJourney | To Verify |
| US-FORM-005 | 删除验方 | HerbFormulaJourney | To Verify |
| US-FORM-006 | 启用/禁用验方 | HerbFormulaJourney | To Verify |
| US-MC-001 | 创建医案 | FirstVisitJourney | Covered |
| US-MC-002 | 填写诊断 | FirstVisitJourney | Covered |
| US-MC-003 | 标记处方 | FirstVisitJourney | Covered |
| US-MC-004 | 开具处方 | FirstVisitJourney | Covered |
| US-MC-005 | 聚合保存 | FirstVisitJourney | Covered |
| US-MC-006 | 挂起医案 | MedicalCaseEdit | To Verify |
| US-MC-007 | 完成医案 | FirstVisitJourney | Covered |
| US-MC-009 | 医案列表 | ReturnVisitJourney | Covered |
| US-MC-013 | 权限控制 | BootstrapJourney | Covered |
| US-REG-001 | 创建挂号 | FirstVisitJourney | Covered |
| US-REG-002 | 医生快速看诊 | FirstVisitJourney | Covered |
| US-REG-003 | 挂号队列 | FirstVisitJourney | Covered |
| US-REG-004 | 取消挂号 | FirstVisitJourney | Covered |
| US-REG-005 | 状态跟随 | FirstVisitJourney | To Verify |
| US-REG-006 | 医案取消联动 | CrossNarrative | To Verify |
| US-CFG-001 | 服务端配置 | BootstrapJourney (health) | Covered |
| US-CFG-002 | 客户端配置 | N/A (Desktop) | N/A |
| US-SYNC-008 | 模式切换 | N/A (Desktop) | N/A |

**Must Have 51 US 中:**
- Server WebAPI 相关: ~43 US
- Desktop 专属 (不在本计划范围): ~8 US (SHELL, CFG-002, SYNC-008)
- Journey 已覆盖: ~30 US (Covered)
- 需补充/验证: ~13 US (To Add / To Verify)

---

## 设计原则

1. **一个 Journey = 一部微型电影**: 有角色、有情节、有冲突、有结局。不是孤立的 API 调用。
2. **正常路径 + 关键异常路径**: 每个 Journey 的正常路径是 `[Fact]`，关键业务规则违反是额外的 `[Fact]`。
3. **每个 Journey 自包含**: `ResetForJourneyAsync()` 清空数据库，Journey 从零开始搭建所有前置条件。
4. **命名规范**: `{Narrative}_{NormalOrException}_{Description}` (如 `FirstVisit_Normal_Path`, `FirstVisit_Exception_BR001_DuplicateActiveCase`)
5. **Collection 隔离**: 不同 Collection 使用独立数据库，可并行执行。同一 Collection 内顺序执行。

---

## 后续扩展 (Layer B)

Layer A (Journey) 完成后，按需补充 Layer B (Feature Tests):

1. 从 `_Deferred/Features/` 恢复需要的测试文件
2. 针对 Journey 未覆盖的边界条件补充单功能测试
3. 优先补充: 认证边界、数据验证、分页/过滤

---

> 创建日期: 2026-03-10
> 状态: PLAN (待执行)
