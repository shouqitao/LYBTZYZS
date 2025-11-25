using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Modules.MedicalCase.Models;

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

    private string _patientGender = string.Empty;
    public string PatientGender
    {
        get => _patientGender;
        set => SetProperty(ref _patientGender, value);
    }

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

    private string _chiefComplaint = string.Empty;
    public string ChiefComplaint
    {
        get => _chiefComplaint;
        set => SetProperty(ref _chiefComplaint, value);
    }

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

    private string? _treatmentPlan;
    public string? TreatmentPlan
    {
        get => _treatmentPlan;
        set => SetProperty(ref _treatmentPlan, value);
    }

    private MedicalCaseStatus _status;
    public MedicalCaseStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
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
    /// </summary>
    public static MedicalCaseItem FromDto(MedicalCaseDetailDto dto)
    {
        return new MedicalCaseItem
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            PatientGender = "未知", // DTO中没有此属性，使用默认值
            PatientAge = null, // DTO中没有此属性
            CaseNumber = dto.Id.ToString().Substring(0, 8).ToUpper(), // 使用ID前8位作为案例编号
            ChiefComplaint = dto.ChiefComplaint ?? string.Empty,
            PresentIllness = dto.PresentIllness,
            Diagnosis = dto.DiagnosisResult,
            TreatmentPlan = dto.TreatmentPlan,
            Status = dto.CaseStatus,
            ConsultationId = dto.ConsultationId,
            PrescriptionId = dto.PrescriptionId,
            CreatedAt = dto.CreatedAt,
            CompletedAt = dto.CaseStatus == MedicalCaseStatus.Completed ? dto.UpdatedAt : null,
            CompletionReason = dto.CaseStatus == MedicalCaseStatus.Completed ? "已完成" : null
        };
    }

    /// <summary>
    /// 转换为MedicalCaseDetailDto（用于API调用）
    /// </summary>
    public MedicalCaseDetailDto ToDto()
    {
        return new MedicalCaseDetailDto
        {
            Id = Id,
            PatientId = PatientId,
            PatientName = PatientName,
            DoctorId = Guid.Empty, // 需要从其他地方获取
            DoctorName = string.Empty, // 需要从其他地方获取
            ConsultationId = ConsultationId,
            PrescriptionId = PrescriptionId,
            ConsultationDate = CreatedAt,
            CaseStatus = Status,
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            DiagnosisResult = Diagnosis,
            TreatmentPlan = TreatmentPlan,
            CreatedAt = CreatedAt,
            UpdatedAt = CompletedAt ?? DateTime.Now,
            Status = Status == MedicalCaseStatus.Active ? CommonStatus.Enabled : CommonStatus.Disabled,
            Remark = CompletionReason
        };
    }

    /// <summary>
    /// 从MedicalCaseDetailDto更新当前项
    /// </summary>
    public void UpdateFromDto(MedicalCaseDetailDto dto)
    {
        Id = dto.Id;
        PatientId = dto.PatientId;
        PatientName = dto.PatientName ?? string.Empty;
        PatientGender = "未知"; // DTO中没有此属性，使用默认值
        PatientAge = null; // DTO中没有此属性
        CaseNumber = dto.Id.ToString().Substring(0, 8).ToUpper(); // 使用ID前8位作为案例编号
        ChiefComplaint = dto.ChiefComplaint!;
        PresentIllness = dto.PresentIllness;
        Diagnosis = dto.DiagnosisResult;
        TreatmentPlan = dto.TreatmentPlan;
        Status = dto.CaseStatus;
        ConsultationId = dto.ConsultationId;
        PrescriptionId = dto.PrescriptionId;
        CreatedAt = dto.CreatedAt;
        CompletedAt = dto.CaseStatus == MedicalCaseStatus.Completed ? dto.UpdatedAt : null;
        CompletionReason = dto.CaseStatus == MedicalCaseStatus.Completed ? "已完成" : null;
    }

    /// <summary>
    /// 状态显示文本 - Issue #2242简化版
    /// </summary>
    public string StatusText => Status switch
    {
        MedicalCaseStatus.Draft => "暂存",
        MedicalCaseStatus.Active => "进行中",
        MedicalCaseStatus.Completed => "已完成",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色（用于UI绑定）- Issue #2242简化版
    /// </summary>
    public string StatusColor => Status switch
    {
        MedicalCaseStatus.Draft => "#FFC107",      // 暂存：橙色
        MedicalCaseStatus.Active => "#4CAF50",     // 进行中：绿色
        MedicalCaseStatus.Completed => "#9E9E9E",  // 已完成：灰色
        _ => "#757575"
    };

    /// <summary>
    /// 是否为活动状态
    /// </summary>
    public bool IsActive => Status == MedicalCaseStatus.Active;

    /// <summary>
    /// 是否已完成 - Epic #1612修正版
    /// </summary>
    public bool IsCompleted => Status == MedicalCaseStatus.Completed;

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
