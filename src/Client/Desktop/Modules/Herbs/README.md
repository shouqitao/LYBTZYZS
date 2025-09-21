# LYBT.Desktop.Module.Herbs

> **药材管理客户端模块** - WPF桌面应用药材管理功能
> 药材检索 | 价格维护 | 拼音搜索 | 批量管理
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Desktop.Module.Herbs是WPF桌面客户端的药材管理模块，采用MVVM架构和双层服务设计。管理中药材信息，提供药材检索、价格维护等功能。

**技术栈**: WPF (.NET 8) + Prism.DryIoc + Material Design + Refit
**架构模式**: MVVM + 分层架构（QueryService + BusinessService）
**通信方式**: 通过Refit调用后端Web API，类型安全的HTTP客户端

## 🎆 分层架构实现

### 前端服务架构
```
HerbsModule (主模块 - 纯委托模式)
    │
    ├── HerbsQueryService (查询专业化层)
    │   ├── 数据查询和搜索
    │   ├── 分页和筛选
    │   └── 统计和分析
    │
    └── HerbsBusinessService (业务逻辑层)
        ├── CRUD操作
        ├── 业务规则验证
        └── 状态管理
```

## 📦 模块结构

```
LYBT.Desktop.Module.Herbs/
├── 📁 ViewModels/              # MVVM视图模型
│   ├── HerbsViewModel.cs     # 主视图模型
│   ├── HerbsEditViewModel.cs # 编辑视图模型
│   └── HerbsListViewModel.cs # 列表视图模型
│
├── 📁 Views/                   # WPF视图
│   ├── HerbsView.xaml        # 主视图
│   ├── HerbListView.xaml      # 药材列表
│   ├── HerbEditView.xaml      # 药材编辑
│   ├── HerbSearchView.xaml      # 药材搜索
│   ├── PriceManagementView.xaml      # 价格管理
│   └── Dialogs/                # 对话框视图
│
├── 📁 Services/                # 服务层
│   ├── HerbsService.cs       # 主服务（纯委托）
│   ├── HerbsQueryService.cs  # 查询服务
│   └── HerbsBusinessService.cs # 业务服务
│
├── 📁 Models/                  # 本地模型
│   └── HerbsModel.cs         # 客户端模型
│
└── HerbsModule.cs            # 模块注册类
```

## 🎯 核心功能

### 1. 视图模型（MVVM）
```csharp
public class HerbsViewModel : RegionViewModelBase
{
    private readonly IHerbsService _herbsService;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public HerbsViewModel(
        IHerbsService herbsService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        _herbsService = herbsService;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        InitializeCommands();
        LoadData();
    }

    // 数据绑定属性
    public ObservableCollection<HerbsDto> Items { get; set; }
    public HerbsDto SelectedItem { get; set; }

    // 命令
    public DelegateCommand AddCommand { get; private set; }
    public DelegateCommand<HerbsDto> EditCommand { get; private set; }
    public DelegateCommand<HerbsDto> DeleteCommand { get; private set; }
    public DelegateCommand RefreshCommand { get; private set; }
}
```

### 2. 服务层（）
```csharp
// 主服务 - 纯委托模式
public class HerbsService : IHerbsService
{
    private readonly IHerbsQueryService _queryService;
    private readonly IHerbsBusinessService _businessService;

    public HerbsService(
        IHerbsQueryService queryService,
        IHerbsBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }

    // 查询操作委托到QueryService
    public async Task<ServiceResult<PagedResult<HerbsDto>>> GetPagedAsync(HerbsSearchDto query)
        => await _queryService.GetPagedAsync(query);

    // 业务操作委托到BusinessService
    public async Task<ServiceResult<HerbsDto>> CreateAsync(HerbsCreateDto dto)
        => await _businessService.CreateAsync(dto);
}
```

### 3. API调用（Refit）
```csharp
// 使用Refit定义API接口
public interface IHerbsApi
{
    [Get("/api/v1/herbss")]
    Task<ApiResponse<PagedResult<HerbsDto>>> GetPagedAsync([Query] HerbsSearchDto query);

    [Post("/api/v1/herbss")]
    Task<ApiResponse<HerbsDto>> CreateAsync([Body] HerbsCreateDto dto);

    [Put("/api/v1/herbss/{id}")]
    Task<ApiResponse<HerbsDto>> UpdateAsync(Guid id, [Body] HerbsUpdateDto dto);

    [Delete("/api/v1/herbss/{id}")]
    Task<ApiResponse<bool>> DeleteAsync(Guid id);
}
```

