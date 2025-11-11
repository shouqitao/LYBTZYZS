# 验方编辑区增强功能 - 技术设计文档

## 📋 文档信息

- **版本**: v1.0
- **创建日期**: 2025-11-11
- **需求文档**: `docs/requirements/formula-editing-area-requirements.md` v2.0
- **适用模块**: LYBT.Desktop.Formula
- **架构参考**: Phase 2三层对齐架构（ViewModel → Repository）
- **预计工时**: 22小时（3个Phase）
- **最后修订**: 2025-11-11（删除Phase 3导入验方功能，调整为复制验方）

---

## 🎯 一、设计目标

基于 `formula-editing-area-requirements.md` v2.0最终决策，为验方详情页面（FormulaDetailView）增强药材编辑功能，实现快速录入、验方复制两大核心能力。

### 1.1 核心决策确认

| 决策项 | 最终方案 | 理由 |
|--------|---------|------|
| **布局方案** | ✅ 方案A：8列DataGrid | 匹配Prescription模块，用户熟悉度高 |
| **价格计算** | ✅ 验方不涉及价格 | 简化业务逻辑，聚焦药材配伍 |
| **复制功能** | ✅ 新增复制验方功能 | 提升用户效率，支持验方模板化 |

### 1.2 业务价值

- **效率提升**: 8列布局使单行可录入4组药材，录入效率提升**4倍**
- **易用性提升**: 拼音码过滤，减少鼠标点击，提升**50%**选择速度
- **功能完整性**: 验方复制功能，支持中医验方模板化管理

---

## 🏗️ 二、架构设计

### 2.1 三层对齐架构

遵循 **Phase 2 架构规范**（Issue #1114）：

```
┌─────────────────────────────────────────────────────┐
│                   Desktop Layer                     │
├─────────────────────────────────────────────────────┤
│ FormulaDetailView.xaml (XAML UI)                    │
│         ↓                                           │
│ FormulaDetailViewModel (MVVM)                       │
│         ↓                                           │
│ ┌───────────────────────────────────────────────┐  │
│ │ 组件化架构 (Epic #1773)                         │  │
│ │ - FormulaDataManager (数据管理)                 │  │
│ │ - FormulaCommandHandler (命令处理)              │  │
│ │ - FormulaValidator (验证逻辑)                   │  │
│ │ - FormulaHerbFilterManager (拼音码过滤 - NEW)   │  │
│ └───────────────────────────────────────────────┘  │
│         ↓                                           │
│ IFormulaRepository (Refit客户端)                    │
│         ↓                                           │
│ IHerbRepository (药材数据源 - NEW依赖)              │
└─────────────────────────────────────────────────────┘
                    ↓ HTTP
┌─────────────────────────────────────────────────────┐
│                   Server Layer                      │
├─────────────────────────────────────────────────────┤
│ FormulaController (ASP.NET Core Web API)            │
│         ↓                                           │
│ IFormulaRepository (Repository模式)                 │
│         ↓                                           │
│ AppDbContext (EF Core)                              │
│         ↓                                           │
│ SQL Server Database                                 │
└─────────────────────────────────────────────────────┘
```

**关键原则**:
- ✅ **无Service层**: ViewModel直接调用Repository（Phase 2架构）
- ✅ **组件化**: 新增FormulaHerbFilterManager专职拼音码过滤
- ✅ **依赖注入**: 所有组件通过构造函数注入，Prism容器管理

### 2.2 新增组件职责划分

#### FormulaHerbFilterManager（新增组件）

**职责**:
- 拼音码实时过滤（支持首字母、全拼）
- 药材列表缓存管理
- 焦点跳转逻辑（Tab键自动跳转至下一个ComboBox）

**参考实现**: `LYBT.Desktop.Prescriptions/ViewModels/Components/HerbFilterManager.cs`

**核心方法**:
```csharp
public class FormulaHerbFilterManager
{
    private readonly IHerbRepository _herbRepository;
    private List<HerbDto> _allHerbs = new();
    private ObservableCollection<HerbDto> _filteredHerbs = new();

    /// <summary>
    /// 初始化加载所有药材数据
    /// </summary>
    public async Task InitializeAsync();

    /// <summary>
    /// 智能匹配过滤（名称 + 拼音码）
    /// - 输入"黄芪" → 匹配名称包含"黄芪"的药材
    /// - 输入"HQ" → 匹配拼音码以"HQ"开头的药材（黄芪、黄岐等）
    /// - 默认返回前5个匹配结果
    /// </summary>
    /// <param name="searchText">药材名称或拼音码</param>
    /// <param name="maxResults">最大返回数量（默认5）</param>
    /// <returns>过滤后的药材列表</returns>
    public ObservableCollection<HerbDto> FilterHerbs(string searchText, int maxResults = 5);

    /// <summary>
    /// 处理ComboBox焦点跳转
    /// </summary>
    /// <param name="currentColumn">当前列索引（1-8）</param>
    /// <returns>下一个焦点列索引</returns>
    public int GetNextFocusColumn(int currentColumn);
}
```

#### FormulaDataManager（增强现有）

**新增职责**:
- 8列模型（FormulaItemRow）的转换与管理
- 验方导入逻辑（从Formula导入到当前Formula）
- 验方复制数据快照

**新增方法**:
```csharp
// 已有方法（无需修改）
Task<(bool success, FormulaDto? formula, string? errorMessage)> LoadFormulaAsync(Guid formulaId);
void LoadHerbItems(ObservableCollection<FormulaHerbItemDto> collection, IEnumerable<FormulaHerbItemDto>? items);

// 新增方法
/// <summary>
/// 将8列模型转换为FormulaHerbItemDto列表
/// </summary>
List<FormulaHerbItemDto> ConvertRowsToHerbItems(ObservableCollection<FormulaItemRow> rows);

/// <summary>
/// 将FormulaHerbItemDto列表转换为8列模型
/// </summary>
ObservableCollection<FormulaItemRow> ConvertHerbItemsToRows(List<FormulaHerbItemDto> herbItems);

/// <summary>
/// 导入验方到当前编辑中的验方
/// </summary>
Task<bool> ImportFormulaAsync(Guid sourceFormulaId);

/// <summary>
/// 创建验方副本（用于复制功能）
/// </summary>
FormulaDto CreateFormulaCopy(FormulaDto sourceFormula, string currentUserName);
```

