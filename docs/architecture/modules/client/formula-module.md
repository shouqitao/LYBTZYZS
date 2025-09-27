# 客户端Formula模块设计文档

## 1. 模块概述

**Formula模块**是WPF桌面客户端的核心业务模块之一，负责中医验方的管理功能。该模块采用**UltraThink双层架构**设计，通过MVVM模式实现用户界面与业务逻辑的分离，提供验方的创建、编辑、查询、复制等完整功能。

### 1.1 模块定位
- **模块类型**: Prism业务模块
- **责任范围**: 验方管理界面与交互逻辑
- **技术栈**: WPF + Prism.DryIoc + MVVM
- **架构模式**: UltraThink双层架构（Module层 + Service层）

### 1.2 功能特性
- 验方列表管理（分页查询、搜索、筛选）
- 验方详情查看（完整信息展示、药材组成）
- 验方编辑功能（基本信息、药材配伍）
- 验方复制克隆（快速创建相似验方）
- 对话框交互（新增、编辑、查看验方）
- 数据导入导出（支持Excel模板）

## 2. 架构设计（MVVM模式）

### 2.1 整体架构图

```
┌─────────────────────────────────────────────────────────┐
│                    WPF Views                           │
├─────────────────────────────────────────────────────────┤
│                   ViewModels                           │
│  ┌─────────────────┬─────────────────┬─────────────────┐│
│  │ Management      │ Detail          │ Dialog          ││
│  │ ViewModel       │ ViewModel       │ ViewModels      ││
│  └─────────────────┴─────────────────┴─────────────────┘│
├─────────────────────────────────────────────────────────┤
│                   Services                             │
│  ┌─────────────────┬─────────────────┬─────────────────┐│
│  │ FormulaService  │ DialogService   │ Navigation      ││
│  │ (API Proxy)     │                 │ Service         ││
│  └─────────────────┴─────────────────┴─────────────────┘│
├─────────────────────────────────────────────────────────┤
│                   Shared DTOs                          │
│            (来自 LYBT.Shared.Models)                    │
└─────────────────────────────────────────────────────────┘
```

### 2.2 UltraThink双层架构应用

**委托层 (Module层)**:
- ViewModels: 处理UI状态管理和用户交互
- Views: WPF用户界面组件
- Models: UI模型（FormulaItem等）

**服务层 (Service层)**:
- FormulaService: 验方业务服务（API调用代理）
- DialogService: 对话框管理服务
- NavigationService: 页面导航服务

## 3. ViewModels设计

### 3.1 类层次结构

```
NavigationViewModelBase (来自 Desktop.Core)
├── FormulaManagementViewModel (列表管理)
└── FormulaDetailViewModel (详情查看/编辑)

BindableBase (Prism)
├── EditFormulaDialogViewModel (编辑对话框)
└── ViewFormulaDialogViewModel (查看对话框)
```

### 3.2 FormulaManagementViewModel

**职责**: 验方列表管理的主视图模型

**核心属性**:
```csharp
// 数据集合
public ObservableCollection<FormulaItem> Items { get; set; }
public FormulaItem? SelectedItem { get; set; }

// 搜索和筛选
public string SearchKeyword { get; set; }
public string? SelectedCategory { get; set; }

// 分页状态
public int CurrentPage { get; set; }
public int TotalPages { get; set; }
public string StatusText { get; set; }

// 加载状态
public bool IsLoading { get; set; }
```

**关键命令**:
```csharp
public ICommand SearchCommand { get; }
public ICommand AddFormulaCommand { get; }
public ICommand EditCommand { get; }
public ICommand ViewDetailsCommand { get; }
public ICommand CopyCommand { get; }
public ICommand DeleteCommand { get; }
public ICommand RefreshCommand { get; }
public ICommand ImportFormulasCommand { get; }
public ICommand ExportFormulasCommand { get; }

// 分页命令
public ICommand FirstPageCommand { get; }
public ICommand PreviousPageCommand { get; }
public ICommand NextPageCommand { get; }
public ICommand LastPageCommand { get; }
```

### 3.3 FormulaDetailViewModel

**职责**: 验方详情查看和编辑的视图模型

