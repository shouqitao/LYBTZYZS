# Formula模块集成指南

> **文档类型**: How-to Guide
> **目标读者**: Client端开发人员
> **适用场景**: 将Formula（验方）模块集成到Clinical诊疗流程中

---

## 1. 概述

本指南说明如何将Formula（验方）模块集成到Clinical诊疗流程中,实现验方选择、一键套用、快速处方生成等功能。

**集成目标**：
- 在诊疗流程中选择验方模板
- 一键套用验方（自动填充药材）
- 验方药材剂量调整
- 保存常用验方
- 验方分类管理

**前置条件**：
- Formula模块已存在（`LYBT.Desktop.Formula`）
- Herbs模块已集成（参考[Herbs模块集成指南](herbs-integration.md)）
- Clinical模块已实现（参考[Clinical模块开发指南](clinical-development.md)）

---

## 2. Formula模块架构概览

### 核心组件

```
LYBT.Desktop.Formula/
├── ViewModels/
│   ├── FormulaSelectorViewModel.cs     # 验方选择器ViewModel
│   ├── FormulaDetailViewModel.cs       # 验方详情ViewModel
│   └── FormulaEditorViewModel.cs       # 验方编辑器ViewModel
├── Views/
│   ├── FormulaSelectorView.xaml        # 验方选择器View
│   ├── FormulaDetailView.xaml          # 验方详情View
│   └── FormulaEditorView.xaml          # 验方编辑器View
└── Models/
    └── FormulaApplicationModel.cs      # 验方应用模型
```

### 核心服务

```csharp
public interface IFormulaService
{
    Task<IEnumerable<FormulaDto>> GetAllFormulasAsync();
    Task<FormulaDto> GetFormulaByIdAsync(Guid formulaId);
    Task<IEnumerable<FormulaDto>> SearchFormulasAsync(string keyword);
    Task<FormulaDto> CreateFormulaAsync(FormulaCreateDto createDto);
    Task<FormulaDto> UpdateFormulaAsync(Guid formulaId, FormulaUpdateDto updateDto);
    Task DeleteFormulaAsync(Guid formulaId);
}
```

---

## 3. 验方数据模型

### FormulaDto结构

```csharp
public class FormulaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // 验方名称（如"四君子汤"）
    public string Category { get; set; } = string.Empty; // 分类（如"补益剂"）
    public string Description { get; set; } = string.Empty; // 功效描述
    public List<FormulaItemDto> Items { get; set; } = new(); // 药材列表
    public DateTime CreatedAt { get; set; }
}

public class FormulaItemDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } // 标准剂量
    public string Unit { get; set; } = "克";
}
```

---

## 4. Step-by-Step 集成步骤

### Step 1: 添加Formula项目引用

在 `LYBT.Desktop.Clinical.csproj` 中添加引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\LYBT.Desktop.Formula\LYBT.Desktop.Formula.csproj" />
</ItemGroup>
```

### Step 2: 在MedicalCaseFlowViewModel中集成验方选择

编辑 `ViewModels/MedicalCaseFlowViewModel.cs`：

```csharp
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Formula.Models;

namespace LYBT.Desktop.Clinical.ViewModels;

