# 客户端Herbs模块设计文档

## 1. 模块概述

### 1.1 基本信息
- **模块名称**: LYBT.Desktop.Herbs
- **功能定位**: 中药材信息管理模块
- **模块类型**: Prism业务模块
- **主要职责**: 提供中药材档案的查看、编辑、搜索和管理功能

### 1.2 业务范围
- 中药材基本信息管理（名称、拼音码、产地、规格、价格等）
- 中药材功效与用法信息维护
- 药材档案的增删改查操作
- 药材信息的搜索与筛选
- 药材列表的分页浏览与批量操作
- 药材详情的查看与编辑

### 1.3 技术特征
- 基于WPF + Prism.DryIoc架构
- 遵循MVVM设计模式
- 采用依赖注入进行服务解耦
- 使用统一设计系统(UnifiedDesignSystem)
- 支持响应式数据绑定和命令模式

## 2. 架构设计（MVVM模式）

### 2.1 整体架构图
```
┌─────────────────────────────────────────────────────────┐
│                 LYBT.Desktop.Herbs                      │
├─────────────────────────────────────────────────────────┤
│  Views (UI层)                                           │
│  ├── HerbManagementView.xaml    # 药材管理主界面       │
│  └── HerbDetailView.xaml        # 药材详情界面         │
├─────────────────────────────────────────────────────────┤
│  ViewModels (展示逻辑层)                               │
│  ├── HerbManagementViewModel.cs # 列表管理视图模型     │
│  └── HerbDetailViewModel.cs     # 详情编辑视图模型     │
├─────────────────────────────────────────────────────────┤
│  Models (UI数据模型层)                                 │
│  └── HerbItem.cs                # UI展示数据模型       │
├─────────────────────────────────────────────────────────┤
│  Services (业务服务层)                                 │
│  └── HerbService.cs             # 药材业务服务实现     │
├─────────────────────────────────────────────────────────┤
│  Module (模块注册层)                                   │
│  └── HerbsModule.cs             # Prism模块定义        │
└─────────────────────────────────────────────────────────┘
                    ↕ (接口依赖)
┌─────────────────────────────────────────────────────────┐
│              Shared Components                          │
│  ├── IHerbService               # 服务接口定义         │
│  ├── HerbDto/CreateDto/UpdateDto # 数据传输对象       │
│  ├── NavigationViewModelBase    # ViewModel基类        │
│  └── UnifiedDesignSystem        # 统一UI样式           │
└─────────────────────────────────────────────────────────┘
```

### 2.2 模块注册与生命周期
```csharp
public class HerbsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.RegisterSingleton<IHerbService, HerbService>();
        
        // 注册视图和视图模型(通过ViewModelLocator自动关联)
        // HerbManagementView ↔ HerbManagementViewModel
        // HerbDetailView ↔ HerbDetailViewModel
    }
}
```

### 2.3 依赖关系图
```
HerbManagementView
    ↓ (AutoWireViewModel)
HerbManagementViewModel
    ↓ (Constructor Injection)
├── IEventAggregator         # Prism事件聚合器
├── ILoggerFactory          # 日志工厂
├── IRegionManager          # 区域管理器
├── ISessionManager         # 会话管理器
└── IErrorHandlingService   # 错误处理服务

HerbService
    ↓ (Constructor Injection)  
├── IHerbApi                # HTTP API客户端
├── ILogger<HerbService>    # 类型化日志器
└── IExceptionHandler       # 异常处理器
```

## 3. ViewModels设计

### 3.1 基类继承体系
```csharp
NavigationViewModelBase (来自Desktop.Core)
    ↓
HerbManagementViewModel / HerbDetailViewModel
```

### 3.2 HerbManagementViewModel（列表管理）

#### 3.2.1 核心职责
- 药材列表的分页加载与显示
- 搜索关键词的处理与筛选
- 列表项的选择与状态管理
- 新增、编辑、删除操作的命令处理
- 批量操作功能（导入、导出）

#### 3.2.2 属性设计
```csharp
public class HerbManagementViewModel : NavigationViewModelBase
{
    // 数据绑定属性
    public ObservableCollection<HerbItem> Items { get; set; }
    public HerbItem? SelectedItem { get; set; }
    public string SearchKeyword { get; set; }
    public string StatusText { get; set; }
    
    // 分页属性
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 20;
    
    // 命令属性
    public ICommand SearchCommand { get; }
    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ToggleStatusCommand { get; }
    
    // 批量操作命令
    public ICommand ImportHerbsCommand { get; }
    public ICommand ExportHerbsCommand { get; }
    public ICommand ExportTemplateCommand { get; }
    
    // 分页命令
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }
}
```