**核心属性**:
```csharp
// 验方数据
public Guid FormulaId { get; set; }
public FormulaDto? Formula { get; set; }
public ObservableCollection<FormulaHerbItemDto> HerbItems { get; set; }

// 编辑状态
public bool IsReadOnly { get; set; }
public bool IsLoading { get; set; }

// 计算属性
public string FormulaName => Formula?.Name ?? string.Empty;
public string Effect => Formula?.Effect ?? string.Empty;
public string Usage => Formula?.Usage ?? string.Empty;
public int HerbCount => Formula?.HerbCount ?? 0;
public decimal TotalPrice => Formula?.TotalPrice ?? 0;
```

**关键命令**:
```csharp
public ICommand LoadDataCommand { get; }
public ICommand BackCommand { get; }
public ICommand EditCommand { get; }
public ICommand SaveCommand { get; }
public ICommand CancelEditCommand { get; }
public ICommand PrintCommand { get; }
public ICommand CopyFormulaCommand { get; }
public ICommand ViewUsageHistoryCommand { get; }
```

### 3.4 EditFormulaDialogViewModel

**职责**: 验方编辑对话框的视图模型

**核心属性**:
```csharp
// 验方数据
public FormulaDto Formula { get; set; }
public ObservableCollection<FormulaHerbItemDto> HerbItems { get; set; }
public FormulaHerbItemDto? SelectedHerbItem { get; set; }

// 辅助数据
public ObservableCollection<string> Categories { get; }
public ObservableCollection<HerbDto> AvailableHerbs { get; }

// 状态
public bool IsLoading { get; set; }
public string StatusMessage { get; set; }
```

**关键命令**:
```csharp
public DelegateCommand SaveCommand { get; }
public DelegateCommand CancelCommand { get; }
public DelegateCommand AddHerbCommand { get; }
public DelegateCommand<FormulaHerbItemDto> RemoveHerbCommand { get; }
public DelegateCommand<FormulaHerbItemDto> EditHerbCommand { get; }
public DelegateCommand LoadDataCommand { get; }
```

## 4. Views界面设计

### 4.1 界面组件结构

```
LYBT.Desktop.Formula.Views/
├── FormulaManagementView.xaml    # 验方列表管理界面
├── FormulaDetailView.xaml        # 验方详情界面
├── EditFormulaDialog.xaml        # 编辑验方对话框
└── ViewFormulaDialog.xaml        # 查看验方对话框
```

### 4.2 FormulaManagementView界面设计

**布局结构**:
```xml
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto" />    <!-- 工具栏 -->
    <RowDefinition Height="*" />       <!-- 数据表格 -->
    <RowDefinition Height="Auto" />    <!-- 状态栏和分页 -->
  </Grid.RowDefinitions>
</Grid>
```

**工具栏功能**:
- 搜索框（支持回车搜索）
- 分类筛选（未来扩展）
- 清空筛选按钮
- 导入模板按钮
- 导出模板按钮  
- 导出验方按钮
- 新增验方按钮
- 刷新按钮

**数据表格列定义**:
```xml
<DataGrid.Columns>
  <DataGridTextColumn Header="验方名称" Binding="{Binding Name}" Width="150" />
  <DataGridTextColumn Header="分类" Binding="{Binding Category}" Width="100" />
  <DataGridTextColumn Header="功效" Binding="{Binding Effect}" Width="*" />
  <DataGridTextColumn Header="来源" Binding="{Binding Source}" Width="120" />
  <DataGridTextColumn Header="总价(元)" Binding="{Binding TotalPrice, StringFormat='{}{0:F2}'}" Width="80" />
  <DataGridTextColumn Header="药材数" Binding="{Binding HerbCount}" Width="60" />
  <DataGridTemplateColumn Header="状态" Width="80" />
  <DataGridTemplateColumn Header="操作" Width="280" />
</DataGrid.Columns>
```

**操作按钮**:
- 查看：查看验方详情
- 编辑：编辑验方信息
- 复制：克隆验方
- 删除：软删除验方

### 4.3 FormulaDetailView界面设计

**布局结构**:
```xml
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="Auto" />    <!-- 标题栏 -->
    <RowDefinition Height="*" />       <!-- 内容区域 -->
  </Grid.RowDefinitions>
</Grid>
```

