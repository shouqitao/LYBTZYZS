# Consultation模块 - 诊疗管理

## 📦 模块定位

### Server端
- **层级**: Server端（三层对齐架构）
- **位置**: `src/Server/Modules/LYBT.Module.Consultation`
- **职责**: 提供中医四诊合参（望闻问切）的诊断数据管理，支持诊断记录的创建、查询、更新，作为MedicalCase聚合根的一部分，与MedicalCase共享主键（1:1关系），实现辨证论治核心业务逻辑。

### Client端
- **层级**: Client端（Desktop WPF）
- **位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Consultation`
- **职责**: 为医生提供中医四诊合参（望、闻、问、切）的诊断记录界面，支持诊断数据录入、辨证论治、诊断完成标记、暂存/继续功能，并作为医案流程的Step1环节与MedicalCase模块集成，通过ISaveable/IValidatable接口契约与流程编排器解耦。

---

## 🎯 功能概述

Consultation模块负责中医诊疗管理，核心功能包括：

1. **中医四诊录入**: 完整的望闻问切（Inspection/AuscultationOlfaction/Inquiry/Palpation）数据结构
2. **辨证论治**: 记录中医诊断（TCMDiagnosis）和治法（TreatmentPrinciple）
3. **诊断完成标记**: 通过Step1CompletedAt时间戳控制处方启用状态
4. **暂存与继续**: 支持诊断数据暂存和恢复，适应中断工作流
5. **患者病史查询**: 通过MedicalCase关联查询患者历史诊断记录
6. **接口契约集成**: 通过ISaveable/IValidatable接口与MedicalCase流程编排器解耦

---

## 🏗️ 模块架构

### Server端架构

```
┌─────────────────────────────────────────────────────────────┐
│                LYBT.Module.Consultation (Server)             │
│                       (诊疗管理模块)                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ 三层对齐架构
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│ Controllers  │─────►│   Services   │─────►│ Repositories │
│              │      │              │      │              │
├──────────────┤      ├──────────────┤      ├──────────────┤
│Consultation  │      │Consultation  │      │Consultation  │
│Controller    │      │Service       │      │Repository    │
│              │      │2个方法        │      │6个方法       │
│8个端点       │      │              │      │              │
│              │      │GetByIdAsync  │      │GetByPatient  │
│GET/POST/     │      │GetByMedical  │      │IdAsync       │
│PUT/DELETE    │      │CaseIdAsync   │      │GetPagedWith  │
│              │      │              │      │DetailsAsync  │
└──────────────┘      └──────────────┘      │GetByIdWith   │
                                            │DetailsAsync  │
                                            │GetByMedical  │
                                            │CaseIdAsync   │
                                            │GetAllAsync   │
                                            │FindAsync     │
                                            └──────────────┘
                                                    │
                                                    │ EF Core
                                                    ▼
                                            ┌──────────────┐
                                            │SQL Server DB │
                                            │Consultations │
                                            │表            │
                                            └──────────────┘

