# LYBT.Desktop.Consultation - 诊疗管理模块

## 📦 项目定位

- **层级**:Client端(Desktop WPF)
- **类型**:业务模块(诊疗管理)
- **职责**:为医生提供中医四诊合参（望、闻、问、切）的诊断记录界面，支持诊断数据录入、辨证论治、诊断完成标记、暂存/继续功能，并作为医案流程的Step1环节与MedicalCase模块集成。采用Prism MVVM架构，通过ISaveable/IValidatable接口契约与流程编排器解耦，确保诊断数据完整性和流程一致性。

## 📂 代码结构

```
LYBT.Desktop.Consultation/
├── Interfaces/                         # 接口定义
│   └── IConsultationRepository.cs      # 诊断仓储接口(继承基类)
├── Models/                             # 数据模型
│   └── ConsultationItem.cs             # 诊断条目模型(列表显示)
├── ViewModels/                         # MVVM视图模型
│   ├── ConsultationFormViewModel.cs    # 诊断表单ViewModel(607行)
│   └── ConsultationManagementViewModel.cs # 诊断列表ViewModel(198行)
├── Views/                              # WPF视图
│   ├── ConsultationFormView.xaml       # 诊断表单UI
│   ├── ConsultationFormView.xaml.cs    # 诊断表单后台
│   ├── ConsultationManagementView.xaml # 诊断列表UI
│   └── ConsultationManagementView.xaml.cs # 诊断列表后台
├── ConsultationModule.cs               # Prism模块注册(2 ViewModels + 2 Views)
├── LYBT.Desktop.Consultation.csproj    # 项目文件
└── README.md                           # 项目文档
```

**说明**:
- **ConsultationFormViewModel**(607行):中医四诊表单核心ViewModel，实现ISaveable/IValidatable接口，支持诊断数据录入、验证、保存、完成标记、暂存功能
- **ConsultationManagementViewModel**(198行):诊断记录列表管理ViewModel，支持数据加载、搜索、详情查看
- **IConsultationRepository**:诊断数据访问接口，继承基类IBaseRepository
- **ConsultationModule**:模块注册中心，统一注册2个ViewModels和2个Views
- **中医特色**:完整的望闻问切（Inspection/AuscultationOlfaction/Inquiry/Palpation）数据结构
- **流程集成**:通过ISaveable/IValidatable接口与MedicalCaseFlowViewModel集成，作为Step1环节

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Core** - 核心库(UnifiedViewModelBase、INavigationAware)
2. **LYBT.Desktop.Foundation** - 基础设施库(BaseApiRepository、IApiService)
3. **LYBT.Desktop.Contracts** - 接口契约库(ISaveable、IValidatable)
4. **LYBT.Desktop.Presentation** - 表示层库(MessageService、IFeatureToggleService)
5. **LYBT.Shared.Models** - 共享DTO模型(ConsultationDto、UpdateConsultationDto)
6. **LYBT.Shared.Interfaces** - 共享接口定义(IConsultationRepository基类)

### 被依赖项目
1. **LYBT.Desktop.MedicalCase** - 医案模块通过MedicalCaseFlowViewModel编排Consultation为Step1
2. **LYBT.Desktop.Shell** - Shell加载Consultation模块并注册到ContentRegion
3. **测试项目**:
   - LYBT.Desktop.Consultation.Tests（单元测试）
   - LYBT.Desktop.IntegrationTests（集成测试）

### NuGet包
- **Prism.DryIoc** (8.x) - MVVM框架、依赖注入、区域导航
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件库
- **Microsoft.Extensions.Logging** (8.0.x) - 日志记录框架

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF (Windows Presentation Foundation)**: Windows桌面UI框架
- **Prism.DryIoc 8.x**: MVVM框架，模块化、依赖注入、区域导航
- **MaterialDesignThemes 5.1.x**: Material Design风格UI组件库
- **MVVM模式**: Model-View-ViewModel架构，数据绑定与业务逻辑分离
- **异步编程**: 全异步方法(async/await)，UI响应性优化
- **Repository模式**: 三层架构数据访问(ViewModel → Repository → BaseApiRepository → IApiService)
- **接口契约模式**: ISaveable/IValidatable确保与流程编排器的一致性

##  快速开始

此项目是一个类库（Prism模块），作为Desktop客户端的一部分被 `LYBT.Desktop.Shell` 加载和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Consultation/LYBT.Desktop.Consultation.csproj
```

**集成说明**:

### 1. Shell加载Consultation模块(在App.xaml.cs中)
```csharp
using LYBT.Desktop.Consultation;

protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Consultation模块(自动注册ViewModels+Views)
    moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);
}
```

### 2. ConsultationFormViewModel核心属性与方法

**属性表** (21个属性 + 5个命令):

| 属性名 | 类型 | 说明 |
|-------|------|------|
| **中医四诊属性** | | |
| ChiefComplaint | string | 主诉（患者就诊原因，必填） |
| PresentIllness | string | 现病史（病情发展过程） |
| TCMDiagnosis | string | 中医诊断（辨证结果，必填） |
| TreatmentPrinciple | string | 治法（治疗原则） |
| Inspection | string | 望诊（观察面色、舌象等） |
| AuscultationOlfaction | string | 闻诊（听声音、嗅气味） |
| Inquiry | string | 问诊（询问症状、病史） |
| Palpation | string | 切诊（把脉、按腹） |
| Remark | string | 备注（其他补充信息） |
| **状态与验证属性** | | |
| MedicalCaseId | Guid | 关联的医案ID（必需） |
| CurrentPatient | PatientDto? | 当前患者信息 |
| Step1CompletedAt | DateTime? | 诊断完成时间（标记Step1完成） |
| Step1CompletedAtText | string | 诊断完成时间文本（UI显示） |
| Step1CompletedAtVisibility | Visibility | 诊断完成时间可见性 |
| PrescriptionEnabled | bool | 是否允许开处方（Step1完成后启用） |
| PrescriptionDisabled | bool | 是否禁用处方（Step1未完成时禁用） |
| HasChiefComplaint | bool | 是否有主诉（验证标志） |
| HasTCMDiagnosis | bool | 是否有中医诊断（验证标志） |
| ValidationMessage | string | 验证消息（错误提示） |
| **命令属性** | | |
| CompleteStep1Command | AsyncDelegateCommand | 完成Step1诊断命令 |
| SaveDraftCommand | AsyncDelegateCommand | 暂存草稿命令 |
| ClearFormCommand | DelegateCommand | 清空表单命令 |
| ShowOtherCasesQueryCommand | DelegateCommand | 显示其他医案查询命令 |

**方法表** (7个核心方法):

| 方法名 | 返回类型 | 说明 |
|-------|---------|------|
| SaveAsync() | Task | 保存诊断数据到服务器（ISaveable接口实现） |
| Validate() | bool | 验证必填项（主诉+中医诊断，IValidatable接口实现） |
| ExecuteCompleteStep1() | Task | 完成Step1诊断，更新Step1CompletedAt，启用处方 |
| ExecuteSaveDraft() | Task | 暂存草稿，保存当前数据但不完成Step1 |
| ExecuteClearForm() | void | 清空表单所有字段 |
| ExecuteShowOtherCasesQuery() | void | 显示患者其他医案（辅助诊断参考） |
| OnNavigatedTo(NavigationContext) | Task | 导航生命周期，加载医案详情并恢复数据 |

### 3. ConsultationManagementViewModel核心属性与方法

**属性表** (9个属性 + 5个命令):

| 属性名 | 类型 | 说明 |
|-------|------|------|
| Consultations | ObservableCollection<ConsultationItem> | 诊断记录列表（DataGrid绑定） |
| SelectedConsultation | ConsultationItem? | 当前选中的诊断记录 |
| SearchKeyword | string | 搜索关键词（按患者姓名/医案ID） |
| IsLoading | bool | 数据加载中标志（显示加载动画） |
| CanSearch | bool | 是否允许搜索（SearchKeyword非空） |
| CanViewDetail | bool | 是否允许查看详情（SelectedConsultation非空） |
| **命令属性** | | |
| LoadDataCommand | AsyncDelegateCommand | 加载数据命令 |
| RefreshCommand | AsyncDelegateCommand | 刷新数据命令 |
| SearchCommand | AsyncDelegateCommand | 搜索命令 |
| ViewDetailsCommand | DelegateCommand<ConsultationItem> | 查看详情命令 |

**方法表** (6个核心方法):

| 方法名 | 返回类型 | 说明 |
|-------|---------|------|
| InitializeAsync() | Task | 初始化ViewModel，加载初始数据 |
| LoadDataAsync() | Task | 加载诊断记录列表（从API） |
| RefreshAsync() | Task | 刷新数据（重新加载） |
| SearchAsync() | Task | 执行搜索（按关键词过滤） |
| ViewDetails(ConsultationItem) | void | 导航到详情视图 |

### 4. 中医四诊表单录入(ConsultationFormViewModel)
```csharp
using LYBT.Desktop.Consultation.ViewModels;
using LYBT.Desktop.Contracts;
using LYBT.Shared.Models;

