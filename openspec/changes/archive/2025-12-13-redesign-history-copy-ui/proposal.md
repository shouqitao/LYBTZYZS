# redesign-history-copy-ui Proposal

## Summary

重新设计医案编辑界面的"历史医案查询"导入功能，优化用户体验和UI设计。当前的HistoryCopyDialog存在以下问题：

1. **UI布局问题**: ListView列表信息密度低，预览区域太小
2. **搜索体验差**: 缺少时间范围筛选、诊断分类筛选
3. **处方预览不直观**: 仅显示文本列表，无法直观看到药材卡片
4. **交互流程单一**: 只能单选一个处方导入

## Problem Statement

当前HistoryCopyDialog的问题：

### 现状分析

**现有实现** (`Dialogs/HistoryCopyDialog.xaml`):
- 700x550固定尺寸对话框
- ListView + GridView显示历史医案列表
- 简单文本搜索框（仅支持诊断、日期关键词）
- 底部预览区仅显示"药材名 用量"文本

**用户痛点**:
1. 历史医案数量多时难以快速定位（无时间范围筛选）
2. 处方预览区太小，看不清完整组成
3. 无法预览处方的完整信息（剂数、用法用量等）
4. 导入后无法调整药材用量

## Proposed Solution

### 新设计方案

采用**左右双栏布局**（参考FormulaImportDialog设计），提供更丰富的信息展示和更好的交互体验：

```
+---------------------------------------------------------------------+
| 标题栏: 从历史医案复制                                            [X] |
+----------------------------+----------------------------------------+
|  当前患者: 张三                                                     |
+----------------------------+----------------------------------------+
|  [搜索框: 患者/诊断]       |  处方详情                              |
|  [起始日期] ~ [结束日期]   |                                        |
+----------------------------+                                        |
|  +----------------------+  |  就诊日期: 2024-12-01                  |
|  | 2024-12-01     [8味] |  |  中医诊断: 风寒感冒                    |
|  | 诊断: 风寒感冒       |  |  主诉: 恶寒发热两天...                 |
|  +----------------------+  |  状态: 已完成                          |
|  +----------------------+  |                                        |
|  | 2024-11-15     [6味] |  |  +--------------------------------+   |
|  | 诊断: 脾胃虚弱       |  |  | 处方药材 (8味)                 |   |
|  +----------------------+  |  | +------+ +------+ +------+     |   |
|  ...                       |  | |麻黄  | |桂枝  | |杏仁  |     |   |
|                            |  | | 6g   | | 9g   | | 9g   |     |   |
|                            |  | +------+ +------+ +------+     |   |
|                            |  | ...更多药材卡片...             |   |
|                            |  +--------------------------------+   |
|  共 25 条历史医案          |                                        |
+----------------------------+----------------------------------------+
|                                          [确认复制选中处方] [取消]  |
+---------------------------------------------------------------------+
```

### 核心改进

1. **左右双栏布局**
   - 左侧: 历史医案列表（卡片式）+ 搜索筛选
   - 右侧: 医案详情预览（复用MedicalCaseViewControl）

2. **增强搜索功能**
   - 默认显示当前患者的历史医案
   - 模糊查询支持（患者姓名 + 中医诊断）
   - 时间区间筛选（起始日期 - 结束日期）

3. **历史医案卡片列表设计**（三维度信息展示）
   ```
   +--------------------------------------------------+
   | 患者维度: 患者姓名 + 就诊日期                      |
   | 张三                           2024-12-01        |
   +--------------------------------------------------+
   | 诊断维度: 中医诊断（核心信息）                     |
   | 风寒感冒                                          |
   +--------------------------------------------------+
   | 处方维度: 药材数量 + 剂数                         |
   | 8味药材 | 3剂                     [已诊疗][已开方] |
   +--------------------------------------------------+
   ```

   **卡片三维度设计**:
   - **患者维度**: 患者姓名 + 就诊日期（定位历史记录）
   - **诊断维度**: 中医诊断（核心参考信息）
   - **处方维度**: 药材数量 + 剂数 + 状态标签

   **交互设计**:
   - 悬停反馈: 鼠标悬停时卡片边框变为主题色
   - 选中状态: 选中卡片背景色变为淡蓝色，边框加粗

   **导入说明**:
   - 预览: 右侧显示完整医案详情（诊断+处方信息供参考）
   - 导入: 仅导入**药材组合**到当前医案的处方中

4. **详情面板（复用MedicalCaseViewControl）**
   - 直接复用`MedicalCaseViewControl`控件
   - 控件已包含完整的医案详情展示:
     - 基本信息卡片（患者、医生、创建时间、状态）
     - 诊疗信息卡片（主诉、现病史、既往史、诊断、治疗方案）
     - 处方信息卡片（处方编号、类型、剂数、总价、用法用量、处方明细DataGrid）
   - 通过DependencyProperty传递`MedicalCaseDetail`对象

## UI设计参考

本设计参考了医疗健康行业的优秀UI/UX实践案例:

### Master-Detail模式最佳实践

**模式特点**:
- 左侧列表提供快速浏览和筛选
- 右侧详情面板提供深度信息展示
- 选中项与详情面板实时同步

