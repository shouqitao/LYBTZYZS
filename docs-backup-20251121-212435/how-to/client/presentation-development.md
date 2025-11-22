# Client端呈现层开发指南

## 📋 概述

本文档提供LYBT Desktop端**呈现层（Presentation Layer）**的完整开发指南，涵盖ViewModel、View、数据绑定、命令、导航、对话框、样式和模块注册的最佳实践。

**适用场景**：
- ✅ 开发新的Desktop功能模块
- ✅ 创建ViewModel和View
- ✅ 实现数据绑定和命令
- ✅ 处理导航和对话框
- ✅ 应用统一样式系统

**核心原则**：
- **MVVM模式**：严格分离View（XAML）和ViewModel（C#）
- **Prism框架**：使用Prism 8.x进行模块化和导航
- **依赖注入**：仅使用构造函数注入
- **数据绑定**：利用WPF强大的数据绑定引擎
- **命令模式**：使用DelegateCommand处理用户交互

**参考文档**：
- 架构设计：`docs/explanation/architecture/client/presentation-design.md`
- Server端对齐：`docs/how-to-guides/server/` 对应模块开发指南
- 共享组件：`docs/how-to-guides/shared/components-usage.md`

---

## 🔧 前置条件

### 1. 开发环境

**必需工具**：
```
- Visual Studio 2022（17.8+）
- .NET 8.0 SDK
- WPF开发工作负载
- XAML设计器
```

**推荐扩展**：
- XAML Styler（代码格式化）
- ReSharper（代码分析）
- Prism Template Pack（快速创建）

### 2. 核心依赖

**NuGet包版本**：
```xml
<PackageReference Include="Prism.Wpf" Version="8.1.537" />
<PackageReference Include="Prism.DryIoc" Version="8.1.271" />
<PackageReference Include="MaterialDesignThemes" Version="5.1.0" />
```

### 3. 项目结构理解

**典型模块结构**：
```
LYBT.Desktop.{ModuleName}/
├── Views/                    # XAML视图
│   ├── {Feature}View.xaml
│   ├── {Feature}ListView.xaml
│   └── Dialogs/
│       └── {Dialog}Dialog.xaml
├── ViewModels/               # 视图模型
│   ├── {Feature}ViewModel.cs
│   ├── {Feature}ListViewModel.cs
│   └── Dialogs/
│       └── {Dialog}DialogViewModel.cs
├── Models/                   # 展示模型
│   └── {Feature}DisplayModel.cs
├── Converters/               # 值转换器
│   └── {Custom}Converter.cs
├── Repositories/             # 数据访问层接口
│   └── I{Feature}Repository.cs
└── {ModuleName}Module.cs     # Prism模块注册
```

---

## 🏗️ ViewModel开发

### 3.1 选择合适的基类

**ViewModelBase（推荐用于大多数场景）**：
```csharp
using LYBT.Desktop.Foundation.ViewModels;

public class PatientListViewModel : ViewModelBase
{
    private string _searchKeyword = string.Empty;

    // ✅ 自动实现属性变更通知
    public string SearchKeyword
    {
        get => _searchKeyword;
        set => SetProperty(ref _searchKeyword, value);
    }

    // ✅ 自动实现命令
    public DelegateCommand SearchCommand { get; }

    public PatientListViewModel(IPatientRepository repository, ILogger<PatientListViewModel> logger)
        : base(logger)
    {
        SearchCommand = new DelegateCommand(ExecuteSearch, CanExecuteSearch)
            .ObservesProperty(() => SearchKeyword);
    }

    private void ExecuteSearch()
    {
        // 搜索逻辑
    }

    private bool CanExecuteSearch()
    {
        return !string.IsNullOrWhiteSpace(SearchKeyword);
    }
}
```

**UnifiedViewModelBase（用于需要Prism生命周期的场景）**：
```csharp
using LYBT.Desktop.Foundation.ViewModels;

public class MedicalCaseFlowViewModel : UnifiedViewModelBase, INavigationAware
{
    private int _currentStep = 1;

    public int CurrentStep
    {
        get => _currentStep;
        set => SetProperty(ref _currentStep, value);
    }

    public MedicalCaseFlowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        ILogger<MedicalCaseFlowViewModel> logger)
        : base(regionManager, eventAggregator, logger)
    {
    }

    // ✅ 导航进入时调用
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var patientId = navigationContext.Parameters.GetValue<Guid>("patientId");
        LoadPatientData(patientId);
    }

    // ✅ 导航离开前调用
    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 清理资源
    }
}
```

### 3.2 属性定义规范

**基本属性**：
```csharp
// ❌ 错误：手动实现INotifyPropertyChanged
private string _name;
public string Name
{
    get => _name;
    set
    {
        _name = value;
        OnPropertyChanged(nameof(Name));
    }
}

// ✅ 正确：使用SetProperty自动通知
private string _name = string.Empty;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}
```

**计算属性**：
```csharp
public class PrescriptionEditorViewModel : ViewModelBase
{
    private decimal _totalPrice;
    public decimal TotalPrice
    {
        get => _totalPrice;
        set
        {
            if (SetProperty(ref _totalPrice, value))
            {
                // ✅ 触发关联属性通知
                RaisePropertyChanged(nameof(FormattedTotalPrice));
            }
        }
    }

    // 只读计算属性
    public string FormattedTotalPrice => $"总价：¥{TotalPrice:F2}";
}
```

**集合属性**：
```csharp
using System.Collections.ObjectModel;

public class HerbSelectionDialogViewModel : ViewModelBase
{
    // ✅ 使用ObservableCollection自动通知UI
    public ObservableCollection<HerbDisplayModel> Herbs { get; }

    public HerbSelectionDialogViewModel()
    {
        Herbs = new ObservableCollection<HerbDisplayModel>();
    }

    private void LoadHerbs()
    {
        Herbs.Clear();
        foreach (var herb in GetHerbsFromRepository())
        {
            Herbs.Add(herb);
        }
    }
}
```