/// <summary>
/// 诊断表单ViewModel - 实现ISaveable/IValidatable接口
/// </summary>
public class ConsultationFormViewModel : UnifiedViewModelBase, ISaveable, IValidatable, INavigationAware
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    // Step 1: 中医四诊数据属性(望闻问切)
    public string ChiefComplaint { get; set; } = string.Empty;         // 主诉(必填)
    public string PresentIllness { get; set; } = string.Empty;         // 现病史
    public string TCMDiagnosis { get; set; } = string.Empty;           // 中医诊断(必填)
    public string TreatmentPrinciple { get; set; } = string.Empty;     // 治法
    public string Inspection { get; set; } = string.Empty;             // 望诊
    public string AuscultationOlfaction { get; set; } = string.Empty;  // 闻诊
    public string Inquiry { get; set; } = string.Empty;                // 问诊
    public string Palpation { get; set; } = string.Empty;              // 切诊
    public string Remark { get; set; } = string.Empty;                 // 备注

    // Step 2: 状态与控制属性
    public Guid MedicalCaseId { get; set; }                            // 医案ID
    public DateTime? Step1CompletedAt { get; set; }                    // 诊断完成时间
    public bool PrescriptionEnabled => Step1CompletedAt.HasValue;     // 处方启用(诊断完成后)
    public bool PrescriptionDisabled => !Step1CompletedAt.HasValue;   // 处方禁用(诊断未完成)

    // Step 3: 验证必填项(ISaveable接口实现)
    public bool Validate()
    {
        // 必填项验证:主诉+中医诊断
        if (string.IsNullOrWhiteSpace(ChiefComplaint))
        {
            ValidationMessage = "主诉不能为空";
            return false;
        }

        if (string.IsNullOrWhiteSpace(TCMDiagnosis))
        {
            ValidationMessage = "中医诊断不能为空";
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }

    // Step 4: 保存诊断数据(IValidatable接口实现)
    public async Task SaveAsync()
    {
        try
        {
            // 构造诊断DTO
            var dto = new UpdateConsultationDto
            {
                ChiefComplaint = ChiefComplaint,
                PresentIllness = PresentIllness,
                TCMDiagnosis = TCMDiagnosis,
                TreatmentPrinciple = TreatmentPrinciple,
                Inspection = Inspection,
                AuscultationOlfaction = AuscultationOlfaction,
                Inquiry = Inquiry,
                Palpation = Palpation,
                Remark = Remark
            };

            // 调用Repository保存到Server
            await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);

            _logger.LogInformation($"诊断数据已保存:MedicalCaseId={MedicalCaseId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存诊断数据失败");
            throw;
        }
    }

    // Step 5: 完成Step1诊断(标记完成时间)
    private async Task ExecuteCompleteStep1()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            // 验证数据完整性
            if (!Validate())
            {
                SetWarningMessage(ValidationMessage);
                return;
            }

            // 保存诊断数据
            await SaveAsync();

            // 标记Step1完成时间(启用处方)
            Step1CompletedAt = DateTime.Now;
            await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId, Step1CompletedAt.Value);

            SetSuccessMessage("诊断已完成,可以开具处方");

            // 通知属性变更(更新UI)
            RaisePropertyChanged(nameof(PrescriptionEnabled));
            RaisePropertyChanged(nameof(PrescriptionDisabled));
            RaisePropertyChanged(nameof(Step1CompletedAtText));
            RaisePropertyChanged(nameof(Step1CompletedAtVisibility));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成诊断失败");
            SetErrorMessage($"操作失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Step 6: 暂存草稿(不完成Step1)
    private async Task ExecuteSaveDraft()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            // 保存当前数据(不验证必填项)
            await SaveAsync();

            SetSuccessMessage("诊断数据已暂存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "暂存诊断失败");
            SetErrorMessage($"暂存失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Step 7: 导航生命周期 - 加载医案详情并恢复数据
    public async Task OnNavigatedTo(NavigationContext navigationContext)
    {
        try
        {
            IsBusy = true;

            // 获取医案ID参数
            MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");

            // 加载医案详情(含诊断数据)
            var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId);

            if (medicalCase?.Consultation != null)
            {
                // 恢复诊断数据
                var consultation = medicalCase.Consultation;
                ChiefComplaint = consultation.ChiefComplaint ?? string.Empty;
                PresentIllness = consultation.PresentIllness ?? string.Empty;
                TCMDiagnosis = consultation.TcmDiagnosis ?? string.Empty;
                TreatmentPrinciple = consultation.TreatmentMethod ?? string.Empty;
                Inspection = consultation.Inspection ?? string.Empty;
                AuscultationOlfaction = consultation.Auscultation ?? string.Empty;
                Inquiry = consultation.Inquiry ?? string.Empty;
                Palpation = consultation.Palpation ?? string.Empty;
                Remark = consultation.Notes ?? string.Empty;

                // 恢复Step1完成状态
                Step1CompletedAt = consultation.Step1CompletedAt;
            }

            // 加载患者信息
            CurrentPatient = medicalCase?.Patient;

            _logger.LogInformation($"诊断数据已加载:MedicalCaseId={MedicalCaseId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载诊断数据失败");
            SetErrorMessage($"加载失败:{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### 5. 诊断列表管理(ConsultationManagementViewModel)
```csharp
using LYBT.Desktop.Consultation.ViewModels;
using LYBT.Desktop.Consultation.Models;

/// <summary>
/// 诊断列表管理ViewModel - 支持加载、搜索、查看详情
/// </summary>
public class ConsultationManagementViewModel : UnifiedViewModelBase
{
    private readonly IConsultationRepository _consultationApi;

    // Step 1: 数据集合与状态
    public ObservableCollection<ConsultationItem> Consultations { get; } = new();
    public ConsultationItem? SelectedConsultation { get; set; }
    public string SearchKeyword { get; set; } = string.Empty;
    public bool IsLoading { get; set; }

    // Step 2: 命令定义
    public AsyncDelegateCommand LoadDataCommand { get; }
    public AsyncDelegateCommand RefreshCommand { get; }
    public AsyncDelegateCommand SearchCommand { get; }
    public DelegateCommand<ConsultationItem> ViewDetailsCommand { get; }

    public ConsultationManagementViewModel(
        IConsultationRepository consultationApi,
        IRegionManager regionManager,
        IMessageService messageService,
        ILogger<ConsultationManagementViewModel> logger)
    {
        _consultationApi = consultationApi;
        _regionManager = regionManager;
        _messageService = messageService;
        _logger = logger;

        // 初始化命令
        LoadDataCommand = new AsyncDelegateCommand(LoadDataAsync);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
        SearchCommand = new AsyncDelegateCommand(SearchAsync);
        ViewDetailsCommand = new DelegateCommand<ConsultationItem>(ViewDetails);
    }

    // Step 3: 初始化 - 加载数据
    public async Task InitializeAsync()
    {
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化失败");
            _messageService.ShowError("初始化失败");
        }
    }

    // Step 4: 加载诊断记录列表
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            Consultations.Clear();

            // 调用Repository获取诊断列表(分页查询)
            var result = await _consultationApi.GetPagedAsync(1, 100);

            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    Consultations.Add(new ConsultationItem
                    {
                        Id = item.Id,
                        PatientName = item.Patient?.Name ?? "未知患者",
                        ChiefComplaint = item.ChiefComplaint,
                        TCMDiagnosis = item.TcmDiagnosis ?? string.Empty,
                        ConsultationDate = item.CreatedAt,
                        DoctorName = item.Doctor?.Name ?? "未知医生"
                    });
                }

                _logger.LogInformation($"已加载{Consultations.Count}条诊断记录");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载数据失败");
            _messageService.ShowError("加载数据失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Step 5: 搜索诊断记录
    private async Task SearchAsync()
    {
        // 简化实现:重新加载并过滤(实际应调用服务器搜索)
        await LoadDataAsync();
    }

    // Step 6: 查看诊断详情
    private void ViewDetails(ConsultationItem? item)
    {
        if (item == null) return;

        // 导航到详情页面(传递医案ID)
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", item.MedicalCaseId }
        };

        _regionManager.RequestNavigate("ContentRegion", "ConsultationFormView", parameters);
    }
}
```

### 6. ISaveable/IValidatable接口契约(与MedicalCase集成)
```csharp
// ISaveable.cs - Desktop.Contracts
public interface ISaveable
{
    /// <summary>
    /// 保存当前步骤数据到服务器
    /// </summary>
    Task SaveAsync();
}

// IValidatable.cs - Desktop.Contracts
public interface IValidatable
{
    /// <summary>
    /// 验证当前步骤数据完整性
    /// </summary>
    /// <returns>true=验证通过, false=验证失败</returns>
    bool Validate();
}

// ConsultationFormViewModel实现接口
public class ConsultationFormViewModel : UnifiedViewModelBase, ISaveable, IValidatable
{
    // 验证必填项:主诉+中医诊断
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ChiefComplaint))
        {
            ValidationMessage = "主诉不能为空";
            return false;
        }

        if (string.IsNullOrWhiteSpace(TCMDiagnosis))
        {
            ValidationMessage = "中医诊断不能为空";
            return false;
        }

        return true;
    }

    // 保存诊断数据
    public async Task SaveAsync()
    {
        var dto = new UpdateConsultationDto
        {
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            TCMDiagnosis = TCMDiagnosis,
            TreatmentPrinciple = TreatmentPrinciple,
            Inspection = Inspection,
            AuscultationOlfaction = AuscultationOlfaction,
            Inquiry = Inquiry,
            Palpation = Palpation,
            Remark = Remark
        };

        await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);
    }
}

// MedicalCaseFlowViewModel使用接口(无需知道具体类型)
public async Task ExecuteNextStepAsync()
{
    // Step 1: 验证当前步骤(接口契约)
    if (CurrentStepViewModel is IValidatable validatable)
    {
        if (!validatable.Validate())
        {
            SetWarningMessage("请完成必填项");
            return;
        }
    }

    // Step 2: 保存当前步骤(接口契约)
    if (CurrentStepViewModel is ISaveable saveable)
    {
        await saveable.SaveAsync();
    }

    // Step 3: 导航到下一步骤
    CurrentStep = ConsultationStep.Step2Prescription;
}
```

### 7. ConsultationFormView XAML绑定(中医四诊UI)
```xml
<!-- ConsultationFormView.xaml -->
<UserControl x:Class="LYBT.Desktop.Consultation.Views.ConsultationFormView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <ScrollViewer>
        <StackPanel Margin="20">
            <!-- 患者信息栏 -->
            <TextBlock Text="{Binding CurrentPatient.Name}"
                       Style="{StaticResource MaterialDesignHeadline5TextBlock}" />

            <!-- Step1完成标记 -->
            <TextBlock Text="{Binding Step1CompletedAtText}"
                       Foreground="Green"
                       Visibility="{Binding Step1CompletedAtVisibility}"
                       Margin="0,10,0,0" />

            <!-- 必填项:主诉 -->
            <TextBox md:HintAssist.Hint="主诉(必填)"
                     Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,20,0,0" />

            <!-- 现病史 -->
            <TextBox md:HintAssist.Hint="现病史"
                     Text="{Binding PresentIllness, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True"
                     Height="80"
                     Margin="0,10,0,0" />

            <!-- 必填项:中医诊断 -->
            <TextBox md:HintAssist.Hint="中医诊断(必填)"
                     Text="{Binding TCMDiagnosis, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,10,0,0" />

            <!-- 治法 -->
            <TextBox md:HintAssist.Hint="治法"
                     Text="{Binding TreatmentPrinciple, UpdateSourceTrigger=PropertyChanged}"
                     Margin="0,10,0,0" />

            <!-- 中医四诊 -->
            <GroupBox Header="四诊合参" Margin="0,20,0,0">
                <StackPanel>
                    <!-- 望诊 -->
                    <TextBox md:HintAssist.Hint="望诊(观察面色、舌象)"
                             Text="{Binding Inspection, UpdateSourceTrigger=PropertyChanged}"
                             AcceptsReturn="True"
                             Height="60"
                             Margin="0,10,0,0" />

                    <!-- 闻诊 -->
                    <TextBox md:HintAssist.Hint="闻诊(听声音、嗅气味)"
                             Text="{Binding AuscultationOlfaction, UpdateSourceTrigger=PropertyChanged}"
                             AcceptsReturn="True"
                             Height="60"
                             Margin="0,10,0,0" />

                    <!-- 问诊 -->
                    <TextBox md:HintAssist.Hint="问诊(询问症状、病史)"
                             Text="{Binding Inquiry, UpdateSourceTrigger=PropertyChanged}"
                             AcceptsReturn="True"
                             Height="60"
                             Margin="0,10,0,0" />

                    <!-- 切诊 -->
                    <TextBox md:HintAssist.Hint="切诊(把脉、按腹)"
                             Text="{Binding Palpation, UpdateSourceTrigger=PropertyChanged}"
                             AcceptsReturn="True"
                             Height="60"
                             Margin="0,10,0,0" />
                </StackPanel>
            </GroupBox>

            <!-- 备注 -->
            <TextBox md:HintAssist.Hint="备注"
                     Text="{Binding Remark, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True"
                     Height="60"
                     Margin="0,20,0,0" />

            <!-- 验证消息 -->
            <TextBlock Text="{Binding ValidationMessage}"
                       Foreground="Red"
                       Visibility="{Binding ValidationMessage, Converter={StaticResource StringToVisibilityConverter}}"
                       Margin="0,10,0,0" />

            <!-- 操作按钮 -->
            <StackPanel Orientation="Horizontal" Margin="0,20,0,0">
                <!-- 完成诊断按钮 -->
                <Button Content="完成诊断"
                        Command="{Binding CompleteStep1Command}"
                        Style="{StaticResource MaterialDesignRaisedButton}"
                        IsEnabled="{Binding PrescriptionDisabled}" />

                <!-- 暂存按钮 -->
                <Button Content="暂存"
                        Command="{Binding SaveDraftCommand}"
                        Style="{StaticResource MaterialDesignOutlinedButton}"
                        Margin="10,0,0,0" />

                <!-- 清空表单按钮 -->
                <Button Content="清空"
                        Command="{Binding ClearFormCommand}"
                        Style="{StaticResource MaterialDesignOutlinedButton}"
                        Margin="10,0,0,0" />
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

### 8. Repository模式与三层架构(ViewModel → Repository → API)
```csharp
// Step 1: IConsultationRepository接口定义(Interfaces/)
public interface IConsultationRepository : IBaseRepository<ConsultationDto>
{
    // 继承基类接口:
    // - Task<PagedResult<ConsultationDto>> GetPagedAsync(int pageIndex, int pageSize)
    // - Task<ConsultationDto?> GetByIdAsync(Guid id)
    // - Task<ConsultationDto> CreateAsync(ConsultationDto dto)
    // - Task UpdateAsync(Guid id, ConsultationDto dto)
    // - Task DeleteAsync(Guid id)
}

// Step 2: ConsultationRepository实现(Foundation层提供基类)
public class ConsultationRepository : BaseApiRepository<ConsultationDto>, IConsultationRepository
{
    public ConsultationRepository(IApiService apiService, ILogger<ConsultationRepository> logger)
        : base(apiService, logger, "consultations") // API路由前缀
    {
    }

    // 继承基类实现:
    // - GetPagedAsync → GET /api/v1/consultations?pageIndex={x}&pageSize={y}
    // - GetByIdAsync → GET /api/v1/consultations/{id}
    // - CreateAsync → POST /api/v1/consultations
    // - UpdateAsync → PUT /api/v1/consultations/{id}
    // - DeleteAsync → DELETE /api/v1/consultations/{id}
}

// Step 3: ConsultationFormViewModel使用Repository
public class ConsultationFormViewModel : UnifiedViewModelBase
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    public ConsultationFormViewModel(IMedicalCaseRepository medicalCaseRepository)
    {
        _medicalCaseRepository = medicalCaseRepository;
    }

    // 保存诊断数据(调用MedicalCase Repository的UpdateConsultationAsync)
    public async Task SaveAsync()
    {
        var dto = new UpdateConsultationDto
        {
            ChiefComplaint = ChiefComplaint,
            TCMDiagnosis = TCMDiagnosis
            // ...其他字段
        };

        // Repository → BaseApiRepository → IApiService → HttpClient → Server API
        await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);
    }
}

// Step 4: BaseApiRepository封装HTTP通信(Foundation层)
public abstract class BaseApiRepository<TDto>
{
    private readonly IApiService _apiService;
    private readonly string _routePrefix;

    public async Task<PagedResult<TDto>> GetPagedAsync(int pageIndex, int pageSize)
    {
        // 构造URL: /api/v1/{routePrefix}?pageIndex={x}&pageSize={y}
        var url = $"{_routePrefix}?pageIndex={pageIndex}&pageSize={pageSize}";

        // 调用IApiService发送HTTP请求
        return await _apiService.GetAsync<PagedResult<TDto>>(url);
    }
}

// Step 5: IApiService统一HTTP通信(Foundation层)
public interface IApiService
{
    Task<TResult?> GetAsync<TResult>(string url);
    Task<TResult?> PostAsync<TRequest, TResult>(string url, TRequest data);
    Task<TResult?> PutAsync<TRequest, TResult>(string url, TRequest data);
    Task DeleteAsync(string url);
}

// Step 6: ApiService实现(Foundation层)
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public async Task<TResult?> GetAsync<TResult>(string url)
    {
        // 发送HTTP GET请求到Server API
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        // 反序列化响应为DTO
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResult>(json);
    }
}
```

### 9. 诊断完成标记与处方启用控制
```csharp
/// <summary>
/// 诊断完成标记 - 控制处方启用状态
/// </summary>
public class ConsultationFormViewModel : UnifiedViewModelBase
{
    // Step 1: 诊断完成时间属性
    private DateTime? _step1CompletedAt;
    public DateTime? Step1CompletedAt
    {
        get => _step1CompletedAt;
        set
        {
            if (SetProperty(ref _step1CompletedAt, value))
            {
                // 通知相关属性变更
                RaisePropertyChanged(nameof(PrescriptionEnabled));
                RaisePropertyChanged(nameof(PrescriptionDisabled));
                RaisePropertyChanged(nameof(Step1CompletedAtText));
                RaisePropertyChanged(nameof(Step1CompletedAtVisibility));
            }
        }
    }

    // Step 2: 计算属性 - 处方启用(诊断完成后)
    public bool PrescriptionEnabled => Step1CompletedAt.HasValue;

    // Step 3: 计算属性 - 处方禁用(诊断未完成)
    public bool PrescriptionDisabled => !Step1CompletedAt.HasValue;

    // Step 4: UI显示文本
    public string Step1CompletedAtText =>
        Step1CompletedAt.HasValue
            ? $"诊断已完成于:{Step1CompletedAt.Value:yyyy-MM-dd HH:mm:ss}"
            : string.Empty;

    // Step 5: UI可见性控制
    public Visibility Step1CompletedAtVisibility =>
        Step1CompletedAt.HasValue ? Visibility.Visible : Visibility.Collapsed;

    // Step 6: 完成诊断按钮逻辑
    private async Task ExecuteCompleteStep1()
    {
        try
        {
            // 验证必填项
            if (!Validate())
            {
                SetWarningMessage(ValidationMessage);
                return;
            }

            // 保存诊断数据
            await SaveAsync();

            // 标记完成时间(启用处方)
            Step1CompletedAt = DateTime.Now;
            await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId, Step1CompletedAt.Value);

            SetSuccessMessage("诊断已完成,可以开具处方");

            // UI自动更新:
            // - PrescriptionEnabled = true (处方按钮启用)
            // - PrescriptionDisabled = false (完成按钮禁用)
            // - Step1CompletedAtText = "诊断已完成于:2024-01-15 10:30:00" (显示完成时间)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成诊断失败");
            SetErrorMessage($"操作失败:{ex.Message}");
        }
    }
}

// XAML绑定示例:
<StackPanel>
    <!-- 完成时间提示(完成后显示) -->
    <TextBlock Text="{Binding Step1CompletedAtText}"
               Foreground="Green"
               Visibility="{Binding Step1CompletedAtVisibility}" />

    <!-- 完成诊断按钮(完成后禁用) -->
    <Button Content="完成诊断"
            Command="{Binding CompleteStep1Command}"
            IsEnabled="{Binding PrescriptionDisabled}" />

    <!-- 开具处方按钮(完成后启用) -->
    <Button Content="开具处方"
            Command="{Binding NavigateToPrescriptionCommand}"
            IsEnabled="{Binding PrescriptionEnabled}" />
</StackPanel>
```

### 10. ConsultationModule注册(Prism模块入口)
```csharp
using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Consultation.ViewModels;
using LYBT.Desktop.Consultation.Views;
using LYBT.Desktop.Consultation.Interfaces;

/// <summary>
/// Consultation模块 - 注册ViewModels与Views
/// </summary>
public class ConsultationModule : IModule
{
    /// <summary>
    /// 模块初始化(可选)
    /// </summary>
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块加载完成后的初始化逻辑(可选)
    }

    /// <summary>
    /// 注册类型到DI容器
    /// </summary>
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Step 1: 注册Repository(数据访问层)
        //  注意:实际项目中IConsultationRepository继承基类接口,由Foundation层提供实现
        // 此处仅作为示例,实际注册可能在Foundation或Services层

        // Step 2: 注册ViewModels(MVVM视图模型)
        containerRegistry.Register<ConsultationFormViewModel>();
        containerRegistry.Register<ConsultationManagementViewModel>();

        // Step 3: 注册Views(WPF视图)
        containerRegistry.RegisterForNavigation<ConsultationFormView, ConsultationFormViewModel>();
        containerRegistry.RegisterForNavigation<ConsultationManagementView, ConsultationManagementViewModel>();
    }
}

