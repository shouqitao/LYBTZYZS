# 验方编辑区域增强需求文档 v2.0

**创建日期**: 2025-11-11
**更新日期**: 2025-11-11
**文档状态**: 已确定方案
**关联Epic**: Phase 2 - Formula CRUD Enhancement
**参考设计**: Prescription 8列编辑区域设计
**选定方案**: 方案A - 8列DataGrid布局

---

## 📌 重要修正

- ✅ **验方不涉及价格计算**（已移除所有价格相关需求）
- ✅ **选择方案A**：8列DataGrid布局（与处方一致）
- ✅ **新增功能**：复制验方（当前阶段完成）

---

## 1. 需求背景

### 1.1 现状分析

**验方当前编辑区域（FormulaDetailView.xaml:314-348）**：

```xml
<!-- 当前实现：传统DataGrid - 只读展示型 -->
<DataGrid Grid.Row="1"
          ItemsSource="{Binding HerbItems}"
          AutoGenerateColumns="False"
          CanUserAddRows="False"
          CanUserDeleteRows="False"
          IsReadOnly="{Binding IsReadOnly}"
          GridLinesVisibility="Horizontal"
          HeadersVisibility="Column"
          SelectionMode="Single">
    <DataGrid.Columns>
        <DataGridTextColumn Header="序号" Binding="{Binding SortOrder}" Width="60" />
        <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}" Width="120" />
        <DataGridTextColumn Header="用量" Binding="{Binding Quantity, StringFormat=F1}" Width="80" />
        <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60" />
        <DataGridTextColumn Header="炮制方法" Binding="{Binding Preparation}" Width="100" />
        <DataGridTextColumn Header="用法" Binding="{Binding Usage}" Width="*" />
    </DataGrid.Columns>
</DataGrid>
```

**问题识别**：
- ❌ **纯展示型**：DataGridTextColumn，无法快速编辑
- ❌ **无快速录入**：缺少拼音码快速选择药材功能
- ❌ **无操作按钮**：缺少"添加药材"、"删除药材"、"复制验方"等操作
- ❌ **单一模式**：通过IsReadOnly绑定控制，但编辑模式下仍是纯文本列

### 1.2 处方参考实现（PrescriptionView.xaml:199-319）

**核心特性**：
1. ✅ **8列DataGrid布局**（4个药材+4个用量）：横向快速录入
2. ✅ **ComboBox拼音码过滤**：`IsEditable=true`，支持拼音码快速定位
3. ✅ **焦点自动跳转**：用量输入完成后自动跳转到下一个药材
4. ✅ **丰富操作按钮**：添加药材、导入验方、清空处方
5. ✅ **HerbFilterManager**：拼音码过滤管理器

