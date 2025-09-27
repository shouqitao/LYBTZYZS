# 客户端Prescriptions模块设计文档

## 1. 模块概述

### 1.1 功能定位
处方管理模块（Prescriptions Module）是WPF桌面客户端的核心业务模块之一，负责中医处方的开具、编辑、管理和打印功能。该模块采用MVVM设计模式，遵循Prism模块化架构，为医生提供便捷的处方管理工作台。

### 1.2 核心功能
- **处方开具**：支持新建处方，添加中药材，设置用法用量
- **处方编辑**：支持草稿保存、正式保存、处方修改
- **药材管理**：药材选择、用量设置、价格计算
- **验方导入**：从验方模板快速导入中药组合
- **处方审核**：处方验证、配伍检查、合理性校验
- **处方打印**：处方单据生成和打印功能

### 1.3 技术特点
- 基于UltraThink双层架构：Module（委托层）+ Services（业务层）
- 严格遵循MVVM模式，实现界面与业务逻辑分离
- 支持依赖注入，确保模块间松耦合
- 集成事件聚合器，实现模块间通信

## 2. 架构设计（MVVM模式）

### 2.1 整体架构图
```
┌─────────────────────────────────────────────────────────┐
│                 Prescriptions Module                    │
├─────────────────────────────────────────────────────────┤
│  Views Layer (界面层)                                    │
│  ├── PrescriptionsMainView.xaml                        │
│  ├── PrescriptionComposerView.xaml                     │
│  ├── PrescriptionManagementView.xaml                   │
│  └── Dialog Views                                      │
├─────────────────────────────────────────────────────────┤
│  ViewModels Layer (视图模型层)                           │
│  ├── PrescriptionsMainViewModel                        │
│  ├── PrescriptionComposerViewModel                     │
│  ├── PrescriptionManagementViewModel                   │
│  └── Dialog ViewModels                                 │
├─────────────────────────────────────────────────────────┤
│  Services Layer (前端服务层)                             │
│  ├── PrescriptionService (API代理)                      │
│  ├── PrescriptionComposerService (编辑器服务)            │
│  └── PrescriptionsService (业务协调)                     │
├─────────────────────────────────────────────────────────┤
│  Models Layer (模型层)                                   │
│  ├── PrescriptionItem (UI模型)                         │
│  ├── PrescriptionHerbItem (药材项模型)                   │
│  └── Constants                                         │
└─────────────────────────────────────────────────────────┘
```

### 2.2 依赖关系
- **Views** → **ViewModels**：通过DataBinding绑定
- **ViewModels** → **Services**：通过构造函数注入
- **Services** → **Shared APIs**：调用共享接口层
- **Models** → **Shared DTOs**：与共享数据传输对象转换

### 2.3 模块注册
```csharp
public class PrescriptionsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册服务
        containerRegistry.RegisterSingleton<IPrescriptionService, PrescriptionService>();
        containerRegistry.RegisterSingleton<IPrescriptionComposerService, PrescriptionComposerService>();

        // 注册视图和视图模型将在重构完成后添加
    }
}
```

## 3. ViewModels设计

### 3.1 主要ViewModels一览
| ViewModel类名 | 功能描述 | 当前状态 |
|--------------|---------|---------|
| PrescriptionsMainViewModel | 处方模块主视图模型 | 简化版（待重构） |
| PrescriptionComposerViewModel | 处方编辑器视图模型 | 功能完整 |
| PrescriptionManagementViewModel | 处方管理视图模型 | 简化版（待重构） |
| HerbSelectionDialogViewModel | 药材选择对话框模型 | 简化版（待重构） |
| FormulaTemplateDialogViewModel | 验方模板对话框模型 | 简化版（待重构） |

### 3.2 PrescriptionComposerViewModel（核心ViewModel）

#### 3.2.1 职责范围
- 处方基本信息编辑（诊断、剂数、用法、医嘱）
- 药材列表管理（添加、编辑、删除、导入验方）
- 价格计算与显示（单剂价格、总价）
- 处方保存（草稿保存、正式保存）
- 数据验证与用户交互

