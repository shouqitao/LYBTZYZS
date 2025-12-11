# Tasks: add-prescription-import-dialogs

## Phase 1: 修复对话框基础设施

- [x] 1.1 创建DialogStyles.xaml资源文件
  - 路径: `LYBT.Desktop.Shell/Styles/DialogStyles.xaml`
  - 定义 `CustomDialogWindowStyle` 窗口样式
  - 设置无边框、居中、自适应大小

- [x] 1.2 在App.xaml中引用DialogStyles.xaml
  - 修改: `LYBT.Desktop.Shell/App.xaml`
  - 在 `Application.Resources` 中添加资源字典引用

- [ ] 1.3 验证对话框能正常显示
  - 测试点击"经验方查询"按钮
  - 测试点击"历史处方查询"按钮
  - 确认无运行时异常

## Phase 2: 验证对话框功能

- [ ] 2.1 验证FormulaImportDialog功能
  - 验证验方列表加载（来自IFormulaRepository）
  - 验证搜索筛选（按名称/功效/适应症）
  - 验证预览功能（选中后显示药材组成）
  - 验证确认返回（SelectedFormula + SelectedHerbs）

- [ ] 2.2 验证HistoryCopyDialog功能
  - 验证患者历史医案加载（来自IMedicalCaseRepository）
  - 验证搜索筛选（按诊断/主诉/日期）
  - 验证预览功能（选中后显示处方药材）
  - 验证确认返回（SelectedCase + SelectedItems）

## Phase 3: 集成导入处理逻辑

- [ ] 3.1 检查PrescriptionPanelViewModel的导入命令实现
  - 确认 `ImportFormulaCommand` 调用流程
  - 确认 `CopyHistoryPrescriptionCommand` 调用流程

- [ ] 3.2 验证PrescriptionImportHandler集成
  - 验证 `ImportFromFormulaAsync` 方法
  - 验证 `ImportFromHistoryAsync` 方法
  - 验证重复药材检测逻辑

- [ ] 3.3 验证DuplicateHerbAlertDialog集成
  - 检测到重复药材时弹出确认对话框
  - 用户可选择"替换剂量"或"保留两者"
  - 验证选择后药材列表正确更新

- [ ] 3.4 验证导入后UI刷新
  - 药材卡片正确显示
  - 价格自动重新计算
  - 空白输入框正确管理

## Phase 4: 完善测试

- [ ] 4.1 添加FormulaImportDialogViewModel单元测试
  - 测试搜索筛选逻辑
  - 测试预览加载逻辑
  - 测试CanConfirm条件

- [ ] 4.2 添加HistoryCopyDialogViewModel单元测试
  - 测试历史医案加载
  - 测试筛选逻辑
  - 测试预览加载逻辑

## Notes

### 现有代码位置

**对话框实现**：
- `LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml(.cs)`
- `LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialogViewModel.cs`
- `LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml(.cs)`
- `LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialogViewModel.cs`

**导入处理器**：
- `LYBT.Desktop.MedicalCase/ViewModels/Components/PrescriptionImportHandler.cs`

**处方面板**：
- `LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs`

### 依赖关系

```
PrescriptionPanelViewModel
    → ImportFormulaCommand
        → FormulaImportDialog
            → IFormulaRepository
        → PrescriptionImportHandler
            → DuplicateHerbAlertDialog (如有重复)
    → CopyHistoryPrescriptionCommand
        → HistoryCopyDialog
            → IMedicalCaseRepository
        → PrescriptionImportHandler
```
