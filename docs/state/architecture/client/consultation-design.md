# LYBT.Desktop.Consultation - Client端诊疗管理架构设计

> ⚠️ **重要架构变更（2025-11-02）**
> Desktop端已删除`IConsultationRepository`接口（[ADR-008](../decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md)）。
> **所有Consultation操作必须通过`IMedicalCaseRepository`聚合根**进行，符合DDD聚合边界原则。
> 本文档中的Repository设计部分仅作为**历史参考**，实际开发请参考[Desktop端架构指南](README.md#聚合根模式mvvm实践)。

## 📋 文档信息

**文档类型**: 架构设计文档（Architecture Design Document）
**目标读者**: 架构师、高级开发工程师、技术负责人
**关联文档**:
- **架构决策**: [ADR-008: Desktop端Consultation/Prescription不独立实现Repository](../decisions/ADR-008-desktop-consultation-prescription-no-independent-repository.md)
- **开发指南**: `docs/how-to-guides/client/consultation-development.md` *(待创建)*
- **Server端设计**: `docs/explanation/architecture/server/consultation-design.md`
- **业务规则**: `docs/explanation/business-rules.md` (BF-002三步工作流、DC-003必填验证)
- **接口契约**: `docs/explanation/architecture/client/contracts-design.md` *(待创建)*

**文档版本**: v1.1.0
**创建日期**: 2025-10-30
**最后更新**: 2025-11-02（添加ADR-008架构变更说明）

---

## 1. 模块概述

### 1.1 模块定位

**LYBT.Desktop.Consultation** 是Client端WPF桌面应用的核心业务模块，负责提供中医诊疗管理功能。作为医案流程的Step1环节（辨证阶段），Consultation模块实现了完整的中医四诊合参（望、闻、问、切）数据录入、辨证论治、诊断完成标记、暂存/继续等功能，并通过ISaveable/IValidatable接口契约与MedicalCase流程编排器解耦，确保诊断数据完整性和流程一致性。

**层级定位**: Client端（Desktop WPF）
**架构层次**: 业务模块层（Business Module Layer）
**MVVM职责**: Model-View-ViewModel模式，数据绑定与业务逻辑分离

### 1.2 核心职责

| 职责类别 | 具体职责 | 实现方式 |
|---------|---------|---------|
| **中医四诊录入** | 望诊、闻诊、问诊、切诊数据采集 | ConsultationFormViewModel属性绑定 |
| **辨证论治** | 主诉、现病史、中医诊断、治法录入 | 表单ViewModel + 验证逻辑 |
| **诊断完成标记** | Step1完成时间标记，启用处方功能 | Step1CompletedAt属性 + CompleteStep1Command |
| **暂存/继续** | 支持诊断过程中断和恢复 | SaveDraftCommand + OnNavigatedTo |
| **流程集成** | 作为MedicalCase流程的Step1环节 | ISaveable/IValidatable接口契约 |
| **就诊历史** | 查询患者历史就诊记录 | ConsultationManagementViewModel + Repository |

### 1.3 模块特性

```
┌─────────────────────────────────────────────────────────────┐
│              LYBT.Desktop.Consultation 核心特性              │
├─────────────────────────────────────────────────────────────┤
│ ✅ 中医四诊合参 (望闻问切) - 完整的中医诊疗数据结构          │
│ ✅ 接口契约模式 (ISaveable/IValidatable) - 与流程解耦       │
│ ✅ 诊断完成标记 (Step1CompletedAt) - 控制处方启用           │
│ ✅ 暂存/继续功能 - 支持诊断过程中断和恢复                   │
│ ✅ Repository模式 - 三层架构数据访问                        │
│ ✅ 异步优先 (async/await) - UI响应流畅                     │
│ ✅ Material Design UI - 现代化用户界面                      │
│ ✅ Prism MVVM - 模块化、依赖注入、区域导航                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 模块架构

### 2.1 整体架构图

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
│Consultation  │      │Consultation  │      │IMedicalCase  │
│FormView      │      │FormViewModel │      │Repository    │
│.xaml         │      │607行         │      │(共20个方法)  │
│              │      ├──────────────┤      │              │
│Consultation  │      │21属性+7方法  │      └──────────────┘
│Management    │      │              │              │
│View.xaml     │      │ISaveable +   │              │
│              │      │IValidatable  │              │
└──────────────┘      │接口实现      │              │
                      │              │              ▼
                      │Consultation  │      ┌──────────────┐
                      │Management    │      │BaseApi       │
                      │ViewModel     │      │Repository    │
                      │198行         │      │(Foundation)   │
                      │              │      └──────────────┘
                      │9属性+6方法   │              │
                      └──────────────┘              │
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
                              │              │medicalcases  │
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
```

### 2.2 代码结构

```
LYBT.Desktop.Consultation/
├── Interfaces/                         # 接口定义（已清空）
│   └── (已删除IConsultationRepository - ADR-008)
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
└── README.md                           # 项目文档(1294行)
```

### 2.3 依赖关系

#### 2.3.1 依赖的项目

```
LYBT.Desktop.Consultation
    │
    ├─ LYBT.Desktop.Core               # 核心库
    │   └─ UnifiedViewModelBase        # ViewModel基类
    │   └─ INavigationAware            # 导航生命周期
    │
    ├─ LYBT.Desktop.Foundation         # 基础设施库
    │   └─ BaseApiRepository           # 数据访问基类
    │   └─ IApiService                 # HTTP通信接口
    │
    ├─ LYBT.Desktop.Contracts          # 接口契约库
    │   └─ ISaveable                   # 保存接口
    │   └─ IValidatable                # 验证接口
    │
    ├─ LYBT.Desktop.Presentation       # 表示层库
    │   └─ MessageService              # 消息服务
    │   └─ IFeatureToggleService       # 功能开关服务
    │
    └─ LYBT.Shared.Models              # 共享DTO模型
        └─ ConsultationDto             # 诊断DTO
        └─ UpdateConsultationDto       # 更新诊断DTO
```

#### 2.3.2 被依赖项目

```
LYBT.Desktop.Consultation
    │
    ├─ LYBT.Desktop.MedicalCase        # 医案模块
    │   └─ MedicalCaseFlowViewModel    # 流程编排器
    │       └─ 通过ISaveable/IValidatable接口调用
    │
    ├─ LYBT.Desktop.Shell              # Shell主程序
    │   └─ App.xaml.cs                 # 加载Consultation模块
    │       └─ ConfigureModuleCatalog()
    │
    └─ 测试项目
        ├─ LYBT.Desktop.Consultation.Tests      # 单元测试
        └─ LYBT.Desktop.IntegrationTests        # 集成测试
```

#### 2.3.3 NuGet包依赖

```xml
<PackageReference Include="Prism.DryIoc" Version="8.1.x" />
<PackageReference Include="MaterialDesignThemes" Version="5.1.x" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.x" />
```

---

## 3. 数据模型

### 3.1 核心实体与DTO映射

#### 3.1.1 Consultation实体（Server端）

```csharp
/// <summary>
/// 诊疗实体 - 共享主键设计
/// </summary>
public class Consultation : BaseEntity
{
    // ========== 主键与关联 ==========
    public Guid Id { get; set; }  // 共享主键（等于MedicalCase.Id）
    public virtual MedicalCase MedicalCase { get; set; } = null!;  // 一对一关系

    // ========== 基础信息 ==========
    public string? ChiefComplaint { get; set; }         // 主诉（患者就诊原因）
    public string? PresentIllness { get; set; }         // 现病史

    // ========== 中医四诊 ==========
    public string? Inspection { get; set; }             // 望诊（观察面色、舌象）
    public string? AuscultationOlfaction { get; set; }  // 闻诊（听声音、嗅气味）
    public string? Inquiry { get; set; }                // 问诊（询问症状、病史）
    public string? Palpation { get; set; }              // 切诊（把脉、按腹）

    // ========== 诊断结果 ==========
    public string? TCMDiagnosis { get; set; }           // 中医诊断（辨证结果）
    public string? TreatmentPrinciple { get; set; }     // 治疗原则
    public string? MedicalAdvice { get; set; }          // 医嘱

    // ========== 工作流状态（三步工作流）==========
    public DateTime? Step1CompletedAt { get; set; }     // 辨证完成时间
    public DateTime? Step2CompletedAt { get; set; }     // 施治完成时间
    public DateTime? Step3CompletedAt { get; set; }     // 总结完成时间
    public bool PrescriptionEnabled { get; set; } = true;  // 处方开关

    // ========== 审计字段（继承自BaseEntity）==========
    public string? Remark { get; set; }                 // 备注
    public DateTime CreatedAt { get; set; }             // 创建时间
    public DateTime? UpdatedAt { get; set; }            // 更新时间
}
```

#### 3.1.2 ConsultationDto（共享DTO）

```csharp
/// <summary>
/// 诊疗信息DTO - 简化版（Issue #1562 Phase 2）
/// </summary>
public class ConsultationDto : StatusDto, IRemarkable
{
    // ========== 主键与关联 ==========
    public Guid MedicalCaseId { get; set; }  // 医案ID（共享主键）
    public Guid PatientId { get; set; }      // 患者ID
    public Guid UserId { get; set; }         // 医生ID

    // ========== 展示信息 ==========
    public string? PatientName { get; set; }  // 患者姓名
    public string? DoctorName { get; set; }   // 医生姓名

    // ========== 基础信息 ==========
    public string? ChiefComplaint { get; set; }      // 主诉
    public string? PresentIllness { get; set; }      // 现病史

    // ========== 中医四诊 ==========
    public string? Inspection { get; set; }          // 望诊
    public string? AuscultationOlfaction { get; set; }  // 闻诊
    public string? Inquiry { get; set; }             // 问诊
    public string? Palpation { get; set; }           // 切诊

    // ========== 诊断结果 ==========
    public string? TCMDiagnosis { get; set; }        // 中医诊断
    public string? TreatmentPrinciple { get; set; }  // 治疗原则
    public string? MedicalAdvice { get; set; }       // 医嘱

    // ========== 审计字段 ==========
    public string? Remark { get; set; }              // 备注
}
```

#### 3.1.3 ConsultationUpdateDto（更新DTO）

```csharp
/// <summary>
/// 诊疗更新DTO - 简化版（Issue #1562 Phase 2）
/// </summary>
public class ConsultationUpdateDto : ConsultationInputBaseDto, IIdentifiable<Guid>
{
    public Guid Id { get; set; }  // 诊疗ID（共享主键）

    // ========== 继承自ConsultationInputBaseDto的字段 ==========
    // 主诉、现病史、四诊、诊断、治疗原则、医嘱、备注（9个字段）
}
```

### 3.2 ConsultationItem模型（列表显示）

```csharp
/// <summary>
/// 诊断条目模型 - 用于列表显示
/// </summary>
public class ConsultationItem : BindableBase
{
    public Guid Id { get; set; }                    // 诊断ID
    public Guid MedicalCaseId { get; set; }         // 医案ID
    public string PatientName { get; set; }         // 患者姓名
    public string ChiefComplaint { get; set; }      // 主诉
    public string TCMDiagnosis { get; set; }        // 中医诊断
    public DateTime ConsultationDate { get; set; }  // 就诊日期
    public string DoctorName { get; set; }          // 医生姓名

    // ========== 格式化属性 ==========
    public string ConsultationDateText =>
        ConsultationDate.ToString("yyyy-MM-dd HH:mm");
}
```

### 3.3 数据字段说明

#### 3.3.1 中医四诊字段

| 字段名 | 中文名 | 类型 | 长度 | 必填 | 说明 |
|-------|-------|------|-----|------|------|
| **Inspection** | 望诊 | string? | 500 | 否 | 观察面色、舌象、形体等外在表现 |
| **AuscultationOlfaction** | 闻诊 | string? | 500 | 否 | 听声音、嗅气味 |
| **Inquiry** | 问诊 | string? | 500 | 否 | 询问症状、病史、生活习惯等 |
| **Palpation** | 切诊 | string? | 500 | 否 | 把脉、按腹、触诊等 |

#### 3.3.2 诊断结果字段

| 字段名 | 中文名 | 类型 | 长度 | 必填 | 说明 |
|-------|-------|------|-----|------|------|
| **ChiefComplaint** | 主诉 | string? | 500 | **是** | 患者就诊的主要原因（BF-002业务规则） |
| **PresentIllness** | 现病史 | string? | 1000 | 否 | 病情发展过程 |
| **TCMDiagnosis** | 中医诊断 | string? | 500 | **是** | 辨证结果（BF-002业务规则） |
| **TreatmentPrinciple** | 治疗原则 | string? | 500 | 否 | 治法（如：疏肝健脾） |
| **MedicalAdvice** | 医嘱 | string? | 1000 | 否 | 饮食、起居建议 |

#### 3.3.3 工作流状态字段

| 字段名 | 中文名 | 类型 | 必填 | 说明 |
|-------|-------|------|------|------|
| **Step1CompletedAt** | 辨证完成时间 | DateTime? | 否 | 标记诊断完成，启用处方 |
| **Step2CompletedAt** | 施治完成时间 | DateTime? | 否 | 标记施治完成（处方或非药物治疗） |
| **Step3CompletedAt** | 总结完成时间 | DateTime? | 否 | 标记总结完成（医嘱记录） |
| **PrescriptionEnabled** | 处方开关 | bool | 是 | 控制是否允许开具处方 |

---

## 4. ConsultationFormViewModel详解

### 4.1 类定义与职责

```csharp
/// <summary>
/// 诊断表单ViewModel - 实现ISaveable/IValidatable接口
/// 职责：中医四诊数据录入、验证、保存、完成标记、暂存功能
/// </summary>
public class ConsultationFormViewModel : UnifiedViewModelBase,
                                         ISaveable,
                                         IValidatable,
                                         INavigationAware
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IRegionManager _regionManager;
    private readonly IMessageService _messageService;
    private readonly ILogger<ConsultationFormViewModel> _logger;

    public ConsultationFormViewModel(
        IMedicalCaseRepository medicalCaseRepository,
        IRegionManager regionManager,
        IMessageService messageService,
        ILogger<ConsultationFormViewModel> logger)
    {
        _medicalCaseRepository = medicalCaseRepository;
        _regionManager = regionManager;
        _messageService = messageService;
        _logger = logger;

        // 初始化命令
        CompleteStep1Command = new AsyncDelegateCommand(
            ExecuteCompleteStep1,
            CanExecuteCompleteStep1)
            .ObservesProperty(() => IsBusy)
            .ObservesProperty(() => PrescriptionDisabled);

        SaveDraftCommand = new AsyncDelegateCommand(ExecuteSaveDraft);
        ClearFormCommand = new DelegateCommand(ExecuteClearForm);
        ShowOtherCasesQueryCommand = new DelegateCommand(ExecuteShowOtherCasesQuery);
    }
}
```

### 4.2 属性定义（21个属性 + 5个命令）

#### 4.2.1 中医四诊属性（9个）

```csharp
// ========== 基础信息 ==========
private string _chiefComplaint = string.Empty;
[Required(ErrorMessage = "主诉不能为空")]
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        if (SetProperty(ref _chiefComplaint, value))
        {
            RaisePropertyChanged(nameof(HasChiefComplaint));
            CompleteStep1Command.RaiseCanExecuteChanged();
        }
    }
}

private string _presentIllness = string.Empty;
public string PresentIllness
{
    get => _presentIllness;
    set => SetProperty(ref _presentIllness, value);
}

// ========== 中医四诊 ==========
private string _inspection = string.Empty;
public string Inspection
{
    get => _inspection;
    set => SetProperty(ref _inspection, value);
}

private string _auscultationOlfaction = string.Empty;
public string AuscultationOlfaction
{
    get => _auscultationOlfaction;
    set => SetProperty(ref _auscultationOlfaction, value);
}

private string _inquiry = string.Empty;
public string Inquiry
{
    get => _inquiry;
    set => SetProperty(ref _inquiry, value);
}

private string _palpation = string.Empty;
public string Palpation
{
    get => _palpation;
    set => SetProperty(ref _palpation, value);
}

// ========== 诊断结果 ==========
private string _tcmDiagnosis = string.Empty;
[Required(ErrorMessage = "中医诊断不能为空")]
public string TCMDiagnosis
{
    get => _tcmDiagnosis;
    set
    {
        if (SetProperty(ref _tcmDiagnosis, value))
        {
            RaisePropertyChanged(nameof(HasTCMDiagnosis));
            CompleteStep1Command.RaiseCanExecuteChanged();
        }
    }
}

private string _treatmentPrinciple = string.Empty;
public string TreatmentPrinciple
{
    get => _treatmentPrinciple;
    set => SetProperty(ref _treatmentPrinciple, value);
}

private string _remark = string.Empty;
public string Remark
{
    get => _remark;
    set => SetProperty(ref _remark, value);
}
```

#### 4.2.2 状态与验证属性（8个）

```csharp
// ========== 主键与关联 ==========
private Guid _medicalCaseId;
public Guid MedicalCaseId
{
    get => _medicalCaseId;
    set => SetProperty(ref _medicalCaseId, value);
}

private PatientDto? _currentPatient;
public PatientDto? CurrentPatient
{
    get => _currentPatient;
    set => SetProperty(ref _currentPatient, value);
}

// ========== 工作流状态 ==========
private DateTime? _step1CompletedAt;
public DateTime? Step1CompletedAt
{
    get => _step1CompletedAt;
    set
    {
        if (SetProperty(ref _step1CompletedAt, value))
        {
            RaisePropertyChanged(nameof(PrescriptionEnabled));
            RaisePropertyChanged(nameof(PrescriptionDisabled));
            RaisePropertyChanged(nameof(Step1CompletedAtText));
            RaisePropertyChanged(nameof(Step1CompletedAtVisibility));
            CompleteStep1Command.RaiseCanExecuteChanged();
        }
    }
}

// ========== 计算属性 ==========
public bool PrescriptionEnabled => Step1CompletedAt.HasValue;
public bool PrescriptionDisabled => !Step1CompletedAt.HasValue;

public string Step1CompletedAtText =>
    Step1CompletedAt.HasValue
        ? $"诊断已完成于: {Step1CompletedAt.Value:yyyy-MM-dd HH:mm:ss}"
        : string.Empty;

public Visibility Step1CompletedAtVisibility =>
    Step1CompletedAt.HasValue ? Visibility.Visible : Visibility.Collapsed;

// ========== 验证标志 ==========
public bool HasChiefComplaint => !string.IsNullOrWhiteSpace(ChiefComplaint);
public bool HasTCMDiagnosis => !string.IsNullOrWhiteSpace(TCMDiagnosis);

private string _validationMessage = string.Empty;
public string ValidationMessage
{
    get => _validationMessage;
    set => SetProperty(ref _validationMessage, value);
}
```

#### 4.2.3 命令属性（5个）

```csharp
// ========== 命令定义 ==========
public AsyncDelegateCommand CompleteStep1Command { get; }  // 完成Step1诊断
public AsyncDelegateCommand SaveDraftCommand { get; }      // 暂存草稿
public DelegateCommand ClearFormCommand { get; }           // 清空表单
public DelegateCommand ShowOtherCasesQueryCommand { get; } // 显示其他医案查询
```

### 4.3 核心方法（7个方法）

#### 4.3.1 ISaveable接口实现

```csharp
/// <summary>
/// 保存诊断数据到服务器（ISaveable接口实现）
/// </summary>
public async Task SaveAsync()
{
    try
    {
        // 构造诊断DTO
        var dto = new UpdateConsultationDto
        {
            Id = MedicalCaseId,
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            Inspection = Inspection,
            AuscultationOlfaction = AuscultationOlfaction,
            Inquiry = Inquiry,
            Palpation = Palpation,
            TCMDiagnosis = TCMDiagnosis,
            TreatmentPrinciple = TreatmentPrinciple,
            Remark = Remark
        };

        // 调用Repository保存到Server
        await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);

        _logger.LogInformation($"诊断数据已保存: MedicalCaseId={MedicalCaseId}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "保存诊断数据失败");
        throw;
    }
}
```

#### 4.3.2 IValidatable接口实现

```csharp
/// <summary>
/// 验证必填项（主诉+中医诊断，IValidatable接口实现）
/// </summary>
public bool Validate()
{
    // 必填项验证: 主诉+中医诊断（BF-002业务规则）
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
```

#### 4.3.3 完成Step1诊断

```csharp
/// <summary>
/// 完成Step1诊断，更新Step1CompletedAt，启用处方
/// </summary>
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

        // 标记Step1完成时间（启用处方）
        var now = DateTime.Now;
        await _medicalCaseRepository.CompleteStep1Async(
            MedicalCaseId,
            new CompleteStep1Request
            {
                PrescriptionEnabled = true  // 默认启用处方
            });

        // 更新本地状态
        Step1CompletedAt = now;

        SetSuccessMessage("诊断已完成，可以开具处方");

        _logger.LogInformation($"Step1诊断已完成: MedicalCaseId={MedicalCaseId}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "完成诊断失败");
        SetErrorMessage($"操作失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

/// <summary>
/// 判断是否可以执行完成Step1命令
/// </summary>
private bool CanExecuteCompleteStep1()
{
    return !IsBusy && PrescriptionDisabled && HasChiefComplaint && HasTCMDiagnosis;
}
```

#### 4.3.4 暂存草稿

```csharp
/// <summary>
/// 暂存草稿，保存当前数据但不完成Step1
/// </summary>
private async Task ExecuteSaveDraft()
{
    try
    {
        IsBusy = true;
        ClearMessage();

        // 保存当前数据（不验证必填项）
        await SaveAsync();

        SetSuccessMessage("诊断数据已暂存");

        _logger.LogInformation($"诊断数据已暂存: MedicalCaseId={MedicalCaseId}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "暂存诊断失败");
        SetErrorMessage($"暂存失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

#### 4.3.5 清空表单

```csharp
/// <summary>
/// 清空表单所有字段
/// </summary>
private void ExecuteClearForm()
{
    ChiefComplaint = string.Empty;
    PresentIllness = string.Empty;
    Inspection = string.Empty;
    AuscultationOlfaction = string.Empty;
    Inquiry = string.Empty;
    Palpation = string.Empty;
    TCMDiagnosis = string.Empty;
    TreatmentPrinciple = string.Empty;
    Remark = string.Empty;

    ClearMessage();

    _logger.LogInformation("表单已清空");
}
```

#### 4.3.6 导航生命周期

```csharp
/// <summary>
/// 导航生命周期 - 加载医案详情并恢复数据
/// </summary>
public async Task OnNavigatedTo(NavigationContext navigationContext)
{
    try
    {
        IsBusy = true;

        // 获取医案ID参数
        MedicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");

        // 加载医案详情（含诊断数据）
        var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId);

        if (medicalCase?.Consultation != null)
        {
            // 恢复诊断数据
            var consultation = medicalCase.Consultation;
            ChiefComplaint = consultation.ChiefComplaint ?? string.Empty;
            PresentIllness = consultation.PresentIllness ?? string.Empty;
            Inspection = consultation.Inspection ?? string.Empty;
            AuscultationOlfaction = consultation.AuscultationOlfaction ?? string.Empty;
            Inquiry = consultation.Inquiry ?? string.Empty;
            Palpation = consultation.Palpation ?? string.Empty;
            TCMDiagnosis = consultation.TcmDiagnosis ?? string.Empty;
            TreatmentPrinciple = consultation.TreatmentMethod ?? string.Empty;
            Remark = consultation.Notes ?? string.Empty;

            // 恢复Step1完成状态
            Step1CompletedAt = consultation.Step1CompletedAt;
        }

        // 加载患者信息
        CurrentPatient = medicalCase?.Patient;

        _logger.LogInformation($"诊断数据已加载: MedicalCaseId={MedicalCaseId}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载诊断数据失败");
        SetErrorMessage($"加载失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

---

## 5. ConsultationManagementViewModel详解

### 5.1 类定义与职责

```csharp
/// <summary>
/// 诊断列表管理ViewModel - 支持加载、搜索、查看详情
/// 职责：诊断记录列表管理、搜索、详情查看
/// </summary>
/// ⚠️ 注意：此代码示例为历史设计参考，Desktop端已删除IConsultationRepository（ADR-008）
/// 实际实现应通过IMedicalCaseRepository聚合根访问Consultation数据
public class ConsultationManagementViewModel : UnifiedViewModelBase
{
    // ❌ 已删除：private readonly IConsultationRepository _consultationApi; (ADR-008)
    // ✅ 应使用：private readonly IMedicalCaseRepository _medicalCaseRepository;
    private readonly IRegionManager _regionManager;
    private readonly IMessageService _messageService;
    private readonly ILogger<ConsultationManagementViewModel> _logger;

    public ConsultationManagementViewModel(
        // IConsultationRepository consultationApi,  // ❌ Desktop端已删除此接口
        IRegionManager regionManager,
        IMessageService messageService,
        ILogger<ConsultationManagementViewModel> logger)
    {
        // _consultationApi = consultationApi;  // ❌ 已删除
        _regionManager = regionManager;
        _messageService = messageService;
        _logger = logger;

        // 初始化命令
        LoadDataCommand = new AsyncDelegateCommand(LoadDataAsync);
        RefreshCommand = new AsyncDelegateCommand(RefreshAsync);
        SearchCommand = new AsyncDelegateCommand(
            SearchAsync,
            CanExecuteSearch)
            .ObservesProperty(() => CanSearch);
        ViewDetailsCommand = new DelegateCommand<ConsultationItem>(ViewDetails);
    }
}
```

### 5.2 属性定义（9个属性 + 4个命令）

```csharp
// ========== 数据集合 ==========
private ObservableCollection<ConsultationItem> _consultations = new();
public ObservableCollection<ConsultationItem> Consultations
{
    get => _consultations;
    set => SetProperty(ref _consultations, value);
}

private ConsultationItem? _selectedConsultation;
public ConsultationItem? SelectedConsultation
{
    get => _selectedConsultation;
    set
    {
        if (SetProperty(ref _selectedConsultation, value))
        {
            RaisePropertyChanged(nameof(CanViewDetail));
        }
    }
}

// ========== 搜索与状态 ==========
private string _searchKeyword = string.Empty;
public string SearchKeyword
{
    get => _searchKeyword;
    set
    {
        if (SetProperty(ref _searchKeyword, value))
        {
            RaisePropertyChanged(nameof(CanSearch));
        }
    }
}

private bool _isLoading;
public bool IsLoading
{
    get => _isLoading;
    set => SetProperty(ref _isLoading, value);
}

// ========== 计算属性 ==========
public bool CanSearch => !string.IsNullOrWhiteSpace(SearchKeyword);
public bool CanViewDetail => SelectedConsultation != null;

// ========== 命令定义 ==========
public AsyncDelegateCommand LoadDataCommand { get; }
public AsyncDelegateCommand RefreshCommand { get; }
public AsyncDelegateCommand SearchCommand { get; }
public DelegateCommand<ConsultationItem> ViewDetailsCommand { get; }
```

### 5.3 核心方法（6个方法）

```csharp
/// <summary>
/// 初始化ViewModel，加载初始数据
/// </summary>
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

/// <summary>
/// 加载诊断记录列表
/// </summary>
private async Task LoadDataAsync()
{
    try
    {
        IsLoading = true;
        Consultations.Clear();

        // 调用Repository获取诊断列表（分页查询）
        var result = await _consultationApi.GetPagedAsync(1, 100);

        if (result?.Items != null)
        {
            foreach (var item in result.Items)
            {
                Consultations.Add(new ConsultationItem
                {
                    Id = item.Id,
                    MedicalCaseId = item.MedicalCaseId,
                    PatientName = item.PatientName ?? "未知患者",
                    ChiefComplaint = item.ChiefComplaint ?? string.Empty,
                    TCMDiagnosis = item.TcmDiagnosis ?? string.Empty,
                    ConsultationDate = item.CreatedAt,
                    DoctorName = item.DoctorName ?? "未知医生"
                });
            }

            _logger.LogInformation($"已加载 {Consultations.Count} 条诊断记录");
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

/// <summary>
/// 刷新数据（重新加载）
/// </summary>
private async Task RefreshAsync()
{
    await LoadDataAsync();
}

/// <summary>
/// 执行搜索（按关键词过滤）
/// </summary>
private async Task SearchAsync()
{
    // 简化实现: 重新加载并过滤（实际应调用服务器搜索）
    await LoadDataAsync();
}

private bool CanExecuteSearch()
{
    return CanSearch && !IsLoading;
}

/// <summary>
/// 查看诊断详情
/// </summary>
private void ViewDetails(ConsultationItem? item)
{
    if (item == null) return;

    // 导航到详情页面（传递医案ID）
    var parameters = new NavigationParameters
    {
        { "MedicalCaseId", item.MedicalCaseId }
    };

    _regionManager.RequestNavigate("ContentRegion", "ConsultationFormView", parameters);
}
```

---

## 6. Repository模式与三层架构

### 6.1 架构层次

```
┌─────────────────────────────────────────────────────────────┐
│                   三层架构数据访问流程                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ConsultationFormViewModel (业务逻辑层)                      │
│      │                                                       │
│      │ 依赖注入 IMedicalCaseRepository                       │
│      ▼                                                       │
│  MedicalCaseRepository (数据访问层)                          │
│      │                                                       │
│      │ 继承 BaseApiRepository<MedicalCaseDto>               │
│      ▼                                                       │
│  BaseApiRepository (基础设施层)                              │
│      │                                                       │
│      │ 依赖注入 IApiService                                  │
│      ▼                                                       │
│  ApiService (HTTP通信层)                                     │
│      │                                                       │
│      │ 使用 HttpClient                                      │
│      ▼                                                       │
│  LYBT.WebAPI (Server端API)                                  │
│      └─ PUT /api/v1/medicalcases/{id}/consultation          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 6.2 IMedicalCaseRepository接口（Consultation相关方法）

```csharp
/// <summary>
/// 医案仓储接口 - 包含Consultation管理方法
/// </summary>
public interface IMedicalCaseRepository : IBaseRepository<MedicalCaseDto>
{
    // ========== Consultation管理方法（3个）==========

    /// <summary>
    /// 更新诊断信息
    /// </summary>
    Task<ConsultationDto> UpdateConsultationAsync(
        Guid caseId,
        UpdateConsultationDto dto);

    /// <summary>
    /// 完成Step1诊断（标记完成时间）
    /// </summary>
    Task<ConsultationFlowResult> CompleteStep1Async(
        Guid caseId,
        CompleteStep1Request request);

    /// <summary>
    /// 重置诊断步骤（用于修改诊断）
    /// </summary>
    Task ResetConsultationStepsAsync(Guid caseId);

    // ========== 其他17个方法省略 ==========
}
```

### 6.3 BaseApiRepository基类

```csharp
/// <summary>
/// API仓储基类 - 提供通用CRUD实现
/// </summary>
public abstract class BaseApiRepository<TDto>
{
    private readonly IApiService _apiService;
    private readonly ILogger _logger;
    private readonly string _routePrefix;

    public BaseApiRepository(
        IApiService apiService,
        ILogger logger,
        string routePrefix)
    {
        _apiService = apiService;
        _logger = logger;
        _routePrefix = routePrefix;
    }

    /// <summary>
    /// 分页查询
    /// </summary>
    public async Task<PagedResult<TDto>> GetPagedAsync(
        int pageIndex,
        int pageSize)
    {
        var url = $"{_routePrefix}?pageIndex={pageIndex}&pageSize={pageSize}";
        return await _apiService.GetAsync<PagedResult<TDto>>(url);
    }

    /// <summary>
    /// 按ID查询
    /// </summary>
    public async Task<TDto?> GetByIdAsync(Guid id)
    {
        var url = $"{_routePrefix}/{id}";
        return await _apiService.GetAsync<TDto>(url);
    }

    /// <summary>
    /// 创建实体
    /// </summary>
    public async Task<TDto> CreateAsync(TDto dto)
    {
        return await _apiService.PostAsync<TDto, TDto>(_routePrefix, dto);
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task UpdateAsync(Guid id, TDto dto)
    {
        var url = $"{_routePrefix}/{id}";
        await _apiService.PutAsync<TDto, object>(url, dto);
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        var url = $"{_routePrefix}/{id}";
        await _apiService.DeleteAsync(url);
    }
}
```

### 6.4 IApiService接口

```csharp
/// <summary>
/// API服务接口 - 统一HTTP通信
/// </summary>
public interface IApiService
{
    Task<TResult?> GetAsync<TResult>(string url);
    Task<TResult?> PostAsync<TRequest, TResult>(string url, TRequest data);
    Task<TResult?> PutAsync<TRequest, TResult>(string url, TRequest data);
    Task DeleteAsync(string url);
}
```

### 6.5 数据流程示例

```csharp
// ========== Step 1: ViewModel调用Repository ==========
public async Task SaveAsync()
{
    var dto = new UpdateConsultationDto
    {
        Id = MedicalCaseId,
        ChiefComplaint = ChiefComplaint,
        TCMDiagnosis = TCMDiagnosis
        // ...其他字段
    };

    // ViewModel → Repository
    await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);
}

// ========== Step 2: Repository实现（MedicalCaseRepository）==========
public async Task<ConsultationDto> UpdateConsultationAsync(
    Guid caseId,
    UpdateConsultationDto dto)
{
    // Repository → BaseApiRepository → IApiService
    var url = $"medicalcases/{caseId}/consultation";
    return await _apiService.PutAsync<UpdateConsultationDto, ConsultationDto>(url, dto);
}

// ========== Step 3: ApiService发送HTTP请求 ==========
public async Task<TResult?> PutAsync<TRequest, TResult>(string url, TRequest data)
{
    // ApiService → HttpClient → Server API
    var json = JsonSerializer.Serialize(data);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var response = await _httpClient.PutAsync(url, content);
    response.EnsureSuccessStatusCode();

    var responseJson = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<TResult>(responseJson);
}

// ========== Step 4: Server API处理请求 ==========
// PUT /api/v1/medicalcases/{id}/consultation
[HttpPut("{id}/consultation")]
public async Task<IActionResult> UpdateConsultation(
    Guid id,
    [FromBody] UpdateConsultationDto dto)
{
    var result = await _medicalCaseService.UpdateConsultationAsync(id, dto);
    return Ok(result);
}
```

---

## 7. UI设计与XAML绑定

### 7.1 ConsultationFormView布局结构

```xml
<!-- ConsultationFormView.xaml -->
<UserControl x:Class="LYBT.Desktop.Consultation.Views.ConsultationFormView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/">

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="20">

            <!-- 患者信息栏 -->
            <StackPanel Orientation="Horizontal" Margin="0,0,0,20">
                <md:PackIcon Kind="AccountCircle" Width="32" Height="32"
                             VerticalAlignment="Center" Margin="0,0,10,0" />
                <TextBlock Text="{Binding CurrentPatient.Name}"
                           Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           VerticalAlignment="Center" />
            </StackPanel>

            <!-- Step1完成标记 -->
            <TextBlock Text="{Binding Step1CompletedAtText}"
                       Foreground="Green"
                       FontWeight="Bold"
                       Visibility="{Binding Step1CompletedAtVisibility}"
                       Margin="0,0,0,20" />

            <!-- 必填项: 主诉 -->
            <TextBox md:HintAssist.Hint="主诉（必填）"
                     Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     Margin="0,10,0,0" />

            <!-- 现病史 -->
            <TextBox md:HintAssist.Hint="现病史"
                     Text="{Binding PresentIllness, UpdateSourceTrigger=PropertyChanged}"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     AcceptsReturn="True"
                     Height="80"
                     TextWrapping="Wrap"
                     VerticalScrollBarVisibility="Auto"
                     Margin="0,10,0,0" />

            <!-- 必填项: 中医诊断 -->
            <TextBox md:HintAssist.Hint="中医诊断（必填）"
                     Text="{Binding TCMDiagnosis, UpdateSourceTrigger=PropertyChanged}"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     Margin="0,10,0,0" />

            <!-- 治疗原则 -->
            <TextBox md:HintAssist.Hint="治疗原则"
                     Text="{Binding TreatmentPrinciple, UpdateSourceTrigger=PropertyChanged}"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     Margin="0,10,0,0" />

            <!-- 中医四诊 -->
            <GroupBox Header="四诊合参" Margin="0,20,0,0"
                      Style="{StaticResource MaterialDesignCardGroupBox}">
                <StackPanel Margin="10">

                    <!-- 望诊 -->
                    <TextBox md:HintAssist.Hint="望诊（观察面色、舌象）"
                             Text="{Binding Inspection, UpdateSourceTrigger=PropertyChanged}"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             Height="60"
                             TextWrapping="Wrap"
                             Margin="0,10,0,0" />

                    <!-- 闻诊 -->
                    <TextBox md:HintAssist.Hint="闻诊（听声音、嗅气味）"
                             Text="{Binding AuscultationOlfaction, UpdateSourceTrigger=PropertyChanged}"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             Height="60"
                             TextWrapping="Wrap"
                             Margin="0,10,0,0" />

                    <!-- 问诊 -->
                    <TextBox md:HintAssist.Hint="问诊（询问症状、病史）"
                             Text="{Binding Inquiry, UpdateSourceTrigger=PropertyChanged}"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             Height="60"
                             TextWrapping="Wrap"
                             Margin="0,10,0,0" />

                    <!-- 切诊 -->
                    <TextBox md:HintAssist.Hint="切诊（把脉、按腹）"
                             Text="{Binding Palpation, UpdateSourceTrigger=PropertyChanged}"
                             Style="{StaticResource MaterialDesignOutlinedTextBox}"
                             AcceptsReturn="True"
                             Height="60"
                             TextWrapping="Wrap"
                             Margin="0,10,0,0" />

                </StackPanel>
            </GroupBox>

            <!-- 备注 -->
            <TextBox md:HintAssist.Hint="备注"
                     Text="{Binding Remark, UpdateSourceTrigger=PropertyChanged}"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     AcceptsReturn="True"
                     Height="60"
                     TextWrapping="Wrap"
                     Margin="0,20,0,0" />

            <!-- 验证消息 -->
            <TextBlock Text="{Binding ValidationMessage}"
                       Foreground="Red"
                       Visibility="{Binding ValidationMessage,
                                    Converter={StaticResource StringToVisibilityConverter}}"
                       Margin="0,10,0,0" />

            <!-- 操作按钮 -->
            <StackPanel Orientation="Horizontal" Margin="0,20,0,0">

                <!-- 完成诊断按钮 -->
                <Button Content="完成诊断"
                        Command="{Binding CompleteStep1Command}"
                        Style="{StaticResource MaterialDesignRaisedButton}"
                        IsEnabled="{Binding PrescriptionDisabled}">
                    <Button.CommandParameter>
                        <MultiBinding Converter="{StaticResource CompleteStep1ParameterConverter}">
                            <Binding Path="ChiefComplaint" />
                            <Binding Path="TCMDiagnosis" />
                        </MultiBinding>
                    </Button.CommandParameter>
                </Button>

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

### 7.2 ConsultationManagementView布局结构

```xml
<!-- ConsultationManagementView.xaml -->
<UserControl x:Class="LYBT.Desktop.Consultation.Views.ConsultationManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">

            <!-- 搜索框 -->
            <TextBox md:HintAssist.Hint="搜索（患者姓名/医案ID）"
                     Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                     Style="{StaticResource MaterialDesignOutlinedTextBox}"
                     Width="300"
                     Margin="0,0,10,0" />

            <!-- 搜索按钮 -->
            <Button Content="搜索"
                    Command="{Binding SearchCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}"
                    IsEnabled="{Binding CanSearch}"
                    Margin="0,0,10,0" />

            <!-- 刷新按钮 -->
            <Button Content="刷新"
                    Command="{Binding RefreshCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}" />

        </StackPanel>

        <!-- 数据表格 -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding Consultations}"
                  SelectedItem="{Binding SelectedConsultation}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True"
                  CanUserSortColumns="True"
                  CanUserResizeColumns="True"
                  Margin="10">

            <DataGrid.Columns>

                <!-- 就诊日期 -->
                <DataGridTextColumn Header="就诊日期"
                                    Binding="{Binding ConsultationDateText}"
                                    Width="150" />

                <!-- 患者姓名 -->
                <DataGridTextColumn Header="患者姓名"
                                    Binding="{Binding PatientName}"
                                    Width="100" />

                <!-- 主诉 -->
                <DataGridTextColumn Header="主诉"
                                    Binding="{Binding ChiefComplaint}"
                                    Width="*" />

                <!-- 中医诊断 -->
                <DataGridTextColumn Header="中医诊断"
                                    Binding="{Binding TCMDiagnosis}"
                                    Width="200" />

                <!-- 医生姓名 -->
                <DataGridTextColumn Header="医生"
                                    Binding="{Binding DoctorName}"
                                    Width="100" />

                <!-- 操作列 -->
                <DataGridTemplateColumn Header="操作" Width="100">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="查看详情"
                                    Command="{Binding DataContext.ViewDetailsCommand,
                                            RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                    CommandParameter="{Binding}"
                                    Style="{StaticResource MaterialDesignFlatButton}" />
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

            </DataGrid.Columns>

        </DataGrid>

        <!-- 加载指示器 -->
        <md:Card Grid.Row="1"
                 Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"
                 Background="#80000000"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center">
            <StackPanel Margin="20">
                <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}"
                             IsIndeterminate="True"
                             Width="50"
                             Height="50" />
                <TextBlock Text="加载中..."
                           Margin="0,10,0,0"
                           Foreground="White"
                           HorizontalAlignment="Center" />
            </StackPanel>
        </md:Card>

    </Grid>

</UserControl>
```

### 7.3 Material Design样式应用

```xml
<!-- App.xaml中引入Material Design资源 -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Material Design主题 -->
            <materialDesign:BundledTheme BaseTheme="Light"
                                        PrimaryColor="DeepPurple"
                                        SecondaryColor="Lime" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

---

## 8. 设计模式应用

### 8.1 接口契约模式（Interface Contract Pattern）

**核心思想**: ConsultationFormViewModel通过实现ISaveable/IValidatable接口与MedicalCaseFlowViewModel解耦，流程编排器无需知道Consultation的具体实现细节。

#### 8.1.1 ISaveable接口

```csharp
/// <summary>
/// ISaveable接口 - 保存当前步骤数据到服务器
/// </summary>
public interface ISaveable
{
    Task SaveAsync();
}
```

#### 8.1.2 IValidatable接口

```csharp
/// <summary>
/// IValidatable接口 - 验证当前步骤数据完整性
/// </summary>
public interface IValidatable
{
    bool Validate();
}
```

#### 8.1.3 ConsultationFormViewModel实现接口

```csharp
public class ConsultationFormViewModel : UnifiedViewModelBase,
                                         ISaveable,
                                         IValidatable
{
    // 验证必填项: 主诉+中医诊断
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
        var dto = new UpdateConsultationDto { /* ...字段赋值 */ };
        await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);
    }
}
```

#### 8.1.4 MedicalCaseFlowViewModel使用接口

```csharp
/// <summary>
/// 流程编排器 - 通过接口契约调用Step ViewModel
/// </summary>
public async Task ExecuteNextStepAsync()
{
    // Step 1: 验证当前步骤（接口契约）
    if (CurrentStepViewModel is IValidatable validatable)
    {
        if (!validatable.Validate())
        {
            SetWarningMessage("请完成必填项");
            return;
        }
    }

    // Step 2: 保存当前步骤（接口契约）
    if (CurrentStepViewModel is ISaveable saveable)
    {
        await saveable.SaveAsync();
    }

    // Step 3: 导航到下一步骤
    CurrentStep = ConsultationStep.Step2Prescription;
}
```

**优势**:
- ✅ 流程编排与步骤实现解耦，易于扩展和维护
- ✅ 接口契约确保一致性，所有Step ViewModel遵循相同协议
- ✅ 验证逻辑集中管理，避免在FlowViewModel中硬编码
- ✅ 支持多态，可灵活切换不同步骤实现

### 8.2 Repository模式（Repository Pattern）

**核心思想**: 采用Repository模式封装数据访问逻辑，ViewModel通过Repository接口与Server API交互，实现三层架构。

```
ConsultationFormViewModel (业务逻辑)
    ↓ 依赖注入 IMedicalCaseRepository
