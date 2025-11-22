# Formula编辑对话框修复方案设计文档

**文档编号**: DES-Formula-EditDialog-Fix-001
**版本**: v1.0
**状态**: 🎯 待实施
**创建时间**: 2025-01-17
**关联Issue**: #2149
**需求文档**: [formula-editing-area-comprehensive-requirements.md](../requirements/formula-editing-area-comprehensive-requirements.md)

---

## 📋 执行摘要

### 问题描述

**严重架构不匹配**: Issue #2149的8个功能（拼音码匹配、4列布局、键盘快捷键等）已在`FormulaDetailView`完整实现，但该视图被标记为**只读**（编辑功能已移除）。实际编辑界面`EditFormulaDialog`仍使用老式`DataGrid`，且ViewModel仅有骨架Command实现，导致用户**无法使用**这8个已实现的功能。

### 解决方案

**迁移8个功能**从`FormulaDetailView`到`EditFormulaDialog`，保持凌隐宝堂项目的Repository直接模式架构，不引入组件化（Dialog场景简化），复用现有`HerbCardControl`组件和Command实现逻辑。

### 工作量估算

| 任务 | 工作量 | 描述 |
|-----|-------|------|
| **Task 1**: XAML布局迁移 | 0.3天 | 替换DataGrid为UniformGrid+HerbCardControl |
| **Task 2**: ViewModel逻辑实现 | 0.5天 | 实现8个功能的Command逻辑 |
| **Task 3**: 跨模块数据加载 | 0.2天 | IContainerProvider延迟解析IHerbDataManager |
| **总计** | **1.0天** | MVP范围，仅迁移现有功能 |

---

## 第1章：问题根因分析

### 1.1 架构不匹配详情

#### 已实现功能的位置（无法使用）

| 功能ID | 功能名称 | 实现位置 | 状态 | 问题 |
|-------|---------|---------|------|------|
| **FR-001** | 7级拼音码匹配 | FormulaHerbItemViewModel.cs:207-262 | ✅ 完整实现 | ⛔ 在只读视图 |
| **FR-002** | 4列UniformGrid布局 | FormulaDetailView.xaml:342-355 | ✅ 完整实现 | ⛔ 在只读视图 |
| **FR-003** | 键盘快捷键 | HerbCardControl.xaml.cs:98-173 | ✅ 完整实现 | ⛔ 未集成到Dialog |
| **FR-004** | 重复检测+合并 | FormulaDetailViewModel.cs:815-860 | ✅ 完整实现 | ⛔ 在只读视图 |
| **FR-005** | 空槽位管理 | FormulaDetailViewModel.cs:898-910 | ✅ 完整实现 | ⛔ 在只读视图 |
| **FR-006** | 自动填充单位 | FormulaDetailViewModel.cs:864-894 | ✅ 完整实现 | ⛔ 在只读视图 |
| **FR-007** | 读/编辑模式切换 | IsEditMode DependencyProperty | ⚠️ 部分实现 | ⛔ 编辑按钮已移除 |
| **FR-008** | 焦点管理 | HerbCardControl.xaml.cs:206-244 | ✅ 完整实现 | ⛔ 未集成到Dialog |

#### 实际编辑界面的问题（EditFormulaDialog）

**文件**: `EditFormulaDialog.xaml` (Lines 86-133)
```xaml
<!-- ❌ 问题：仍使用老式DataGrid -->
<DataGrid Grid.Row="1" ItemsSource="{Binding HerbItems}"
          SelectedItem="{Binding SelectedHerbItem}"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}" Width="150" />
        <DataGridTextColumn Header="用量" Binding="{Binding Quantity}" Width="80" />
        <!-- ... 其他列 -->
    </DataGrid.Columns>
</DataGrid>
```

**文件**: `EditFormulaDialogViewModel.cs` (Lines 148-150)
```csharp
// ❌ 问题：Command仅骨架实现（只有日志输出）
AddHerbCommand = new DelegateCommand(() =>
    Logger.LogInformation("EditFormulaDialog - 添加药材命令（骨架实现）"));
EditHerbCommand = new DelegateCommand(() =>
    Logger.LogInformation("EditFormulaDialog - 编辑药材命令（骨架实现）"));
RemoveHerbCommand = new DelegateCommand(() =>
    Logger.LogInformation("EditFormulaDialog - 移除药材命令（骨架实现）"));
```

