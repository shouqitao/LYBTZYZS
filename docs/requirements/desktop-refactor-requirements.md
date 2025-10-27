# Desktop层架构重构需求文档

**文档版本**：v1.0
**创建时间**：2025-10-27
**需求来源**：Desktop层架构分析报告（docs/reports/desktop-refactor-analysis-2025-10-27.md）
**对应Epic**：待创建
**优先级**：高（参照Server端Epic #1600重构成功经验）

---

## 📋 需求概述

### 业务背景

基于Server端重构（Epic #1600）成功完成，Desktop层存在**代码膨胀**、**Server-Client功能不匹配**和**技术债积累**三大问题，需要进行系统性架构重构，提升代码质量和可维护性。

### 核心问题

1. **代码膨胀严重**：39个View中，头部3模块（MedicalCase, Prescriptions, Users）占59%
2. **功能匹配失衡**：Prescriptions模块Desktop端8个View vs Server端4个端点（比率2.00）
3. **技术债积累**：36个TODO注释分布在15个ViewModel
4. **临时方案遗留**：MedicalCaseQueryService标记"Phase 2临时实现"

### 业务目标

- **提升架构质量**：Desktop-Server模块功能匹配度提升
- **降低维护成本**：View数量优化23%，TODO注释减少44%
- **清理技术债**：删除临时方案，实现专用API
- **完善文档体系**：代码与文档100%同步

---

## 🎯 功能需求

### FR-1: 代码膨胀治理

**需求描述**：
对MedicalCase和Prescriptions模块进行View合并评估，减少冗余界面，提升代码复用率。

**用户故事**：
作为开发者，我希望Desktop层View数量与Server端点数量保持合理比例，避免过度复杂化，降低维护成本。

**验收标准**：
- [ ] 完成PrescriptionsMainView与PrescriptionManagementView功能交集分析
- [ ] 完成PrescriptionView与Management Detail功能重复性评估
- [ ] 输出合并建议清单（含影响评估）
- [ ] View数量优化：39 → ~30（减少23%）
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证通过（功能完整可用）

**优先级**：🔴 P0（最高）

**相关模块**：
- `LYBT.Desktop.Prescriptions`（8 Views → 6-7 Views）
- `LYBT.Desktop.MedicalCase`（8 Views，评估优化空间）

**技术约束**：
- 不能破坏现有用户操作流程
- 合并后界面需通过Tab或其他方式保持功能可访问性

---

### FR-2: 通用组件提取

**需求描述**：
提取通用Dialog组件，替换模块专用Dialog，提升代码复用率。

**用户故事**：
作为开发者，我希望删除确认类Dialog能够复用全局组件，而不是每个模块都实现专用版本，减少重复代码。

**验收标准**：
- [ ] PrescriptionDeleteConfirmDialog替换为全局ConfirmationDialog
- [ ] 搜索并识别其他模块的专用Dialog（如UserDeleteConfirmDialog等）
- [ ] 逐个替换专用Dialog为全局组件
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证Dialog功能正常（软删除/物理删除选项正确）

**优先级**：🟡 P1（中）

**相关模块**：
- `LYBT.Desktop.Prescriptions.Views.PrescriptionDeleteConfirmDialog`（删除）
- `LYBT.Desktop.Shell.Dialogs.ConfirmationDialog`（复用）
- 其他模块潜在专用Dialog

**预期改进**：
- View数量减少：1-2个
- 通用组件复用率提升

---

### FR-3: 技术债清理

**需求描述**：
清理36个TODO注释标记的未完成功能，优先处理核心业务流程（患者导入、诊断记录、病案完成）。

**用户故事**：
作为开发者，我希望清理积压的TODO标记，明确哪些是真正需要实现的功能，哪些是过时的计划，提升代码清晰度。

**验收标准**：
- [ ] PatientImportWizardViewModel TODO清理完成（6 → 0）
- [ ] MedicalCaseConsultationViewModel TODO清理完成（5 → 0）
- [ ] CompletionViewModel TODO清理完成（5 → 0）
- [ ] 其他ViewModel TODO评估完成（20 → <10）
- [ ] 总TODO数量：36 → <20（减少44%）
- [ ] 对于保留的TODO，添加明确的Issue引用
- [ ] 编译通过，功能验证通过

**优先级**：🟡 P1（中）

**相关模块**：
- `LYBT.Desktop.Patients.ViewModels.PatientImportWizardViewModel`（6个TODO）
- `LYBT.Desktop.MedicalCase.ViewModels.MedicalCaseConsultationViewModel`（5个TODO）
- `LYBT.Desktop.MedicalCase.ViewModels.CompletionViewModel`（5个TODO）
- 其他12个ViewModel（20个TODO）

