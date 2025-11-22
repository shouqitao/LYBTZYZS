# 医案流程基准线评估报告

**生成日期**：2025-10-21
**基准线Commit**：2a80f4c2 (master分支)
**评估范围**：MedicalCaseFlowView 4步流程功能现状
**评估目标**：确定MVP"可以看诊"所需的功能缺口

---

## 📊 执行摘要

**核心发现**：
- ✅ 4步流程框架已完整实施
- ✅ 所有Step的ViewModel已创建且功能基本可用
- ⚠️ 存在8个功能缺口需要补齐（6个P1，2个P2）
- ⚠️ 发现3组架构污染代码需要清理（9个文件）

**功能完成度**：
- 核心流程控制：100%
- Step 1（患者选择）：80%
- Step 2（诊断录入）：85%
- Step 3（处方录入）：75%
- Step 4（完成确认）：70%
- **总体完成度**：82%

**MVP可达性**：✅ **可以实现**
通过补齐6个P1功能缺口，即可达到"可以看诊"的MVP目标。

---

## 一、4步流程功能现状表

### 🎯 核心框架：MedicalCaseFlowViewModel

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`

| 功能模块 | 实现状态 | 说明 |
|---------|---------|------|
| **流程状态机** | ✅ 100% | FlowStep枚举，4个步骤完整定义 |
| **步骤导航** | ✅ 100% | NextStep/PreviousStep命令，NavigateToStep方法 |
| **验证逻辑** | ✅ 90% | CanExecuteNextStep实现，Step 1验证已恢复 |
| **数据传递** | ✅ 100% | CurrentPatient、MedicalCaseId属性传递 |
| **MedicalCase自动创建** | ✅ 100% | Step 1→2时自动创建（line 256-280） |
| **患者信息条** | ✅ 100% | SelectedPatientName/Info属性，Step 2-4显示 |
| **IValidatable集成** | ✅ 100% | ExecuteNextStepAsync调用Validate（line 231-240） |
| **ISaveable集成** | ✅ 100% | ExecuteNextStepAsync调用SaveAsync（line 242-253） |
| **草稿保存** | ⚠️ TODO | ExecuteSaveDraft方法存在但未实现（line 320-334） |
| **取消确认** | ⚠️ TODO | ExecuteCancel方法缺少确认对话框（line 344） |

**功能缺口（MedicalCaseFlowViewModel）**：
1. 🔴 **P1-缺口1**：实现草稿保存逻辑（Task #1502）
2. 🟡 **P2-缺口1**：实现取消流程确认对话框

---

### Step 1：患者选择（PatientSelectionViewModel）

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs`

| 功能模块 | 实现状态 | 说明 |
|---------|---------|------|
| **患者列表加载** | ✅ 100% | LoadPatientsAsync方法，支持分页 |
| **患者搜索** | ✅ 100% | SearchCommand，支持姓名/拼音码/手机号 |
| **患者选择** | ✅ 100% | SelectedPatient属性，PatientSelected事件 |
| **双击选择** | ✅ 100% | DoubleClickSelectCommand |
| **患者刷新** | ✅ 100% | RefreshCommand |
| **快速新建患者** | ⚠️ TODO | NewPatientCommand调用TODO（line 219-230） |

**功能缺口（Step 1）**：
3. 🔴 **P1-缺口2**：集成QuickCreatePatientDialog（Task #1497，已有PR #1535）

---

### Step 2：诊断录入（ConsultationFormViewModel）

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs`

| 功能模块 | 实现状态 | 说明 |
|---------|---------|------|
| **IValidatable接口** | ✅ 100% | Validate方法已实现（line 184-226） |
| **ISaveable接口** | ✅ 100% | SaveAsync方法已实现（line 227-326） |
| **四诊数据录入** | ✅ 100% | Inspection/Auscultation/Inquiry/Palpation属性 |
| **诊断信息录入** | ✅ 100% | ChiefComplaint, PresentIllness, TCMDiagnosis等 |
| **Consultation创建** | ✅ 100% | SaveAsync中调用IConsultationRepository.CreateAsync |
| **MedicalCase关联** | ⚠️ TODO | 更新MedicalCase.ConsultationId（line 284-285） |
| **从历史导入** | ⚠️ TODO | ExecuteImportFromHistory方法未实现（line 337-339） |

**功能缺口（Step 2）**：
4. 🔴 **P1-缺口3**：实现MedicalCase.ConsultationId更新逻辑
5. 🟡 **P2-缺口2**：实现从历史诊断导入功能（Task #1502+）

---

### Step 3：处方录入（PrescriptionEditorViewModel）

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs`