## 🎨 UI设计

### Material Design主题
- 使用Material Design in XAML Toolkit
- 支持明暗主题切换
- 响应式布局设计
- 动画和过渡效果

### 数据绑定示例
```xml
<DataGrid ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedItem}"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="名称"
                           Binding="{Binding Name}"
                           Width="200"/>
        <DataGridTextColumn Header="状态"
                           Binding="{Binding Status}"
                           Width="100"/>
        <DataGridTemplateColumn Header="操作" Width="150">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Button Command="{Binding DataContext.EditCommand,
                                         RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                CommandParameter="{Binding}"
                                Content="编辑"/>
                        <Button Command="{Binding DataContext.DeleteCommand,
                                         RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                CommandParameter="{Binding}"
                                Content="删除"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
    </DataGrid.Columns>
</DataGrid>
```

## 🔧 特色功能

### 1. 数据缓存
- 本地数据缓存
- 离线模式支持
- 数据同步机制

### 2. 批量操作
- 批量导入导出
- 批量状态更新
- 批量数据验证

## 📱 响应式设计

### 自适应布局
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>  <!-- 工具栏 -->
        <RowDefinition Height="*"/>     <!-- 内容区 -->
        <RowDefinition Height="Auto"/>  <!-- 状态栏 -->
    </Grid.RowDefinitions>

    <!-- 响应式内容区 -->
    <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
        <ContentControl prism:RegionManager.RegionName="ContentRegion"/>
    </ScrollViewer>
</Grid>
```

## 🚀 模块注册

```csharp
public class HerbsModule : IModule
{
    private readonly IRegionManager _regionManager;

    public HerbsModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册视图到区域
        _regionManager.RegisterViewWithRegion("MainRegion", typeof(HerbsView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.Register<IHerbsService, HerbsService>();
        containerRegistry.Register<IHerbsQueryService, HerbsQueryService>();
        containerRegistry.Register<IHerbsBusinessService, HerbsBusinessService>();

        // 注册视图
        containerRegistry.RegisterForNavigation<HerbsView, HerbsViewModel>();
        containerRegistry.RegisterDialog<HerbsEditDialog, HerbsEditDialogViewModel>();

        // 注册API客户端
        containerRegistry.RegisterSingleton<IHerbsApi>(() =>
            RestService.For<IHerbsApi>(containerProvider.Resolve<HttpClient>()));
    }
}
```

## 📊 状态管理

### 使用Prism事件聚合器
```csharp
// 发布事件
_eventAggregator.GetEvent<HerbsUpdatedEvent>()
    .Publish(new HerbsUpdatedEventArgs { Item = updatedItem });

// 订阅事件
_eventAggregator.GetEvent<HerbsUpdatedEvent>()
    .Subscribe(OnItemUpdated, ThreadOption.UIThread);
```

## 🔒 错误处理

```csharp
public async Task LoadDataAsync()
{
    try
    {
        ShowLoading();
        var result = await _herbsService.GetPagedAsync(new HerbsSearchDto());

        if (result.IsSuccess)
        {
            Items = new ObservableCollection<HerbsDto>(result.Data.Items);
        }
        else
        {
            ShowError(result.ErrorMessage);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载数据失败");
        ShowError("加载数据失败，请重试");
    }
    finally
    {
        HideLoading();
    }
}
```

## 📚 相关依赖

- **Prism.DryIoc** - MVVM框架和依赖注入（DI）
- **Material Design** - UI组件库
- **Refit** - REST API客户端
- **AutoMapper** - 对象映射
- **FluentValidation** - 数据验证

## 🎯 最佳实践

1. **MVVM模式**: 严格遵循MVVM模式，视图与逻辑分离
2. **异步编程**: 所有API调用使用async/await
3. **错误处理**: 统一的错误处理和用户提示
4. **数据验证**: 客户端和服务端双重验证
5. **性能优化**: 虚拟化列表、延迟加载、数据缓存

---

> 📌 **最新成果**: 分层架构在客户端完整实现，MVVM模式规范应用
> 🎆 **生产就绪**: 完整的药材管理功能，优秀的用户体验

## 🎯 项目概述
- [待补充] 简要描述 Herbs 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。
