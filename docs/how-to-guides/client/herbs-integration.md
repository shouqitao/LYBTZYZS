# Herbs模块集成指南

> **文档类型**: How-to Guide
> **目标读者**: Client端开发人员
> **适用场景**: 将Herbs模块集成到Clinical诊疗流程中

---

## 1. 概述

本指南说明如何将Herbs（药材）模块集成到Clinical诊疗流程中,实现药材选择、处方生成等功能。

**集成目标**：
- 在诊疗流程中选择药材
- 生成包含药材的处方
- 自动计算药材价格
- 药材库存管理集成
- 药材搜索与筛选

**前置条件**：
- Herbs模块已存在（`LYBT.Desktop.Herbs`）
- Clinical模块已实现（参考[Clinical模块开发指南](clinical-development.md)）
- MedicalCase模块已实现

---

## 2. Herbs模块架构概览

### 核心组件

```
LYBT.Desktop.Herbs/
├── ViewModels/
│   ├── HerbSelectorViewModel.cs      # 药材选择器ViewModel
│   ├── HerbSearchViewModel.cs        # 药材搜索ViewModel
│   └── HerbDetailViewModel.cs        # 药材详情ViewModel
├── Views/
│   ├── HerbSelectorView.xaml         # 药材选择器View
│   ├── HerbSearchView.xaml           # 药材搜索View
│   └── HerbDetailView.xaml           # 药材详情View
└── Models/
    └── HerbSelectionModel.cs         # 药材选择模型（包含剂量、单位）
```

### 核心服务

```csharp
public interface IHerbService
{
    Task<IEnumerable<HerbDto>> SearchHerbsAsync(string keyword);
    Task<HerbDto> GetHerbByIdAsync(Guid herbId);
    Task<IEnumerable<HerbDto>> GetHerbsByCategoryAsync(string category);
    Task<decimal> CalculateHerbPriceAsync(Guid herbId, decimal quantity);
}
```

---

## 3. 集成点分析

### 3.1 诊疗流程集成点

```mermaid
graph LR
    A[开始诊疗] --> B[四诊输入]
    B --> C[诊断]
    C --> D[选择药材]
    D --> E[生成处方]
    E --> F[确认收费]
    F --> G[完成诊疗]

    style D fill:#4CAF50
```

**集成位置**：在"诊断"步骤后,"生成处方"步骤前。

### 3.2 数据流向

```
ClinicalHomeViewModel
    ↓ (创建病案)
MedicalCaseFlowViewModel
    ↓ (选择药材)
HerbSelectorViewModel ← IHerbService
    ↓ (生成处方)
PrescriptionViewModel ← IPrescriptionService
    ↓ (计算费用)
PaymentViewModel
```

---

## 4. Step-by-Step 集成步骤

### Step 1: 添加Herbs项目引用

在 `LYBT.Desktop.Clinical.csproj` 中添加引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\LYBT.Desktop.Herbs\LYBT.Desktop.Herbs.csproj" />
</ItemGroup>
```

### Step 2: 在MedicalCaseFlowViewModel中集成药材选择

编辑 `ViewModels/MedicalCaseFlowViewModel.cs`：

```csharp
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Herbs.Models;

namespace LYBT.Desktop.Clinical.ViewModels;