**技术约束**：
- 每个TODO需逐个评估：快速实现 or 删除标记 or 转化为Issue
- 不能删除真正需要实现的功能计划

---

### FR-4: Services层优化

**需求描述**：
实施Epic #1583 Phase 5计划，删除MedicalCaseQueryService临时方案，改用专用API。

**用户故事**：
作为开发者，我希望Desktop层直接调用语义清晰的Server端API，而不是使用临时方案过滤数据，提升代码可读性和维护性。

**验收标准**：

**Server端**：
- [ ] 实现专用API：`GET /api/v1/medicalcases/patient/{patientId}/unfinished`（查询未完成医案）
- [ ] 实现专用API：`PUT /api/v1/medicalcases/{id}/close`（关闭医案）
- [ ] API单元测试通过
- [ ] API集成测试通过

**Desktop端**：
- [ ] 删除`LYBT.Desktop.MedicalCase.Services.MedicalCaseQueryService.cs`
- [ ] ViewModel改用新API（通过Repository或ApiClient）
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 运行时验证功能正常（查询未完成医案、关闭医案）

**优先级**：🟢 P2（低）

**相关模块**：
- Server端：`LYBT.WebAPI.Controllers.MedicalCaseController`（新增2个端点）
- Server端：`LYBT.Module.MedicalCase.Services.MedicalCaseService`（新增2个方法）
- Desktop端：`LYBT.Desktop.MedicalCase.Services.MedicalCaseQueryService`（删除）
- Desktop端：相关ViewModel（改用新API）

**技术约束**：
- 遵循AR-001业务规则（写操作通过聚合根）
- API版本保持v1（不升级到v2）
- 专用API需符合RESTful规范

---

### FR-5: 文档同步更新

**需求描述**：
更新Desktop架构文档，反映Phase 1-4的改进成果，确保文档与代码100%同步。

**用户故事**：
作为开发者，我希望架构文档能够准确反映当前代码状态，方便新成员快速理解系统架构。

**验收标准**：
- [ ] 更新`docs/architecture/client/README.md`（View数量、Services层、技术债统计）
- [ ] 更新`docs/modules/medicalcase/README.md`（删除MedicalCaseQueryService说明）
- [ ] 更新`docs/modules/prescriptions/README.md`（View合并说明）
- [ ] 更新`docs/index.md`（导航链接检查）
- [ ] 生成Desktop重构总结报告（`docs/reports/desktop-refactor-summary-2025-10-27.md`）
- [ ] 文档交叉引用检查通过（无死链接）

**优先级**：🟢 P2（低）

**相关文档**：
- `docs/architecture/client/README.md`
- `docs/modules/medicalcase/README.md`
- `docs/modules/prescriptions/README.md`
- `docs/index.md`

**输出物**：
- Desktop层架构重构总结报告

---

## 🚫 非功能需求

### NFR-1: 性能要求

**需求描述**：
重构过程不能降低应用性能，合并View后界面加载时间不能增加。

**验收标准**：
- [ ] View加载时间：≤2秒（与重构前持平）
- [ ] Dialog打开时间：≤500ms（与重构前持平）
- [ ] 内存占用：不增加（监控重构前后对比）

---

### NFR-2: 质量标准

**需求描述**：
所有重构必须通过严格的质量验证，参照Server端Epic #1600标准。

**验收标准**（每个Phase强制）：
- [ ] **编译验证**：`dotnet build LYBT.All.sln -c Release --no-restore`（0 errors, 0 warnings）
- [ ] **运行时验证**：启动Desktop应用，测试修改的模块功能
- [ ] **用户视角验证**：从用户操作场景确认功能完整可用
- [ ] **代码审查通过**：符合项目代码规范
- [ ] **文档同步完成**：代码改动同步更新文档

---

### NFR-3: 兼容性要求

**需求描述**：
重构不能破坏现有用户操作习惯和数据兼容性。

**验收标准**：
- [ ] 用户操作流程不变（或通过Tab等方式保持可访问性）
- [ ] 数据库Schema不变（纯Desktop层重构）
- [ ] Server端API契约不变（除非新增专用API）
- [ ] 现有单元测试通过（或根据重构调整测试）

---

### NFR-4: 可维护性提升

**需求描述**：
重构后代码结构更清晰，维护成本降低。

**量化指标**：
- [ ] View数量减少：39 → ~30（-23%）
- [ ] TODO注释减少：36 → <20（-44%）
- [ ] Services层简化：5 → 4（删除临时方案）
- [ ] 通用组件复用率提升：新增1-2个通用Dialog复用场景

---

## 📊 验收标准（总体）

### 量化指标