#### FormulaCommandHandler（增强现有）

**新增命令**:
```csharp
// 已有命令（无需修改）
ICommand SaveCommand { get; }
ICommand EditCommand { get; }
ICommand DeleteCommand { get; }
ICommand CancelEditCommand { get; }

// 新增命令
ICommand AddHerbCommand { get; }         // Phase 1: 添加药材行
ICommand RemoveHerbCommand { get; }      // Phase 1: 删除药材行
ICommand ClearAllCommand { get; }        // Phase 1: 清空所有药材
ICommand CopyFormulaCommand { get; }     // Phase 3: 复制验方（列表页）
```

---

## 📊 三、数据模型设计

### 3.1 FormulaItemRow（新增模型）

8列快速录入的核心模型，每行包含4组药材：

```csharp
/// <summary>
/// 验方药材行模型 - 8列布局（4组药材）
/// </summary>
public class FormulaItemRow : BindableBase
{
    // 药材1
    private HerbDto? _herb1;
    public HerbDto? Herb1
    {
        get => _herb1;
        set => SetProperty(ref _herb1, value);
    }

    private decimal _quantity1;
    public decimal Quantity1
    {
        get => _quantity1;
        set => SetProperty(ref _quantity1, value);
    }

    // 药材2
    private HerbDto? _herb2;
    public HerbDto? Herb2
    {
        get => _herb2;
        set => SetProperty(ref _herb2, value);
    }

    private decimal _quantity2;
    public decimal Quantity2
    {
        get => _quantity2;
        set => SetProperty(ref _quantity2, value);
    }

    // 药材3
    private HerbDto? _herb3;
    public HerbDto? Herb3
    {
        get => _herb3;
        set => SetProperty(ref _herb3, value);
    }

    private decimal _quantity3;
    public decimal Quantity3
    {
        get => _quantity3;
        set => SetProperty(ref _quantity3, value);
    }

    // 药材4
    private HerbDto? _herb4;
    public HerbDto? Herb4
    {
        get => _herb4;
        set => SetProperty(ref _herb4, value);
    }

    private decimal _quantity4;
    public decimal Quantity4
    {
        get => _quantity4;
        set => SetProperty(ref _quantity4, value);
    }

    /// <summary>
    /// 转换为FormulaHerbItemDto列表
    /// </summary>
    public List<FormulaHerbItemDto> ToHerbItems()
    {
        var items = new List<FormulaHerbItemDto>();

        if (Herb1 != null)
            items.Add(new FormulaHerbItemDto
            {
                Id = Guid.NewGuid(),
                HerbId = Herb1.Id,
                HerbName = Herb1.Name,
                Quantity = Quantity1,
                SortOrder = items.Count
            });

        if (Herb2 != null)
            items.Add(new FormulaHerbItemDto
            {
                Id = Guid.NewGuid(),
                HerbId = Herb2.Id,
                HerbName = Herb2.Name,
                Quantity = Quantity2,
                SortOrder = items.Count
            });

        if (Herb3 != null)
            items.Add(new FormulaHerbItemDto
            {
                Id = Guid.NewGuid(),
                HerbId = Herb3.Id,
                HerbName = Herb3.Name,
                Quantity = Quantity3,
                SortOrder = items.Count
            });

        if (Herb4 != null)
            items.Add(new FormulaHerbItemDto
            {
                Id = Guid.NewGuid(),
                HerbId = Herb4.Id,
                HerbName = Herb4.Name,
                Quantity = Quantity4,
                SortOrder = items.Count
            });

        return items;
    }
}
```

### 3.2 数据转换逻辑

**FormulaHerbItemDto → FormulaItemRow**:
```csharp
// FormulaDataManager.cs
public ObservableCollection<FormulaItemRow> ConvertHerbItemsToRows(List<FormulaHerbItemDto> herbItems)
{
    var rows = new ObservableCollection<FormulaItemRow>();

    for (int i = 0; i < herbItems.Count; i += 4)
    {
        var row = new FormulaItemRow();

        if (i < herbItems.Count)
        {
            row.Herb1 = await _herbRepository.GetByIdAsync(herbItems[i].HerbId);
            row.Quantity1 = herbItems[i].Quantity;
        }

        if (i + 1 < herbItems.Count)
        {
            row.Herb2 = await _herbRepository.GetByIdAsync(herbItems[i + 1].HerbId);
            row.Quantity2 = herbItems[i + 1].Quantity;
        }

        if (i + 2 < herbItems.Count)
        {
            row.Herb3 = await _herbRepository.GetByIdAsync(herbItems[i + 2].HerbId);
            row.Quantity3 = herbItems[i + 2].Quantity;
        }

        if (i + 3 < herbItems.Count)
        {
            row.Herb4 = await _herbRepository.GetByIdAsync(herbItems[i + 3].HerbId);
            row.Quantity4 = herbItems[i + 3].Quantity;
        }

        rows.Add(row);
    }

    return rows;
}
```

**FormulaItemRow → FormulaHerbItemDto**:
```csharp
// FormulaDataManager.cs
public List<FormulaHerbItemDto> ConvertRowsToHerbItems(ObservableCollection<FormulaItemRow> rows)
{
    var herbItems = new List<FormulaHerbItemDto>();

    foreach (var row in rows)
    {
        herbItems.AddRange(row.ToHerbItems());
    }

    // 重新设置SortOrder
    for (int i = 0; i < herbItems.Count; i++)
    {
        herbItems[i].SortOrder = i;
    }

    return herbItems;
}
```

