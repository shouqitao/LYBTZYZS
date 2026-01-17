# 设计文档: unify-control-data-binding

## 1. 架构概览

### 1.1 对象类型层次

```
┌─────────────────────────────────────────────────────────────┐
│                    Control (UserControl)                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │ DisplayModel│  │  EditModel  │  │    ViewState        │  │
│  │ (只读展示)  │  │ (可编辑)    │  │  (UI状态管理)       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐                           │
│  │   Options   │  │  Commands   │                           │
│  │ (控件配置)  │  │ (操作命令)  │                           │
│  └─────────────┘  └─────────────┘                           │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 数据流

```
DTO (API Response)
    │
    ▼ Mapper
DisplayModel / EditModel
    │
    ▼ DependencyProperty
Control (XAML Binding)
    │
    ▼ User Interaction
EditModel (Modified)
    │
    ▼ Mapper
InputDto (API Request)
```

## 2. 通用组件设计

### 2.1 PaginationState

**位置**: `Infrastructure/Models/State/PaginationState.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的分页状态
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class PaginationState : ObservableObject
{
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private int _totalCount;

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// 是否为首页
    /// </summary>
    public bool IsFirstPage => CurrentPage == 1;

    /// <summary>
    /// 是否为末页
    /// </summary>
    public bool IsLastPage => CurrentPage >= TotalPages;

    /// <summary>
    /// 跳转到指定页
    /// </summary>
    public void GoToPage(int page)
    {
        CurrentPage = Math.Clamp(page, 1, Math.Max(1, TotalPages));
    }

    /// <summary>
    /// 上一页
    /// </summary>
    public void PreviousPage()
    {
        if (HasPreviousPage) CurrentPage--;
    }

    /// <summary>
    /// 下一页
    /// </summary>
    public void NextPage()
    {
        if (HasNextPage) CurrentPage++;
    }

    /// <summary>
    /// 首页
    /// </summary>
    public void FirstPage() => CurrentPage = 1;

    /// <summary>
    /// 末页
    /// </summary>
    public void LastPage() => CurrentPage = TotalPages;

    /// <summary>
    /// 重置
    /// </summary>
    public void Reset()
    {
        CurrentPage = 1;
        TotalCount = 0;
    }

    /// <summary>
    /// 更新总数并自动调整当前页
    /// </summary>
    public void UpdateTotalCount(int count)
    {
        TotalCount = count;
        // 如果当前页超出范围，调整到最后一页
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
        }
    }

    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(IsFirstPage));
        OnPropertyChanged(nameof(IsLastPage));
    }

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(IsFirstPage));
        OnPropertyChanged(nameof(IsLastPage));
    }

    partial void OnPageSizeChanged(int value)
    {
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(IsLastPage));
    }
}
```

### 2.2 LoadingState

**位置**: `Infrastructure/Models/State/LoadingState.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的加载状态
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class LoadingState : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _message;

    /// <summary>
    /// 开始加载
    /// </summary>
    public void Start(string? message = "加载中...")
    {
        IsLoading = true;
        Message = message;
    }

    /// <summary>
    /// 停止加载
    /// </summary>
    public void Stop()
    {
        IsLoading = false;
        Message = null;
    }

    /// <summary>
    /// 执行异步操作并自动管理加载状态
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string? message = "加载中...")
    {
        try
        {
            Start(message);
            return await action();
        }
        finally
        {
            Stop();
        }
    }

    /// <summary>
    /// 执行异步操作并自动管理加载状态
    /// </summary>
    public async Task ExecuteAsync(Func<Task> action, string? message = "加载中...")
    {
        try
        {
            Start(message);
            await action();
        }
        finally
        {
            Stop();
        }
    }
}
```

### 2.3 SearchState

**位置**: `Infrastructure/Models/State/SearchState.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Infrastructure.Models.State;

/// <summary>
/// 可复用的搜索状态
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class SearchState : ObservableObject
{
    [ObservableProperty]
    private string _keyword = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    /// <summary>
    /// 是否有搜索关键字
    /// </summary>
    public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);

    /// <summary>
    /// 清除搜索
    /// </summary>
    public void Clear()
    {
        Keyword = string.Empty;
        IsSearching = false;
    }

    partial void OnKeywordChanged(string value)
    {
        OnPropertyChanged(nameof(HasKeyword));
    }
}
```

### 2.4 DisplayOptions

**位置**: `Infrastructure/Models/Options/DisplayOptions.cs`

```csharp
namespace LYBT.Desktop.Infrastructure.Models.Options;

/// <summary>
/// 通用显示选项
/// OpenSpec: unify-control-data-binding
/// </summary>
public record DisplayOptions(
    bool IsCompactMode = false,
    bool ShowHeader = true,
    bool ShowFooter = true,
    bool ShowToolbar = true,
    bool IsReadOnly = false
);
```

### 2.5 PaginationOptions

**位置**: `Infrastructure/Models/Options/PaginationOptions.cs`

```csharp
namespace LYBT.Desktop.Infrastructure.Models.Options;