// Shell加载模块(App.xaml.cs)
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Consultation模块(自动注册2 ViewModels + 2 Views)
    moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);
}
```

## 🎨 模块架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                   LYBT.Desktop.Consultation                      │
│                      (诊疗管理模块)                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ 三层架构 + 接口契约
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│ Views(WPF UI)│◄─────│  ViewModels  │─────►│ Repositories │
│              │ XAML │   (MVVM)     │ DI   │ (数据访问)    │
├──────────────┤ Bind │              │      │              │
│Consultation  │      │Consultation  │      │IConsultation │
│FormView      │      │FormViewModel │      │Repository    │
│.xaml         │      │607行         │      │(继承基类)    │
│              │      ├──────────────┤      │              │
│Consultation  │      │21属性+7方法  │      └──────────────┘
│Management    │      │              │              │
│View.xaml     │      │ISaveable +   │              │
│              │      │IValidatable  │              │
└──────────────┘      │接口实现      │              │
                      │              │              │
                      │Consultation  │              ▼
                      │Management    │      ┌──────────────┐
                      │ViewModel     │      │BaseApi       │
                      │198行         │      │Repository    │
                      │              │      │(Foundation)   │
                      │9属性+6方法   │      └──────────────┘
                      └──────────────┘              │
                              │                     │
                              │                     ▼
                              │              ┌──────────────┐
                              │              │IApiService   │
                              │              │(HttpClient)  │
                              │              └──────────────┘
                              │                     │
                              │                     │ HTTP
                              │                     ▼
                              │              ┌──────────────┐
                              │              │LYBT.WebAPI   │
                              │              │/api/v1/      │
                              │              │consultations │
                              │              └──────────────┘
                              ▼
                    ┌─────────────────┐
                    │ MedicalCase      │
                    │ FlowViewModel    │
                    │ (流程编排器)      │
                    └─────────────────┘
                              │
                    ISaveable + IValidatable
                    接口契约调用
                              │
                    ExecuteNextStepAsync()
                    验证 → 保存 → 导航

┌──────────────────────────────────────────────────────────┐
│                     中医四诊数据结构                       │
├──────────────────────────────────────────────────────────┤
│ ChiefComplaint (主诉)                                     │
│ PresentIllness (现病史)                                   │
│ TCMDiagnosis (中医诊断)                                   │
│ TreatmentPrinciple (治法)                                 │
│ ─────────────────────────────────────────                │
│ 四诊合参:                                                 │
│   Inspection (望诊 - 观察面色、舌象)                      │
│   AuscultationOlfaction (闻诊 - 听声音、嗅气味)           │
│   Inquiry (问诊 - 询问症状、病史)                         │
│   Palpation (切诊 - 把脉、按腹)                           │
│ ─────────────────────────────────────────                │
│ Remark (备注)                                             │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│                     状态管理与验证                         │
├──────────────────────────────────────────────────────────┤
│ Step1CompletedAt (诊断完成时间)                           │
│   ├─ PrescriptionEnabled (处方启用)                       │
│   └─ PrescriptionDisabled (处方禁用)                      │
│                                                           │
│ Validate() (验证必填项)                                   │
│   ├─ HasChiefComplaint (主诉验证)                         │
│   └─ HasTCMDiagnosis (中医诊断验证)                       │
│                                                           │
│ SaveAsync() (保存诊断数据)                                │
│   └─ UpdateConsultationAsync(MedicalCaseId, dto)         │
└──────────────────────────────────────────────────────────┘
```