#### 3.2.2 核心属性
```csharp
public class PrescriptionComposerViewModel
{
    // 处方基本信息
    public PrescriptionDto CurrentPrescription { get; set; }
    public string PatientInfo { get; private set; }
    public string Diagnosis { get; set; }
    public int DosageCount { get; set; }
    public string Usage { get; set; }
    public string Advice { get; set; }

    // 药材管理
    public ObservableCollection<PrescriptionItemDto> PrescriptionItems { get; }

    // 价格信息
    public decimal SingleDosePrice { get; }
    public decimal TotalPrice { get; }

    // 命令
    public ICommand AddHerbCommand { get; }
    public ICommand ImportFormulaCommand { get; }
    public ICommand SaveDraftCommand { get; }
    public ICommand SavePrescriptionCommand { get; }
}
```

#### 3.2.3 关键业务逻辑
- **价格计算**：实时计算单剂价格和总价
- **数据验证**：处方保存前的完整性验证
- **事件通信**：通过EventAggregator与其他模块通信
- **导航支持**：实现INavigationAware接口，支持参数传递

### 3.3 事件通信机制
```csharp
// 处方保存事件
_eventAggregator.GetEvent<PrescriptionSavedEvent>()
    .Publish(new PrescriptionSavedEventArgs
    {
        PrescriptionId = _currentPrescription.Id,
        Prescription = _currentPrescription,
        IsNew = true
    });

// 处方编辑器关闭事件
_eventAggregator.GetEvent<PrescriptionComposerClosedEvent>()
    .Publish(new PrescriptionComposerClosedEventArgs());
```

## 4. Views界面设计

### 4.1 主要Views一览
| View类名 | 界面描述 | 布局特点 |
|---------|---------|---------|
| PrescriptionsMainView | 处方模块主界面 | 顶部导航 + 内容区域 |
| PrescriptionComposerView | 处方编辑器界面 | 分区布局 + DataGrid |
| PrescriptionManagementView | 处方管理界面 | 列表 + 操作面板 |
| HerbSelectionDialog | 药材选择对话框 | 搜索 + 选择列表 |
| FormulaTemplateDialog | 验方模板对话框 | 模板展示 + 导入 |

### 4.2 PrescriptionsMainView（主界面）

#### 4.2.1 界面结构
```xml
<Grid>
    <!-- 顶部导航栏 -->
    <Border Background="#2E86AB" Padding="15,10">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="🔧 处方管理" />
            <Button Command="{Binding SwitchToManagementCommand}" Content="📋 历史管理" />
            <Button Command="{Binding ReturnToSourceCommand}" Content="↩️ 返回诊疗" />
        </StackPanel>
    </Border>

    <!-- 主内容区 -->
    <ContentControl Content="{Binding CurrentWorkflowContent}" />

    <!-- 引导界面（无医疗案例时） -->
    <Border Visibility="{Binding HasMedicalCase, Converter=Inverse}">
        <StackPanel>
            <TextBlock Text="📋 处方模块" />
            <Button Command="{Binding CreateNewPrescriptionCommand}" Content="➕ 新建处方" />
        </StackPanel>
    </Border>
</Grid>
```

#### 4.2.2 设计特点
- **响应式布局**：根据是否有医疗案例显示不同内容
- **状态指示**：顶部显示当前医疗案例信息
- **快速导航**：提供返回诊疗、历史管理等快捷操作

### 4.3 PrescriptionComposerView（编辑器界面）