### 3.3 命令定义规范

**基本命令**：
```csharp
public class PatientListViewModel : ViewModelBase
{
    public DelegateCommand AddCommand { get; }
    public DelegateCommand<PatientDisplayModel> EditCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    public PatientListViewModel()
    {
        // 无参数命令
        AddCommand = new DelegateCommand(ExecuteAdd);

        // 带参数命令
        EditCommand = new DelegateCommand<PatientDisplayModel>(ExecuteEdit);

        // 异步命令
        RefreshCommand = new DelegateCommand(async () => await LoadPatientsAsync());
    }

    private void ExecuteAdd()
    {
        // 新增逻辑
    }

    private void ExecuteEdit(PatientDisplayModel patient)
    {
        if (patient == null) return;
        // 编辑逻辑
    }
}
```

**带CanExecute的命令**：
```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    private bool _isDataValid;

    public bool IsDataValid
    {
        get => _isDataValid;
        set => SetProperty(ref _isDataValid, value);
    }

    public DelegateCommand NextStepCommand { get; }

    public MedicalCaseFlowViewModel()
    {
        // ✅ 使用ObservesProperty自动更新CanExecute
        NextStepCommand = new DelegateCommand(ExecuteNextStep, CanExecuteNextStep)
            .ObservesProperty(() => IsDataValid);
    }

    private void ExecuteNextStep()
    {
        CurrentStep++;
    }

    private bool CanExecuteNextStep()
    {
        return IsDataValid;
    }
}
```

### 3.4 异步操作规范

**加载数据**：
```csharp
public class PatientListViewModel : ViewModelBase
{
    private readonly IPatientRepository _repository;
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<PatientDisplayModel> Patients { get; }

    public DelegateCommand LoadCommand { get; }

    public PatientListViewModel(IPatientRepository repository, ILogger<PatientListViewModel> logger)
        : base(logger)
    {
        _repository = repository;
        Patients = new ObservableCollection<PatientDisplayModel>();
        LoadCommand = new DelegateCommand(async () => await LoadPatientsAsync());
    }

    private async Task LoadPatientsAsync()
    {
        try
        {
            IsLoading = true;

            var result = await _repository.GetAllAsync();
            if (result.IsSuccess)
            {
                Patients.Clear();
                foreach (var patient in result.Data)
                {
                    Patients.Add(patient);
                }
            }
            else
            {
                Logger.LogError("加载患者失败：{Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者异常");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 3.5 组件化设计模式（Issue #1790+）

> **⚠️ 重要**：当ViewModel代码量超过300行或承担3个以上职责时，应考虑组件化重构。

**核心理念**：

ViewModel不应直接处理复杂的业务逻辑，而应委托给专门的Manager/Handler组件：

```
传统模式:  ViewModel (700行) → Repository → API
                ↓
组件化模式:  ViewModel (350行)
              ├→ SearchManager → CommandHandler → API
              ├→ PendingQueueManager → CommandHandler → API
              └→ UnfinishedCaseHandler → CommandHandler → API
