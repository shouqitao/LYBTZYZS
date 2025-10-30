# Client端验方管理开发指南

> **文档版本**: v1.0
> **最后更新**: 2025-01-30
> **适用范围**: LYBT Desktop Client - Formula模块
> **前置阅读**: `docs/explanation/architecture/client/formula-design.md`

---

## 1. 概述

### 1.1 模块定位

Formula模块负责**验方模板管理**，是Client端的核心业务模块之一。主要职责包括：

- **验方CRUD**: 创建、编辑、删除验方模板
- **药材配伍管理**: 管理验方中的药材组成（HerbItems）
- **Excel导入导出**: 支持批量导入验方模板，导出现有验方
- **延迟绑定验证**: 处理导入验方中的药材映射到系统药材库
- **验方克隆**: 快速基于现有验方创建新模板

**架构定位**:
```
Presentation层 (Views/UserControls)
    ↓
ViewModel层 (FormulaManagementViewModel, FormulaDetailViewModel, FormulaValidationViewModel)
    ↓
Service Adapter层 (FormulaRepository - HttpClient)
    ↓
Contract层 (FormulaDto, FormulaCreateDto, FormulaUpdateDto)
    ↓
Domain层 (Server端领域模型)
```

### 1.2 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 框架基础 |
| WPF | .NET 8.0 | UI框架 |
| Prism | 9.0.x | MVVM框架、导航、DI |
| HttpClient | .NET 8.0 | WebAPI调用 |
| ObservableCollection | .NET 8.0 | 数据绑定 |
| System.Text.Json | .NET 8.0 | JSON序列化 |

### 1.3 核心组件

Formula模块采用**组件化架构**，将ViewModel职责拆分到4个核心组件：

1. **FormulaDataManager**: 数据加载、快照管理、ObservableCollection操作
2. **FormulaCommandHandler**: Command执行逻辑（保存、删除、克隆）
3. **FormulaCalculator**: 计算逻辑（总价、药材数量、验证进度）
4. **FormulaValidator**: 客户端验证规则（必填、长度、业务规则）

**优势**:
- ✅ 单一职责：每个组件只负责一类功能
- ✅ 可测试性：组件可以独立测试
- ✅ 复用性：组件可在多个ViewModel中复用
- ✅ 维护性：修改逻辑只需调整对应组件

---

## 2. MVVM架构实践

### 2.1 ViewModel基类

Formula模块的ViewModel继承自 `UnifiedViewModelBase`（位于 `LYBT.Desktop.Common`）：