#### 4.3.1 界面分区
```xml
<Grid>
    <!-- 1. 标题栏：显示患者信息 -->
    <Border Background="#34495E">
        <TextBlock Text="处方开具" />
        <TextBlock Text="{Binding PatientInfo}" />
    </Border>

    <!-- 2. 基本信息区：诊断、剂数、用法、价格 -->
    <Border Background="#F8F9FA">
        <Grid.ColumnDefinitions>
            <ColumnDefinition /> <!-- 诊断 -->
            <ColumnDefinition /> <!-- 剂数 -->
            <ColumnDefinition /> <!-- 用法 -->
            <ColumnDefinition /> <!-- 价格 -->
        </Grid.ColumnDefinitions>

        <TextBox Text="{Binding Diagnosis}" />
        <TextBox Text="{Binding DosageCount}" />
        <ComboBox Text="{Binding Usage}" />
        <StackPanel>
            <TextBlock Text="{Binding SingleDosePrice, StringFormat=C}" />
            <TextBlock Text="{Binding TotalPrice, StringFormat=C}" />
        </StackPanel>
    </Border>

    <!-- 3. 药材列表区：操作按钮 + DataGrid -->
    <Border>
        <!-- 操作按钮栏 -->
        <StackPanel Orientation="Horizontal">
            <Button Command="{Binding AddHerbCommand}" Content="添加药材" />
            <Button Command="{Binding ImportFormulaCommand}" Content="导入验方" />
            <Button Command="{Binding ClearAllCommand}" Content="清空处方" />
        </StackPanel>

        <!-- 药材DataGrid -->
        <DataGrid ItemsSource="{Binding PrescriptionItems}">
            <DataGrid.Columns>
                <DataGridTextColumn Header="药材名称" Binding="{Binding HerbName}" />
                <DataGridTextColumn Header="用量" Binding="{Binding Quantity}" />
                <DataGridTextColumn Header="单价" Binding="{Binding UnitPrice}" />
                <DataGridTemplateColumn Header="操作">
                    <Button Command="{Binding DataContext.EditHerbCommand}" />
                    <Button Command="{Binding DataContext.RemoveHerbCommand}" />
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>
    </Border>

    <!-- 4. 医嘱区：多行文本输入 -->
    <Border>
        <TextBox Text="{Binding Advice}" AcceptsReturn="True" />
    </Border>

    <!-- 5. 底部操作区：保存按钮 -->
    <Border Background="#2C3E50">
        <StackPanel Orientation="Horizontal">
            <Button Command="{Binding SaveDraftCommand}" Content="保存草稿" />
            <Button Command="{Binding SavePrescriptionCommand}" Content="保存处方" />
            <Button Command="{Binding CloseCommand}" Content="关闭" />
        </StackPanel>
    </Border>
</Grid>
```

#### 4.3.2 交互特性
- **实时计算**：药材数量变化时自动计算价格
- **数据验证**：输入内容实时验证，错误时高亮显示
- **拖拽支持**：支持药材项目的拖拽排序
- **快捷操作**：双击药材项目快速编辑

### 4.4 样式系统
```xml
<UserControl.Resources>
    <!-- 标题样式 -->
    <Style x:Key="SectionHeaderStyle" TargetType="TextBlock">
        <Setter Property="FontSize" Value="16" />
        <Setter Property="FontWeight" Value="Bold" />
        <Setter Property="Foreground" Value="#2C3E50" />
    </Style>

    <!-- 输入框样式 -->
    <Style x:Key="InputStyle" TargetType="TextBox">
        <Setter Property="Height" Value="32" />
        <Setter Property="Padding" Value="8,5" />
        <Setter Property="BorderBrush" Value="#BDC3C7" />
    </Style>

    <!-- 按钮样式 -->
    <Style x:Key="ButtonStyle" TargetType="Button">
        <Setter Property="Background" Value="#3498DB" />
        <Setter Property="Foreground" Value="White" />
        <Setter Property="Padding" Value="15,0" />
    </Style>
</UserControl.Resources>
```

## 5. 前端服务层

### 5.1 服务架构
```
┌─────────────────────────────────────┐
│          前端服务层                  │
├─────────────────────────────────────┤
│  PrescriptionService                │  ← API代理服务
│  (API调用代理)                       │
├─────────────────────────────────────┤
│  PrescriptionComposerService        │  ← 业务逻辑服务
│  (处方编辑器业务逻辑)                │
├─────────────────────────────────────┤
│  PrescriptionsService               │  ← 协调服务
│  (模块协调服务)                      │
└─────────────────────────────────────┘
```

### 5.2 PrescriptionService（API代理服务）

#### 5.2.1 服务职责
- 封装对后端处方API的调用
- 统一异常处理和错误包装
- 提供标准的ServiceResult返回格式

#### 5.2.2 核心方法
```csharp
public class PrescriptionService : IPrescriptionService
{
    // 基础CRUD操作
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);
    Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);

    // 业务查询
    Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
}
```

#### 5.2.3 异常处理模式
```csharp
public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
{
    return await _exceptionHandler.HandleException<PrescriptionDto>(async () =>
    {
        var response = await _prescriptionApi.GetPrescriptionByIdAsync(id);
        return ServiceResult<PrescriptionDto>.Success(response.Content);
    }, nameof(GetByIdAsync));
}
```

### 5.3 PrescriptionComposerService（编辑器服务）