```

**典型场景**：

| 场景 | 传统做法（❌） | 组件化做法（✅） |
|-----|------------|-------------|
| 患者搜索+分页 | ViewModel内200行 | 提取`PatientSearchManager` |
| 待诊队列管理 | ViewModel内150行 | 提取`PendingQueueManager` |
| 药材选择+验证 | ViewModel内100行 | 提取`HerbSelectionHandler` |
| 打印数据准备 | ViewModel内180行 | 提取`PrintDataBuilder` |

**组件注入示例**（Issue #1790）：

```csharp
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    #region 组件依赖

    // Issue #1790: 组件化服务
    private readonly PatientSearchManager _searchManager;         // 负责搜索和分页（~200行）
    private readonly UnfinishedCaseHandler _unfinishedCaseHandler; // 负责未完成医案查询（~100行）
    private readonly PendingQueueManager _pendingQueueManager;    // 负责待诊队列（~150行）

    #endregion

    public PatientSelectionViewModel(
        PatientSearchManager searchManager,             // ✅ 注入搜索管理器
        UnfinishedCaseHandler unfinishedCaseHandler,   // ✅ 注入医案处理器
        PendingQueueManager pendingQueueManager,       // ✅ 注入队列管理器
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        /* ... */)
        : base(eventAggregator, loggerFactory, /* ... */)
    {
        _searchManager = searchManager ?? throw new ArgumentNullException(nameof(searchManager));
        _unfinishedCaseHandler = unfinishedCaseHandler ?? throw new ArgumentNullException(nameof(unfinishedCaseHandler));
        _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));

        // ViewModel只负责UI协调，业务逻辑委托给组件
        SearchCommand = new DelegateCommand<string>(
            async (keyword) => await _searchManager.ExecuteSearchAsync(keyword));
    }
}
```

**重构效果**（Issue #1790真实数据）：

| 指标 | 重构前 | 重构后 | 改善 |
|-----|-------|-------|------|
| **ViewModel行数** | 726 行 | 350 行 | **-52%** |
| **单方法复杂度** | Critical（>100行） | Low（<50行） | **降4级** |
| **职责数量** | 5个职责 | 2个职责 | **-60%** |
| **可测试性** | Mock 8个依赖 | Mock 3个组件 | **-62%** |

**完整指南** → [../../explanation/architecture/client/component-pattern.md](../../explanation/architecture/client/component-pattern.md)

**DI注册** → 参见第11.1节"模块注册"

---

### 3.6 方法复杂度控制（Issue #1795+）

> **⚠️ 强制规范**：单方法行数超过50行必须拆分，超过100行立即重构（Priority P0）。

**复杂度级别定义**：

| 级别 | 行数范围 | 状态 | 处理策略 | 优先级 |
|------|---------|------|---------|--------|
| **Low** | <50 行 | ✅ 可接受 | 保持现状 | - |
| **Medium** | 50-75 行 | ⚠️ 建议拆分 | 排期优化 | P2-P3 |
| **High** | 75-100 行 | 🔴 优先拆分 | 2周内完成 | P1-P2 |
| **Critical** | >100 行 | 🚨 必须拆分 | 立即处理 | P0 |

**重构触发条件**（满足任一即触发）：

**定量指标**：
- 📊 方法行数 ≥ 50 行
- 📊 圈复杂度 > 10
- 📊 嵌套层级 > 3 层

**业务指标**：
- 🔄 近1个月修改频率 ≥ 3 次
- 🐛 因复杂度导致的Bug ≥ 2 次
- 👥 Code Review反馈复杂难懂

**Extract Method重构模式**（Issue #1795）：

**重构前**（77行，High复杂度）：
```csharp
// ❌ 复杂方法：选择药材、打开对话框、处理结果、验证映射（77行）
private async Task SelectHerbAsync(FormulaHerbItemDto? herbItem)
{
    if (herbItem == null || SelectedFormula == null) return;
    if (herbItem.IsValidated) { await ShowWarningMessageAsync("该药材已校验"); return; }

    try
    {
        SetIsBusy(true, $"正在处理药材「{herbItem.HerbName}」...");

        // 创建对话框参数（10行）
        var parameters = new DialogParameters
        {
            { "AllowMultipleSelection", false },
            { "Title", $"为「{herbItem.OriginalHerbName ?? herbItem.HerbName}」选择系统药材" }
        };

        // 显示对话框（30行）
        _dialogService.ShowDialog("HerbSelectionDialog", parameters, async result =>
        {
            try
            {
                if (result.Result == ButtonResult.OK)
                {
                    var selectedHerbs = result.Parameters.GetValue<List<HerbDto>>("SelectedHerbs");
                    if (selectedHerbs != null && selectedHerbs.Any())
                    {
                        var selectedHerb = selectedHerbs.First();

                        // 记录日志（5行）
                        Logger.LogInformation("用户选择了系统药材ID: {HerbId}", selectedHerb.Id);

                        // 验证并映射（15行）
                        var validateResult = await _commandHandler.ValidateFormulaHerbAsync(
                            SelectedFormula!.Id, herbItem.Id, selectedHerb.Id);

                        if (validateResult.success)
                        {
                            await ShowSuccessMessageAsync("药材映射成功");
                            await LoadPendingFormulasAsync();
                        }
                        else
                        {
                            await ShowErrorMessageAsync("药材映射失败");
                        }
                    }
                }
            }
            finally { SetIsBusy(false); }
        });
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "选择药材时发生异常");
        await ShowErrorMessageAsync("系统错误");
    }
    finally { SetIsBusy(false); }
}
```

**重构后**（40行，Low复杂度）：
```csharp
// ✅ 主方法（40行）：清晰的业务流程
private async Task SelectHerbAsync(FormulaHerbItemDto? herbItem)
{
    if (herbItem == null || SelectedFormula == null) return;
    if (herbItem.IsValidated) { await ShowWarningMessageAsync("该药材已校验"); return; }

    try
    {
        SetIsBusy(true, $"正在处理药材「{herbItem.HerbName}」...");

        // ✅ 提取方法1：创建对话框参数
        var parameters = CreateHerbSelectionDialogParameters(herbItem);

        // ✅ 提取方法2：处理对话框结果
        _dialogService.ShowDialog("HerbSelectionDialog", parameters, async result =>
        {
            await HandleHerbSelectionResultAsync(result, herbItem);
        });
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "选择药材时发生异常");
        await ShowErrorMessageAsync("系统错误");
    }
    finally { SetIsBusy(false); }
}

// ✅ 提取方法1（5行）：单一职责
private DialogParameters CreateHerbSelectionDialogParameters(FormulaHerbItemDto herbItem)
{
    return new DialogParameters
    {
        { "AllowMultipleSelection", false },
        { "Title", $"为「{herbItem.OriginalHerbName ?? herbItem.HerbName}」选择系统药材" }
    };
}

// ✅ 提取方法2（15行）：处理回调逻辑
private async Task HandleHerbSelectionResultAsync(IDialogResult result, FormulaHerbItemDto herbItem)
{
    try
    {
        if (result.Result == ButtonResult.OK)
        {
            var selectedHerbs = result.Parameters.GetValue<List<HerbDto>>("SelectedHerbs");
            if (selectedHerbs != null && selectedHerbs.Any())
            {
                // ✅ 提取方法3：处理选中的药材
                await ProcessSelectedHerbAsync(selectedHerbs.First(), herbItem);
            }
        }
    }
    finally { SetIsBusy(false); }
}

// ✅ 提取方法3（10行）：验证和映射逻辑
private async Task ProcessSelectedHerbAsync(HerbDto selectedHerb, FormulaHerbItemDto herbItem)
{
    var validateResult = await _commandHandler.ValidateFormulaHerbAsync(
        SelectedFormula!.Id, herbItem.Id, selectedHerb.Id);

    if (validateResult.success)
    {
        await ShowSuccessMessageAsync("药材映射成功");
        await LoadPendingFormulasAsync();
    }
    else
    {
        await ShowErrorMessageAsync("药材映射失败");
    }
}
```

**重构收益**：
- 主方法从 **77行 → 40行**（减少48%）
- 提取4个辅助方法（5行+15行+10行）
- 每个方法职责单一、易于理解和测试
- 圈复杂度从15降至5

**完整标准** → [../../explanation/architecture/code-quality/method-complexity.md](../../explanation/architecture/code-quality/method-complexity.md)

**工具支持**：
- Visual Studio 2022: 代码指标（Ctrl+K, Ctrl+M）
- Roslyn Analyzers: 实时复杂度警告
- SonarQube: CI/CD集成检查

---

## 🎨 View开发

### 4.1 View基本结构

**标准View模板**：
```xml
<UserControl x:Class="LYBT.Desktop.Patients.Views.PatientListView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="800">

    <!-- ✅ 使用资源字典中的统一样式 -->
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Styles/Colors.xaml"/>
                <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Styles/Controls.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Grid>
        <!-- 视图内容 -->
    </Grid>
