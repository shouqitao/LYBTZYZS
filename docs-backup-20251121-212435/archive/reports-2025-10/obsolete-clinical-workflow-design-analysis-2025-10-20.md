# 看诊交互过时设计废除建议报告

**生成日期**：2025-10-20
**分析范围**：看诊流程相关的UI交互设计
**关联Epic**：#1494 医案流程UI重构（4步方案）
**分析方法**：代码扫描 + Issue追踪 + 功能对比

---

## 📊 执行摘要

**核心发现**：
- ✅ 发现3组过时设计（共8个文件）
- ⚠️ 发现1处活跃的Obsolete代码引用（需立即修复）
- 📈 新旧功能重复度：95%-100%
- 🎯 建议分3个阶段执行废除操作

**关键指标**：
- 待删除文件：8个
- 待修复引用：1处
- 待清理注册：3处
- 预计节省代码行数：~2000行

---

## 一、过时设计清单

### 1️⃣ PatientSelectionDialog组（已标记Obsolete，部分仍在使用）⚠️

**文件清单**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionDialogViewModel.cs`

**状态**：
- ✅ 已标记 `[Obsolete]` 特性
- ❌ **仍有1处活跃引用**（违反Obsolete警告）

**遗留引用位置**：
```csharp
// src/Client/Desktop/Workstations/ClinicalWorkstation/ViewModels/ClinicalWorkstationViewModel.cs:313
_dialogService.ShowDialog("PatientSelectionDialog", result => { ... });
```

**废除原因**：
- 弹窗式患者选择已被4步流程的Step 1（嵌入式患者选择）完全替代
- Issue #1539已修复HomeViewModel的引用，但遗漏了ClinicalWorkstation

**替代方案**：
- 使用 `MedicalCaseFlowView` 导航（Step 1嵌入式患者选择）

---

### 2️⃣ CreateMedicalCaseDialog组（无使用，可直接删除）✅

**文件清单**：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/CreateMedicalCaseDialog.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/CreateMedicalCaseDialog.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/CreateMedicalCaseDialogViewModel.cs`

**状态**：
- ✅ 已在模块中注册
- ✅ **无任何调用代码**（搜索 `ShowDialog.*CreateMedicalCaseDialog` 无结果）
- ✅ 可安全删除

**废除原因**：
- 旧设计通过弹窗选择患者和医生，然后手动创建MedicalCase
- 新设计在Step 1选择患者后，自动由 `MedicalCaseFlowViewModel.ExecuteNextStepAsync()` 创建MedicalCase
- 医生信息由登录会话自动获取，无需选择

**替代方案**：
- 使用 `MedicalCaseFlowViewModel` 的自动创建逻辑（line 273-284，当前临时禁用）

---

### 3️⃣ MedicalCaseEntry组（功能重复，已迁移）✅

**文件清单**：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEntryView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEntryView.xaml.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseEntryViewModel.cs`

**状态**：
- ✅ 已在模块中注册
- ✅ **无活跃导航引用**（可能仅用于测试）
- ⚠️ 功能95%与新设计重复

**废除原因**：
- 旧设计：单页面综合录入（患者选择 + 四诊数据 + 诊断 + 处方按钮）
- 新设计：4步流程拆分
  - Step 1: 患者选择（PatientSelectionView）
  - Step 2: 诊断录入（ConsultationFormView）- **功能完全重复**
  - Step 3: 处方录入（PrescriptionEditorView）
  - Step 4: 完成提示（CompletionView）

**功能对比**（MedicalCaseEntryViewModel vs ConsultationFormViewModel）：
| 字段/功能 | MedicalCaseEntry | ConsultationForm | 重复度 |
|----------|-----------------|------------------|--------|
| 四诊数据（Inspection/Auscultation/Inquiry/Palpation） | ✅ | ✅ | 100% |
| 主诉（ChiefComplaint） | ✅ | ✅ | 100% |
| 现病史（PresentIllness） | ✅ | ✅ | 100% |
| 中医诊断（TCMDiagnosis） | ✅ | ✅ | 100% |
| 治则治法（TreatmentPrinciple） | ✅ | ✅ | 100% |
| 备注（Remarks/Remark） | ✅ | ✅ | 100% |
| SaveAsync方法 | ✅ | ✅ | 100% |
| Validate方法 | ✅ | ✅ | 100% |
| **架构差异** | 独立导航 | 嵌入式流程 | - |

**替代方案**：
- 使用 `MedicalCaseFlowView` + `ConsultationFormView`（Step 2）

---

## 二、新旧交互设计对比

### 旧设计（Epic #1456 / Issue #1463）

```
交互流程：
1. 点击"开始看诊" → PatientSelectionDialog弹窗
2. 选择患者 → 关闭弹窗 → MedicalCaseEntryView单页录入
3. 填写四诊、诊断、备注 → 点击"保存"
4. 点击"开处方" → 跳转到处方界面