public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    private readonly IHerbService _herbService;
    private readonly IPrescriptionService _prescriptionService;

    // 选中的药材列表
    private ObservableCollection<HerbSelectionModel> _selectedHerbs = new();
    public ObservableCollection<HerbSelectionModel> SelectedHerbs
    {
        get => _selectedHerbs;
        set => SetProperty(ref _selectedHerbs, value);
    }

    // 命令：打开药材选择器
    public ICommand OpenHerbSelectorCommand { get; private set; } = null!;

    public MedicalCaseFlowViewModel(
        IHerbService herbService,
        IPrescriptionService prescriptionService,
        // ... 其他依赖
    )
    {
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
        _prescriptionService = prescriptionService ?? throw new ArgumentNullException(nameof(prescriptionService));

        // 初始化命令
        OpenHerbSelectorCommand = new DelegateCommand(ExecuteOpenHerbSelector);
    }

    private void ExecuteOpenHerbSelector()
    {
        // 打开药材选择器
        var herbSelector = new HerbSelectorView();
        var herbSelectorViewModel = new HerbSelectorViewModel(_herbService);

        // 传递已选药材（如需支持编辑）
        herbSelectorViewModel.SetSelectedHerbs(SelectedHerbs.ToList());

        herbSelector.DataContext = herbSelectorViewModel;

        var dialogResult = herbSelector.ShowDialog();

        if (dialogResult == true)
        {
            // 获取选中的药材
            var newSelections = herbSelectorViewModel.GetSelectedHerbs();
            SelectedHerbs = new ObservableCollection<HerbSelectionModel>(newSelections);

            // 自动生成处方
            _ = GeneratePrescriptionAsync();
        }
    }

    private async Task GeneratePrescriptionAsync()
    {
        try
        {
            if (SelectedHerbs.Count == 0)
            {
                MessageBoxHelper.ShowWarning("请先选择药材");
                return;
            }

            var prescriptionDto = new PrescriptionCreateDto
            {
                MedicalCaseId = CurrentMedicalCaseId,
                HerbItems = SelectedHerbs.Select(h => new PrescriptionItemDto
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Quantity = h.Quantity,
                    Unit = h.Unit,
                    UnitPrice = h.UnitPrice,
                    TotalPrice = h.TotalPrice
                }).ToList()
            };

            var createdPrescription = await _prescriptionService.CreatePrescriptionAsync(prescriptionDto);

            MessageBoxHelper.ShowSuccess("处方生成成功");

            // 发布事件通知其他模块
            _eventAggregator.GetEvent<PrescriptionCreatedEvent>().Publish(createdPrescription);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "生成处方失败");
            MessageBoxHelper.ShowError("生成处方失败，请稍后重试");
        }
    }
}
```

### Step 3: 在MedicalCaseFlowView中添加药材选择按钮

编辑 `Views/MedicalCaseFlowView.xaml`：

```xml
<!-- 诊断步骤后添加药材选择按钮 -->
<StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,20,0,0">
    <Button Content="选择药材" Command="{Binding OpenHerbSelectorCommand}"
            Width="120" Height="36" Background="#4CAF50" Foreground="White"/>

    <TextBlock Text="{Binding SelectedHerbs.Count, StringFormat='已选择 {0} 种药材'}"
               FontSize="14" Foreground="Gray" Margin="20,0,0,0" VerticalAlignment="Center"/>
</StackPanel>

<!-- 已选药材列表 -->
<Border Grid.Row="4" Margin="0,20,0,0" BorderBrush="LightGray" BorderThickness="1" CornerRadius="8" Padding="15">
    <StackPanel>
        <TextBlock Text="已选药材" FontSize="16" FontWeight="Bold" Margin="0,0,0,10"/>

        <ListView ItemsSource="{Binding SelectedHerbs}" BorderThickness="0">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="药材名称" Width="150">
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding HerbName}" FontWeight="Bold"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>

                    <GridViewColumn Header="剂量" Width="100">
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding Quantity, StringFormat='{}{0:F1}'}"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>

                    <GridViewColumn Header="单位" Width="80">
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding Unit}"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>

                    <GridViewColumn Header="单价" Width="100">
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding UnitPrice, StringFormat='¥{0:F2}'}"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>

                    <GridViewColumn Header="小计" Width="100">
                        <GridViewColumn.CellTemplate>
                            <DataTemplate>
                                <TextBlock Text="{Binding TotalPrice, StringFormat='¥{0:F2}'}"/>
                            </DataTemplate>
                        </GridViewColumn.CellTemplate>
                    </GridViewColumn>
                </GridView>
            </ListView.View>
        </ListView>
    </StackPanel>
</Border>
```

### Step 4: 实现HerbSelectionModel

创建 `Models/HerbSelectionModel.cs`（如不存在）：

```csharp
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LYBT.Desktop.Herbs.Models;

/// <summary>
/// 药材选择模型（包含剂量、单价、小计）
/// </summary>
public class HerbSelectionModel : INotifyPropertyChanged
{
    public Guid HerbId { get; set; }

    public string HerbName { get; set; } = string.Empty;

    private decimal _quantity = 10; // 默认剂量10克
    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPrice));
            }
        }
    }

    public string Unit { get; set; } = "克"; // 默认单位"克"

    public decimal UnitPrice { get; set; } // 单价（元/克）

    /// <summary>
    /// 小计 = 剂量 × 单价
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### Step 5: 实现HerbSelectorViewModel

创建 `ViewModels/HerbSelectorViewModel.cs`（如不存在）：

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
using LYBT.Desktop.Herbs.Models;
using LYBT.Shared.Contracts.DTOs;

namespace LYBT.Desktop.Herbs.ViewModels;

