# 基准线重置 - Issue/PR审查报告

**文档版本**: v1.0
**创建日期**: 2025-10-21
**基准Commit**: `2a80f4c2`（恢复Step验证逻辑）
**审查范围**: Epic #1494的4个open Issue + 9个open PR
**战略背景**: MVP目标是"可以看诊"，4步MedicalCaseFlowView框架已确定，需清理过期Issue/PR

---

## 📋 审查概述

### 核心发现

1. **4个open Issue**：
   - ✅ **1个已在master解决**（#1539，"开始接诊"导航问题）
   - ⏳ **2个需保留**（#1538验证任务、#1502草稿保存功能）
   - 📦 **1个降低优先级**（#1503小屏幕测试，P2优化）

2. **9个open PR**：
   - ❌ **3个应关闭**（基于旧架构或已废弃功能）
   - 📦 **2个提取功能**（草稿保存#1533、处方打印#1422，重新基于master实施）
   - ❌ **3个应关闭**（Epic #1343旧处方编辑器功能，与当前架构冲突）
   - 📦 **1个降低优先级**（#1536测试文档，P2优化）

3. **关键结论**：
   - 9个PR中有**6个需要关闭**（过期或冲突）
   - **2个PR包含MVP必需功能**，需提取到新Issue重新实施
   - **1个PR**可保留但降低优先级（测试文档）

---

## 📊 Issue审查详情

### Issue #1539：修复主页"开始接诊"导航逻辑，删除过期功能

**状态**: OPEN
**优先级**: P0 (priority:high, type:bug)
**创建日期**: 未知（需查询）

**Issue描述**:
- 问题：主页点击"开始接诊"应直接进入MedicalCaseFlowView，但当前弹出PatientSelectionDialog（旧功能）
- 目标：修改HomeViewModel直接导航到MedicalCaseFlowView

**审查结果**: ✅ **已在master解决，应关闭**

**证据**:
- Commit `869987eb`: 添加HomeViewModel错误日志
- Commit `f9eaa9d2`: 注册MedicalCaseFlowViewModel到DI容器
- Commit `b63f2d34`: 修改MedicalCaseModule为WhenAvailable加载模式
- 用户确认："\"开始接诊\"加载成功。"
- 当前master的HomeViewModel.ExecuteStartConsultation()直接导航到"MedicalCaseFlowView"（Line 101）

**建议操作**: **关闭Issue #1539**

**关闭理由**:
```
Issue已在master分支解决（commit 869987eb + f9eaa9d2 + b63f2d34）。
当前"开始看诊"按钮已正常导航到MedicalCaseFlowView（4步流程），
用户确认功能正常。基准线重置，关闭过期Issue。
```

---

### Issue #1538：阶段1收尾 - 验证4步医案流程UI交互

**状态**: OPEN
**优先级**: P0 (priority:high, type:task)
**创建日期**: 未知

**Issue描述**:
- 验证4步流程的UI交互是否正常（Step 3 → Step 4导航、前一步/后一步、取消/保存草稿）
- 已知技术债务记录在文档中，阶段2修复
- 目标："先让软件跑起来"

**审查结果**: ⏳ **保留，需人工测试验证**

**理由**:
- 从baseline-assessment报告看，4步流程框架已100%实现
- 基本交互功能已完成（导航、状态机、验证）
- 但还有9个功能缺口（P1-1到P1-6，P2-1到P2-3）
- 此Issue的验证目标与当前MVP基准线一致（验证UI可以跑起来）

**建议操作**: **保留Issue #1538，等待人工测试**

**更新建议**:
- 在Issue中添加评论：基于新的基准线（commit 2a80f4c2），4步框架已确认可用
- 列出已完成功能：导航、状态机、Step 1验证
- 列出待验证功能：Step 2-4交互、前一步/后一步、保存草稿（已知技术债务）
- 明确验收标准：可以完整走完4步流程（数据丢失可接受，阶段2修复）

---

### Issue #1503：小屏幕兼容性测试（1366x768 + 1280x720）

**状态**: OPEN
**优先级**: P2 (priority:medium, type:task, test)
**创建日期**: 未知

