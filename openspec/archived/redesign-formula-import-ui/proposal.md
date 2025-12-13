# redesign-formula-import-ui Proposal

## Summary

重新设计看诊界面的"经验方查询"导入功能，优化用户体验和UI设计。当前的FormulaImportDialog存在以下问题：

1. **UI布局问题**: ListView列表信息密度低，预览区域太小
2. **搜索体验差**: 缺少分类筛选、拼音码搜索优化
3. **药材预览不直观**: 仅显示文本列表，无法直观看到药材卡片
4. **交互流程单一**: 只能单选一个验方导入

## Problem Statement

当前FormulaImportDialog的问题：

### 现状分析

**现有实现** (`Dialogs/FormulaImportDialog.xaml`):
- 650x500固定尺寸对话框
- ListView + GridView显示验方列表
- 简单文本搜索框
- 底部预览区仅显示"药材名 用量"文本

**用户痛点**:
1. 验方数量多时难以快速定位（无分类筛选）
2. 药材预览区太小，看不清完整组成
3. 无法预览验方的适应症、功效等详细信息
4. 导入后无法调整药材用量

## Proposed Solution

### 新设计方案

采用**左右双栏布局**，提供更丰富的信息展示和更好的交互体验：

```
┌─────────────────────────────────────────────────────────────────────┐
│ 标题栏: 从经验方导入                                            [X] │
├────────────────────────────┬────────────────────────────────────────┤
│  [搜索框]     [分类筛选▼]  │  验方详情                              │
├────────────────────────────┤                                        │
│  ┌──────────────────────┐  │  名称: 感冒方                          │
│  │ 感冒方        [12味] │  │  分类: 感冒类    来源: 经典方剂        │
│  │ 适应症: 风寒感冒...  │  │  适应症: 风寒感冒，恶寒发热...         │
│  └──────────────────────┘  │  功效: 发散风寒，宣肺止咳              │
│  ┌──────────────────────┐  │                                        │
│  │ 清热方        [8味]  │  │  ┌────────────────────────────────┐   │
│  │ 适应症: 热毒炽盛...  │  │  │ 药材组成 (12味)                │   │
│  └──────────────────────┘  │  │ ┌──────┐ ┌──────┐ ┌──────┐     │   │
│  ...                       │  │ │麻黄  │ │桂枝  │ │杏仁  │     │   │
│                            │  │ │ 6g   │ │ 9g   │ │ 9g   │     │   │
│                            │  │ └──────┘ └──────┘ └──────┘     │   │
│                            │  │ ...更多药材卡片...             │   │
│                            │  └────────────────────────────────┘   │
│  共 156 个经验方           │                                        │
├────────────────────────────┴────────────────────────────────────────┤
│                                          [确认导入选中验方] [取消]  │
└─────────────────────────────────────────────────────────────────────┘
```

### 核心改进

1. **左右双栏布局**
   - 左侧: 验方列表（卡片式）+ 搜索筛选
   - 右侧: 验方详情预览 + 药材卡片展示

2. **增强搜索功能**
   - 拼音码搜索支持
   - 分类下拉筛选（感冒类/温热类/补益类等）
   - 按药材搜索（包含某味药材的验方）

3. **药材卡片预览**
   - 复用HerbCardControl组件
   - UniformGrid 4列布局
   - 显示药材名称、用量

4. **详情面板**
   - 验方基本信息（名称、分类、来源）
   - 适应症、功效完整展示
   - 用法用量说明

## Impact Analysis

### 影响范围

| 类型 | 文件 | 变更 |
|------|------|------|
| XAML | FormulaImportDialog.xaml | 完全重写，新布局 |
| ViewModel | FormulaImportDialogViewModel.cs | 增加分类筛选、详情加载 |
| DTO | - | 复用现有FormulaDto |

### 依赖规范

- `dialog-patterns/spec.md`: Prism IDialogAware模式
- `formula-copy-flow/spec.md`: 验方数据结构
- `herb-card-control/spec.md`: HerbCardControl组件

## Technical Design

### UI层变更

**FormulaImportDialog.xaml**:
- 对话框尺寸调整为 900x650
- 左右Grid双栏布局 (300:*)
- 左栏: StackPanel + ItemsControl (卡片列表)
- 右栏: 详情区域 + ItemsControl (HerbCardControl)

### ViewModel层变更

**FormulaImportDialogViewModel.cs**:
```csharp
// 新增属性
public ObservableCollection<string> Categories { get; }
public string SelectedCategory { get; set; }
public FormulaDetailDto? SelectedFormulaDetail { get; private set; }

// 新增方法
private void FilterByCategory();
private async Task LoadFormulaDetailAsync(Guid formulaId);
```

### 样式资源

复用现有资源:
- `CustomDialogWindowStyle` (DialogStyles.xaml)
- `HerbCardControl` (Presentation层)
- `PrimaryColorBrush` (主题色)

## Acceptance Criteria

1. [AC-1] 对话框显示左右双栏布局，左侧验方列表，右侧详情预览
2. [AC-2] 搜索框支持验方名称和拼音码搜索
3. [AC-3] 分类筛选下拉框能正确过滤验方列表
4. [AC-4] 选中验方后右侧显示完整详情和药材卡片
5. [AC-5] 点击确认导入后正确返回选中验方的药材列表
6. [AC-6] 对话框遵循CustomDialogWindowStyle样式规范

## Risks and Mitigations

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| HerbCardControl在对话框中性能问题 | 大量药材时可能卡顿 | 启用虚拟化，限制单次显示数量 |
| 验方分类数据不完整 | 筛选功能受限 | 提供"全部"默认选项 |

## Spec Deltas

### 新增规范

无需新增规范文件，本次变更在dialog-patterns规范框架内。

### 规范更新

**dialog-patterns/spec.md**: 可选更新
- 添加"左右双栏对话框"布局示例

## Related Issues

- Issue #2246: 验方导入弹窗（原始实现）
- Epic #2175 BF-002: 处方导入功能

---
创建时间: 2025-12-11
状态: Draft