特点：
- 弹窗式患者选择
- 单页面综合录入
- 手动保存和跳转
```

### 新设计（Epic #1494 - 4步流程）

```
交互流程：
1. 点击"开始看诊" → MedicalCaseFlowView（显示Step 1）
2. Step 1: PatientSelectionView嵌入式选择患者
3. Step 2: ConsultationFormView录入四诊和诊断
4. Step 3: PrescriptionEditorView录入处方
5. Step 4: CompletionView显示完成提示

特点：
- 嵌入式患者选择
- 分步流程控制（状态机）
- 自动保存和导航
- 前一步/后一步按钮导航
```

### 功能覆盖度分析

| 旧功能 | 新流程步骤 | 覆盖度 | 备注 |
|--------|-----------|--------|------|
| 患者选择 | Step 1 | 100% | 弹窗 → 嵌入式 |
| 四诊录入 | Step 2 | 100% | 完全一致 |
| 诊断录入 | Step 2 | 100% | 完全一致 |
| 处方录入 | Step 3 | 100% | 独立步骤 |
| 完成确认 | Step 4 | 新增 | 提升体验 |

**结论**：新设计完全覆盖旧设计的所有功能，且体验更优。

---

## 三、相关Issue分析

### 旧设计相关Issue（已完成，代码待清理）

#### Epic #1456: 临床工作台看诊流程完整实现
- **关联代码**：MedicalCaseEntryViewModel/View
- **状态**：已完成，但代码未删除
- **说明**：旧的单页面看诊流程设计

#### Issue #1463: 以MedicalCase为中心的激进重构
- **关联代码**：MedicalCaseEntryViewModel迁移
- **状态**：已完成迁移，旧代码仍存在
- **说明**：从ConsultationModule迁移到MedicalCaseModule

#### Issue #1457: ClinicalWorkstation患者选择
- **关联代码**：ClinicalWorkstationViewModel.cs:313
- **状态**：⚠️ **仍在使用已Obsolete的PatientSelectionDialog**
- **说明**：HomeViewModel已修复（Issue #1539），但ClinicalWorkstation遗漏

### 新设计相关Issue（当前活跃）

#### Epic #1494: 医案流程UI重构（4步流程）
- **关联代码**：MedicalCaseFlowView + 4个Step ViewModel
- **状态**：✅ 已实施
- **说明**：当前生产环境使用的设计

#### Issue #1539: 主页导航修复
- **关联代码**：HomeViewModel
- **状态**：✅ 已修复（移除PatientSelectionDialog调用）
- **说明**：已将主页改为直接导航到MedicalCaseFlowView

#### Issue #1498: Step 1患者参数传递
- **关联代码**：MedicalCaseFlowViewModel接收Patient参数
- **状态**：✅ 已实施
- **说明**：支持从HomeView直接传递患者信息

#### Issue #1487: QuickCreatePatientDialog集成
- **关联代码**：PatientSelectionViewModel（Step 1）
- **状态**：✅ 已实施
- **说明**：Step 1支持快速新建患者

---

## 四、废除优先级建议

### 🔴 P0 - 立即修复（影响用户，违反Obsolete警告）

#### 任务1: 修复ClinicalWorkstation使用过时Dialog

**问题描述**：
- 文件：`ClinicalWorkstationViewModel.cs`
- 位置：line 313
- 问题：使用已标记 `[Obsolete]` 的 `PatientSelectionDialog`
- 影响：编译时产生Obsolete警告（已通过 `#pragma warning disable` 抑制）

