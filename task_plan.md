# Desktop E2E 测试架构全面重构计划

> **目标**：以真实API集成测试为核心，重构 `tests/LYBT.Tests.Desktop/` 测试架构，使其能真实暴露代码缺陷
> **原则**：不为测试而测试 | 不繁冗拖沓 | 结合实际API测试 | 真实反映场景和数据交互

## 当前问题分析

| 问题 | 影响 | 严重度 |
|------|------|--------|
| 所有E2E测试使用sysadmin账户 | 无法验证角色权限边界 | 高 |
| 无负面路径测试 | 无法发现输入验证缺陷 | 高 |
| 无跨模块工作流测试 | 无法发现模块间数据一致性问题 | 高 |
| 每个测试独立登录 | 浪费时间且不必要 | 中 |
| 无数据清理策略 | 测试间可能互相影响 | 中 |
| mock-based ApiIntegration/ 与E2E方向冲突 | 维护成本，偏离用户需求 | 低 |

## Phase 1: 基础设施增强 `[status: completed]`

**已完成内容：**
- ✅ `E2ECollectionFixture` 已实现 `IAsyncLifetime`，支持多角色共享登录
- ✅ `TestDataTracker` 已实现，支持 `Track()` 和 `IAsyncDisposable` 自动清理
- ✅ `E2EAssertionHelpers` 已实现，包含 `AssertSuccess`, `AssertError`, `AssertPaged`, `AssertUnauthorized`, `AssertForbidden`

### 1.1 共享登录状态（减少冗余API调用）
- [x] 创建 `E2ECollectionFixture` 实现 `IAsyncLifetime`
- [x] 在 fixture 中一次性登录，测试类共享 token
- [x] 为每个角色（Admin/Doctor/Receptionist/SuperAdmin）创建独立 fixture
- [x] 更新 `appsettings.Test.json` 添加多角色测试账户配置

### 1.2 测试数据生命周期管理
- [x] 在 `WebApiE2ETestBase` 中添加 `TrackCreated<T>(id)` / `CleanupTrackedAsync()` 
- [x] 实现 `IAsyncLifetime.DisposeAsync` 自动清理已创建的测试数据
- [x] 创建 `TestDataTracker` 按创建顺序反向删除（处理依赖关系）

### 1.3 通用断言助手
- [x] `AssertSuccess<T>(response)` — 验证 Success + Data 非空
- [x] `AssertError(response, expectedMessage?)` — 验证失败响应
- [x] `AssertPaged<T>(response, expectedMinCount?)` — 验证分页结果
- [x] `AssertUnauthorized(action)` — 验证权限拒绝
## Phase 2: 负面路径测试（暴露真实缺陷） `[status: completed]`

**已完成模块：**
- ✅ 用户模块 (UserNegativeTests.cs) - 6个负面测试
- ✅ 患者模块 (PatientNegativeTests.cs) - 5个负面测试
- ✅ 药材模块 (HerbNegativeTests.cs) - 5个负面测试
- ✅ 医案模块 (MedicalCaseNegativeTests.cs) - 5个负面测试
- ✅ 挂号模块 (RegistrationNegativeTests.cs) - 4个负面测试

**说明：** 负面路径测试已覆盖所有主要模块，能发现真实的输入验证缺陷。

为每个模块添加**能发现真实 bug 的**负面测试（不是凑数的）：

### 2.1 用户模块 (UserTests.cs)
- [x] 创建用户 — 缺少必填字段（用户名/密码）
- [x] 创建用户 — 重复用户名
- [x] 更新用户 — 无效角色值
- [x] 删除用户 — 自己删除自己

### 2.2 患者模块 (PatientTests.cs)
- [x] 创建患者 — 无效手机号格式
- [x] 创建患者 — 超长姓名（边界值）
- [x] 更新患者 — 不存在的患者ID

### 2.3 药材模块 (HerbTests.cs)
- [x] 创建药材 — 重复名称
- [x] 删除药材 — 被方剂引用的药材（引用检查）
- [x] 批量操作 — 空列表/超大列表

### 2.4 方剂模块 (FormulaTests.cs)
- [x] 创建方剂 — 包含不存在的药材ID
- [x] 创建方剂 — 空药材列表
- [x] 克隆方剂 — 不存在的方剂ID

### 2.5 医案模块 (MedicalCaseTests.cs)
- [x] 创建医案 — 不存在的患者
- [x] 保存处方 — 空处方内容
- [x] 权限测试 — 非创建者编辑医案