MedicalCaseRepository (数据访问)
    ↓ 继承 BaseApiRepository
BaseApiRepository (通用实现)
    ↓ 依赖注入 IApiService
ApiService (HTTP通信)
    ↓ HttpClient
LYBT.WebAPI (Server端)
```

**优势**:
- ✅ 数据访问逻辑集中管理，易于测试和维护
- ✅ ViewModel无需关心HTTP通信细节，专注业务逻辑
- ✅ BaseApiRepository提供标准实现，减少重复代码
- ✅ 接口抽象便于Mock，支持单元测试

### 8.3 MVVM模式（Model-View-ViewModel）

**核心思想**: 数据绑定与业务逻辑分离，View通过DataBinding与ViewModel交互，ViewModel负责业务逻辑和数据转换。

```
View (XAML)
  ↕ DataBinding
ViewModel (业务逻辑)
  ↕ Repository
Model (数据模型/DTO)
```

**优势**:
- ✅ 视图与逻辑分离，UI可独立变更
- ✅ 数据绑定自动同步，减少手动UI更新
- ✅ 易于单元测试，ViewModel可脱离UI测试
- ✅ 符合SOLID原则，职责清晰

### 8.4 观察者模式（Observer Pattern）

**核心思想**: ViewModel通过INotifyPropertyChanged接口实现属性变更通知，UI自动更新绑定数据。

```csharp
// ViewModel属性实现
private string _chiefComplaint = string.Empty;
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        if (SetProperty(ref _chiefComplaint, value))
        {
            // 通知相关属性变更
            RaisePropertyChanged(nameof(HasChiefComplaint));
            CompleteStep1Command.RaiseCanExecuteChanged();
        }
    }
}

