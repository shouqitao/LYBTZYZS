# 客户端Patients模块设计文档

## 1. 模块概述

### 1.1 模块定位
客户端Patients模块负责提供完整的患者档案管理功能，包括患者信息的录入、查询、编辑、导入等操作。该模块基于WPF + Prism.DryIoc架构，遵循MVVM设计模式。

### 1.2 核心功能
- **患者档案管理**: 患者基本信息的CRUD操作
- **患者详情查看**: 患者完整信息展示与编辑
- **批量数据导入**: Excel批量导入患者数据
- **患者搜索**: 支持多条件搜索患者

### 1.3 模块结构
```
src/Client/Desktop/Modules/Patients/
├── Interfaces/
│   └── IPatientBusinessService.cs     # 业务服务接口
├── Models/
│   ├── ImportWizardStep.cs           # 导入向导相关模型
│   ├── PatientItem.cs                # 患者列表项UI模型
│   └── PatientViewState.cs           # 患者视图状态
├── Services/
│   ├── PatientBusinessService.cs     # 患者业务服务实现
│   └── PatientService.cs             # 患者基础服务实现
├── ViewModels/
│   ├── PatientDetailViewModel.cs     # 患者详情视图模型
│   └── PatientImportWizardViewModel.cs # 导入向导视图模型
├── Views/
│   ├── PatientDetailView.xaml        # 患者详情视图
│   ├── PatientImportWizardView.xaml  # 导入向导视图
│   └── ...
└── PatientsModule.cs                 # 模块注册类
```

## 2. 架构设计（MVVM模式）

### 2.1 整体架构
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│      View       │    │   ViewModel     │    │     Model       │
│   (XAML + CS)   │◄──►│  (业务逻辑)      │◄──►│   (数据实体)     │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       ▼                       │
         │              ┌─────────────────┐               │
         │              │    Services     │               │
         │              │  (业务/API服务)   │               │
         │              └─────────────────┘               │
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 ▼
                    ┌─────────────────┐
                    │   Server API    │
                    │  (后端接口)      │
                    └─────────────────┘
```

### 2.2 分层职责

#### View层
- **PatientDetailView**: 患者详情展示与编辑界面
- **PatientImportWizardView**: Excel导入向导界面
- 负责UI展示和用户交互响应

#### ViewModel层
- **PatientDetailViewModel**: 患者详情业务逻辑
- **PatientImportWizardViewModel**: 导入向导业务逻辑
- 负责数据绑定、命令处理、状态管理

#### Model层
- **PatientItem**: UI专用的患者数据模型
- **ImportWizardStep**: 导入向导步骤枚举
- **ImportProgressInfo**: 导入进度信息模型

#### Service层
- **PatientService**: 基础CRUD服务
- **PatientBusinessService**: 业务逻辑服务

## 3. ViewModels设计

### 3.1 PatientDetailViewModel

#### 核心职责
- 患者详情数据加载与展示
- 患者信息编辑与保存
- 命令处理（编辑、保存、取消、打印等）
- 导航管理

#### 关键属性
```csharp
public class PatientDetailViewModel : NavigationViewModelBase
{
    // 数据属性
    public Guid PatientId { get; set; }
    public PatientDto? Patient { get; set; }
    
    // 状态属性
    public bool IsLoading { get; set; }
    public bool IsReadOnly { get; set; }
    