**Issue描述**:
- 测试任务：验证UI在小屏幕分辨率下的显示效果
- 已有评论（2025-10-20）说明测试准备已完成，等待人工执行
- 已识别3个潜在问题（P1-1, P1-2, P1-3）

**审查结果**: 📦 **保留但降低优先级（P2优化任务）**

**理由**:
- 从baseline-assessment报告看，这不是MVP核心功能（P1）
- 属于P2优化类任务（UI优化）
- 测试准备工作已完成（PR #1536），可保留
- 应在MVP核心功能完成后再执行

**建议操作**: **保留Issue #1503，降低优先级到P2**

**更新建议**:
- 在Issue中添加评论：基于MVP优先级调整，此任务为P2优化类
- 建议在P1功能缺口补齐后（18小时工作）再执行测试
- 关联PR #1536可保留，但不急于合并

---

### Issue #1502：自动保存草稿功能（DispatcherTimer + LocalStorage）

**状态**: OPEN
**优先级**: P1 (priority:medium, type:task)
**创建日期**: 未知

**Issue描述**:
- 实现定时自动保存、草稿恢复、启动时恢复
- 工作量估算4-6小时

**审查结果**: ✅ **保留，这是MVP必需的P1功能**

**理由**:
- 从baseline-assessment报告的P1-1缺口看，这正是我们识别出的功能缺口之一
- 此Issue的描述与P1-1完全对应（草稿保存逻辑，3小时）
- 已有PR #1533实现了完整功能，但需基于当前master重新审查

**建议操作**: **保留Issue #1502，作为P1-1缺口的实施Issue**

**更新建议**:
- 关联PR #1533包含完整实现（FlowDraftState + LocalStorageService + DispatcherTimer）
- 建议关闭PR #1533，基于当前master（commit 2a80f4c2）重新实施
- 功能范围：定时自动保存（5分钟）、启动时恢复、完成后清除草稿
- 验收标准：参考PR #1533的实现，但简化MVP版本（跳过RestoreDraftDialog，直接自动恢复）

---

## 📊 PR审查详情

### PR #1536：创建Issue #1503小屏幕兼容性测试文档

**状态**: OPEN
**创建日期**: 2025-10-20
**Branch**: `docs/1503-ui-test-preparation`

**PR描述**:
- 为Issue #1503创建测试准备文档
- 包含测试清单和报告模板（共1269行）
- 审查了5个XAML文件，发现3个P1级潜在问题

**审查结果**: 📦 **保留但降低优先级（P2优化任务）**

**理由**:
- 这是一个文档PR，准备测试环境
- 关联Issue #1503是P2优化任务（小屏幕兼容性）
- 文档本身质量高，可保留
- 但不应在MVP核心功能完成前合并

**建议操作**: **保留PR #1536，标记为P2优先级**

**合并条件**:
- 等待P1功能缺口补齐完成（18小时工作）
- 与Issue #1503同步处理

---

### PR #1535：修复PatientSelectionDialog资源引用错误

**状态**: OPEN
**创建日期**: 2025-10-20
**Branch**: `fix/1534-resource-reference-error`

**PR描述**:
- 修复 `AlternateSurfaceBrush` → `AlternateRowBrush` 资源引用错误
- P0级阻塞问题
- 影响文件：`PatientSelectionDialog.xaml`

**审查结果**: ❌ **应关闭（修复已废弃功能）**

**关键证据**:
1. **PatientSelectionDialog是过期功能**：
   - 从baseline-assessment的架构污染分析看，这是旧的弹出对话框方式
   - 新功能是MedicalCaseFlowView的PatientSelectionView（Step 1内嵌）
   - 位于不同模块：旧的在LYBT.Desktop.Patients，新的在LYBT.Desktop.MedicalCase

2. **用户已确认新功能正常**：
   - Issue #1539描述：主页"开始接诊"应直接进入MedicalCaseFlowView，不再弹出PatientSelectionDialog
   - 用户确认："\"开始接诊\"加载成功。"
   - 当前master已实现正确的导航逻辑

3. **修复无意义**：
   - 修复一个即将删除的旧功能的资源引用错误，没有价值
   - baseline-assessment已将PatientSelectionDialog列为待删除的架构污染代码（P2-1组）

**建议操作**: **关闭PR #1535**