**标题栏操作**:
- 返回按钮
- 编辑/保存/取消按钮
- 复制验方按钮
- 查看使用记录按钮
- 打印按钮

**内容区域**:
1. **验方基本信息卡片**: 名称、分类、药材数量、状态等
2. **基本信息展开区**: 名称、难度、性味归经、功效、用法、时间信息
3. **药材组成展开区**: 药材统计信息 + 药材列表DataGrid
4. **详细描述展开区**: 验方描述、备注信息

### 4.4 样式系统

**内联样式设计**:
为避免外部资源引用问题，界面采用内联样式定义：

```xml
<UserControl.Resources>
  <Style x:Key="ToolBarBorder" TargetType="Border">
    <Setter Property="Background" Value="#F8F9FA" />
    <Setter Property="BorderBrush" Value="#E9ECEF" />
    <Setter Property="BorderThickness" Value="0,0,0,1" />
    <Setter Property="Padding" Value="20,15" />
  </Style>
  
  <Style x:Key="PrimaryButton" TargetType="Button" BasedOn="{StaticResource BaseButton}">
    <Setter Property="Background" Value="#007BFF" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="BorderBrush" Value="#007BFF" />
  </Style>
  
  <!-- 更多样式定义... -->
</UserControl.Resources>
```

## 5. 前端服务层

### 5.1 FormulaService设计

**接口定义**: `IFormulaService` (来自 Shared.Interfaces)

**实现类**: `LYBT.Desktop.Formula.Services.FormulaService`

**核心职责**:
- API调用代理（通过IFormulaApi）
- 异常处理包装
- 服务结果转换

**关键方法**:
```csharp
public class FormulaService : IFormulaService
{
    private readonly IFormulaApi _formulaApi;
    private readonly ILogger<FormulaService> _logger;
    private readonly IExceptionHandler _exceptionHandler;

    // 分页查询
    public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(
        int page = 1, int pageSize = 20, string? keyword = null)

    // 获取详情
    public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)

    // 创建验方
    public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)

    // 更新验方
    public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)

    // 删除验方
    public async Task<ServiceResult> DeleteAsync(Guid id)

    // 搜索验方
    public async Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword)

    // 克隆验方
    public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId)
}
```

### 5.2 异常处理机制

**异常处理流程**:
```csharp
return await _exceptionHandler.HandleException<FormulaDto>(async () =>
{
    var response = await _formulaApi.GetFormulaByIdAsync(id);
    return ServiceResult<FormulaDto>.Success(response.Content);
}, nameof(GetByIdAsync));
```

**统一异常处理**:
- 网络异常处理
- API响应错误处理
- 数据转换异常处理
- 用户友好错误消息

## 6. 数据绑定与验证

### 6.1 数据绑定策略

**双向绑定**:
```xml
<!-- 搜索关键词 -->
<TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}" />

<!-- 验方信息编辑 -->
<TextBox Text="{Binding Formula.Name, Mode=TwoWay}"
         IsReadOnly="{Binding IsReadOnly}" />
```

**命令绑定**:
```xml
<!-- 搜索命令 -->
<TextBox.InputBindings>
  <KeyBinding Key="Enter" Command="{Binding SearchCommand}" />
</TextBox.InputBindings>

<!-- 操作按钮 -->
<Button Content="编辑" Command="{Binding EditCommand}" CommandParameter="{Binding}" />
```

**集合绑定**:
```xml
<!-- 验方列表 -->
<DataGrid ItemsSource="{Binding Items}" SelectedItem="{Binding SelectedItem}" />

<!-- 药材组成 -->
<DataGrid ItemsSource="{Binding HerbItems}" />
```

### 6.2 数据验证

**DTO验证**: 使用DataAnnotations进行服务端验证
```csharp
[Required(ErrorMessage = "验方名称不能为空")]
[StringLength(100, ErrorMessage = "验方名称不能超过100个字符")]
public string Name { get; set; } = string.Empty;
```

**UI验证**: 实现INotifyDataErrorInfo接口进行客户端验证
```csharp
// 命令可执行状态验证
private bool CanSave()
{
    return !string.IsNullOrWhiteSpace(Formula?.Name) && HerbItems.Count > 0;
}
```

## 7. 路由与导航