**实现要点**:
- 列表项保持信息密度适中，避免过载
- 详情面板提供完整上下文信息
- 支持键盘导航（上下键切换选中项）

### 患者记录列表设计原则

**信息层级** (来自Koru UX医疗UI研究):
1. **主标识**: 患者姓名/日期（快速定位）
2. **核心内容**: 诊断信息（决策依据）
3. **辅助信息**: 处方摘要/状态标签（补充参考）

**视觉设计**:
- 卡片式布局提升可读性
- 状态标签使用色彩编码（绿色=已完成，蓝色=进行中）
- 悬停/选中状态提供明确视觉反馈

**搜索筛选** (来自EHR+设计研究):
- 多关键词模糊搜索（患者+诊断组合）
- 时间范围快速筛选（日期选择器）
- 实时筛选结果计数显示

### 交互设计原则

**即时反馈**:
- 选中列表项后0.3s内加载详情
- 加载状态显示骨架屏或进度指示
- 筛选条件变更立即更新列表

**信息可扫描性**:
- 关键信息（诊断、药材数）突出显示
- 使用视觉标签（Tag）替代纯文本描述
- 列表项高度统一，便于快速浏览

### 本设计采纳的实践

| 实践 | 应用位置 |
|------|----------|
| Master-Detail双栏布局 | 整体对话框结构 |
| 三维度卡片信息展示 | 左栏历史医案列表 |
| 时间区间筛选器 | 搜索筛选区 |
| 状态色彩编码标签 | 卡片底部状态标签 |
| 复用详情展示控件 | 右栏MedicalCaseViewControl |

## Impact Analysis

### 影响范围

| 类型 | 文件 | 变更 |
|------|------|------|
| XAML | HistoryCopyDialog.xaml | 完全重写，新布局 |
| ViewModel | HistoryCopyDialogViewModel.cs | 增加时间筛选、详情加载 |
| DTO | - | 复用现有MedicalCaseDto |

### 依赖规范

- `dialog-patterns/spec.md`: Prism IDialogAware模式
- `extract-detail-controls/spec.md`: MedicalCaseViewControl组件

## Technical Design

### UI层变更

**HistoryCopyDialog.xaml**:
- 对话框尺寸调整为 1100x680（与FormulaImportDialog一致）
- 左右Grid双栏布局 (320:*)
- 左栏: StackPanel + ListBox (卡片列表)
- 右栏: 复用MedicalCaseViewControl控件

**历史医案卡片模板XAML示例**:
```xml
<DataTemplate x:Key="HistoryCaseCardTemplate">
    <Border Style="{StaticResource HistoryCaseCardStyle}">
        <StackPanel>
            <!-- 患者维度: 患者姓名 + 就诊日期 -->
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0"
                           Text="{Binding PatientName}"
                           FontSize="13"
                           FontWeight="SemiBold"
                           Foreground="{StaticResource PrimaryTextBrush}" />
                <TextBlock Grid.Column="1"
                           Text="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd}}"
                           FontSize="12"
                           Foreground="{StaticResource SecondaryTextBrush}" />
            </Grid>

            <!-- 诊断维度: 中医诊断（核心信息） -->
            <TextBlock Text="{Binding Diagnosis, TargetNullValue=暂无诊断}"
                       FontSize="14"
                       FontWeight="Medium"
                       TextTrimming="CharacterEllipsis"
                       Margin="0,6,0,0" />

            <!-- 处方维度: 药材数量 + 剂数 + 状态标签 -->
            <Grid Margin="0,6,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>

                <!-- 处方摘要标签 -->
                <StackPanel Grid.Column="0" Orientation="Horizontal"
                            Visibility="{Binding HasPrescription, Converter={StaticResource BoolToVis}}">
                    <Border Background="#E8F5E9" CornerRadius="3" Padding="6,2">
                        <TextBlock FontSize="11" Foreground="#4CAF50">
                            <Run Text="{Binding HerbCount}" /><Run Text="味药材" />
                        </TextBlock>
                    </Border>
                    <Border Background="#FFF3E0" CornerRadius="3" Padding="6,2" Margin="6,0,0,0">
                        <TextBlock FontSize="11" Foreground="#FF9800">
                            <Run Text="{Binding Quantity}" /><Run Text="剂" />
                        </TextBlock>
                    </Border>
                </StackPanel>

                <!-- 状态标签 -->
                <StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Border Background="{StaticResource SuccessBrush}"
                            CornerRadius="3"
                            Padding="6,2"
                            Visibility="{Binding HasConsultation, Converter={StaticResource BoolToVis}}">
                        <TextBlock Text="已诊疗" FontSize="10" Foreground="White" />
                    </Border>
                    <Border Background="{StaticResource InfoBrush}"
                            CornerRadius="3"
                            Padding="6,2"
                            Margin="4,0,0,0"
                            Visibility="{Binding HasPrescription, Converter={StaticResource BoolToVis}}">
                        <TextBlock Text="已开方" FontSize="10" Foreground="White" />
                    </Border>
                </StackPanel>
            </Grid>
        </StackPanel>
    </Border>
</DataTemplate>
```