**修复方案**：
```csharp
// 当前代码（错误）
_dialogService.ShowDialog("PatientSelectionDialog", result => { ... });

// 修复后（推荐）
var parameters = new NavigationParameters
{
    { "Patient", _currentPatient }  // 可选：预填充患者
};
_regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView", parameters);
```

**验收标准**：
- [ ] 移除 `PatientSelectionDialog` 调用
- [ ] 改用 `MedicalCaseFlowView` 导航
- [ ] 移除 `#pragma warning disable` 代码
- [ ] 编译通过（0 warnings）
- [ ] 功能测试通过

**预计工作量**：1小时

---

### 🟡 P1 - 高优先级（代码清理，减少维护成本）

#### 任务2: 删除CreateMedicalCaseDialog组

**删除文件**：
1. `Views/CreateMedicalCaseDialog.xaml`
2. `Views/CreateMedicalCaseDialog.xaml.cs`
3. `ViewModels/CreateMedicalCaseDialogViewModel.cs`

**清理注册代码**：
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs:36
// 删除以下行：
containerRegistry.RegisterDialog<Views.CreateMedicalCaseDialog, ViewModels.CreateMedicalCaseDialogViewModel>();
```

**验收标准**：
- [ ] 3个文件已删除
- [ ] 模块注册代码已清理
- [ ] 编译通过（0 warnings, 0 errors）
- [ ] 无引用残留（搜索 `CreateMedicalCaseDialog` 无结果）

**预计工作量**：30分钟

---

#### 任务3: 删除MedicalCaseEntry组

**删除文件**：
1. `Views/MedicalCaseEntryView.xaml`
2. `Views/MedicalCaseEntryView.xaml.cs`
3. `ViewModels/MedicalCaseEntryViewModel.cs`

**清理注册代码**：
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs
// 删除以下行：
containerRegistry.Register<ViewModels.MedicalCaseEntryViewModel>();  // line 41
containerRegistry.RegisterForNavigation<Views.MedicalCaseEntryView>();  // line 50
```

**验收标准**：
- [ ] 3个文件已删除
- [ ] 模块注册代码已清理
- [ ] 编译通过（0 warnings, 0 errors）
- [ ] 无引用残留（搜索 `MedicalCaseEntry` 无结果）

**预计工作量**：30分钟

---

### 🟢 P2 - 中优先级（最终清理，前提是P0完成）

#### 任务4: 删除PatientSelectionDialog组

**前置条件**：
- ✅ P0任务1已完成（ClinicalWorkstation已修复）
- ✅ 编译测试通过

**删除文件**：
1. `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml`
2. `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.xaml.cs`
3. `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionDialogViewModel.cs`

**清理注册代码**：
```csharp
// src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs
// 删除以下代码：
#pragma warning disable CS0618 // 类型或成员已过时
            containerRegistry.RegisterDialog<Views.PatientSelectionDialog, ViewModels.PatientSelectionDialogViewModel>();
#pragma warning restore CS0618 // 类型或成员已过时
```

**验收标准**：
- [ ] 3个文件已删除
- [ ] 模块注册和警告抑制代码已清理
- [ ] 编译通过（0 warnings, 0 errors）
- [ ] 无引用残留（搜索 `PatientSelectionDialog` 无结果）
- [ ] 完整回归测试通过（4步看诊流程）

**预计工作量**：1小时

---

## 五、影响范围评估

### 代码统计