##  设计原则

### 1. ISaveable/IValidatable接口契约 - 与MedicalCase流程解耦

**核心思想**：ConsultationFormViewModel作为Step1环节，通过实现ISaveable/IValidatable接口与MedicalCaseFlowViewModel解耦，流程编排器无需知道Consultation的具体实现细节，只需调用接口方法。

**实现要点**：
- ConsultationFormViewModel实现ISaveable/IValidatable接口
- MedicalCaseFlowViewModel通过接口契约调用`Validate()`和`SaveAsync()`
- 验证逻辑封装在Consultation模块内部，主诉+中医诊断为必填项
- 保存逻辑调用`_medicalCaseRepository.UpdateConsultationAsync()`

**优势**：
- 流程编排与步骤实现解耦，易于扩展和维护
- 接口契约确保一致性，所有Step ViewModel遵循相同协议
- 验证逻辑集中管理，避免在FlowViewModel中硬编码

**反例**：
```csharp
//  不要在FlowViewModel中硬编码验证逻辑
public async Task ExecuteNextStepAsync()
{
    if (CurrentStepViewModel is ConsultationFormViewModel consultation)
    {
        if (string.IsNullOrEmpty(consultation.ChiefComplaint))  // 硬编码验证
        {
            SetWarningMessage("主诉不能为空");
            return;
        }
        await consultation.SaveAsync();  // 直接调用具体类型
    }
}

//  正确:使用接口契约
public async Task ExecuteNextStepAsync()
{
    if (CurrentStepViewModel is IValidatable validatable)
    {
        if (!validatable.Validate())  // 接口调用
        {
            SetWarningMessage("请完成必填项");
            return;
        }
    }

    if (CurrentStepViewModel is ISaveable saveable)
    {
        await saveable.SaveAsync();  // 接口调用
    }
}
```

