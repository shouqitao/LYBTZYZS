# LYBT.Desktop.Module.Patients

> **患者管理客户端模块** - WPF桌面应用患者管理功能
> 患者档案 | 就诊历史 | 快速检索 | 信息维护
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Desktop.Module.Patients是WPF桌面客户端的患者管理模块，采用MVVM架构和双层服务设计。管理患者基本信息、就诊记录和健康档案，支持快速搜索和信息维护。

**技术栈**: WPF (.NET 8) + Prism.DryIoc + Material Design + Refit
**架构模式**: MVVM + 分层架构（QueryService + BusinessService）
**通信方式**: 通过Refit调用后端Web API，类型安全的HTTP客户端

## 🎆 分层架构实现

### 前端服务架构
```
PatientsModule (主模块 - 纯委托模式)
    │
    ├── PatientsQueryService (查询专业化层)
    │   ├── 数据查询和搜索
    │   ├── 分页和筛选
    │   └── 统计和分析
    │
    └── PatientsBusinessService (业务逻辑层)
        ├── CRUD操作
        ├── 业务规则验证
        └── 状态管理
```

## 📦 模块结构

```
LYBT.Desktop.Module.Patients/
├── 📁 ViewModels/              # MVVM视图模型
│   ├── PatientsViewModel.cs     # 主视图模型
│   ├── PatientsEditViewModel.cs # 编辑视图模型
│   └── PatientsListViewModel.cs # 列表视图模型
│
├── 📁 Views/                   # WPF视图
│   ├── PatientsView.xaml        # 主视图
│   ├── PatientListView.xaml      # 患者列表
│   ├── PatientEditView.xaml      # 患者编辑
│   ├── PatientDetailView.xaml      # 患者详情
│   ├── PatientSearchView.xaml      # 患者搜索
│   └── Dialogs/                # 对话框视图
│
├── 📁 Services/                # 服务层
│   ├── PatientsService.cs       # 主服务（纯委托）
│   ├── PatientsQueryService.cs  # 查询服务
│   └── PatientsBusinessService.cs # 业务服务
│
├── 📁 Models/                  # 本地模型
│   └── PatientsModel.cs         # 客户端模型
│
└── PatientsModule.cs            # 模块注册类
```

## 🎯 核心功能

### 1. 视图模型（MVVM）
```csharp
public class PatientsViewModel : RegionViewModelBase
{
    private readonly IPatientsService _patientsService;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public PatientsViewModel(
        IPatientsService patientsService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        _patientsService = patientsService;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        InitializeCommands();
        LoadData();
    }

    // 数据绑定属性
    public ObservableCollection<PatientsDto> Items { get; set; }
    public PatientsDto SelectedItem { get; set; }

    // 命令
    public DelegateCommand AddCommand { get; private set; }
    public DelegateCommand<PatientsDto> EditCommand { get; private set; }
    public DelegateCommand<PatientsDto> DeleteCommand { get; private set; }
    public DelegateCommand RefreshCommand { get; private set; }
}
```

### 2. 服务层（）
```csharp
// 主服务 - 纯委托模式
public class PatientsService : IPatientsService
{
    private readonly IPatientsQueryService _queryService;
    private readonly IPatientsBusinessService _businessService;

    public PatientsService(
        IPatientsQueryService queryService,
        IPatientsBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }

    // 查询操作委托到QueryService
    public async Task<ServiceResult<PagedResult<PatientsDto>>> GetPagedAsync(PatientsSearchDto query)
        => await _queryService.GetPagedAsync(query);

    // 业务操作委托到BusinessService
    public async Task<ServiceResult<PatientsDto>> CreateAsync(PatientsCreateDto dto)
        => await _businessService.CreateAsync(dto);
}
```

### 3. API调用（Refit）
```csharp
// 使用Refit定义API接口
public interface IPatientsApi
{
    [Get("/api/v1/patientss")]
    Task<ApiResponse<PagedResult<PatientsDto>>> GetPagedAsync([Query] PatientsSearchDto query);

    [Post("/api/v1/patientss")]
    Task<ApiResponse<PatientsDto>> CreateAsync([Body] PatientsCreateDto dto);

    [Put("/api/v1/patientss/{id}")]
    Task<ApiResponse<PatientsDto>> UpdateAsync(Guid id, [Body] PatientsUpdateDto dto);

    [Delete("/api/v1/patientss/{id}")]
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

### 1. 快速搜索
- 支持拼音首字母搜索
- 模糊匹配患者姓名
- 历史记录快速访问

### 2. 患者画像
- 就诊历史统计
- 用药偏好分析
- 健康趋势图表

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
public class PatientsModule : IModule
{
    private readonly IRegionManager _regionManager;

    public PatientsModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册视图到区域
        _regionManager.RegisterViewWithRegion("MainRegion", typeof(PatientsView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.Register<IPatientsService, PatientsService>();
        containerRegistry.Register<IPatientsQueryService, PatientsQueryService>();
        containerRegistry.Register<IPatientsBusinessService, PatientsBusinessService>();

        // 注册视图
        containerRegistry.RegisterForNavigation<PatientsView, PatientsViewModel>();
        containerRegistry.RegisterDialog<PatientsEditDialog, PatientsEditDialogViewModel>();

        // 注册API客户端
        containerRegistry.RegisterSingleton<IPatientsApi>(() =>
            RestService.For<IPatientsApi>(containerProvider.Resolve<HttpClient>()));
    }
}
```

## 📊 状态管理

### 使用Prism事件聚合器
```csharp
// 发布事件
_eventAggregator.GetEvent<PatientsUpdatedEvent>()
    .Publish(new PatientsUpdatedEventArgs { Item = updatedItem });

// 订阅事件
_eventAggregator.GetEvent<PatientsUpdatedEvent>()
    .Subscribe(OnItemUpdated, ThreadOption.UIThread);
```

## 🔒 错误处理

```csharp
public async Task LoadDataAsync()
{
    try
    {
        ShowLoading();
        var result = await _patientsService.GetPagedAsync(new PatientsSearchDto());

        if (result.IsSuccess)
        {
            Items = new ObservableCollection<PatientsDto>(result.Data.Items);
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
> 🎆 **生产就绪**: 完整的患者管理功能，优秀的用户体验

## 🎯 项目概述
- [待补充] 简要描述 Patients 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

## 🔌 API 接口
- [待补充] 集成的 API/Refit 客户端：例如 IPatientsApi
- [待补充] 关键调用路径与鉴权方式（JWT Bearer）

## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- [待补充] 本模块相关的设计/实现文档链接