#### 5.3.1 服务职责
- 处方编辑的业务逻辑封装
- 价格计算和数据验证
- 草稿保存和正式保存逻辑

#### 5.3.2 核心功能方法
```csharp
public class PrescriptionComposerService : IPrescriptionComposerService
{
    // 处方编辑核心功能
    Task<PrescriptionDto> CreateDraftAsync(Guid medicalCaseId, Guid patientId, Guid doctorId);
    Task<(bool Success, string Message)> SaveDraftAsync(PrescriptionDto prescription);
    Task<(bool Success, string Message)> SavePrescriptionAsync(PrescriptionDto prescription);

    // 验证和计算
    ValidationResult ValidatePrescription(PrescriptionDto prescription);
    PriceCalculationResult CalculatePrice(PrescriptionDto prescription);

    // 药材管理辅助
    (bool IsValid, string Message) ValidateHerbQuantity(string herbName, decimal quantity);
    (bool Success, string Message) AddHerbToPrescription(PrescriptionDto prescription, PrescriptionItemDto herbItem);
    (bool Success, string Message) RemoveHerbFromPrescription(PrescriptionDto prescription, PrescriptionItemDto herbItem);
}
```

#### 5.3.3 验证机制
```csharp
private bool ValidatePrescription()
{
    if (string.IsNullOrWhiteSpace(Diagnosis))
    {
        ShowMessage("请输入诊断信息");
        return false;
    }

    if (!PrescriptionItems.Any())
    {
        ShowMessage("请添加至少一味中药材");
        return false;
    }

    if (DosageCount <= 0)
    {
        ShowMessage("剂数必须大于0");
        return false;
    }

    return true;
}
```

## 6. 数据绑定与验证

### 6.1 数据绑定架构
```
┌─────────────────┐    Binding    ┌─────────────────┐
│     Views       │ ◄──────────► │   ViewModels    │
│   (XAML UI)     │               │ (Business Logic)│
└─────────────────┘               └─────────────────┘
                                           │
                                    Property Change
                                           │
                                           ▼
                                  ┌─────────────────┐
                                  │     Services    │
                                  │  (Data Access)  │
                                  └─────────────────┘
```

### 6.2 双向绑定示例
```xml
<!-- 诊断信息双向绑定 -->
<TextBox Text="{Binding Diagnosis, UpdateSourceTrigger=PropertyChanged, ValidatesOnDataErrors=True}" />

<!-- 剂数绑定，变化时触发价格重算 -->
<TextBox Text="{Binding DosageCount, UpdateSourceTrigger=PropertyChanged}" />

<!-- 药材列表绑定 -->
<DataGrid ItemsSource="{Binding PrescriptionItems}" SelectedItem="{Binding SelectedHerbItem}" />
```

### 6.3 数据验证规则
#### 6.3.1 ViewModel级验证
```csharp
public string Diagnosis
{
    get => _currentPrescription.Indication ?? string.Empty;
    set
    {
        if (_currentPrescription.Indication != value)
        {
            _currentPrescription.Indication = value;
            RaisePropertyChanged();
            ValidateProperty(value); // 触发验证
        }
    }
}
```

#### 6.3.2 业务规则验证
```csharp
public class BasicValidator
{
    public ValidationResult ValidatePrescription(PrescriptionDto prescription)
    {
        var result = new ValidationResult();

        // 必填项验证
        if (string.IsNullOrWhiteSpace(prescription.Indication))
            result.AddError("诊断不能为空");

        if (prescription.DosageCount <= 0)
            result.AddError("剂数必须大于0");

        if (!prescription.Items.Any())
            result.AddError("至少需要添加一味中药");

        // 业务规则验证
        foreach (var item in prescription.Items)
        {
            if (item.Quantity <= 0)
                result.AddWarning($"药材{item.HerbName}的用量似乎偏少");
        }

        return result;
    }
}
```

### 6.4 实时计算绑定
```csharp
// 价格属性的实时计算
public decimal SingleDosePrice
{
    get
    {
        if (!PrescriptionItems.Any()) return 0m;
        return PrescriptionItems.Sum(item => item.UnitPrice * item.Quantity);
    }
}

public decimal TotalPrice => SingleDosePrice * DosageCount;

// 当药材或剂数变化时，自动触发价格重算
private void RefreshPriceCalculation()
{
    RaisePropertyChanged(nameof(SingleDosePrice));
    RaisePropertyChanged(nameof(TotalPrice));
}
```