### 2. 中医四诊合参数据结构 - 辨证论治核心

**核心思想**：完整实现中医四诊合参（望闻问切）数据结构，体现中医诊疗特色，支持医生综合四诊信息进行辨证论治。

**实现要点**：
- 望诊(Inspection):观察面色、舌象、形体等外在表现
- 闻诊(AuscultationOlfaction):听声音、嗅气味
- 问诊(Inquiry):询问症状、病史、生活习惯等
- 切诊(Palpation):把脉、按腹、触诊等

**优势**：
- 符合中医诊疗流程，四诊合参是中医辨证的基础
- 数据结构完整，支持医生全面记录诊断信息
- 与中医诊断(TCMDiagnosis)和治法(TreatmentPrinciple)形成完整链条

**示例**：
```csharp
// 四诊属性定义
public string Inspection { get; set; } = string.Empty;             // 望诊
public string AuscultationOlfaction { get; set; } = string.Empty;  // 闻诊
public string Inquiry { get; set; } = string.Empty;                // 问诊
public string Palpation { get; set; } = string.Empty;              // 切诊

// 中医诊断与治法
public string TCMDiagnosis { get; set; } = string.Empty;           // 辨证结果
public string TreatmentPrinciple { get; set; } = string.Empty;     // 治疗原则

// 诊断流程:
// 1. 望诊:面色萎黄,舌淡苔白
// 2. 闻诊:语声低微,懒言
// 3. 问诊:乏力倦怠,食少纳呆
// 4. 切诊:脉细弱
// 5. 辨证:气血两虚证
// 6. 治法:益气养血
// 7. 处方:八珍汤加减
```