**处方8列布局示例**：
```xml
<!-- 药材1 + 用量1 + 药材2 + 用量2 + 药材3 + 用量3 + 药材4 + 用量4 -->
<DataGridTemplateColumn Header="药材1" Width="2*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <ComboBox IsEditable="True"
                     ItemsSource="{Binding DataContext.FilteredHerbs, RelativeSource={RelativeSource AncestorType=UserControl}}"
                     DisplayMemberPath="Name"
                     Text="{Binding Item1.HerbName, UpdateSourceTrigger=PropertyChanged}"
                     BorderThickness="0" Background="Transparent"
                     Loaded="HerbComboBox_Loaded" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>

<DataGridTemplateColumn Header="用量1" Width="1*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Item1.Quantity, UpdateSourceTrigger=PropertyChanged}"
                    BorderThickness="0" Background="Transparent"
                    PreviewKeyDown="QuantityTextBox_PreviewKeyDown" />
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

---

## 2. 需求目标

### 2.1 核心目标

**为验方模块增加快速药材编辑功能**，采用8列DataGrid布局（方案A），实现：

1. ✅ **快速药材录入**：ComboBox拼音码过滤 + 焦点自动跳转
2. ✅ **双模式支持**：查看模式（只读）vs 编辑模式（可编辑）
3. ✅ **丰富操作按钮**：添加/删除药材、导入验方、清空验方、**复制验方**
4. ✅ **与处方一致**：用户体验统一，学习成本低

### 2.2 非目标

- ❌ **不改变现有数据模型**：FormulaDto、FormulaHerbItemDto 保持不变
- ❌ **不改变导航逻辑**：沿用现有 ReadOnly 参数传递机制
- ❌ **不实现价格计算**：验方不涉及价格
- ❌ **暂不实现重复校验**：名称、组成重复校验为后期工作

---

## 3. 详细需求

### 3.1 UI布局方案：8列DataGrid（方案A）

**布局示例**：
```
┌────────────── 药材组成 ──────────────┐
│ [添加药材] [导入验方] [清空验方]                                           │
│                                                                            │
│ ┌─────────────────────────────────────────────────────────────────┐      │
│ │ 药材1  │用量1│ 药材2  │用量2│ 药材3  │用量3│ 药材4  │用量4│      │      │
│ ├────────┼────┼────────┼────┼────────┼────┼────────┼────┤      │      │
│ │[桂枝▼] │ 9  │[白芍▼] │ 9  │[生姜▼] │ 9  │[大枣▼] │ 3  │      │      │
│ │[甘草▼] │ 6  │        │    │        │    │        │    │      │      │
│ │        │    │        │    │        │    │        │    │      │      │
│ └────────┴────┴────────┴────┴────────┴────┴────────┴────┘      │      │
│                                                                            │
│ 说明：                                                                     │
│ • 在"药材1"下拉框输入拼音码（如"gz"）快速定位"桂枝"                       │
│ • 输入用量后按Enter或Tab键，焦点自动跳到"药材2"                          │
│ • 一行可录入4味药材，适合快速连续录入                                     │
└──────────────────────────────────────┘
```

**录入流程**：
```
步骤1: 点击"药材1"下拉框
    ↓
步骤2: 输入拼音码"gz"，下拉框过滤显示"桂枝"、"甘草"等
    ↓
步骤3: 选择"桂枝"
    ↓
步骤4: 在"用量1"输入"9"，按Enter
    ↓
步骤5: 焦点自动跳转到"药材2"（继续录入下一味药）
    ↓
步骤6: 重复步骤2-5，一行录入4味药材
```

---

### 3.2 交互功能需求

#### **3.2.1 拼音码快速选择（参考 PrescriptionView.xaml:209-221）**

**需求描述**：
- 用户在ComboBox中输入拼音码首字母，自动过滤匹配的药材
- 例如输入"hq"自动过滤出"黄芪"、"黄芩"等

**技术实现**（参考处方）：
```csharp
// FormulaHerbFilterManager.cs（参考 HerbFilterManager）
public class FormulaHerbFilterManager
{
    public ObservableCollection<HerbDto> FilteredHerbs { get; set; }

    public void FilterByPinYin(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            FilteredHerbs = AllHerbs;
            return;
        }

        FilteredHerbs = new ObservableCollection<HerbDto>(
            AllHerbs.Where(h => h.PinYinCode.StartsWith(input, StringComparison.OrdinalIgnoreCase))
        );
    }
}
```

**验方需求**：
- ✅ 创建 `FormulaHerbFilterManager`（参考 HerbFilterManager）
- ✅ 绑定 `FilteredHerbs` 到 ComboBox.ItemsSource
- ✅ 监听 ComboBox.TextChanged 事件，实时过滤

---

#### **3.2.2 焦点自动跳转（参考 PrescriptionView.xaml:228-230）**

**需求描述**：
- 用量输入完成后（按Enter或Tab），焦点自动跳转到下一个药材ComboBox
- 提高录入效率，减少鼠标操作

**技术实现**（参考处方）：
```csharp
// FormulaDetailView.xaml.cs
private void QuantityTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.Enter || e.Key == Key.Tab)
    {
        e.Handled = true;

        // 移动焦点到下一个ComboBox
        var textBox = sender as TextBox;
        // ... 查找下一个ComboBox并设置焦点
    }
}
```

**验方需求**：
- ✅ 在 FormulaDetailView.xaml.cs 实现 `QuantityTextBox_PreviewKeyDown`
- ✅ 焦点跳转逻辑：用量1 → 药材2 → 用量2 → 药材3 → ...

---

#### **3.2.3 操作按钮（参考 PrescriptionView.xaml:165-195）**

**需求描述**：
- **添加药材**：打开 HerbSelectionDialog，选择药材并设置用量
- **导入验方**：打开验方选择对话框，导入另一个验方的药材列表
- **清空验方**：清空当前所有药材（需确认对话框）
- **删除药材**：删除选中的药材行

**技术实现**（参考处方）：
```csharp
// FormulaDetailViewModel.cs
public DelegateCommand AddHerbCommand { get; }
public DelegateCommand ImportFormulaCommand { get; }
public DelegateCommand ClearAllCommand { get; }
public DelegateCommand<FormulaHerbItemDto> RemoveHerbCommand { get; }