---

## 🎨 四、UI设计

### 4.1 FormulaDetailView.xaml 增强

**参考实现**: `LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml` (lines 199-319)

**8列DataGrid布局**:
```xml
<!-- 药材编辑区域 -->
<GroupBox Header="药材组成" Grid.Row="2" Margin="0,10,0,0">
    <DataGrid ItemsSource="{Binding HerbRows}"
              AutoGenerateColumns="False"
              CanUserAddRows="True"
              CanUserDeleteRows="True"
              HeadersVisibility="Column"
              SelectionMode="Single"
              IsReadOnly="{Binding IsReadOnly}">

        <!-- 药材1 -->
        <DataGrid.Columns>
            <DataGridTemplateColumn Header="药材1" Width="2*">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <ComboBox IsEditable="True"
                                 ItemsSource="{Binding DataContext.FilteredHerbs, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                 SelectedItem="{Binding Herb1, UpdateSourceTrigger=PropertyChanged}"
                                 DisplayMemberPath="Name"
                                 Text="{Binding Herb1.Name, UpdateSourceTrigger=PropertyChanged}"
                                 IsDropDownOpen="{Binding DataContext.IsDropDownOpen1, Mode=TwoWay, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                 MaxDropDownHeight="200"
                                 Loaded="HerbComboBox_Loaded"
                                 Tag="1">
                            <i:Interaction.Triggers>
                                <!-- 智能匹配：TextChanged实时过滤 -->
                                <i:EventTrigger EventName="TextChanged">
                                    <i:InvokeCommandAction Command="{Binding DataContext.FilterHerbsCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                          CommandParameter="{Binding Text, RelativeSource={RelativeSource AncestorType=ComboBox}}" />
                                </i:EventTrigger>
                                <!-- 键盘导航：上下键滚动，回车确认，Tab切换 -->
                                <i:EventTrigger EventName="PreviewKeyDown">
                                    <i:InvokeCommandAction Command="{Binding DataContext.HandleKeyNavigationCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                          CommandParameter="{Binding Tag, RelativeSource={RelativeSource AncestorType=ComboBox}}" />
                                </i:EventTrigger>
                            </i:Interaction.Triggers>
                        </ComboBox>
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>

            <DataGridTextColumn Header="用量1" Width="*" Binding="{Binding Quantity1, UpdateSourceTrigger=PropertyChanged}" />

            <!-- 药材2 -->
            <DataGridTemplateColumn Header="药材2" Width="2*">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <ComboBox IsEditable="True"
                                 ItemsSource="{Binding DataContext.FilteredHerbs, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                 SelectedItem="{Binding Herb2, UpdateSourceTrigger=PropertyChanged}"
                                 DisplayMemberPath="Name"
                                 Text="{Binding Herb2.Name, UpdateSourceTrigger=PropertyChanged}"
                                 Loaded="HerbComboBox_Loaded"
                                 Tag="2">
                            <!-- 同Herb1的Triggers -->
                        </ComboBox>
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>

            <DataGridTextColumn Header="用量2" Width="*" Binding="{Binding Quantity2, UpdateSourceTrigger=PropertyChanged}" />

            <!-- 药材3 -->
            <DataGridTemplateColumn Header="药材3" Width="2*">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <ComboBox IsEditable="True"
                                 ItemsSource="{Binding DataContext.FilteredHerbs, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                 SelectedItem="{Binding Herb3, UpdateSourceTrigger=PropertyChanged}"
                                 DisplayMemberPath="Name"
                                 Text="{Binding Herb3.Name, UpdateSourceTrigger=PropertyChanged}"
                                 Loaded="HerbComboBox_Loaded"
                                 Tag="3">
                            <!-- 同Herb1的Triggers -->
                        </ComboBox>
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>

            <DataGridTextColumn Header="用量3" Width="*" Binding="{Binding Quantity3, UpdateSourceTrigger=PropertyChanged}" />

            <!-- 药材4 -->
            <DataGridTemplateColumn Header="药材4" Width="2*">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <ComboBox IsEditable="True"
                                 ItemsSource="{Binding DataContext.FilteredHerbs, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                 SelectedItem="{Binding Herb4, UpdateSourceTrigger=PropertyChanged}"
                                 DisplayMemberPath="Name"
                                 Text="{Binding Herb4.Name, UpdateSourceTrigger=PropertyChanged}"
                                 Loaded="HerbComboBox_Loaded"
                                 Tag="4">
                            <!-- 同Herb1的Triggers -->
                        </ComboBox>
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>

            <DataGridTextColumn Header="用量4" Width="*" Binding="{Binding Quantity4, UpdateSourceTrigger=PropertyChanged}" />
        </DataGrid.Columns>
    </DataGrid>
</GroupBox>

<!-- 操作按钮区 -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Grid.Row="3" Margin="0,10,0,0">
    <Button Content="添加行" Command="{Binding AddHerbCommand}" Width="80" Margin="0,0,10,0" />
    <Button Content="删除行" Command="{Binding RemoveHerbCommand}" Width="80" Margin="0,0,10,0" />
    <Button Content="清空" Command="{Binding ClearAllCommand}" Width="80" Margin="0,0,10,0" />
    <Button Content="导入验方" Command="{Binding ImportFormulaCommand}" Width="100" Margin="0,0,10,0" />
</StackPanel>
```

### 4.2 ComboBox智能匹配交互设计

**用途**: Phase 2药材快速录入，无需独立对话框，使用ComboBox内联智能匹配

**交互逻辑**:
1. **输入触发过滤**: TextChanged事件实时过滤，显示前5个匹配结果
2. **上下键滚动**:
   - 下箭头：如果超过5个结果，继续显示第6、7、8...个
   - 上箭头：返回上一个结果
3. **选择确认**:
   - 鼠标点击：直接选中
   - 回车键：确认当前高亮项
   - Tab键：切换至下一列（用量列）