#### 3.2.3 关键方法
```csharp
// 数据加载
private async Task LoadHerbsAsync(int page = 1, string? keyword = null)
private async Task RefreshAsync()

// 搜索处理
private async Task ExecuteSearchAsync()
private bool CanExecuteSearch() => !string.IsNullOrWhiteSpace(SearchKeyword)

// CRUD操作
private async Task ExecuteAddAsync()
private async Task ExecuteEditAsync(HerbItem item)
private async Task ExecuteDeleteAsync(HerbItem item)
private async Task ExecuteToggleStatusAsync(HerbItem item)

// 批量操作
private async Task ExecuteImportAsync()
private async Task ExecuteExportAsync()
private async Task ExecuteExportTemplateAsync()

// 分页操作
private async Task ExecuteFirstPageAsync()
private async Task ExecutePreviousPageAsync()
private async Task ExecuteNextPageAsync()
private async Task ExecuteLastPageAsync()
```

### 3.3 HerbDetailViewModel（详情编辑）

#### 3.3.1 核心职责
- 药材详情数据的加载与显示
- 编辑模式与只读模式的切换
- 数据验证与保存操作
- 用药记录查询功能
- 打印功能支持

#### 3.3.2 属性设计
```csharp
public class HerbDetailViewModel : NavigationViewModelBase
{
    // 核心数据属性
    public HerbItem Herb { get; set; }
    public bool IsReadOnly { get; set; } = true;
    public bool IsLoading { get; set; }
    
    // 显示属性
    public string HerbName => Herb?.Name ?? "未知药材";
    public string Origin => Herb?.Origin ?? "未知";
    public decimal Price => Herb?.UnitPrice ?? 0;
    public string StatusText => Herb?.StatusText ?? "未知";
    public DateTime CreateTime => Herb?.CreatedAt ?? DateTime.MinValue;
    public DateTime? UpdateTime => Herb?.UpdatedAt;
    
    // 命令属性
    public ICommand BackCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand ViewUsageHistoryCommand { get; }
    public ICommand PrintCommand { get; }
}
```

#### 3.3.3 关键方法
```csharp
// 数据操作
private async Task LoadHerbAsync(Guid herbId)
private async Task SaveAsync()
private void CancelEdit()

// 模式切换
private void EnterEditMode()
private void ExitEditMode()

// 业务功能
private async Task ViewUsageHistoryAsync()
private async Task PrintAsync()
private void NavigateBack()

// 数据验证
private bool ValidateHerbData()
private void ResetValidationErrors()
```

## 4. Views界面设计

### 4.1 HerbManagementView（药材管理主界面）

#### 4.1.1 界面布局结构
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />   <!-- 工具栏 -->
        <RowDefinition Height="*" />      <!-- 数据表格 -->
        <RowDefinition Height="Auto" />   <!-- 状态栏和分页 -->
    </Grid.RowDefinitions>
    
    <!-- 工具栏区域 -->
    <Border Grid.Row="0" Style="{StaticResource ToolBarContainer}">
        <!-- 搜索区域 + 操作按钮 -->
    </Border>
    
    <!-- 数据表格区域 -->
    <DataGrid Grid.Row="1" Style="{StaticResource HerbManagementDataGrid}">
        <!-- 药材信息列 -->
    </DataGrid>
    
    <!-- 状态栏和分页区域 -->
    <Border Grid.Row="2" Style="{StaticResource StatusBarContainer}">
        <!-- 统计信息 + 分页控件 -->
    </Border>