// XAML绑定
<TextBox Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}" />
```

**优势**:
- ✅ UI自动响应数据变更，无需手动更新
- ✅ 支持复杂依赖关系，多个属性联动
- ✅ 命令CanExecute自动更新，按钮状态联动

### 8.5 命令模式（Command Pattern）

**核心思想**: 将用户操作封装为命令对象，支持异步执行、CanExecute判断、参数传递。

```csharp
// 命令定义
public AsyncDelegateCommand CompleteStep1Command { get; }

public ConsultationFormViewModel()
{
    CompleteStep1Command = new AsyncDelegateCommand(
        ExecuteCompleteStep1,
        CanExecuteCompleteStep1)
        .ObservesProperty(() => IsBusy)
        .ObservesProperty(() => PrescriptionDisabled);
}

// CanExecute判断
private bool CanExecuteCompleteStep1()
{
    return !IsBusy && PrescriptionDisabled && HasChiefComplaint && HasTCMDiagnosis;
}

// Execute执行
private async Task ExecuteCompleteStep1()
{
    // 业务逻辑
}

// XAML绑定
<Button Content="完成诊断" Command="{Binding CompleteStep1Command}" />
```

**优势**:
- ✅ UI与业务逻辑解耦，命令可复用
- ✅ 支持异步操作，不阻塞UI线程
- ✅ CanExecute自动判断，按钮状态联动
- ✅ ObservesProperty自动监听属性变更

### 8.6 策略模式（Strategy Pattern）

**核心思想**: 验证逻辑封装在IValidatable接口中，不同步骤实现不同验证策略。

```csharp
// Step1验证策略（主诉+中医诊断）
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