**关闭理由**:
```
此PR修复的PatientSelectionDialog是已废弃的旧功能（Issue #1539已确认）。
新的4步流程使用内嵌的PatientSelectionView（Step 1），不再需要弹出对话框。
PatientSelectionDialog已列入架构污染代码清理清单（P2-1组）。
基准线重置，关闭过期PR。
```

---

### PR #1533：实现自动保存草稿功能（Issue #1502）

**状态**: OPEN
**创建日期**: 2025-10-20
**Branch**: `feature/1502-auto-save-draft`

**PR描述**:
- 实现FlowDraftState、ILocalStorageService、LocalStorageService
- 集成DispatcherTimer自动保存（5分钟间隔）
- 草稿恢复逻辑（暂时跳过RestoreDraftDialog，直接自动恢复）
- 新增3个文件，修改2个文件，编译通过

**审查结果**: 📦 **提取功能到新Issue，关闭当前PR**

**理由**:
1. **功能本身是MVP必需**：
   - 对应baseline-assessment的P1-1缺口（草稿保存逻辑，3小时）
   - Issue #1502是保留的P1任务

2. **PR基于旧基准线**：
   - PR创建于2025-10-20，可能基于旧的MedicalCaseFlowViewModel实现
   - 当前master（commit 2a80f4c2）已有大量变更
   - 需要基于新基准线重新审查和实施

3. **代码质量**：
   - PR显示MVP简化（跳过RestoreDraftDialog），符合当前策略
   - 但需要验证与当前master的MedicalCaseFlowViewModel的兼容性

**建议操作**: **关闭PR #1533，功能提取到Issue #1502**

**提取功能清单**:
- ✅ FlowDraftState数据传输类（CurrentStep、PatientId、MedicalCaseId、Consultation、Prescription、SavedAt）
- ✅ ILocalStorageService接口和LocalStorageService实现
- ✅ MedicalCaseFlowViewModel集成DispatcherTimer（5分钟间隔）
- ✅ 手动保存草稿（SaveDraftAsync）和自动保存（AutoSaveTickAsync）
- ✅ 草稿恢复逻辑（RestoreDraftAsync，MVP简化版）
- ✅ 完成医案后自动清除草稿
- ✅ MedicalCaseModule注册ILocalStorageService服务

**重新实施建议**:
- 基于当前master（commit 2a80f4c2）
- 参考PR #1533的实现，但重新验证与当前MedicalCaseFlowViewModel的集成点
- 重新测试编译和基本功能

**关闭理由**:
```
PR包含MVP必需的P1功能（草稿保存），但基于旧基准线。
基准线重置，需基于新的master（commit 2a80f4c2）重新实施。
功能已提取到Issue #1502，将作为P1-1缺口的实施任务。
关闭当前PR。
```

---

### PR #1530：导航与Shell框架实现（Issue #1485）

**状态**: OPEN
**创建日期**: 2025-10-20
**Branch**: `feature/1485-navigation-shell`

**PR描述**:
- 实现左侧菜单导航（6个菜单按钮）
- MainWindowViewModel实现6个导航Command
- 基于Prism Region导航
- 关联Epic #1483: UI/UX交互优化方案

**审查结果**: ❌ **应关闭（非MVP核心功能）**

**理由**:
1. **不符合MVP战略目标**：
   - 用户明确："MVP的目标是可以看诊。为了实现看诊UI反反复复已经多次。目前看诊UI框架已经确定。"
   - MVP重点是MedicalCaseFlowView的4步流程完善，不是导航菜单优化

2. **Epic #1483不是当前关注点**：
   - Epic #1483是"UI/UX交互优化方案"
   - 当前基准线重置的目标是聚焦MVP核心功能（Epic #1494 + Epic #1343）
   - 导航菜单是UI优化，属于MVP后的改进

3. **可能与当前架构冲突**：
   - PR基于旧的HomeView设计
   - 当前master的HomeViewModel已调整（Shell模块）

**建议操作**: **关闭PR #1530**

**关闭理由**:
```
此PR实现的导航菜单优化不属于MVP核心功能（Epic #1483）。
MVP战略目标是"可以看诊"，重点是MedicalCaseFlowView 4步流程完善。
基准线重置，暂停非MVP功能开发。
建议在MVP完成后，根据用户反馈重新评估导航优化需求。
```