private async void AddHerb()
{
    var dialog = _container.Resolve<HerbSelectionDialog>();
    var result = await dialog.ShowAsync();

    if (result == DialogResult.OK)
    {
        var selectedHerb = dialog.ViewModel.SelectedHerb;
        var quantity = dialog.ViewModel.Quantity;

        HerbItems.Add(new FormulaHerbItemDto
        {
            HerbId = selectedHerb.Id,
            HerbName = selectedHerb.Name,
            Quantity = quantity,
            Unit = selectedHerb.Unit,
            SortOrder = HerbItems.Count + 1
        });
    }
}
```

**验方需求**：
- ✅ 实现 `AddHerbCommand`（复用 HerbSelectionDialog）
- ✅ 实现 `ImportFormulaCommand`（创建 FormulaSelectionDialog）
- ✅ 实现 `ClearAllCommand`（带确认对话框）
- ✅ 实现 `RemoveHerbCommand`（删除指定药材）

---

#### **3.2.4 复制验方功能（新增）**

**需求描述**：

用户可以复制一个验方（自己的或别人共享的），生成一个新验方，具体行为：

1. **复制来源**：
   - 自己创建的验方
   - 他人共享的验方（IsShared=true）

2. **复制后状态**：
   - 生成新的验方ID（`Guid.NewGuid()`）
   - 创建者变成当前登录用户
   - 创建时间、更新时间重置为当前时间
   - **所有内容保持一致**：名称、拼音码、性味归经、功效、用法、备注、药材组成
   - 进入编辑模式（IsEditMode=true）

3. **操作入口**：
   - 在验方列表页（FormulaManagementView）添加"复制"按钮
   - 在验方详情页（FormulaDetailView）添加"复制验方"按钮

**UI示例（列表页）**：
```
┌─ 验方列表 ─┐
│ 编号 │ 验方名称 │ 创建者 │ 操作                    │
├─────┼─────────┼────────┼────────────────────────┤
│ 001 │ 桂枝汤   │ 张三   │ [查看] [编辑] [复制] [删除] │
│ 002 │ 麻黄汤   │ 李四   │ [查看]        [复制]        │ ← 共享验方，只能查看和复制
└─────┴─────────┴────────┴────────────────────────┘
```

**技术实现**：

```csharp
// FormulaManagementViewModel.cs
public DelegateCommand<FormulaDto> CopyFormulaCommand { get; }