// 流程编排器统一调用验证策略
if (CurrentStepViewModel is IValidatable validatable)
{
    if (!validatable.Validate())
    {
        SetWarningMessage("请完成必填项");
        return;
    }
}
```

**优势**:
- ✅ 验证逻辑可灵活切换，不同步骤不同策略
- ✅ 验证逻辑封装在步骤内部，集中管理
- ✅ 流程编排器无需知道具体验证规则

---

## 9. 核心设计原则

### 9.1 ISaveable/IValidatable接口契约 - 与MedicalCase流程解耦

**核心思想**: ConsultationFormViewModel作为Step1环节，通过实现ISaveable/IValidatable接口与MedicalCaseFlowViewModel解耦，流程编排器无需知道Consultation的具体实现细节，只需调用接口方法。

**实现要点**:
- ConsultationFormViewModel实现ISaveable/IValidatable接口
- MedicalCaseFlowViewModel通过接口契约调用`Validate()`和`SaveAsync()`
- 验证逻辑封装在Consultation模块内部，主诉+中医诊断为必填项
- 保存逻辑调用`_medicalCaseRepository.UpdateConsultationAsync()`

**反例**:
```csharp
// ❌ 不要在FlowViewModel中硬编码验证逻辑
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

// ✅ 正确: 使用接口契约
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