public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    private readonly IFormulaService _formulaService;
    private readonly IHerbService _herbService;

    // 当前应用的验方
    private FormulaDto? _appliedFormula;
    public FormulaDto? AppliedFormula
    {
        get => _appliedFormula;
        set => SetProperty(ref _appliedFormula, value);
    }

    // 命令：打开验方选择器
    public ICommand OpenFormulaSelectorCommand { get; private set; } = null!;

    public MedicalCaseFlowViewModel(
        IFormulaService formulaService,
        IHerbService herbService,
        // ... 其他依赖
    )
    {
        _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));

        // 初始化命令
        OpenFormulaSelectorCommand = new DelegateCommand(ExecuteOpenFormulaSelector);
    }

    private void ExecuteOpenFormulaSelector()
    {
        // 打开验方选择器
        var formulaSelector = new FormulaSelectorView();
        var formulaSelectorViewModel = new FormulaSelectorViewModel(_formulaService);

        formulaSelector.DataContext = formulaSelectorViewModel;

        var dialogResult = formulaSelector.ShowDialog();

        if (dialogResult == true)
        {
            // 获取选中的验方
            var selectedFormula = formulaSelectorViewModel.GetSelectedFormula();
            AppliedFormula = selectedFormula;

            // 自动填充药材到SelectedHerbs
            ApplyFormulaToHerbs(selectedFormula);
        }
    }

    /// <summary>
    /// 将验方药材应用到SelectedHerbs列表
    /// </summary>
    private void ApplyFormulaToHerbs(FormulaDto formula)
    {
        try
        {
            // 清空现有药材
            SelectedHerbs.Clear();

            // 将验方药材转换为HerbSelectionModel
            foreach (var item in formula.Items)
            {
                var selection = new HerbSelectionModel
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = GetHerbUnitPrice(item.HerbId) // 查询单价
                };

                SelectedHerbs.Add(selection);
            }

            MessageBoxHelper.ShowSuccess($"已套用验方：{formula.Name}");
            Logger.Info($"套用验方: {formula.Name}，包含 {formula.Items.Count} 种药材");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "套用验方失败");
            MessageBoxHelper.ShowError("套用验方失败，请稍后重试");
        }
    }

    private decimal GetHerbUnitPrice(Guid herbId)
    {
        // 从Herb服务查询单价（可以缓存）
        var herb = _herbService.GetHerbByIdAsync(herbId).Result;
        return herb?.UnitPrice ?? 0m;
    }
}
```

### Step 3: 在MedicalCaseFlowView中添加验方选择按钮

编辑 `Views/MedicalCaseFlowView.xaml`：

```xml
<!-- 诊断步骤后添加验方选择按钮 -->
<StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,20,0,0">
    <Button Content="选择验方" Command="{Binding OpenFormulaSelectorCommand}"
            Width="120" Height="36" Background="#2196F3" Foreground="White" Margin="0,0,10,0"/>

    <Button Content="选择药材" Command="{Binding OpenHerbSelectorCommand}"
            Width="120" Height="36" Background="#4CAF50" Foreground="White"/>

    <TextBlock Text="{Binding AppliedFormula.Name, StringFormat='已套用验方: {0}'}"
               FontSize="14" Foreground="#2196F3" Margin="20,0,0,0" VerticalAlignment="Center"
               Visibility="{Binding AppliedFormula, Converter={StaticResource NullToVisibilityConverter}}"/>
</StackPanel>

<!-- 验方详情卡片 -->
<Border Grid.Row="4" Margin="0,10,0,0" Background="#E3F2FD" BorderBrush="#2196F3" BorderThickness="1" CornerRadius="8" Padding="15"
        Visibility="{Binding AppliedFormula, Converter={StaticResource NullToVisibilityConverter}}">
    <StackPanel>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="验方：" FontSize="14" FontWeight="Bold"/>
            <TextBlock Text="{Binding AppliedFormula.Name}" FontSize="14" Foreground="#2196F3" Margin="5,0,0,0"/>
        </StackPanel>

        <TextBlock Text="{Binding AppliedFormula.Description}" FontSize="12" Foreground="Gray" Margin="0,5,0,0" TextWrapping="Wrap"/>

        <TextBlock Text="{Binding AppliedFormula.Items.Count, StringFormat='包含 {0} 种药材'}"
                   FontSize="12" Foreground="Gray" Margin="0,5,0,0"/>
    </StackPanel>