| 指标 | 当前值 | 目标值 | 改进幅度 | 优先级 |
|-----|-------|-------|---------|-------|
| **View总数** | 39 | ~30 | **-23%** | P0 |
| **TODO注释** | 36 | <20 | **-44%** | P1 |
| **Services层** | 5 | 4 | -1（删除临时方案） | P2 |
| **Prescriptions模块View** | 8 | 6-7 | -1~2 | P0 |
| **通用组件复用** | 低 | 中 | 提升 | P1 |

### 质量标准

每个Phase完成后必须满足：
- ✅ 编译通过：0 errors, 0 warnings
- ✅ 运行时验证通过：功能完整可用
- ✅ 用户视角验证通过：操作流程正常
- ✅ 文档同步完成：100%对齐

---

## 🔄 实施阶段（参照Epic #1600）

### Phase 1: 代码膨胀分析与合并评估（2-3天）

**对应功能需求**：FR-1

**目标**：
- 深度分析View合并可行性
- 输出合并建议清单

**交付物**：
- View合并可行性分析报告
- 功能交集分析表
- 用户体验影响评估

---

### Phase 2: 通用组件提取（1-2天）

**对应功能需求**：FR-2

**目标**：
- 替换专用Dialog为全局组件
- 提升代码复用率

**交付物**：
- 删除PrescriptionDeleteConfirmDialog
- 其他专用Dialog替换清单

---

### Phase 3: 技术债清理（3-5天）

**对应功能需求**：FR-3

**目标**：
- 清理36个TODO注释
- TODO数量减少至<20

**交付物**：
- 头部3个ViewModel TODO清零
- 其他ViewModel TODO评估报告

---

### Phase 4: Services层优化（1-2天）

**对应功能需求**：FR-4

**目标**：
- 实现专用API
- 删除临时方案

**交付物**：
- Server端2个新API
- Desktop端删除MedicalCaseQueryService

---

### Phase 5: 文档同步与验证（1天）

**对应功能需求**：FR-5

**目标**：
- 文档100%同步
- 生成总结报告

**交付物**：
- 更新4个架构文档
- Desktop重构总结报告

---

## 🎯 成功标准

### 最小可验收标准（MVP）

- ✅ View数量减少至≤32（至少减少18%）
- ✅ TODO注释减少至≤24（至少减少33%）
- ✅ Prescriptions模块至少删除1个View
- ✅ 所有改动编译通过（0 errors, 0 warnings）
- ✅ 核心业务流程运行时验证通过

### 理想标准

- ✅ View数量减少至~30（减少23%）
- ✅ TODO注释减少至<20（减少44%）
- ✅ Services层临时方案清理完成
- ✅ 文档100%同步
- ✅ 通用组件复用率提升

---

## 📋 关联Issue（待创建）

### Epic Issue

**标题**：Desktop层架构重构与代码膨胀治理
**描述**：基于分析报告的5个Phase渐进式重构
**预期工作量**：8-13天

### 子Issue（Sequential）

| Issue | 标题 | Phase | 工作量 | 优先级 |
|-------|------|-------|-------|-------|
| #TBD1 | Desktop层View合并可行性分析 | Phase 1 | 2-3天 | P0 |
| #TBD2 | 通用Dialog组件提取与替换 | Phase 2 | 1-2天 | P1 |
| #TBD3 | 技术债清理（36→<20 TODO） | Phase 3 | 3-5天 | P1 |
| #TBD4 | MedicalCaseQueryService优化 | Phase 4 | 1-2天 | P2 |
| #TBD5 | Desktop架构文档同步更新 | Phase 5 | 1天 | P2 |

---

## 🔗 参考资料

### 分析报告
- `docs/reports/desktop-refactor-analysis-2025-10-27.md` - 本需求文档的数据来源

### 架构文档
- `docs/architecture/client/README.md` - Desktop层架构指南（v5.0）
- `docs/architecture/server/README.md` - Server层架构指南

### 成功案例
- Epic #1600 - Server端重构（2025-10-27完成，5个Issues，0破坏性变更）

### 技术约束
- `.spec-workflow/steering/constitution.md` - 项目强制性原则
- `docs/development/client/code-standards.md` - Desktop代码规范

---

## 📝 变更历史

| 版本 | 日期 | 作者 | 变更说明 |
|-----|------|------|---------|
| v1.0 | 2025-10-27 | Claude Code | 初始版本，基于Desktop层架构分析报告 |

---

**下一步行动**：
1. ✅ 需求文档已完成
2. ⏭️ 生成设计文档（使用`lybtzyzs-design-generator` Skill）
3. ⏭️ 生成任务分解（使用`lybtzyzs-task-breakdown` Skill）
4. ⏭️ 批量创建Issues（使用`lybtzyzs-issue-template` Skill）