### 7.1 Prism区域导航

**主要区域**:
- `SystemWorkbenchContentRegion`: 系统工作台内容区域
- `DialogRegion`: 对话框区域（如果使用）

**导航调用**:
```csharp
// 导航到验方管理
_navigationService.NavigateTo(RegionNames.SystemWorkbenchContentRegion, "FormulaManagementView");

// 导航到验方详情（带参数）
var parameters = new NavigationParameters
{
    { "FormulaId", formulaId },
    { "ViewMode", "Edit" }
};
_navigationService.NavigateToAsync(RegionNames.SystemWorkbenchContentRegion, "FormulaDetailView", parameters);
```

### 7.2 INavigationAware实现

**FormulaDetailViewModel导航处理**:
```csharp
public void OnNavigatedTo(NavigationContext navigationContext)
{
    if (navigationContext.Parameters.ContainsKey("FormulaId"))
    {
        FormulaId = navigationContext.Parameters.GetValue<Guid>("FormulaId");
        
        if (navigationContext.Parameters.ContainsKey("ViewMode"))
        {
            var viewMode = navigationContext.Parameters.GetValue<string>("ViewMode");
            IsReadOnly = viewMode != "Edit";
        }

        Task.Run(async () => await LoadDataAsync());
    }
}

public bool IsNavigationTarget(NavigationContext navigationContext)
{
    if (navigationContext.Parameters.ContainsKey("FormulaId"))
    {
        var targetFormulaId = navigationContext.Parameters.GetValue<Guid>("FormulaId");
        return FormulaId == targetFormulaId;
    }
    return true;
}
```

## 8. 状态管理

### 8.1 本地状态管理

**ViewModels状态**:
- 加载状态: `IsLoading`
- 编辑状态: `IsReadOnly`
- 选择状态: `SelectedItem`
- 搜索状态: `SearchKeyword`

**状态同步**:
```csharp
// 属性变更通知
private string _searchKeyword = string.Empty;
public string SearchKeyword
{
    get => _searchKeyword;
    set => SetProperty(ref _searchKeyword, value);
}

// 命令状态更新
private void RaiseCanExecuteChanged()
{
    ((DelegateCommand)EditCommand).RaiseCanExecuteChanged();
    ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
    ((DelegateCommand)CancelEditCommand).RaiseCanExecuteChanged();
}
```

### 8.2 会话状态管理

**分页状态持久化**:
```csharp
// 保存当前页码和搜索条件
public class FormulaManagementState
{
    public int CurrentPage { get; set; } = 1;
    public string SearchKeyword { get; set; } = string.Empty;
    public string? SelectedCategory { get; set; }
}
```

## 9. API集成

### 9.1 API接口映射

**服务端API**: `IFormulaApi` (通过 LYBT.Desktop.Services)

**客户端调用链**:
```
ViewModel → IFormulaService → IFormulaApi → HTTP Client → Server API
```

**API方法映射**:
```csharp
// 客户端服务方法 → 服务端API端点
GetPagedAsync(page, pageSize, keyword) → GET /api/formulas?page={page}&pageSize={pageSize}&keyword={keyword}
GetByIdAsync(id) → GET /api/formulas/{id}
CreateAsync(dto) → POST /api/formulas
UpdateAsync(id, dto) → PUT /api/formulas/{id}
DeleteAsync(id) → DELETE /api/formulas/{id}
SearchAsync(keyword) → GET /api/formulas/search?keyword={keyword}
CloneFormulaAsync(formulaId) → POST /api/formulas/{formulaId}/clone
```

### 9.2 数据传输对象

**主要DTOs** (来自 LYBT.Shared.Models.Contracts.Formula):