### 3. 诊断完成标记 - Step1状态管理与处方启用控制

**核心思想**：通过Step1CompletedAt时间戳标记诊断完成状态，控制处方启用/禁用逻辑，确保诊断完成后才能开具处方。

**实现要点**：
- Step1CompletedAt为null表示诊断未完成，处方禁用
- Step1CompletedAt有值表示诊断已完成，处方启用
- 计算属性PrescriptionEnabled/PrescriptionDisabled自动更新UI状态
- CompleteStep1Command按钮验证必填项后标记完成时间

**优势**：
- 强制诊断完成流程，避免跳过诊断直接开方
- UI状态自动联动，按钮启用/禁用逻辑清晰
- 完成时间可追溯，记录诊断完成的准确时刻

**示例**：
```csharp
// Step1完成状态判断
public bool PrescriptionEnabled => Step1CompletedAt.HasValue;
public bool PrescriptionDisabled => !Step1CompletedAt.HasValue;

// 完成诊断逻辑
private async Task ExecuteCompleteStep1()
{
    // 验证必填项
    if (!Validate()) return;

    // 保存诊断数据
    await SaveAsync();

    // 标记完成时间(启用处方)
    Step1CompletedAt = DateTime.Now;
    await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId, Step1CompletedAt.Value);

    // UI自动更新:
    // - PrescriptionEnabled = true (处方按钮启用)
    // - PrescriptionDisabled = false (完成按钮禁用)
}

// XAML绑定:
<Button Content="完成诊断"
        Command="{Binding CompleteStep1Command}"
        IsEnabled="{Binding PrescriptionDisabled}" />  // 完成后禁用

<Button Content="开具处方"
        Command="{Binding NavigateToPrescriptionCommand}"
        IsEnabled="{Binding PrescriptionEnabled}" />   // 完成后启用
```