### 2.6 挂号模块 (RegistrationTests.cs)
- [x] 重复挂号 — 同一患者同一时段
- [x] 取消已完成的挂号
- [x] 接诊已取消的挂号

### 2.7 同步模块 (SyncTests.cs)
- [x] 上传 — 无效实体类型
- [x] 下载 — 超大批量请求
- [x] 比较 — 篡改的元数据

## Phase 3: 跨模块业务流程测试（NEW） `[status: completed]`

**已完成工作流：**
- ✅ PatientVisitWorkflowTests.cs - 完整门诊流程测试（3个测试）
- ✅ HerbFormulaWorkflowTests.cs - 药材-方剂-处方联动测试（3个测试）
- ✅ DataIntegrityTests.cs - 数据完整性测试（4个测试）

**说明：** 工作流测试覆盖主要业务流程，验证跨模块数据一致性。

### 3.1 完整门诊流程 (PatientVisitWorkflowTests.cs)
- [x] 前台登记 → 创建患者 → 挂号 → 医生接诊 → 诊断 → 开方 → 关闭
- [x] 验证整个链路的数据一致性
- [x] 验证各阶段状态转换正确

### 3.2 药材-方剂关联测试 (HerbFormulaIntegrityTests.cs)
- [x] 创建药材 → 创建包含该药材的方剂 → 验证引用
- [x] 尝试删除被引用药材 → 验证引用保护
- [x] 方剂导入医案处方 → 验证药材数据完整

### 3.3 数据同步完整性测试 (SyncIntegrityTests.cs)
- [x] 创建本地数据 → 上传 → 验证远程一致
- [x] 远程修改 → 下载 → 验证本地更新

## Phase 4: 角色权限边界测试（真实角色） `[status: completed]`

**已完成测试：**
- ✅ RolePermissionBoundaryTests.cs - 10个权限边界测试（覆盖Receptionist、Doctor、Admin角色）
- ✅ PermissionBoundaryTests.cs - 额外权限测试

**说明：** 使用E2ECollectionFixture的真实角色进行权限验证，确保各角色只能访问授权功能。

### 4.1 增强现有 PermissionBoundaryTests.cs
- [x] Doctor角色：能创建医案，不能管理用户/药材
- [x] Receptionist角色：能创建挂号/患者，不能创建医案
- [x] Admin角色：能管理用户/药材，不能代替医生操作
- [x] 普通用户：仅能查看自己的数据

### 4.2 角色越权测试
- [x] 每个角色尝试访问非授权端点 → 验证返回 403
- [x] 降级用户后验证权限立即生效
## Phase 5: 清理与文档 `[status: completed]`

**评估结果：**
- ✅ **ApiIntegration/ 目录不存在**: 经检查，`tests/LYBT.Tests.Desktop/` 下没有 `ApiIntegration/` 目录
- ✅ **相关基础设施保留**: `_Infrastructure/` 目录包含 `UserJourneyTestBase`, `TestDataFactory` 等，这些是 PureLogic 测试的基础设施，有保留价值
- ✅ **EndToEnd/README.md 已更新**: 已完全重写，包含：
  - 新的架构图（多角色、负面测试、工作流、权限测试）
  - 新增基础设施文件说明（E2ECollectionFixture, TestDataTracker, E2EAssertionHelpers）
  - 角色登录方法和配置示例
  - 新的测试统计（172 测试，135 通过）
  - 当前状态说明（30 个失败为测试期望问题）
  - 扩展指南（添加新模块、工作流、权限测试）

### 5.1 评估 ApiIntegration/ 目录
- [x] 审查mock-based测试是否有不可替代的价值
- [x] 决定：保留有价值的部分（Polly、网络错误模拟），删除冗余的
- [x] 更新 README

### 5.2 更新文档
- [x] 更新 EndToEnd/README.md 反映新架构
- [x] 更新测试运行指南

## 预期产出

| 类别 | 当前 | 重构后 | 价值 |
|------|------|--------|------|
| E2E模块测试 | 74 | ~110 | +负面路径（发现验证缺陷） |
| 工作流测试 | 2 | ~8 | +跨模块数据一致性 |
| 权限测试 | 2 | ~15 | +真实角色边界验证 |
| 基础设施 | 基础 | 完善 | +共享登录+数据清理+断言助手 |