</Grid>
```

#### 4.1.2 工具栏功能区域
- **搜索区域**: 关键词输入框 + 搜索按钮
- **批量操作**: 导入药材、导出模板、导出药材按钮
- **基础操作**: 新增药材、刷新按钮

#### 4.1.3 数据表格列设计
| 列名 | 绑定属性 | 宽度 | 说明 |
|------|---------|------|------|
| 药材名称 | Name | Large | 主要标识信息 |
| 拼音码 | PinYinCode | Normal | 快速检索码 |
| 产地 | Origin | Normal | 药材来源 |
| 规格 | Spec | Small | 规格说明 |
| 单位 | Unit | Small | 计量单位 |
| 单价(元) | Price | Normal | 价格信息 |
| 功效 | Effect | XLarge | 功效说明 |
| 状态 | StatusText/StatusColor | Small | 启用/停用状态 |
| 操作 | Commands | Normal | 编辑/状态/删除按钮 |

#### 4.1.4 分页控件设计
- 首页、上一页、下一页、末页按钮
- 当前页码/总页数显示
- 统计信息显示（总记录数、当前显示范围）

### 4.2 HerbDetailView（药材详情界面）

#### 4.2.1 界面布局结构
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />   <!-- 标题栏 -->
        <RowDefinition Height="*" />      <!-- 内容区域 -->
    </Grid.RowDefinitions>
    
    <!-- 标题栏 -->
    <Border Grid.Row="0" Background="{DynamicResource PrimaryHueMidBrush}">
        <!-- 返回按钮 + 标题 + 操作按钮组 -->
    </Border>
    
    <!-- 内容区域(可滚动) -->
    <ScrollViewer Grid.Row="1">
        <!-- 药材基本信息卡片 -->
        <!-- 详细信息展开器 -->
        <!-- 功效用法信息展开器 -->
    </ScrollViewer>
    
    <!-- 加载遮罩 -->
    <Grid Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}">
        <!-- 加载动画 -->
    </Grid>
</Grid>
```

#### 4.2.2 标题栏操作按钮
- **返回按钮**: 返回列表页面
- **编辑按钮**: 进入编辑模式（只读模式下显示）
- **保存按钮**: 保存修改（编辑模式下显示）
- **取消按钮**: 取消编辑（编辑模式下显示）
- **用药记录按钮**: 查看用药历史
- **打印按钮**: 打印药材档案

#### 4.2.3 信息卡片设计
1. **药材基本信息卡片**
   - 药材图标 + 名称、产地、价格概览
   - 状态标签显示
   
2. **基本信息展开器**
   - 药材名称、拼音码（第一行）
   - 产地、规格（第二行）
   - 单位、单价（第三行）
   - 创建时间、更新时间（第四行）

3. **功效用法信息展开器**
   - 功效说明（多行文本框）
   - 用法用量（多行文本框）
   - 备注（多行文本框）

## 5. 前端服务层

### 5.1 HerbService实现

#### 5.1.1 接口定义
```csharp
public interface IHerbService
{
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

#### 5.1.2 服务实现特点
- **异常处理**: 使用IExceptionHandler统一处理异常
- **日志记录**: 集成ILogger进行操作日志记录
- **结果封装**: 使用ServiceResult统一返回格式
- **API调用**: 通过IHerbApi进行HTTP请求

#### 5.1.3 错误处理策略
```csharp
public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
{
    return await _exceptionHandler.HandleException<HerbDto>(async () =>
    {
        var response = await _herbApi.GetHerbByIdAsync(id);
        return ServiceResult<HerbDto>.Success(response.Content);
    }, nameof(GetByIdAsync));
}
```

### 5.2 数据转换与映射

#### 5.2.1 HerbItem模型特点
- 继承自BindableBase，支持属性变更通知
- 提供FromDto/ToDto方法进行数据转换
- 包含UI专用属性（StatusColor、StockStatus等）
- 支持业务逻辑属性（IsAvailable、HasStock等）

#### 5.2.2 关键转换方法
```csharp
// DTO到UI模型转换
public static HerbItem FromDto(HerbDto dto)
{
    return new HerbItem
    {
        Id = dto.Id,
        Name = dto.Name,
        // ... 其他属性映射
        IsActive = dto.Status == CommonStatus.Enabled
    };
}

// UI模型到DTO转换
public HerbDto ToDto()
{
    return new HerbDto
    {
        Id = Id,
        Name = Name,
        // ... 其他属性映射
        Status = IsActive ? CommonStatus.Enabled : CommonStatus.Disabled
    };
}
```

## 6. 数据绑定与验证

### 6.1 数据绑定模式

#### 6.1.1 列表绑定
```xml
<DataGrid ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedItem}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="药材名称"
                            Binding="{Binding Name}" />
        <!-- 其他列定义 -->
    </DataGrid.Columns>
</DataGrid>
```

#### 6.1.2 详情绑定
```xml
<TextBox Text="{Binding Herb.Name, Mode=TwoWay}"
         IsReadOnly="{Binding IsReadOnly}" />
```

#### 6.1.3 命令绑定
```xml
<Button Content="搜索"
        Command="{Binding SearchCommand}" />
        
<Button Content="编辑"
        Command="{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}"
        CommandParameter="{Binding}" />