**缺失的关键代码**:
- ❌ 无`ObservableCollection<FormulaHerbItemViewModel> HerbItems`
- ❌ 无`LoadAllHerbsAsync()`方法（分页加载药材）
- ❌ 无`IContainerProvider`注入（跨模块依赖解析）
- ❌ 无真实Command实现（DeleteHerb/OnDosageCompleted/OnHerbSelected）
- ❌ 无`EnsureMinimumBlankRows()`逻辑

### 1.2 用户影响

**用户期望**: 编辑方剂时能使用拼音码快速输入药材、自动检测重复、自动空槽位等智能功能。

**实际情况**: EditFormulaDialog打开后是传统DataGrid界面，无任何智能辅助功能，所有已实现的8个功能**完全无法使用**。

**业务损失**: 医生编辑方剂效率低下，无法享受Issue #2149开发的智能功能，等同于该Issue的开发成果**完全浪费**。

---

## 第2章：架构设计

### 2.1 凌隐宝堂项目架构约束

#### Phase 2/4 Repository直接模式

**数据流**（Issue #1114移除Service层）:
```
User Interaction (EditFormulaDialog)
    ↓
ViewModel (EditFormulaDialogViewModel)
    ↓
Repository (IFormulaRepository + 跨模块IHerbDataManager)
    ↓
HTTP Request (Refit自动生成)
    ↓
Server API (REST Endpoint)
```

**关键特征**:
- ✅ **直接注入Repository**: ViewModel构造函数注入`IFormulaRepository`
- ✅ **跨模块依赖**: 使用`IContainerProvider.Resolve<IHerbDataManager>()`延迟解析
- ❌ **禁止Service层**: 无业务Service中间层
- ❌ **禁止AutoMapper**: Client端手动映射DTO

#### 聚合根模式

**Formula聚合根边界**（需求文档第5.1章）:
```
Formula (聚合根)
├── Id: Guid
├── Name: string
├── Effect: string?
├── ValidationStatus: enum
└── Herbs: List<FormulaHerbItem> (1:N关系)
```

**保存规则**:
- ✅ 一次性提交完整Formula聚合（含Herbs列表）
- ✅ Server端`FormulaService.UpdateAsync()`统一处理
- ❌ 禁止独立保存`FormulaHerbItem`（无独立Repository）

### 2.2 组件设计决策

#### 不引入组件化（简化方案）

**理由**:
1. **Dialog场景**: 编辑对话框是临时性UI，非主视图
2. **代码量适中**: 预估ViewModel代码<500行，无需拆分
3. **MVP原则**: 够用即好，避免过度设计
4. **参考先例**: Users模块的`QuickCreatePatientDialogViewModel`也未组件化

**对比**:
| 场景 | 是否组件化 | 原因 |
|-----|-----------|------|
| PatientDetailViewModel | ✅ 组件化 | 主视图，代码量>600行 |
| QuickCreatePatientDialogViewModel | ❌ 不组件化 | Dialog，代码量<300行 |
| **EditFormulaDialogViewModel** | ❌ **不组件化** | **Dialog，预估<500行** |

### 2.3 依赖注入设计

#### ViewModel构造函数注入

```csharp
public class EditFormulaDialogViewModel : UnifiedViewModelBase, IDialogAware
{
    #region 服务依赖

    private readonly IFormulaRepository _formulaRepository;  // 聚合根Repository
    private readonly IContainerProvider _containerProvider;  // ⭐ 跨模块延迟解析
    private readonly ObservableCollection<HerbDto> _allHerbs = new();  // 药材缓存

    #endregion

    public EditFormulaDialogViewModel(
        IFormulaRepository formulaRepository,  // ⭐ 注入聚合根Repository
        IContainerProvider containerProvider,  // ⭐ 注入容器（延迟解析IHerbDataManager）
        IEventAggregator eventAggregator,      // Prism事件聚合器
        ILoggerFactory loggerFactory,          // 日志工厂
        IRegionManager regionManager,          // Prism区域管理器
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
        _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));

        // 初始化Commands...
    }
}
```