### 9.2 中医四诊合参数据结构 - 辨证论治核心

**核心思想**: 完整实现中医四诊合参（望闻问切）数据结构，体现中医诊疗特色，支持医生综合四诊信息进行辨证论治。

**实现要点**:
- 望诊(Inspection): 观察面色、舌象、形体等外在表现
- 闻诊(AuscultationOlfaction): 听声音、嗅气味
- 问诊(Inquiry): 询问症状、病史、生活习惯等
- 切诊(Palpation): 把脉、按腹、触诊等

**诊断流程**:
```
1. 望诊: 面色萎黄，舌淡苔白
2. 闻诊: 语声低微，懒言
3. 问诊: 乏力倦怠，食少纳呆
4. 切诊: 脉细弱
5. 辨证: 气血两虚证
6. 治法: 益气养血
7. 处方: 八珍汤加减
```

### 9.3 诊断完成标记 - Step1状态管理与处方启用控制

**核心思想**: 通过Step1CompletedAt时间戳标记诊断完成状态，控制处方启用/禁用逻辑，确保诊断完成后才能开具处方。

**实现要点**:
- Step1CompletedAt为null表示诊断未完成，处方禁用
- Step1CompletedAt有值表示诊断已完成，处方启用
- 计算属性PrescriptionEnabled/PrescriptionDisabled自动更新UI状态
- CompleteStep1Command按钮验证必填项后标记完成时间