</Border>
```

### Step 4: 实现FormulaSelectorViewModel

创建 `ViewModels/FormulaSelectorViewModel.cs`（如不存在）：

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NLog;
using Prism.Commands;
using LYBT.Desktop.Foundation.ViewModels;
using LYBT.Desktop.Foundation.Services;
using LYBT.Desktop.Foundation.Helpers;
using LYBT.Shared.Contracts.DTOs;

namespace LYBT.Desktop.Formula.ViewModels;

/// <summary>
/// 验方选择器ViewModel
/// </summary>
public class FormulaSelectorViewModel : ViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IFormulaService _formulaService;

    public FormulaSelectorViewModel(IFormulaService formulaService)
    {
        _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));

        SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), CanExecuteSearch);
        SelectFormulaCommand = new DelegateCommand<FormulaDto>(ExecuteSelectFormula);
        ViewDetailCommand = new DelegateCommand<FormulaDto>(ExecuteViewDetail);
        ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
        CancelCommand = new DelegateCommand(ExecuteCancel);

        // 加载初始数据（所有验方）
        _ = LoadFormulasAsync();
    }

    #region Properties

    private string _searchKeyword = string.Empty;
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                (SearchCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private string _selectedCategory = "全部";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _ = FilterFormulasByCategoryAsync();
            }
        }
    }

    private ObservableCollection<string> _categories = new() { "全部", "补益剂", "解表剂", "清热剂", "泻下剂" };
    public ObservableCollection<string> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    private ObservableCollection<FormulaDto> _allFormulas = new();
    public ObservableCollection<FormulaDto> AllFormulas
    {
        get => _allFormulas;
        set => SetProperty(ref _allFormulas, value);
    }

    private ObservableCollection<FormulaDto> _displayedFormulas = new();
    public ObservableCollection<FormulaDto> DisplayedFormulas
    {
        get => _displayedFormulas;
        set => SetProperty(ref _displayedFormulas, value);
    }

    private FormulaDto? _selectedFormula;
    public FormulaDto? SelectedFormula
    {
        get => _selectedFormula;
        set
        {
            if (SetProperty(ref _selectedFormula, value))
            {
                (ConfirmCommand as DelegateCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand SearchCommand { get; }
    public ICommand SelectFormulaCommand { get; }
    public ICommand ViewDetailCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(SearchKeyword);

    private async Task ExecuteSearchAsync()
    {
        try
        {
            var results = await _formulaService.SearchFormulasAsync(SearchKeyword);
            DisplayedFormulas = new ObservableCollection<FormulaDto>(results);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "搜索验方失败");
            MessageBoxHelper.ShowError("搜索验方失败，请稍后重试");
        }
    }

    private void ExecuteSelectFormula(FormulaDto? formula)
    {
        SelectedFormula = formula;
    }

    private void ExecuteViewDetail(FormulaDto? formula)
    {
        if (formula == null)
            return;

        // 打开验方详情对话框
        var detailView = new FormulaDetailView();
        var detailViewModel = new FormulaDetailViewModel(_formulaService, formula);
        detailView.DataContext = detailViewModel;
        detailView.ShowDialog();
    }

    private bool CanExecuteConfirm() => SelectedFormula != null;

    private void ExecuteConfirm()
    {
        DialogResult = true;
        CloseWindow();
    }

    private void ExecuteCancel()
    {
        DialogResult = false;
        CloseWindow();
    }

    #endregion

    #region Data Loading

    private async Task LoadFormulasAsync()
    {
        try
        {
            var allFormulas = await _formulaService.GetAllFormulasAsync();
            AllFormulas = new ObservableCollection<FormulaDto>(allFormulas);
            DisplayedFormulas = new ObservableCollection<FormulaDto>(allFormulas);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "加载验方列表失败");
        }
    }

    private async Task FilterFormulasByCategoryAsync()
    {
        if (SelectedCategory == "全部")
        {
            DisplayedFormulas = new ObservableCollection<FormulaDto>(AllFormulas);
        }
        else
        {
            var filtered = AllFormulas.Where(f => f.Category == SelectedCategory).ToList();
            DisplayedFormulas = new ObservableCollection<FormulaDto>(filtered);
        }
    }

    #endregion

    #region Helper Methods

    public FormulaDto? GetSelectedFormula()
    {
        return SelectedFormula;
    }

    #endregion

    #region Dialog Support

    public bool? DialogResult { get; private set; }

    private void CloseWindow()
    {
        // 关闭窗口逻辑
    }

    #endregion
}
```

