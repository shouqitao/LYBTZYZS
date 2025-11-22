# FormulaDetailView药材选择Bug修复需求文档

**文档编号**: REQ-Formula-Bug-Fix-001
**创建日期**: 2025-01-18
**版本**: v1.0
**状态**: ✅ 已确认
**优先级**: P0（核心功能Bug）
**预估工作量**: 0.5天
**相关Issue**: #2149
**前置分析**: ANALYSIS-Formula-Editing-2025-01-18

---

## 📋 执行摘要

### 需求背景
FormulaDetailView药材编辑区在Issue #2149中已完成8个核心功能（FR-001~FR-008）的实现，但发现FR-006（药材信息自动填充）存在Bug：用户选择药材后，HerbId、HerbName、Unit未能正确填充到FormulaHerbItemViewModel。

### 核心问题
- **Bug描述**: HerbSelectedCommand实现错误，导致药材库关联失效
- **影响范围**: 所有药材选择操作
- **严重级别**: P0（核心功能完全失效）

### 用户确认结果
经过三个问题的逐一确认，明确了以下需求：

| 确认项 | 用户选择 | 说明 |
|-------|---------|------|
| **Bug修复方案** | 方案B（ViewModel内部处理） | 架构更优雅，符合MVVM原则 |
| **测试范围** | 完整回归测试 | 测试所有FR-001~FR-008功能 |
| **实施范围** | 仅修复Bug | 不实施FR-012、FR-017等扩展功能 |

### 补充说明
- 药材库中Unit字段为**必填项**，不会为空
- 单位自动关联功能修复后即可正常工作
- "总共X味药"统计功能已实现，无需额外开发
- 总剂量、平均剂量统计功能**不需要**

---

## 第1章：Bug详细分析

### 1.1 问题描述

**用户操作流程**:
1. 用户在HerbCardControl的ComboBox中输入拼音码（如"dg"）
2. FilteredHerbs显示匹配的药材列表（如"当归"）
3. 用户选择"当归"
4. **预期结果**: HerbId、HerbName、Unit自动填充
5. **实际结果**: ❌ 信息未填充，药材库关联失效

### 1.2 根因分析

**问题代码位置**: `FormulaDetailViewModel.cs:867-895`

```csharp
private void OnHerbSelected(HerbDto? selectedHerb)
{
    if (selectedHerb == null || !IsEditMode)
        return;

    // ❌ 错误的查找逻辑
    var currentItem = HerbItems.FirstOrDefault(h =>
        h.HerbId == selectedHerb.Id ||  // 问题1: 会找到已有相同药材
        (string.IsNullOrEmpty(h.HerbName) && h.HerbId == Guid.Empty));  // 问题2: 找不到正在编辑的项

    if (currentItem != null)
    {
        currentItem.HerbId = selectedHerb.Id;
        currentItem.HerbName = selectedHerb.Name ?? string.Empty;
        currentItem.Unit = selectedHerb.Unit ?? "g";  // Unit为必填项，?? "g"不会执行
    }
}
```

**架构缺陷**:
1. **违反单一职责**: FormulaDetailViewModel不应该管理子项的选择逻辑
2. **缺少上下文**: Command只传递selectedHerb，无法知道是哪个HerbItem在操作
3. **查找逻辑错误**: 无法正确定位当前正在编辑的HerbItem

### 1.3 影响范围
- ✅ 拼音码过滤功能正常（FilterHerbs()未受影响）
- ✅ 焦点管理功能正常
- ✅ 重复检测功能正常
- ❌ **药材选择后自动填充失效**（核心功能Bug）

---

## 第2章：修复方案（方案B）

### 2.1 方案选择理由

经架构分析，选择**方案B（ViewModel内部处理）**：

**架构优势**:
1. ✅ **单一职责**: FormulaHerbItemViewModel自己管理自己的状态
2. ✅ **类型安全**: 强类型属性，编译时检查
3. ✅ **低耦合**: 父ViewModel不需要介入子项的选择逻辑
4. ✅ **纯MVVM**: 利用WPF双向绑定，符合设计理念
5. ✅ **代码简洁**: 移除不必要的Command和事件处理

**对比方案A的劣势**:
- ❌ 类型不安全（匿名对象 + 反射）
- ❌ 违反单一职责原则
- ❌ 父子ViewModel耦合度高

### 2.2 技术实现方案