**示例**:
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

    // 标记完成时间（启用处方）
    Step1CompletedAt = DateTime.Now;
    await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId, Step1CompletedAt.Value);

    // UI自动更新:
    // - PrescriptionEnabled = true (处方按钮启用)
    // - PrescriptionDisabled = false (完成按钮禁用)
}
```

### 9.4 暂存/继续功能 - 工作流中断与恢复

**核心思想**: 支持诊断数据暂存（不完成Step1）和继续（恢复上次未完成的诊断），适应中医诊所实际工作场景。

**实现要点**:
- SaveDraftCommand保存当前数据但不标记Step1CompletedAt
- OnNavigatedTo导航生命周期方法加载医案详情并恢复所有字段
- 暂存不验证必填项，允许部分数据保存
- 继续时判断Step1CompletedAt状态，决定是否禁用完成按钮

**示例**:
```csharp
// 暂存草稿（不验证必填项）
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
        // ...其他字段

        // 恢复Step1完成状态
        Step1CompletedAt = medicalCase.Consultation.Step1CompletedAt;
    }
}
```

### 9.5 异步优先与UI响应性 - 流畅的用户体验

**核心思想**: 所有I/O操作（API调用、数据库访问）必须使用async/await异步模式，避免阻塞UI线程，确保应用响应流畅。

**实现要点**:
- 所有Repository方法返回Task<T>
- ViewModel方法标记async/await
- AsyncDelegateCommand支持异步命令绑定
- IsBusy标志显示加载动画，防止重复操作

**示例**:
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

        // 异步保存（不阻塞UI）
        await SaveAsync();

        // 异步标记完成（不阻塞UI）
        await _medicalCaseRepository.CompleteStep1Async(MedicalCaseId, DateTime.Now);

        SetSuccessMessage("诊断已完成");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "完成诊断失败");
        SetErrorMessage($"操作失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;  // 隐藏加载动画
    }
}
```

---

## 10. 模块集成与使用

### 10.1 Shell加载Consultation模块

```csharp
// App.xaml.cs
using Prism.Modularity;
using LYBT.Desktop.Consultation;

public class App : PrismApplication
{
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 注册Consultation模块（自动注册2 ViewModels + 2 Views）
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);
    }
}
```

### 10.2 ConsultationModule注册

```csharp
/// <summary>
/// Consultation模块 - 注册ViewModels与Views
/// </summary>
public class ConsultationModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块加载完成后的初始化逻辑（可选）
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Step 1: 注册ViewModels（MVVM视图模型）
        containerRegistry.Register<ConsultationFormViewModel>();
        containerRegistry.Register<ConsultationManagementViewModel>();

        // Step 2: 注册Views（WPF视图）
        containerRegistry.RegisterForNavigation<ConsultationFormView, ConsultationFormViewModel>();
        containerRegistry.RegisterForNavigation<ConsultationManagementView, ConsultationManagementViewModel>();
    }
}
```

### 10.3 导航到ConsultationFormView

```csharp
// 从MedicalCase流程导航到Consultation表单
public void NavigateToConsultationForm(Guid medicalCaseId)
{
    var parameters = new NavigationParameters
    {
        { "MedicalCaseId", medicalCaseId }
    };

    _regionManager.RequestNavigate("ContentRegion", "ConsultationFormView", parameters);
}
```

### 10.4 MedicalCaseFlowViewModel集成Consultation