```

### 6.2 数据验证机制

#### 6.2.1 DTO级别验证
- 使用DataAnnotations特性进行字段验证
- Required、StringLength、Range等验证规则
- DisplayName特性提供友好字段名称

#### 6.2.2 ViewModel级别验证
```csharp
private bool ValidateHerbData()
{
    var validationResults = new List<ValidationResult>();
    var context = new ValidationContext(Herb.ToDto());
    
    return Validator.TryValidateObject(
        Herb.ToDto(), 
        context, 
        validationResults, 
        true);
}
```

#### 6.2.3 UI级别验证反馈
- 错误信息通过ToolTip显示
- 验证失败时边框变红
- 保存按钮在验证失败时禁用

## 7. 路由与导航

### 7.1 模块内导航

#### 7.1.1 区域导航方式
```csharp
// 导航到药材详情
_regionManager.RequestNavigate("MainRegion", "HerbDetailView", 
    new NavigationParameters { { "HerbId", herbId } });

// 返回列表页面  
_regionManager.RequestNavigate("MainRegion", "HerbManagementView");
```

#### 7.1.2 参数传递机制
```csharp
// 在ViewModel中接收导航参数
public override void OnNavigatedTo(NavigationContext navigationContext)
{
    if (navigationContext.Parameters.ContainsKey("HerbId"))
    {
        var herbId = navigationContext.Parameters.GetValue<Guid>("HerbId");
        await LoadHerbAsync(herbId);
    }
}
```

### 7.2 跨模块导航

#### 7.2.1 事件聚合器通信
```csharp
// 发布药材选择事件
_eventAggregator.GetEvent<HerbSelectedEvent>()
    .Publish(new HerbSelectedEventArgs { HerbId = selectedHerb.Id });

// 订阅药材变更事件
_eventAggregator.GetEvent<HerbUpdatedEvent>()
    .Subscribe(OnHerbUpdated, ThreadOption.UIThread);
```

#### 7.2.2 全局导航服务
```csharp
// 通过全局导航服务跳转
await _navigationService.NavigateToAsync("Prescriptions", 
    new { SelectedHerbs = selectedHerbIds });
```

## 8. 状态管理

### 8.1 本地状态管理

#### 8.1.1 ViewModel状态
- **IsLoading**: 加载状态，控制进度指示器显示
- **IsReadOnly**: 只读模式状态，控制编辑功能可用性
- **SelectedItem**: 当前选中项，用于操作对象确定
- **SearchKeyword**: 搜索关键词，支持实时搜索

#### 8.1.2 数据状态
- **Items集合**: 使用ObservableCollection支持动态更新
- **分页状态**: CurrentPage、TotalPages等分页相关属性
- **过滤状态**: 搜索条件、排序条件等

### 8.2 会话状态管理

#### 8.2.1 用户会话
```csharp
// 通过SessionManager获取当前用户信息
var currentUser = _sessionManager.CurrentUser;
var hasPermission = _sessionManager.HasPermission("Herb.Edit");
```

#### 8.2.2 操作历史
- 记录用户的浏览历史
- 支持返回上一个操作页面
- 保存搜索条件和页面状态

### 8.3 全局状态同步

#### 8.3.1 状态变更通知
```csharp
// 药材信息变更后通知其他模块
_eventAggregator.GetEvent<HerbDataChangedEvent>()
    .Publish(new HerbDataChangedEventArgs 
    { 
        ChangeType = DataChangeType.Updated,
        HerbId = herb.Id 
    });