---

### PR #1517：迁移ClinicalHomeView到MedicalCase模块并修复导航架构 (#1514)

**状态**: OPEN
**创建日期**: 2025-10-20
**Branch**: `feature/1514-migrate-clinicalhomeview`

**PR描述**:
- HomeViewModel → ClinicalHomeViewModel（迁移到MedicalCase模块）
- 修复LoginViewModel双重导航问题（应用SRP原则）
- 用户确认："现在可以看到主页了。"
- 创建ADR-003文档记录架构决策
- 关联Epic #1513: Workstation架构重构

**审查结果**: ❌ **应关闭（架构调整与当前master冲突）**

**理由**:
1. **当前master的HomeViewModel已工作正常**：
   - 用户确认"\"开始接诊\"加载成功。"
   - HomeViewModel（Shell模块）已正确导航到MedicalCaseFlowView
   - 无需迁移到MedicalCase模块

2. **Epic #1513不是当前MVP关注点**：
   - Epic #1513是"Workstation架构重构"
   - 当前MVP目标是"可以看诊"，不是架构重构

3. **架构调整可能引入风险**：
   - 迁移HomeViewModel到MedicalCase模块会改变模块依赖关系
   - 当前Shell → MedicalCase的导航流程已稳定
   - 不应在MVP阶段进行大规模架构调整

**建议操作**: **关闭PR #1517**

**关闭理由**:
```
此PR的架构调整（HomeView迁移到MedicalCase模块）不属于MVP核心功能。
当前master的HomeViewModel（Shell模块）已正常工作，用户确认"开始接诊"加载成功。
Epic #1513 Workstation架构重构不是当前MVP关注点。
基准线重置，暂停架构调整，聚焦MVP功能完善。
```

---

### PR #1422：实现处方打印功能 (PRINT-2/3/4)

**状态**: OPEN
**创建日期**: 2025-10-17
**Branch**: `feature/print-2-flowdocument-builder`

**PR描述**:
- 实现PrescriptionFlowDocumentBuilder（432行）
- 实现PrescriptionPrintService（368行）
- 集成到ViewModel并注册DI
- 总代码量约900行
- 关联Issue #1379, #1380, #1381

**审查结果**: 📦 **提取功能到新Issue，关闭当前PR**

**理由**:
1. **功能本身是MVP必需**：
   - 对应baseline-assessment的P1-6缺口（处方打印功能，4小时）
   - 处方打印是医疗系统的核心功能

2. **PR基于旧基准线**：
   - PR创建于2025-10-17，早于基准线重置
   - 可能基于旧的PrescriptionComposerViewModel实现
   - 需要确认是否与当前master的PrescriptionEditorViewModel兼容

3. **代码质量**：
   - PR显示完整的FlowDocument实现，技术栈合理（WPF原生API）
   - 但需要验证与当前master的处方编辑器的集成

**建议操作**: **关闭PR #1422，功能提取到新Issue**

**提取功能清单**:
- ✅ PrescriptionPrintDto打印数据模型
- ✅ PrescriptionFlowDocumentBuilder（Builder模式，7个fluent方法）
- ✅ PrescriptionPrintService（实现IPrescriptionPrintService接口）
- ✅ ViewModel集成（PrintPreviewCommand）
- ✅ DI注册（PrescriptionsModule）

**重新实施建议**:
- 基于当前master（commit 2a80f4c2）
- 确认目标ViewModel：PrescriptionEditorViewModel（MedicalCaseFlowView Step 3）
- 参考PR #1422的FlowDocumentBuilder实现
- 重新集成到PrescriptionEditorViewModel

**关闭理由**:
```
PR包含MVP必需的P1功能（处方打印），但基于旧基准线。
需要确认与当前master的PrescriptionEditorViewModel兼容性。
基准线重置，功能提取到新Issue重新实施。
关闭当前PR。
```

---

### PR #1421：实现8列表格录入功能 (ENTRY-1到ENTRY-6)

**状态**: OPEN
**创建日期**: 2025-10-17
**Branch**: `feature/entry-1-to-6-prescription-table-editing`