/// <summary>
/// 药材选择器ViewModel
/// </summary>
public class HerbSelectorViewModel : ViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IHerbService _herbService;

    public HerbSelectorViewModel(IHerbService herbService)
    {
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));

        SearchCommand = new DelegateCommand(async () => await ExecuteSearchAsync(), CanExecuteSearch);
        AddHerbCommand = new DelegateCommand<HerbDto>(ExecuteAddHerb);
        RemoveHerbCommand = new DelegateCommand<HerbSelectionModel>(ExecuteRemoveHerb);
        ConfirmCommand = new DelegateCommand(ExecuteConfirm);
        CancelCommand = new DelegateCommand(ExecuteCancel);

        // 加载初始数据（所有药材）
        _ = LoadHerbsAsync();
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

    private ObservableCollection<HerbDto> _allHerbs = new();
    public ObservableCollection<HerbDto> AllHerbs
    {
        get => _allHerbs;
        set => SetProperty(ref _allHerbs, value);
    }

    private ObservableCollection<HerbSelectionModel> _selectedHerbs = new();
    public ObservableCollection<HerbSelectionModel> SelectedHerbs
    {
        get => _selectedHerbs;
        set => SetProperty(ref _selectedHerbs, value);
    }

    private HerbDto? _selectedHerbFromList;
    public HerbDto? SelectedHerbFromList
    {
        get => _selectedHerbFromList;
        set => SetProperty(ref _selectedHerbFromList, value);
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
    }

    #endregion

    #region Commands

    public ICommand SearchCommand { get; }
    public ICommand AddHerbCommand { get; }
    public ICommand RemoveHerbCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(SearchKeyword);

    private async Task ExecuteSearchAsync()
    {
        try
        {
            var results = await _herbService.SearchHerbsAsync(SearchKeyword);
            AllHerbs = new ObservableCollection<HerbDto>(results);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "搜索药材失败");
            MessageBoxHelper.ShowError("搜索药材失败，请稍后重试");
        }
    }

    private void ExecuteAddHerb(HerbDto? herb)
    {
        if (herb == null)
            return;

        // 检查是否已添加
        if (SelectedHerbs.Any(h => h.HerbId == herb.Id))
        {
            MessageBoxHelper.ShowWarning($"药材"{herb.Name}"已添加");
            return;
        }

        // 添加到选中列表
        var selection = new HerbSelectionModel
        {
            HerbId = herb.Id,
            HerbName = herb.Name,
            Quantity = 10, // 默认剂量10克
            Unit = "克",
            UnitPrice = herb.UnitPrice
        };

        SelectedHerbs.Add(selection);

        // 更新总金额
        UpdateTotalAmount();

        Logger.Info($"添加药材: {herb.Name}");
    }

    private void ExecuteRemoveHerb(HerbSelectionModel? selection)
    {
        if (selection == null)
            return;

        SelectedHerbs.Remove(selection);
        UpdateTotalAmount();

        Logger.Info($"移除药材: {selection.HerbName}");
    }

    private void ExecuteConfirm()
    {
        // 关闭对话框并返回结果
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

    private async Task LoadHerbsAsync()
    {
        try
        {
            var allHerbs = await _herbService.GetAllHerbsAsync();
            AllHerbs = new ObservableCollection<HerbDto>(allHerbs);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "加载药材列表失败");
        }
    }

    #endregion

    #region Helper Methods

    private void UpdateTotalAmount()
    {
        TotalAmount = SelectedHerbs.Sum(h => h.TotalPrice);
    }

    public void SetSelectedHerbs(List<HerbSelectionModel> herbs)
    {
        SelectedHerbs = new ObservableCollection<HerbSelectionModel>(herbs);
        UpdateTotalAmount();
    }

    public List<HerbSelectionModel> GetSelectedHerbs()
    {
        return SelectedHerbs.ToList();
    }

    #endregion

    #region Dialog Support

    public bool? DialogResult { get; private set; }

    private void CloseWindow()
    {
        // 关闭窗口逻辑（通过事件或其他方式）
    }

    #endregion
}
```

---

## 5. 事件集成

### Step 1: 定义Herbs相关事件

在Foundation项目中创建：

```csharp
// Foundation/Events/PrescriptionCreatedEvent.cs
using Prism.Events;
using LYBT.Shared.Contracts.DTOs;

namespace LYBT.Desktop.Foundation.Events;

public class PrescriptionCreatedEvent : PubSubEvent<PrescriptionDto> { }

// Foundation/Events/HerbSelectedEvent.cs
public class HerbSelectedEvent : PubSubEvent<List<HerbSelectionModel>> { }
```

### Step 2: 发布和订阅事件

**发布事件**（在MedicalCaseFlowViewModel中）：

```csharp
// 处方生成后
_eventAggregator.GetEvent<PrescriptionCreatedEvent>().Publish(createdPrescription);
```

**订阅事件**（在PaymentViewModel中自动更新费用）：

```csharp
private void SubscribeEvents()
{
    _eventAggregator.GetEvent<PrescriptionCreatedEvent>().Subscribe(OnPrescriptionCreated);
}