**右栏详情区域（复用MedicalCaseViewControl）**:
```xml
<!-- 右栏: 医案详情预览 -->
<Grid Grid.Column="1" Margin="10">
    <!-- 无选中时的提示 -->
    <TextBlock Text="请从左侧选择一个历史医案"
               FontSize="14"
               Foreground="#999"
               HorizontalAlignment="Center"
               VerticalAlignment="Center"
               Visibility="{Binding SelectedCaseDetail, Converter={StaticResource NullToCollapsedConverter}}" />

    <!-- 有选中时显示详情 - 直接复用MedicalCaseViewControl -->
    <ScrollViewer VerticalScrollBarVisibility="Auto"
                  HorizontalScrollBarVisibility="Disabled"
                  Visibility="{Binding SelectedCaseDetail, Converter={StaticResource NullToVisibilityConverter}}">
        <medicalCaseControls:MedicalCaseViewControl
            MedicalCaseDetail="{Binding SelectedCaseDetail}"
            HasConsultation="{Binding SelectedCaseHasConsultation}"
            HasPrescription="{Binding SelectedCaseHasPrescription}"/>
    </ScrollViewer>
</Grid>
```

### ViewModel层变更

**HistoryCopyDialogViewModel.cs**:
```csharp
// === 新增属性 ===

// 时间区间筛选
public DateTime? StartDate { get; set; }  // 起始日期
public DateTime? EndDate { get; set; }    // 结束日期

// 选中医案详情（用于右侧MedicalCaseViewControl绑定）
public MedicalCaseDetailDto? SelectedCaseDetail { get; private set; }
public bool SelectedCaseHasConsultation => SelectedCaseDetail?.ConsultationDate != null;
public bool SelectedCaseHasPrescription => SelectedCaseDetail?.PrescriptionItems?.Any() == true;

// === 修改属性 ===

// SearchText: 支持模糊查询（患者姓名 + 中医诊断）
// 原有仅支持单一关键词，修改为同时匹配PatientName和Diagnosis字段

// === 新增方法 ===

/// <summary>
/// 综合筛选方法 - 结合关键词和时间区间
/// </summary>
private void FilterCases()
{
    var filtered = _allCases.AsEnumerable();

    // 关键词筛选（患者姓名 OR 中医诊断）
    if (!string.IsNullOrWhiteSpace(SearchText))
    {
        var keyword = SearchText.Trim().ToLower();
        filtered = filtered.Where(c =>
            (c.PatientName?.ToLower().Contains(keyword) == true) ||
            (c.Diagnosis?.ToLower().Contains(keyword) == true));
    }

    // 时间区间筛选
    if (StartDate.HasValue)
        filtered = filtered.Where(c => c.CreatedAt >= StartDate.Value);
    if (EndDate.HasValue)
        filtered = filtered.Where(c => c.CreatedAt <= EndDate.Value.AddDays(1));

    FilteredCases = new ObservableCollection<MedicalCaseSummaryDto>(filtered);
    StatusMessage = $"共 {FilteredCases.Count} 条历史医案";
}

/// <summary>
/// 加载选中医案的完整详情（用于右侧预览）
/// </summary>
private async Task LoadCaseDetailAsync(Guid caseId)
{
    IsLoading = true;
    try
    {
        SelectedCaseDetail = await _medicalCaseService.GetDetailAsync(caseId);
        RaisePropertyChanged(nameof(SelectedCaseHasConsultation));
        RaisePropertyChanged(nameof(SelectedCaseHasPrescription));
    }
    finally
    {
        IsLoading = false;
    }
}
```

### 样式资源

复用现有资源:
- `CustomDialogWindowStyle` (DialogStyles.xaml)
- `HerbCardControl` (Formula模块)
- `PrimaryBrush` (主题色)
- `FormulaCardStyle` (FormulaImportDialog中定义)

## Acceptance Criteria

1. [AC-1] 对话框显示左右双栏布局，左侧历史医案列表，右侧详情预览
2. [AC-2] 默认显示当前患者的历史医案列表
3. [AC-3] 搜索框支持患者姓名和中医诊断模糊查询
4. [AC-4] 时间区间筛选（起始日期-结束日期）能正确过滤处方列表
5. [AC-5] 选中处方后右侧显示完整详情和药材卡片
6. [AC-6] 点击确认复制后正确返回选中处方的药材列表
7. [AC-7] 对话框遵循CustomDialogWindowStyle样式规范

## Risks and Mitigations

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 药材卡片在对话框中性能问题 | 大量药材时可能卡顿 | 启用虚拟化，限制单次显示数量 |
| 历史医案数据量大 | 加载缓慢 | 使用虚拟化列表，默认只显示近3个月 |

## Spec Deltas

### 新增规范

无需新增规范文件，本次变更在dialog-patterns规范框架内。

### 规范更新

无

## Related Issues

- Issue #2246: 历史医案复制弹窗（原始实现）
- OpenSpec: redesign-formula-import-ui（参考模式）
- OpenSpec: extract-detail-controls（复用组件）

---
创建时间: 2025-12-13
状态: Draft