</UserControl>
```

### 4.2 布局规范

**Grid布局（推荐用于复杂布局）**：
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>   <!-- 标题栏 -->
        <RowDefinition Height="Auto"/>   <!-- 搜索栏 -->
        <RowDefinition Height="*"/>      <!-- 主内容 -->
        <RowDefinition Height="Auto"/>   <!-- 操作栏 -->
    </Grid.RowDefinitions>

    <!-- 标题栏 -->
    <TextBlock Grid.Row="0"
               Text="患者列表"
               Style="{StaticResource TitleTextBlockStyle}"/>

    <!-- 搜索栏 -->
    <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,10">
        <TextBox Width="300"
                 Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                 md:HintAssist.Hint="搜索患者姓名或手机号"/>
        <Button Command="{Binding SearchCommand}"
                Content="搜索"
                Margin="10,0,0,0"/>
    </StackPanel>

    <!-- 主内容 -->
    <DataGrid Grid.Row="2"
              ItemsSource="{Binding Patients}"
              SelectedItem="{Binding SelectedPatient}"
              Style="{StaticResource StandardDataGridStyle}"/>

    <!-- 操作栏 -->
    <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10">
        <Button Command="{Binding AddCommand}" Content="新增"/>
        <Button Command="{Binding EditCommand}" Content="编辑" Margin="10,0,0,0"/>
        <Button Command="{Binding DeleteCommand}" Content="删除" Margin="10,0,0,0"/>
    </StackPanel>
</Grid>
```

**StackPanel布局（用于简单线性布局）**：
```xml
<StackPanel Orientation="Vertical" Margin="20">
    <TextBlock Text="患者基本信息" Style="{StaticResource SectionHeaderStyle}"/>

    <StackPanel Orientation="Horizontal" Margin="0,10">
        <TextBlock Text="姓名：" Width="80"/>
        <TextBox Text="{Binding PatientName}" Width="200"/>
    </StackPanel>

    <StackPanel Orientation="Horizontal" Margin="0,10">
        <TextBlock Text="性别：" Width="80"/>
        <ComboBox ItemsSource="{Binding Genders}"
                  SelectedItem="{Binding SelectedGender}"
                  Width="200"/>
    </StackPanel>
</StackPanel>
```

### 4.3 DataGrid使用规范

**基本配置**：
```xml
<DataGrid ItemsSource="{Binding Patients}"
          SelectedItem="{Binding SelectedPatient}"
          AutoGenerateColumns="False"
          IsReadOnly="True"
          SelectionMode="Single"
          CanUserAddRows="False"
          CanUserDeleteRows="False"
          Style="{StaticResource StandardDataGridStyle}">

    <DataGrid.Columns>
        <!-- 文本列 -->
        <DataGridTextColumn Header="姓名"
                            Binding="{Binding Name}"
                            Width="150"/>

        <!-- 模板列（自定义内容） -->
        <DataGridTemplateColumn Header="性别" Width="80">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Gender}"
                               Foreground="{Binding Gender, Converter={StaticResource GenderToColorConverter}}"/>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>

        <!-- 操作列 -->
        <DataGridTemplateColumn Header="操作" Width="150">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Button Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                CommandParameter="{Binding}"
                                Content="编辑"
                                Style="{StaticResource LinkButtonStyle}"/>
                        <Button Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                CommandParameter="{Binding}"
                                Content="删除"
                                Style="{StaticResource LinkButtonStyle}"
                                Margin="10,0,0,0"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

### 4.4 ListBox使用规范

**自定义ItemTemplate**：
```xml
<ListBox ItemsSource="{Binding Herbs}"
         SelectedItem="{Binding SelectedHerb}"
         SelectionMode="Multiple"
         Style="{StaticResource StandardListBoxStyle}">

    <ListBox.ItemTemplate>
        <DataTemplate>
            <Border BorderBrush="{StaticResource BorderBrush}"
                    BorderThickness="1"
                    Padding="10"
                    Margin="0,5">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>

                    <StackPanel Grid.Column="0">
                        <TextBlock Text="{Binding Name}"
                                   FontWeight="Bold"
                                   FontSize="16"/>
                        <TextBlock Text="{Binding Category}"
                                   Foreground="{StaticResource SecondaryTextBrush}"
                                   Margin="0,5"/>
                        <TextBlock Text="{Binding Description}"
                                   TextWrapping="Wrap"/>
                    </StackPanel>

                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="{Binding Price, StringFormat='¥{0:F2}'}"
                                   FontSize="18"
                                   Foreground="{StaticResource PrimaryBrush}"/>
                        <TextBlock Text="{Binding Unit}"
                                   HorizontalAlignment="Center"/>
                    </StackPanel>
                </Grid>
            </Border>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

---

## 🔗 数据绑定

### 5.1 基本绑定模式

**OneWay（单向）**：
```xml
<!-- ViewModel → View（只读显示） -->
<TextBlock Text="{Binding PatientName, Mode=OneWay}"/>
```

**TwoWay（双向）**：
```xml
<!-- ViewModel ↔ View（可编辑） -->
<TextBox Text="{Binding PatientName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
```

**OneTime（一次性）**：
```xml
<!-- 初始化后不再更新 -->
<TextBlock Text="{Binding SystemVersion, Mode=OneTime}"/>
```

### 5.2 更新触发时机