## 7. 路由与导航

### 7.1 导航架构
```
                    ┌─────────────────────┐
                    │   Region Manager    │
                    │   (Prism导航管理)    │
                    └─────────────────────┘
                              │
                    ┌─────────▼─────────┐
                    │  Navigation      │
                    │  Context         │
                    └─────────┬─────────┘
                              │
            ┌─────────────────┼─────────────────┐
            │                 │                 │
            ▼                 ▼                 ▼
    ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
    │ Main View    │ │ Composer     │ │ Management   │
    │ Region       │ │ Region       │ │ Region       │
    └──────────────┘ └──────────────┘ └──────────────┘
```

### 7.2 INavigationAware实现
```csharp
public class PrescriptionComposerViewModel : INavigationAware
{
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        // 接收医疗案例ID参数
        if (navigationContext.Parameters.TryGetValue<object>("MedicalCaseId", out var medicalCaseIdParam)
            && medicalCaseIdParam is Guid medicalCaseId)
        {
            _currentMedicalCaseId = medicalCaseId;
            _currentPrescription.MedicalCaseId = medicalCaseId;

            // 加载医疗案例相关信息
            _ = LoadMedicalCaseInfoAsync(medicalCaseId);
        }

        // 接收患者信息参数
        if (navigationContext.Parameters.TryGetValue("PatientInfo", out string patientInfo))
        {
            PatientInfo = patientInfo;
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
        // 清理资源
    }
}
```

### 7.3 导航参数传递
```csharp
// 从诊疗模块导航到处方编辑器
var parameters = new NavigationParameters
{
    { "MedicalCaseId", currentMedicalCase.Id },
    { "PatientInfo", $"{patient.Name} ({patient.Gender}, {patient.Age}岁)" }
};

_regionManager.RequestNavigate("ContentRegion", "PrescriptionComposerView", parameters);
```

### 7.4 返回导航处理
```csharp
private void OnClose()
{
    // 检查是否有未保存的更改
    if (HasUnsavedChanges())
    {
        _dialogService.ShowDialog("ConfirmDialog",
            new DialogParameters { { "Message", "有未保存的更改，确定要关闭吗？" } },
            r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    NavigateBack();
                }
            });
    }
    else
    {
        NavigateBack();
    }
}

private void NavigateBack()
{
    // 发布关闭事件
    _eventAggregator.GetEvent<PrescriptionComposerClosedEvent>()
        .Publish(new PrescriptionComposerClosedEventArgs());

    // 导航回上一个视图
    _regionManager.Regions["ContentRegion"].NavigationService.GoBack();
}
```

## 8. 状态管理

### 8.1 状态管理架构
```
┌─────────────────────────────────────────────────────────┐
│                    状态管理层                            │
├─────────────────────────────────────────────────────────┤
│  Local State (ViewModel内部状态)                        │
│  ├── CurrentPrescription (当前处方)                      │
│  ├── PrescriptionItems (药材列表)                       │
│  ├── IsLoading (加载状态)                               │
│  └── ValidationErrors (验证错误)                        │
├─────────────────────────────────────────────────────────┤
│  Session State (会话状态)                               │
│  ├── CurrentMedicalCaseId (当前医疗案例)                 │
│  ├── PatientInfo (患者信息)                             │
│  └── DraftPrescriptions (草稿列表)                      │
├─────────────────────────────────────────────────────────┤
│  Event State (事件状态)                                 │
│  ├── PrescriptionSavedEvent                            │
│  ├── PrescriptionComposerClosedEvent                   │
│  └── HerbAddedEvent                                    │
└─────────────────────────────────────────────────────────┘
```

### 8.2 本地状态管理
```csharp
public class PrescriptionComposerViewModel
{
    // 主要状态属性
    private PrescriptionDto _currentPrescription = new();
    private bool _isLoading;
    private string _loadingMessage = string.Empty;
    private ObservableCollection<string> _validationErrors = new();

    // 状态变更通知
    public PrescriptionDto CurrentPrescription
    {
        get => _currentPrescription;
        set
        {
            SetProperty(ref _currentPrescription, value);
            OnPrescriptionChanged(); // 触发关联状态更新
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    // 状态变更处理
    private void OnPrescriptionChanged()
    {
        // 同步药材列表
        PrescriptionItems.Clear();
        if (_currentPrescription.Items?.Any() == true)
        {
            foreach (var item in _currentPrescription.Items)
            {
                PrescriptionItems.Add(item);
            }
        }

        // 刷新计算属性
        RefreshPriceCalculation();
    }
}
```

