# Proposal: add-prescription-import-dialogs

## Summary

完成处方编辑阶段的药材导入功能，包括"从验方导入"和"从历史处方复制"两个对话框。当前ViewModel和XAML已实现，但因缺少`CustomDialogWindowStyle`资源导致运行时崩溃。

## Why

1. **功能缺失**：处方面板的"经验方查询"和"历史处方查询"按钮点击后无响应（实际是因样式资源缺失导致崩溃）
2. **用户体验**：医生需要快速导入常用验方或复制历史处方药材，减少重复录入
3. **业务完整性**：此功能是医案编辑流程的核心辅助功能

## What Changes

### Phase 1: 创建CustomDialogWindowStyle资源（修复崩溃）

在共享资源中定义`CustomDialogWindowStyle`，用于Prism DialogService的窗口样式。

**设计要点**：
- 无边框窗口，便于自定义标题栏
- 允许拖动（通过标题栏区域）
- 固定大小（根据各对话框设计尺寸）
- 居中显示

**影响范围**（9个对话框）：
- `FormulaImportDialog.xaml` (MedicalCase模块)
- `HistoryCopyDialog.xaml` (MedicalCase模块)
- `AuditReasonDialog.xaml` (MedicalCase模块)
- `AuditLogDialog.xaml` (MedicalCase模块)
- `UnsavedChangesDialog.xaml` (MedicalCase模块)
- `FormulaSelectionDialog.xaml` (MedicalCase模块)
- `HistoryPrescriptionSelectionDialog.xaml` (MedicalCase模块)
- `DuplicateHerbAlertDialog.xaml` (MedicalCase模块)
- `EditFormulaDialog.xaml` (Formula模块)

### Phase 2: 验证对话框功能（无代码修改）

验证已实现的对话框功能正常工作：

**FormulaImportDialog（从验方导入）**：
- 搜索：按验方名称、功效、适应症实时筛选
- 列表：显示验方名称、药材数量、分类、状态
- 预览：选中验方后显示药材组成
- 返回：`SelectedFormula` + `SelectedHerbs`

**HistoryCopyDialog（从历史处方复制）**：
- 患者：显示当前患者姓名
- 搜索：按诊断、主诉、日期筛选
- 列表：显示就诊日期、诊断、主诉、状态
- 预览：选中医案后显示处方药材
- 返回：`SelectedCase` + `SelectedItems`

### Phase 3: 集成PrescriptionPanelViewModel

连接对话框与处方面板的药材导入逻辑：

**现有实现**：
- `PrescriptionImportHandler.cs` - 处理药材导入，包含重复药材检测
- `PrescriptionPanelViewModel.cs` - 处方面板ViewModel，有`ImportFormulaCommand`和`CopyHistoryPrescriptionCommand`

**需要验证/实现**：
1. 对话框调用流程（ShowDialog → 获取返回值 → 调用ImportHandler）
2. 重复药材提醒对话框（DuplicateHerbAlertDialog）集成
3. 导入后自动刷新药材列表和价格计算

## Impact

### 影响范围

**新增文件**：
- `LYBT.Desktop.Presentation/Themes/DialogStyles.xaml` - 对话框样式资源

**修改文件**：
- `LYBT.Desktop.Shell/App.xaml` - 引用DialogStyles.xaml资源字典

### 风险评估

- **低风险**：主要是添加缺失的样式资源
- **已有实现**：ViewModel和XAML已经完成，只需修复资源问题
- **测试覆盖**：需要手动测试对话框显示和导入流程

### 收益

1. **即时修复**：解决当前运行时崩溃问题
2. **功能完整**：处方导入功能可用
3. **统一样式**：所有对话框使用一致的窗口样式

## Dependencies

- 依赖 `IFormulaRepository` 获取验方列表和详情
- 依赖 `IMedicalCaseRepository` 获取患者历史医案
- 依赖 `PrescriptionImportHandler` 处理药材导入逻辑

## Success Criteria

1. 点击"经验方查询"按钮正常弹出FormulaImportDialog
2. 点击"历史处方查询"按钮正常弹出HistoryCopyDialog
3. 选择验方/历史处方后药材正确导入到处方列表
4. 重复药材时正确提示用户
5. 编译通过，无运行时异常
6. 所有9个对话框都能正常显示

## Design Details

### CustomDialogWindowStyle设计

```xml
<Style x:Key="CustomDialogWindowStyle" TargetType="Window">
    <Setter Property="WindowStyle" Value="None"/>
    <Setter Property="ResizeMode" Value="NoResize"/>
    <Setter Property="ShowInTaskbar" Value="False"/>
    <Setter Property="SizeToContent" Value="WidthAndHeight"/>
    <Setter Property="WindowStartupLocation" Value="CenterOwner"/>
    <Setter Property="Background" Value="White"/>
</Style>
```

### 对话框交互流程

```
用户点击"经验方查询"
    ↓
PrescriptionPanelViewModel.ImportFormulaCommand
    ↓
DialogService.ShowDialog("FormulaImportDialog")
    ↓
FormulaImportDialogViewModel加载验方列表
    ↓
用户搜索、选择、预览
    ↓
用户点击"确认导入"
    ↓
DialogResult.OK + DialogParameters(SelectedFormula, SelectedHerbs)
    ↓
PrescriptionImportHandler.ImportFromFormulaAsync()
    ↓
检测重复药材 → DuplicateHerbAlertDialog（如有）
    ↓
追加到HerbItems集合
    ↓
刷新UI、重新计算价格
```

### 数据流转

**验方导入**：
```
FormulaDto → FormulaHerbItemDto[] → PrescriptionHerbItemViewModel[]
```

**历史处方复制**：
```
MedicalCaseDto → PrescriptionDto → PrescriptionItemDto[] → PrescriptionHerbItemViewModel[]
```

## Alternatives Considered

### 替代方案1：直接在PrismApplication中定义样式

- 优点：简单
- 缺点：与其他窗口样式混杂，不易维护
- **决定：不采用**

### 替代方案2：每个对话框独立定义窗口样式

- 优点：各模块独立
- 缺点：代码重复，样式不统一
- **决定：不采用**

### 替代方案3：移除prism:Dialog.WindowStyle（使用默认样式）

- 优点：最简单的修复
- 缺点：对话框使用系统默认窗口样式，与项目UI风格不统一
- **决定：作为临时方案可考虑，但最终应实现统一样式**