```csharp
public abstract class UnifiedViewModelBase : BindableBase, INavigationAware
{
    protected bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    // INavigationAware接口
    public virtual void OnNavigatedTo(NavigationContext navigationContext) { }
    public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }

    // 异步安全执行
    protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action)
    {
        try
        {
            IsBusy = true;
            return await action();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

**关键特性**:
- `IsBusy`: 绑定到ProgressBar，显示加载状态
- `ExecuteSafelyAsync`: 统一异常处理，避免未捕获异常
- `INavigationAware`: Prism导航生命周期钩子

### 2.2 属性变更通知

**SetProperty模式**（推荐）:
```csharp
private FormulaDto? _selectedItem;
public FormulaDto? SelectedItem
{
    get => _selectedItem;
    set
    {
        if (SetProperty(ref _selectedItem, value))
        {
            // 属性变更后的联动逻辑
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }
}
```

**计算属性**:
```csharp
public int HerbCount => HerbItems?.Count ?? 0;
public decimal TotalPrice => _calculator.CalculateTotalPrice(HerbItems);

// 当HerbItems变化时，手动触发计算属性通知
private void OnHerbItemsChanged()
{
    RaisePropertyChanged(nameof(HerbCount));
    RaisePropertyChanged(nameof(TotalPrice));
}
```

### 2.3 Command模式

**DelegateCommand**:
```csharp
public DelegateCommand SaveCommand { get; }
public DelegateCommand<FormulaDto> DeleteCommand { get; }

public FormulaManagementViewModel(...)
{
    // 无参Command
    SaveCommand = new DelegateCommand(
        executeMethod: async () => await SaveFormulaAsync(),
        canExecuteMethod: () => !IsBusy && CurrentFormula != null
    );

    // 带参Command
    DeleteCommand = new DelegateCommand<FormulaDto>(
        executeMethod: async (formula) => await DeleteFormulaAsync(formula),
        canExecuteMethod: (formula) => !IsBusy && formula != null
    );
}
```

**CanExecute更新**:
```csharp
private void UpdateCommandStates()
{
    SaveCommand.RaiseCanExecuteChanged();
    DeleteCommand.RaiseCanExecuteChanged();
    AddHerbCommand.RaiseCanExecuteChanged();
}
```

---

## 3. FormulaManagementViewModel实现

### 3.1 职责定义

FormulaManagementViewModel负责**验方列表管理**，继承自 `UnifiedListViewModelBase<FormulaDto>`：

**核心职责**:
- 分页加载验方列表
- 搜索验方（按名称/功效）
- 新增/编辑/删除验方
- Excel导入/导出验方
- 导出Excel导入模板
- 打开验证视图
- 显示统计信息（总数、草稿数、已验证数）

### 3.2 ViewModel结构

```csharp
public class FormulaManagementViewModel : UnifiedListViewModelBase<FormulaDto>
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDialogService _dialogService;
    private readonly IRegionManager _regionManager;

    // 数据属性
    public ObservableCollection<FormulaDto> Items { get; set; } = new();

    private FormulaDto? _selectedItem;
    public FormulaDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    // 统计属性
    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    public int DraftCount => Items?.Count(f => f.ValidationStatus == FormulaValidationStatus.Draft) ?? 0;
    public int ValidatedCount => Items?.Count(f => f.ValidationStatus == FormulaValidationStatus.Validated) ?? 0;

    // Commands
    public DelegateCommand AddCommand { get; }
    public DelegateCommand EditCommand { get; }
    public DelegateCommand<FormulaDto> DeleteCommand { get; }
    public DelegateCommand ImportFormulasCommand { get; }
    public DelegateCommand ExportFormulasCommand { get; }
    public DelegateCommand ExportTemplateCommand { get; }
    public DelegateCommand SearchCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand OpenValidationViewCommand { get; }

    public FormulaManagementViewModel(
        IFormulaRepository formulaRepository,
        IDialogService dialogService,
        IRegionManager regionManager)
    {
        _formulaRepository = formulaRepository;
        _dialogService = dialogService;
        _regionManager = regionManager;

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        AddCommand = new DelegateCommand(NavigateToAddView);
        EditCommand = new DelegateCommand(NavigateToEditView, () => SelectedItem != null);
        DeleteCommand = new DelegateCommand<FormulaDto>(async (formula) => await DeleteFormulaAsync(formula));
        ImportFormulasCommand = new DelegateCommand(async () => await ImportFormulasAsync());
        ExportFormulasCommand = new DelegateCommand(async () => await ExportFormulasAsync());
        ExportTemplateCommand = new DelegateCommand(ExportImportTemplate);
        SearchCommand = new DelegateCommand(async () => await SearchFormulasAsync());
        RefreshCommand = new DelegateCommand(async () => await LoadPageAsync(CurrentPage));
        OpenValidationViewCommand = new DelegateCommand(NavigateToValidationView);
    }

    public override async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await LoadPageAsync(1);
    }
}
```

### 3.3 分页加载

```csharp
protected override async Task LoadPageAsync(int pageNumber)
{
    await ExecuteSafelyAsync(async () =>
    {
        var result = await _formulaRepository.GetPagedAsync(pageNumber, PageSize, SearchText);

        if (result.Succeeded && result.Data != null)
        {
            Items.Clear();
            foreach (var item in result.Data.Items)
            {
                Items.Add(item);
            }

            CurrentPage = result.Data.PageNumber;
            TotalPages = result.Data.TotalPages;
            TotalCount = result.Data.TotalCount;

            // 更新统计属性
            RaisePropertyChanged(nameof(DraftCount));
            RaisePropertyChanged(nameof(ValidatedCount));
        }

        return result.Data;
    });
}
```

### 3.4 搜索功能

```csharp
private async Task SearchFormulasAsync()
{
    if (string.IsNullOrWhiteSpace(SearchText))
    {
        await LoadPageAsync(1);
        return;
    }

    await ExecuteSafelyAsync(async () =>
    {
        var result = await _formulaRepository.SearchAsync(SearchText);

        if (result.Succeeded && result.Data != null)
        {
            Items.Clear();
            foreach (var item in result.Data)
            {
                Items.Add(item);
            }

            TotalCount = result.Data.Count;
            RaisePropertyChanged(nameof(DraftCount));
            RaisePropertyChanged(nameof(ValidatedCount));
        }

        return result.Data;
    });
}
```

### 3.5 删除验方

```csharp
private async Task DeleteFormulaAsync(FormulaDto? formula)
{
    if (formula == null)
        return;

    var result = MessageBox.Show(
        $"确定要删除验方'{formula.Name}'吗？",
        "确认删除",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result != MessageBoxResult.Yes)
        return;

    await ExecuteSafelyAsync(async () =>
    {
        var deleteResult = await _formulaRepository.DeleteAsync(formula.Id);

        if (deleteResult.Succeeded)
        {
            Items.Remove(formula);
            TotalCount--;

            RaisePropertyChanged(nameof(DraftCount));
            RaisePropertyChanged(nameof(ValidatedCount));

            MessageBox.Show("删除成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"删除失败: {deleteResult.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return deleteResult.Succeeded;
    });
}
```

### 3.6 导航到详情视图

```csharp
private void NavigateToAddView()
{
    _regionManager.RequestNavigate("ContentRegion", "FormulaDetailView");
}

private void NavigateToEditView()
{
    if (SelectedItem == null)
        return;

    var parameters = new NavigationParameters
    {
        { "FormulaId", SelectedItem.Id.ToString() }
    };

    _regionManager.RequestNavigate("ContentRegion", "FormulaDetailView", parameters);
}

private void NavigateToValidationView()
{
    _regionManager.RequestNavigate("ContentRegion", "FormulaValidationView");
}
```

---

## 4. FormulaDetailViewModel实现

### 4.1 职责定义

FormulaDetailViewModel负责**验方详情编辑**，采用**组件化架构**：

**核心职责**:
- 加载验方详情（编辑模式）
- 初始化新验方（新增模式）
- 管理药材列表（HerbItems）
- 保存验方（新增/更新）
- 取消编辑并回滚
- 克隆验方
- 计算总价和药材数量

### 4.2 组件化架构

```csharp
public class FormulaDetailViewModel : UnifiedViewModelBase
{
    // 核心组件（构造函数注入）
    private readonly FormulaDataManager _dataManager;
    private readonly FormulaCommandHandler _commandHandler;
    private readonly FormulaCalculator _calculator;
    private readonly FormulaValidator _validator;

    // Repository和Prism服务
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDialogService _dialogService;
    private readonly IRegionManager _regionManager;

    // 数据属性
    private FormulaDto _currentFormula = new();
    public FormulaDto CurrentFormula
    {
        get => _currentFormula;
        set
        {
            if (SetProperty(ref _currentFormula, value))
            {
                RaisePropertyChanged(nameof(HerbCount));
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }
    }

    public ObservableCollection<FormulaHerbItemDto> HerbItems { get; set; } = new();

    // 编辑模式标志
    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    private Guid _formulaId;
    public Guid FormulaId
    {
        get => _formulaId;
        set => SetProperty(ref _formulaId, value);
    }

    // 计算属性（使用Calculator组件）
    public int HerbCount => HerbItems?.Count ?? 0;
    public decimal TotalPrice => _calculator.CalculateTotalPrice(HerbItems);

    // Commands
    public DelegateCommand SaveCommand { get; }
    public DelegateCommand CancelCommand { get; }
    public DelegateCommand AddHerbCommand { get; }
    public DelegateCommand<FormulaHerbItemDto> RemoveHerbCommand { get; }
    public DelegateCommand CloneCommand { get; }

    public FormulaDetailViewModel(
        FormulaDataManager dataManager,
        FormulaCommandHandler commandHandler,
        FormulaCalculator calculator,
        FormulaValidator validator,
        IFormulaRepository formulaRepository,
        IDialogService dialogService,
        IRegionManager regionManager)
    {
        _dataManager = dataManager;
        _commandHandler = commandHandler;
        _calculator = calculator;
        _validator = validator;
        _formulaRepository = formulaRepository;
        _dialogService = dialogService;
        _regionManager = regionManager;

        InitializeCommands();

        // 监听HerbItems变化
        HerbItems.CollectionChanged += (s, e) =>
        {
            RaisePropertyChanged(nameof(HerbCount));
            RaisePropertyChanged(nameof(TotalPrice));
        };
    }

    private void InitializeCommands()
    {
        SaveCommand = new DelegateCommand(async () => await SaveFormulaAsync(), () => !IsBusy);
        CancelCommand = new DelegateCommand(CancelEdit);
        AddHerbCommand = new DelegateCommand(OpenAddHerbDialog);
        RemoveHerbCommand = new DelegateCommand<FormulaHerbItemDto>(RemoveHerb);
        CloneCommand = new DelegateCommand(async () => await CloneFormulaAsync(), () => IsEditMode);
    }
}
```

### 4.3 导航参数处理

```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    var formulaIdStr = navigationContext.Parameters.GetValue<string>("FormulaId");

    if (Guid.TryParse(formulaIdStr, out var formulaId))
    {
        // 编辑模式
        FormulaId = formulaId;
        IsEditMode = true;
        await LoadFormulaAsync(formulaId);

        // 创建快照（用于取消时回滚）
        _dataManager.CreateSnapshot(CurrentFormula);
    }
    else
    {
        // 新增模式
        IsEditMode = false;
        InitializeNewFormula();
    }
}

private void InitializeNewFormula()
{
    CurrentFormula = new FormulaDto
    {
        Id = Guid.NewGuid(),
        Name = string.Empty,
        Effect = string.Empty,
        Usage = string.Empty,
        Property = string.Empty,
        Remark = string.Empty,
        Category = "自定义",
        IsShared = false,
        ValidationStatus = FormulaValidationStatus.Draft,
        Herbs = new List<FormulaHerbItemDto>()
    };

    HerbItems.Clear();
}
```

### 4.4 加载验方详情

```csharp
private async Task LoadFormulaAsync(Guid formulaId)
{
    await ExecuteSafelyAsync(async () =>
    {
        var result = await _dataManager.LoadFormulaAsync(formulaId);

        if (result.success && result.formula != null)
        {
            CurrentFormula = result.formula;
            _dataManager.LoadHerbItems(HerbItems, result.formula.Herbs);
        }
        else
        {
            MessageBox.Show(
                result.errorMessage ?? "加载验方失败",
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            NavigateBack();
        }

        return result.formula;
    });
}
```

### 4.5 保存验方

```csharp
private async Task SaveFormulaAsync()
{
    // 1. 客户端验证
    var formulaValidation = _validator.ValidateFormula(CurrentFormula);
    if (!formulaValidation.IsValid)
    {
        MessageBox.Show(
            string.Join("\n", formulaValidation.ErrorMessages),
            "验证失败",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }

    var herbValidation = _validator.ValidateHerbItems(HerbItems);
    if (!herbValidation.IsValid)
    {
        MessageBox.Show(
            string.Join("\n", herbValidation.ErrorMessages),
            "验证失败",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
    }

    // 2. 调用CommandHandler保存
    await ExecuteSafelyAsync(async () =>
    {
        bool success;
        string message;

        if (IsEditMode)
        {
            // 更新现有验方
            var updateDto = new FormulaUpdateDto
            {
                Name = CurrentFormula.Name,
                Effect = CurrentFormula.Effect,
                Usage = CurrentFormula.Usage,
                Property = CurrentFormula.Property,
                Remark = CurrentFormula.Remark,
                Category = CurrentFormula.Category,
                IsShared = CurrentFormula.IsShared,
                Herbs = HerbItems.Select(h => new FormulaHerbItemUpdateDto
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Quantity = h.Quantity,
                    Unit = h.Unit
                }).ToList()
            };

            (success, message) = await _commandHandler.UpdateFormulaAsync(FormulaId, updateDto);
        }
        else
        {
            // 创建新验方
            var createDto = new FormulaCreateDto
            {
                Name = CurrentFormula.Name,
                Effect = CurrentFormula.Effect,
                Usage = CurrentFormula.Usage,
                Property = CurrentFormula.Property,
                Remark = CurrentFormula.Remark,
                Category = CurrentFormula.Category,
                IsShared = CurrentFormula.IsShared,
                Herbs = HerbItems.Select(h => new FormulaHerbItemCreateDto
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Quantity = h.Quantity,
                    Unit = h.Unit
                }).ToList()
            };

            (success, message) = await _commandHandler.CreateFormulaAsync(createDto);
        }

        if (success)
        {
            MessageBox.Show("保存成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            NavigateBack();
        }
        else
        {
            MessageBox.Show($"保存失败: {message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return success;
    });
}
```

### 4.6 取消编辑（回滚）

```csharp
private void CancelEdit()
{
    var result = MessageBox.Show(
        "确定要取消编辑吗？未保存的修改将丢失。",
        "确认取消",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result != MessageBoxResult.Yes)
        return;

    if (IsEditMode)
    {
        // 从快照恢复数据
        _dataManager.RestoreFromSnapshot(CurrentFormula, HerbItems);
    }

    NavigateBack();
}
```

### 4.7 添加药材

```csharp
private void OpenAddHerbDialog()
{
    var parameters = new DialogParameters
    {
        { "SelectionMode", "Single" },
        { "Title", "选择药材" }
    };

    _dialogService.ShowDialog("HerbSelectionDialog", parameters, result =>
    {
        if (result.Result != ButtonResult.OK)
            return;

        var selectedHerbs = result.Parameters.GetValue<List<HerbDto>>("SelectedHerbs");
        if (selectedHerbs == null || !selectedHerbs.Any())
            return;

        var selectedHerb = selectedHerbs.First();

        var herbItem = new FormulaHerbItemDto
        {
            Id = Guid.NewGuid(),
            HerbId = selectedHerb.Id,
            HerbName = selectedHerb.Name,
            Quantity = 1,
            Unit = "克",
            IsValidated = true,
            Herb = selectedHerb
        };

        HerbItems.Add(herbItem);
    });
}

private void RemoveHerb(FormulaHerbItemDto? herbItem)
{
    if (herbItem == null)
        return;

    var result = MessageBox.Show(
        $"确定要移除药材'{herbItem.HerbName}'吗？",
        "确认移除",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result == MessageBoxResult.Yes)
    {
        HerbItems.Remove(herbItem);
    }
}
```

### 4.8 克隆验方

```csharp
private async Task CloneFormulaAsync()
{
    var result = MessageBox.Show(
        $"确定要克隆验方'{CurrentFormula.Name}'吗？",
        "确认克隆",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result != MessageBoxResult.Yes)
        return;

    await ExecuteSafelyAsync(async () =>
    {
        var cloneResult = await _formulaRepository.CloneFormulaAsync(FormulaId);

        if (cloneResult.Succeeded && cloneResult.Data != null)
        {
            MessageBox.Show("克隆成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            // 导航到克隆后的验方编辑页
            var parameters = new NavigationParameters
            {
                { "FormulaId", cloneResult.Data.Id.ToString() }
            };
            _regionManager.RequestNavigate("ContentRegion", "FormulaDetailView", parameters);
        }
        else
        {
            MessageBox.Show($"克隆失败: {cloneResult.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return cloneResult.Data;
    });
}
```

---

## 5. FormulaValidationViewModel实现

### 5.1 职责定义

FormulaValidationViewModel负责**延迟绑定验证工作流**：

**核心职责**:
- 加载待验证验方列表（Draft状态）
- 显示选中验方的未验证药材列表
- 打开药材选择对话框，映射药材到系统药材库
- 调用WebAPI验证药材绑定
- 显示验证进度
- 完成验证（所有药材验证后，状态变为Validated）

**延迟绑定场景**:
```
用户导入Excel → 验方中包含"人参"、"黄芪"等药材名称
                ↓
Server端创建验方（HerbId=null, OriginalHerbName="人参"）
                ↓
Client端FormulaValidationView → 用户手动映射"人参"到系统药材库
                ↓
调用ValidateFormulaHerbAsync → HerbId赋值，IsValidated=true
                ↓
所有药材验证完成 → ValidationStatus变为Validated
```

### 5.2 ViewModel结构

```csharp
public class FormulaValidationViewModel : UnifiedViewModelBase
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDialogService _dialogService;

    // 待验证验方列表
    public ObservableCollection<FormulaDto> PendingFormulas { get; set; } = new();

    private FormulaDto? _selectedFormula;
    public FormulaDto? SelectedFormula
    {
        get => _selectedFormula;
        set
        {
            if (SetProperty(ref _selectedFormula, value))
            {
                RaisePropertyChanged(nameof(UnvalidatedHerbs));
                RaisePropertyChanged(nameof(ValidationProgress));
                RaisePropertyChanged(nameof(CanCompleteValidation));
                CompleteValidationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // 未验证药材列表（从SelectedFormula.Herbs筛选）
    public List<FormulaHerbItemDto> UnvalidatedHerbs
    {
        get
        {
            return SelectedFormula?.Herbs
                .Where(h => !h.IsValidated)
                .ToList() ?? new List<FormulaHerbItemDto>();
        }
    }

    // 验证进度显示
    public string ValidationProgress
    {
        get
        {
            if (SelectedFormula?.Herbs == null || !SelectedFormula.Herbs.Any())
                return "0/0";

            var total = SelectedFormula.Herbs.Count;
            var validated = SelectedFormula.Herbs.Count(h => h.IsValidated);
            return $"{validated}/{total}";
        }
    }

    // 是否可以完成验证
    public bool CanCompleteValidation
    {
        get
        {
            return SelectedFormula?.Herbs != null &&
                   SelectedFormula.Herbs.Any() &&
                   SelectedFormula.Herbs.All(h => h.IsValidated);
        }
    }

    // Commands
    public DelegateCommand<FormulaHerbItemDto> SelectHerbCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand CompleteValidationCommand { get; }

    public FormulaValidationViewModel(
        IFormulaRepository formulaRepository,
        IDialogService dialogService)
    {
        _formulaRepository = formulaRepository;
        _dialogService = dialogService;

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        SelectHerbCommand = new DelegateCommand<FormulaHerbItemDto>(
            async (herbItem) => await SelectHerbAsync(herbItem)
        );

        RefreshCommand = new DelegateCommand(
            async () => await LoadPendingFormulasAsync()
        );

        CompleteValidationCommand = new DelegateCommand(
            async () => await CompleteValidationAsync(),
            () => CanCompleteValidation
        );
    }

    public override async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await LoadPendingFormulasAsync();
    }
}
```

### 5.3 加载待验证验方

```csharp
private async Task LoadPendingFormulasAsync()
{
    await ExecuteSafelyAsync(async () =>
    {
        var result = await _formulaRepository.GetPendingValidationFormulasAsync();

        if (result.Succeeded && result.Data != null)
        {
            PendingFormulas.Clear();
            foreach (var formula in result.Data)
            {
                PendingFormulas.Add(formula);
            }

            // 自动选择第一个验方
            if (PendingFormulas.Any())
            {
                SelectedFormula = PendingFormulas.First();
            }
        }

        return result.Data;
    });
}
```

### 5.4 选择药材并验证

```csharp
private async Task SelectHerbAsync(FormulaHerbItemDto? herbItem)
{
    if (herbItem == null || SelectedFormula == null)
        return;

    // 打开药材选择对话框
    var parameters = new DialogParameters
    {
        { "SelectionMode", "Single" },
        { "Title", $"为药材'{herbItem.OriginalHerbName}'选择系统药材" }
    };

    _dialogService.ShowDialog("HerbSelectionDialog", parameters, async result =>
    {
        if (result.Result != ButtonResult.OK)
            return;

        var selectedHerbs = result.Parameters.GetValue<List<HerbDto>>("SelectedHerbs");
        if (selectedHerbs == null || !selectedHerbs.Any())
            return;

        var selectedHerb = selectedHerbs.First();

        // 调用WebAPI验证药材绑定
        await ExecuteSafelyAsync(async () =>
        {
            var validateResult = await _formulaRepository.ValidateFormulaHerbAsync(
                SelectedFormula.Id,
                herbItem.Id,
                selectedHerb.Id);

            if (validateResult.Succeeded)
            {
                // 更新本地状态
                herbItem.HerbId = selectedHerb.Id;
                herbItem.HerbName = selectedHerb.Name;
                herbItem.IsValidated = true;

                // 刷新UI
                RaisePropertyChanged(nameof(UnvalidatedHerbs));
                RaisePropertyChanged(nameof(ValidationProgress));
                RaisePropertyChanged(nameof(CanCompleteValidation));
                CompleteValidationCommand.RaiseCanExecuteChanged();

                MessageBox.Show(
                    $"药材'{herbItem.OriginalHerbName}'已成功映射到系统药材'{selectedHerb.Name}'",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"验证失败: {validateResult.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return validateResult.Succeeded;
        });
    });
}
```

### 5.5 完成验证

```csharp
private async Task CompleteValidationAsync()
{
    if (SelectedFormula == null || !CanCompleteValidation)
        return;

    var result = MessageBox.Show(
        $"确定要完成验方'{SelectedFormula.Name}'的验证吗？验证后状态将变为'已验证'。",
        "确认完成",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result != MessageBoxResult.Yes)
        return;

    await ExecuteSafelyAsync(async () =>
    {
        // Server端会自动将ValidationStatus更新为Validated
        // 这里只需刷新列表
        await LoadPendingFormulasAsync();

        MessageBox.Show(
            "验证完成！验方已移至'已验证'状态。",
            "提示",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        return true;
    });
}
```

---

## 6. 组件化架构实现

### 6.1 FormulaDataManager

**职责**: 数据加载、快照管理、ObservableCollection操作

```csharp
public class FormulaDataManager
{
    private readonly IFormulaRepository _formulaRepository;
    private FormulaDataSnapshot? _snapshot;

    public FormulaDataManager(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    /// <summary>
    /// 加载验方数据
    /// </summary>
    public async Task<(bool success, FormulaDto? formula, string? errorMessage)> LoadFormulaAsync(Guid formulaId)
    {
        try
        {
            var result = await _formulaRepository.GetByIdAsync(formulaId);
            if (result.Succeeded && result.Data != null)
            {
                return (true, result.Data, null);
            }
            return (false, null, result.Message);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// 批量加载药材到ObservableCollection（避免多次UI更新）
    /// </summary>
    public void LoadHerbItems(
        ObservableCollection<FormulaHerbItemDto> targetCollection,
        IEnumerable<FormulaHerbItemDto>? sourceItems)
    {
        targetCollection.Clear();
        if (sourceItems != null)
        {
            foreach (var item in sourceItems)
            {
                targetCollection.Add(item);
            }
        }
    }

    /// <summary>
    /// 创建快照（用于取消时回滚）
    /// </summary>
    public FormulaDataSnapshot CreateSnapshot(FormulaDto formula)
    {
        _snapshot = new FormulaDataSnapshot
        {
            Name = formula.Name,
            Effect = formula.Effect,
            Usage = formula.Usage,
            Property = formula.Property,
            Remark = formula.Remark,
            Category = formula.Category,
            IsShared = formula.IsShared,
            Herbs = formula.Herbs.Select(h => new FormulaHerbItemDto
            {
                Id = h.Id,
                HerbId = h.HerbId,
                HerbName = h.HerbName,
                Quantity = h.Quantity,
                Unit = h.Unit
            }).ToList()
        };
        return _snapshot;
    }

    /// <summary>
    /// 从快照恢复数据
    /// </summary>
    public void RestoreFromSnapshot(
        FormulaDto formula,
        ObservableCollection<FormulaHerbItemDto> herbItems)
    {
        if (_snapshot == null)
            return;

        formula.Name = _snapshot.Name;
        formula.Effect = _snapshot.Effect;
        formula.Usage = _snapshot.Usage;
        formula.Property = _snapshot.Property;
        formula.Remark = _snapshot.Remark;
        formula.Category = _snapshot.Category;
        formula.IsShared = _snapshot.IsShared;

        LoadHerbItems(herbItems, _snapshot.Herbs);
    }

    /// <summary>
    /// 获取药材数量
    /// </summary>
    public int GetHerbItemCount(ObservableCollection<FormulaHerbItemDto> herbItems)
    {
        return herbItems?.Count ?? 0;
    }
}

/// <summary>
/// 验方数据快照（用于取消时回滚）
/// </summary>
public class FormulaDataSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string? Effect { get; set; }
    public string? Usage { get; set; }
    public string? Property { get; set; }
    public string? Remark { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsShared { get; set; }
    public List<FormulaHerbItemDto> Herbs { get; set; } = new();
}
```

### 6.2 FormulaCommandHandler

**职责**: Command执行逻辑（保存、删除、克隆）

```csharp
public class FormulaCommandHandler
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    /// <summary>
    /// 创建验方
    /// </summary>
    public async Task<(bool success, string message)> CreateFormulaAsync(FormulaCreateDto createDto)
    {
        try
        {
            var result = await _formulaRepository.CreateAsync(createDto);
            if (result.Succeeded)
            {
                return (true, "创建成功");
            }
            return (false, result.Message ?? "创建失败");
        }
        catch (Exception ex)
        {
            return (false, $"创建失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新验方
    /// </summary>
    public async Task<(bool success, string message)> UpdateFormulaAsync(Guid formulaId, FormulaUpdateDto updateDto)
    {
        try
        {
            var result = await _formulaRepository.UpdateAsync(formulaId, updateDto);
            if (result.Succeeded)
            {
                return (true, "更新成功");
            }
            return (false, result.Message ?? "更新失败");
        }
        catch (Exception ex)
        {
            return (false, $"更新失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 删除验方
    /// </summary>
    public async Task<(bool success, string message)> DeleteFormulaAsync(Guid formulaId)
    {
        try
        {
            var result = await _formulaRepository.DeleteAsync(formulaId);
            if (result.Succeeded)
            {
                return (true, "删除成功");
            }
            return (false, result.Message ?? "删除失败");
        }
        catch (Exception ex)
        {
            return (false, $"删除失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 克隆验方
    /// </summary>
    public async Task<(bool success, FormulaDto? clonedFormula, string message)> CloneFormulaAsync(Guid sourceId)
    {
        try
        {
            var result = await _formulaRepository.CloneFormulaAsync(sourceId);
            if (result.Succeeded && result.Data != null)
            {
                return (true, result.Data, "克隆成功");
            }
            return (false, null, result.Message ?? "克隆失败");
        }
        catch (Exception ex)
        {
            return (false, null, $"克隆失败: {ex.Message}");
        }
    }
}
```

### 6.3 FormulaCalculator

**职责**: 计算逻辑（总价、药材数量、验证进度）

```csharp
public class FormulaCalculator
{
    /// <summary>
    /// 计算验方总价
    /// </summary>
    public decimal CalculateTotalPrice(IEnumerable<FormulaHerbItemDto>? herbItems)
    {
        if (herbItems == null || !herbItems.Any())
            return 0m;

        return herbItems
            .Where(h => h.Herb != null)
            .Sum(h => (h.Herb!.Price ?? 0m) * h.Quantity);
    }

    /// <summary>
    /// 计算未验证药材数量
    /// </summary>
    public int CountUnvalidatedHerbs(IEnumerable<FormulaHerbItemDto>? herbItems)
    {
        if (herbItems == null || !herbItems.Any())
            return 0;

        return herbItems.Count(h => !h.IsValidated);
    }

    /// <summary>
    /// 计算验证进度（百分比）
    /// </summary>
    public double CalculateValidationProgress(IEnumerable<FormulaHerbItemDto>? herbItems)
    {
        if (herbItems == null || !herbItems.Any())
            return 0;

        var total = herbItems.Count();
        var validated = herbItems.Count(h => h.IsValidated);
        return (double)validated / total * 100;
    }

    /// <summary>
    /// 计算验证进度（文本）
    /// </summary>
    public string GetValidationProgressText(IEnumerable<FormulaHerbItemDto>? herbItems)
    {
        if (herbItems == null || !herbItems.Any())
            return "0/0";

        var total = herbItems.Count();
        var validated = herbItems.Count(h => h.IsValidated);
        return $"{validated}/{total}";
    }
}
```

### 6.4 FormulaValidator

**职责**: 客户端验证规则

```csharp
public class FormulaValidator
{
    /// <summary>
    /// 验证验方基本信息
    /// </summary>
    public ValidationResult ValidateFormula(FormulaDto formula)
    {
        var errors = new List<string>();

        // 必填验证
        if (string.IsNullOrWhiteSpace(formula.Name))
            errors.Add("验方名称不能为空");

        // 长度验证
        if (formula.Name?.Length > 100)
            errors.Add("验方名称不能超过100个字符");

        if (formula.Effect?.Length > 500)
            errors.Add("功效说明不能超过500个字符");

        if (formula.Usage?.Length > 500)
            errors.Add("用法说明不能超过500个字符");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            ErrorMessages = errors
        };
    }

    /// <summary>
    /// 验证药材列表
    /// </summary>
    public ValidationResult ValidateHerbItems(IEnumerable<FormulaHerbItemDto> herbItems)
    {
        var errors = new List<string>();

        if (!herbItems.Any())
            errors.Add("至少需要添加一味药材");

        foreach (var item in herbItems)
        {
            if (string.IsNullOrWhiteSpace(item.HerbName))
                errors.Add($"药材名称不能为空");

            if (item.Quantity <= 0)
                errors.Add($"药材'{item.HerbName}'的剂量必须大于0");

            if (string.IsNullOrWhiteSpace(item.Unit))
                errors.Add($"药材'{item.HerbName}'的单位不能为空");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            ErrorMessages = errors
        };
    }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}
```

---

## 7. XAML视图设计

### 7.1 FormulaManagementView.xaml

```xml
<UserControl x:Class="LYBT.Desktop.Formula.Views.FormulaManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>    <!-- ToolBar -->
            <RowDefinition Height="Auto"/>    <!-- SearchBar -->
            <RowDefinition Height="*"/>       <!-- DataGrid -->
            <RowDefinition Height="Auto"/>    <!-- Pagination -->
            <RowDefinition Height="Auto"/>    <!-- StatusBar -->
        </Grid.RowDefinitions>

        <!-- ToolBar -->
        <ToolBar Grid.Row="0">
            <Button Content="新增验方" Command="{Binding AddCommand}"/>
            <Button Content="编辑验方" Command="{Binding EditCommand}"/>
            <Button Content="删除验方" Command="{Binding DeleteCommand}" CommandParameter="{Binding SelectedItem}"/>
            <Separator/>
            <Button Content="导入验方" Command="{Binding ImportFormulasCommand}"/>
            <Button Content="导出验方" Command="{Binding ExportFormulasCommand}"/>
            <Button Content="导出模板" Command="{Binding ExportTemplateCommand}"/>
            <Separator/>
            <Button Content="验证管理" Command="{Binding OpenValidationViewCommand}"/>
        </ToolBar>

        <!-- SearchBar -->
        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="10">
            <TextBox Width="300" Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                     Watermark="输入验方名称或功效搜索"/>
            <Button Content="搜索" Command="{Binding SearchCommand}" Margin="10,0,0,0"/>
            <Button Content="刷新" Command="{Binding RefreshCommand}" Margin="10,0,0,0"/>
        </StackPanel>

        <!-- DataGrid -->
        <DataGrid Grid.Row="2"
                  ItemsSource="{Binding Items}"
                  SelectedItem="{Binding SelectedItem}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTextColumn Header="验方名称" Binding="{Binding Name}" Width="150"/>
                <DataGridTextColumn Header="功效" Binding="{Binding Effect}" Width="200"/>
                <DataGridTextColumn Header="用法" Binding="{Binding Usage}" Width="150"/>
                <DataGridTextColumn Header="药材" Binding="{Binding HerbNames}" Width="200"/>
                <DataGridTextColumn Header="药材数" Binding="{Binding HerbCount}" Width="80"/>
                <DataGridTextColumn Header="总价" Binding="{Binding TotalPrice, StringFormat={}{0:F2}元}" Width="100"/>
                <DataGridTemplateColumn Header="验证状态" Width="100">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding ValidationStatus, Converter={StaticResource ValidationStatusConverter}}"
                                       Foreground="{Binding ValidationStatus, Converter={StaticResource ValidationStatusColorConverter}}"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTextColumn Header="分类" Binding="{Binding Category}" Width="100"/>
                <DataGridCheckBoxColumn Header="共享" Binding="{Binding IsShared}" Width="60"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Pagination -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Center" Margin="10">
            <Button Content="首页" Command="{Binding FirstPageCommand}"/>
            <Button Content="上一页" Command="{Binding PreviousPageCommand}" Margin="5,0"/>
            <TextBlock Text="{Binding CurrentPage}" VerticalAlignment="Center" Margin="5,0"/>
            <TextBlock Text="/" VerticalAlignment="Center"/>
            <TextBlock Text="{Binding TotalPages}" VerticalAlignment="Center" Margin="5,0"/>
            <Button Content="下一页" Command="{Binding NextPageCommand}" Margin="5,0"/>
            <Button Content="末页" Command="{Binding LastPageCommand}"/>
        </StackPanel>

        <!-- StatusBar -->
        <StatusBar Grid.Row="4">
            <StatusBarItem>
                <TextBlock Text="{Binding TotalCount, StringFormat={}共 {0} 条记录}"/>
            </StatusBarItem>
            <StatusBarItem>
                <TextBlock Text="{Binding DraftCount, StringFormat={}草稿: {0}}" Foreground="Orange"/>
            </StatusBarItem>
            <StatusBarItem>
                <TextBlock Text="{Binding ValidatedCount, StringFormat={}已验证: {0}}" Foreground="Green"/>
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <ProgressBar Width="100" Height="20" IsIndeterminate="True"
                             Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</UserControl>
```

### 7.2 FormulaDetailView.xaml

```xml
<UserControl x:Class="LYBT.Desktop.Formula.Views.FormulaDetailView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>    <!-- ToolBar -->
            <RowDefinition Height="*"/>       <!-- Content -->
            <RowDefinition Height="Auto"/>    <!-- StatusBar -->
        </Grid.RowDefinitions>

        <!-- ToolBar -->
        <ToolBar Grid.Row="0">
            <Button Content="保存" Command="{Binding SaveCommand}"/>
            <Button Content="取消" Command="{Binding CancelCommand}"/>
            <Separator/>
            <Button Content="克隆验方" Command="{Binding CloneCommand}"
                    Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibilityConverter}}"/>
        </ToolBar>

        <!-- Content -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <StackPanel Margin="20">
                <!-- 基本信息 -->
                <GroupBox Header="基本信息" Margin="0,0,0,20">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>

                        <TextBlock Grid.Row="0" Grid.Column="0" Text="验方名称:" Margin="0,5,10,5" VerticalAlignment="Center"/>
                        <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding CurrentFormula.Name, UpdateSourceTrigger=PropertyChanged}" Margin="0,5"/>

                        <TextBlock Grid.Row="1" Grid.Column="0" Text="分类:" Margin="0,5,10,5" VerticalAlignment="Center"/>
                        <ComboBox Grid.Row="1" Grid.Column="1" SelectedValue="{Binding CurrentFormula.Category}" Margin="0,5">
                            <ComboBoxItem Content="经方"/>
                            <ComboBoxItem Content="时方"/>
                            <ComboBoxItem Content="自定义"/>
                        </ComboBox>

                        <TextBlock Grid.Row="2" Grid.Column="0" Text="功效:" Margin="0,5,10,5" VerticalAlignment="Top"/>
                        <TextBox Grid.Row="2" Grid.Column="1" Text="{Binding CurrentFormula.Effect, UpdateSourceTrigger=PropertyChanged}"
                                 TextWrapping="Wrap" AcceptsReturn="True" Height="60" Margin="0,5"/>

                        <TextBlock Grid.Row="3" Grid.Column="0" Text="用法:" Margin="0,5,10,5" VerticalAlignment="Top"/>
                        <TextBox Grid.Row="3" Grid.Column="1" Text="{Binding CurrentFormula.Usage, UpdateSourceTrigger=PropertyChanged}"
                                 TextWrapping="Wrap" AcceptsReturn="True" Height="60" Margin="0,5"/>

                        <TextBlock Grid.Row="4" Grid.Column="0" Text="性质:" Margin="0,5,10,5" VerticalAlignment="Top"/>
                        <TextBox Grid.Row="4" Grid.Column="1" Text="{Binding CurrentFormula.Property, UpdateSourceTrigger=PropertyChanged}"
                                 TextWrapping="Wrap" AcceptsReturn="True" Height="60" Margin="0,5"/>

                        <TextBlock Grid.Row="5" Grid.Column="0" Text="备注:" Margin="0,5,10,5" VerticalAlignment="Top"/>
                        <TextBox Grid.Row="5" Grid.Column="1" Text="{Binding CurrentFormula.Remark, UpdateSourceTrigger=PropertyChanged}"
                                 TextWrapping="Wrap" AcceptsReturn="True" Height="60" Margin="0,5"/>

                        <CheckBox Grid.Row="6" Grid.Column="1" Content="共享给其他医生" IsChecked="{Binding CurrentFormula.IsShared}" Margin="0,10,0,5"/>
                    </Grid>
                </GroupBox>

                <!-- 药材组成 -->
                <GroupBox Header="药材组成" Margin="0,0,0,20">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="*"/>
                        </Grid.RowDefinitions>

                        <ToolBar Grid.Row="0">
                            <Button Content="添加药材" Command="{Binding AddHerbCommand}"/>
                        </ToolBar>

                        <DataGrid Grid.Row="1"
                                  ItemsSource="{Binding HerbItems}"
                                  AutoGenerateColumns="False"
                                  CanUserAddRows="False"
                                  MinHeight="200">
                            <DataGrid.Columns>
                                <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}" Width="150" IsReadOnly="True"/>
                                <DataGridTextColumn Header="剂量" Binding="{Binding Quantity}" Width="80"/>
                                <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="80"/>
                                <DataGridTextColumn Header="单价" Binding="{Binding Herb.Price, StringFormat={}{0:F2}元}" Width="100" IsReadOnly="True"/>
                                <DataGridTextColumn Header="小计" Binding="{Binding SubTotal, StringFormat={}{0:F2}元}" Width="100" IsReadOnly="True"/>
                                <DataGridTemplateColumn Header="已验证" Width="80">
                                    <DataGridTemplateColumn.CellTemplate>
                                        <DataTemplate>
                                            <CheckBox IsChecked="{Binding IsValidated}" IsEnabled="False"/>
                                        </DataTemplate>
                                    </DataGridTemplateColumn.CellTemplate>
                                </DataGridTemplateColumn>
                                <DataGridTemplateColumn Header="操作" Width="80">
                                    <DataGridTemplateColumn.CellTemplate>
                                        <DataTemplate>
                                            <Button Content="移除" Command="{Binding DataContext.RemoveHerbCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                    CommandParameter="{Binding}"/>
                                        </DataTemplate>
                                    </DataGridTemplateColumn.CellTemplate>
                                </DataGridTemplateColumn>
                            </DataGrid.Columns>
                        </DataGrid>
                    </Grid>
                </GroupBox>
            </StackPanel>
        </ScrollViewer>

        <!-- StatusBar -->
        <StatusBar Grid.Row="2">
            <StatusBarItem>
                <TextBlock Text="{Binding HerbCount, StringFormat={}药材数: {0}}"/>
            </StatusBarItem>
            <StatusBarItem>
                <TextBlock Text="{Binding TotalPrice, StringFormat={}总价: {0:F2}元}"/>
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <ProgressBar Width="100" Height="20" IsIndeterminate="True"
                             Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</UserControl>
```

### 7.3 FormulaValidationView.xaml

```xml
<UserControl x:Class="LYBT.Desktop.Formula.Views.FormulaValidationView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>    <!-- ToolBar -->
            <RowDefinition Height="*"/>       <!-- Content -->
            <RowDefinition Height="Auto"/>    <!-- StatusBar -->
        </Grid.RowDefinitions>

        <!-- ToolBar -->
        <ToolBar Grid.Row="0">
            <Button Content="刷新" Command="{Binding RefreshCommand}"/>
            <Button Content="完成验证" Command="{Binding CompleteValidationCommand}"/>
        </ToolBar>

        <!-- Content -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="300"/>
                <ColumnDefinition Width="5"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- 左侧：待验证验方列表 -->
            <GroupBox Grid.Column="0" Header="待验证验方">
                <ListBox ItemsSource="{Binding PendingFormulas}"
                         SelectedItem="{Binding SelectedFormula}">
                    <ListBox.ItemTemplate>
                        <DataTemplate>
                            <StackPanel>
                                <TextBlock Text="{Binding Name}" FontWeight="Bold"/>
                                <TextBlock Text="{Binding HerbCount, StringFormat={}药材数: {0}}" FontSize="10" Foreground="Gray"/>
                            </StackPanel>
                        </DataTemplate>
                    </ListBox.ItemTemplate>
                </ListBox>
            </GroupBox>

            <GridSplitter Grid.Column="1" Width="5" HorizontalAlignment="Stretch"/>

            <!-- 右侧：未验证药材列表 -->
            <GroupBox Grid.Column="2" Header="未验证药材">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                    </Grid.RowDefinitions>

                    <!-- 验证进度 -->
                    <StackPanel Grid.Row="0" Margin="10">
                        <TextBlock Text="{Binding ValidationProgress, StringFormat={}验证进度: {0}}" FontWeight="Bold"/>
                        <ProgressBar Height="20" Margin="0,5,0,0"
                                     Value="{Binding SelectedFormula.ValidationProgressPercent}"
                                     Maximum="100"/>
                    </StackPanel>

                    <!-- 未验证药材列表 -->
                    <DataGrid Grid.Row="1"
                              ItemsSource="{Binding UnvalidatedHerbs}"
                              AutoGenerateColumns="False"
                              IsReadOnly="True"
                              SelectionMode="Single">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="原始药材名" Binding="{Binding OriginalHerbName}" Width="150"/>
                            <DataGridTextColumn Header="剂量" Binding="{Binding Quantity}" Width="80"/>
                            <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="80"/>
                            <DataGridTemplateColumn Header="操作" Width="100">
                                <DataGridTemplateColumn.CellTemplate>
                                    <DataTemplate>
                                        <Button Content="选择药材"
                                                Command="{Binding DataContext.SelectHerbCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                CommandParameter="{Binding}"/>
                                    </DataTemplate>
                                </DataGridTemplateColumn.CellTemplate>
                            </DataGridTemplateColumn>
                        </DataGrid.Columns>
                    </DataGrid>
                </Grid>
            </GroupBox>
        </Grid>

        <!-- StatusBar -->
        <StatusBar Grid.Row="2">
            <StatusBarItem>
                <TextBlock Text="{Binding PendingFormulas.Count, StringFormat={}待验证验方: {0}}"/>
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <ProgressBar Width="100" Height="20" IsIndeterminate="True"
                             Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</UserControl>
```

---

## 8. Repository数据访问

### 8.1 IFormulaRepository接口

```csharp
public interface IFormulaRepository
{
    // CRUD操作
    Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);
    Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

    // 查询操作
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null);
    Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync();
    Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);
    Task<ServiceResult<List<FormulaDto>>> GetPendingValidationFormulasAsync();

    // Excel导入导出
    Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(Stream stream, string? fileName = null);
    Task<ServiceResult<byte[]>> ExportAsync(List<Guid>? formulaIds = null);
    ServiceResult<byte[]> GenerateImportTemplate();

    // 验证与克隆
    Task<ServiceResult> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId);
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid sourceId);
}
```

### 8.2 FormulaRepository实现

```csharp
public class FormulaRepository : IFormulaRepository
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "/api/v1/formulas";

    public FormulaRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(_baseUrl, dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult<FormulaDto>>();
            return result ?? ServiceResult<FormulaDto>.Failure("创建失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<FormulaDto>.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<FormulaDto>.Failure($"创建失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/{id}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult<FormulaDto>>();
            return result ?? ServiceResult<FormulaDto>.Failure("获取失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<FormulaDto>.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<FormulaDto>.Failure($"获取失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/{id}", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult<FormulaDto>>();
            return result ?? ServiceResult<FormulaDto>.Failure("更新失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<FormulaDto>.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<FormulaDto>.Failure($"更新失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/{id}");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
            return result ?? ServiceResult.Failure("删除失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"删除失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null)
    {
        try
        {
            var url = $"{_baseUrl}?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                url += $"&keyword={Uri.EscapeDataString(keyword)}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult<PagedResult<FormulaDto>>>();
            return result ?? ServiceResult<PagedResult<FormulaDto>>.Failure("获取列表失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<PagedResult<FormulaDto>>.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<PagedResult<FormulaDto>>.Failure($"获取列表失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword)
    {
        try
        {
            var url = $"{_baseUrl}/search?keyword={Uri.EscapeDataString(keyword)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<FormulaDto>>>();
            return result ?? ServiceResult<List<FormulaDto>>.Failure("搜索失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<List<FormulaDto>>.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<FormulaDto>>.Failure($"搜索失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetPendingValidationFormulasAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/pending-validation");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult<List<FormulaDto>>>();
            return result ?? ServiceResult<List<FormulaDto>>.Failure("获取待验证验方失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<List<FormulaDto>>.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<List<FormulaDto>>.Failure($"获取待验证验方失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId)
    {
        try
        {
            var dto = new ValidateFormulaHerbDto
            {
                FormulaId = formulaId,
                HerbItemId = herbItemId,
                SelectedHerbId = selectedHerbId
            };

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/validate-herb", dto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
            return result ?? ServiceResult.Failure("验证失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult.Failure($"验证失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid sourceId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/{sourceId}/clone", null);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ServiceResult<FormulaDto>>();
            return result ?? ServiceResult<FormulaDto>.Failure("克隆失败");
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<FormulaDto>.Failure($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<FormulaDto>.Failure($"克隆失败: {ex.Message}");
        }
    }
}
```

### 8.3 Excel导入实现

```csharp
public async Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(Stream stream, string? fileName = null)
{
    try
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(streamContent, "file", fileName ?? "formula.xlsx");

        var response = await _httpClient.PostAsync($"{_baseUrl}/import", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceResult<FormulaImportResultDto>>();
        return result ?? ServiceResult<FormulaImportResultDto>.Failure("导入失败");
    }
    catch (HttpRequestException ex)
    {
        return ServiceResult<FormulaImportResultDto>.Failure($"网络请求失败: {ex.Message}");
    }
    catch (Exception ex)
    {
        return ServiceResult<FormulaImportResultDto>.Failure($"导入失败: {ex.Message}");
    }
}
```

**ViewModel中的调用**:
```csharp
private async Task ImportFormulasAsync()
{
    var openFileDialog = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "Excel Files|*.xlsx;*.xls",
        Title = "选择验方Excel文件"
    };

    if (openFileDialog.ShowDialog() != true)
        return;

    await ExecuteSafelyAsync(async () =>
    {
        using var stream = File.OpenRead(openFileDialog.FileName);
        var result = await _formulaRepository.ImportFromExcelAsync(stream, Path.GetFileName(openFileDialog.FileName));

        if (result.Succeeded && result.Data != null)
        {
            var importResult = result.Data;
            MessageBox.Show(
                $"导入完成！\n成功: {importResult.SuccessCount}\n失败: {importResult.FailureCount}",
                "导入结果",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await LoadPageAsync(1);
        }
        else
        {
            MessageBox.Show($"导入失败: {result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return result.Data;
    });
}
```

### 8.4 Excel导出实现

```csharp
public async Task<ServiceResult<byte[]>> ExportAsync(List<Guid>? formulaIds = null)
{
    try
    {
        var url = $"{_baseUrl}/export";
        if (formulaIds != null && formulaIds.Any())
        {
            url += "?ids=" + string.Join("&ids=", formulaIds.Select(id => id.ToString()));
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        return ServiceResult<byte[]>.Success(fileBytes);
    }
    catch (HttpRequestException ex)
    {
        return ServiceResult<byte[]>.Failure($"网络请求失败: {ex.Message}");
    }
    catch (Exception ex)
    {
        return ServiceResult<byte[]>.Failure($"导出失败: {ex.Message}");
    }
}
```

**ViewModel中的调用**:
```csharp
private async Task ExportFormulasAsync()
{
    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
    {
        Filter = "Excel Files|*.xlsx",
        FileName = $"验方导出_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
    };

    if (saveFileDialog.ShowDialog() != true)
        return;

    await ExecuteSafelyAsync(async () =>
    {
        var result = await _formulaRepository.ExportAsync();

        if (result.Succeeded && result.Data != null)
        {
            await File.WriteAllBytesAsync(saveFileDialog.FileName, result.Data);
            MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"导出失败: {result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return result.Data;
    });
}
```

---

## 9. 数据验证与保存

### 9.1 FormulaDto验证

**IValidatable接口**:
```csharp
public class FormulaDto : IValidatable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Effect { get; set; }
    public string? Usage { get; set; }
    public string? Property { get; set; }
    public string? Remark { get; set; }
    public string Category { get; set; } = "自定义";
    public bool IsShared { get; set; }
    public FormulaValidationStatus ValidationStatus { get; set; }
    public List<FormulaHerbItemDto> Herbs { get; set; } = new();

    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("验方名称不能为空");

        if (Name?.Length > 100)
            errors.Add("验方名称不能超过100个字符");

        if (Effect?.Length > 500)
            errors.Add("功效说明不能超过500个字符");

        if (Usage?.Length > 500)
            errors.Add("用法说明不能超过500个字符");

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            ErrorMessages = errors
        };
    }
}
```

### 9.2 客户端验证规则

**FormulaValidator组件**（见第6.4节）:
```csharp
// 验证验方基本信息
var formulaValidation = _validator.ValidateFormula(CurrentFormula);
if (!formulaValidation.IsValid)
{
    MessageBox.Show(
        string.Join("\n", formulaValidation.ErrorMessages),
        "验证失败",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    return;
}

// 验证药材列表
var herbValidation = _validator.ValidateHerbItems(HerbItems);
if (!herbValidation.IsValid)
{
    MessageBox.Show(
        string.Join("\n", herbValidation.ErrorMessages),
        "验证失败",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    return;
}
```

### 9.3 保存流程

```csharp
private async Task SaveFormulaAsync()
{
    // 1. 客户端验证
    var formulaValidation = _validator.ValidateFormula(CurrentFormula);
    if (!formulaValidation.IsValid)
    {
        MessageBox.Show(string.Join("\n", formulaValidation.ErrorMessages), "验证失败");
        return;
    }

    var herbValidation = _validator.ValidateHerbItems(HerbItems);
    if (!herbValidation.IsValid)
    {
        MessageBox.Show(string.Join("\n", herbValidation.ErrorMessages), "验证失败");
        return;
    }

    // 2. 调用CommandHandler保存
    await ExecuteSafelyAsync(async () =>
    {
        bool success;
        string message;

        if (IsEditMode)
        {
            var updateDto = new FormulaUpdateDto { /* 映射属性 */ };
            (success, message) = await _commandHandler.UpdateFormulaAsync(FormulaId, updateDto);
        }
        else
        {
            var createDto = new FormulaCreateDto { /* 映射属性 */ };
            (success, message) = await _commandHandler.CreateFormulaAsync(createDto);
        }

        // 3. 处理保存结果
        if (success)
        {
            MessageBox.Show("保存成功", "提示");
            NavigateBack();
        }
        else
        {
            MessageBox.Show($"保存失败: {message}", "错误");
        }

        return success;
    });
}
```

---

## 10. Prism Region导航

### 10.1 Region注册

**App.xaml.cs**:
```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册Views和ViewModels
    containerRegistry.RegisterForNavigation<FormulaManagementView, FormulaManagementViewModel>();
    containerRegistry.RegisterForNavigation<FormulaDetailView, FormulaDetailViewModel>();
    containerRegistry.RegisterForNavigation<FormulaValidationView, FormulaValidationViewModel>();
}
```

**ShellWindow.xaml**:
```xml
<ContentControl prism:RegionManager.RegionName="ContentRegion"/>
```

### 10.2 导航到FormulaManagementView

```csharp
// 在MainWindowViewModel或其他入口ViewModel中
_regionManager.RequestNavigate("ContentRegion", "FormulaManagementView");
```

### 10.3 导航到FormulaDetailView（新增）

```csharp
_regionManager.RequestNavigate("ContentRegion", "FormulaDetailView");
```

### 10.4 导航到FormulaDetailView（编辑）

```csharp
var parameters = new NavigationParameters
{
    { "FormulaId", selectedFormula.Id.ToString() }
};

_regionManager.RequestNavigate("ContentRegion", "FormulaDetailView", parameters);
```

### 10.5 接收导航参数

```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    var formulaIdStr = navigationContext.Parameters.GetValue<string>("FormulaId");

    if (Guid.TryParse(formulaIdStr, out var formulaId))
    {
        // 编辑模式
        FormulaId = formulaId;
        IsEditMode = true;
        await LoadFormulaAsync(formulaId);
        _dataManager.CreateSnapshot(CurrentFormula);
    }
    else
    {
        // 新增模式
        IsEditMode = false;
        InitializeNewFormula();
    }
}
```

### 10.6 返回列表页

```csharp
private void NavigateBack()
{
    _regionManager.RequestNavigate("ContentRegion", "FormulaManagementView");
}
```

---

## 11. Dialog使用

### 11.1 HerbSelectionDialog注册

**App.xaml.cs**:
```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册Dialog
    containerRegistry.RegisterDialog<HerbSelectionDialog, HerbSelectionDialogViewModel>();
}
```

### 11.2 打开HerbSelectionDialog

```csharp
private void OpenAddHerbDialog()
{
    var parameters = new DialogParameters
    {
        { "SelectionMode", "Single" }, // 或 "Multiple"
        { "Title", "选择药材" }
    };

    _dialogService.ShowDialog("HerbSelectionDialog", parameters, result =>
    {
        if (result.Result != ButtonResult.OK)
            return;

        var selectedHerbs = result.Parameters.GetValue<List<HerbDto>>("SelectedHerbs");
        if (selectedHerbs == null || !selectedHerbs.Any())
            return;

        foreach (var selectedHerb in selectedHerbs)
        {
            var herbItem = new FormulaHerbItemDto
            {
                Id = Guid.NewGuid(),
                HerbId = selectedHerb.Id,
                HerbName = selectedHerb.Name,
                Quantity = 1,
                Unit = "克",
                IsValidated = true,
                Herb = selectedHerb
            };

            HerbItems.Add(herbItem);
        }
    });
}
```

### 11.3 HerbSelectionDialog传递参数

**HerbSelectionDialogViewModel**:
```csharp
public class HerbSelectionDialogViewModel : BindableBase, IDialogAware
{
    public string Title { get; set; } = "选择药材";

    public event Action<IDialogResult>? RequestClose;

    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        // 接收参数
        var title = parameters.GetValue<string>("Title");
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title;
        }

        var selectionMode = parameters.GetValue<string>("SelectionMode");
        IsMultipleSelection = selectionMode == "Multiple";
    }

    private void ConfirmSelection()
    {
        var parameters = new DialogParameters
        {
            { "SelectedHerbs", SelectedHerbs.ToList() }
        };

        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
    }
}
```

---

## 12. Excel导入导出实现

### 12.1 导入Excel工作流

```csharp
private async Task ImportFormulasAsync()
{
    // 1. 打开文件对话框
    var openFileDialog = new Microsoft.Win32.OpenFileDialog
    {
        Filter = "Excel Files|*.xlsx;*.xls",
        Title = "选择验方Excel文件"
    };

    if (openFileDialog.ShowDialog() != true)
        return;

    // 2. 读取文件流
    await ExecuteSafelyAsync(async () =>
    {
        using var stream = File.OpenRead(openFileDialog.FileName);

        // 3. 调用Repository导入
        var result = await _formulaRepository.ImportFromExcelAsync(
            stream,
            Path.GetFileName(openFileDialog.FileName));

        // 4. 显示导入结果
        if (result.Succeeded && result.Data != null)
        {
            var importResult = result.Data;

            var message = $"导入完成！\n\n" +
                         $"成功: {importResult.SuccessCount} 条\n" +
                         $"失败: {importResult.FailureCount} 条\n";

            if (importResult.Errors.Any())
            {
                message += $"\n失败原因:\n{string.Join("\n", importResult.Errors.Take(5))}";
            }

            MessageBox.Show(message, "导入结果", MessageBoxButton.OK, MessageBoxImage.Information);

            // 5. 刷新列表
            await LoadPageAsync(1);
        }
        else
        {
            MessageBox.Show($"导入失败: {result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return result.Data;
    });
}
```

### 12.2 导出Excel工作流

```csharp
private async Task ExportFormulasAsync()
{
    // 1. 打开保存文件对话框
    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
    {
        Filter = "Excel Files|*.xlsx",
        FileName = $"验方导出_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
    };

    if (saveFileDialog.ShowDialog() != true)
        return;

    // 2. 调用Repository导出
    await ExecuteSafelyAsync(async () =>
    {
        // 获取选中项ID（如果需要导出选中项）
        var selectedIds = Items
            .Where(f => f.IsSelected) // 假设FormulaDto有IsSelected属性
            .Select(f => f.Id)
            .ToList();

        var result = await _formulaRepository.ExportAsync(selectedIds.Any() ? selectedIds : null);

        // 3. 保存文件
        if (result.Succeeded && result.Data != null)
        {
            await File.WriteAllBytesAsync(saveFileDialog.FileName, result.Data);
            MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"导出失败: {result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return result.Data;
    });
}
```

### 12.3 导出Excel模板

```csharp
private void ExportImportTemplate()
{
    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
    {
        Filter = "Excel Files|*.xlsx",
        FileName = "验方导入模板.xlsx"
    };

    if (saveFileDialog.ShowDialog() != true)
        return;

    try
    {
        var result = _formulaRepository.GenerateImportTemplate();

        if (result.Succeeded && result.Data != null)
        {
            File.WriteAllBytes(saveFileDialog.FileName, result.Data);
            MessageBox.Show("模板导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"模板导出失败: {result.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"模板导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### 12.4 进度反馈（可选）

**显示ProgressBar**:
```xml
<ProgressBar IsIndeterminate="True"
             Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}"/>
```

**IsBusy管理**:
```csharp
protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action)
{
    try
    {
        IsBusy = true; // 开始操作，显示ProgressBar
        return await action();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"操作失败: {ex.Message}", "错误");
        return default;
    }
    finally
    {
        IsBusy = false; // 操作结束，隐藏ProgressBar
    }
}
```

---

## 13. 错误处理与日志

### 13.1 ExecuteSafelyAsync模式

**UnifiedViewModelBase中的实现**:
```csharp
protected async Task<T?> ExecuteSafelyAsync<T>(Func<Task<T>> action)
{
    try
    {
        IsBusy = true;
        return await action();
    }
    catch (HttpRequestException ex)
    {
        MessageBox.Show($"网络请求失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        return default;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        return default;
    }
    finally
    {
        IsBusy = false;
    }
}
```

**使用示例**:
```csharp
private async Task LoadFormulaAsync(Guid formulaId)
{
    await ExecuteSafelyAsync(async () =>
    {
        var result = await _dataManager.LoadFormulaAsync(formulaId);

        if (result.success && result.formula != null)
        {
            CurrentFormula = result.formula;
            _dataManager.LoadHerbItems(HerbItems, result.formula.Herbs);
        }
        else
        {
            MessageBox.Show(result.errorMessage ?? "加载验方失败", "错误");
            NavigateBack();
        }

        return result.formula;
    });
}
```

### 13.2 Repository异常处理

```csharp
public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
{
    try
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{id}");
        response.EnsureSuccessStatusCode(); // 抛出HttpRequestException如果状态码非2xx

        var result = await response.Content.ReadFromJsonAsync<ServiceResult<FormulaDto>>();
        return result ?? ServiceResult<FormulaDto>.Failure("获取失败");
    }
    catch (HttpRequestException ex)
    {
        // 网络异常（超时、连接失败、404等）
        return ServiceResult<FormulaDto>.Failure($"网络请求失败: {ex.Message}");
    }
    catch (JsonException ex)
    {
        // JSON反序列化异常
        return ServiceResult<FormulaDto>.Failure($"数据解析失败: {ex.Message}");
    }
    catch (Exception ex)
    {
        // 其他异常
        return ServiceResult<FormulaDto>.Failure($"获取失败: {ex.Message}");
    }
}
```

### 13.3 日志记录（推荐）

**使用ILogger**:
```csharp
public class FormulaDetailViewModel : UnifiedViewModelBase
{
    private readonly ILogger<FormulaDetailViewModel> _logger;

    public FormulaDetailViewModel(
        ILogger<FormulaDetailViewModel> logger,
        /* 其他依赖 */)
    {
        _logger = logger;
    }

    private async Task LoadFormulaAsync(Guid formulaId)
    {
        _logger.LogInformation("开始加载验方: {FormulaId}", formulaId);

        await ExecuteSafelyAsync(async () =>
        {
            var result = await _dataManager.LoadFormulaAsync(formulaId);

            if (result.success && result.formula != null)
            {
                _logger.LogInformation("验方加载成功: {FormulaId}, 名称: {Name}", formulaId, result.formula.Name);
                CurrentFormula = result.formula;
                _dataManager.LoadHerbItems(HerbItems, result.formula.Herbs);
            }
            else
            {
                _logger.LogWarning("验方加载失败: {FormulaId}, 错误: {Error}", formulaId, result.errorMessage);
                MessageBox.Show(result.errorMessage ?? "加载验方失败", "错误");
                NavigateBack();
            }

            return result.formula;
        });
    }
}
```

---

## 14. 常见问题与陷阱

### 14.1 ObservableCollection性能陷阱

**❌ 错误:逐个添加导致多次UI更新**:
```csharp
// 每次Add都会触发CollectionChanged事件，导致UI重绘
HerbItems.Clear();
foreach (var herb in newHerbs)
{
    HerbItems.Add(herb); // 多次UI更新
}
```

**✅ 正确:使用FormulaDataManager批量加载**:
```csharp
// FormulaDataManager.LoadHerbItems内部优化了批量操作
_dataManager.LoadHerbItems(HerbItems, newHerbs);
```

### 14.2 内存泄漏风险

**❌ 错误:直接订阅事件**:
```csharp
HerbItems.CollectionChanged += OnHerbItemsChanged;
```

**✅ 正确:使用WeakEventManager**:
```csharp
WeakEventManager<ObservableCollection<FormulaHerbItemDto>, NotifyCollectionChangedEventArgs>
    .AddHandler(HerbItems, nameof(HerbItems.CollectionChanged), OnHerbItemsChanged);
```

### 14.3 计算属性未更新

**❌ 错误:未触发PropertyChanged**:
```csharp
public decimal TotalPrice => _calculator.CalculateTotalPrice(HerbItems);

// HerbItems变化后，TotalPrice不会自动更新
```

**✅ 正确:手动触发PropertyChanged**:
```csharp
private void OnHerbItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
{
    RaisePropertyChanged(nameof(HerbCount));
    RaisePropertyChanged(nameof(TotalPrice)); // 手动触发
}
```

### 14.4 导航参数丢失

**❌ 错误:未检查参数存在性**:
```csharp
var formulaIdStr = navigationContext.Parameters.GetValue<string>("FormulaId");
var formulaId = Guid.Parse(formulaIdStr); // 如果参数不存在，抛出异常
```

**✅ 正确:使用TryParse**:
```csharp
var formulaIdStr = navigationContext.Parameters.GetValue<string>("FormulaId");
if (Guid.TryParse(formulaIdStr, out var formulaId))
{
    // 编辑模式
    await LoadFormulaAsync(formulaId);
}
else
{
    // 新增模式
    InitializeNewFormula();
}
```

### 14.5 UI线程阻塞

**❌ 错误:同步调用阻塞UI**:
```csharp
var result = _formulaRepository.GetByIdAsync(formulaId).Result; // 阻塞UI线程
```

**✅ 正确:使用async/await**:
```csharp
var result = await _formulaRepository.GetByIdAsync(formulaId); // 不阻塞UI线程
```

### 14.6 快照未及时创建

**❌ 错误:未创建快照导致取消功能失效**:
```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    await LoadFormulaAsync(formulaId);
    // 未创建快照
}
```

**✅ 正确:加载后立即创建快照**:
```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    await LoadFormulaAsync(formulaId);
    _dataManager.CreateSnapshot(CurrentFormula); // 创建快照
}
```

### 14.7 Command CanExecute未更新

**❌ 错误:属性变化后未触发CanExecute检查**:
```csharp
public FormulaDto? SelectedItem { get; set; }
```

**✅ 正确:属性变化后触发RaiseCanExecuteChanged**:
```csharp
private FormulaDto? _selectedItem;
public FormulaDto? SelectedItem
{
    get => _selectedItem;
    set
    {
        if (SetProperty(ref _selectedItem, value))
        {
            EditCommand.RaiseCanExecuteChanged(); // 触发CanExecute检查
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }
}
```

---

## 15. 检查清单

### 15.1 开发前检查

- [ ] 已阅读 `docs/explanation/architecture/client/formula-design.md`
- [ ] 理解Formula模块的职责边界
- [ ] 理解组件化架构设计（4个核心组件）
- [ ] 理解延迟绑定验证工作流
- [ ] 理解Prism Region导航机制

### 15.2 ViewModel开发检查

- [ ] 继承自 `UnifiedViewModelBase`
- [ ] 使用 `SetProperty` 触发PropertyChanged
- [ ] 使用 `DelegateCommand` 定义Command
- [ ] Command有正确的CanExecute逻辑
- [ ] 属性变化后触发 `RaiseCanExecuteChanged`
- [ ] 计算属性手动触发 `RaisePropertyChanged`
- [ ] 使用 `ExecuteSafelyAsync` 包装异步操作
- [ ] 实现 `INavigationAware` 接口处理导航参数

### 15.3 组件化架构检查

- [ ] 使用 `FormulaDataManager` 管理数据加载和快照
- [ ] 使用 `FormulaCommandHandler` 处理Command执行
- [ ] 使用 `FormulaCalculator` 计算总价和统计数据
- [ ] 使用 `FormulaValidator` 进行客户端验证
- [ ] 组件通过构造函数注入
- [ ] ViewModel只负责协调组件，不包含业务逻辑

### 15.4 XAML开发检查

- [ ] 使用 `{Binding}` 绑定ViewModel属性
- [ ] 使用 `Command` 绑定ViewModel命令
- [ ] 使用 `UpdateSourceTrigger=PropertyChanged` 实现实时更新
- [ ] 使用 `Converter` 转换ValidationStatus等枚举值
- [ ] 使用 `DataTemplate` 自定义ListBox/ComboBox显示
- [ ] 使用 `ProgressBar` 绑定 `IsBusy` 显示加载状态

### 15.5 Repository开发检查

- [ ] 实现 `IFormulaRepository` 接口的14个方法
- [ ] 使用 `HttpClient` 调用WebAPI
- [ ] 使用 `ServiceResult<T>` 统一返回类型
- [ ] 捕获 `HttpRequestException` 和其他异常
- [ ] 返回友好的错误消息
- [ ] Excel导入使用 `MultipartFormDataContent`
- [ ] Excel导出返回 `byte[]`

### 15.6 导航开发检查

- [ ] 在 `App.xaml.cs` 注册Views和ViewModels
- [ ] 使用 `IRegionManager.RequestNavigate` 导航
- [ ] 使用 `NavigationParameters` 传递参数
- [ ] 在 `OnNavigatedTo` 中接收参数
- [ ] 使用 `TryParse` 检查参数有效性
- [ ] 导航失败时返回列表页

### 15.7 Dialog开发检查

- [ ] 在 `App.xaml.cs` 注册Dialog
- [ ] 使用 `IDialogService.ShowDialog` 打开Dialog
- [ ] 使用 `DialogParameters` 传递参数
- [ ] 在 `OnDialogOpened` 中接收参数
- [ ] 使用 `RequestClose` 关闭Dialog并返回结果
- [ ] 检查 `ButtonResult` 判断用户操作

### 15.8 错误处理检查

- [ ] 所有async方法使用 `ExecuteSafelyAsync` 包装
- [ ] 捕获 `HttpRequestException` 处理网络异常
- [ ] 捕获 `JsonException` 处理序列化异常
- [ ] 使用 `MessageBox` 显示错误消息
- [ ] 使用 `ILogger` 记录关键操作日志

### 15.9 性能优化检查

- [ ] 使用 `FormulaDataManager.LoadHerbItems` 批量加载
- [ ] 避免在循环中逐个Add到ObservableCollection
- [ ] 使用 `WeakEventManager` 订阅事件
- [ ] 避免同步调用async方法（`.Result`）
- [ ] 避免在UI线程执行耗时操作

### 15.10 编译与测试检查

- [ ] 0 errors, 0 warnings
- [ ] 运行应用，验证列表加载
- [ ] 测试新增验方流程
- [ ] 测试编辑验方流程
- [ ] 测试删除验方确认对话框
- [ ] 测试搜索功能
- [ ] 测试分页功能
- [ ] 测试Excel导入导出
- [ ] 测试延迟绑定验证工作流
- [ ] 测试克隆验方功能

---

## 16. 参考资料

### 16.1 架构文档

- **Formula架构设计**: `docs/explanation/architecture/client/formula-design.md` - 组件化架构、ViewModel设计、XAML布局、Repository模式
- **Client端架构概览**: `docs/explanation/architecture/client/README.md` - MVVM架构、Prism框架、依赖注入
- **Shared架构**: `docs/explanation/architecture/shared/README.md` - Contract层、DTO设计、ServiceResult模式

### 16.2 开发指南

- **Herbs开发指南**: `docs/how-to-guides/client/herbs-development.md` - 类似的客户端开发模式
- **Patients开发指南**: `docs/how-to-guides/client/patients-development.md` - UnifiedListViewModelBase使用示例
- **Auth开发指南**: `docs/how-to-guides/client/auth-development.md` - Prism导航和Dialog使用

### 16.3 相关Issue

- **Epic #1347**: Formula模块架构设计
- **Epic #1348**: Formula模块Client端实现
- **Epic #1349**: Formula延迟绑定验证实现

### 16.4 外部资源

- **Prism官方文档**: https://prismlibrary.com/docs/
- **WPF MVVM模式**: https://learn.microsoft.com/zh-cn/dotnet/desktop/wpf/data/data-binding-overview
- **HttpClient最佳实践**: https://learn.microsoft.com/zh-cn/dotnet/fundamentals/networking/http/httpclient-guidelines

---

**文档维护**: 本文档应与 `formula-design.md` 保持同步，任何架构变更需同步更新。

**问题反馈**: 如有疑问或建议，请在GitHub Issue中提出。