### 8.3 会话状态持久化
```csharp
public interface ISessionManager
{
    // 保存/恢复草稿
    Task SaveDraftAsync(string key, PrescriptionDto prescription);
    Task<PrescriptionDto?> LoadDraftAsync(string key);
    Task RemoveDraftAsync(string key);

    // 会话信息
    Guid? CurrentMedicalCaseId { get; set; }
    string? CurrentPatientInfo { get; set; }
}

// 使用示例
private async Task SaveSessionState()
{
    var draftKey = $"prescription_draft_{_currentMedicalCaseId}";
    await _sessionManager.SaveDraftAsync(draftKey, _currentPrescription);
}
```

### 8.4 事件状态通信
```csharp
// 事件定义
public class PrescriptionSavedEvent : PubSubEvent<PrescriptionSavedEventArgs> { }

public class PrescriptionSavedEventArgs
{
    public Guid PrescriptionId { get; set; }
    public PrescriptionDto Prescription { get; set; }
    public bool IsNew { get; set; }
}

// 发布事件
_eventAggregator.GetEvent<PrescriptionSavedEvent>()
    .Publish(new PrescriptionSavedEventArgs
    {
        PrescriptionId = prescription.Id,
        Prescription = prescription,
        IsNew = isNewPrescription
    });

// 订阅事件
_eventAggregator.GetEvent<PrescriptionSavedEvent>()
    .Subscribe(OnPrescriptionSaved, ThreadOption.UIThread);
```

## 9. API集成

### 9.1 API集成架构
```
┌─────────────────────────────────────────────────────────┐
│                 前端Services层                          │
├─────────────────────────────────────────────────────────┤
│  PrescriptionService                                    │
│  (实现IPrescriptionService接口)                         │
└─────────────────┬───────────────────────────────────────┘
                  │ 调用
                  ▼
┌─────────────────────────────────────────────────────────┐
│               Shared.Interfaces                         │
├─────────────────────────────────────────────────────────┤
│  IPrescriptionApi                                       │
│  (API接口定义)                                          │
└─────────────────┬───────────────────────────────────────┘
                  │ HTTP请求
                  ▼
┌─────────────────────────────────────────────────────────┐
│              WebAPI后端                                  │
├─────────────────────────────────────────────────────────┤
│  PrescriptionController                                 │
│  PrescriptionBusinessService                            │
│  PrescriptionRepository                                 │
└─────────────────────────────────────────────────────────┘
```

### 9.2 API接口调用
```csharp
public class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionApi _prescriptionApi;
    private readonly IExceptionHandler _exceptionHandler;

    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
    {
        return await _exceptionHandler.HandleException<PrescriptionDto>(async () =>
        {
            // 调用共享API接口
            var response = await _prescriptionApi.CreatePrescriptionAsync(dto);

            // 包装返回结果
            return ServiceResult<PrescriptionDto>.Success(response.Content);
        }, nameof(CreateAsync));
    }
}
```

### 9.3 DTO转换管理
```csharp
// UI模型与DTO转换
public static class PrescriptionMapper
{
    public static PrescriptionCreateDto ToCreateDto(PrescriptionDto prescription)
    {
        return new PrescriptionCreateDto
        {
            MedicalCaseId = prescription.MedicalCaseId,
            PatientId = prescription.PatientId,
            Indication = prescription.Indication,
            DosageCount = prescription.DosageCount,
            Advice = prescription.Advice,
            Items = prescription.Items.Select(ToItemCreateDto).ToList()
        };
    }

    private static PrescriptionItemCreateDto ToItemCreateDto(PrescriptionItemDto item)
    {
        return new PrescriptionItemCreateDto
        {
            HerbId = item.HerbId,
            Quantity = item.Quantity,
            Usage = item.Usage
        };
    }
}
```

