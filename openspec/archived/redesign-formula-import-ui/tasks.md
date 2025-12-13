# redesign-formula-import-ui Tasks

## Phase 1: UI布局重构

### Task 1.1: 重写FormulaImportDialog.xaml布局
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml`

**变更内容**:
- 对话框尺寸调整为 900x650
- 实现左右双栏Grid布局 (320:*)
- 左栏: 搜索区 + 分类筛选 + 验方卡片列表
- 右栏: 验方详情区 + 药材卡片区

**验收标准**:
- [x] 对话框正常打开无闪退
- [x] 左右双栏布局正确显示
- [x] 响应鼠标拖动窗口

### Task 1.2: 实现验方卡片列表组件
**文件**: `FormulaImportDialog.xaml` (内联DataTemplate)

**变更内容**:
- 创建验方卡片样式 (Border + 名称 + 药材数量 + 适应症摘要)
- ListBox + VirtualizingStackPanel
- 选中状态高亮效果

**验收标准**:
- [x] 验方卡片正确显示信息
- [x] 选中卡片有视觉反馈
- [x] 滚动流畅无卡顿

### Task 1.3: 实现详情预览面板
**文件**: `FormulaImportDialog.xaml`

**变更内容**:
- 验方基本信息区 (名称/分类/来源/适应症/功效)
- 药材卡片区使用只读预览模板
- UniformGrid 3列布局

**验收标准**:
- [x] 详情面板正确显示选中验方信息
- [x] 药材卡片正确渲染
- [x] 只读模式，无编辑功能

---

## Phase 2: ViewModel逻辑增强

### Task 2.1: 增加分类筛选功能
**文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialogViewModel.cs`

**变更内容**:
```csharp
// 新增属性
public ObservableCollection<string> Categories { get; }
public string? SelectedCategory { get; set; }

// 新增方法
private void InitializeCategories();
private void FilterByCategory();
```

**验收标准**:
- [x] 分类下拉框显示所有分类 + "全部"选项
- [x] 选择分类后列表正确过滤
- [x] 切换分类时保持搜索文本

### Task 2.2: 优化搜索功能
**文件**: `FormulaImportDialogViewModel.cs`

**变更内容**:
- 搜索支持名称、适应症、功效字段
- 与分类筛选组合过滤

**验收标准**:
- [x] 搜索结果包含适应症匹配项
- [x] 搜索与分类筛选可组合使用

### Task 2.3: 验方详情加载优化
**文件**: `FormulaImportDialogViewModel.cs`

**变更内容**:
```csharp
private ObservableCollection<FormulaHerbItemDto> _selectedFormulaHerbs;
public ObservableCollection<FormulaHerbItemDto> SelectedFormulaHerbs { get; set; }

private bool _isLoading;
public bool IsLoading { get; set; }
```

**验收标准**:
- [x] 选中验方后异步加载详情
- [x] 加载过程显示loading状态
- [x] 详情包含完整药材列表

---

## Phase 3: 样式与细节优化

### Task 3.1: 搜索框占位符样式
**文件**: `FormulaImportDialog.xaml`

**变更内容**:
- 搜索框 placeholder 文本
- 输入时 placeholder 隐藏

**验收标准**:
- [x] 空输入时显示提示文本
- [x] 输入后提示文本消失

### Task 3.2: 验方卡片悬停效果
**文件**: `FormulaImportDialog.xaml`

**变更内容**:
- 鼠标悬停背景色变化
- 选中状态边框高亮

**验收标准**:
- [x] 悬停时有视觉反馈
- [x] 选中状态清晰可辨

### Task 3.3: 空状态提示
**文件**: `FormulaImportDialog.xaml`

**变更内容**:
- 未选中验方时详情区显示提示

**验收标准**:
- [x] 详情区默认显示"请从左侧选择一个经验方"

---

## 依赖关系

```
Phase 1.1 ─┬─> Phase 1.2 ─┬─> Phase 2.1
           │              │
           └─> Phase 1.3 ─┴─> Phase 2.2 ─> Phase 2.3
                                  │
Phase 3.1 ─────────────────────────┴─> Phase 3.2 ─> Phase 3.3
```

## 测试计划

1. **单元测试**: FormulaImportDialogViewModel筛选逻辑
2. **集成测试**: 对话框打开/关闭/返回结果
3. **手动测试**:
   - 搜索各种关键词
   - 分类筛选切换
   - 选择验方查看详情
   - 确认导入验证返回数据

---
创建时间: 2025-12-11
完成时间: 2025-12-12
状态: Completed