```

#### 8.3.2 缓存状态管理
- 本地缓存常用药材信息
- 支持离线浏览历史数据
- 定期同步服务器最新数据

## 9. API集成

### 9.1 HTTP客户端配置

#### 9.1.1 IHerbApi接口定义
```csharp
public interface IHerbApi
{
    Task<ApiResponse<PagedResult<HerbDto>>> GetHerbsAsync(int page, int pageSize, string? keyword = null);
    Task<ApiResponse<HerbDto>> GetHerbByIdAsync(Guid id);
    Task<ApiResponse<HerbDto>> CreateHerbAsync(HerbCreateDto dto);
    Task<ApiResponse<HerbDto>> UpdateHerbAsync(Guid id, HerbUpdateDto dto);
    Task<ApiResponse> DeleteHerbAsync(Guid id);
}
```

#### 9.1.2 请求响应模式
- **请求**: 使用强类型DTO进行参数传递
- **响应**: 统一ApiResponse<T>格式，包含状态和数据
- **异常**: HTTP异常通过IExceptionHandler统一处理

### 9.2 数据同步策略

#### 9.2.1 实时同步
- 新增、修改、删除操作立即同步到服务器
- 操作成功后更新本地缓存和UI状态
- 操作失败时提供重试机制

#### 9.2.2 批量同步
- 支持批量导入药材数据
- 导入过程显示进度条
- 导入结果提供详细报告

### 9.3 离线支持

#### 9.3.1 缓存机制
- 缓存常用药材基础信息
- 支持离线浏览和搜索
- 网络恢复后自动同步

#### 9.3.2 冲突解决
- 检测数据版本冲突
- 提供手动合并选择
- 记录冲突解决历史

## 10. 实现状态

### 10.1 当前实现进度

#### 10.1.1 已完成功能
✅ **基础架构**
- Prism模块注册和依赖注入配置
- MVVM基础结构搭建
- 统一设计系统集成

✅ **UI界面**  
- HerbManagementView主界面布局完成
- HerbDetailView详情界面布局完成
- 响应式设计和样式应用

✅ **数据模型**
- HerbItem UI模型完整实现
- DTO转换方法完备
- 数据绑定属性齐全

✅ **服务层**
- HerbService基础CRUD实现
- 异常处理和日志记录集成
- API客户端接口定义

#### 10.1.2 简化实现状态
⚠️ **ViewModels简化**
- HerbManagementViewModel和HerbDetailViewModel当前为简化版本
- 仅包含基础构造函数和依赖注入
- 业务逻辑和命令处理待重新实现

⚠️ **功能缺失**
- 搜索和筛选逻辑未实现
- 分页功能未实现  
- 批量操作功能未实现
- 数据验证机制未完善

### 10.2 待实现功能清单

#### 10.2.1 核心功能
🔄 **数据加载与显示**
- [ ] 实现药材列表分页加载
- [ ] 实现药材详情数据加载
- [ ] 添加加载状态指示器

🔄 **搜索与筛选**
- [ ] 实现关键词搜索功能
- [ ] 添加高级筛选条件
- [ ] 实现搜索结果高亮

🔄 **CRUD操作**
- [ ] 实现新增药材功能
- [ ] 实现编辑药材功能
- [ ] 实现删除药材功能
- [ ] 添加操作确认对话框

#### 10.2.2 增强功能
🔄 **批量操作**
- [ ] 实现Excel导入功能
- [ ] 实现数据导出功能
- [ ] 添加导入模板下载

🔄 **用户体验**
- [ ] 添加操作进度反馈
- [ ] 实现错误消息提示
- [ ] 添加操作撤销功能

🔄 **高级特性**
- [ ] 实现用药记录查询
- [ ] 添加打印功能
- [ ] 实现数据验证提示

### 10.3 架构重构影响

#### 10.3.1 重构原因
由于项目进行了整体架构重构，Herbs模块当前处于简化状态：
- 移除了复杂的业务逻辑以专注架构稳定性
- 简化了ViewModel实现以便于调试
- 保留了完整的UI界面以支持未来功能恢复

#### 10.3.2 重构后优势
- **更清晰的依赖关系**: 通过构造函数注入明确服务依赖
- **更好的可测试性**: 接口驱动的设计便于单元测试
- **更强的可维护性**: 分层架构便于功能扩展
- **更统一的设计语言**: 使用统一设计系统保证UI一致性

#### 10.3.3 恢复计划
1. **第一阶段**: 恢复基础CRUD功能（1-2周）
2. **第二阶段**: 实现搜索和分页功能（1周）  
3. **第三阶段**: 添加批量操作和高级功能（2-3周）
4. **第四阶段**: 完善用户体验和错误处理（1周）

### 10.4 技术债务与改进建议

#### 10.4.1 当前技术债务
- ViewModels业务逻辑缺失，需要重新实现
- 数据验证机制不完整，需要加强
- 错误处理和用户反馈需要改进
- 单元测试覆盖率需要提升

#### 10.4.2 改进建议
1. **优先恢复核心功能**: 专注于药材基础CRUD操作
2. **逐步添加增强功能**: 采用迭代方式逐步完善
3. **加强测试覆盖**: 为每个功能点添加单元测试
4. **完善文档**: 及时更新设计文档和API文档

---

## 总结

客户端Herbs模块采用了成熟的MVVM架构和Prism模块化设计，具备良好的扩展性和可维护性。虽然当前处于架构重构后的简化状态，但基础设施完备，UI设计完整，为后续功能恢复和扩展奠定了坚实基础。

模块严格遵循项目的统一设计标准，通过依赖注入实现了良好的解耦，通过事件聚合器支持跨模块通信，是整个桌面应用的重要组成部分。随着业务逻辑的逐步恢复，该模块将为用户提供完整、高效的中药材管理功能。