### Step 5: 创建FormulaSelectorView

创建 `Views/FormulaSelectorView.xaml`：

```xml
<Window x:Class="LYBT.Desktop.Formula.Views.FormulaSelectorView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:prism="http://prismlibrary.com/"
        prism:ViewModelLocator.AutoWireViewModel="True"
        Title="选择验方" Width="800" Height="600"
        WindowStartupLocation="CenterOwner">

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 搜索栏 -->
        <Grid Grid.Row="0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <TextBox Grid.Column="0" Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                     Height="36" VerticalContentAlignment="Center" Padding="10"
                     FontSize="14"/>

            <Button Grid.Column="1" Content="搜索" Command="{Binding SearchCommand}"
                    Width="80" Height="36" Margin="10,0,0,0"/>
        </Grid>

        <!-- 分类筛选 -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,15,0,0">
            <TextBlock Text="分类：" FontSize="14" VerticalAlignment="Center" Margin="0,0,10,0"/>

            <ComboBox ItemsSource="{Binding Categories}"
                      SelectedItem="{Binding SelectedCategory}"
                      Width="120" Height="32"/>
        </StackPanel>

        <!-- 验方列表 -->
        <Border Grid.Row="2" Margin="0,15,0,0" BorderBrush="LightGray" BorderThickness="1" CornerRadius="8">
            <ListBox ItemsSource="{Binding DisplayedFormulas}"
                     SelectedItem="{Binding SelectedFormula}"
                     SelectionMode="Single"
                     BorderThickness="0">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border Style="{StaticResource FormulaCardStyle}">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>

                                <!-- 验方名称 -->
                                <TextBlock Grid.Row="0" Text="{Binding Name}" FontSize="16" FontWeight="Bold"/>

                                <!-- 功效描述 -->
                                <TextBlock Grid.Row="1" Text="{Binding Description}" FontSize="12" Foreground="Gray" Margin="0,5,0,0" TextWrapping="Wrap"/>

                                <!-- 药材数量 + 分类标签 -->
                                <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,10,0,0">
                                    <Border Background="#E3F2FD" CornerRadius="4" Padding="8,4">
                                        <TextBlock Text="{Binding Category}" FontSize="10" Foreground="#2196F3"/>
                                    </Border>

                                    <TextBlock Text="{Binding Items.Count, StringFormat='包含 {0} 种药材'}"
                                               FontSize="10" Foreground="Gray" Margin="10,0,0,0" VerticalAlignment="Center"/>
                                </StackPanel>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </Border>

        <!-- 按钮 -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <Button Content="查看详情" Command="{Binding ViewDetailCommand}" CommandParameter="{Binding SelectedFormula}"
                    Width="100" Height="36" Margin="0,0,10,0"/>

            <Button Content="套用验方" Command="{Binding ConfirmCommand}"
                    Width="100" Height="36" Margin="0,0,10,0" IsDefault="True"
                    Background="#2196F3" Foreground="White"/>

            <Button Content="取消" Command="{Binding CancelCommand}"
                    Width="80" Height="36" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>
```

---

## 5. 保存常用验方

### Step 1: 添加保存验方功能

在 `MedicalCaseFlowViewModel` 中添加保存当前配方为验方的功能：