private async void OnPrescriptionCreated(PrescriptionDto prescription)
{
    // 自动更新费用明细
    await LoadPaymentDetailsAsync(prescription.MedicalCaseId);
}
```

---

## 6. UI样式统一

### Step 1: 创建Herbs模块样式资源

创建 `Styles/HerbsStyles.xaml`：

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- 药材卡片样式 -->
    <Style x:Key="HerbCardStyle" TargetType="Border">
        <Setter Property="Background" Value="White"/>
        <Setter Property="BorderBrush" Value="LightGray"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="Padding" Value="15"/>
        <Setter Property="Margin" Value="5"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Style.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
                <Setter Property="Background" Value="#F5F5F5"/>
            </Trigger>
        </Style.Triggers>
    </Style>

    <!-- 药材选择器按钮样式 -->
    <Style x:Key="AddHerbButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="#4CAF50"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="FontWeight" Value="Bold"/>
        <Setter Property="BorderThickness" Value="0"/>
        <Setter Property="Padding" Value="10,5"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

</ResourceDictionary>
```

### Step 2: 在App.xaml中合并资源

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Herbs模块样式 -->
            <ResourceDictionary Source="pack://application:,,,/LYBT.Desktop.Herbs;component/Styles/HerbsStyles.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

---

## 7. 测试验证

### Step 1: 单元测试

创建 `HerbSelectorViewModelTests.cs`：

```csharp
using Xunit;
using NSubstitute;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Foundation.Services;

namespace LYBT.Desktop.Herbs.Tests;

public class HerbSelectorViewModelTests
{
    private readonly IHerbService _herbService;
    private readonly HerbSelectorViewModel _viewModel;

    public HerbSelectorViewModelTests()
    {
        _herbService = Substitute.For<IHerbService>();
        _viewModel = new HerbSelectorViewModel(_herbService);
    }

    [Fact]
    public async Task AddHerb_ShouldAddToSelectedList()
    {
        // Arrange
        var herb = new HerbDto { Id = Guid.NewGuid(), Name = "当归", UnitPrice = 0.5m };

        // Act
        _viewModel.ExecuteAddHerb(herb);

        // Assert
        Assert.Single(_viewModel.SelectedHerbs);
        Assert.Equal("当归", _viewModel.SelectedHerbs[0].HerbName);
    }

    [Fact]
    public void UpdateTotalAmount_ShouldCalculateCorrectly()
    {
        // Arrange
        var herb1 = new HerbDto { Id = Guid.NewGuid(), Name = "当归", UnitPrice = 0.5m };
        var herb2 = new HerbDto { Id = Guid.NewGuid(), Name = "黄芪", UnitPrice = 0.3m };

        // Act
        _viewModel.ExecuteAddHerb(herb1); // 10克 × 0.5元 = 5元
        _viewModel.ExecuteAddHerb(herb2); // 10克 × 0.3元 = 3元

        // Assert
        Assert.Equal(8m, _viewModel.TotalAmount); // 5元 + 3元 = 8元
    }
}
```

### Step 2: 集成测试

```powershell
# 启动WebAPI
cd src/Server/Services/LYBT.WebAPI
dotnet run --launch-profile Production

# 启动Desktop客户端
cd src/Client/Desktop/Shell/LYBT.Desktop.Shell
dotnet run
```

**测试清单**：
- [ ] 点击"选择药材"按钮打开药材选择器
- [ ] 搜索药材功能正常
- [ ] 可以添加药材到选中列表
- [ ] 剂量可以修改
- [ ] 小计自动计算正确
- [ ] 总金额自动更新
- [ ] 可以移除已选药材
- [ ] 确认后药材列表显示在诊疗流程中
- [ ] 自动生成处方
- [ ] 处方创建后触发事件通知

---

## 8. 常见问题

### Q1: 药材列表不显示

**解决方案**：检查IHerbService是否已注册：

```csharp
containerRegistry.RegisterSingleton<IHerbService, HerbService>();
```

### Q2: 小计计算不正确

**解决方案**：检查HerbSelectionModel的TotalPrice属性是否正确触发PropertyChanged：

```csharp
public decimal Quantity
{
    get => _quantity;
    set
    {
        if (_quantity != value)
        {
            _quantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalPrice)); // ← 必须触发
        }
    }
}
```

### Q3: 处方生成失败

**解决方案**：检查API端点和数据格式：

```powershell
curl -X POST https://localhost:5001/api/v1/prescriptions \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "medicalCaseId": "xxx",
    "herbItems": [...]
  }'
```

---

## 9. 参考资料

- [Clinical模块开发指南](clinical-development.md)
- [Prescription API文档](../../api/prescription-api.md)
- [业务规则：处方生成规则](../../explanation/business-rules.md#pr-001-处方生成规则)