    // 显示属性
    public string PatientName => Patient?.Name ?? string.Empty;
    public string Gender => Patient?.Gender.ToString();
    public int Age => Patient?.Age ?? 0;
    // ... 其他显示属性
}
```

#### 关键命令
```csharp
public ICommand LoadDataCommand { get; }        // 加载数据
public ICommand BackCommand { get; }            // 返回
public ICommand EditCommand { get; }            // 编辑
public ICommand SaveCommand { get; }            // 保存
public ICommand CancelEditCommand { get; }      // 取消编辑
public ICommand PrintCommand { get; }           // 打印
public ICommand ViewMedicalHistoryCommand { get; } // 查看病历
```

#### 导航支持
- 实现`INavigationAware`接口
- 支持参数传递（PatientId、ViewMode）
- 处理导航状态变化

### 3.2 PatientImportWizardViewModel

#### 核心职责
- 4步导入向导流程管理
- Excel文件读取与验证
- 批量数据导入执行
- 进度跟踪与结果反馈

#### 向导步骤流程
```csharp
public enum ImportWizardStep
{
    TemplateDownload = 1,    // 模板下载
    FileSelection = 2,       // 文件选择
    DataPreview = 3,         // 数据预览
    ImportExecution = 4      // 导入执行
}
```

#### 关键属性
```csharp
public class PatientImportWizardViewModel : BindableBase
{
    // 向导状态
    public ImportWizardStep CurrentStep { get; set; }
    public string SelectedFilePath { get; set; }
    public DataTable? PreviewData { get; set; }
    public ImportValidationResult? ValidationResult { get; set; }
    
    // 进度管理
    public ImportProgressInfo ProgressInfo { get; set; }
    public bool IsImporting { get; set; }
    public bool IsLoading { get; set; }
}
```

#### 数据验证机制
- **必填字段验证**: 姓名、性别
- **格式验证**: 年龄、电话、证件号
- **重复检查**: 姓名、电话、证件号
- **业务规则验证**: 年龄范围、地址长度等

## 4. Views界面设计

### 4.1 PatientDetailView设计

#### 界面布局
```
┌─────────────────────────────────────────────────────┐
│ ← 返回  |  患者详情  |  编辑 保存 ✖ 📋 🖨           │ 标题栏
├─────────────────────────────────────────────────────┤
│ ┌─────┐  张三          👤 正常                      │ 头像信息卡
│ │ 👤  │  男 · 35岁      状态标签                    │
│ │     │  13800138000                               │
│ └─────┘                                            │
├─────────────────────────────────────────────────────┤
│ ▼ 详细信息                                          │ 详细信息卡
│   证件号码: [___________]  联系电话: [___________]   │
│   家庭住址: [_________________________________]     │
│   紧急联系人: [_________]  联系电话: [_________]     │
│   创建时间: 2023-01-01   更新时间: 2023-01-15       │
├─────────────────────────────────────────────────────┤
│ ▼ 就诊记录                                          │ 就诊记录卡
│   最近就诊记录将在此显示                             │
│   [查看完整病历]                                    │
└─────────────────────────────────────────────────────┘
```

#### 关键特性
- **响应式设计**: 支持不同屏幕尺寸
- **状态驱动**: 基于`IsReadOnly`切换编辑/查看模式
- **卡片式布局**: 信息分组展示，提升可读性
- **快捷操作**: 编辑、打印、查看病历等快捷按钮

### 4.2 PatientImportWizardView设计

#### 4步向导流程界面
```
步骤指示器: ● ○ ○ ○
           1 2 3 4