**关键点**:
- ✅ **IContainerProvider**: Prism容器，用于延迟解析跨模块依赖
- ✅ **IFormulaRepository**: 聚合根Repository，负责Formula+Herbs整体操作
- ❌ **不注入IHerbRepository**: 药材加载通过`IHerbDataManager`（跨模块）

#### DI注册（FormulaModule.cs）

```csharp
public class FormulaModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ⭐ Repository已注册（Singleton生命周期）
        containerRegistry.RegisterSingleton<IFormulaRepository, FormulaRepository>();

        // ⭐ ViewModel（Scoped生命周期 - Register）
        containerRegistry.Register<EditFormulaDialogViewModel>();

        // ⭐ Dialog注册（Prism对话框服务）
        containerRegistry.RegisterDialog<EditFormulaDialog, EditFormulaDialogViewModel>();
    }
}
```

---

## 第3章：详细设计

### 3.1 XAML布局迁移（Task 1 - 0.3天）

#### 替换DataGrid为UniformGrid + HerbCardControl

**源文件**: `EditFormulaDialog.xaml` (Lines 86-133)

**修改前**（老式DataGrid）:
```xaml
<DataGrid Grid.Row="1" ItemsSource="{Binding HerbItems}"
          SelectedItem="{Binding SelectedHerbItem}"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}" Width="150" />
        <DataGridTextColumn Header="用量" Binding="{Binding Quantity}" Width="80" />
        <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60" />
        <DataGridTextColumn Header="处理方法" Binding="{Binding ProcessingMethod}" Width="100" />
    </DataGrid.Columns>
</DataGrid>
```

**修改后**（4列UniformGrid + HerbCardControl）:
```xaml
<!-- ⭐ 参考：FormulaDetailView.xaml Lines 334-355 -->
<ItemsControl Grid.Row="1"
              ItemsSource="{Binding HerbItems}"
              Margin="10">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <!-- ⭐ 4列网格布局 -->
            <UniformGrid Columns="4" />
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>

    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <!-- ⭐ 使用现有HerbCardControl组件 -->
            <controls:HerbCardControl
                IsEditMode="{Binding DataContext.IsEditMode, RelativeSource={RelativeSource AncestorType=UserControl}}"
                DeleteCommand="{Binding DataContext.DeleteHerbCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                DosageCompletedCommand="{Binding DataContext.DosageCompletedCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"
                HerbSelectedCommand="{Binding DataContext.HerbSelectedCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**关键变更**:
1. ✅ **ItemsControl替换DataGrid**: 支持自定义ItemTemplate
2. ✅ **UniformGrid面板**: `Columns="4"`实现4列布局
3. ✅ **HerbCardControl组件**: 复用现有组件（已实现键盘快捷键、拼音码过滤）
4. ✅ **Command绑定**: 通过`RelativeSource`绑定到ViewModel的Commands

#### 添加命名空间引用

在`EditFormulaDialog.xaml`顶部添加：
```xaml
<UserControl x:Class="LYBT.Desktop.Formula.Views.EditFormulaDialog"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="clr-namespace:LYBT.Desktop.Formula.Controls">  <!-- ⭐ 添加此行 -->
```

### 3.2 ViewModel逻辑实现（Task 2 - 0.5天）

#### 数据结构

**源文件**: `EditFormulaDialogViewModel.cs`

```csharp
#region 数据属性

private ObservableCollection<FormulaHerbItemViewModel> _herbItems = new();
private bool _isEditMode = true;  // Dialog默认编辑模式

/// <summary>
/// 药材项目集合 - Issue #2149: 支持4列卡片布局
/// </summary>
public ObservableCollection<FormulaHerbItemViewModel> HerbItems
{
    get => _herbItems;
    set => SetProperty(ref _herbItems, value);
}

/// <summary>
/// 编辑模式 - HerbCardControl通过IsEditMode控制UI状态
/// </summary>
public bool IsEditMode
{
    get => _isEditMode;
    set => SetProperty(ref _isEditMode, value);
}