```csharp
// 验方信息DTO
public class FormulaDto : StatusDto, IRemarkable
{
    public string Name { get; set; }
    public string? Effect { get; set; }
    public string? Usage { get; set; }
    public string? Property { get; set; }
    public bool IsShared { get; set; }
    public string? Remark { get; set; }
    public List<FormulaHerbItemDto> Herbs { get; set; }
    
    // 计算属性
    public int HerbCount { get; }
    public decimal TotalPrice { get; }
    public string HerbNames { get; }
    public string Category { get; }
}

// 验方药材组成DTO
public class FormulaHerbItemDto : BaseDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }
    public string? Preparation { get; set; }
    public string? Usage { get; set; }
    public decimal Price { get; set; }
    public int SortOrder { get; set; }
    public HerbDto? Herb { get; set; }  // 导航属性
}

// 创建验方DTO
public class FormulaCreateDto : FormulaInputBaseDto
{
    public List<FormulaHerbItemCreateDto> Herbs { get; set; }
}

// 更新验方DTO  
public class FormulaUpdateDto : FormulaInputBaseDto, IIdentifiable<Guid>
{
    public Guid Id { get; set; }
    public List<FormulaHerbItemUpdateDto> Herbs { get; set; }
}
```

### 9.3 UI模型适配

**FormulaItem UI模型**:
```csharp
public class FormulaItem : BindableBase
{
    // 基础属性映射
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Category { get; set; }
    public string? Effect { get; set; }
    public bool IsActive { get; set; }
    
    // UI专用属性
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsFavorite { get; set; }
    
    // 计算属性
    public string TypeText { get; }
    public string TypeColor { get; }
    public string StatusText { get; }
    public string StatusColor { get; }
    
    // 转换方法
    public static FormulaItem FromDto(FormulaDto dto)
    public FormulaDto ToDto()
}
```

## 10. 实现状态

### 10.1 已完成功能

✅ **基础架构**:
- Prism模块注册与初始化
- MVVM基础架构搭建
- 依赖注入配置

✅ **验方列表管理**:
- FormulaManagementView界面实现
- 分页数据展示
- 搜索和筛选功能
- 操作按钮定义

✅ **验方详情功能**:
- FormulaDetailView界面实现
- 详情信息展示
- 编辑模式切换
- 药材组成展示

✅ **对话框功能**:
- EditFormulaDialog实现
- ViewFormulaDialog实现
- 对话框ViewModel实现

✅ **服务层实现**:
- FormulaService API代理实现
- 异常处理机制
- ServiceResult包装

✅ **数据模型**:
- FormulaItem UI模型实现
- DTO转换机制
- 数据绑定支持

### 10.2 待完成功能

⚠️ **功能完善**:
- [ ] 验方搜索功能完善（SearchAsync方法实现）
- [ ] 验方克隆功能完善（CloneFormulaAsync方法实现）
- [ ] 导入导出功能实现
- [ ] 打印功能实现
- [ ] 使用历史查看功能

⚠️ **UI优化**:
- [ ] 加载状态指示器
- [ ] 错误消息显示优化
- [ ] 响应式布局适配
- [ ] 主题样式统一

⚠️ **数据处理**:
- [ ] 数据验证规则完善
- [ ] 缓存机制实现
- [ ] 离线数据支持

⚠️ **用户体验**:
- [ ] 操作确认对话框
- [ ] 快捷键支持
- [ ] 拖拽排序功能
- [ ] 批量操作功能

### 10.3 技术债务

🔧 **架构优化**:
- 缺少Interfaces目录，需要添加本地接口定义
- 部分ViewModel缺少完整的业务逻辑实现
- 需要引入AutoMapper进行DTO转换优化

🔧 **代码质量**:
- 需要补充单元测试覆盖
- 需要添加XML文档注释
- 需要代码规范检查和优化

🔧 **性能优化**:
- 大数据量分页性能优化
- 图片和资源加载优化
- 内存使用优化

### 10.4 版本规划

**v2.1.0 当前版本**:
- 基础CRUD功能完整实现
- UI界面基本完成
- API集成基本完成

**v2.2.0 计划版本**:
- 完善搜索和筛选功能
- 实现导入导出功能
- 优化用户体验

**v2.3.0 未来版本**:
- 添加高级搜索功能
- 实现智能推荐功能
- 支持移动端适配

---

## 总结

Formula模块作为WPF桌面客户端的重要业务模块，已经建立了完整的MVVM架构基础，实现了基本的验方管理功能。该模块严格遵循**UltraThink双层架构**设计原则，通过清晰的分层和职责分离，确保了代码的可维护性和可扩展性。

当前实现状态良好，基础功能已经完成，后续需要重点完善业务逻辑实现和用户体验优化，以满足中医诊所验方管理的实际需求。