┌─────────────────────────────────────────────────────┐
│ 第1步：下载患者数据导入模板                          │
│                                                     │
│ [下载Excel模板]                                     │
│                                                     │
│ 模板说明：                                          │
│ • 必填字段：姓名、性别                              │
│ • 选填字段：年龄、电话、证件号、地址、过敏史         │
│                                                     │
│                           [下一步]  [取消]          │
└─────────────────────────────────────────────────────┘
```

#### 步骤内容切换
- **步骤1**: 模板下载说明与下载按钮
- **步骤2**: 文件选择界面
- **步骤3**: 数据预览与验证结果
- **步骤4**: 导入进度与结果展示

## 5. 前端服务层

### 5.1 服务架构

#### IPatientService (基础服务接口)
```csharp
public interface IPatientService
{
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
}
```

#### IPatientBusinessService (业务服务接口)
```csharp
public interface IPatientBusinessService
{
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto, CancellationToken cancellationToken);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> EnableAsync(Guid patientId);
    Task<ServiceResult<bool>> DisableAsync(Guid patientId);
    Task<ServiceResult<bool>> DeleteAsync(Guid patientId);
}
```

### 5.2 服务实现特点

#### PatientService特点
- **API封装**: 封装对`IPatientApi`的调用
- **异常处理**: 使用`IExceptionHandler`统一处理异常
- **结果包装**: 统一返回`ServiceResult<T>`格式

#### PatientBusinessService特点
- **业务逻辑**: 包含患者管理的业务规则
- **取消令牌支持**: 支持长时间操作的取消
- **日志记录**: 详细的操作日志记录

### 5.3 错误处理机制
```csharp
return await _exceptionHandler.HandleException<PatientDto>(async () =>
{
    var response = await _patientApi.CreatePatientAsync(dto);
    return ServiceResult<PatientDto>.Success(response.Content);
}, nameof(CreateAsync));
```

## 6. 数据绑定与验证

### 6.1 数据绑定模式

#### PatientItem模型特点
```csharp
public class PatientItem : BindableBase
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    // DTO转换方法
    public static PatientItem FromDto(PatientDto dto) { ... }
    public PatientDto ToDto() { ... }
    public void UpdateFromDto(PatientDto dto) { ... }
}
```

#### 双向绑定支持
- **属性变更通知**: 继承`BindableBase`
- **命令绑定**: 使用`DelegateCommand`
- **状态绑定**: 视图状态与ViewModel同步

### 6.2 数据验证

#### 导入数据验证规则
```csharp
private ImportValidationResult ValidateImportData(DataTable dataTable)
{
    // 必填字段验证
    var requiredColumns = new[] { "姓名", "性别" };
    
    // 数据格式验证
    - 姓名：非空，长度≤50
    - 性别：男/女/未知
    - 年龄：0-150整数
    - 电话：7-15位，数字/符号格式
    - 证件号：15或18位（警告）
    - 地址：长度≤200
    - 过敏史：长度≤500
    
    // 重复检查
    - 姓名重复：警告
    - 电话重复：警告  
    - 证件号重复：错误
}
```

## 7. 路由与导航

### 7.1 导航架构

#### NavigationViewModelBase继承
```csharp
public class PatientDetailViewModel : NavigationViewModelBase
{
    // 实现INavigationAware接口
    public void OnNavigatedTo(NavigationContext navigationContext) { ... }
    public bool IsNavigationTarget(NavigationContext navigationContext) { ... }
    public void OnNavigatedFrom(NavigationContext navigationContext) { ... }
}
```

#### 导航参数传递
```csharp
// 导航到患者详情
var parameters = new NavigationParameters
{
    { "PatientId", patientId },
    { "ViewMode", "Edit" }
};
await _navigationService.NavigateToAsync(RegionNames.SystemWorkbenchContentRegion, "PatientDetailView", parameters);
```

### 7.2 区域管理
- **SystemWorkbenchContentRegion**: 主要内容区域
- **支持嵌套导航**: 患者详情→病历查看
- **导航历史**: 支持前进后退操作

## 8. 状态管理

### 8.1 ViewModel状态

#### 加载状态管理
```csharp
private async Task LoadDataAsync()
{
    try
    {
        IsLoading = true;
        // 数据加载逻辑
    }
    finally
    {
        IsLoading = false;
    }
}
```

#### 编辑状态切换
```csharp
public bool IsReadOnly
{
    get => _isReadOnly;
    set
    {
        SetProperty(ref _isReadOnly, value);
        RaiseCanExecuteChanged(); // 更新命令状态
    }
}
```

### 8.2 导入向导状态

#### 步骤状态管理
```csharp
private void UpdateStepStyles()
{
    var completedStyle = Application.Current.FindResource("CompletedStep") as Style;
    var activeStyle = Application.Current.FindResource("ActiveStep") as Style;
    var pendingStyle = Application.Current.FindResource("PendingStep") as Style;
    
    Step1Style = GetStepStyle(ImportWizardStep.TemplateDownload);
    // ... 其他步骤
}
```

#### 进度状态跟踪
```csharp
public class ImportProgressInfo
{
    public int PercentComplete { get; set; }
    public string CurrentItem { get; set; }
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public string Message { get; set; }
}
```

## 9. API集成

### 9.1 API服务依赖

#### IPatientApi接口
```csharp
// 通过依赖注入获取API服务
private readonly IPatientApi _patientApi;