/// <summary>
/// 药材数量统计（排除空槽位）
/// </summary>
public int HerbCount => HerbItems.Count(h => h.HerbId != Guid.Empty);

#endregion
```

#### 初始化逻辑

**参考**: `FormulaDetailViewModel.cs` Lines 532-563

```csharp
/// <summary>
/// 对话框初始化（Prism IDialogAware生命周期）
/// </summary>
public void OnDialogOpened(IDialogParameters parameters)
{
    try
    {
        // 1. 加载所有药材列表（跨模块依赖）
        _ = LoadAllHerbsAsync();

        // 2. 检查是否是编辑现有方剂
        if (parameters.ContainsKey("FormulaId"))
        {
            var formulaId = parameters.GetValue<Guid>("FormulaId");
            _ = LoadFormulaAsync(formulaId);
        }
        else
        {
            // 3. 新建方剂：初始化4个空槽位
            InitializeEmptyHerbs();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "对话框初始化失败");
        _ = ShowErrorMessageAsync("初始化失败，请重试");
    }
}

/// <summary>
/// 初始化空槽位（新建方剂）
/// </summary>
private void InitializeEmptyHerbs()
{
    HerbItems.Clear();
    for (int i = 0; i < 4; i++)
    {
        var emptyItem = CreateBlankHerbItem();
        HerbItems.Add(emptyItem);
    }
}