4. **智能匹配规则**:
   - 输入"黄芪" → 匹配名称包含"黄芪"的药材
   - 输入"HQ" → 匹配拼音码以"HQ"开头的所有药材（黄芪、黄岐等）

### 4.3 FormulaSelectionDialog.xaml（Phase 3导入验方对话框）

**用途**: Phase 3导入验方功能（从验方库导入整个配方）

**设计说明**:
- **备选方案1**: 使用对话框（如下）
- **备选方案2**: 使用Region导航至验方选择页面（符合项目导航模式）
- **推荐方案**: 方案2（导航模式），避免弹窗

**对话框设计**（如选择方案1）：

**参考实现**: `LYBT.Desktop.Prescriptions/Views/HerbSelectionDialog.xaml`

```xml
<Window x:Class="LYBT.Desktop.Formula.Views.FormulaSelectionDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="选择验方" Height="500" Width="800" WindowStartupLocation="CenterOwner">
    <Grid Margin="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 搜索框 -->
        <TextBox Grid.Row="0"
                 Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                 Margin="0,0,0,10"
                 Watermark="搜索验方名称..." />

        <!-- 验方列表 -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Formulas}"
                  SelectedItem="{Binding SelectedFormula}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTextColumn Header="验方名称" Binding="{Binding Name}" Width="2*"/>
                <DataGridTextColumn Header="功效" Binding="{Binding Effect}" Width="3*"/>
                <DataGridTextColumn Header="药材数量" Binding="{Binding Herbs.Count}" Width="*"/>
                <DataGridTextColumn Header="创建人" Binding="{Binding CreatedBy}" Width="*"/>
                <DataGridTextColumn Header="创建日期" Binding="{Binding CreatedAt, StringFormat={}{0:yyyy-MM-dd}}" Width="*"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
            <Button Content="确定" Command="{Binding ConfirmCommand}" Width="80" Margin="0,0,10,0" IsDefault="True" />
            <Button Content="取消" Command="{Binding CancelCommand}" Width="80" IsCancel="True" />
        </StackPanel>
    </Grid>
</Window>
```

**FormulaSelectionDialogViewModel.cs**:
```csharp
public class FormulaSelectionDialogViewModel : BindableBase
{
    private readonly IFormulaRepository _formulaRepository;

    private ObservableCollection<FormulaDto> _formulas = new();
    public ObservableCollection<FormulaDto> Formulas
    {
        get => _formulas;
        set => SetProperty(ref _formulas, value);
    }

    private FormulaDto? _selectedFormula;
    public FormulaDto? SelectedFormula
    {
        get => _selectedFormula;
        set => SetProperty(ref _selectedFormula, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            _ = FilterFormulasAsync();
        }
    }

    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public FormulaSelectionDialogViewModel(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;

        ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
        CancelCommand = new DelegateCommand(ExecuteCancel);

        _ = LoadFormulasAsync();
    }

    private async Task LoadFormulasAsync()
    {
        try
        {
            var allFormulas = await _formulaRepository.GetAllAsync();
            Formulas = new ObservableCollection<FormulaDto>(allFormulas);
        }
        catch (Exception ex)
        {
            // 错误处理
        }
    }

    private async Task FilterFormulasAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadFormulasAsync();
            return;
        }

        var allFormulas = await _formulaRepository.GetAllAsync();
        var filtered = allFormulas.Where(f => f.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        Formulas = new ObservableCollection<FormulaDto>(filtered);
    }

    private void ExecuteConfirm()
    {
        // DialogResult = true;
        // Close();
    }

    private bool CanExecuteConfirm()
    {
        return SelectedFormula != null;
    }

    private void ExecuteCancel()
    {
        // DialogResult = false;
        // Close();
    }
}
```

---

## 🔧 五、核心实现代码

### 5.1 FormulaHerbFilterManager.cs（新增）

**文件路径**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaHerbFilterManager.cs`

```csharp
using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels.Components
{
    /// <summary>
    /// 验方药材过滤管理器 - 拼音码实时过滤
    /// </summary>
    public class FormulaHerbFilterManager
    {
        private readonly IHerbRepository _herbRepository;
        private readonly ILogger<FormulaHerbFilterManager> _logger;

        private List<HerbDto> _allHerbs = new();
        private ObservableCollection<HerbDto> _filteredHerbs = new();

        public ObservableCollection<HerbDto> FilteredHerbs => _filteredHerbs;

        public FormulaHerbFilterManager(
            IHerbRepository herbRepository,
            ILogger<FormulaHerbFilterManager> logger)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 初始化加载所有药材数据
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("加载药材数据用于拼音码过滤");

                var herbs = await _herbRepository.GetAllAsync();
                _allHerbs = herbs.ToList();
                _filteredHerbs = new ObservableCollection<HerbDto>(_allHerbs);

                _logger.LogInformation("已加载 {Count} 个药材", _allHerbs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材数据失败");
                throw;
            }
        }

        /// <summary>
        /// 智能匹配过滤（名称 + 拼音码）
        /// </summary>
        /// <param name="searchText">药材名称或拼音码</param>
        /// <param name="maxResults">最大返回数量（默认5，用于ComboBox下拉显示）</param>
        public void FilterHerbs(string? searchText, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredHerbs.Clear();
                // 空输入时显示前5个常用药材
                foreach (var herb in _allHerbs.Take(maxResults))
                {
                    _filteredHerbs.Add(herb);
                }
                return;
            }

            // 双重匹配逻辑：
            // 1. 名称包含匹配（如：输入"黄芪"匹配"黄芪"、"生黄芪"）
            // 2. 拼音码前缀匹配（如：输入"HQ"匹配"黄芪"、"黄岐"）
            var filtered = _allHerbs.Where(h =>
                h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (h.PinYinCode != null && h.PinYinCode.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
            ).Take(maxResults).ToList();

            _filteredHerbs.Clear();
            foreach (var herb in filtered)
            {
                _filteredHerbs.Add(herb);
            }

            _logger.LogDebug("智能匹配: {Count}/{MaxResults} 个药材匹配 '{SearchText}'", filtered.Count, maxResults, searchText);
        }

        /// <summary>
        /// 处理Tab键焦点跳转
        /// </summary>
        /// <param name="currentColumn">当前列索引（1-8）</param>
        /// <returns>下一个焦点列索引</returns>
        public int GetNextFocusColumn(int currentColumn)
        {
            // 8列循环: 药材1 → 用量1 → 药材2 → 用量2 → ... → 用量4 → 下一行药材1
            return (currentColumn % 8) + 1;
        }
    }
}
```

### 5.2 FormulaDataManager.cs（增强）

**新增方法**:

```csharp
// 文件路径: src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaDataManager.cs

