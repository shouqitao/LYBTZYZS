# LYBT.Desktop.Module.Prescriptions

> **处方管理客户端模块** - WPF桌面应用处方管理功能
> 处方开具 | 药材配伍 | 剂量计算 | 处方复制
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Desktop.Module.Prescriptions是WPF桌面客户端的处方管理模块，采用MVVM架构和双层服务设计。提供中医处方开具界面，支持药材选择、剂量计算和处方管理。

**技术栈**: WPF (.NET 8) + Prism.DryIoc + Material Design + Refit
**架构模式**: MVVM + 分层架构（QueryService + BusinessService）
**通信方式**: 通过Refit调用后端Web API，类型安全的HTTP客户端

## 🎆 分层架构实现

### 前端服务架构
```
PrescriptionsModule (主模块 - 纯委托模式)
    │
    ├── PrescriptionsQueryService (查询专业化层)
    │   ├── 数据查询和搜索
    │   ├── 分页和筛选
    │   └── 统计和分析
    │
    └── PrescriptionsBusinessService (业务逻辑层)
        ├── CRUD操作
        ├── 业务规则验证
        └── 状态管理
```

## 📦 模块结构

```
LYBT.Desktop.Module.Prescriptions/
├── 📁 ViewModels/              # MVVM视图模型
│   ├── PrescriptionsViewModel.cs     # 主视图模型
│   ├── PrescriptionsEditViewModel.cs # 编辑视图模型
│   └── PrescriptionsListViewModel.cs # 列表视图模型
│
├── 📁 Views/                   # WPF视图
│   ├── PrescriptionsView.xaml        # 主视图
│   ├── PrescriptionEditView.xaml      # 处方编辑
│   ├── HerbSelectionView.xaml      # 药材选择
│   ├── DosageCalculatorView.xaml      # 剂量计算
│   ├── PrescriptionHistoryView.xaml      # 处方历史
│   └── Dialogs/                # 对话框视图
│
├── 📁 Services/                # 服务层
│   ├── PrescriptionsService.cs       # 主服务（纯委托）
│   ├── PrescriptionsQueryService.cs  # 查询服务
│   └── PrescriptionsBusinessService.cs # 业务服务
│
├── 📁 Models/                  # 本地模型
│   └── PrescriptionsModel.cs         # 客户端模型
│
└── PrescriptionsModule.cs            # 模块注册类
```

## 🎯 核心功能

### 1. 视图模型（MVVM）
```csharp
public class PrescriptionsViewModel : RegionViewModelBase
{
    private readonly IPrescriptionsService _prescriptionsService;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public PrescriptionsViewModel(
        IPrescriptionsService prescriptionsService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        _prescriptionsService = prescriptionsService;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        InitializeCommands();
        LoadData();
    }

    // 数据绑定属性
    public ObservableCollection<PrescriptionsDto> Items { get; set; }
    public PrescriptionsDto SelectedItem { get; set; }

    // 命令
    public DelegateCommand AddCommand { get; private set; }
    public DelegateCommand<PrescriptionsDto> EditCommand { get; private set; }
    public DelegateCommand<PrescriptionsDto> DeleteCommand { get; private set; }
    public DelegateCommand RefreshCommand { get; private set; }
}
```

### 2. 服务层（）
```csharp
// 主服务 - 纯委托模式
public class PrescriptionsService : IPrescriptionsService
{
    private readonly IPrescriptionsQueryService _queryService;
    private readonly IPrescriptionsBusinessService _businessService;

    public PrescriptionsService(
        IPrescriptionsQueryService queryService,
        IPrescriptionsBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }

    // 查询操作委托到QueryService
    public async Task<ServiceResult<PagedResult<PrescriptionsDto>>> GetPagedAsync(PrescriptionsSearchDto query)
        => await _queryService.GetPagedAsync(query);

    // 业务操作委托到BusinessService
    public async Task<ServiceResult<PrescriptionsDto>> CreateAsync(PrescriptionsCreateDto dto)
        => await _businessService.CreateAsync(dto);
}
```

### 3. API调用（Refit）
```csharp
// 使用Refit定义API接口
public interface IPrescriptionsApi
{
    [Get("/api/v1/prescriptionss")]
    Task<ApiResponse<PagedResult<PrescriptionsDto>>> GetPagedAsync([Query] PrescriptionsSearchDto query);

    [Post("/api/v1/prescriptionss")]
    Task<ApiResponse<PrescriptionsDto>> CreateAsync([Body] PrescriptionsCreateDto dto);

    [Put("/api/v1/prescriptionss/{id}")]
    Task<ApiResponse<PrescriptionsDto>> UpdateAsync(Guid id, [Body] PrescriptionsUpdateDto dto);

    [Delete("/api/v1/prescriptionss/{id}")]
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

### 1. 智能组方
- 药材智能推荐
- 配伍禁忌提示
- 剂量自动计算

### 2. 处方模板
- 常用处方保存
- 快速套用模板
- 个性化调整

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
public class PrescriptionsModule : IModule
{
    private readonly IRegionManager _regionManager;

    public PrescriptionsModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册视图到区域
        _regionManager.RegisterViewWithRegion("MainRegion", typeof(PrescriptionsView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.Register<IPrescriptionsService, PrescriptionsService>();
        containerRegistry.Register<IPrescriptionsQueryService, PrescriptionsQueryService>();
        containerRegistry.Register<IPrescriptionsBusinessService, PrescriptionsBusinessService>();

        // 注册视图
        containerRegistry.RegisterForNavigation<PrescriptionsView, PrescriptionsViewModel>();
        containerRegistry.RegisterDialog<PrescriptionsEditDialog, PrescriptionsEditDialogViewModel>();

        // 注册API客户端
        containerRegistry.RegisterSingleton<IPrescriptionsApi>(() =>
            RestService.For<IPrescriptionsApi>(containerProvider.Resolve<HttpClient>()));
    }
}
```

## 📊 状态管理

### 使用Prism事件聚合器
```csharp
// 发布事件
_eventAggregator.GetEvent<PrescriptionsUpdatedEvent>()
    .Publish(new PrescriptionsUpdatedEventArgs { Item = updatedItem });

// 订阅事件
_eventAggregator.GetEvent<PrescriptionsUpdatedEvent>()
    .Subscribe(OnItemUpdated, ThreadOption.UIThread);
```

## 🔒 错误处理

```csharp
public async Task LoadDataAsync()
{
    try
    {
        ShowLoading();
        var result = await _prescriptionsService.GetPagedAsync(new PrescriptionsSearchDto());

        if (result.IsSuccess)
        {
            Items = new ObservableCollection<PrescriptionsDto>(result.Data.Items);
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
> 🎆 **生产就绪**: 完整的处方管理功能，优秀的用户体验

## 🎯 项目概述
- [待补充] 简要描述 Prescriptions 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

## 🔌 API 接口
- [待补充] 集成的 API/Refit 客户端：例如 IPrescriptionsApi
- [待补充] 关键调用路径与鉴权方式（JWT Bearer）

## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- [待补充] 本模块相关的设计/实现文档链接