/// <summary>
/// 创建空白药材项（参考FormulaDetailViewModel Lines 775-781）
/// </summary>
private FormulaHerbItemViewModel CreateBlankHerbItem()
{
    var item = _containerProvider.Resolve<FormulaHerbItemViewModel>();
    item.HerbId = Guid.Empty;
    item.HerbName = string.Empty;
    item.Quantity = 0;
    item.Unit = "g";
    item.AllHerbs = _allHerbs;  // ⭐ 注入药材列表引用（支持拼音码过滤）
    return item;
}
```

#### Commands实现

**参考**: `FormulaDetailViewModel.cs` Lines 783-936

##### DeleteHerbCommand（删除药材 + 自动前移）

```csharp
/// <summary>
/// 删除药材命令实现 - Issue #2149: FR-004自动前移
/// 参考：FormulaDetailViewModel.cs Lines 783-794
/// </summary>
private void DeleteHerb(FormulaHerbItemViewModel? herbItem)
{
    if (herbItem == null || !IsEditMode)
        return;

    // 1. 删除药材
    HerbItems.Remove(herbItem);

    // 2. 刷新HerbCount
    RaisePropertyChanged(nameof(HerbCount));

    // 3. 确保至少有4个空槽位（一行）
    EnsureMinimumBlankRows();

    Logger.LogInformation("删除药材: {HerbName}", herbItem.HerbName);
}
```

##### DosageCompletedCommand（剂量完成 + 重复检测）

```csharp
/// <summary>
/// 剂量输入完成命令 - Issue #2149: FR-004重复检测+自动合并
/// 参考：FormulaDetailViewModel.cs Lines 815-860
/// </summary>
private void OnDosageCompleted(FormulaHerbItemViewModel? herbItem)
{
    if (herbItem == null || !IsEditMode)
        return;

    // 1. 检测重复药材（业务规则BR-009）
    if (herbItem.HerbId != Guid.Empty)
    {
        var duplicates = HerbItems
            .Where(h => h.HerbId == herbItem.HerbId && h != herbItem)
            .ToList();

        if (duplicates.Any())
        {
            // 取较大的剂量（业务规则BR-010）
            var maxQuantity = Math.Max(herbItem.Quantity, duplicates.Max(d => d.Quantity));
            herbItem.Quantity = maxQuantity;

            // 删除重复项
            foreach (var duplicate in duplicates)
            {
                HerbItems.Remove(duplicate);
            }

            _ = ShowWarningMessageAsync($"检测到重复药材：{herbItem.HerbName}，已自动合并，剂量改为{maxQuantity}g（取较大值）");
            Logger.LogWarning("重复药材自动合并: {HerbName}, 剂量: {Quantity}g", herbItem.HerbName, maxQuantity);
        }
    }

    // 2. 刷新HerbCount
    RaisePropertyChanged(nameof(HerbCount));

    // 3. 确保至少有4个空槽位
    EnsureMinimumBlankRows();
}
```

##### HerbSelectedCommand（药材选择 + 自动填充单位）

```csharp
/// <summary>
/// 药材选择完成命令 - Issue #2149: FR-006自动填充单位
/// 参考：FormulaDetailViewModel.cs Lines 864-894
/// </summary>
private void OnHerbSelected(HerbDto? selectedHerb)
{
    if (selectedHerb == null || !IsEditMode)
        return;

    // 查找当前正在编辑的药材项（HerbId为空或匹配）
    var currentItem = HerbItems.FirstOrDefault(h =>
        h.HerbId == selectedHerb.Id ||
        (string.IsNullOrEmpty(h.HerbName) && h.HerbId == Guid.Empty));

    if (currentItem != null)
    {
        // ⭐ 自动填充药材信息
        currentItem.HerbId = selectedHerb.Id;
        currentItem.HerbName = selectedHerb.Name ?? string.Empty;
        currentItem.Unit = selectedHerb.Unit ?? "g";  // 业务规则BR-011

        Logger.LogInformation("药材选择: {HerbName}, 单位: {Unit}", selectedHerb.Name, selectedHerb.Unit);
    }
}
```

##### EnsureMinimumBlankRows（空槽位管理）

```csharp
/// <summary>
/// 确保至少有4个空白槽位 - Issue #2149: FR-005
/// 参考：FormulaDetailViewModel.cs Lines 898-910
/// </summary>
private void EnsureMinimumBlankRows()
{
    const int minBlankSlots = 4;
    var blankSlots = HerbItems.Count(h => h.HerbId == Guid.Empty);

    while (blankSlots < minBlankSlots)
    {
        var newItem = CreateBlankHerbItem();
        HerbItems.Add(newItem);
        blankSlots++;
    }

    Logger.LogDebug("空槽位检查：当前{BlankCount}个，目标{MinBlank}个", blankSlots, minBlankSlots);
}
```

### 3.3 跨模块数据加载（Task 3 - 0.2天）

#### LoadAllHerbsAsync实现

**参考**: `FormulaDetailViewModel.cs` Lines 532-563

```csharp
/// <summary>
/// 加载所有药材列表 - Issue #2149: 通过IContainerProvider延迟解析跨模块依赖
/// 参考：FormulaDetailViewModel.cs Lines 532-563
/// </summary>
private async Task LoadAllHerbsAsync()
{
    try
    {
        SetIsBusy(true, "加载药材数据...");

        // ⭐ 延迟解析跨模块依赖（Herbs模块的IHerbDataManager）
        var herbDataManager = _containerProvider.Resolve<IHerbDataManager>();

        _allHerbs.Clear();

        // ⭐ 分页加载（避免API单次返回数据过多）
        const int pageSize = 100;
        int currentPage = 1;
        bool hasMore = true;

        while (hasMore)
        {
            var pagedResult = await herbDataManager.GetPagedAsync(currentPage, pageSize);

            if (pagedResult?.Items == null || !pagedResult.Items.Any())
            {
                hasMore = false;
                break;
            }

            // 添加到缓存
            foreach (var herb in pagedResult.Items)
            {
                _allHerbs.Add(herb);
            }

            // 检查是否还有更多数据
            if (pagedResult.Items.Count < pageSize)
            {
                hasMore = false;
            }
            else
            {
                currentPage++;
            }
        }

        Logger.LogInformation("药材数据加载完成，共{Count}条", _allHerbs.Count);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载药材数据失败");
        await ShowErrorMessageAsync("加载药材数据失败，请重试");
    }
    finally
    {
        SetIsBusy(false);
    }
}
```

**关键点**:
1. ✅ **延迟解析**: `_containerProvider.Resolve<IHerbDataManager>()`跨模块获取
2. ✅ **分页加载**: 每页100条，避免API单次返回过多数据
3. ✅ **缓存管理**: `_allHerbs`缓存所有药材，供FormulaHerbItemViewModel过滤使用
4. ✅ **异步异常处理**: try-catch统一错误提示

#### 药材数据注入到子ViewModel

```csharp
/// <summary>
/// 创建空白药材项 - 注入AllHerbs引用
/// </summary>
private FormulaHerbItemViewModel CreateBlankHerbItem()
{
    var item = _containerProvider.Resolve<FormulaHerbItemViewModel>();
    item.HerbId = Guid.Empty;
    item.HerbName = string.Empty;
    item.Quantity = 0;
    item.Unit = "g";
    item.AllHerbs = _allHerbs;  // ⭐ 注入药材列表引用（支持FR-001拼音码过滤）
    return item;
}
```

**数据流**:
```
EditFormulaDialogViewModel
    ↓