### 4. Repository模式与三层架构 - 数据访问标准化

**核心思想**：采用Repository模式封装数据访问逻辑，ViewModel通过Repository接口与Server API交互，实现三层架构（ViewModel → Repository → API）。

**实现要点**：
- IConsultationRepository接口继承IBaseRepository基类
- BaseApiRepository提供通用CRUD实现（GetPaged/GetById/Create/Update/Delete）
- IApiService统一HTTP通信，封装HttpClient
- Repository返回裸DTO类型，不包装Result<T>（与Desktop.Services区分）

**优势**：
- 数据访问逻辑集中管理，易于测试和维护
- ViewModel无需关心HTTP通信细节，专注业务逻辑
- BaseApiRepository提供标准实现，减少重复代码
- 接口抽象便于Mock，支持单元测试

**架构层次**：
```
ConsultationFormViewModel (业务逻辑)
    ↓ 依赖注入IConsultationRepository
ConsultationRepository (数据访问)
    ↓ 继承BaseApiRepository
BaseApiRepository (通用实现)
    ↓ 依赖注入IApiService
ApiService (HTTP通信)
    ↓ HttpClient
LYBT.WebAPI (Server端)
```

### 5. 暂存/继续功能 - 工作流中断与恢复

**核心思想**：支持诊断数据暂存（不完成Step1）和继续（恢复上次未完成的诊断），适应中医诊所实际工作场景。

**实现要点**：
- SaveDraftCommand保存当前数据但不标记Step1CompletedAt
- OnNavigatedTo导航生命周期方法加载医案详情并恢复所有字段
- 暂存不验证必填项，允许部分数据保存
- 继续时判断Step1CompletedAt状态，决定是否禁用完成按钮

**优势**：
- 适应实际工作流，支持诊断过程中断和恢复
- 防止数据丢失，医生可以随时暂存当前进度
- 用户体验友好，支持灵活的诊疗节奏

**示例**：
```csharp
// 暂存草稿(不验证必填项)
private async Task ExecuteSaveDraft()
{
    await SaveAsync();  // 保存当前数据
    SetSuccessMessage("诊断数据已暂存");
}

// 导航恢复数据
public async Task OnNavigatedTo(NavigationContext navigationContext)
{
    MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");

    var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId);

    if (medicalCase?.Consultation != null)
    {
        // 恢复所有字段
        ChiefComplaint = medicalCase.Consultation.ChiefComplaint ?? string.Empty;
        TCMDiagnosis = medicalCase.Consultation.TcmDiagnosis ?? string.Empty;
        Inspection = medicalCase.Consultation.Inspection ?? string.Empty;
        // ...其他字段

        // 恢复Step1完成状态
        Step1CompletedAt = medicalCase.Consultation.Step1CompletedAt;
    }
}
```

### 6. 异步优先与UI响应性 - 流畅的用户体验

**核心思想**：所有I/O操作（API调用、数据库访问）必须使用async/await异步模式，避免阻塞UI线程，确保应用响应流畅。

**实现要点**：
- 所有Repository方法返回Task<T>
- ViewModel方法标记async/await
- AsyncDelegateCommand支持异步命令绑定
- IsBusy标志显示加载动画，防止重复操作

**优势**：
- UI始终保持响应，不会因为网络请求而卡顿
- IsBusy标志提供明确的加载反馈，用户体验良好
- 异步命令防止重复点击，避免并发问题

**示例**：
```csharp
// 异步命令定义
public AsyncDelegateCommand CompleteStep1Command { get; }

public ConsultationFormViewModel()
{
    CompleteStep1Command = new AsyncDelegateCommand(ExecuteCompleteStep1);
}

// 异步方法实现
private async Task ExecuteCompleteStep1()
{
    try
    {
        IsBusy = true;  // 显示加载动画

        // 验证数据
        if (!Validate()) return;

        // 异步保存(不阻塞UI)
        await SaveAsync();

        // 异步标记完成(不阻塞UI)
        await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId, DateTime.Now);

        SetSuccessMessage("诊断已完成");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "完成诊断失败");
        SetErrorMessage($"操作失败:{ex.Message}");
    }
    finally
    {
        IsBusy = false;  // 隐藏加载动画
    }
}

// XAML绑定异步命令
<Button Content="完成诊断"
        Command="{Binding CompleteStep1Command}"
        IsEnabled="{Binding IsBusy, Converter={StaticResource InvertBooleanConverter}}" />
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/consultation/](../../../../docs/reference/modules/consultation/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/consultation-design.md](../../../../docs/explanation/architecture/client/consultation-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/consultation-development.md](../../../../docs/how-to-guides/client/consultation-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