┌──────────────────────────────────────────────────────────┐
│            Consultation与MedicalCase关系设计              │
├──────────────────────────────────────────────────────────┤
│ 共享主键设计:                                             │
│   Consultation.Id = MedicalCase.Id                       │
│                                                          │
│ EF Core一对一关系配置:                                    │
│   modelBuilder.Entity<Consultation>()                    │
│       .HasOne(c => c.MedicalCase)                        │
│       .WithOne(mc => mc.Consultation)                    │
│       .HasForeignKey<Consultation>(c => c.Id)            │
│       .OnDelete(DeleteBehavior.Cascade);                 │
└──────────────────────────────────────────────────────────┘
```

### Client端架构

```
┌─────────────────────────────────────────────────────────────┐
│              LYBT.Desktop.Consultation (Client)              │
│                       (诊疗管理模块)                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ MVVM + Prism架构
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
```

### 核心组件说明

**Server端**:
- **ConsultationService**: 2个方法（GetByIdAsync, GetByMedicalCaseIdAsync），查询为主，创建由MedicalCase完成
- **ConsultationRepository**: 6个方法（GetByPatientIdAsync, GetPagedWithDetailsAsync, GetByIdWithDetailsAsync, GetByMedicalCaseIdAsync, GetAllAsync, FindAsync）
- **Consultation实体**: 共享MedicalCase主键，包含中医四诊字段（望闻问切）

**Client端**:
- **ConsultationFormViewModel**: 607行，21属性+7方法，实现ISaveable/IValidatable接口
- **ConsultationManagementViewModel**: 198行，9属性+6方法，管理诊断记录列表
- **IConsultationRepository**: 继承IBaseRepository，通过BaseApiRepository与Server API通信

---

## 🔧 核心功能

### 1. 中医四诊录入与验证（ConsultationFormViewModel）

**Server端实现**（Repository方法）:

```csharp
// ConsultationRepository.cs - 获取包含四诊详情的诊断记录
public async Task<ConsultationDto?> GetByIdWithDetailsAsync(Guid id)
{
    var consultation = await _dbSet
        .Include(c => c.MedicalCase)
            .ThenInclude(mc => mc.Patient)
        .Include(c => c.MedicalCase)
            .ThenInclude(mc => mc.Doctor)
        .FirstOrDefaultAsync(c => c.Id == id);

    if (consultation == null)
        return null;

    return new ConsultationDto
    {
        Id = consultation.Id,
        MedicalCaseId = consultation.MedicalCaseId,
        // 中医四诊字段
        Inspection = consultation.Inspection,                  // 望诊
        AuscultationOlfaction = consultation.AuscultationOlfaction,  // 闻诊
        Inquiry = consultation.Inquiry,                        // 问诊
        Palpation = consultation.Palpation,                    // 切诊
        // 诊断与治法
        ChiefComplaint = consultation.ChiefComplaint,          // 主诉
        PresentIllness = consultation.PresentIllness,          // 现病史
        TcmDiagnosis = consultation.TcmDiagnosis,              // 中医诊断
        TreatmentMethod = consultation.TreatmentMethod,        // 治法
        Notes = consultation.Notes                             // 备注
    };
}
```

**Client端实现**（ViewModel数据录入与验证）:

```csharp
// ConsultationFormViewModel.cs - 中医四诊数据属性与验证
public class ConsultationFormViewModel : UnifiedViewModelBase, ISaveable, IValidatable
{
    // 中医四诊属性(望闻问切)
    public string ChiefComplaint { get; set; } = string.Empty;         // 主诉(必填)
    public string PresentIllness { get; set; } = string.Empty;         // 现病史
    public string TCMDiagnosis { get; set; } = string.Empty;           // 中医诊断(必填)
    public string TreatmentPrinciple { get; set; } = string.Empty;     // 治法
    public string Inspection { get; set; } = string.Empty;             // 望诊
    public string AuscultationOlfaction { get; set; } = string.Empty;  // 闻诊
    public string Inquiry { get; set; } = string.Empty;                // 问诊
    public string Palpation { get; set; } = string.Empty;              // 切诊
    public string Remark { get; set; } = string.Empty;                 // 备注

    // 验证必填项:主诉+中医诊断 (IValidatable接口实现)
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

        ValidationMessage = string.Empty;
        return true;
    }

    // 保存诊断数据到Server (ISaveable接口实现)
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
        _logger.LogInformation($"诊断数据已保存:MedicalCaseId={MedicalCaseId}");
    }
}
```

**验证规则**:
- ✅ **必填项**: 主诉（ChiefComplaint）、中医诊断（TCMDiagnosis）
- ✅ **可选项**: 现病史、治法、四诊记录（望闻问切）、备注

---

### 2. 辨证论治流程（三步工作流）

**Server端实现**（三步工作流标记）:

```csharp
// ConsultationService.cs - 完成Step1辩证
public async Task CompleteStep1Async(
    Guid consultationId,
    string tcmDiagnosis,
    string treatmentPrinciple)
{
    var consultation = await _repository.GetByIdAsync(consultationId);

    // 验证四诊信息已记录
    if (string.IsNullOrWhiteSpace(consultation.Inspection) &&
        string.IsNullOrWhiteSpace(consultation.Inquiry) &&
        string.IsNullOrWhiteSpace(consultation.Palpation))
    {
        throw new ValidationException("请先完成四诊信息录入");
    }

    consultation.TCMDiagnosis = tcmDiagnosis;
    consultation.TreatmentPrinciple = treatmentPrinciple;
    consultation.Step1CompletedAt = DateTime.Now;  // 标记Step1完成

    await _repository.UpdateAsync(consultation);
}

// 完成Step2施治（开具处方或非药物治疗）
public async Task CompleteStep2Async(Guid consultationId)
{
    var consultation = await _repository.GetByIdAsync(consultationId);

    // 验证Step1已完成
    if (!consultation.Step1CompletedAt.HasValue)
    {
        throw new ValidationException("请先完成辩证步骤");
    }

    consultation.Step2CompletedAt = DateTime.Now;
    await _repository.UpdateAsync(consultation);
}