LoadAllHerbsAsync() → _allHerbs (ObservableCollection<HerbDto>)
    ↓
CreateBlankHerbItem() → item.AllHerbs = _allHerbs
    ↓
FormulaHerbItemViewModel.FilterHerbs() → 7级拼音码匹配算法
    ↓
HerbCardControl.ComboBox.ItemsSource="{Binding FilteredHerbs}"
```

### 3.4 保存逻辑

#### 聚合根保存（完整Formula+Herbs）

```csharp
/// <summary>
/// 保存方剂 - 聚合根模式一次性提交
/// </summary>
private async Task SaveAsync()
{
    try
    {
        // 1. 验证必填字段
        if (!ValidateInput())
            return;

        SetIsBusy(true, "保存中...");

        // 2. 构造FormulaInputDto（聚合根）
        var inputDto = new FormulaInputDto
        {
            Id = FormulaId.HasValue && FormulaId.Value != Guid.Empty ? FormulaId.Value : null,
            Name = FormulaName.Trim(),
            Effect = Effect?.Trim(),
            Indication = Indication?.Trim(),
            ValidationStatus = ValidationStatus,
            IsShared = IsShared,

            // ⭐ 聚合根：包含完整Herbs列表
            Herbs = HerbItems
                .Where(h => h.HerbId != Guid.Empty)  // 排除空槽位
                .Select(h => h.ToDto())               // ViewModel → DTO转换
                .ToList()
        };

        // 3. ⭐ 调用聚合根Repository（一次性保存）
        FormulaDto? savedFormula;
        if (FormulaId.HasValue && FormulaId.Value != Guid.Empty)
        {
            savedFormula = await _formulaRepository.UpdateAsync(inputDto);
            Logger.LogInformation("方剂更新成功: {FormulaName} (ID: {FormulaId})", savedFormula.Name, savedFormula.Id);
        }
        else
        {
            savedFormula = await _formulaRepository.CreateAsync(inputDto);
            Logger.LogInformation("方剂创建成功: {FormulaName} (ID: {FormulaId})", savedFormula.Name, savedFormula.Id);
        }

        // 4. 关闭对话框并返回结果
        var parameters = new DialogParameters
        {
            { "SavedFormula", savedFormula }
        };
        RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "保存方剂失败: {FormulaName}", FormulaName);
        await ShowErrorMessageAsync($"保存失败: {ex.Message}");
    }
    finally
    {
        SetIsBusy(false);
    }
}