```csharp
public ICommand SaveAsFormulaCommand { get; private set; } = null!;

private void InitializeCommands()
{
    // ... 其他命令
    SaveAsFormulaCommand = new DelegateCommand(async () => await ExecuteSaveAsFormulaAsync(), CanExecuteSaveAsFormula);
}

private bool CanExecuteSaveAsFormula() => SelectedHerbs.Count > 0;

private async Task ExecuteSaveAsFormulaAsync()
{
    try
    {
        // 打开保存对话框
        var input = MessageBoxHelper.ShowInputDialog("请输入验方名称:", "保存为验方");
        if (string.IsNullOrWhiteSpace(input))
            return;

        var formulaName = input;

        var createDto = new FormulaCreateDto
        {
            Name = formulaName,
            Category = "自定义",
            Description = $"来自病案 {CurrentMedicalCaseId}",
            Items = SelectedHerbs.Select(h => new FormulaItemDto
            {
                HerbId = h.HerbId,
                HerbName = h.HerbName,
                Quantity = h.Quantity,
                Unit = h.Unit
            }).ToList()
        };

        var createdFormula = await _formulaService.CreateFormulaAsync(createDto);

        MessageBoxHelper.ShowSuccess($"验方"{formulaName}"保存成功");
        Logger.Info($"保存验方成功: {formulaName}");
    }
    catch (Exception ex)
    {
        Logger.Error(ex, "保存验方失败");
        MessageBoxHelper.ShowError("保存验方失败，请稍后重试");
    }
}
```

### Step 2: 在UI中添加保存按钮

```xml
<Button Content="保存为验方" Command="{Binding SaveAsFormulaCommand}"
        Width="120" Height="36" Margin="0,0,10,0"/>
```

---

## 6. 验方与药材选择的协同

### Step 1: 允许在验方基础上调整药材

**场景**：套用验方后，医生可以增加或删除药材（加减化裁）。

**实现**：
```csharp
// 套用验方后，SelectedHerbs已填充
// 用户可以：
// 1. 点击"选择药材"按钮添加新药材
// 2. 在SelectedHerbs列表中删除药材
// 3. 修改剂量
```

**UI提示**：
```xml
<TextBlock Text="验方基础上已调整" FontSize="12" Foreground="Orange"
           Visibility="{Binding IsFormulaModified, Converter={StaticResource BoolToVisibilityConverter}}"/>
```

### Step 2: 记录验方应用历史

**目的**：记录哪些病案使用了哪些验方，用于统计分析。

**实现**：
```csharp
private async Task RecordFormulaApplicationAsync(Guid medicalCaseId, Guid formulaId)
{
    var applicationDto = new FormulaApplicationDto
    {
        MedicalCaseId = medicalCaseId,
        FormulaId = formulaId,
        AppliedAt = DateTime.Now,
        DoctorId = SessionManager?.CurrentUser?.Id ?? Guid.Empty
    };

    await _formulaService.RecordApplicationAsync(applicationDto);
}
```

---

## 7. 事件集成

### Step 1: 定义Formula相关事件

在Foundation项目中创建：

```csharp
// Foundation/Events/FormulaAppliedEvent.cs
using Prism.Events;
using LYBT.Shared.Contracts.DTOs;

namespace LYBT.Desktop.Foundation.Events;

public class FormulaAppliedEvent : PubSubEvent<FormulaApplicationModel> { }

public class FormulaApplicationModel
{
    public Guid FormulaId { get; set; }
    public string FormulaName { get; set; } = string.Empty;
    public int HerbCount { get; set; }
}
```

### Step 2: 发布和订阅事件

**发布事件**（在套用验方后）：

```csharp
_eventAggregator.GetEvent<FormulaAppliedEvent>().Publish(new FormulaApplicationModel
{
    FormulaId = selectedFormula.Id,
    FormulaName = selectedFormula.Name,
    HerbCount = selectedFormula.Items.Count
});
```

**订阅事件**（在统计模块中记录）：

```csharp
_eventAggregator.GetEvent<FormulaAppliedEvent>().Subscribe(OnFormulaApplied);

private void OnFormulaApplied(FormulaApplicationModel application)
{
    Logger.Info($"验方应用: {application.FormulaName}, 包含 {application.HerbCount} 种药材");
    // 记录到统计数据
}
```

---

## 8. UI样式统一

### Step 1: 创建Formula模块样式资源