/// <summary>
/// 分页选项
/// OpenSpec: unify-control-data-binding
/// </summary>
public record PaginationOptions
{
    public bool ShowPageSize { get; init; } = true;
    public bool ShowTotalCount { get; init; } = true;
    public bool ShowFirstLastButtons { get; init; } = true;
    public int[] PageSizeOptions { get; init; } = [10, 20, 50, 100];
    public int DefaultPageSize { get; init; } = 20;
}
```

## 3. 模块专用模型设计

### 3.1 ConsultationEditModel (MedicalCase)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.MedicalCase.Models.Edit;

/// <summary>
/// 诊断编辑模型
/// OpenSpec: unify-control-data-binding
/// </summary>
public partial class ConsultationEditModel : ObservableObject
{
    [ObservableProperty]
    private string? _presentIllness;

    [ObservableProperty]
    private string? _tongueDiagnosis;

    [ObservableProperty]
    private string? _pulseDiagnosis;

    [ObservableProperty]
    private string? _tcmDiagnosis;

    /// <summary>
    /// 诊断是否完成（中医诊断必填）
    /// </summary>
    public bool IsComplete => !string.IsNullOrWhiteSpace(TcmDiagnosis);

    /// <summary>
    /// 是否有任何内容
    /// </summary>
    public bool HasContent =>
        !string.IsNullOrWhiteSpace(PresentIllness) ||
        !string.IsNullOrWhiteSpace(TongueDiagnosis) ||
        !string.IsNullOrWhiteSpace(PulseDiagnosis) ||
        !string.IsNullOrWhiteSpace(TcmDiagnosis);

    /// <summary>
    /// 重置
    /// </summary>
    public void Reset()
    {
        PresentIllness = null;
        TongueDiagnosis = null;
        PulseDiagnosis = null;
        TcmDiagnosis = null;
    }

    /// <summary>
    /// 从ConsultationItem填充
    /// </summary>
    public void LoadFrom(ConsultationItem? item)
    {
        if (item == null)
        {
            Reset();
            return;
        }

        PresentIllness = item.PresentIllness;
        TongueDiagnosis = item.TongueDiagnosis;
        PulseDiagnosis = item.PulseDiagnosis;
        TcmDiagnosis = item.TcmDiagnosis;
    }

    partial void OnTcmDiagnosisChanged(string? value)
    {
        OnPropertyChanged(nameof(IsComplete));
        OnPropertyChanged(nameof(HasContent));
    }

    partial void OnPresentIllnessChanged(string? value) => OnPropertyChanged(nameof(HasContent));
    partial void OnTongueDiagnosisChanged(string? value) => OnPropertyChanged(nameof(HasContent));
    partial void OnPulseDiagnosisChanged(string? value) => OnPropertyChanged(nameof(HasContent));
}
```

### 3.2 PatientDetailDisplayModel (Patients)

```csharp
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Models.Display;

/// <summary>
/// 患者详情展示模型 - 用于PatientViewControl
/// OpenSpec: unify-control-data-binding
/// 替代23个独立的DependencyProperty
/// </summary>
public class PatientDetailDisplayModel
{
    #region 基本信息

    public string Name { get; set; } = string.Empty;
    public string PinYinCode { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Male;
    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }
    public string IdNumber { get; set; } = string.Empty;
    public int IdType { get; set; }
    public int MaritalStatus { get; set; }
    public int BloodType { get; set; }

    #endregion

    #region 联系信息

    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    #endregion

    #region 紧急联系人

    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string EmergencyContactRelation { get; set; } = string.Empty;

    #endregion

    #region 病史信息

    public string AllergyHistory { get; set; } = string.Empty;
    public string MedicalHistory { get; set; } = string.Empty;

    #endregion

    #region 就诊信息

    public DateTime? LastVisitTime { get; set; }
    public int VisitCount { get; set; }

    #endregion

    #region 系统信息

    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public string DisableReason { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    #endregion

    #region 计算属性

    public string GenderDisplay => Gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };

    public string AgeDisplay => Age.HasValue ? $"{Age}岁" : "未知";

    public string StatusDisplay => Status switch
    {
        CommonStatus.Enabled => "正常",
        CommonStatus.Disabled => "已禁用",
        _ => "未知"
    };

    public string LastVisitDisplay => LastVisitTime?.ToString("yyyy-MM-dd") ?? "从未就诊";

    public string CreatedAtDisplay => CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

    public string UpdatedAtDisplay => UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

    public bool IsDisabled => Status == CommonStatus.Disabled;

    public bool HasAllergyHistory => !string.IsNullOrWhiteSpace(AllergyHistory);

    public bool HasEmergencyContact => !string.IsNullOrWhiteSpace(EmergencyContactName);

    #endregion
}
```

## 4. 控件重构示例

### 4.1 PatientViewControl 重构

