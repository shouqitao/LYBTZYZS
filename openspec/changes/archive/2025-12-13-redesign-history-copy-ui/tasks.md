# redesign-history-copy-ui Tasks

## Phase 1: UI重构 (XAML层变更)

### Task 1.1: 更新对话框尺寸和基础布局
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml`
- **变更**:
  - 对话框尺寸从700x550调整为1100x680
  - 实现左右双栏Grid布局 (320:*)
  - 添加FormulaCardStyle卡片样式（参考FormulaImportDialog）
- **AC**: AC-1, AC-6
- **估计**: 1小时

### Task 1.2: 实现左栏 - 搜索筛选区
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml`
- **变更**:
  - 当前患者信息显示区
  - 搜索框支持模糊查询（患者姓名 + 中医诊断）
  - 时间区间筛选（起始日期DatePicker + 结束日期DatePicker）
- **AC**: AC-2, AC-3, AC-4
- **估计**: 45分钟

### Task 1.3: 实现左栏 - 历史医案卡片列表
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml`
- **变更**:
  - 将ListView改为ListBox + HistoryCaseCardTemplate卡片模板
  - **三维度卡片布局设计**:
    - **患者维度**: 患者姓名 + 就诊日期（定位历史记录）
    - **诊断维度**: 中医诊断（核心参考信息）
    - **处方维度**: 药材数量 + 剂数 + 状态标签（已诊疗/已开方）
  - 添加HistoryCaseCardStyle样式（悬停/选中效果）
  - 启用VirtualizingStackPanel虚拟化
- **AC**: AC-1, AC-5
- **估计**: 1小时
- **说明**: 卡片仅显示三个维度的摘要信息，完整详情在右栏展示

### Task 1.4: 实现右栏 - 详情预览区（复用MedicalCaseViewControl）
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml`
- **变更**:
  - 无选中时显示提示文本"请从左侧选择一个历史医案"
  - 有选中时直接复用`MedicalCaseViewControl`控件
  - 绑定`SelectedCaseDetail`、`SelectedCaseHasConsultation`、`SelectedCaseHasPrescription`属性
  - 外层ScrollViewer包装以支持内容滚动
- **AC**: AC-5
- **估计**: 30分钟
- **说明**: MedicalCaseViewControl已包含完整的医案详情展示（基本信息、诊疗信息、处方信息），无需重复实现

## Phase 2: ViewModel层变更

### Task 2.1: 添加时间区间筛选属性
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialogViewModel.cs`
- **变更**:
  - 新增 `DateTime? StartDate` 属性（起始日期）
  - 新增 `DateTime? EndDate` 属性（结束日期）
  - 修改 `FilterCases()` 方法支持:
    - 关键词模糊筛选（患者姓名 OR 中医诊断）
    - 时间区间筛选（StartDate ~ EndDate）
- **AC**: AC-3, AC-4
- **估计**: 30分钟

### Task 2.2: 添加详情加载功能
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialogViewModel.cs`
- **变更**:
  - 新增 `MedicalCaseDetailDto? SelectedCaseDetail` 属性（用于MedicalCaseViewControl绑定）
  - 新增 `SelectedCaseHasConsultation` 计算属性（判断是否有诊疗记录）
  - 新增 `SelectedCaseHasPrescription` 计算属性（判断是否有处方）
  - 修改 `LoadCasePreviewAsync()` 为 `LoadCaseDetailAsync(Guid caseId)`
  - 在SelectedCase变更时自动调用LoadCaseDetailAsync加载完整详情
- **AC**: AC-5
- **估计**: 30分钟

### Task 2.3: 更新确认逻辑（仅导入药材组合）
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialogViewModel.cs`
- **变更**:
  - 确认复制时仅返回**药材组合**（PrescriptionItems列表）
  - 不导入诊断信息、主诉等其他医案信息
  - 更新CanConfirm判断逻辑（选中医案必须有处方才能导入）
- **AC**: AC-6
- **估计**: 15分钟
- **说明**: 预览时显示完整详情供参考，但导入操作只提取药材组合到当前医案的处方中

## Phase 3: 样式和资源

### Task 3.1: 复用样式资源
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml`
- **变更**:
  - 引入 UnifiedComponents.xaml
  - 引入 HerbCardControl 命名空间
  - 复用 FormulaCardStyle 或定义 HistoryCardStyle
- **AC**: AC-6
- **估计**: 15分钟

## 依赖关系

```
Task 1.1 -> Task 1.2 -> Task 1.3 -> Task 1.4
                |
                v
            Task 2.1 -> Task 2.2 -> Task 2.3
                            |
                            v
                        Task 3.1
```

## 总估计时间

- Phase 1: 3小时15分钟
- Phase 2: 1小时15分钟
- Phase 3: 15分钟
- **总计**: 约4.5小时

## 验收检查清单

- [x] 对话框尺寸为1100x680
- [x] 左右双栏布局正确显示
- [x] 搜索框功能正常
- [x] 时间范围筛选功能正常
- [x] 卡片列表正确显示历史医案
- [x] 选中医案后右侧显示详情
- [x] 药材卡片正确显示（通过MedicalCaseViewControl复用）
- [x] 确认复制功能正常（仅导入药材组合）
- [x] 遵循CustomDialogWindowStyle样式规范