**PR描述**:
- 实现PrescriptionItemRow模型
- 实现Items→ItemRows转换逻辑
- 8列DataGrid布局、ComboBox拼音码过滤、焦点自动跳转
- 关联Epic #1343: 中医处方系统MVP功能实现
- 目标文件：PrescriptionComposerView

**审查结果**: ❌ **应关闭（与当前架构冲突）**

**理由**:
1. **目标文件与当前master不一致**：
   - PR修改的是PrescriptionComposerView（旧的处方编辑器）
   - 当前master使用PrescriptionEditorView（MedicalCaseFlowView Step 3）
   - 两者功能重叠，可能导致架构混乱

2. **Epic #1343的旧实施路径**：
   - Epic #1343是"中医处方系统MVP功能实现"
   - 但PR基于旧的PrescriptionComposer设计
   - 当前基准线已采用MedicalCaseFlowView的4步流程设计

3. **功能可能已在PrescriptionEditorView实现**：
   - 从baseline-assessment看，PrescriptionEditorView已实现基本的表格编辑功能
   - 需要确认是否有缺失的功能（如拼音码过滤、焦点跳转）

**建议操作**: **关闭PR #1421**

**确认需求**:
- 检查PrescriptionEditorView是否已包含拼音码过滤和焦点跳转功能
- 如缺失，创建新Issue基于PrescriptionEditorView实现

**关闭理由**:
```
此PR基于旧的PrescriptionComposerView实现，与当前master的架构不一致。
当前基准线使用PrescriptionEditorView（MedicalCaseFlowView Step 3）作为处方编辑器。
Epic #1343的实施路径已调整为4步流程设计。
基准线重置，关闭基于旧架构的PR。
如确认功能缺失，将基于PrescriptionEditorView创建新Issue。
```

---

### PR #1420：实现处方历史查询和复制功能 (ENTRY-12 to ENTRY-15)

**状态**: OPEN
**创建日期**: 2025-10-17
**Branch**: `feature/entry-12-to-15-prescription-search-and-clone`

**PR描述**:
- 实现患者历史处方查询、全局处方查询、克隆处方
- 创建PrescriptionSearchDialog
- 新增3个API端点
- 总代码量约1200行
- 关联Epic #1343
- 目标文件：PrescriptionComposerViewModel

**审查结果**: ❌ **应关闭（与当前架构冲突）**

**理由**:
1. **目标文件与当前master不一致**：
   - PR修改的是PrescriptionComposerViewModel（旧的处方编辑器）
   - 当前master使用PrescriptionEditorViewModel（MedicalCaseFlowView Step 3）

2. **功能价值**：
   - 历史查询和复制功能本身有价值
   - 但需要基于正确的架构实施（PrescriptionEditorViewModel）

**建议操作**: **关闭PR #1420**

**功能提取**:
- 如果确认历史查询功能是MVP必需，可以提取Server端API实现（3个端点）
- Client端集成需要基于PrescriptionEditorViewModel重新实施

**关闭理由**:
```
此PR基于旧的PrescriptionComposerViewModel实现，与当前master的架构不一致。
当前基准线使用PrescriptionEditorViewModel（MedicalCaseFlowView Step 3）。
基准线重置，关闭基于旧架构的PR。
如确认历史查询功能是MVP必需，将基于PrescriptionEditorViewModel创建新Issue。
```

---

### PR #1419：实现验方导入到处方功能 (ENTRY-7 to ENTRY-10)

**状态**: OPEN
**创建日期**: 2025-10-17
**Branch**: `feature/entry-7-to-10-formula-import`

**PR描述**:
- 实现验方导入到处方功能
- 增强服务层ImportFormulaIntoPrescriptionAsync方法
- 客户端完整集成（API + Repository + ViewModel）
- 总代码量约205行
- 关联Epic #1343
- 目标文件：PrescriptionComposerViewModel

**审查结果**: ❌ **应关闭（与当前架构冲突）**

**理由**:
1. **目标文件与当前master不一致**：
   - PR修改的是PrescriptionComposerViewModel（旧的处方编辑器）
   - 当前master使用PrescriptionEditorViewModel（MedicalCaseFlowView Step 3）

2. **功能价值**：
   - 验方导入功能本身有价值
   - Server端实现（ImportFormulaIntoPrescriptionAsync）可以保留
   - Client端集成需要基于PrescriptionEditorViewModel重新实施

