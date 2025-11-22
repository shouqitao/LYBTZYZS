# Desktop客户端已废弃组件清单

**文档类型**: 架构解释（Explanation）
**目标读者**: 开发者、维护人员
**创建时间**: 2025-11-05
**最后更新**: 2025-11-05

---

## 📋 目录

- [文档目的](#文档目的)
- [废弃组件列表](#废弃组件列表)
  - [2025-11 Epic #1822 废弃](#2025-11-epic-1822-废弃)
- [迁移指南](#迁移指南)

---

## 文档目的

本文档记录Desktop客户端中**已删除**的组件，包括：
- 废弃原因
- 废弃时间
- 替代方案
- 迁移路径

**目标**：帮助开发者理解架构演进，避免引用已废弃的组件。

---

## 废弃组件列表

### 2025-11 Epic #1822 废弃

#### 1. ConsultationManagementView（已删除）

**文件路径**（已删除）:
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/Views/ConsultationManagementView.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.Consultation/ViewModels/ConsultationManagementViewModel.cs`

**废弃原因**:
- 功能已合并至 `MedicalCaseFlowView` 的三步诊疗流程
- 独立的辨证管理视图违反了**三步一体化**设计原则
- Issue #1806: MedicalCase模块重构，统一流程管理

**废弃时间**: 2025-11（Epic #1822）

**删除提交**: 待查（Epic #1822相关提交）

**替代方案**: `MedicalCaseFlowView` - 三步诊疗流程

**功能对比**:

| 功能 | ConsultationManagementView（已废弃） | MedicalCaseFlowView（替代） |
|-----|----------------------------------|---------------------------|
| 辨证信息录入 | ✅ 独立视图 | ✅ 第1步：辨证 |
| 开方决策 | ❌ 需切换到其他视图 | ✅ 第2步：开方标记 |
| 处方编辑 | ❌ 需切换到其他视图 | ✅ 第3步：处方 |
| 流程一体化 | ❌ 多视图切换 | ✅ 单视图完整流程 |

**迁移路径**:

```csharp
// ❌ 旧代码（已废弃）
_regionManager.RequestNavigate("ContentRegion", "ConsultationManagementView",
    new NavigationParameters { { "MedicalCaseId", medicalCaseId } });

// ✅ 新代码（替代方案）
_regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView",
    new NavigationParameters
    {
        { "PatientId", patientId },
        { "MedicalCaseId", medicalCaseId } // 如果是编辑已有病历
    });
```

**业务流程变更**:

**Before（多视图分离）**:
```
开始接诊 → 患者选择 → ConsultationManagementView（辨证）
                           ↓
                    PrescriptionManagementView（处方）
```

**After（三步一体化）**:
```
开始接诊 → 患者选择 → MedicalCaseFlowView
                    ├─ Step 1: 辨证（ConsultationEditor）
                    ├─ Step 2: 开方标记
                    └─ Step 3: 处方编辑（PrescriptionEditor）
```

---

#### 2. ViewFormulaDialog（已删除）

**文件路径**（已删除）:
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Dialogs/ViewFormulaDialog.xaml`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Dialogs/ViewFormulaDialogViewModel.cs`

**废弃原因**:
- 功能已集成至 `MedicalCaseFlowView` 的内联验方选择
- 弹窗式验方查看影响用户操作流程连贯性
- Issue #1807: MedicalCaseFlowViewModel组件化重构

**废弃时间**: 2025-11（Epic #1822）

**删除提交**: 待查（Epic #1822相关提交）

**替代方案**: `MedicalCaseFlowView` 内联验方选择

**功能对比**:

| 功能 | ViewFormulaDialog（已废弃） | MedicalCaseFlowView（替代） |
|-----|---------------------------|---------------------------|
| 验方查看 | ✅ 弹窗显示 | ✅ 内联显示（右侧面板） |
| 验方导入 | ✅ 需手动复制 | ✅ 一键导入到处方 |
| 工作流连贯性 | ❌ 弹窗打断流程 | ✅ 无缝集成 |

**迁移路径**:

```csharp
// ❌ 旧代码（已废弃）
public void ViewFormulaDetails()
{
    var dialog = new ViewFormulaDialog();
    var viewModel = new ViewFormulaDialogViewModel(selectedFormula);
    dialog.DataContext = viewModel;
    dialog.ShowDialog();
}

// ✅ 新代码（替代方案）
// MedicalCaseFlowView内部已集成验方选择面板
public void SelectFormula()
{
    // 在MedicalCaseFlowView的第3步（处方编辑）中
    // 验方列表直接显示在右侧面板
    SelectedFormula = formula;

    // 一键导入验方到处方
    await _formulaImporter.ImportFormulaAsync(formula.Id);
}
```

**UI交互变更**:

**Before（弹窗式）**:
```
处方编辑界面 → [点击"选择验方"]
                    ↓
         弹窗：ViewFormulaDialog
         - 查看验方详情
         - 手动复制药材到处方
         - 关闭弹窗
```

**After（内联式）**:
```
MedicalCaseFlowView - Step 3（处方编辑）
├─ 左侧：处方编辑区
└─ 右侧：验方选择面板（内联）
         - 验方列表
         - 验方详情预览
         - [一键导入]按钮
```

---

## 迁移指南

### 通用迁移步骤

1. **代码搜索**
   ```bash
   # 搜索项目中是否仍有引用已废弃组件
   grep -r "ConsultationManagementView" src/
   grep -r "ViewFormulaDialog" src/
   ```

2. **替换导航代码**
   - 将所有导航至 `ConsultationManagementView` 的代码改为 `MedicalCaseFlowView`
   - 移除所有 `ViewFormulaDialog` 的弹窗调用，改用 `MedicalCaseFlowView` 内联功能

3. **验证功能**
   - 测试辨证功能是否正常（MedicalCaseFlowView Step 1）
   - 测试验方导入功能是否正常（MedicalCaseFlowView Step 3）

4. **清理依赖**
   ```csharp
   // 从Module注册中移除（如果仍存在）
   // ❌ 移除
   containerRegistry.RegisterForNavigation<ConsultationManagementView>();
   containerRegistry.RegisterDialog<ViewFormulaDialog>();
   ```

### 测试验证清单

- [ ] 辨证功能正常（MedicalCaseFlowView Step 1）
- [ ] 开方标记功能正常（MedicalCaseFlowView Step 2）
- [ ] 处方编辑功能正常（MedicalCaseFlowView Step 3）
- [ ] 验方内联选择功能正常
- [ ] 验方一键导入功能正常
- [ ] 无编译错误（引用已删除组件）
- [ ] 无导航错误（导航至已删除视图）

---

## 相关文档

### 内部文档

- **MedicalCase模块设计**: `docs/explanation/architecture/client/medical-case-design.md`
- **新增服务文档**: `docs/updates/issue-1822-new-services-documentation.md`

### Issue追踪

- **#1822**: Epic - 启动到工作台流程端到端重构优化
- **#1806**: MedicalCaseManagementView注册修复
- **#1807**: MedicalCaseFlowViewModel组件化重构

---

## 附录：废弃组件历史记录

### 统计数据

| 废弃批次 | 废弃组件数 | 废弃原因 |
|---------|-----------|---------|
| 2025-11 Epic #1822 | 4个文件 | 功能合并至MedicalCaseFlowView |

### 代码行数统计

| 组件 | 删除前代码行数 | 替代方案 |
|-----|--------------|---------|
| ConsultationManagementViewModel | ~300行 | MedicalCaseFlowView Step 1 |
| ConsultationManagementView.xaml | ~200行 | MedicalCaseFlowView Step 1 |
| ViewFormulaDialogViewModel | ~150行 | MedicalCaseFlowView内联验方 |
| ViewFormulaDialog.xaml | ~100行 | MedicalCaseFlowView内联验方 |
| **合计** | **~750行** | - |

**重构效果**: 删除750行冗余代码，功能统一至MedicalCaseFlowView（629行核心代码）

---

**文档创建时间**: 2025-11-05
**文档维护**: Architecture Team
**下次审查**: 2025-12（每月审查废弃组件清单）