// API调用示例
var response = await _patientApi.CreatePatientAsync(dto);
if (response.IsSuccessStatusCode && response.Content != null)
{
    return ServiceResult<PatientDto>.Success(response.Content);
}
```

### 9.2 数据传输对象

#### DTO映射转换
```csharp
// PatientItem ↔ PatientDto 转换
public static PatientItem FromDto(PatientDto dto)
{
    return new PatientItem
    {
        Id = dto.Id,
        Name = dto.Name,
        Gender = dto.Gender.ToString(),
        Age = dto.Age,
        // ... 其他属性映射
    };
}
```

### 9.3 异步操作支持

#### 取消令牌支持
```csharp
public async Task<ServiceResult<PatientDto>> CreateAsync(
    PatientCreateDto createDto, 
    CancellationToken cancellationToken = default)
{
    return await _exceptionHandler.HandleException<PatientDto>(
        async (ct) =>
        {
            var response = await _patientApi.CreatePatientAsync(createDto).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            // 处理响应
        },
        nameof(CreateAsync), 
        $"创建患者档案: {createDto.Name}", 
        cancellationToken);
}
```

## 10. 实现状态

### 10.1 已完成功能 ✅

#### 核心架构
- [x] 模块注册与依赖注入配置
- [x] MVVM架构实现
- [x] 服务层抽象与实现

#### 患者详情功能
- [x] 患者详情查看界面
- [x] 患者信息编辑功能
- [x] 导航支持与参数传递
- [x] 状态管理（加载、编辑状态）
- [x] 命令绑定与处理
- [x] 打印功能集成

#### 数据导入功能
- [x] 4步导入向导界面
- [x] Excel模板生成与下载
- [x] 文件选择与预览
- [x] 数据验证机制
- [x] 批量导入执行
- [x] 进度跟踪与结果反馈
- [x] BackgroundWorker异步处理

#### 数据模型
- [x] PatientItem UI模型
- [x] 导入向导相关模型
- [x] DTO转换机制

### 10.2 待实现功能 ⚠️

#### 模块注册完善
```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // TODO: 注册简化后的视图和视图模型
    // containerRegistry.Register<PatientDetailViewModel>();
    // containerRegistry.Register<PatientImportWizardViewModel>();
}
```

#### 缺失的服务
- [ ] PatientBusinessService在模块中未注册
- [ ] 视图与ViewModel的完整注册
- [ ] 导航路由配置

#### 界面完善
- [ ] 患者列表主界面
- [ ] 患者搜索界面
- [ ] 导入向导视图完整实现

#### 功能扩展
- [ ] 患者头像上传功能
- [ ] 高级搜索条件
- [ ] 导出功能
- [ ] 批量操作支持

### 10.3 技术债务

#### 设计改进点
1. **服务职责重叠**: `PatientService`与`PatientBusinessService`功能重复
2. **导入向导UI**: 步骤内容创建方法需要完善
3. **异常处理**: 部分异步操作缺少取消令牌支持
4. **内存管理**: BackgroundWorker需要正确释放

#### 代码质量优化
1. **命名一致性**: 部分属性命名需要统一
2. **注释完善**: 复杂业务逻辑需要更详细注释
3. **单元测试**: 缺少ViewModels的单元测试
4. **性能优化**: 大数据量导入的性能优化

## 总结

客户端Patients模块基本完成了患者档案管理的核心功能实现，采用了标准的MVVM架构和Prism框架。模块具备了患者详情管理、批量数据导入等关键功能，但在模块注册、界面完善和功能扩展方面还需要进一步开发。

该模块的设计体现了以下优点：
- **职责分离**: View、ViewModel、Service各层职责清晰
- **可扩展性**: 基于接口的设计便于功能扩展
- **用户体验**: 完整的导入向导和进度反馈
- **错误处理**: 统一的异常处理机制

未来开发重点应关注模块注册完善、缺失界面实现以及性能优化等方面。