using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 病历列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用MedicalCaseDto，实现Desktop层与Shared层的解耦
/// 保持属性名与MedicalCaseDto一致，确保XAML绑定兼容
/// </summary>
public class MedicalCaseItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private Guid _patientId;
    public Guid PatientId
    {
        get => _patientId;
        set => SetProperty(ref _patientId, value);
    }

    private string _patientName = string.Empty;
    public string PatientName
    {
        get => _patientName;
        set => SetProperty(ref _patientName, value);
    }

    /// <summary>
    /// 患者性别 - OpenSpec: unify-frontend-backend-types Phase 2
    /// 统一使用Gender枚举，与DTO保持一致
    /// </summary>
    private Gender _patientGender;
    public Gender PatientGender
    {
        get => _patientGender;
        set
        {
            if (SetProperty(ref _patientGender, value))
            {
                RaisePropertyChanged(nameof(PatientGenderDisplay));
            }
        }
    }

    /// <summary>
    /// 患者性别显示文本（用于UI绑定）- OpenSpec: unify-frontend-backend-types Phase 2
    /// </summary>
    public string PatientGenderDisplay => PatientGender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };

    private int? _patientAge;
    public int? PatientAge
    {
        get => _patientAge;
        set => SetProperty(ref _patientAge, value);
    }

    private string _caseNumber = string.Empty;
    public string CaseNumber
    {
        get => _caseNumber;
        set => SetProperty(ref _caseNumber, value);
    }

    // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint, TreatmentPlan
    // 保留PresentIllness和Diagnosis用于显示

    private string? _presentIllness;
    public string? PresentIllness
    {
        get => _presentIllness;
        set => SetProperty(ref _presentIllness, value);
    }

    private string? _diagnosis;
    public string? Diagnosis
    {
        get => _diagnosis;
        set => SetProperty(ref _diagnosis, value);
    }

    /// <summary>
    /// 医案状态 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为CaseStatus，与DTO保持一致
    /// </summary>
    private MedicalCaseStatus _caseStatus;
    public MedicalCaseStatus CaseStatus
    {
        get => _caseStatus;
        set => SetProperty(ref _caseStatus, value);
    }

    private Guid? _consultationId;
    public Guid? ConsultationId
    {
        get => _consultationId;
        set => SetProperty(ref _consultationId, value);
    }

    private Guid? _prescriptionId;
    public Guid? PrescriptionId
    {
        get => _prescriptionId;
        set => SetProperty(ref _prescriptionId, value);
    }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    private DateTime? _completedAt;
    public DateTime? CompletedAt
    {
        get => _completedAt;
        set => SetProperty(ref _completedAt, value);
    }

    private string? _completionReason;
    public string? CompletionReason
    {
        get => _completionReason;
        set => SetProperty(ref _completionReason, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isHighlighted;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// 从MedicalCaseDetailDto创建MedicalCaseItem
    /// OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint, DiagnosisResult, TreatmentPlan
    /// OpenSpec: unify-frontend-backend-types Phase 2 - PatientGender使用枚举
    /// </summary>
    public static MedicalCaseItem FromDto(MedicalCaseDetailDto dto)
    {
        return new MedicalCaseItem
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            PatientGender = dto.PatientGender, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
            PatientAge = dto.PatientAge,
            CaseNumber = dto.CaseNumber ?? dto.Id.ToString().Substring(0, 8).ToUpper(), // 优先使用CaseNumber
            PresentIllness = dto.PresentIllness,
            Diagnosis = dto.Diagnosis ?? dto.Consultation?.TcmDiagnosis, // 优先使用Diagnosis字段
            CaseStatus = dto.CaseStatus, // OpenSpec: unify-frontend-backend-types - 直接映射
            ConsultationId = dto.ConsultationId,
            PrescriptionId = dto.PrescriptionId,
            CreatedAt = dto.CreatedAt,
            CompletedAt = dto.CaseStatus == MedicalCaseStatus.Completed ? dto.UpdatedAt : null,
            CompletionReason = dto.CaseStatus == MedicalCaseStatus.Completed ? "已完成" : null
        };
    }

    /// <summary>
    /// 转换为MedicalCaseDetailDto（用于API调用）
    /// OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId, ConsultationDate删除
    /// OpenSpec: unify-frontend-backend-types Phase 2 - PatientGender使用枚举
    /// </summary>
    public MedicalCaseDetailDto ToDto()
    {
        return new MedicalCaseDetailDto
        {
            Id = Id,
            PatientId = PatientId,
            PatientName = PatientName,
            PatientGender = PatientGender, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
            PatientAge = PatientAge,
            UserId = Guid.Empty, // 需要从其他地方获取
            DoctorName = string.Empty, // 需要从其他地方获取
            CaseNumber = CaseNumber,
            ConsultationId = ConsultationId,
            PrescriptionId = PrescriptionId,
            // ConsultationDate已删除，使用CreatedAt代替
            CaseStatus = CaseStatus, // OpenSpec: unify-frontend-backend-types - 直接映射
            PresentIllness = PresentIllness,
            Diagnosis = Diagnosis,
            CreatedAt = CreatedAt,
            UpdatedAt = CompletedAt ?? DateTime.Now,
            Remark = CompletionReason
        };
    }

    /// <summary>
    /// 从MedicalCaseDetailDto更新当前项
    /// OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint, DiagnosisResult, TreatmentPlan
    /// OpenSpec: unify-frontend-backend-types Phase 2 - PatientGender使用枚举
    /// </summary>
    public void UpdateFromDto(MedicalCaseDetailDto dto)
    {
        Id = dto.Id;
        PatientId = dto.PatientId;
        PatientName = dto.PatientName ?? string.Empty;
        PatientGender = dto.PatientGender; // OpenSpec: unify-frontend-backend-types - 直接使用枚举
        PatientAge = dto.PatientAge;
        CaseNumber = dto.CaseNumber ?? dto.Id.ToString().Substring(0, 8).ToUpper(); // 优先使用CaseNumber
        PresentIllness = dto.PresentIllness;
        Diagnosis = dto.Diagnosis ?? dto.Consultation?.TcmDiagnosis; // 优先使用Diagnosis字段
        CaseStatus = dto.CaseStatus; // OpenSpec: unify-frontend-backend-types - 直接映射
        ConsultationId = dto.ConsultationId;
        PrescriptionId = dto.PrescriptionId;
        CreatedAt = dto.CreatedAt;
        CompletedAt = dto.CaseStatus == MedicalCaseStatus.Completed ? dto.UpdatedAt : null;
        CompletionReason = dto.CaseStatus == MedicalCaseStatus.Completed ? "已完成" : null;
    }

    /// <summary>
    /// 状态显示文本 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public string StatusText => CaseStatus switch
    {
        MedicalCaseStatus.Draft => "暂存",
        MedicalCaseStatus.Active => "进行中",
        MedicalCaseStatus.Completed => "已完成",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色（用于UI绑定）- OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public string StatusColor => CaseStatus switch
    {
        MedicalCaseStatus.Draft => "#FFC107",      // 暂存：橙色
        MedicalCaseStatus.Active => "#4CAF50",     // 进行中：绿色
        MedicalCaseStatus.Completed => "#9E9E9E",  // 已完成：灰色
        _ => "#757575"
    };

    /// <summary>
    /// 是否为活动状态 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public bool IsActive => CaseStatus == MedicalCaseStatus.Active;

    /// <summary>
    /// 是否已完成 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public bool IsCompleted => CaseStatus == MedicalCaseStatus.Completed;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool CanEdit => IsActive;

    /// <summary>
    /// 是否可开始问诊
    /// </summary>
    public bool CanStartConsultation => IsActive && !ConsultationId.HasValue;

    /// <summary>
    /// 是否可开处方
    /// </summary>
    public bool CanCreatePrescription => IsActive && ConsultationId.HasValue && !PrescriptionId.HasValue;

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{CaseNumber} - {PatientName} ({StatusText})";

    /// <summary>
    /// 就诊时长（分钟）
    /// </summary>
    public int? DurationMinutes
    {
        get
        {
            if (CompletedAt.HasValue)
            {
                return (int)(CompletedAt.Value - CreatedAt).TotalMinutes;
            }
            else if (IsActive)
            {
                return (int)(DateTime.Now - CreatedAt).TotalMinutes;
            }
            return null;
        }
    }
}