**建议操作**: **关闭PR #1419**

**功能提取**:
- Server端的ImportFormulaIntoPrescriptionAsync实现可以提取
- Client端需要基于PrescriptionEditorViewModel重新实施

**关闭理由**:
```
此PR基于旧的PrescriptionComposerViewModel实现，与当前master的架构不一致。
当前基准线使用PrescriptionEditorViewModel（MedicalCaseFlowView Step 3）。
基准线重置，关闭基于旧架构的PR。
Server端的验方导入服务实现有价值，可提取到新Issue重新集成。
```

---

## 📋 决策汇总表

| ID | 类型 | 标题 | 决策 | 理由 | 关联操作 |
|----|------|------|------|------|---------|
| #1539 | Issue | 修复主页"开始接诊"导航逻辑 | ❌ 关闭 | 已在master解决（commit 869987eb + f9eaa9d2 + b63f2d34） | 无 |
| #1538 | Issue | 阶段1收尾 - 验证4步医案流程UI交互 | ⏳ 保留 | MVP核心验证任务，需人工测试 | 更新Issue描述 |
| #1503 | Issue | 小屏幕兼容性测试 | 📦 降低优先级 | P2优化任务，非MVP核心 | 标记为P2 |
| #1502 | Issue | 自动保存草稿功能 | ✅ 保留 | MVP必需的P1功能（对应P1-1缺口） | 关联PR #1533功能 |
| #1536 | PR | 创建Issue #1503小屏幕兼容性测试文档 | 📦 降低优先级 | P2优化任务，文档质量高可保留 | 与#1503同步 |
| #1535 | PR | 修复PatientSelectionDialog资源引用错误 | ❌ 关闭 | 修复已废弃的旧功能 | 无 |
| #1533 | PR | 实现自动保存草稿功能 | 📦 提取功能 | MVP必需，但基于旧基准线 | 提取到#1502 |
| #1530 | PR | 导航与Shell框架实现 | ❌ 关闭 | 非MVP核心功能（Epic #1483） | 无 |
| #1517 | PR | 迁移ClinicalHomeView到MedicalCase模块 | ❌ 关闭 | 架构调整与当前master冲突 | 无 |
| #1422 | PR | 实现处方打印功能 | 📦 提取功能 | MVP必需，但基于旧基准线 | 创建新Issue（P1-6） |
| #1421 | PR | 实现8列表格录入功能 | ❌ 关闭 | 基于旧的PrescriptionComposerView | 确认功能缺失后创建新Issue |
| #1420 | PR | 实现处方历史查询和复制功能 | ❌ 关闭 | 基于旧的PrescriptionComposerViewModel | 确认功能必需后创建新Issue |
| #1419 | PR | 实现验方导入到处方功能 | ❌ 关闭 | 基于旧的PrescriptionComposerViewModel | 提取Server端实现 |

---

## 🎯 执行计划

### 阶段2.4：执行关闭操作（预计20分钟）

#### 立即关闭（6个Issue/PR）

**关闭Issue**:
- [ ] #1539 - 修复主页"开始接诊"导航逻辑

**关闭PR**:
- [ ] #1535 - 修复PatientSelectionDialog资源引用错误
- [ ] #1530 - 导航与Shell框架实现
- [ ] #1517 - 迁移ClinicalHomeView到MedicalCase模块
- [ ] #1421 - 实现8列表格录入功能
- [ ] #1420 - 实现处方历史查询和复制功能
- [ ] #1419 - 实现验方导入到处方功能