private async void CopyFormula(FormulaDto sourceFormula)
{
    if (sourceFormula == null) return;

    // 1. 读取完整验方信息（包括药材列表）
    var fullFormula = await _queryService.GetByIdAsync(sourceFormula.Id);
    if (fullFormula == null)
    {
        MessageBox.Show("读取验方信息失败");
        return;
    }

    // 2. 创建新验方DTO
    var newFormula = new FormulaDto
    {
        Id = Guid.NewGuid(),  // 新ID
        Name = fullFormula.Name,  // 保持名称一致（后期可能需要校验重复）
        PinYinCode = fullFormula.PinYinCode,
        Property = fullFormula.Property,
        Effect = fullFormula.Effect,
        Usage = fullFormula.Usage,
        Remark = fullFormula.Remark,
        IsShared = false,  // 默认不共享（可由用户后续修改）
        Status = fullFormula.Status,
        CreatedBy = _currentUser.Name,  // 创建者变成当前用户
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now,
        Herbs = fullFormula.Herbs.Select(h => new FormulaHerbItemDto
        {
            Id = Guid.NewGuid(),  // 药材项也生成新ID
            HerbId = h.HerbId,
            HerbName = h.HerbName,
            Quantity = h.Quantity,
            Unit = h.Unit,
            Preparation = h.Preparation,
            Usage = h.Usage,
            SortOrder = h.SortOrder
        }).ToList()
    };

    // 3. 导航到编辑页面（传递新验方数据）
    var parameters = new NavigationParameters
    {
        { "Formula", newFormula },  // 传递完整数据
        { "ReadOnly", false },      // 编辑模式
        { "IsCopy", true }          // 标记为复制操作（可选）
    };
    NavigateTo("ContentRegion", "FormulaDetailView", parameters);
}
```

```csharp
// FormulaDetailViewModel.cs
protected override void ProcessNavigationParameters(NavigationParameters parameters)
{
    base.ProcessNavigationParameters(parameters);

    // 处理复制验方的情况
    if (parameters.ContainsKey("Formula"))
    {
        var copiedFormula = parameters.GetValue<FormulaDto>("Formula");

        // 预填充所有数据
        Formula = copiedFormula;
        FormulaId = copiedFormula.Id;
        FormulaName = copiedFormula.Name;
        PinYinCode = copiedFormula.PinYinCode;
        Property = copiedFormula.Property;
        Effect = copiedFormula.Effect;
        Usage = copiedFormula.Usage;
        Remark = copiedFormula.Remark;
        IsShared = copiedFormula.IsShared;

        // 预填充药材列表
        HerbItems = new ObservableCollection<FormulaHerbItemDto>(copiedFormula.Herbs);

        // 进入编辑模式
        IsEditMode = true;

        // 可选：显示提示信息
        if (parameters.GetValue<bool>("IsCopy"))
        {
            MessageBox.Show("验方已复制，请修改后保存");
        }
    }

    // 原有的导航参数处理逻辑...
}
```

**验收标准**：
- ✅ 列表页点击"复制"按钮，成功进入编辑页面
- ✅ 编辑页面预填充所有数据（基本信息 + 药材列表）
- ✅ 验方ID、创建者、创建时间均为新值
- ✅ 保存后生成新的验方记录
- ✅ 原验方不受影响

**后期工作（暂不实施）**：
- ❌ 名称重复校验（提示用户修改名称）
- ❌ 组成重复校验（提示用户修改药材）
- ❌ 复制时自动在名称后添加"（副本）"后缀

---

### 3.3 查看模式 vs 编辑模式

**需求描述**：
- 查看模式：DataGrid IsReadOnly=true，按钮区隐藏
- 编辑模式：DataGrid IsReadOnly=false，按钮区显示

**技术实现**：
```xml
<!-- FormulaDetailView.xaml -->
<StackPanel Orientation="Horizontal"
            Visibility="{Binding IsEditMode, Converter={StaticResource BooleanToVisibilityConverter}}">
    <Button Content="添加药材" Command="{Binding AddHerbCommand}" />
    <Button Content="导入验方" Command="{Binding ImportFormulaCommand}" />
    <Button Content="清空验方" Command="{Binding ClearAllCommand}" />
</StackPanel>

<DataGrid IsReadOnly="{Binding IsReadOnly}" ... />
```

**验方需求**：
- ✅ FormulaDetailViewModel 已有 `IsEditMode` 和 `IsReadOnly` 属性
- ✅ 按钮区使用 `Visibility` 绑定到 `IsEditMode`
- ✅ DataGrid 保持绑定 `IsReadOnly`

---

### 3.4 数据绑定要求

#### **3.4.1 FormulaDetailViewModel 新增属性**

```csharp
// 药材过滤
public ObservableCollection<HerbDto> FilteredHerbs { get; set; }
public ObservableCollection<HerbDto> AllHerbs { get; set; }

// 命令
public DelegateCommand AddHerbCommand { get; }
public DelegateCommand ImportFormulaCommand { get; }
public DelegateCommand ClearAllCommand { get; }
public DelegateCommand<FormulaHerbItemDto> RemoveHerbCommand { get; }
```

#### **3.4.2 HerbItems 集合变化监听**

```csharp
public ObservableCollection<FormulaHerbItemDto> HerbItems
{
    get => _herbItems;
    set
    {
        if (_herbItems != null)
            _herbItems.CollectionChanged -= OnHerbItemsChanged;

        SetProperty(ref _herbItems, value);

        if (_herbItems != null)
            _herbItems.CollectionChanged += OnHerbItemsChanged;
    }
}