#### 修改1: FormulaHerbItemViewModel.cs添加SelectedHerb属性

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaHerbItemViewModel.cs`

**添加私有字段**（行27后）:
```csharp
private HerbDto? _selectedHerb;
```

**添加公共属性**（行111后，UnitPrice属性之后）:
```csharp
/// <summary>
/// 选中的药材 - 自动填充HerbId、HerbName、Unit
/// Issue #2149 Bug修复: 通过双向绑定自动触发药材信息填充
/// </summary>
public HerbDto? SelectedHerb
{
    get => _selectedHerb;
    set
    {
        if (SetProperty(ref _selectedHerb, value) && value != null)
        {
            // 自动填充药材信息
            HerbId = value.Id;
            HerbName = value.Name ?? string.Empty;
            Unit = value.Unit;  // Unit为必填项，不需要 ?? "g"

            Logger.LogInformation("选择药材: {HerbName}, 单位: {Unit}",
                value.Name, value.Unit);
        }
    }
}
```

**设计说明**:
- 利用WPF双向绑定自动触发
- SetProperty触发PropertyChanged，更新UI
- 直接使用`value.Unit`，因为药材库Unit为必填项

#### 修改2: HerbCardControl.xaml修改ComboBox绑定

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/HerbCardControl.xaml:22-43`

**修改前**:
```xaml
<ComboBox x:Name="HerbNameComboBox"
          Grid.Column="0"
          Margin="0,0,8,0"
          IsEditable="True"
          IsTextSearchEnabled="False"
          StaysOpenOnEdit="True"
          Text="{Binding HerbName, UpdateSourceTrigger=PropertyChanged}"
          ItemsSource="{Binding FilteredHerbs}"
          DisplayMemberPath="Name"
          SelectedValuePath="Id"
          SelectedValue="{Binding HerbId, Mode=TwoWay}"
          PreviewKeyDown="OnHerbNameKeyDown"
          SelectionChanged="OnHerbNameSelectionChanged"
          ...>
```

**修改后**:
```xaml
<ComboBox x:Name="HerbNameComboBox"
          Grid.Column="0"
          Margin="0,0,8,0"
          IsEditable="True"
          IsTextSearchEnabled="False"
          StaysOpenOnEdit="True"
          Text="{Binding HerbName, UpdateSourceTrigger=PropertyChanged}"
          ItemsSource="{Binding FilteredHerbs}"
          DisplayMemberPath="Name"
          SelectedItem="{Binding SelectedHerb, Mode=TwoWay}"
          PreviewKeyDown="OnHerbNameKeyDown"
          ...>
```

**关键变更**:
- ❌ 移除 `SelectedValuePath="Id"`
- ❌ 移除 `SelectedValue="{Binding HerbId, Mode=TwoWay}"`
- ❌ 移除 `SelectionChanged="OnHerbNameSelectionChanged"`
- ✅ 添加 `SelectedItem="{Binding SelectedHerb, Mode=TwoWay}"`

#### 修改3: HerbCardControl.xaml.cs移除SelectionChanged事件

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/HerbCardControl.xaml.cs`

**删除方法**（行123-137）:
```csharp
/// <summary>
/// 药材选择变更事件
/// 自动填充单位等信息
/// </summary>
private void OnHerbNameSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (HerbNameComboBox.SelectedItem is HerbDto selectedHerb)
    {
        // 触发药材选择完成命令（ViewModel层处理数据填充）
        if (HerbSelectedCommand?.CanExecute(selectedHerb) == true)
        {
            HerbSelectedCommand.Execute(selectedHerb);
        }
    }
}
```

**删除DependencyProperty**（行66-79）:
```csharp
/// <summary>
/// 药材选择完成命令（用于自动填充单位等信息）
/// </summary>
public static readonly DependencyProperty HerbSelectedCommandProperty =
    DependencyProperty.Register(
        nameof(HerbSelectedCommand),
        typeof(ICommand),
        typeof(HerbCardControl),
        new PropertyMetadata(null));

public ICommand? HerbSelectedCommand
{
    get => (ICommand?)GetValue(HerbSelectedCommandProperty);
    set => SetValue(HerbSelectedCommandProperty, value);
}
```

#### 修改4: FormulaDetailView.xaml移除Command绑定

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaDetailView.xaml:230-235`

**修改前**:
```xaml
<controls:HerbCardControl
    IsEditMode="{Binding DataContext.IsEditMode, RelativeSource={RelativeSource AncestorType=UserControl}}"
    DeleteCommand="{Binding DataContext.DeleteHerbCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
    DosageCompletedCommand="{Binding DataContext.DosageCompletedCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
    HerbSelectedCommand="{Binding DataContext.HerbSelectedCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
```

**修改后**:
```xaml
<controls:HerbCardControl
    IsEditMode="{Binding DataContext.IsEditMode, RelativeSource={RelativeSource AncestorType=UserControl}}"
    DeleteCommand="{Binding DataContext.DeleteHerbCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
    DosageCompletedCommand="{Binding DataContext.DosageCompletedCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
```