// 完成Step3总结（记录医嘱、注意事项）
public async Task CompleteStep3Async(
    Guid consultationId,
    string medicalAdvice)
{
    var consultation = await _repository.GetByIdAsync(consultationId);

    // 验证Step2已完成
    if (!consultation.Step2CompletedAt.HasValue)
    {
        throw new ValidationException("请先完成施治步骤");
    }

    consultation.MedicalAdvice = medicalAdvice;
    consultation.Step3CompletedAt = DateTime.Now;

    await _repository.UpdateAsync(consultation);
}
```

**Client端实现**（诊断完成标记与处方启用控制）:

```csharp
// ConsultationFormViewModel.cs - 诊断完成标记
public class ConsultationFormViewModel : UnifiedViewModelBase
{
    // Step1完成时间属性
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

    // 计算属性 - 处方启用(诊断完成后)
    public bool PrescriptionEnabled => Step1CompletedAt.HasValue;

    // 计算属性 - 处方禁用(诊断未完成)
    public bool PrescriptionDisabled => !Step1CompletedAt.HasValue;

    // UI显示文本
    public string Step1CompletedAtText =>
        Step1CompletedAt.HasValue
            ? $"诊断已完成于:{Step1CompletedAt.Value:yyyy-MM-dd HH:mm:ss}"
            : string.Empty;