```csharp
/// <summary>
/// 医案流程编排器 - 集成Consultation为Step1
/// </summary>
public class MedicalCaseFlowViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;

    // Step 1: 导航到Consultation表单
    public void NavigateToStep1()
    {
        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", MedicalCaseId }
        };

        _regionManager.RequestNavigate("StepRegion", "ConsultationFormView", parameters);
    }

    // Step 2: 验证与保存Step1数据（通过接口契约）
    public async Task ExecuteNextStepAsync()
    {
        // 获取当前步骤ViewModel
        var currentStepViewModel = GetCurrentStepViewModel();

        // 验证数据
        if (currentStepViewModel is IValidatable validatable)
        {
            if (!validatable.Validate())
            {
                SetWarningMessage("请完成必填项");
                return;
            }
        }

        // 保存数据
        if (currentStepViewModel is ISaveable saveable)
        {
            await saveable.SaveAsync();
        }

        // 导航到下一步
        CurrentStep = ConsultationStep.Step2Prescription;
        NavigateToStep2();
    }
}
```

---

## 11. 测试策略

### 11.1 单元测试（ConsultationFormViewModel）

```csharp
/// <summary>
/// ConsultationFormViewModel单元测试
/// </summary>
public class ConsultationFormViewModelTests
{
    private Mock<IMedicalCaseRepository> _mockRepository;
    private Mock<IRegionManager> _mockRegionManager;
    private Mock<IMessageService> _mockMessageService;
    private Mock<ILogger<ConsultationFormViewModel>> _mockLogger;
    private ConsultationFormViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<IMedicalCaseRepository>();
        _mockRegionManager = new Mock<IRegionManager>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<ConsultationFormViewModel>>();

        _viewModel = new ConsultationFormViewModel(
            _mockRepository.Object,
            _mockRegionManager.Object,
            _mockMessageService.Object,
            _mockLogger.Object);
    }

    [Test]
    public void Validate_WhenChiefComplaintIsEmpty_ReturnsFalse()
    {
        // Arrange
        _viewModel.ChiefComplaint = string.Empty;
        _viewModel.TCMDiagnosis = "测试诊断";

        // Act
        var result = _viewModel.Validate();

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual("主诉不能为空", _viewModel.ValidationMessage);
    }

    [Test]
    public void Validate_WhenTCMDiagnosisIsEmpty_ReturnsFalse()
    {
        // Arrange
        _viewModel.ChiefComplaint = "测试主诉";
        _viewModel.TCMDiagnosis = string.Empty;

        // Act
        var result = _viewModel.Validate();

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual("中医诊断不能为空", _viewModel.ValidationMessage);
    }

    [Test]
    public void Validate_WhenAllRequiredFieldsFilled_ReturnsTrue()
    {
        // Arrange
        _viewModel.ChiefComplaint = "测试主诉";
        _viewModel.TCMDiagnosis = "测试诊断";

        // Act
        var result = _viewModel.Validate();

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(string.Empty, _viewModel.ValidationMessage);
    }

    [Test]
    public async Task SaveAsync_CallsRepositoryUpdateConsultationAsync()
    {
        // Arrange
        _viewModel.MedicalCaseId = Guid.NewGuid();
        _viewModel.ChiefComplaint = "测试主诉";
        _viewModel.TCMDiagnosis = "测试诊断";

        _mockRepository.Setup(r => r.UpdateConsultationAsync(
            It.IsAny<Guid>(),
            It.IsAny<UpdateConsultationDto>()))
            .ReturnsAsync(new ConsultationDto());

        // Act
        await _viewModel.SaveAsync();

        // Assert
        _mockRepository.Verify(r => r.UpdateConsultationAsync(
            It.IsAny<Guid>(),
            It.IsAny<UpdateConsultationDto>()),
            Times.Once);
    }

    [Test]
    public void Step1CompletedAt_WhenSet_UpdatesRelatedProperties()
    {
        // Arrange
        var now = DateTime.Now;
        var prescriptionEnabledChangedCount = 0;
        var prescriptionDisabledChangedCount = 0;

        _viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(ConsultationFormViewModel.PrescriptionEnabled))
                prescriptionEnabledChangedCount++;
            if (args.PropertyName == nameof(ConsultationFormViewModel.PrescriptionDisabled))
                prescriptionDisabledChangedCount++;
        };

        // Act
        _viewModel.Step1CompletedAt = now;

        // Assert
        Assert.IsTrue(_viewModel.PrescriptionEnabled);
        Assert.IsFalse(_viewModel.PrescriptionDisabled);
        Assert.AreEqual(1, prescriptionEnabledChangedCount);
        Assert.AreEqual(1, prescriptionDisabledChangedCount);
    }
}
```

### 11.2 集成测试（Repository + API）

```csharp
/// <summary>
/// Consultation集成测试
/// </summary>
[TestFixture]
public class ConsultationIntegrationTests
{
    private TestServer _testServer;
    private HttpClient _httpClient;
    private IApiService _apiService;
    private IMedicalCaseRepository _repository;

    [SetUp]
    public void Setup()
    {
        // 创建测试服务器
        _testServer = new TestServer(new WebHostBuilder()
            .UseStartup<Startup>());

        _httpClient = _testServer.CreateClient();
        _apiService = new ApiService(_httpClient);
        _repository = new MedicalCaseRepository(_apiService, Mock.Of<ILogger>());
    }

    [Test]
    public async Task UpdateConsultationAsync_WhenCalled_UpdatesConsultationData()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var dto = new UpdateConsultationDto
        {
            Id = medicalCaseId,
            ChiefComplaint = "集成测试主诉",
            TCMDiagnosis = "集成测试诊断"
        };

        // Act
        var result = await _repository.UpdateConsultationAsync(medicalCaseId, dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("集成测试主诉", result.ChiefComplaint);
        Assert.AreEqual("集成测试诊断", result.TcmDiagnosis);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _testServer?.Dispose();
    }
}
```

### 11.3 UI测试（Prism导航）

```csharp
/// <summary>
/// Consultation UI测试
/// </summary>
[TestFixture]
public class ConsultationUITests
{
    private Mock<IRegionManager> _mockRegionManager;
    private Mock<IMedicalCaseRepository> _mockRepository;
    private ConsultationFormViewModel _viewModel;

    [SetUp]
    public void Setup()
    {
        _mockRegionManager = new Mock<IRegionManager>();
        _mockRepository = new Mock<IMedicalCaseRepository>();

        _viewModel = new ConsultationFormViewModel(
            _mockRepository.Object,
            _mockRegionManager.Object,
            Mock.Of<IMessageService>(),
            Mock.Of<ILogger<ConsultationFormViewModel>>());
    }

    [Test]
    public async Task OnNavigatedTo_LoadsMedicalCaseWithConsultation()
    {
        // Arrange
        var medicalCaseId = Guid.NewGuid();
        var medicalCase = new MedicalCaseDto
        {
            Id = medicalCaseId,
            Consultation = new ConsultationDto
            {
                ChiefComplaint = "测试主诉",
                TcmDiagnosis = "测试诊断"
            }
        };

        _mockRepository.Setup(r => r.GetByIdWithDetailsAsync(medicalCaseId))
            .ReturnsAsync(medicalCase);

        var parameters = new NavigationParameters
        {
            { "MedicalCaseId", medicalCaseId }
        };
        var context = new NavigationContext(
            Mock.Of<IRegionNavigationService>(),
            new Uri("ConsultationFormView", UriKind.Relative),
            parameters);

        // Act
        await _viewModel.OnNavigatedTo(context);

        // Assert
        Assert.AreEqual(medicalCaseId, _viewModel.MedicalCaseId);
        Assert.AreEqual("测试主诉", _viewModel.ChiefComplaint);
        Assert.AreEqual("测试诊断", _viewModel.TCMDiagnosis);
    }
}
```

---

## 12. 性能优化

### 12.1 异步加载策略

```csharp
// ✅ 正确: 异步加载数据，不阻塞UI
public async Task OnNavigatedTo(NavigationContext navigationContext)
{
    try
    {
        IsBusy = true;  // 显示加载动画

        // 异步加载医案详情
        var medicalCase = await _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId);

        // 恢复数据
        if (medicalCase?.Consultation != null)
        {
            ChiefComplaint = medicalCase.Consultation.ChiefComplaint ?? string.Empty;
            // ...其他字段
        }
    }
    finally
    {
        IsBusy = false;  // 隐藏加载动画
    }
}

// ❌ 错误: 同步加载数据，阻塞UI
public void OnNavigatedTo(NavigationContext navigationContext)
{
    var medicalCase = _medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId).Result;  // 阻塞UI
    // ...
}
```

### 12.2 属性变更优化

```csharp
// ✅ 正确: 只在值真正改变时通知
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        if (SetProperty(ref _chiefComplaint, value))  // SetProperty内部判断值是否变更
        {
            RaisePropertyChanged(nameof(HasChiefComplaint));
            CompleteStep1Command.RaiseCanExecuteChanged();
        }
    }
}

// ❌ 错误: 每次Set都通知，即使值未变更
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        _chiefComplaint = value;
        RaisePropertyChanged(nameof(ChiefComplaint));  // 每次都通知
        RaisePropertyChanged(nameof(HasChiefComplaint));
        CompleteStep1Command.RaiseCanExecuteChanged();
    }
}
```

### 12.3 命令CanExecute优化