创建 `Styles/FormulaStyles.xaml`：

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 验方卡片样式 -->
    <Style x:Key="FormulaCardStyle" TargetType="Border">
        <Setter Property="Background" Value="White"/>
        <Setter Property="BorderBrush" Value="#2196F3"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="Padding" Value="15"/>
        <Setter Property="Margin" Value="5"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#E3F2FD"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- 验方按钮样式 -->
    <Style x:Key="ApplyFormulaButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="#2196F3"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="10,5"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

</ResourceDictionary>
```

---

## 9. 测试验证

### Step 1: 单元测试

创建 `FormulaSelectorViewModelTests.cs`：

```csharp
using Xunit;
using NSubstitute;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Foundation.Services;

namespace LYBT.Desktop.Formula.Tests;

public class FormulaSelectorViewModelTests
{
    private readonly IFormulaService _formulaService;
    private readonly FormulaSelectorViewModel _viewModel;

    public FormulaSelectorViewModelTests()
    {
        _formulaService = Substitute.For<IFormulaService>();
        _viewModel = new FormulaSelectorViewModel(_formulaService);
    }

    [Fact]
    public async Task SelectFormula_ShouldSetSelectedFormula()
    {
        // Arrange
        var formula = new FormulaDto
        {
            Id = Guid.NewGuid(),
            Name = "四君子汤",
            Items = new List<FormulaItemDto>()
        };

        // Act
        _viewModel.ExecuteSelectFormula(formula);

        // Assert
        Assert.Equal(formula, _viewModel.SelectedFormula);
    }

    [Fact]
    public async Task FilterByCategory_ShouldShowOnlyCategoryFormulas()
    {
        // Arrange
        _viewModel.AllFormulas = new ObservableCollection<FormulaDto>
        {
            new() { Name = "四君子汤", Category = "补益剂" },
            new() { Name = "麻黄汤", Category = "解表剂" }
        };

        // Act
        _viewModel.SelectedCategory = "补益剂";
        await _viewModel.FilterFormulasByCategoryAsync();

        // Assert
        Assert.Single(_viewModel.DisplayedFormulas);
        Assert.Equal("四君子汤", _viewModel.DisplayedFormulas[0].Name);
    }
}
```

### Step 2: 集成测试

**测试清单**：
- [ ] 点击"选择验方"按钮打开验方选择器
- [ ] 验方按分类筛选正常
- [ ] 搜索验方功能正常
- [ ] 选中验方后可以查看详情
- [ ] 套用验方后自动填充药材到SelectedHerbs
- [ ] 药材剂量自动填充
- [ ] 药材单价自动查询
- [ ] 套用验方后可以继续调整药材（加减）
- [ ] 保存为验方功能正常
- [ ] 验方应用后触发事件通知

---

## 10. 常见问题

### Q1: 套用验方后药材列表为空

**解决方案**：检查 `ApplyFormulaToHerbs` 方法是否正确填充：

```csharp
foreach (var item in formula.Items)
{
    var selection = new HerbSelectionModel
    {
        HerbId = item.HerbId,
        HerbName = item.HerbName,
        Quantity = item.Quantity,
        Unit = item.Unit,
        UnitPrice = GetHerbUnitPrice(item.HerbId)
    };
    SelectedHerbs.Add(selection);
}
```

### Q2: 验方保存失败

**解决方案**：检查API端点和权限：

```powershell
curl -X POST https://localhost:5001/api/v1/formulas \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "四君子汤",
    "category": "补益剂",
    "items": [...]
  }'
```

### Q3: 验方分类不显示

**解决方案**：检查 `FormulaDto.Category` 字段是否正确返回：

```csharp
public class FormulaDto
{
    public string Category { get; set; } = string.Empty; // ← 必须有值
}
```

---

## 11. 参考资料

- [Clinical模块开发指南](clinical-development.md)
- [Herbs模块集成指南](herbs-integration.md)
- [Formula API文档](../../api/formula-api.md)
- [业务规则：验方管理规则](../../explanation/business-rules.md#fm-001-验方管理规则)