**关闭命令模板**:
```bash
# Issue #1539
gh issue close 1539 --comment "Issue已在master分支解决（commit 869987eb + f9eaa9d2 + b63f2d34）。当前\"开始看诊\"按钮已正常导航到MedicalCaseFlowView（4步流程），用户确认功能正常。基准线重置，关闭过期Issue。"

# PR #1535
gh pr close 1535 --comment "此PR修复的PatientSelectionDialog是已废弃的旧功能（Issue #1539已确认）。新的4步流程使用内嵌的PatientSelectionView（Step 1），不再需要弹出对话框。PatientSelectionDialog已列入架构污染代码清理清单（P2-1组）。基准线重置，关闭过期PR。"

# PR #1530
gh pr close 1530 --comment "此PR实现的导航菜单优化不属于MVP核心功能（Epic #1483）。MVP战略目标是\"可以看诊\"，重点是MedicalCaseFlowView 4步流程完善。基准线重置，暂停非MVP功能开发。建议在MVP完成后，根据用户反馈重新评估导航优化需求。"

# PR #1517
gh pr close 1517 --comment "此PR的架构调整（HomeView迁移到MedicalCase模块）不属于MVP核心功能。当前master的HomeViewModel（Shell模块）已正常工作，用户确认\"开始接诊\"加载成功。Epic #1513 Workstation架构重构不是当前MVP关注点。基准线重置，暂停架构调整，聚焦MVP功能完善。"

# PR #1421
gh pr close 1421 --comment "此PR基于旧的PrescriptionComposerView实现，与当前master的架构不一致。当前基准线使用PrescriptionEditorView（MedicalCaseFlowView Step 3）作为处方编辑器。Epic #1343的实施路径已调整为4步流程设计。基准线重置，关闭基于旧架构的PR。如确认功能缺失，将基于PrescriptionEditorView创建新Issue。"

# PR #1420
gh pr close 1420 --comment "此PR基于旧的PrescriptionComposerViewModel实现，与当前master的架构不一致。当前基准线使用PrescriptionEditorViewModel（MedicalCaseFlowView Step 3）。基准线重置，关闭基于旧架构的PR。如确认历史查询功能是MVP必需，将基于PrescriptionEditorViewModel创建新Issue。"

# PR #1419
gh pr close 1419 --comment "此PR基于旧的PrescriptionComposerViewModel实现，与当前master的架构不一致。当前基准线使用PrescriptionEditorViewModel（MedicalCaseFlowView Step 3）。基准线重置，关闭基于旧架构的PR。Server端的验方导入服务实现有价值，可提取到新Issue重新集成。"
```

#### 保留但更新（3个Issue/PR）

**更新Issue**:
- [ ] #1538 - 添加评论：基于新基准线（commit 2a80f4c2），4步框架已确认可用
- [ ] #1503 - 标记为P2优先级，添加评论：建议在P1功能缺口补齐后执行
- [ ] #1502 - 添加评论：关联PR #1533包含完整实现，需基于当前master重新实施

**更新PR**:
- [ ] #1536 - 标记为P2优先级，添加评论：与Issue #1503同步处理

**更新命令模板**:
```bash
# Issue #1538
gh issue comment 1538 --body "**基准线重置更新**：基于新的基准线（commit 2a80f4c2），4步MedicalCaseFlowView框架已确认可用。

**已完成功能**：
- ✅ 导航功能正常（\"开始接诊\"按钮）
- ✅ 状态机实现完整
- ✅ Step 1验证已恢复

**待验证功能**（需人工测试）：
- [ ] Step 2-4交互流程
- [ ] 前一步/后一步按钮
- [ ] 保存草稿功能（已知技术债务，阶段2修复）

**验收标准**：可以完整走完4步流程（数据丢失可接受，阶段2修复）"

# Issue #1503
gh issue comment 1503 --body "**基准线重置更新**：基于MVP优先级调整，此任务为P2优化类。

**执行时机**：建议在P1功能缺口补齐后（预计18小时工作）再执行测试。

**关联PR**：#1536包含完整的测试准备文档，可保留但不急于合并。"

# Issue #1502
gh issue comment 1502 --body "**基准线重置更新**：此Issue对应baseline-assessment报告的P1-1缺口（草稿保存逻辑，3小时）。

**关联PR**：#1533包含完整实现（FlowDraftState + LocalStorageService + DispatcherTimer），但基于旧基准线。

**建议**：关闭PR #1533，基于当前master（commit 2a80f4c2）重新实施。

**功能范围**：
- 定时自动保存（5分钟）
- 启动时恢复草稿
- 完成医案后清除草稿
- MVP简化版（跳过RestoreDraftDialog，直接自动恢复）"

# PR #1536
gh pr comment 1536 --body "**基准线重置更新**：此PR关联的Issue #1503是P2优化任务（小屏幕兼容性）。

**建议**：保留PR但降低优先级，与Issue #1503同步处理。

**合并条件**：等待P1功能缺口补齐完成（18小时工作）后再考虑合并。"
```