/// <summary>
/// 将8列模型转换为FormulaHerbItemDto列表
/// </summary>
public List<FormulaHerbItemDto> ConvertRowsToHerbItems(ObservableCollection<FormulaItemRow> rows)
{
    if (rows == null)
    {
        throw new ArgumentNullException(nameof(rows));
    }

    var herbItems = new List<FormulaHerbItemDto>();

    foreach (var row in rows)
    {
        herbItems.AddRange(row.ToHerbItems());
    }

    // 重新设置SortOrder
    for (int i = 0; i < herbItems.Count; i++)
    {
        herbItems[i].SortOrder = i;
    }

    _logger.LogDebug("转换 {RowCount} 行为 {HerbCount} 个药材项", rows.Count, herbItems.Count);

    return herbItems;
}

/// <summary>
/// 将FormulaHerbItemDto列表转换为8列模型
/// </summary>
public async Task<ObservableCollection<FormulaItemRow>> ConvertHerbItemsToRowsAsync(List<FormulaHerbItemDto> herbItems)
{
    if (herbItems == null)
    {
        throw new ArgumentNullException(nameof(herbItems));
    }

    var rows = new ObservableCollection<FormulaItemRow>();

    for (int i = 0; i < herbItems.Count; i += 4)
    {
        var row = new FormulaItemRow();

        // 药材1
        if (i < herbItems.Count)
        {
            var herb1 = await _herbRepository.GetByIdAsync(herbItems[i].HerbId);
            row.Herb1 = herb1;
            row.Quantity1 = herbItems[i].Quantity;
        }

        // 药材2
        if (i + 1 < herbItems.Count)
        {
            var herb2 = await _herbRepository.GetByIdAsync(herbItems[i + 1].HerbId);
            row.Herb2 = herb2;
            row.Quantity2 = herbItems[i + 1].Quantity;
        }

        // 药材3
        if (i + 2 < herbItems.Count)
        {
            var herb3 = await _herbRepository.GetByIdAsync(herbItems[i + 2].HerbId);
            row.Herb3 = herb3;
            row.Quantity3 = herbItems[i + 2].Quantity;
        }

        // 药材4
        if (i + 3 < herbItems.Count)
        {
            var herb4 = await _herbRepository.GetByIdAsync(herbItems[i + 3].HerbId);
            row.Herb4 = herb4;
            row.Quantity4 = herbItems[i + 3].Quantity;
        }

        rows.Add(row);
    }

    _logger.LogDebug("转换 {HerbCount} 个药材项为 {RowCount} 行", herbItems.Count, rows.Count);

    return rows;
}