**UpdateSourceTrigger选项**：
```xml
<!-- PropertyChanged：实时更新（推荐用于搜索框） -->
<TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"/>

<!-- LostFocus：失去焦点时更新（推荐用于表单） -->
<TextBox Text="{Binding PatientName, UpdateSourceTrigger=LostFocus}"/>

<!-- Explicit：手动调用BindingExpression.UpdateSource() -->
<TextBox x:Name="txtName" Text="{Binding PatientName, UpdateSourceTrigger=Explicit}"/>
```

### 5.3 多重绑定

**MultiBinding示例**：
```xml
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} - {1}">
            <Binding Path="PatientName"/>
            <Binding Path="Age"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>

<!-- 或使用Converter -->
<TextBlock>
    <TextBlock.Text>
        <MultiBinding Converter="{StaticResource FullNameConverter}">
            <Binding Path="FirstName"/>
            <Binding Path="LastName"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

### 5.4 RelativeSource绑定

**绑定到父控件**：
```xml
<!-- 绑定到DataGrid的DataContext -->
<Button Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
        CommandParameter="{Binding}"/>

<!-- 绑定到Window的DataContext -->
<Button Command="{Binding DataContext.CloseCommand, RelativeSource={RelativeSource AncestorType=Window}}"/>
```

**绑定到自身**：
```xml
<!-- 绑定到自身的属性 -->
<TextBox x:Name="txtInput" Text="Hello"/>
<TextBlock Text="{Binding Text, ElementName=txtInput}"/>
```

---

## ⚡ 命令绑定

### 6.1 基本命令绑定

**Button命令**：
```xml
<!-- 无参数命令 -->
<Button Command="{Binding AddCommand}" Content="新增"/>

<!-- 带参数命令 -->
<Button Command="{Binding EditCommand}"
        CommandParameter="{Binding SelectedPatient}"
        Content="编辑"/>
```

**快捷键命令**：
```xml
<UserControl>
    <UserControl.InputBindings>
        <KeyBinding Key="F" Modifiers="Ctrl" Command="{Binding SearchCommand}"/>
        <KeyBinding Key="N" Modifiers="Ctrl" Command="{Binding AddCommand}"/>
        <KeyBinding Key="Delete" Command="{Binding DeleteCommand}"/>
    </UserControl.InputBindings>
</UserControl>
```

### 6.2 DataGrid中的命令

**行双击命令**：
```xml
<DataGrid ItemsSource="{Binding Patients}">
    <DataGrid.InputBindings>
        <MouseBinding MouseAction="LeftDoubleClick"
                      Command="{Binding EditCommand}"
                      CommandParameter="{Binding SelectedPatient}"/>
    </DataGrid.InputBindings>
</DataGrid>
```

**单元格按钮命令**：
```xml
<DataGridTemplateColumn Header="操作">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Button Command="{Binding DataContext.ViewDetailsCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                    CommandParameter="{Binding}"
                    Content="查看"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

---

## 🧭 导航实现

### 7.1 Region导航基础

**配置Region**：
```xml
<!-- MainWindow.xaml -->
<Window>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="60"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 菜单区域 -->
        <ContentControl Grid.Row="0" prism:RegionManager.RegionName="MenuRegion"/>

        <!-- 主内容区域 -->
        <ContentControl Grid.Row="1" prism:RegionManager.RegionName="MainRegion"/>
    </Grid>
</Window>
```

**ViewModel中导航**：
```csharp
public class MenuViewModel : UnifiedViewModelBase
{
    public DelegateCommand NavigateToPatientListCommand { get; }

    public MenuViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, ILogger<MenuViewModel> logger)
        : base(regionManager, eventAggregator, logger)
    {
        NavigateToPatientListCommand = new DelegateCommand(ExecuteNavigateToPatientList);
    }

    private void ExecuteNavigateToPatientList()
    {
        // ✅ 无参数导航
        RegionManager.RequestNavigate("MainRegion", "PatientListView");
    }
}
```

### 7.2 带参数的导航

**传递参数**：
```csharp
public class PatientListViewModel : UnifiedViewModelBase
{
    public DelegateCommand<PatientDisplayModel> ViewDetailsCommand { get; }

    public PatientListViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, ILogger<PatientListViewModel> logger)
        : base(regionManager, eventAggregator, logger)
    {
        ViewDetailsCommand = new DelegateCommand<PatientDisplayModel>(ExecuteViewDetails);
    }

    private void ExecuteViewDetails(PatientDisplayModel patient)
    {
        var parameters = new NavigationParameters
        {
            { "patientId", patient.Id }
        };

        RegionManager.RequestNavigate("MainRegion", "PatientDetailView", parameters);
    }
}
```

**接收参数**：
```csharp
public class PatientDetailViewModel : UnifiedViewModelBase, INavigationAware
{
    private Guid _patientId;

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        _patientId = navigationContext.Parameters.GetValue<Guid>("patientId");
        LoadPatientData(_patientId);
    }

    private async void LoadPatientData(Guid patientId)
    {
        var result = await _repository.GetByIdAsync(patientId);
        if (result.IsSuccess)
        {
            // 更新ViewModel属性
        }
    }
}
```

### 7.3 导航确认

**离开前确认**：
```csharp
public class MedicalCaseEditorViewModel : UnifiedViewModelBase, IConfirmNavigationRequest
{
    private bool _isDirty;

    public void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
    {
        if (_isDirty)
        {
            var result = MessageBox.Show("数据未保存，确定离开？", "确认", MessageBoxButton.YesNo);
            continuationCallback(result == MessageBoxResult.Yes);
        }
        else
        {
            continuationCallback(true);
        }
    }
}
```

---

## 💬 对话框开发

### 8.1 IDialogAware基础