**Before** (23个属性):
```csharp
public partial class PatientViewControl : UserControl
{
    public static readonly DependencyProperty PatientNameProperty = ...;
    public static readonly DependencyProperty PinYinCodeProperty = ...;
    public static readonly DependencyProperty GenderProperty = ...;
    // ... 20个更多属性
}
```

**After** (2个属性):
```csharp
public partial class PatientViewControl : UserControl
{
    public PatientViewControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 患者详情数据
    /// </summary>
    public static readonly DependencyProperty PatientProperty =
        DependencyProperty.Register(
            nameof(Patient),
            typeof(PatientDetailDisplayModel),
            typeof(PatientViewControl),
            new PropertyMetadata(null));

    public PatientDetailDisplayModel? Patient
    {
        get => (PatientDetailDisplayModel?)GetValue(PatientProperty);
        set => SetValue(PatientProperty, value);
    }

    /// <summary>
    /// 是否显示状态字段
    /// </summary>
    public static readonly DependencyProperty ShowStatusProperty =
        DependencyProperty.Register(
            nameof(ShowStatus),
            typeof(bool),
            typeof(PatientViewControl),
            new PropertyMetadata(true));

    public bool ShowStatus
    {
        get => (bool)GetValue(ShowStatusProperty);
        set => SetValue(ShowStatusProperty, value);
    }
}
```

**XAML绑定变化**:
```xml
<!-- Before -->
<TextBlock Text="{Binding PatientName, ElementName=Root}"/>
<TextBlock Text="{Binding Gender, ElementName=Root, Converter={StaticResource GenderConverter}}"/>
<TextBlock Text="{Binding Age, ElementName=Root, StringFormat={}{0}岁}"/>

<!-- After -->
<TextBlock Text="{Binding Patient.Name, ElementName=Root}"/>
<TextBlock Text="{Binding Patient.GenderDisplay, ElementName=Root}"/>
<TextBlock Text="{Binding Patient.AgeDisplay, ElementName=Root}"/>
```

### 4.2 ViewModel适配

```csharp
public partial class PatientMasterDetailViewModel : MasterDetailViewModelBase<PatientListDto, PatientDetailDto>
{
    // 使用Mapper创建DisplayModel
    private readonly PatientDetailDisplayModelMapper _displayMapper = new();

    // 展示模型
    [ObservableProperty]
    private PatientDetailDisplayModel? _patientDisplay;

    protected override async Task OnItemSelectedAsync(PatientListDto? item)
    {
        if (item == null)
        {
            PatientDisplay = null;
            return;
        }

        var detail = await LoadDetailAsync(item.Id);
        PatientDisplay = _displayMapper.ToDisplayModel(detail);
    }
}
```

## 5. Mapper设计

### 5.1 PatientDetailDisplayModelMapper

```csharp
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Patients.Mappers;

/// <summary>
/// 患者详情展示模型映射器
/// OpenSpec: unify-control-data-binding
/// </summary>
[Mapper]
public partial class PatientDetailDisplayModelMapper
{
    public partial PatientDetailDisplayModel ToDisplayModel(PatientDetailDto dto);

    public partial PatientDetailDisplayModel ToDisplayModel(PatientItem item);
}
```

## 6. 迁移检查清单

### 6.1 属性迁移对照表

| 控件 | 旧属性 | 新对象.属性 |
|------|--------|-------------|
| PatientViewControl.PatientName | → | Patient.Name |
| PatientViewControl.Gender | → | Patient.GenderDisplay |
| PatientViewControl.Age | → | Patient.AgeDisplay |
| ... | | |
| MedicalCaseEditControl.PresentIllness | → | Consultation.PresentIllness |
| MedicalCaseEditControl.TongueDiagnosis | → | Consultation.TongueDiagnosis |
| ... | | |
| BaseMasterDataListView.CurrentPage | → | Pagination.CurrentPage |
| BaseMasterDataListView.TotalPages | → | Pagination.TotalPages |
| BaseMasterDataListView.IsBusy | → | Loading.IsLoading |
| ... | | |

### 6.2 绑定模式迁移

| 场景 | Before | After |
|------|--------|-------|
| 只读展示 | `{Binding PropertyName}` | `{Binding Model.PropertyName}` |
| 双向绑定 | `{Binding Property, Mode=TwoWay}` | `{Binding EditModel.Property, Mode=TwoWay}` |
| 命令绑定 | `{Binding SomeCommand}` | `{Binding Commands.SomeCommand}` |
| 状态绑定 | `{Binding IsLoading}` | `{Binding State.IsLoading}` |

## 7. 性能考虑

### 7.1 对象创建开销

使用对象模式会增加少量对象创建开销，但:
- DisplayModel是轻量POCO，创建成本极低
- EditModel/ViewState通常是长生命周期对象
- 绑定路径多一层的性能影响可忽略

### 7.2 内存优化

- DisplayModel可使用对象池复用
- ViewState在ViewModel中复用，不重复创建
- 使用record类型的Options自动支持结构化相等比较