```csharp
// ✅ 正确: ObservesProperty自动监听属性变更
public ConsultationFormViewModel()
{
    CompleteStep1Command = new AsyncDelegateCommand(
        ExecuteCompleteStep1,
        CanExecuteCompleteStep1)
        .ObservesProperty(() => IsBusy)               // 监听IsBusy
        .ObservesProperty(() => PrescriptionDisabled);  // 监听PrescriptionDisabled
}

// ❌ 错误: 手动调用RaiseCanExecuteChanged（易遗漏）
public string ChiefComplaint
{
    get => _chiefComplaint;
    set
    {
        if (SetProperty(ref _chiefComplaint, value))
        {
            CompleteStep1Command.RaiseCanExecuteChanged();  // 手动调用，容易遗漏
        }
    }
}
```

### 12.4 数据绑定优化

```xml
<!-- ✅ 正确: UpdateSourceTrigger=PropertyChanged，实时验证 -->
<TextBox md:HintAssist.Hint="主诉（必填）"
         Text="{Binding ChiefComplaint, UpdateSourceTrigger=PropertyChanged}"
         Style="{StaticResource MaterialDesignOutlinedTextBox}" />

<!-- ❌ 错误: UpdateSourceTrigger=LostFocus（默认），失去焦点时才更新 -->
<TextBox md:HintAssist.Hint="主诉（必填）"
         Text="{Binding ChiefComplaint}"
         Style="{StaticResource MaterialDesignOutlinedTextBox}" />
```

---

## 13. 安全性考虑

### 13.1 输入验证

```csharp
// ✅ 正确: 在ViewModel中验证输入
public bool Validate()
{
    if (string.IsNullOrWhiteSpace(ChiefComplaint))
    {
        ValidationMessage = "主诉不能为空";
        return false;
    }

    if (ChiefComplaint.Length > 500)
    {
        ValidationMessage = "主诉长度不能超过500个字符";
        return false;
    }

    return true;
}
```

### 13.2 敏感数据处理

```csharp
// ✅ 正确: 敏感数据不记录到日志
catch (Exception ex)
{
    _logger.LogError(ex, "保存诊断数据失败");  // 不记录敏感字段
    throw;
}

// ❌ 错误: 敏感数据记录到日志
catch (Exception ex)
{
    _logger.LogError(ex, $"保存诊断数据失败: ChiefComplaint={ChiefComplaint}");  // 泄露敏感数据
    throw;
}
```

### 13.3 异常处理

```csharp
// ✅ 正确: 完整的异常处理
private async Task ExecuteCompleteStep1()
{
    try
    {
        IsBusy = true;

        // 业务逻辑
        await SaveAsync();
    }
    catch (ValidationException vex)
    {
        SetWarningMessage(vex.Message);  // 显示验证错误
    }
    catch (HttpRequestException hex)
    {
        _logger.LogError(hex, "网络请求失败");
        SetErrorMessage("网络连接失败，请检查网络后重试");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "完成诊断失败");
        SetErrorMessage($"操作失败: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

---

## 14. 未来扩展

### 14.1 支持更多诊断模板

**需求**: 预定义常见病症的诊断模板，加快录入速度。

**实现方案**:
```csharp
/// <summary>
/// 诊断模板
/// </summary>
public class ConsultationTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }                    // 模板名称（如："感冒辨证模板"）
    public string TCMDiagnosis { get; set; }            // 中医诊断
    public string TreatmentPrinciple { get; set; }      // 治疗原则
    public string InspectionTemplate { get; set; }      // 望诊模板
    public string AuscultationTemplate { get; set; }    // 闻诊模板
    public string InquiryTemplate { get; set; }         // 问诊模板
    public string PalpationTemplate { get; set; }       // 切诊模板
}

// ConsultationFormViewModel增加模板选择
public ObservableCollection<ConsultationTemplate> Templates { get; } = new();

private async Task ExecuteSelectTemplate(ConsultationTemplate template)
{
    if (template == null) return;

    // 应用模板数据
    TCMDiagnosis = template.TCMDiagnosis;
    TreatmentPrinciple = template.TreatmentPrinciple;
    Inspection = template.InspectionTemplate;
    AuscultationOlfaction = template.AuscultationTemplate;
    Inquiry = template.InquiryTemplate;
    Palpation = template.PalpationTemplate;

    SetSuccessMessage($"已应用模板: {template.Name}");
}
```

### 14.2 支持语音录入

**需求**: 通过语音识别快速录入四诊信息，提高录入效率。

**实现方案**:
```csharp
/// <summary>
/// 语音识别服务接口
/// </summary>
public interface ISpeechRecognitionService
{
    Task<string> RecognizeAsync(byte[] audioData);
}

// ConsultationFormViewModel增加语音录入
public AsyncDelegateCommand<string> StartVoiceInputCommand { get; }

private async Task ExecuteStartVoiceInput(string fieldName)
{
    try
    {
        // 开始录音
        var audioData = await _audioRecorder.RecordAsync();

        // 语音识别
        var recognizedText = await _speechRecognitionService.RecognizeAsync(audioData);

        // 填充到对应字段
        switch (fieldName)
        {
            case "Inspection":
                Inspection = recognizedText;
                break;
            case "Inquiry":
                Inquiry = recognizedText;
                break;
            // ...其他字段
        }

        SetSuccessMessage("语音录入成功");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "语音录入失败");
        SetErrorMessage("语音录入失败");
    }
}
```

### 14.3 支持离线模式

**需求**: 在网络不稳定时支持离线录入，网络恢复后自动同步。

**实现方案**:
```csharp
/// <summary>
/// 离线存储服务接口
/// </summary>
public interface IOfflineStorageService
{
    Task SaveLocalAsync<T>(string key, T data);
    Task<T?> LoadLocalAsync<T>(string key);
    Task<bool> HasPendingSyncAsync();
    Task SyncAllAsync();
}

// ConsultationFormViewModel增加离线支持
public async Task SaveAsync()
{
    try
    {
        var dto = new UpdateConsultationDto { /* ...字段赋值 */ };

        if (await _networkService.IsOnlineAsync())
        {
            // 在线模式: 直接保存到Server
            await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);
        }
        else
        {
            // 离线模式: 保存到本地
            await _offlineStorageService.SaveLocalAsync($"consultation_{MedicalCaseId}", dto);
            SetWarningMessage("当前离线，数据已保存到本地，将在网络恢复后自动同步");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "保存诊断数据失败");
        throw;
    }
}

// 网络恢复后自动同步
private async Task OnNetworkStatusChanged(bool isOnline)
{
    if (isOnline && await _offlineStorageService.HasPendingSyncAsync())
    {
        await _offlineStorageService.SyncAllAsync();
        SetSuccessMessage("离线数据已同步到服务器");
    }
}
```

### 14.4 支持多医生协同

**需求**: 支持多位医生同时编辑同一个医案的诊断信息（乐观锁并发控制）。

**实现方案**:
```csharp
/// <summary>
/// 乐观锁并发控制
/// </summary>
public class ConsultationDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }  // 版本号（乐观锁）
    // ...其他字段
}

// ConsultationFormViewModel增加并发控制
public async Task SaveAsync()
{
    try
    {
        var dto = new UpdateConsultationDto
        {
            Id = MedicalCaseId,
            Version = _currentVersion,  // 提交时带上版本号
            // ...其他字段
        };

        var result = await _medicalCaseRepository.UpdateConsultationAsync(MedicalCaseId, dto);

        // 更新本地版本号
        _currentVersion = result.Version;
    }
    catch (ConcurrencyException cex)
    {
        // 并发冲突: 提示用户刷新数据
        SetWarningMessage("数据已被其他用户修改，请刷新后重试");
        await RefreshDataAsync();
    }
}
```

---

## 15. 总结

**LYBT.Desktop.Consultation** 模块是Client端WPF桌面应用的核心业务模块，负责提供中医诊疗管理功能。通过完整的中医四诊合参数据结构、ISaveable/IValidatable接口契约、诊断完成标记、暂存/继续功能、Repository模式与三层架构、异步优先与UI响应性等设计，实现了一个功能完整、架构清晰、易于扩展的诊疗管理模块。

**核心优势**:
- ✅ **中医特色**: 完整的望闻问切四诊合参数据结构
- ✅ **接口契约**: 通过ISaveable/IValidatable与MedicalCase流程解耦
- ✅ **状态管理**: Step1完成标记控制处方启用逻辑
- ✅ **工作流支持**: 暂存/继续功能适应实际诊疗场景
- ✅ **三层架构**: Repository模式标准化数据访问
- ✅ **异步优先**: 全异步方法确保UI响应流畅
- ✅ **Material Design**: 现代化用户界面体验
- ✅ **Prism MVVM**: 模块化、依赖注入、区域导航

**关键技术**:
- **.NET 8** + **WPF** + **Prism.DryIoc 8.x** + **MaterialDesignThemes 5.1**
- **MVVM模式** + **Repository模式** + **接口契约模式**
- **async/await异步编程** + **INotifyPropertyChanged观察者模式**

**文档维护**: 本文档应随代码演进持续更新，确保架构设计与实现保持一致。

---

**最后更新**: 2025-10-30
**文档版本**: v1.0.0
**维护负责**: Client端开发组