**ViewModel实现**：
```csharp
public class HerbSelectionDialogViewModel : ViewModelBase, IDialogAware
{
    public string Title => "选择药材";

    public event Action<IDialogResult> RequestClose;

    public DelegateCommand ConfirmCommand { get; }
    public DelegateCommand CancelCommand { get; }

    public ObservableCollection<HerbDisplayModel> SelectedHerbs { get; }

    public HerbSelectionDialogViewModel(ILogger<HerbSelectionDialogViewModel> logger)
        : base(logger)
    {
        SelectedHerbs = new ObservableCollection<HerbDisplayModel>();
        ConfirmCommand = new DelegateCommand(ExecuteConfirm, CanExecuteConfirm);
        CancelCommand = new DelegateCommand(ExecuteCancel);
    }

    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed()
    {
        // 清理资源
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 接收传入参数
        var prescriptionId = parameters.GetValue<Guid>("prescriptionId");
        LoadHerbs(prescriptionId);
    }

    private void ExecuteConfirm()
    {
        var result = new DialogResult(ButtonResult.OK);
        result.Parameters.Add("selectedHerbs", SelectedHerbs.ToList());
        RequestClose?.Invoke(result);
    }

    private bool CanExecuteConfirm()
    {
        return SelectedHerbs.Any();
    }

    private void ExecuteCancel()
    {
        RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
    }
}
```

**View实现**：
```xml
<UserControl x:Class="LYBT.Desktop.Herbs.Views.Dialogs.HerbSelectionDialog"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True"
             Width="800" Height="600">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 标题 -->
        <TextBlock Grid.Row="0" Text="{Binding Title}" Style="{StaticResource DialogTitleStyle}"/>

        <!-- 内容 -->
        <ListBox Grid.Row="1" ItemsSource="{Binding Herbs}" SelectedItem="{Binding SelectedHerb}"/>

        <!-- 按钮 -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10">
            <Button Command="{Binding ConfirmCommand}" Content="确定"/>
            <Button Command="{Binding CancelCommand}" Content="取消" Margin="10,0,0,0"/>
        </StackPanel>
    </Grid>
</UserControl>
```

### 8.2 显示对话框

**调用对话框**：
```csharp
public class PrescriptionEditorViewModel : UnifiedViewModelBase
{
    private readonly IDialogService _dialogService;

    public DelegateCommand AddHerbCommand { get; }

    public PrescriptionEditorViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IDialogService dialogService,
        ILogger<PrescriptionEditorViewModel> logger)
        : base(regionManager, eventAggregator, logger)
    {
        _dialogService = dialogService;
        AddHerbCommand = new DelegateCommand(ExecuteAddHerb);
    }

    private void ExecuteAddHerb()
    {
        var parameters = new DialogParameters
        {
            { "prescriptionId", CurrentPrescriptionId }
        };

        _dialogService.ShowDialog("HerbSelectionDialog", parameters, callback =>
        {
            if (callback.Result == ButtonResult.OK)
            {
                var selectedHerbs = callback.Parameters.GetValue<List<HerbDisplayModel>>("selectedHerbs");
                foreach (var herb in selectedHerbs)
                {
                    PrescriptionItems.Add(herb);
                }
            }
        });
    }
}
```

### 8.3 模态与非模态对话框

**模态对话框（阻塞）**：
```csharp
_dialogService.ShowDialog("ConfirmDialog", parameters, callback =>
{
    // 用户关闭对话框后执行
});
```

**非模态对话框（不阻塞）**：
```csharp
_dialogService.Show("NotificationDialog", parameters, callback =>
{
    // 对话框仍可见时也会执行
});
```

---

## 🎨 样式应用

### 9.1 使用统一样式

**引用全局样式**：
```xml
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- ✅ 颜色定义 -->
            <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Styles/Colors.xaml"/>

            <!-- ✅ 控件样式 -->
            <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Styles/Controls.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</UserControl.Resources>
```

**应用预定义样式**：
```xml
<!-- 标题文本 -->
<TextBlock Text="患者列表" Style="{StaticResource TitleTextBlockStyle}"/>

<!-- 按钮样式 -->
<Button Content="新增" Style="{StaticResource PrimaryButtonStyle}"/>
<Button Content="取消" Style="{StaticResource SecondaryButtonStyle}"/>

<!-- DataGrid样式 -->
<DataGrid Style="{StaticResource StandardDataGridStyle}"/>

<!-- 链接按钮 -->
<Button Content="查看详情" Style="{StaticResource LinkButtonStyle}"/>
```

### 9.2 常用颜色资源

**颜色定义（Colors.xaml）**：
```xml
<!-- 主色调 -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#1976D2"/>
<SolidColorBrush x:Key="SecondaryBrush" Color="#424242"/>

<!-- 状态颜色 -->
<SolidColorBrush x:Key="SuccessBrush" Color="#4CAF50"/>
<SolidColorBrush x:Key="WarningBrush" Color="#FF9800"/>
<SolidColorBrush x:Key="ErrorBrush" Color="#F44336"/>

<!-- 文本颜色 -->
<SolidColorBrush x:Key="PrimaryTextBrush" Color="#212121"/>
<SolidColorBrush x:Key="SecondaryTextBrush" Color="#757575"/>

<!-- 背景颜色 -->
<SolidColorBrush x:Key="BackgroundBrush" Color="#FAFAFA"/>
<SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF"/>

<!-- 边框颜色 -->
<SolidColorBrush x:Key="BorderBrush" Color="#E0E0E0"/>
```

**使用颜色**：
```xml
<TextBlock Text="成功" Foreground="{StaticResource SuccessBrush}"/>
<Border Background="{StaticResource SurfaceBrush}" BorderBrush="{StaticResource BorderBrush}"/>
```

---

## 🔄 Converter开发

### 10.1 简单Converter

**BooleanToVisibilityConverter（已内置）**：
```xml
<TextBlock Text="加载中..."
           Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"/>
```

**自定义StatusToColorConverter**：
```csharp
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Active" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    "Pending" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    "Inactive" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
```