/// <summary>
/// 验证输入
/// </summary>
private bool ValidateInput()
{
    if (string.IsNullOrWhiteSpace(FormulaName))
    {
        _ = ShowWarningMessageAsync("方剂名称不能为空");
        return false;
    }

    if (HerbCount == 0)
    {
        _ = ShowWarningMessageAsync("请至少添加一味药材");
        return false;
    }

    return true;
}
```

---

## 第4章：代码复用策略

### 4.1 复用现有组件

| 组件 | 文件路径 | 复用方式 | 已实现功能 |
|-----|---------|---------|-----------|
| **HerbCardControl** | Controls/HerbCardControl.xaml | XAML直接引用 | FR-003键盘快捷键、拼音码ComboBox |
| **FormulaHerbItemViewModel** | ViewModels/FormulaHerbItemViewModel.cs | IContainerProvider.Resolve<>() | FR-001七级拼音码匹配算法 |

### 4.2 复用Command实现逻辑

**源代码**: `FormulaDetailViewModel.cs` Lines 783-936

**复用策略**: **直接复制**以下方法到`EditFormulaDialogViewModel.cs`

| 方法 | 行号 | 功能 | 复用度 |
|-----|-----|------|-------|
| `DeleteHerb()` | 783-794 | 删除药材+自动前移 | 100%复制 |
| `OnDosageCompleted()` | 815-860 | 重复检测+自动合并 | 100%复制 |
| `OnHerbSelected()` | 864-894 | 药材选择+单位填充 | 100%复制 |
| `EnsureMinimumBlankRows()` | 898-910 | 空槽位管理 | 100%复制 |
| `CreateBlankHerbItem()` | 775-781 | 创建空槽位 | 微调（注入AllHerbs） |
| `LoadAllHerbsAsync()` | 532-563 | 药材列表加载 | 100%复制 |

**注意**:
- ✅ **代码一致性**: 保持与FormulaDetailViewModel实现完全一致，避免行为差异
- ✅ **日志完整**: 保留所有Logger.LogInformation/LogWarning调用
- ✅ **业务规则**: 保留所有注释中的业务规则引用（BR-009/BR-010/BR-011）

---

## 第5章：测试计划

### 5.1 功能测试

| 测试场景 | 验收标准 | 对应功能ID |
|---------|---------|-----------|
| **拼音码输入** | 输入"dg"显示"当归"排在前列 | FR-001 |
| **4列卡片布局** | 药材以4列UniformGrid展示 | FR-002 |
| **Enter快捷键** | 输入剂量后按Enter自动跳转到下一药材 | FR-003 |
| **重复检测** | 添加重复药材时自动合并并取较大剂量 | FR-004 |
| **空槽位管理** | 删除药材后自动保持至少4个空槽位 | FR-005 |
| **自动填充单位** | 选择药材后自动填充单位（如"g"） | FR-006 |
| **编辑模式** | HerbCardControl的编辑控件可用 | FR-007 |
| **焦点自动前移** | 输入完成后焦点自动移至下一空槽位 | FR-008 |

### 5.2 集成测试

**测试流程**:
1. 打开EditFormulaDialog
2. 输入拼音码"dg" → 验证下拉列表显示"当归"
3. 选择"当归" → 验证单位自动填充为"g"
4. 输入剂量"15"后按Enter → 验证焦点跳转
5. 再次输入"当归" → 验证重复检测弹窗
6. 删除一味药材 → 验证空槽位自动补充
7. 点击保存 → 验证数据提交成功

### 5.3 性能测试

| 性能指标 | 目标值 | 测量方法 |
|---------|-------|---------|
| 拼音码过滤响应时间 | <100ms | UI线程计时 |
| 药材列表加载时间 | <500ms | LoadAllHerbsAsync()耗时 |
| 重复检测时间 | <50ms | OnDosageCompleted()内存操作 |

---

## 第6章：实施计划

### 6.1 Phase划分（MVP - 1天）

| Phase | 任务 | 工作量 | 依赖 | 交付物 |
|-------|-----|-------|------|-------|
| **Phase 1** | XAML布局迁移 | 0.3天 | 无 | EditFormulaDialog.xaml（修改后） |
| **Phase 2** | ViewModel逻辑实现 | 0.5天 | Phase 1 | EditFormulaDialogViewModel.cs（完整实现） |
| **Phase 3** | 跨模块数据加载 | 0.2天 | Phase 2 | LoadAllHerbsAsync()方法 |

### 6.2 详细时间线

**Day 1 上午**（3小时 - Phase 1）:
- [ ] 备份当前`EditFormulaDialog.xaml`（重要！）
- [ ] 替换DataGrid为UniformGrid + HerbCardControl
- [ ] 添加xmlns:controls命名空间
- [ ] 绑定Commands到ViewModel
- [ ] 编译验证XAML无错误

**Day 1 下午**（5小时 - Phase 2 + Phase 3）:
- [ ] 添加数据结构（ObservableCollection<FormulaHerbItemViewModel>）
- [ ] 复制Command实现逻辑（6个方法）
- [ ] 实现LoadAllHerbsAsync()方法
- [ ] 修改OnDialogOpened()初始化逻辑
- [ ] 编译验证无错误
- [ ] 手动测试8个功能
- [ ] 修复发现的Bug

### 6.3 验收标准

**必须通过**:
- ✅ 编译成功（0 Error, 0 Warning）
- ✅ 8个功能全部可用（参考第5.1章测试场景）
- ✅ 无性能退化（参考第5.3章性能指标）
- ✅ 重复检测正确触发（业务规则BR-009/BR-010）
- ✅ 空槽位管理正确（至少4个）
- ✅ 跨模块数据加载成功（AllHerbs.Count > 0）

**可选优化**:
- ⚪ 添加Loading动画（LoadAllHerbsAsync期间）
- ⚪ 优化拼音码过滤性能（<50ms）
- ⚪ 添加单元测试（EditFormulaDialogViewModelTests.cs）

---

## 第7章：风险和缓解措施

### 7.1 技术风险

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| **跨模块依赖解析失败** | 高 | 低 | 参考FormulaDetailViewModel Lines 532-563验证过的实现 |
| **HerbCardControl绑定错误** | 中 | 低 | 使用Snoop工具调试XAML绑定 |
| **重复检测逻辑Bug** | 中 | 中 | 复制FormulaDetailViewModel测试过的代码，避免修改 |
| **性能问题（加载慢）** | 低 | 低 | 分页加载（每页100条）已验证 |

### 7.2 业务风险

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| **用户数据丢失** | 高 | 极低 | 保存前验证输入，使用聚合根事务保证一致性 |
| **功能不符合预期** | 中 | 低 | 复用已实现的代码，行为与FormulaDetailView保持一致 |

---

## 第8章：参考文档

### 8.1 需求文档
- [formula-editing-area-comprehensive-requirements.md](../requirements/formula-editing-area-comprehensive-requirements.md)

### 8.2 架构文档
- [Client端架构指南](../explanation/architecture/client/README.md)
- [Repository模式](../explanation/architecture/client/README.md#6-repository层---数据访问层phase-2核心)
- [组件化设计](../explanation/architecture/client/README.md#5-组件化设计模式epic-1773)

### 8.3 代码参考
- **FormulaDetailViewModel.cs** (Lines 783-936) - 8个功能完整实现
- **HerbCardControl.xaml** - 自定义UserControl组件
- **FormulaHerbItemViewModel.cs** (Lines 207-262) - 7级拼音码匹配算法

### 8.4 相关Issue
- **#2149** - Formula编辑区域8个功能开发（已完成但无法使用）
- **#1114** - Client端移除Service层架构变更

---

## 附录A：关键代码位置索引

| 代码功能 | 文件路径 | 行号 | 说明 |
|---------|---------|------|------|
| **7级拼音码匹配** | FormulaHerbItemViewModel.cs | 207-262 | GetMatchScore()方法 |
| **4列布局XAML** | FormulaDetailView.xaml | 334-355 | UniformGrid + ItemsControl |
| **键盘快捷键** | HerbCardControl.xaml.cs | 98-173 | OnHerbNameKeyDown()方法 |
| **重复检测逻辑** | FormulaDetailViewModel.cs | 815-860 | OnDosageCompleted()方法 |
| **空槽位管理** | FormulaDetailViewModel.cs | 898-910 | EnsureMinimumBlankRows()方法 |
| **单位自动填充** | FormulaDetailViewModel.cs | 864-894 | OnHerbSelected()方法 |
| **跨模块加载** | FormulaDetailViewModel.cs | 532-563 | LoadAllHerbsAsync()方法 |
| **聚合根保存** | FormulaRepository.cs | N/A | UpdateAsync()方法 |

---

**文档作者**: Claude (lybtzyzs-design-generator skill)
**审核状态**: ⏳ 待用户确认
**下一步**: 用户确认后，进入Phase 3实施阶段