**变更**: 移除 `HerbSelectedCommand` 绑定

#### 修改5: FormulaDetailViewModel.cs移除HerbSelectedCommand

**位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs`

**删除命令声明**（行48-49）:
```csharp
public DelegateCommand<HerbDto> HerbSelectedCommand { get; }
```

**删除构造函数初始化**（约行274）:
```csharp
HerbSelectedCommand = new DelegateCommand<HerbDto>(OnHerbSelected);
```

**删除命令实现**（行864-895）:
```csharp
/// <summary>
/// 药材选择完成命令实现（自动填充单位）
/// </summary>
private void OnHerbSelected(HerbDto? selectedHerb)
{
    if (selectedHerb == null || !IsEditMode)
        return;

    try
    {
        // 查找当前正在编辑的HerbItem
        var currentItem = HerbItems.FirstOrDefault(h =>
            h.HerbId == selectedHerb.Id ||
            (string.IsNullOrEmpty(h.HerbName) && h.HerbId == Guid.Empty));

        if (currentItem != null)
        {
            // 自动填充药材信息
            currentItem.HerbId = selectedHerb.Id;
            currentItem.HerbName = selectedHerb.Name ?? string.Empty;
            currentItem.Unit = selectedHerb.Unit ?? "g";

            Logger.LogInformation("选择药材: {HerbName}, 单位: {Unit}",
                selectedHerb.Name, selectedHerb.Unit);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "药材选择处理时发生异常");
        _ = ShowErrorMessageAsync("处理药材选择失败");
    }
}
```

---

## 第3章：测试验收标准

### 3.1 完整回归测试清单（FR-001 ~ FR-008）

#### FR-001: 拼音码快速输入 ✅
- [ ] 输入"dg"自动匹配"当归"
- [ ] 输入"当"自动匹配"当归"
- [ ] 匹配结果按分数降序排列（名称完全匹配>拼音码完全匹配>前缀匹配...）
- [ ] 最多显示5个匹配结果
- [ ] FilteredHerbs实时响应HerbName变更

#### FR-002: 4列卡片布局 ✅
- [ ] 药材以4列UniformGrid排列
- [ ] 超过4个药材自动换行
- [ ] 支持垂直滚动

#### FR-003: 键盘快捷键 ✅
- [ ] ComboBox中Enter键跳转到剂量输入框
- [ ] 剂量输入框中Enter键跳转到下一个药材ComboBox
- [ ] Shift+Delete删除当前药材
- [ ] 剂量输入框获得焦点时自动全选

#### FR-004: 重复药材检测 ✅
- [ ] 同一药材重复添加时自动检测（基于HerbId）
- [ ] 合并后保留较大剂量
- [ ] 删除重复项
- [ ] 弹窗提示用户合并结果

#### FR-005: 空槽位管理 ✅
- [ ] 始终保持至少4个空槽位（HerbId == Guid.Empty）
- [ ] 删除药材后自动前移
- [ ] 剂量输入完成后自动调整空槽位数量

#### FR-006: 药材信息自动填充 ⚠️ **本次修复重点**
- [ ] **选择药材后HerbId正确填充**
- [ ] **选择药材后HerbName正确显示**
- [ ] **选择药材后Unit从药材库自动填充**
- [ ] 多个槽位分别选择不同药材，互不干扰
- [ ] 选择相同药材到不同槽位，重复检测正常工作

#### FR-007: 只读/编辑模式 ✅
- [ ] 编辑模式下所有控件可操作
- [ ] 查看模式下所有控件禁用
- [ ] IsEditMode状态正确传递到HerbCardControl

#### FR-008: 焦点管理（水平优先） ✅
- [ ] Enter键跳转到下一个药材ComboBox（水平优先）
- [ ] 遍历顺序：药材1→药材2→药材3→药材4→药材5...
- [ ] 到达最后一个槽位后不再跳转

### 3.2 核心验收标准

#### 必须通过（P0）
1. ✅ 选择药材后HerbId、HerbName、Unit正确填充
2. ✅ 单位值来自药材库HerbDto.Unit字段
3. ✅ 拼音码过滤正常工作
4. ✅ 重复检测正常工作

#### 应该通过（P1）
1. ✅ 所有FR-001~FR-008功能正常
2. ✅ 无编译警告和错误
3. ✅ 无运行时异常

---

## 第4章：实施计划

### 4.1 开发任务清单

| 任务 | 文件 | 预估时间 |
|------|------|---------|
| 1. 添加SelectedHerb属性 | FormulaHerbItemViewModel.cs | 30分钟 |
| 2. 修改ComboBox绑定 | HerbCardControl.xaml | 15分钟 |
| 3. 移除事件和Command | HerbCardControl.xaml.cs | 15分钟 |
| 4. 移除Command绑定 | FormulaDetailView.xaml | 5分钟 |
| 5. 移除Command实现 | FormulaDetailViewModel.cs | 15分钟 |
| 6. 编译验证 | - | 15分钟 |
| 7. 完整回归测试 | - | 60分钟 |
| 8. 提交代码 | - | 15分钟 |
| **总计** | - | **2.5小时** |

### 4.2 验证流程

#### 步骤1: 编译验证
```bash
dotnet build LYBT.Desktop.Formula.csproj -c Debug --no-restore
```
**预期结果**: 0 errors, 0 warnings

#### 步骤2: 单元测试（如存在）
```bash
dotnet test LYBT.Desktop.Formula.Tests.csproj
```

#### 步骤3: 手动功能测试
按照3.1章节的测试清单逐项验证

---

## 第5章：技术约束

### 5.1 架构约束
- ✅ 遵循MVVM模式
- ✅ 遵循单一职责原则
- ✅ 使用WPF双向绑定机制
- ✅ 不引入新的第三方依赖

### 5.2 代码规范
- ✅ XML文档注释完整
- ✅ 中文注释清晰
- ✅ 符合.NET命名规范
- ✅ 使用Logger记录关键操作

### 5.3 兼容性要求
- ✅ 不影响现有FR-001~FR-008功能
- ✅ 不改变API接口
- ✅ 不改变数据模型

---

## 第6章：风险评估

### 6.1 技术风险

| 风险 | 可能性 | 影响 | 缓解措施 |
|------|-------|------|---------|
| SelectedItem绑定失效 | 低 | 高 | 完整回归测试验证 |
| 重复检测受影响 | 低 | 中 | 测试多种场景 |
| 焦点管理受影响 | 低 | 中 | 测试键盘导航 |

### 6.2 回退方案
如果修复后出现问题，可回退到修复前版本（当前commit: 2519f6331）

---

## 第7章：交付物

### 7.1 代码交付
- [ ] FormulaHerbItemViewModel.cs（修改）
- [ ] HerbCardControl.xaml（修改）
- [ ] HerbCardControl.xaml.cs（修改）
- [ ] FormulaDetailView.xaml（修改）
- [ ] FormulaDetailViewModel.cs（修改）

### 7.2 文档交付
- [ ] 本需求文档
- [ ] Git Commit Message（描述清晰）
- [ ] Graphiti记忆更新（Bug修复记录）

### 7.3 测试报告
- [ ] 完整回归测试清单（已通过）
- [ ] 核心功能验证截图（可选）

---

## 第8章：后续规划

### 8.1 不在本次范围内
- ❌ FR-012: 总剂量、平均剂量统计（用户不需要）
- ❌ FR-017: 单位标准化下拉选择（单位自动关联即可）
- ❌ FR-009~FR-018: 其他扩展功能

### 8.2 未来可选功能
根据用户后续反馈，可考虑：
- FR-009: 方剂模板管理
- FR-011: 配伍禁忌检查
- FR-014: 批量导入药材
- FR-018: 自由处方支持

---

## 附录A: 关键代码对比

### 修改前（Bug代码）
```csharp
// FormulaDetailViewModel.cs
private void OnHerbSelected(HerbDto? selectedHerb)
{
    var currentItem = HerbItems.FirstOrDefault(h =>
        h.HerbId == selectedHerb.Id ||  // ❌ 错误逻辑
        (string.IsNullOrEmpty(h.HerbName) && h.HerbId == Guid.Empty));

    currentItem.Unit = selectedHerb.Unit ?? "g";
}
```

### 修改后（正确代码）
```csharp
// FormulaHerbItemViewModel.cs
public HerbDto? SelectedHerb
{
    get => _selectedHerb;
    set
    {
        if (SetProperty(ref _selectedHerb, value) && value != null)
        {
            HerbId = value.Id;
            HerbName = value.Name ?? string.Empty;
            Unit = value.Unit;  // ✅ 直接使用，Unit为必填项
        }
    }
}
```

---

## 附录B: 参考文档

| 文档名称 | 路径 |
|---------|------|
| 分析报告 | ANALYSIS-Formula-Editing-2025-01-18 |
| 综合需求 | docs/requirements/formula-editing-area-comprehensive-requirements.md |
| Issue #2149 | https://github.com/shouqitao/LYBTZYZS/issues/2149 |

---

**文档状态**: ✅ 已确认，可开始实施
**下一步**: 根据本需求文档实施代码修改
**预计完成时间**: 0.5天（约4小时）