**注册Converter**：
```xml
<UserControl.Resources>
    <ResourceDictionary>
        <local:StatusToColorConverter x:Key="StatusToColorConverter"/>
    </ResourceDictionary>
</UserControl.Resources>

<!-- 使用 -->
<TextBlock Text="{Binding Status}"
           Foreground="{Binding Status, Converter={StaticResource StatusToColorConverter}}"/>
```

### 10.2 MultiValueConverter

**FullNameConverter示例**：
```csharp
public class FullNameConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is string firstName && values[1] is string lastName)
        {
            return $"{lastName}{firstName}";
        }
        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

**使用MultiValueConverter**：
```xml
<TextBlock>
    <TextBlock.Text>
        <MultiBinding Converter="{StaticResource FullNameConverter}">
            <Binding Path="FirstName"/>
            <Binding Path="LastName"/>
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>
```

---

## 📦 模块注册

### 11.1 IModule实现

**标准模块注册**：
```csharp
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Patients
{
    [Module(ModuleName = nameof(PatientsModule))]
    public class PatientsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化逻辑（可选）
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // ✅ 注册Repository（Singleton）
            containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();

            // ✅ 注册ViewModel（Transient）
            containerRegistry.Register<PatientListViewModel>();
            containerRegistry.Register<PatientDetailViewModel>();

            // ✅ 注册View用于导航
            containerRegistry.RegisterForNavigation<Views.PatientListView>();
            containerRegistry.RegisterForNavigation<Views.PatientDetailView>();

            // ✅ 注册Dialog
            containerRegistry.RegisterDialog<Views.Dialogs.PatientSelectionDialog, ViewModels.Dialogs.PatientSelectionDialogViewModel>();
        }
    }
}
```

### 11.2 模块依赖

**声明依赖关系**：
```csharp
[Module(ModuleName = nameof(MedicalCaseModule))]
[ModuleDependency("PatientsModule")]  // ✅ 依赖患者模块
[ModuleDependency("PrescriptionsModule")]  // ✅ 依赖处方模块
public class MedicalCaseModule : IModule
{
    // ...
}
```

### 11.3 Shell中加载模块

**App.xaml.cs配置**：
```csharp
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // ✅ 基础模块
    moduleCatalog.AddModule<AuthModule>();
    moduleCatalog.AddModule<UsersModule>();

    // ✅ 业务模块（会自动解析依赖顺序）
    moduleCatalog.AddModule<PatientsModule>();
    moduleCatalog.AddModule<MedicalCaseModule>();
    moduleCatalog.AddModule<PrescriptionsModule>();
    moduleCatalog.AddModule<HerbsModule>();
    moduleCatalog.AddModule<FormulaModule>();
    moduleCatalog.AddModule<ConsultationModule>();
}
```

---

## ✅ 最佳实践

### 12.1 MVVM原则

**严格分离关注点**：
```csharp
// ❌ 错误：ViewModel直接操作View
public class BadViewModel : ViewModelBase
{
    public void UpdateUI()
    {
        var view = Application.Current.MainWindow;
        ((TextBox)view.FindName("txtName")).Text = "Updated";
    }
}

// ✅ 正确：通过数据绑定
public class GoodViewModel : ViewModelBase
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public void UpdateData()
    {
        Name = "Updated";  // View自动更新
    }
}
```

### 12.2 性能优化

**虚拟化大数据集**：
```xml
<!-- ✅ 启用虚拟化 -->
<DataGrid ItemsSource="{Binding Patients}"
          VirtualizingPanel.IsVirtualizing="True"
          VirtualizingPanel.VirtualizationMode="Recycling"/>

<ListBox ItemsSource="{Binding Herbs}"
         VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"/>
```

**延迟加载**：
```csharp
public class PatientListViewModel : ViewModelBase, INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // ✅ 导航到页面时才加载数据
        _ = LoadPatientsAsync();
    }
}
```

### 12.3 内存管理

**释放资源**：
```csharp
public class MedicalCaseFlowViewModel : UnifiedViewModelBase, INavigationAware, IDisposable
{
    private CancellationTokenSource _cts;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // ✅ 取消未完成的异步操作
        _cts?.Cancel();
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
```

**取消订阅**：
```csharp
public class NotificationViewModel : UnifiedViewModelBase, IDisposable
{
    private SubscriptionToken _token;

    public NotificationViewModel(IEventAggregator eventAggregator)
    {
        _token = eventAggregator.GetEvent<PatientCreatedEvent>().Subscribe(OnPatientCreated);
    }