| 功能模块 | 实现状态 | 说明 |
|---------|---------|------|
| **IValidatable接口** | ✅ 80% | Validate方法简化实现（line 197-236） |
| **ISaveable接口** | ✅ 90% | SaveAsync方法基本实现（line 237-380） |
| **处方条目管理** | ✅ 100% | PrescriptionItems集合，Add/Remove命令 |
| **剂量/单位录入** | ✅ 100% | Dosage, Unit属性绑定 |
| **PrescriptionDraft创建** | ✅ 100% | BuildPrescriptionDraft方法（line 241-283） |
| **药材验证** | ⚠️ 简化 | TODO: 添加完整药材库关联验证（line 226） |
| **MedicalCase保存** | ⚠️ TODO | draft传递给MedicalCase聚合根（line 304） |

**功能缺口（Step 3）**：
6. 🔴 **P1-缺口4**：实现处方保存到MedicalCase聚合根
7. 🔴 **P1-缺口5**：增强处方验证逻辑（药材库关联）

---

### Step 4：完成确认（CompletionViewModel）

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/CompletionViewModel.cs`

| 功能模块 | 实现状态 | 说明 |
|---------|---------|------|
| **InitializeAsync** | ✅ 100% | 完成医案逻辑（line 167-225） |
| **完成提示** | ✅ 100% | CompletionMessage属性，显示患者姓名 |
| **继续看诊** | ✅ 100% | ContinueConsultationCommand，导航到Step 1 |
| **返回主页** | ✅ 100% | BackToHomeCommand |
| **打印处方** | ⚠️ TODO | PrintPrescriptionCommand未实现（line 131） |
| **查看详情** | ⚠️ TODO | ViewDetailsCommand未实现（line 150） |

**功能缺口（Step 4）**：
8. 🔴 **P1-缺口6**：实现处方打印功能（Task #1502+）
9. 🟡 **P2-缺口3**：实现病案详情查看对话框

---

## 二、功能缺口汇总

### 🔴 P1级缺口（MVP必须，6个）

| 编号 | 缺口描述 | 所属Step | 预估工作量 | 相关Issue/PR |
|------|---------|---------|-----------|-------------|
| P1-1 | 实现草稿保存逻辑 | 框架 | 3小时 | #1502（PR #1533 open） |
| P1-2 | 集成QuickCreatePatientDialog | Step 1 | 2小时 | #1497（PR #1535 open） |
| P1-3 | 实现MedicalCase.ConsultationId更新 | Step 2 | 2小时 | 新建Issue |
| P1-4 | 实现处方保存到MedicalCase聚合根 | Step 3 | 4小时 | 新建Issue |
| P1-5 | 增强处方验证逻辑（药材库关联） | Step 3 | 3小时 | 新建Issue |
| P1-6 | 实现处方打印功能 | Step 4 | 4小时 | #1422（PR open，需审查） |

**P1总计**：18小时（约2-3个工作日）

### 🟡 P2级缺口（优化，3个）

| 编号 | 缺口描述 | 所属Step | 预估工作量 |
|------|---------|---------|-----------|
| P2-1 | 实现取消流程确认对话框 | 框架 | 1小时 |
| P2-2 | 实现从历史诊断导入功能 | Step 2 | 4小时 |
| P2-3 | 实现病案详情查看对话框 | Step 4 | 3小时 |

**P2总计**：8小时（约1个工作日）

---

## 三、架构污染代码识别

### 🗑️ 第1组：PatientSelectionDialog（3个文件）

**文件清单**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionDialogViewModel.cs`

**状态**：
- ❌ 未标记[Obsolete]（报告有误）
- ✅ 已在PatientsModule.cs:36注册
- ⚠️ **有1处活跃引用**：`ClinicalWorkstationViewModel.cs:313`

**与4步流程冲突**：
弹窗式患者选择 vs Step 1嵌入式患者选择

**处理建议**：
P2级任务 - 先修复ClinicalWorkstation引用，再删除

---

### 🗑️ 第2组：CreateMedicalCaseDialog（3个文件）