    // 完成Step1诊断，标记完成时间，启用处方
    private async Task ExecuteCompleteStep1()
    {
        try
        {
            IsBusy = true;

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

            // UI自动更新:
            // - PrescriptionEnabled = true (处方按钮启用)
            // - PrescriptionDisabled = false (完成按钮禁用)
            // - Step1CompletedAtText = "诊断已完成于:2024-01-15 10:30:00"
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

**三步工作流说明**:
1. **Step1 辩证**: 记录四诊信息 → 中医诊断 → 治法 → 标记Step1CompletedAt → 启用处方
2. **Step2 施治**: 开具处方或非药物治疗 → 标记Step2CompletedAt
3. **Step3 总结**: 记录医嘱、注意事项 → 标记Step3CompletedAt → 完成诊疗

---

### 3. 暂存与继续功能（工作流中断与恢复）

**Client端实现**（暂存草稿）:

```csharp
// ConsultationFormViewModel.cs - 暂存草稿(不完成Step1)
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
```

**Client端实现**（恢复数据）:

```csharp
// ConsultationFormViewModel.cs - 导航生命周期，加载医案详情并恢复数据
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
    finally
    {
        IsBusy = false;
    }
}
```

**暂存与继续说明**:
- ✅ **暂存功能**: SaveDraftCommand保存当前数据但不标记Step1CompletedAt，不验证必填项
- ✅ **继续功能**: OnNavigatedTo加载医案详情并恢复所有字段，判断Step1CompletedAt状态
- ✅ **防止数据丢失**: 医生可随时暂存当前进度，支持诊疗节奏灵活性

---

### 4. 患者病史查询（历史诊断记录）

**Server端实现**（Repository方法）:

```csharp
// ConsultationRepository.cs - 获取患者所有诊断记录
public async Task<List<ConsultationDto>> GetByPatientIdAsync(Guid patientId)
{
    var consultations = await _dbSet
        .Include(c => c.MedicalCase)
            .ThenInclude(mc => mc.Patient)
        .Include(c => c.MedicalCase)
            .ThenInclude(mc => mc.Doctor)
        .Where(c => c.MedicalCase.PatientId == patientId)
        .OrderByDescending(c => c.CreatedAt)  // 按时间倒序
        .ToListAsync();

    return consultations.Select(c => new ConsultationDto
    {
        Id = c.Id,
        MedicalCaseId = c.MedicalCaseId,
        ChiefComplaint = c.ChiefComplaint,
        TcmDiagnosis = c.TcmDiagnosis,
        TreatmentMethod = c.TreatmentMethod,
        Inspection = c.Inspection,
        AuscultationOlfaction = c.AuscultationOlfaction,
        Inquiry = c.Inquiry,
        Palpation = c.Palpation,
        CreatedAt = c.CreatedAt,
        Patient = new PatientDto
        {
            Id = c.MedicalCase.Patient.Id,
            Name = c.MedicalCase.Patient.Name
        },
        Doctor = new DoctorDto
        {
            Id = c.MedicalCase.Doctor.Id,
            Name = c.MedicalCase.Doctor.Name
        }
    }).ToList();
}
```

**Client端实现**（查看患者历史诊断）:

```csharp
// ConsultationFormViewModel.cs - 显示患者其他医案（辅助诊断参考）
private void ExecuteShowOtherCasesQuery()
{
    if (CurrentPatient == null) return;

    // 导航到患者病史查询页面
    var parameters = new NavigationParameters
    {
        { "PatientId", CurrentPatient.Id },
        { "PatientName", CurrentPatient.Name }
    };

    _regionManager.RequestNavigate("ContentRegion", "PatientCaseHistoryView", parameters);
}
```

**病史查询说明**:
- ✅ **按患者ID查询**: 获取患者所有历史诊断记录
- ✅ **时间倒序排列**: 最新的诊断记录排在最前
- ✅ **Include预加载**: 一次性加载Patient和Doctor关联数据，避免N+1查询
- ✅ **辅助诊断参考**: 医生可查看患者历史诊断，辅助当前诊断决策

---

### 5. ISaveable/IValidatable接口契约（与MedicalCase流程集成）

**接口定义**（Desktop.Contracts）:

```csharp
// ISaveable.cs - 保存接口
public interface ISaveable
{
    /// <summary>
    /// 保存当前步骤数据到服务器
    /// </summary>
    Task SaveAsync();
}

// IValidatable.cs - 验证接口
public interface IValidatable
{
    /// <summary>
    /// 验证当前步骤数据完整性
    /// </summary>
    /// <returns>true=验证通过, false=验证失败</returns>
    bool Validate();
}
```

**ConsultationFormViewModel实现接口**:

```csharp
// ConsultationFormViewModel.cs - 实现接口契约
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
```

**MedicalCaseFlowViewModel使用接口**（流程编排器）:

```csharp
// MedicalCaseFlowViewModel.cs - 使用接口契约(无需知道具体类型)
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

**接口契约优势**:
- ✅ **解耦流程编排与步骤实现**: MedicalCaseFlowViewModel无需知道Consultation具体实现
- ✅ **统一协议**: 所有Step ViewModel遵循相同接口契约（ISaveable + IValidatable）
- ✅ **易于扩展**: 新增Step只需实现接口，无需修改FlowViewModel

---

### 6. 诊断列表管理（ConsultationManagementViewModel）

**Client端实现**（列表加载与搜索）:

```csharp
// ConsultationManagementViewModel.cs - 诊断列表管理
public class ConsultationManagementViewModel : UnifiedViewModelBase
{
    private readonly IConsultationRepository _consultationApi;

    // 数据集合与状态
    public ObservableCollection<ConsultationItem> Consultations { get; } = new();
    public ConsultationItem? SelectedConsultation { get; set; }
    public string SearchKeyword { get; set; } = string.Empty;
    public bool IsLoading { get; set; }

    // 命令定义
    public AsyncDelegateCommand LoadDataCommand { get; }
    public AsyncDelegateCommand RefreshCommand { get; }
    public AsyncDelegateCommand SearchCommand { get; }
    public DelegateCommand<ConsultationItem> ViewDetailsCommand { get; }

    // 初始化 - 加载数据
    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    // 加载诊断记录列表
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
        finally
        {
            IsLoading = false;
        }
    }

    // 查看诊断详情
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

**列表管理说明**:
- ✅ **分页加载**: GetPagedAsync获取诊断列表（默认1-100条）
- ✅ **搜索功能**: 按患者姓名/医案ID搜索
- ✅ **查看详情**: 导航到ConsultationFormView查看/编辑诊断详情

---

## 📋 业务规则

### 中医诊疗规则

| 规则类别 | 规则描述 | 约束条件 |
|---------|---------|---------|
| **必填项验证** | 主诉、中医诊断为必填项 | ChiefComplaint、TCMDiagnosis不能为空 |
| **四诊合参** | 望闻问切为可选项，但建议完整记录 | Inspection、AuscultationOlfaction、Inquiry、Palpation |
| **辨证论治** | 中医诊断和治法形成完整链条 | TCMDiagnosis → TreatmentPrinciple |

### 三步工作流规则

| 规则类别 | 规则描述 | 约束条件 |
|---------|---------|---------|
| **Step1辩证** | 必须完成四诊信息录入、中医诊断、治法 | Step1CompletedAt标记完成时间 |
| **Step2施治** | 必须先完成Step1辩证 | Step1CompletedAt.HasValue == true |
| **Step3总结** | 必须先完成Step2施治 | Step2CompletedAt.HasValue == true |
| **处方启用** | 必须完成Step1辩证后才能开具处方 | PrescriptionEnabled = Step1CompletedAt.HasValue |

### 共享主键规则

| 规则类别 | 规则描述 | 约束条件 |
|---------|---------|---------|
| **1:1关系** | Consultation与MedicalCase共享主键 | Consultation.Id = MedicalCase.Id |
| **级联删除** | 删除MedicalCase时级联删除Consultation | OnDelete(DeleteBehavior.Cascade) |
| **创建顺序** | 先创建MedicalCase，再创建Consultation | Consultation.Id必须等于已存在的MedicalCase.Id |

### 暂存与继续规则

| 规则类别 | 规则描述 | 约束条件 |
|---------|---------|---------|
| **暂存规则** | 暂存不验证必填项，允许部分数据保存 | SaveDraftCommand不调用Validate() |
| **继续规则** | 恢复时加载所有字段，包括Step1CompletedAt状态 | OnNavigatedTo恢复所有属性 |
| **状态判断** | 根据Step1CompletedAt判断是否启用处方按钮 | PrescriptionEnabled/PrescriptionDisabled |

---

## 🔌 API 端点

### 诊断管理端点

| HTTP方法 | 端点路径 | 功能描述 | 参数说明 |
|---------|---------|---------|---------|
| GET | `/api/v1/consultations?pageIndex={x}&pageSize={y}` | 分页查询诊断列表 | pageIndex: 页码, pageSize: 每页数量 |
| GET | `/api/v1/consultations/{id}` | 根据ID查询诊断详情 | id: 诊断ID（共享MedicalCase ID） |
| GET | `/api/v1/consultations/medical-case/{medicalCaseId}` | 根据医案ID查询诊断 | medicalCaseId: 医案ID |
| GET | `/api/v1/consultations/patient/{patientId}` | 查询患者所有诊断记录 | patientId: 患者ID |
| POST | `/api/v1/consultations` | 创建诊断记录 | Body: CreateConsultationDto |
| PUT | `/api/v1/consultations/{id}` | 更新诊断记录 | id: 诊断ID, Body: UpdateConsultationDto |
| PUT | `/api/v1/consultations/{id}/complete-step1` | 完成Step1辩证 | id: 诊断ID, Body: CompleteStep1Dto |
| DELETE | `/api/v1/consultations/{id}` | 删除诊断记录（软删除） | id: 诊断ID |

### DTO定义

#### CreateConsultationDto
```csharp
public class CreateConsultationDto
{
    public Guid MedicalCaseId { get; set; }          // 医案ID（必需，共享主键）
    public string? ChiefComplaint { get; set; }      // 主诉
    public string? PresentIllness { get; set; }      // 现病史
    public string? TcmDiagnosis { get; set; }        // 中医诊断
    public string? TreatmentMethod { get; set; }     // 治法
    public string? Inspection { get; set; }          // 望诊
    public string? AuscultationOlfaction { get; set; } // 闻诊
    public string? Inquiry { get; set; }             // 问诊
    public string? Palpation { get; set; }           // 切诊
    public string? Notes { get; set; }               // 备注
}
```

#### UpdateConsultationDto
```csharp
public class UpdateConsultationDto
{
    public string? ChiefComplaint { get; set; }      // 主诉
    public string? PresentIllness { get; set; }      // 现病史
    public string? TcmDiagnosis { get; set; }        // 中医诊断
    public string? TreatmentMethod { get; set; }     // 治法
    public string? Inspection { get; set; }          // 望诊
    public string? AuscultationOlfaction { get; set; } // 闻诊
    public string? Inquiry { get; set; }             // 问诊
    public string? Palpation { get; set; }           // 切诊
    public string? Notes { get; set; }               // 备注
}
```

#### CompleteStep1Dto
```csharp
public class CompleteStep1Dto
{
    public string TcmDiagnosis { get; set; }         // 中医诊断（必填）
    public string TreatmentPrinciple { get; set; }   // 治法（必填）
    public DateTime Step1CompletedAt { get; set; }   // 完成时间
}
```

---

## 🎯 设计原则

### Server端设计原则

#### 1. 共享主键设计 - Consultation与MedicalCase的1:1关系

**核心思想**：Consultation与MedicalCase共享主键，通过EF Core一对一关系配置实现1:1关联，确保一个MedicalCase只有一个Consultation。

**实现要点**:
```csharp
// Consultation与MedicalCase共享主键
public class Consultation : BaseEntity
{
    // Id字段与MedicalCase共享主键
    [Required]
    public virtual MedicalCase MedicalCase { get; set; } = null!;
}

// EF Core配置（在AppDbContext中）
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Consultation>()
        .HasOne(c => c.MedicalCase)
        .WithOne(mc => mc.Consultation)
        .HasForeignKey<Consultation>(c => c.Id)  // 共享主键
        .OnDelete(DeleteBehavior.Cascade);       // 级联删除
}

// 创建看诊时自动生成共享ID
var medicalCase = new MedicalCase { Id = Guid.NewGuid() };
var consultation = new Consultation { Id = medicalCase.Id }; // ⚠️ 共享主键
```

**优势**:
- ✅ 强制1:1关系，一个MedicalCase只有一个Consultation
- ✅ 级联删除，删除MedicalCase时自动删除Consultation
- ✅ 简化查询，通过MedicalCase.Consultation导航属性直接访问

#### 2. 三步工作流时间戳 - 辨证→施治→总结流程管理

**核心思想**：通过Step1CompletedAt、Step2CompletedAt、Step3CompletedAt三个时间戳字段管理诊疗流程，确保步骤顺序执行。

**实现要点**:
- Step1CompletedAt为null表示辩证未完成
- Step1CompletedAt有值表示辩证已完成，可进入Step2施治
- Step2CompletedAt有值表示施治已完成，可进入Step3总结
- Step3CompletedAt有值表示诊疗完成

**优势**:
- ✅ 流程可追溯，记录每个步骤的完成时间
- ✅ 强制步骤顺序，通过验证前置步骤完成状态
- ✅ 支持中断和恢复，未完成的步骤可随时继续

#### 3. 处方开关字段 - PrescriptionEnabled控制处方启用

**核心思想**：通过PrescriptionEnabled bool字段控制是否允许开具处方，默认为true，医生可根据诊断情况关闭处方功能（如仅记录诊断、非药物治疗等）。

**实现要点**:
```csharp
public class Consultation
{
    public bool PrescriptionEnabled { get; set; } = true;  // 默认允许开处方
}
```

**优势**:
- ✅ 灵活控制处方流程，适应不同诊疗场景
- ✅ 避免误操作，医生可主动关闭处方功能
- ✅ 支持非药物治疗，如针灸、推拿等

#### 4. Include预加载优化 - 避免N+1查询

**核心思想**：使用EF Core的Include/ThenInclude预加载Patient和Doctor关联数据，避免N+1查询问题。

**实现要点**:
```csharp
// ConsultationRepository.cs - 预加载关联数据
var consultation = await _dbSet
    .Include(c => c.MedicalCase)
        .ThenInclude(mc => mc.Patient)
    .Include(c => c.MedicalCase)
        .ThenInclude(mc => mc.Doctor)
    .FirstOrDefaultAsync(c => c.Id == id);
```

**优势**:
- ✅ 一次查询加载所有关联数据，避免多次数据库往返
- ✅ 性能优化，减少网络延迟和数据库负载
- ✅ 代码简洁，EF Core自动生成JOIN查询

#### 5. 软删除机制 - IsDeleted标志

**核心思想**：使用IsDeleted标志实现软删除，数据不会真正从数据库中删除，支持数据恢复和审计。

**实现要点**:
```csharp
public async Task DeleteAsync(Guid id)
{
    var consultation = await _dbSet.FindAsync(id);
    if (consultation == null)
        throw new NotFoundException("诊断记录不存在");

    consultation.IsDeleted = true;  // 软删除
    consultation.DeletedAt = DateTime.Now;

    await _dbContext.SaveChangesAsync();
}

// 查询时过滤已删除数据
var consultations = await _dbSet
    .Where(c => !c.IsDeleted)
    .ToListAsync();
```

**优势**:
- ✅ 数据可恢复，避免误删除
- ✅ 支持审计，保留删除记录和删除时间
- ✅ 数据完整性，不影响外键关联

#### 6. DTO分离 - Entity与DTO解耦

**核心思想**：使用DTO（Data Transfer Object）作为API输入输出类型，与Entity实体类解耦，避免直接暴露数据库结构。

**优势**:
- ✅ 安全性提升，不暴露敏感字段（如密码、审计字段）
- ✅ 灵活映射，DTO可以裁剪或组合多个Entity字段
- ✅ 版本兼容，DTO变更不影响Entity定义

---

### Client端设计原则

#### 1. ISaveable/IValidatable接口契约 - 与MedicalCase流程解耦

**核心思想**：ConsultationFormViewModel作为Step1环节，通过实现ISaveable/IValidatable接口与MedicalCaseFlowViewModel解耦，流程编排器无需知道Consultation的具体实现细节。

**优势**:
- ✅ 解耦流程编排与步骤实现，易于扩展和维护
- ✅ 统一协议，所有Step ViewModel遵循相同接口契约
- ✅ 验证逻辑集中管理，避免在FlowViewModel中硬编码

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

// ✅ 正确:使用接口契约
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

#### 2. 中医四诊合参数据结构 - 辨证论治核心

**核心思想**：完整实现中医四诊合参（望闻问切）数据结构，体现中医诊疗特色，支持医生综合四诊信息进行辨证论治。

**实现要点**:
- 望诊(Inspection):观察面色、舌象、形体等外在表现
- 闻诊(AuscultationOlfaction):听声音、嗅气味
- 问诊(Inquiry):询问症状、病史、生活习惯等
- 切诊(Palpation):把脉、按腹、触诊等

**优势**:
- ✅ 符合中医诊疗流程，四诊合参是中医辨证的基础
- ✅ 数据结构完整，支持医生全面记录诊断信息
- ✅ 与中医诊断(TCMDiagnosis)和治法(TreatmentPrinciple)形成完整链条

#### 3. 诊断完成标记 - Step1状态管理与处方启用控制

**核心思想**：通过Step1CompletedAt时间戳标记诊断完成状态，控制处方启用/禁用逻辑，确保诊断完成后才能开具处方。

**实现要点**:
```csharp
// Step1完成状态判断
public bool PrescriptionEnabled => Step1CompletedAt.HasValue;
public bool PrescriptionDisabled => !Step1CompletedAt.HasValue;

// 完成诊断逻辑
private async Task ExecuteCompleteStep1()
{
    if (!Validate()) return;
    await SaveAsync();
    Step1CompletedAt = DateTime.Now;  // 启用处方
}
```

**优势**:
- ✅ 强制诊断完成流程，避免跳过诊断直接开方
- ✅ UI状态自动联动，按钮启用/禁用逻辑清晰
- ✅ 完成时间可追溯，记录诊断完成的准确时刻

#### 4. Repository模式与三层架构 - 数据访问标准化

**核心思想**：采用Repository模式封装数据访问逻辑，ViewModel通过Repository接口与Server API交互，实现三层架构（ViewModel → Repository → API）。

**架构层次**:
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

**优势**:
- ✅ 数据访问逻辑集中管理，易于测试和维护
- ✅ ViewModel无需关心HTTP通信细节，专注业务逻辑
- ✅ BaseApiRepository提供标准实现，减少重复代码

#### 5. 暂存/继续功能 - 工作流中断与恢复

**核心思想**：支持诊断数据暂存（不完成Step1）和继续（恢复上次未完成的诊断），适应中医诊所实际工作场景。

**实现要点**:
- SaveDraftCommand保存当前数据但不标记Step1CompletedAt
- OnNavigatedTo导航生命周期方法加载医案详情并恢复所有字段
- 暂存不验证必填项，允许部分数据保存

**优势**:
- ✅ 适应实际工作流，支持诊断过程中断和恢复
- ✅ 防止数据丢失，医生可以随时暂存当前进度
- ✅ 用户体验友好，支持灵活的诊疗节奏

#### 6. 异步优先与UI响应性 - 流畅的用户体验

**核心思想**：所有I/O操作（API调用、数据库访问）必须使用async/await异步模式，避免阻塞UI线程，确保应用响应流畅。

**实现要点**:
```csharp
// 异步命令定义
public AsyncDelegateCommand CompleteStep1Command { get; }

// 异步方法实现
private async Task ExecuteCompleteStep1()
{
    try
    {
        IsBusy = true;  // 显示加载动画
        await SaveAsync();  // 不阻塞UI
    }
    finally
    {
        IsBusy = false;  // 隐藏加载动画
    }
}
```

**优势**:
- ✅ UI始终保持响应，不会因为网络请求而卡顿
- ✅ IsBusy标志提供明确的加载反馈
- ✅ 异步命令防止重复点击

---

## 🛠 技术栈

### Server端技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| .NET | 8.0 | 基础框架 |
| ASP.NET Core | 8.0 | Web API框架 |
| Entity Framework Core | 8.0.0 | ORM框架，数据访问层 |
| SQL Server | 2022 | 关系型数据库 |
| AutoMapper | 13.x | Entity ↔ DTO自动映射 |
| Microsoft.Extensions.Logging | 8.0.x | 日志记录框架 |

### Client端技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| .NET | 8.0 | 基础框架 |
| WPF | .NET 8 | Windows桌面UI框架 |
| Prism.DryIoc | 9.0.x | MVVM框架、依赖注入、区域导航 |
| MaterialDesignThemes | 5.1.x | Material Design风格UI组件库 |
| Microsoft.Extensions.Logging | 8.0.x | 日志记录框架 |

---

## 🚀 快速开始

### Server端集成

```csharp
// Step 1: 注册ConsultationModule服务（在Program.cs中）
using LYBT.Module.Consultation;

var builder = WebApplication.CreateBuilder(args);

// 注册Consultation模块（自动注册Service+Repository）
builder.Services.AddConsultationModule();

var app = builder.Build();
app.Run();

// Step 2: 使用ConsultationService（在Controller中）
[ApiController]
[Route("api/v1/[controller]")]
public class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultationService;

    public ConsultationsController(IConsultationService consultationService)
    {
        _consultationService = consultationService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var consultation = await _consultationService.GetByIdAsync(id);
        return consultation == null ? NotFound() : Ok(consultation);
    }

    [HttpGet("medical-case/{medicalCaseId}")]
    public async Task<IActionResult> GetByMedicalCaseId(Guid medicalCaseId)
    {
        var consultation = await _consultationService.GetByMedicalCaseIdAsync(medicalCaseId);
        return consultation == null ? NotFound() : Ok(consultation);
    }
}
```

### Client端集成

```csharp
// Step 1: Shell加载Consultation模块（在App.xaml.cs中）
using LYBT.Desktop.Consultation;

protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Consultation模块(自动注册ViewModels+Views)
    moduleCatalog.AddModule<ConsultationModule>(InitializationMode.WhenAvailable);
}

// Step 2: 导航到诊断表单（在MedicalCase或Patient模块中）
var parameters = new NavigationParameters
{
    { "MedicalCaseId", selectedCase.Id }
};

_regionManager.RequestNavigate("ContentRegion", "ConsultationFormView", parameters);

// Step 3: ConsultationFormViewModel会自动加载医案详情并恢复诊断数据
// OnNavigatedTo生命周期方法：
// 1. 获取MedicalCaseId参数
// 2. 调用_medicalCaseRepository.GetByIdWithDetailsAsync(MedicalCaseId)
// 3. 恢复所有诊断字段（ChiefComplaint, TCMDiagnosis, Inspection等）
// 4. 恢复Step1CompletedAt状态，控制处方按钮启用
```

---

## 📚 相关文档

### 架构设计
- [Server端架构文档](../../../architecture/server/README.md) - 三层对齐架构详解
- [Client端架构文档](../../../architecture/client/README.md) - MVVM架构与五层设计
- [Shared层架构文档](../../../architecture/shared/README.md) - 共享组件与跨端契约

### 开发指南
- [Server端开发指南](../../../development/server/README.md) - Server端开发标准与最佳实践
- [Client端开发指南](../../../development/client/README.md) - Client端开发标准与UI规范
- [API集成指南](../../../development/shared/api-integration-guide.md) - Server/Client API集成规范

### API参考
- [Consultation API文档](../../../api/consultation-api.md) - API端点详细说明（待创建）
- [MedicalCase API文档](../../../api/medicalcase-api.md) - 医案模块API集成
- [WebAPI完整文档](../../../api/webapi-reference.md) - 所有API端点总览

### 模块文档
- [Auth模块](../auth/README.md) - 用户认证与授权
- [Users模块](../users/README.md) - 用户管理
- [Patients模块](../patients/README.md) - 患者档案管理
- [MedicalCase模块](../medicalcase/README.md) - 医案管理（与Consultation深度集成）
- [Prescriptions模块](../prescriptions/README.md) - 处方管理（与Consultation关联）
- [Herbs模块](../herbs/README.md) - 中药材管理
- [Formulas模块](../formulas/README.md) - 经方管理

### 快速参考
- [代码模式速查](../../../quick-reference/code-patterns.md) - Repository模式、MVVM模式、异步模式
- [配置模板速查](../../../quick-reference/configuration-templates.md) - Prism模块注册、EF Core配置
- [问题解决速查](../../../quick-reference/troubleshooting.md) - 常见问题与解决方案

---

**最后更新**: 2025-10-29
**维护负责**: Server端开发组、Client端开发组
**文档版本**: v1.0
