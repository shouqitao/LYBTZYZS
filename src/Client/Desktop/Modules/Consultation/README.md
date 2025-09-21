# LYBT.Desktop.Module.Consultation

> **看诊管理客户端模块** - WPF桌面应用看诊管理功能
> 四诊记录 | 辨证论治 | 诊断管理 | 医嘱记录
> 模块状态: ✅ **生产就绪** | 🎆 **分层架构完成** | **编译通过** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Desktop.Module.Consultation是WPF桌面客户端的看诊管理模块，采用MVVM架构和双层服务设计。记录中医四诊（望闻问切）信息，支持辨证论治和诊断记录。

**技术栈**: WPF (.NET 8) + Prism.DryIoc + Material Design + Refit
**架构模式**: MVVM + 分层架构（QueryService + BusinessService）
**通信方式**: 通过Refit调用后端Web API，类型安全的HTTP客户端

## 🎆 分层架构实现

### 前端服务架构
```
ConsultationModule (主模块 - 纯委托模式)
    │
    ├── ConsultationQueryService (查询专业化层)
    │   ├── 数据查询和搜索
    │   ├── 分页和筛选
    │   └── 统计和分析
    │
    └── ConsultationBusinessService (业务逻辑层)
        ├── CRUD操作
        ├── 业务规则验证
        └── 状态管理
```

## 📦 模块结构

```
LYBT.Desktop.Module.Consultation/
├── 📁 ViewModels/              # MVVM视图模型
│   ├── ConsultationViewModel.cs     # 主视图模型
│   ├── ConsultationEditViewModel.cs # 编辑视图模型
│   └── ConsultationListViewModel.cs # 列表视图模型
│
├── 📁 Views/                   # WPF视图
│   ├── ConsultationView.xaml        # 主视图
│   ├── ConsultationView.xaml      # 看诊主界面
│   ├── FourDiagnosesView.xaml      # 四诊录入
│   ├── DiagnosisView.xaml      # 诊断界面
│   ├── MedicalAdviceView.xaml      # 医嘱管理
│   └── Dialogs/                # 对话框视图
│
├── 📁 Services/                # 服务层
│   ├── ConsultationService.cs       # 主服务（纯委托）
│   ├── ConsultationQueryService.cs  # 查询服务
│   └── ConsultationBusinessService.cs # 业务服务
│
├── 📁 Models/                  # 本地模型
│   └── ConsultationModel.cs         # 客户端模型
│
└── ConsultationModule.cs            # 模块注册类
```

## 🎯 核心功能

### 1. 视图模型（MVVM）
```csharp
public class ConsultationViewModel : RegionViewModelBase
{
    private readonly IConsultationService _consultationService;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public ConsultationViewModel(
        IConsultationService consultationService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        _consultationService = consultationService;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        InitializeCommands();
        LoadData();
    }

    // 数据绑定属性
    public ObservableCollection<ConsultationDto> Items { get; set; }
    public ConsultationDto SelectedItem { get; set; }

    // 命令
    public DelegateCommand AddCommand { get; private set; }
    public DelegateCommand<ConsultationDto> EditCommand { get; private set; }
    public DelegateCommand<ConsultationDto> DeleteCommand { get; private set; }
    public DelegateCommand RefreshCommand { get; private set; }
}
```

### 2. 服务层（）
```csharp
// 主服务 - 纯委托模式
public class ConsultationService : IConsultationService
{
    private readonly IConsultationQueryService _queryService;
    private readonly IConsultationBusinessService _businessService;

    public ConsultationService(
        IConsultationQueryService queryService,
        IConsultationBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }

    // 查询操作委托到QueryService
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(ConsultationSearchDto query)
        => await _queryService.GetPagedAsync(query);

    // 业务操作委托到BusinessService
    public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto)
        => await _businessService.CreateAsync(dto);
}
```

### 3. API调用（Refit）
```csharp
// 使用Refit定义API接口
public interface IConsultationApi
{
    [Get("/api/v1/consultations")]
    Task<ApiResponse<PagedResult<ConsultationDto>>> GetPagedAsync([Query] ConsultationSearchDto query);

    [Post("/api/v1/consultations")]
    Task<ApiResponse<ConsultationDto>> CreateAsync([Body] ConsultationCreateDto dto);

    [Put("/api/v1/consultations/{id}")]
    Task<ApiResponse<ConsultationDto>> UpdateAsync(Guid id, [Body] ConsultationUpdateDto dto);

    [Delete("/api/v1/consultations/{id}")]
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
public class ConsultationModule : IModule
{
    private readonly IRegionManager _regionManager;

    public ConsultationModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册视图到区域
        _regionManager.RegisterViewWithRegion("MainRegion", typeof(ConsultationView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.Register<IConsultationService, ConsultationService>();
        containerRegistry.Register<IConsultationQueryService, ConsultationQueryService>();
        containerRegistry.Register<IConsultationBusinessService, ConsultationBusinessService>();

        // 注册视图
        containerRegistry.RegisterForNavigation<ConsultationView, ConsultationViewModel>();
        containerRegistry.RegisterDialog<ConsultationEditDialog, ConsultationEditDialogViewModel>();

        // 注册API客户端
        containerRegistry.RegisterSingleton<IConsultationApi>(() =>
            RestService.For<IConsultationApi>(containerProvider.Resolve<HttpClient>()));
    }
}
```

## 📊 状态管理

### 使用Prism事件聚合器
```csharp
// 发布事件
_eventAggregator.GetEvent<ConsultationUpdatedEvent>()
    .Publish(new ConsultationUpdatedEventArgs { Item = updatedItem });

// 订阅事件
_eventAggregator.GetEvent<ConsultationUpdatedEvent>()
    .Subscribe(OnItemUpdated, ThreadOption.UIThread);
```

## 🔒 错误处理

```csharp
public async Task LoadDataAsync()
{
    try
    {
        ShowLoading();
        var result = await _consultationService.GetPagedAsync(new ConsultationSearchDto());

        if (result.IsSuccess)
        {
            Items = new ObservableCollection<ConsultationDto>(result.Data.Items);
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
> 🎆 **生产就绪**: 完整的看诊管理功能，优秀的用户体验