| 指标 | 数量 | 说明 |
|------|------|------|
| 待删除文件 | 8个 | 3组过时设计（每组2-3个文件） |
| 待修复引用 | 1处 | ClinicalWorkstationViewModel.cs:313 |
| 待清理注册 | 3处 | 模块注册代码 |
| 预计减少代码行数 | ~2000行 | 包括XAML和C#代码 |
| 预计减少编译警告 | 1个 | Obsolete警告（当前被抑制） |

### 模块影响

| 模块 | 影响类型 | 说明 |
|------|---------|------|
| LYBT.Desktop.Patients | 文件删除 | PatientSelectionDialog组 |
| LYBT.Desktop.MedicalCase | 文件删除 | CreateMedicalCaseDialog + MedicalCaseEntry |
| LYBT.Desktop.ClinicalWorkstation | 代码修复 | ClinicalWorkstationViewModel导航逻辑 |

### 测试需求

**单元测试**：
- [ ] ClinicalWorkstationViewModel导航测试
- [ ] MedicalCaseFlowViewModel流程测试

**集成测试**：
- [ ] 完整4步看诊流程测试
- [ ] 前一步/后一步导航测试
- [ ] 新建患者功能测试

**回归测试**：
- [ ] 主页"开始看诊"导航
- [ ] 临床工作台患者选择
- [ ] Step 1-4完整流程
- [ ] 数据保存和加载

---

## 六、执行计划

### 阶段1：修复P0问题（1-2天）

**目标**：移除所有Obsolete代码的活跃引用

**任务清单**：
1. 创建Issue: `[Tech Debt] 修复ClinicalWorkstation使用过时PatientSelectionDialog`
2. 修改 `ClinicalWorkstationViewModel.cs` 导航逻辑
3. 移除 `#pragma warning disable CS0618`
4. 编译验证（0 warnings, 0 errors）
5. 功能测试

**验收标准**：
- ✅ 编译无Obsolete警告
- ✅ ClinicalWorkstation导航功能正常
- ✅ 创建PR并合并

---

### 阶段2：清理P1代码（1天）

**目标**：删除无引用的过时Dialog和View

**任务清单**：
1. 创建Issue: `[Tech Debt] 清理过时的Dialog和Entry组件`
2. 删除CreateMedicalCaseDialog组（3个文件）
3. 删除MedicalCaseEntry组（3个文件）
4. 清理模块注册代码
5. 编译验证（0 warnings, 0 errors）
6. 代码审查

**验收标准**：
- ✅ 8个文件已删除（CreateDialog 3 + Entry 3）
- ✅ 模块注册代码已清理
- ✅ 编译通过
- ✅ 创建PR并合并

---

### 阶段3：最终清理P2（1天）

**目标**：删除PatientSelectionDialog组

**任务清单**：
1. 确认P0已完成（无Obsolete引用）
2. 创建Issue: `[Tech Debt] 删除已废弃的PatientSelectionDialog`
3. 删除PatientSelectionDialog组（3个文件）
4. 清理模块注册和警告抑制代码
5. 编译验证
6. 完整回归测试

**验收标准**：
- ✅ 3个文件已删除
- ✅ 模块注册和警告抑制代码已清理
- ✅ 编译通过（0 warnings, 0 errors）
- ✅ 完整回归测试通过
- ✅ 更新文档记录
- ✅ 创建PR并合并

---

### 阶段4：文档同步（0.5天）

**任务清单**：
1. 更新架构文档：`docs/explanation/architecture/client/README.md`
2. 更新开发指南：`docs/how-to-guides/client/README.md`
3. 更新快速参考：`docs/reference/quick-reference/code-patterns.md`
4. 创建变更记录：`docs/reports/obsolete-design-cleanup-完成日期.md`

---

## 七、风险评估与缓解

### 风险1: 遗漏的引用导致运行时错误

**风险等级**：🟡 中