### 9.4 错误处理与重试
```csharp
public class ExceptionHandler : IExceptionHandler
{
    public async Task<ServiceResult<T>> HandleException<T>(
        Func<Task<ServiceResult<T>>> operation,
        string operationName)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "网络请求失败: {Operation}", operationName);
            return ServiceResult<T>.Failure("网络连接失败，请检查网络连接");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "请求超时: {Operation}", operationName);
            return ServiceResult<T>.Failure("请求超时，请重试");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "操作失败: {Operation}", operationName);
            return ServiceResult<T>.Failure($"操作失败: {ex.Message}");
        }
    }
}
```

## 10. 实现状态

### 10.1 当前实现状态总览
| 组件分类 | 实现状态 | 完成度 | 备注 |
|---------|---------|--------|------|
| 核心模块注册 | ✅ 已实现 | 80% | 基础服务注册完成，视图注册待补充 |
| ViewModels层 | 🔄 部分实现 | 60% | 核心ViewModel完整，部分简化版 |
| Views层 | ✅ 已实现 | 85% | 主要界面完整，样式需优化 |
| Services层 | ✅ 已实现 | 90% | API代理和业务服务基本完整 |
| Models层 | ✅ 已实现 | 95% | UI模型和常量定义完整 |
| 事件通信 | ✅ 已实现 | 80% | 核心事件已定义，部分待实现 |
| 导航路由 | ✅ 已实现 | 75% | 基础导航完成，参数传递完整 |
| 数据验证 | ✅ 已实现 | 70% | 基础验证完成，复杂规则待补充 |

### 10.2 已实现功能清单

#### ✅ 完全实现
- **PrescriptionComposerViewModel**：处方编辑器核心功能
- **PrescriptionComposerView**：处方编辑器界面布局
- **PrescriptionService**：API代理服务
- **PrescriptionComposerService**：编辑器业务服务
- **PrescriptionItem模型**：UI数据模型
- **常量定义**：业务常量和配置
- **基础导航**：INavigationAware实现
- **事件定义**：核心事件类型

#### 🔄 部分实现
- **PrescriptionsMainViewModel**：主视图模型（简化版）
- **PrescriptionManagementViewModel**：管理视图模型（简化版）
- **对话框ViewModels**：药材选择、验方导入等（简化版）
- **价格计算**：基础计算完成，复杂折扣规则待补充
- **数据验证**：基础验证完成，业务规则验证待完善

#### ❌ 待实现
- **打印功能**：处方单据打印
- **配伍检查**：中药配伍禁忌检查
- **批量操作**：批量删除、批量打印等
- **高级搜索**：处方高级筛选功能
- **数据导出**：处方数据导出功能

### 10.3 技术债务与改进点

#### 🔧 架构重构
- **状态管理优化**：考虑引入更完善的状态管理机制
- **缓存策略**：实现药材信息、验方模板等的本地缓存
- **性能优化**：大数据量处理时的虚拟化和分页
- **错误恢复**：网络异常时的数据恢复机制

#### 📊 功能完善
- **用户体验**：添加更多用户友好的交互提示
- **数据校验**：完善中医特色的业务规则验证
- **快捷操作**：键盘快捷键、快速模板等
- **多语言支持**：为后续国际化做准备

### 10.4 后续开发计划

#### Phase 1: 架构重构完成（当前阶段）
- 完成简化版ViewModels的重构
- 补充模块注册中的视图注册
- 完善异常处理和日志记录

#### Phase 2: 功能完善
- 实现打印功能模块
- 添加配伍检查功能
- 完善数据验证规则
- 优化用户界面体验

#### Phase 3: 性能优化
- 实现数据缓存机制
- 优化大数据量处理
- 添加离线操作支持
- 完善错误恢复机制

#### Phase 4: 高级功能
- 实现高级搜索功能
- 添加数据分析图表
- 支持自定义报表
- 集成第三方打印机

---

## 总结

客户端Prescriptions模块作为LYBT系统的核心业务模块，在架构设计上严格遵循MVVM模式和Prism模块化原则，实现了界面与业务逻辑的清晰分离。模块采用UltraThink双层架构，通过依赖注入确保了各层间的松耦合，通过事件聚合器实现了模块间的高效通信。

目前模块的核心功能已基本实现，包括处方编辑、药材管理、价格计算等，为医生提供了便捷的处方管理工作台。后续开发将重点关注功能完善、性能优化和用户体验提升，逐步构建完整的中医处方管理解决方案。