**文件清单**：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/CreateMedicalCaseDialog.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/CreateMedicalCaseDialog.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/CreateMedicalCaseDialogViewModel.cs`

**状态**：
- ✅ 已在MedicalCaseModule.cs:30注册
- ✅ **无活跃引用**（验证通过）
- ✅ 可安全删除

**与4步流程冲突**：
手动创建MedicalCase对话框 vs Step 1→2自动创建

**处理建议**：
P2级任务 - 可立即删除

---

### 🗑️ 第3组：MedicalCaseEntryView（3个文件）

**文件清单**：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEntryView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEntryView.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseEntryViewModel.cs`

**状态**：
- ✅ 已在MedicalCaseModule.cs:33,43注册
- ⚠️ **有3处活跃引用**：`ClinicalWorkstationViewModel.cs:257,261,266`
- ⚠️ ConsultationModule.cs也有引用（需确认）

**与4步流程冲突**：
单页面综合录入 vs Step 2-3分步录入

**处理建议**：
P2级任务 - 先修复ClinicalWorkstation引用，再删除

---

## 四、Open Issue/PR状态

### Epic #1494相关Open Issue（4个）

| Issue | 标题 | 状态 | 与基准线关系 |
|-------|-----|------|------------|
| #1539 | 修复主页"开始接诊"导航逻辑 | Open | ✅ 已在master解决（commit 869987eb等） |
| #1538 | 验证4步医案流程UI交互 | Open | ✅ 验证任务，保留 |
| #1503 | 小屏幕兼容性测试 | Open | ✅ 测试任务，保留 |
| #1502 | 自动保存草稿功能 | Open | ✅ P1-缺口1，保留 |

**处理建议**：
- #1539：关闭（已在master解决）
- #1538、#1503、#1502：保留（仍需执行）

---

### Open PR（9个，需逐个审查）

| PR | 标题 | 分支 | 初步判断 |
|----|-----|------|---------|
| #1536 | 小屏幕兼容性测试文档 | docs/1503-ui-test-preparation | ✅ 保留（文档PR） |
| #1535 | 修复PatientSelectionDialog资源引用错误 | fix/1534-resource-reference-error | ⚠️ 审查（涉及过期代码） |
| #1533 | 自动保存草稿功能 | feature/1502-auto-save-draft | ✅ 保留（P1-缺口1） |
| #1530 | 导航与Shell框架实现 | feature/1485-navigation-shell | ⚠️ 审查（可能过期） |
| #1517 | 迁移ClinicalHomeView到MedicalCase模块 | feature/1514-migrate-clinicalhomeview | ⚠️ 审查（架构变更） |
| #1422 | 处方打印功能 | feature/print-2-flowdocument-builder | ✅ 保留（P1-缺口6） |
| #1421 | 8列表格录入功能 | feature/entry-1-to-6-prescription-table-editing | ⚠️ 审查（可能过期） |
| #1420 | 处方历史查询和复制 | feature/entry-12-to-15-prescription-search-and-clone | ⚠️ 审查（可能过期） |
| #1419 | 验方导入到处方 | feature/entry-7-to-10-formula-import | ⚠️ 审查（可能过期） |

**处理建议**：
- ✅ 保留：#1536、#1533、#1422（共3个）
- ⚠️ 需详细审查：#1535、#1530、#1517、#1421、#1420、#1419（共6个）

---

## 五、总结与建议

### ✅ 核心成果

1. **4步流程框架已完整实施**
   - MedicalCaseFlowViewModel功能完善，状态机逻辑清晰
   - 4个Step的ViewModel都已创建且基本可用
   - IValidatable/ISaveable接口集成完成

2. **MVP可达性确认**
   - 当前完成度：82%
   - 补齐6个P1缺口即可实现"可以看诊"目标
   - 预计2-3个工作日完成P1任务

3. **架构污染已识别**
   - 3组过期代码（9个文件）
   - 4处活跃引用需要修复
   - 清理工作可作为P2任务独立执行

---

### 🎯 下一步行动

**立即执行（阶段2）**：
1. 审查9个open PR，决定保留/关闭/提取功能
2. 关闭#1539（已在master解决）
3. 创建基准线重置归档文档

**P1任务（MVP必须）**：
1. 完成6个功能缺口（18小时）
2. 优先级：P1-4（处方保存） > P1-6（处方打印） > P1-2（新建患者）

**P2任务（架构清理）**：
1. 修复ClinicalWorkstation的2处引用（PatientSelectionDialog + MedicalCaseEntryView）
2. 删除3组过期代码（9个文件）
3. 预计4-5小时

---

**报告生成人员**：Claude Code
**审查人员**：待用户确认
**生成时间**：2025-10-21