/// <summary>
/// 导入验方到当前编辑中的验方
/// </summary>
public async Task<(bool success, string? errorMessage)> ImportFormulaAsync(Guid sourceFormulaId)
{
    try
    {
        _logger.LogInformation("导入验方: {SourceFormulaId}", sourceFormulaId);

        var sourceFormula = await _repository.GetByIdAsync(sourceFormulaId);

        if (sourceFormula == null)
        {
            return (false, "未找到源验方");
        }

        if (sourceFormula.Herbs == null || !sourceFormula.Herbs.Any())
        {
            return (false, "源验方没有药材数据");
        }

        // 清空当前药材列表（如果有）
        // 导入源验方的药材
        // 重新生成ID和SortOrder

        return (true, null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导入验方失败: {SourceFormulaId}", sourceFormulaId);
        return (false, $"导入失败: {ex.Message}");
    }
}

/// <summary>
/// 创建验方副本（用于复制功能）
/// </summary>
public FormulaDto CreateFormulaCopy(FormulaDto sourceFormula, string currentUserName)
{
    if (sourceFormula == null)
    {
        throw new ArgumentNullException(nameof(sourceFormula));
    }

    var copiedFormula = new FormulaDto
    {
        Id = Guid.NewGuid(),
        Name = sourceFormula.Name,  // 保持相同名称（需求文档要求）
        Effect = sourceFormula.Effect,
        Usage = sourceFormula.Usage,
        Property = sourceFormula.Property,
        Remark = sourceFormula.Remark,
        IsShared = sourceFormula.IsShared,
        Category = sourceFormula.Category,
        CreatedBy = currentUserName,  // 更换创建人
        CreatedAt = DateTime.Now,
        Herbs = sourceFormula.Herbs?.Select(h => new FormulaHerbItemDto
        {
            Id = Guid.NewGuid(),  // 新ID
            HerbId = h.HerbId,
            HerbName = h.HerbName,
            Quantity = h.Quantity,
            Preparation = h.Preparation,
            Usage = h.Usage,
            SortOrder = h.SortOrder
            // Price字段不复制（验方不涉及价格）
        }).ToList() ?? new List<FormulaHerbItemDto>()
    };

    _logger.LogInformation("创建验方副本: {SourceName} -> {CopyName}", sourceFormula.Name, copiedFormula.Name);

    return copiedFormula;
}
```

### 5.3 FormulaCommandHandler.cs（增强）

**新增命令实现**:

```csharp
// 文件路径: src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/Components/FormulaCommandHandler.cs

// 新增命令定义
public ICommand AddHerbCommand { get; private set; }
public ICommand RemoveHerbCommand { get; private set; }
public ICommand ClearAllCommand { get; private set; }
public ICommand ImportFormulaCommand { get; private set; }

// 新增事件
public event Action? OnHerbAdded;
public event Action? OnHerbRemoved;
public event Action? OnHerbsCleared;
public event Action<Guid>? OnFormulaImported;

// 构造函数中初始化命令
public FormulaCommandHandler(
    ILogger<FormulaCommandHandler> logger,
    IRegionManager regionManager)
{
    _logger = logger;
    _regionManager = regionManager;

    // 已有命令
    SaveCommand = new DelegateCommand(ExecuteSaveCommand, CanExecuteSaveCommand);
    EditCommand = new DelegateCommand(ExecuteEditCommand, CanExecuteEditCommand);
    CancelEditCommand = new DelegateCommand(ExecuteCancelEditCommand, CanExecuteCancelEditCommand);
    DeleteCommand = new DelegateCommand(ExecuteDeleteCommand, CanExecuteDeleteCommand);
    BackCommand = new DelegateCommand(ExecuteBackCommand);

    // 新增命令
    AddHerbCommand = new DelegateCommand(ExecuteAddHerbCommand, CanExecuteAddHerbCommand);
    RemoveHerbCommand = new DelegateCommand(ExecuteRemoveHerbCommand, CanExecuteRemoveHerbCommand);
    ClearAllCommand = new DelegateCommand(ExecuteClearAllCommand, CanExecuteClearAllCommand);
    ImportFormulaCommand = new DelegateCommand(ExecuteImportFormulaCommand, CanExecuteImportFormulaCommand);
}

private void ExecuteAddHerbCommand()
{
    _logger.LogDebug("执行添加药材行命令");
    OnHerbAdded?.Invoke();
}

private bool CanExecuteAddHerbCommand()
{
    return _dataManager != null && !_dataManager.IsReadOnly && !_dataManager.IsLoading;
}

private void ExecuteRemoveHerbCommand()
{
    _logger.LogDebug("执行删除药材行命令");
    OnHerbRemoved?.Invoke();
}

private bool CanExecuteRemoveHerbCommand()
{
    return _dataManager != null && !_dataManager.IsReadOnly && !_dataManager.IsLoading;
}

private void ExecuteClearAllCommand()
{
    _logger.LogDebug("执行清空所有药材命令");
    OnHerbsCleared?.Invoke();
}

private bool CanExecuteClearAllCommand()
{
    return _dataManager != null && !_dataManager.IsReadOnly && !_dataManager.IsLoading;
}

private void ExecuteImportFormulaCommand()
{
    _logger.LogDebug("执行导入验方命令");
    // 打开FormulaSelectionDialog
    // 用户选择后触发OnFormulaImported事件
}

private bool CanExecuteImportFormulaCommand()
{
    return _dataManager != null && !_dataManager.IsReadOnly && !_dataManager.IsLoading;
}
```

### 5.4 FormulaDetailViewModel.cs（增强）

**新增属性和事件处理**:

```csharp
// 文件路径: src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaDetailViewModel.cs

#region 新增属性

/// <summary>
/// 8列药材行集合
/// </summary>
private ObservableCollection<FormulaItemRow> _herbRows = new();
public ObservableCollection<FormulaItemRow> HerbRows
{
    get => _herbRows;
    set => SetProperty(ref _herbRows, value);
}

/// <summary>
/// 过滤后的药材列表（用于ComboBox绑定）
/// </summary>
public ObservableCollection<HerbDto> FilteredHerbs => _herbFilterManager.FilteredHerbs;

#endregion

#region 新增字段

private readonly FormulaHerbFilterManager _herbFilterManager;

#endregion

#region 构造函数修改

public FormulaDetailViewModel(
    FormulaDataManager dataManager,
    FormulaCommandHandler commandHandler,
    FormulaValidator validator,
    FormulaHerbFilterManager herbFilterManager,  // 新增注入
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
    : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
{
    _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
    _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
    _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    _herbFilterManager = herbFilterManager ?? throw new ArgumentNullException(nameof(herbFilterManager));  // 新增

    // 设置组件依赖
    _commandHandler.SetDependencies(_dataManager, _validator);

    // 订阅已有事件
    _commandHandler.OnEditEnabled += HandleEditEnabled;
    _commandHandler.OnEditCancelled += HandleEditCancelled;
    _commandHandler.OnFormulaSaved += HandleFormulaSaved;
    _commandHandler.OnFormulaDeleted += HandleFormulaDeleted;

    // 订阅新增事件
    _commandHandler.OnHerbAdded += HandleHerbAdded;
    _commandHandler.OnHerbRemoved += HandleHerbRemoved;
    _commandHandler.OnHerbsCleared += HandleHerbsCleared;
    _commandHandler.OnFormulaImported += HandleFormulaImported;
}

#endregion

#region 新增事件处理

private void HandleHerbAdded()
{
    // 添加一个空行
    HerbRows.Add(new FormulaItemRow());
    _dataManager.MarkAsChanged();
}

private void HandleHerbRemoved()
{
    if (HerbRows.Count > 0)
    {
        HerbRows.RemoveAt(HerbRows.Count - 1);
        _dataManager.MarkAsChanged();
    }
}

private async void HandleHerbsCleared()
{
    var result = await ShowConfirmationAsync("确定要清空所有药材吗？");
    if (result)
    {
        HerbRows.Clear();
        _dataManager.MarkAsChanged();
    }
}

private async void HandleFormulaImported(Guid sourceFormulaId)
{
    try
    {
        var (success, errorMessage) = await _dataManager.ImportFormulaAsync(sourceFormulaId);

        if (success)
        {
            // 重新加载8列模型
            if (Formula?.Herbs != null)
            {
                HerbRows = await _dataManager.ConvertHerbItemsToRowsAsync(Formula.Herbs.ToList());
            }

            await ShowSuccessMessageAsync("验方导入成功");
        }
        else
        {
            await ShowErrorMessageAsync($"导入失败: {errorMessage}");
        }
    }
    catch (Exception ex)
    {
        await ShowErrorMessageAsync($"导入异常: {ex.Message}");
    }
}

#endregion

#region 新增命令绑定

public ICommand AddHerbCommand => _commandHandler.AddHerbCommand;
public ICommand RemoveHerbCommand => _commandHandler.RemoveHerbCommand;
public ICommand ClearAllCommand => _commandHandler.ClearAllCommand;
public ICommand ImportFormulaCommand => _commandHandler.ImportFormulaCommand;

public ICommand FilterHerbsCommand => new DelegateCommand<string>(ExecuteFilterHerbsCommand);

private void ExecuteFilterHerbsCommand(string? searchText)
{
    _herbFilterManager.FilterHerbs(searchText);
}

#endregion

#region 初始化修改

protected override async Task InitializeAsync(NavigationParameters parameters)
{
    await base.InitializeAsync(parameters);

    // 初始化药材过滤器
    await _herbFilterManager.InitializeAsync();

    if (parameters.ContainsKey("FormulaId"))
    {
        var formulaId = parameters.GetValue<Guid>("FormulaId");
        await LoadDataAsync(formulaId);
    }
    else if (parameters.ContainsKey("Formula"))
    {
        // Phase 3: 复制验方模式
        var copiedFormula = parameters.GetValue<FormulaDto>("Formula");
        var isCopy = parameters.GetValue<bool>("IsCopy");

        if (isCopy && copiedFormula != null)
        {
            Formula = copiedFormula;
            HerbRows = await _dataManager.ConvertHerbItemsToRowsAsync(copiedFormula.Herbs?.ToList() ?? new());
            IsReadOnly = false;
        }
    }
}

private async Task LoadDataAsync(Guid formulaId)
{
    try
    {
        await _dataManager.InitializeAsync(formulaId);
        RefreshProperties();

        // 转换为8列模型
        if (Formula?.Herbs != null)
        {
            HerbRows = await _dataManager.ConvertHerbItemsToRowsAsync(Formula.Herbs.ToList());
        }
    }
    catch (Exception ex)
    {
        await ShowErrorMessageAsync($"加载验方详情失败: {ex.Message}");
    }
}

#endregion
```

---

## 📝 六、Phase分解与实施计划

### Phase 1: 基础编辑功能（8小时）

**目标**: 实现8列DataGrid基础布局和3个核心命令

**任务清单**:
1. ✅ 创建`FormulaItemRow`模型（1h）
   - 8个属性（Herb1-4, Quantity1-4）
   - `ToHerbItems()`转换方法
2. ✅ 创建`FormulaHerbFilterManager`组件（2h）
   - 拼音码过滤逻辑
   - 焦点跳转逻辑
   - DI注册
3. ✅ 增强`FormulaDataManager`（2h）
   - `ConvertRowsToHerbItems()`
   - `ConvertHerbItemsToRowsAsync()`
4. ✅ 增强`FormulaCommandHandler`（1.5h）
   - `AddHerbCommand`
   - `RemoveHerbCommand`
   - `ClearAllCommand`
5. ✅ 修改`FormulaDetailView.xaml`（1.5h）
   - 8列DataGrid布局
   - 按钮区（添加行、删除行、清空）

**验收标准**:
- ✅ 能添加药材行（默认空行）
- ✅ 能删除最后一行
- ✅ 能清空所有药材（有确认对话框）
- ✅ DataGrid编辑时IsReadOnly=false，只读时IsReadOnly=true
- ✅ 0 errors, 0 warnings编译通过

---

### Phase 2: 8列快速录入（8小时）

**目标**: 实现智能匹配（名称+拼音码）和键盘导航，提升录入效率

**任务清单**:
1. ✅ ComboBox智能匹配过滤（3.5h）
   - **名称匹配**: 输入"黄芪"匹配名称包含"黄芪"的药材
   - **拼音码匹配**: 输入"HQ"匹配拼音码以"HQ"开头的药材
   - TextChanged事件触发过滤
   - FilteredHerbs实时更新（前5个结果）
   - 测试名称、拼音码双重匹配
2. ✅ ComboBox键盘导航（2.5h）
   - **上下键滚动**: 下箭头显示第6、7、8...个结果（如果超过5个）
   - **回车确认**: 选中当前高亮药材，光标跳转至用量列
   - **Tab切换**: 光标跳转至下一列（用量或下一药材）
   - PreviewKeyDown事件处理
3. ✅ ComboBox自动完成优化（1h）
   - IsEditable=True
   - MaxDropDownHeight=200（显示~5个结果）
   - IsDropDownOpen绑定（自动展开下拉列表）
   - 选中后自动跳转至用量列
4. ✅ 数据验证（1h）
   - 药材不能为空
   - 用量必须>0
   - 同一行不能有重复药材

**验收标准**:
- ✅ **名称匹配**: 输入"黄芪"显示"黄芪"、"生黄芪"等
- ✅ **拼音码匹配**: 输入"HQ"显示所有拼音码为"HQ"的药材
- ✅ **前5个显示**: 默认只显示前5个匹配结果
- ✅ **上下键滚动**: 下箭头可查看第6、7、8...个结果
- ✅ **回车确认**: 回车后药材被选中，光标跳转至用量列
- ✅ **Tab切换**: Tab键顺序跳转8列
- ✅ **鼠标点击**: 鼠标点击下拉列表直接选中
- ✅ 保存时验证药材和用量必填
- ✅ 真实场景测试：录入10味药材<2分钟

---

### Phase 3: 复制验方（6小时）

**目标**: 在验方列表页增加"复制"按钮，点击后进入编辑界面，保存按钮变为"另存为我的验方"

**核心逻辑**:
- 列表页点击"复制" → 导航至详情页（预填充数据，编辑模式）
- 详情页保存按钮文案变为"另存为我的验方"
- 保存时创建新验方记录（新Id，CreatedBy为当前用户）

**任务清单**:
1. ✅ 增强`FormulaManagementViewModel`（1.5h）
   - 新增`CopyFormulaCommand`
   - 调用`_dataManager.CreateFormulaCopy()`
   - 导航至`FormulaDetailView`（传递IsCopy=true参数）
2. ✅ 修改`FormulaManagementView.xaml`（0.5h）
   - 添加"复制"按钮（位于"编辑"和"删除"之间）
   - 绑定CopyFormulaCommand
3. ✅ 增强`FormulaDataManager.CreateFormulaCopy()`（2h）
   - 复制验方基础信息（保持Name相同）
   - 复制药材列表（新ID）
   - 更换CreatedBy为当前用户
   - 设置CreatedAt为当前时间
4. ✅ 修改`FormulaDetailViewModel.InitializeAsync()`（1h）
   - 检测IsCopy参数
   - 预填充复制的验方数据
   - 设置IsReadOnly=false（编辑模式）
   - **保存按钮文案变更**: SaveButtonText = "另存为我的验方"（IsCopy=true时）
   - XAML动态绑定：Content="{Binding SaveButtonText}"
5. ✅ 集成测试（1h）
   - 复制验方 → 修改药材 → 验证按钮文案 → 保存 → 验证数据库有2条验方

**验收标准**:
- ✅ 列表页点击"复制"按钮导航至详情页
- ✅ 详情页自动填充源验方数据（Name相同）
- ✅ **保存按钮文案显示"另存为我的验方"**
- ✅ 药材列表完整显示
- ✅ 保存后CreatedBy为当前用户
- ✅ 源验方和复制验方独立存在（不同ID）
- ✅ FormulaHerbs表有2组独立的药材记录

---

## 🧪 七、质量标准

### 7.1 编译标准

- ✅ **0 errors**
- ✅ **0 warnings**
- ✅ **所有依赖项正确注入**（Prism DI容器）

### 7.2 功能验证标准

#### Phase 1验证:
1. ✅ 启动应用，导航至验方详情页
2. ✅ 点击"添加行"按钮，DataGrid新增空行
3. ✅ 点击"删除行"按钮，最后一行被删除
4. ✅ 点击"清空"按钮，弹出确认对话框，确认后所有行被清空
5. ✅ 切换至只读模式（IsReadOnly=true），所有按钮禁用

#### Phase 2验证:
1. ✅ **名称匹配**: 在药材1列输入"黄芪"，下拉列表显示"黄芪"、"生黄芪"等（前5个）
2. ✅ **拼音码匹配**: 在药材1列输入"HQ"，下拉列表显示所有拼音码为"HQ"的药材（黄芪、黄岐等，前5个）
3. ✅ **上下键滚动**: 如果匹配结果超过5个，按下箭头显示第6、7、8...个结果
4. ✅ **回车确认**: 高亮药材后按回车，药材被选中，光标自动跳转至用量列
5. ✅ **Tab切换**: 按Tab键，光标依次跳转：用量1 → 药材2 → 用量2 → ... → 用量4 → 下一行药材1
6. ✅ **鼠标点击**: 鼠标点击下拉列表中的药材，直接选中
7. ✅ 保存时验证：药材不能为空、用量必须>0

#### Phase 3验证（复制验方）:
1. ✅ 在验方列表页选中验方，点击"复制"按钮
2. ✅ 导航至详情页，验证保存按钮文案变为"另存为我的验方"
3. ✅ 验证验方名称、功效、药材列表自动填充
4. ✅ 修改药材数量，保存
5. ✅ 数据库验证：有2条验方，Name相同但Id不同，CreatedBy为当前用户
6. ✅ 验证FormulaHerbs表有2组独立的药材记录

### 7.3 性能标准

- ✅ **拼音码过滤响应时间** < 200ms（药材库<500条）
- ✅ **8列模型转换时间** < 500ms
- ✅ **复制验方加载时间** < 1s（药材<50味）

### 7.4 代码质量标准

- ✅ **中文注释覆盖率** ≥ 80%（公共API必须有注释）
- ✅ **命名规范**: PascalCase（类型）、_camelCase（私有字段）
- ✅ **异步方法**: 所有I/O操作使用async/await
- ✅ **DI注入**: 构造函数注入，无属性注入
- ✅ **UTF-8 with BOM编码**

---

## 📚 八、参考文档

### 8.1 需求文档
- `docs/requirements/formula-editing-area-requirements.md` v2.0

### 8.2 架构文档
- `docs/explanation/architecture/client/README.md` - Client端MVVM架构
- `docs/explanation/architecture/shared/README.md` - Shared层架构
- `docs/explanation/business-rules.md` - 业务规则（CR-002验方导入规则）

### 8.3 参考实现
- **8列布局**: `LYBT.Desktop.Prescriptions/Views/PrescriptionView.xaml` (lines 199-319)
- **拼音码过滤**: `LYBT.Desktop.Prescriptions/ViewModels/Components/HerbFilterManager.cs`
- **对话框模式**: `LYBT.Desktop.Prescriptions/Views/HerbSelectionDialog.xaml`
- **组件化模式**: `LYBT.Desktop.Patients/ViewModels/PatientDetailViewModel.cs`

### 8.4 代码模式
- `docs/reference/quick-reference/code-patterns.md` - 组件化ViewModel模式

---

## 📅 九、变更历史

| 日期 | 版本 | 变更内容 | 作者 |
|-----|------|---------|------|
| 2025-11-11 | v1.0 | 初始版本，基于需求文档v2.0最终方案 | Claude Code |

---

**下一步**: 创建GitHub Issue，按Phase 1-3顺序实施
**预计完成时间**: 22工时 ≈ 3.5个工作日（按每日7小时计算）