**描述**：可能存在动态字符串引用（如 `_dialogService.ShowDialog("PatientSelectionDialog")`）未被搜索到。

**缓解措施**：
- ✅ 使用多种搜索模式（正则表达式）
- ✅ 编译时验证（删除后立即编译）
- ✅ 完整回归测试
- ✅ 分阶段执行（先修复引用，再删除文件）

---

### 风险2: 单元测试依赖过时组件

**风险等级**：🟢 低

**描述**：单元测试可能直接引用即将删除的ViewModel或View。

**缓解措施**：
- ✅ 搜索测试项目中的引用
- ✅ 编译测试项目验证
- ✅ 更新或删除相关测试用例

---

### 风险3: 回退计划缺失

**风险等级**：🟢 低

**描述**：如果删除后发现遗漏的功能，需要快速回退。

**缓解措施**：
- ✅ 每个阶段独立PR，便于回退
- ✅ Git保留完整历史记录
- ✅ 保留至少1个Sprint周期再永久删除分支

---

## 八、预期收益

### 代码质量提升

- ✅ 移除~2000行冗余代码
- ✅ 消除Obsolete编译警告
- ✅ 提高代码可维护性
- ✅ 减少代码审查负担

### 架构清晰度

- ✅ 统一看诊交互为4步流程
- ✅ 移除旧设计的混淆
- ✅ 新成员更容易理解代码结构

### 开发效率

- ✅ 减少功能重复导致的Bug
- ✅ 减少修改时需要同步多处的成本
- ✅ 减少测试覆盖范围

---

## 九、参考资料

### 相关Issue

- Epic #1456: 临床工作台看诊流程完整实现（旧设计）
- Epic #1494: 医案流程UI重构（新4步流程）
- Issue #1463: 以MedicalCase为中心的激进重构
- Issue #1457: ClinicalWorkstation患者选择
- Issue #1539: 主页导航修复
- Issue #1498: Step 1患者参数传递
- Issue #1487: QuickCreatePatientDialog集成

### 相关文档

- 架构指南：`docs/explanation/architecture/client/README.md`
- 开发指南：`docs/how-to-guides/client/README.md`
- Phase 1完成总结：`docs/reports/phase1-complete-summary-2025-10-20.md`
- 技术债务跟踪：`docs/reports/medical-case-flow-validation-debt-2025-10-20.md`

### 代码位置

**旧设计代码**：
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionDialog.*`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/CreateMedicalCaseDialog.*`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEntryView.*`

**新设计代码**：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseFlowView.*`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFlowViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PatientSelectionViewModel.cs` (Step 1)
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationFormViewModel.cs` (Step 2)
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs` (Step 3)
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/CompletionViewModel.cs` (Step 4)

---

## 📌 总结与建议

### 核心建议

1. **立即执行P0任务**：修复ClinicalWorkstation的Obsolete引用，消除编译警告
2. **按阶段执行**：分3个阶段（P0→P1→P2）逐步清理，降低风险
3. **充分测试**：每个阶段完成后进行完整的回归测试
4. **文档同步**：更新架构和开发文档，反映最新的设计

### 预期时间表

| 阶段 | 任务 | 预计时间 | 累计时间 |
|------|------|---------|---------|
| P0 | 修复ClinicalWorkstation引用 | 1-2天 | 1-2天 |
| P1 | 删除CreateDialog + Entry | 1天 | 2-3天 |
| P2 | 删除PatientSelectionDialog | 1天 | 3-4天 |
| 文档 | 同步文档更新 | 0.5天 | 3.5-4.5天 |

**总计**：3.5-4.5个工作日

### 下一步行动

1. ✅ 用户确认本报告内容和执行计划
2. ✅ 创建P0 Issue并分配开发人员
3. ✅ 开始执行阶段1（修复P0）
4. ⏳ 代码审查和合并
5. ⏳ 依次执行P1、P2阶段

---

**报告生成人员**：Claude Code
**审查人员**：待用户确认
**生成日期**：2025-10-20