private void OnHerbItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
{
    RaisePropertyChanged(nameof(HerbCount));
}
```

---

## 4. 技术实现方案

### 4.1 核心组件（参考处方模块）

| 组件名称 | 处方模块 | 验方模块（待实现） | 职责 |
|---------|---------|------------------|-----|
| **拼音码过滤管理器** | HerbFilterManager | FormulaHerbFilterManager | 拼音码过滤逻辑 |
| **药材选择对话框** | HerbSelectionDialog | （复用处方） | 选择药材并设置用量 |
| **验方选择对话框** | 无 | FormulaSelectionDialog | 选择验方并导入药材 |

### 4.2 代码结构（推荐）

```
LYBT.Desktop.Formula/
├── ViewModels/
│   ├── FormulaManagementViewModel.cs     # 列表页ViewModel（添加CopyFormulaCommand）
│   ├── FormulaDetailViewModel.cs         # 详情页ViewModel（需增强）
│   ├── Components/
│   │   ├── FormulaHerbFilterManager.cs   # 新增：拼音码过滤
│   │   └── FormulaCommandHandler.cs      # 现有：保存逻辑（无需修改）
│   └── Dialogs/
│       └── FormulaSelectionDialogViewModel.cs  # 新增：验方选择对话框
├── Views/
│   ├── FormulaManagementView.xaml        # 列表页（添加"复制"按钮）
│   ├── FormulaDetailView.xaml            # 详情页（需修改为8列DataGrid）
│   └── Dialogs/
│       └── FormulaSelectionDialog.xaml   # 新增：验方选择对话框
```

### 4.3 实施步骤（Phase划分）

#### **Phase 1：基础编辑功能（优先级：P0）**

**目标**：实现基本的添加/删除药材功能

**任务清单**：
1. ✅ 创建 `FormulaHerbFilterManager`（拼音码过滤）
2. ✅ 在 FormulaDetailViewModel 添加命令：
   - `AddHerbCommand`
   - `RemoveHerbCommand`
   - `ClearAllCommand`
3. ✅ 复用 `HerbSelectionDialog`（处方模块）
4. ✅ 实现药材添加逻辑（调用HerbSelectionDialog → 添加到HerbItems）
5. ✅ 实现药材删除逻辑（从HerbItems移除）
6. ✅ UI修改：FormulaDetailView.xaml 添加操作按钮区

**验收标准**：
- ✅ 编辑模式下，可点击"添加药材"按钮，选择药材后添加到列表
- ✅ 编辑模式下，可点击"删除"按钮，删除选中药材
- ✅ 查看模式下，按钮区隐藏

**预估工作量**：约8小时

---

#### **Phase 2：8列快速录入（优先级：P1）**

**目标**：实现8列DataGrid快速录入

**任务清单**：
1. ✅ 创建 `FormulaItemRow` 模型（参考 PrescriptionItemRow）
2. ✅ FormulaDetailViewModel 添加 `ItemRows` 属性
3. ✅ 实现 `RefreshItemRows()` 方法（HerbItems ↔ ItemRows 双向同步）
4. ✅ UI修改：DataGrid 改为8列模板列（ComboBox + TextBox）
5. ✅ 实现焦点跳转逻辑（PreviewKeyDown事件）
6. ✅ 实现拼音码过滤（ComboBox.TextChanged事件）

**验收标准**：
- ✅ 可使用拼音码快速选择药材
- ✅ 用量输入完成后，焦点自动跳转到下一个药材
- ✅ 一行可录入4味药材

**预估工作量**：约8小时

---

#### **Phase 3：导入验方功能（优先级：P2）**

**目标**：从其他验方导入药材列表

**任务清单**：
1. ✅ 创建 `FormulaSelectionDialog`（选择验方对话框）
2. ✅ 创建 `FormulaSelectionDialogViewModel`
3. ✅ 实现 `ImportFormulaCommand`
4. ✅ 实现导入逻辑：读取选中验方的Herbs → 添加到当前HerbItems
5. ✅ UI修改：添加"导入验方"按钮

**验收标准**：
- ✅ 点击"导入验方"按钮，弹出验方选择对话框
- ✅ 选择验方后，药材列表自动导入到当前验方

**预估工作量**：约6小时

---

#### **Phase 4：复制验方功能（优先级：P1）**

**目标**：复制现有验方，生成新验方

**任务清单**：
1. ✅ FormulaManagementViewModel 添加 `CopyFormulaCommand`
2. ✅ 实现复制逻辑：
   - 读取完整验方信息（包括药材列表）
   - 生成新ID、重置创建者和时间
   - 导航到编辑页面并预填充数据
3. ✅ FormulaDetailViewModel 添加复制数据处理逻辑
4. ✅ UI修改：
   - FormulaManagementView.xaml 添加"复制"按钮
   - FormulaDetailView.xaml 添加"复制验方"按钮（可选）

**验收标准**：
- ✅ 列表页点击"复制"按钮，成功进入编辑页面
- ✅ 编辑页面预填充所有数据（基本信息 + 药材列表）
- ✅ 验方ID、创建者、创建时间均为新值
- ✅ 保存后生成新的验方记录
- ✅ 原验方不受影响

**预估工作量**：约6小时

---

### 4.4 总工作量估算

| Phase | 功能 | 工作量 | 优先级 |
|-------|------|--------|--------|
| Phase 1 | 基础编辑功能 | 8小时 | P0 |
| Phase 2 | 8列快速录入 | 8小时 | P1 |
| Phase 3 | 导入验方 | 6小时 | P2 |
| Phase 4 | 复制验方 | 6小时 | P1 |
| **总计** |  | **28小时** |  |

---

## 5. 用户体验优化

### 5.1 快捷键支持

| 快捷键 | 功能 | 说明 |
|-------|------|------|
| Ctrl+N | 添加药材 | 快速打开药材选择对话框 |
| Delete | 删除选中药材 | 删除DataGrid选中行 |
| Ctrl+I | 导入验方 | 快速打开验方选择对话框 |
| Enter | 焦点跳转 | 用量输入完成后跳转 |
| Esc | 取消编辑 | 退出当前编辑状态 |

### 5.2 数据验证

| 验证项 | 规则 | 错误提示 |
|-------|------|---------|
| 药材名称 | 必填 | "请选择药材" |
| 用量 | >0 | "用量必须大于0" |
| 单位 | 必填 | "请输入单位" |
| 重复药材 | 不允许重复 | "该药材已添加，请勿重复" |

### 5.3 异常处理

| 异常场景 | 处理策略 |
|---------|---------|
| 药材列表加载失败 | 显示错误提示，禁用添加药材按钮 |
| 导入验方失败 | 显示错误提示，不清空现有药材 |
| 复制验方失败 | 显示错误提示，返回列表页 |

---

## 6. 验收标准

### 6.1 功能验收

| 功能点 | 验收标准 |
|-------|---------|
| 添加药材 | ✅ 点击"添加药材"，选择药材后成功添加到列表 |
| 删除药材 | ✅ 点击"删除"或快捷键Delete，成功删除选中药材 |
| 拼音码过滤 | ✅ 输入拼音码首字母，ComboBox自动过滤匹配药材 |
| 焦点跳转 | ✅ 用量输入完成后，焦点自动跳转到下一个药材 |
| 导入验方 | ✅ 选择验方后，药材列表成功导入 |
| 复制验方 | ✅ 复制后进入编辑模式，数据预填充完整 |
| 查看模式 | ✅ 查看模式下，所有编辑功能禁用 |
| 编辑模式 | ✅ 编辑模式下，所有编辑功能启用 |

### 6.2 性能验收

| 性能指标 | 目标值 |
|---------|-------|
| 药材列表加载时间 | <500ms |
| 拼音码过滤响应时间 | <100ms |
| 导入验方响应时间 | <1000ms |
| 复制验方响应时间 | <500ms |

### 6.3 兼容性验收

| 兼容项 | 要求 |
|-------|------|
| 现有数据模型 | ✅ 不改变FormulaDto、FormulaHerbItemDto |
| 现有导航逻辑 | ✅ 沿用ReadOnly参数机制 |
| 现有保存逻辑 | ✅ FormulaCommandHandler无需修改 |

---

## 7. 风险与限制

### 7.1 风险识别

| 风险项 | 影响程度 | 缓解措施 |
|-------|---------|---------|
| 8列布局屏幕适配问题 | 中 | 设置最小宽度，超出时横向滚动 |
| 拼音码过滤性能问题 | 低 | 使用异步加载，限制药材列表大小 |
| 复制验方名称重复 | 中 | 后期实施重复校验（当前阶段允许） |

### 7.2 限制说明

| 限制项 | 说明 |
|-------|------|
| 屏幕宽度 | 8列布局需要至少1280px宽度 |
| 药材列表大小 | 建议不超过500条，保证过滤性能 |
| 浏览器支持 | 仅支持WPF，不支持Web端 |
| 重复校验 | 当前阶段不校验名称、组成重复（后期工作） |

---

## 8. 后期工作（暂不实施）

| 功能 | 说明 | 优先级 |
|-----|------|--------|
| 名称重复校验 | 验方名称不能与现有验方重复 | P2 |
| 组成重复校验 | 药材组成不能与现有验方完全一致 | P2 |
| 复制时自动重命名 | 复制时自动在名称后添加"（副本）"后缀 | P3 |
| 炮制方法、用法编辑对话框 | 双击药材行弹出详细编辑对话框 | P2 |

---

## 9. 附录

### 9.1 参考文档

- **处方设计文档**：`docs/design/prescriptions-design.md`
- **Formula CRUD增强设计**：`docs/explanation/formula-crud-enhancement-design.md`
- **处方编辑区域实现**：`src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml`
- **处方ViewModel实现**：`src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs`
- **布局方案对比文档**：`docs/requirements/formula-layout-comparison.md`

### 9.2 关键代码文件

| 文件路径 | 说明 |
|---------|------|
| `LYBT.Desktop.Formula/Views/FormulaManagementView.xaml` | 验方列表视图（需添加"复制"按钮） |
| `LYBT.Desktop.Formula/ViewModels/FormulaManagementViewModel.cs` | 验方列表ViewModel（需添加CopyFormulaCommand） |
| `LYBT.Desktop.Formula/Views/FormulaDetailView.xaml` | 验方详情视图（需改为8列DataGrid） |
| `LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs` | 验方详情ViewModel（需增强） |
| `LYBT.Desktop.Prescriptions/ViewModels/Components/HerbFilterManager.cs` | 拼音码过滤管理器（参考） |
| `LYBT.Desktop.Prescriptions/Views/HerbSelectionDialog.xaml` | 药材选择对话框（复用） |

### 9.3 数据模型定义

```csharp
// FormulaDto（现有模型，无需修改）
public class FormulaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }              // 验方名称
    public string? PinYinCode { get; set; }       // 拼音码
    public string? Property { get; set; }         // 性味归经
    public string Effect { get; set; }            // 功效（必填）
    public string Usage { get; set; }             // 用法（必填）
    public string? Remark { get; set; }           // 备注
    public bool IsShared { get; set; }            // 是否共享
    public string Status { get; set; }            // 状态
    public string CreatedBy { get; set; }         // 创建者
    public DateTime CreatedAt { get; set; }       // 创建时间
    public DateTime UpdatedAt { get; set; }       // 更新时间
    public List<FormulaHerbItemDto> Herbs { get; set; }  // 药材列表
}

// FormulaHerbItemDto（现有模型，无需修改）
public class FormulaHerbItemDto
{
    public Guid Id { get; set; }
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }          // 药材名称
    public decimal Quantity { get; set; }         // 用量
    public string Unit { get; set; }              // 单位
    public string? Preparation { get; set; }      // 炮制方法
    public string? Usage { get; set; }            // 用法
    public int SortOrder { get; set; }            // 排序
}
```

---

**文档结束**

**下一步**：基于本需求文档开始Phase 1开发（基础编辑功能），完成后逐步推进Phase 2-4。