    public void Dispose()
    {
        // ✅ 取消事件订阅
        EventAggregator.GetEvent<PatientCreatedEvent>().Unsubscribe(_token);
    }
}
```

### 12.4 错误处理

**统一异常处理**：
```csharp
public class PatientListViewModel : ViewModelBase
{
    private async Task LoadPatientsAsync()
    {
        try
        {
            IsLoading = true;

            var result = await _repository.GetAllAsync();
            if (result.IsSuccess)
            {
                Patients.Clear();
                foreach (var patient in result.Data)
                {
                    Patients.Add(patient);
                }
            }
            else
            {
                // ✅ 记录错误日志
                Logger.LogError("加载患者失败：{Error}", result.ErrorMessage);

                // ✅ 显示错误消息
                MessageBox.Show($"加载患者失败：{result.ErrorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载患者异常");
            MessageBox.Show($"加载患者异常：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

---

## ❓ 常见问题

### 13.1 数据绑定不更新

**问题**：修改属性值但UI不更新

**原因**：未实现INotifyPropertyChanged

**解决方案**：
```csharp
// ❌ 错误
public string Name { get; set; }

// ✅ 正确
private string _name = string.Empty;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}
```

### 13.2 命令无法执行

**问题**：Button显示为禁用状态

**原因**：CanExecute返回false

**解决方案**：
```csharp
public DelegateCommand SaveCommand { get; }

public PatientEditorViewModel()
{
    // ✅ 使用ObservesProperty自动更新CanExecute
    SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave)
        .ObservesProperty(() => IsDataValid);
}

private bool CanExecuteSave()
{
    return IsDataValid;
}
```

### 13.3 导航参数为null

**问题**：NavigationContext.Parameters获取参数为null

**原因**：参数键名不匹配

**解决方案**：
```csharp
// 发送方
var parameters = new NavigationParameters
{
    { "patientId", patient.Id }  // ✅ 键名：patientId
};
RegionManager.RequestNavigate("MainRegion", "PatientDetailView", parameters);

// 接收方
public void OnNavigatedTo(NavigationContext navigationContext)
{
    var patientId = navigationContext.Parameters.GetValue<Guid>("patientId");  // ✅ 键名一致
}
```

### 13.4 Dialog无法显示

**问题**：调用ShowDialog()无反应

**原因**：Dialog未注册

**解决方案**：
```csharp
// ✅ 在Module中注册Dialog
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterDialog<Views.Dialogs.HerbSelectionDialog, ViewModels.Dialogs.HerbSelectionDialogViewModel>();
}
```

---

## 🧪 测试指南

### 14.1 ViewModel单元测试

**测试框架**：xUnit + NSubstitute

**测试示例**：
```csharp
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;

public class PatientListViewModelTests
{
    private readonly IPatientRepository _repository;
    private readonly ILogger<PatientListViewModel> _logger;
    private readonly PatientListViewModel _viewModel;

    public PatientListViewModelTests()
    {
        _repository = Substitute.For<IPatientRepository>();
        _logger = Substitute.For<ILogger<PatientListViewModel>>();
        _viewModel = new PatientListViewModel(_repository, _logger);
    }

    [Fact]
    public async Task LoadPatientsAsync_Success_ShouldPopulatePatients()
    {
        // Arrange
        var patients = new List<PatientDto>
        {
            new PatientDto { Id = Guid.NewGuid(), Name = "张三" },
            new PatientDto { Id = Guid.NewGuid(), Name = "李四" }
        };
        _repository.GetAllAsync().Returns(ServiceResult<List<PatientDto>>.Success(patients));

        // Act
        await _viewModel.LoadCommand.Execute();

        // Assert
        Assert.Equal(2, _viewModel.Patients.Count);
        Assert.Equal("张三", _viewModel.Patients[0].Name);
    }

    [Fact]
    public void SearchCommand_CanExecute_ShouldReturnTrueWhenKeywordNotEmpty()
    {
        // Arrange
        _viewModel.SearchKeyword = "张三";

        // Act
        var canExecute = _viewModel.SearchCommand.CanExecute();

        // Assert
        Assert.True(canExecute);
    }
}
```

### 14.2 Converter单元测试

**测试示例**：
```csharp
public class StatusToColorConverterTests
{
    private readonly StatusToColorConverter _converter;

    public StatusToColorConverterTests()
    {
        _converter = new StatusToColorConverter();
    }

    [Theory]
    [InlineData("Active", "#4CAF50")]
    [InlineData("Pending", "#FF9800")]
    [InlineData("Inactive", "#F44336")]
    public void Convert_ValidStatus_ShouldReturnCorrectColor(string status, string expectedColor)
    {
        // Arrange
        var expected = new SolidColorBrush((Color)ColorConverter.ConvertFromString(expectedColor));

        // Act
        var result = _converter.Convert(status, typeof(Brush), null, CultureInfo.InvariantCulture) as SolidColorBrush;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Color, result.Color);
    }
}
```

---

## 🔍 调试技巧

### 15.1 数据绑定调试

**启用绑定追踪**：
```xml
<!-- ✅ 在Window或UserControl中添加 -->
<Window xmlns:diagnostics="clr-namespace:System.Diagnostics;assembly=WindowsBase">
    <Window.Resources>
        <ResourceDictionary>
            <!-- 启用绑定追踪 -->
            <diagnostics:PresentationTraceSources.TraceLevel>High</diagnostics:PresentationTraceSources.TraceLevel>
        </ResourceDictionary>
    </Window.Resources>
</Window>
```

**查看输出窗口**：
```
System.Windows.Data Error: 40 : BindingExpression path error: 'PatientName' property not found on 'object' ''PatientListViewModel'
```

### 15.2 命令调试

**检查CanExecute**：
```csharp
public PatientListViewModel()
{
    SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave)
        .ObservesProperty(() => IsDataValid);

    // ✅ 手动触发CanExecute检查
    SaveCommand.RaiseCanExecuteChanged();
}
```

### 15.3 导航调试

**导航失败回调**：
```csharp
RegionManager.RequestNavigate("MainRegion", "PatientDetailView", navigationResult =>
{
    if (!navigationResult.Result.HasValue || !navigationResult.Result.Value)
    {
        // ✅ 导航失败，查看错误
        Logger.LogError("导航失败：{Error}", navigationResult.Error?.Message);
    }
});
```

---

## 📚 参考资料

### 官方文档
- **Prism Library**: https://prismlibrary.com/docs/
- **WPF Documentation**: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- **Material Design in XAML**: http://materialdesigninxaml.net/

### 内部文档
- 架构设计：`docs/explanation/architecture/client/presentation-design.md`
- Server端开发：`docs/how-to-guides/server/` 对应模块
- 共享组件：`docs/how-to-guides/shared/components-usage.md`
- API参考：`docs/api/` 对应模块

### 代码示例
- ViewModelBase: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/ViewModels/ViewModelBase.cs`
- UnifiedViewModelBase: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/ViewModels/UnifiedViewModelBase.cs`
- 模块示例: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs`

---

**最后更新**: 2025-10-30
**维护负责**: Client端开发组
**文档版本**: v1.0.0
