# LYBT.Desktop.Module.MedicalCase

> **医案管理客户端模块** - WPF桌面应用医案管理功能
> 病历管理 | 诊疗记录 | 处方关联 | 档案归档
> **模块状态**: ✅ **生产就绪** | 🎆 **UltraThink架构完成** | **零编译错误** | **2025-09-20更新**

## 🎯 模块概述

LYBT.Desktop.Module.MedicalCase是WPF桌面客户端的医案管理模块，采用MVVM架构和UltraThink双层服务设计。作为诊疗流程的容器，管理完整的医案信息，包括诊断、处方等。

**技术栈**: WPF (.NET 8) + Prism.DryIoc + Material Design + Refit
**架构模式**: MVVM + UltraThink双层架构（QueryService + BusinessService）
**通信方式**: 通过Refit调用后端Web API，类型安全的HTTP客户端

## 🎆 UltraThink双层架构实现

### 前端服务架构
```
MedicalCaseModule (主模块 - 纯委托模式)
    │
    ├── MedicalCaseQueryService (查询专业化层)
    │   ├── 数据查询和搜索
    │   ├── 分页和筛选
    │   └── 统计和分析
    │
    └── MedicalCaseBusinessService (业务逻辑层)
        ├── CRUD操作
        ├── 业务规则验证
        └── 状态管理
```

## 📦 模块结构

```
LYBT.Desktop.Module.MedicalCase/
├── 📁 ViewModels/              # MVVM视图模型
│   ├── MedicalCaseViewModel.cs     # 主视图模型
│   ├── MedicalCaseEditViewModel.cs # 编辑视图模型
│   └── MedicalCaseListViewModel.cs # 列表视图模型
│
├── 📁 Views/                   # WPF视图
│   ├── MedicalCaseView.xaml        # 主视图
│   ├── MedicalCaseListView.xaml      # 医案列表
│   ├── MedicalCaseEditView.xaml      # 医案编辑
│   ├── MedicalCaseDetailView.xaml      # 医案详情
│   ├── MedicalCaseHistoryView.xaml      # 历史记录
│   └── Dialogs/                # 对话框视图
│
├── 📁 Services/                # 服务层
│   ├── MedicalCaseService.cs       # 主服务（纯委托）
│   ├── MedicalCaseQueryService.cs  # 查询服务
│   └── MedicalCaseBusinessService.cs # 业务服务
│
├── 📁 Models/                  # 本地模型
│   └── MedicalCaseModel.cs         # 客户端模型
│
└── MedicalCaseModule.cs            # 模块注册类
```

## 🎯 核心功能

### 1. 视图模型（MVVM）
```csharp
public class MedicalCaseViewModel : RegionViewModelBase
{
    private readonly IMedicalCaseService _medicalcaseService;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;

    public MedicalCaseViewModel(
        IMedicalCaseService medicalcaseService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator)
    {
        _medicalcaseService = medicalcaseService;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;

        InitializeCommands();
        LoadData();
    }

    // 数据绑定属性
    public ObservableCollection<MedicalCaseDto> Items { get; set; }
    public MedicalCaseDto SelectedItem { get; set; }

    // 命令
    public DelegateCommand AddCommand { get; private set; }
    public DelegateCommand<MedicalCaseDto> EditCommand { get; private set; }
    public DelegateCommand<MedicalCaseDto> DeleteCommand { get; private set; }
    public DelegateCommand RefreshCommand { get; private set; }
}
```

### 2. 服务层（UltraThink）
```csharp
// 主服务 - 纯委托模式
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseQueryService _queryService;
    private readonly IMedicalCaseBusinessService _businessService;

    public MedicalCaseService(
        IMedicalCaseQueryService queryService,
        IMedicalCaseBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }

    // 查询操作委托到QueryService
    public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(MedicalCaseSearchDto query)
        => await _queryService.GetPagedAsync(query);

    // 业务操作委托到BusinessService
    public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        => await _businessService.CreateAsync(dto);
}
```

### 3. API调用（Refit）
```csharp
// 使用Refit定义API接口
public interface IMedicalCaseApi
{
    [Get("/api/v1/medicalcases")]
    Task<ApiResponse<PagedResult<MedicalCaseDto>>> GetPagedAsync([Query] MedicalCaseSearchDto query);

    [Post("/api/v1/medicalcases")]
    Task<ApiResponse<MedicalCaseDto>> CreateAsync([Body] MedicalCaseCreateDto dto);

    [Put("/api/v1/medicalcases/{id}")]
    Task<ApiResponse<MedicalCaseDto>> UpdateAsync(Guid id, [Body] MedicalCaseUpdateDto dto);

    [Delete("/api/v1/medicalcases/{id}")]
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
public class MedicalCaseModule : IModule
{
    private readonly IRegionManager _regionManager;

    public MedicalCaseModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 注册视图到区域
        _regionManager.RegisterViewWithRegion("MainRegion", typeof(MedicalCaseView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.Register<IMedicalCaseService, MedicalCaseService>();
        containerRegistry.Register<IMedicalCaseQueryService, MedicalCaseQueryService>();
        containerRegistry.Register<IMedicalCaseBusinessService, MedicalCaseBusinessService>();

        // 注册视图
        containerRegistry.RegisterForNavigation<MedicalCaseView, MedicalCaseViewModel>();
        containerRegistry.RegisterDialog<MedicalCaseEditDialog, MedicalCaseEditDialogViewModel>();

        // 注册API客户端
        containerRegistry.RegisterSingleton<IMedicalCaseApi>(() =>
            RestService.For<IMedicalCaseApi>(containerProvider.Resolve<HttpClient>()));
    }
}
```

## 📊 状态管理

### 使用Prism事件聚合器
```csharp
// 发布事件
_eventAggregator.GetEvent<MedicalCaseUpdatedEvent>()
    .Publish(new MedicalCaseUpdatedEventArgs { Item = updatedItem });

// 订阅事件
_eventAggregator.GetEvent<MedicalCaseUpdatedEvent>()
    .Subscribe(OnItemUpdated, ThreadOption.UIThread);
```

## 🔒 错误处理

```csharp
public async Task LoadDataAsync()
{
    try
    {
        ShowLoading();
        var result = await _medicalcaseService.GetPagedAsync(new MedicalCaseSearchDto());

        if (result.IsSuccess)
        {
            Items = new ObservableCollection<MedicalCaseDto>(result.Data.Items);
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

- **Prism.DryIoc** - MVVM框架和依赖注入
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

> 📌 **最新成果**: UltraThink架构在客户端完整实现，MVVM模式规范应用
> 🎆 **生产就绪**: 完整的医案管理功能，优秀的用户体验