#### 提取功能创建新Issue（2个PR）

**PR #1533 → 新Issue（P1-1草稿保存）**:
- 已有Issue #1502，无需创建新Issue
- 功能已在Issue #1502中记录

**PR #1422 → 新Issue（P1-6处方打印）**:
- 需要创建新Issue
- 参考baseline-assessment的P1-6缺口定义

**新Issue创建命令**:
```bash
gh issue create --title "[P1-6] 实现处方打印功能（基于PrescriptionEditorView）" \
  --label "type:feature,priority:high,epic:1494,module:prescriptions" \
  --body "## 📋 关联Epic
- Epic: #1494 医案流程UI重构（4步流程）
- 基准线Commit: 2a80f4c2
- 对应P1缺口: baseline-assessment-2025-10-21.md的P1-6

## 📝 功能描述
实现处方打印功能，集成到MedicalCaseFlowView的Step 3（PrescriptionEditorView）。

## 🎯 功能范围
- FlowDocumentBuilder（参考PR #1422实现）
- PrescriptionPrintService（实现IPrescriptionPrintService接口）
- 集成到PrescriptionEditorViewModel
- 打印预览、实际打印、导出XPS（MVP阶段）

## ✅ 验收标准
- [ ] FlowDocumentBuilder支持7个构建方法（AddHeader、AddPatientInfo、AddFourDiagnostics、AddPrescriptionTable、AddUsageInstructions、AddPriceInfo、AddSignature）
- [ ] PrescriptionPrintService实现所有接口方法
- [ ] DI注册正确，服务可被注入
- [ ] PrescriptionEditorViewModel添加PrintPreviewCommand
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 功能测试通过（可打印预览和实际打印）

## 📚 参考资料
- PR #1422包含完整实现（约900行代码）
- FlowDocumentBuilder.cs（432行）
- PrescriptionPrintService.cs（368行）

## ⏱️ 工作量估算
4小时（参考baseline-assessment的P1-6估算）

## 🔗 依赖任务
- Depends on: 当前master（commit 2a80f4c2）稳定运行"
```

---

## 📊 汇总统计

### Issue统计
- **总数**: 4个
- **关闭**: 1个（#1539）
- **保留**: 3个（#1538, #1503, #1502）
  - P1优先级: 2个（#1538, #1502）
  - P2优先级: 1个（#1503）

### PR统计
- **总数**: 9个
- **关闭**: 6个（#1535, #1530, #1517, #1421, #1420, #1419）
- **保留**: 1个（#1536，P2优先级）
- **提取功能**: 2个（#1533 → #1502, #1422 → 新Issue）

### 功能提取统计
- **草稿保存功能**（PR #1533）：提取到Issue #1502
- **处方打印功能**（PR #1422）：创建新Issue（P1-6）

### 时间节约
- **关闭6个PR**：避免合并冲突和后续维护成本
- **提取2个PR功能**：保留有价值的实现，减少重复工作
- **保留3个Issue**：聚焦MVP核心功能（#1538, #1502）+ P2优化（#1503）

---

## 🚀 下一步工作

### 立即执行（阶段2.4）
- [ ] 执行7个关闭命令（1个Issue + 6个PR）
- [ ] 执行4个更新命令（3个Issue + 1个PR）
- [ ] 创建1个新Issue（P1-6处方打印）
- [ ] 生成基准线重置归档文档（`baseline-reset-archive-2025-10-21.md`）

### 后续阶段（阶段3）
- [ ] 根据baseline-assessment报告创建新Issue清单（9个P1功能缺口 + 3个P2优化）
- [ ] 创建或更新Epic #1494
- [ ] 开始P1功能缺口补齐工作（预计18小时）

---

## 📝 备注

**基准线重置原则**：
- ✅ 所有决策基于当前master（commit 2a80f4c2）
- ✅ 聚焦MVP核心目标（"可以看诊"）
- ✅ 4步MedicalCaseFlowView框架是唯一事实标准
- ✅ 过期代码/架构调整暂停，避免架构污染
- ✅ 保留有价值的功能实现，重新基于新基准线实施

**文档生成时间**: 2025-10-21
**审查人**: Claude Code
**批准人**: 待用户确认
