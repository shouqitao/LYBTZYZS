using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Consultation.Models;

/// <summary>
/// 问诊列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用ConsultationDetailDto，实现Desktop层与Shared层的解耦
/// 保持属性名与ConsultationDetailDto一致，确保XAML绑定兼容
/// </summary>
public class ConsultationItem : BindableBase
{
    private Guid _id = Guid.Empty;

    /// <summary>
    /// 问诊记录ID
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private Guid _medicalCaseId = Guid.Empty;

    /// <summary>
    /// 关联的病历ID
    /// </summary>
    public Guid MedicalCaseId
    {
        get => _medicalCaseId;
        set => SetProperty(ref _medicalCaseId, value);
    }

    private Guid _patientId = Guid.Empty;
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

    private string? _pastHistory;
    public string? PastHistory
    {
        get => _pastHistory;
        set => SetProperty(ref _pastHistory, value);
    }

    private string? _personalHistory;
    public string? PersonalHistory
    {
        get => _personalHistory;
        set => SetProperty(ref _personalHistory, value);
    }

    private string? _familyHistory;
    public string? FamilyHistory
    {
        get => _familyHistory;
        set => SetProperty(ref _familyHistory, value);
    }

    private string? _allergyHistory;
    public string? AllergyHistory
    {
        get => _allergyHistory;
        set => SetProperty(ref _allergyHistory, value);
    }

    // 中医四诊（重构版）
    private string? _fourDiagnosis;
    /// <summary>四诊（合并望闻问切）</summary>
    public string? FourDiagnosis
    {
        get => _fourDiagnosis;
        set => SetProperty(ref _fourDiagnosis, value);
    }

    private string? _tongueDiagnosis;
    /// <summary>舌诊</summary>
    public string? TongueDiagnosis
    {
        get => _tongueDiagnosis;
        set => SetProperty(ref _tongueDiagnosis, value);
    }

    private string? _pulseDiagnosis;
    /// <summary>脉诊</summary>
    public string? PulseDiagnosis
    {
        get => _pulseDiagnosis;
        set => SetProperty(ref _pulseDiagnosis, value);
    }

    private string? _tcmDiagnosis;
    public string? TcmDiagnosis
    {
        get => _tcmDiagnosis;
        set => SetProperty(ref _tcmDiagnosis, value);
    } // 中医诊断

    private string? _syndrome;
    public string? Syndrome
    {
        get => _syndrome;
        set => SetProperty(ref _syndrome, value);
    } // 证型

    private string? _treatmentPrinciple;
    public string? TreatmentPrinciple
    {
        get => _treatmentPrinciple;
        set => SetProperty(ref _treatmentPrinciple, value);
    } // 治则

    private ConsultationStatus _status = ConsultationStatus.Pending;
    public ConsultationStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private DateTime _createdAt = DateTime.Now;
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

    private Guid? _prescriptionId;
    public Guid? PrescriptionId
    {
        get => _prescriptionId;
        set => SetProperty(ref _prescriptionId, value);
    }

    private bool _isSelected = false;

    /// <summary>
    /// 是否选中
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isExpanded = false;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>
    /// 从ConsultationDetailDto创建ConsultationItem
    /// </summary>
    public static ConsultationItem FromDto(ConsultationDetailDto dto)
    {
        // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
        return new ConsultationItem
        {
            Id = dto.Id,
            MedicalCaseId = dto.MedicalCaseId,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            PatientGender = string.Empty, // ConsultationDetailDto中没有此属性
            PatientAge = null, // ConsultationDetailDto中没有此属性
            ChiefComplaint = string.Empty, // 已从DTO移除，使用空值
            PresentIllness = dto.PresentIllness,
            PastHistory = null, // ConsultationDetailDto中没有此属性
            PersonalHistory = null, // ConsultationDetailDto中没有此属性
            FamilyHistory = null, // ConsultationDetailDto中没有此属性
            AllergyHistory = null, // ConsultationDetailDto中没有此属性
            FourDiagnosis = null, // 已从DTO移除
            TongueDiagnosis = dto.TongueDiagnosis,
            PulseDiagnosis = dto.PulseDiagnosis,
            TcmDiagnosis = dto.TCMDiagnosis,
            Syndrome = null, // ConsultationDetailDto中没有此属性
            TreatmentPrinciple = null, // 已从DTO移除
            Status = ConsultationStatus.InProgress,
            CreatedAt = dto.CreatedAt,
            CompletedAt = null,
            PrescriptionId = null
        };
    }

    /// <summary>
    /// 转换为ConsultationDetailDto（用于API调用）
    /// </summary>
    public ConsultationDetailDto ToDto()
    {
        // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
        return new ConsultationDetailDto
        {
            Id = Id,
            MedicalCaseId = MedicalCaseId,
            PatientId = PatientId,
            PatientName = PatientName,
            DoctorName = string.Empty,
            UserId = Guid.Empty,
            PresentIllness = PresentIllness,
            TongueDiagnosis = TongueDiagnosis,
            PulseDiagnosis = PulseDiagnosis,
            TCMDiagnosis = TcmDiagnosis,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => Status switch
    {
        ConsultationStatus.InProgress => "进行中",
        ConsultationStatus.Completed => "已完成",
        ConsultationStatus.Cancelled => "已取消",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => Status switch
    {
        ConsultationStatus.InProgress => "#2196F3",
        ConsultationStatus.Completed => "#4CAF50",
        ConsultationStatus.Cancelled => "#F44336",
        _ => "#757575"
    };

    /// <summary>
    /// 是否进行中
    /// </summary>
    public bool IsInProgress => Status == ConsultationStatus.InProgress;

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted => Status == ConsultationStatus.Completed;

    /// <summary>
    /// 是否可编辑
    /// </summary>
    public bool CanEdit => IsInProgress;

    /// <summary>
    /// 是否可开处方
    /// </summary>
    public bool CanCreatePrescription => IsInProgress && !PrescriptionId.HasValue;

    /// <summary>
    /// 四诊是否完整
    /// </summary>
    public bool IsFourDiagnosisComplete =>
        !string.IsNullOrWhiteSpace(FourDiagnosis);

    /// <summary>
    /// 诊断是否完整
    /// </summary>
    public bool IsDiagnosisComplete =>
        !string.IsNullOrWhiteSpace(TcmDiagnosis) &&
        !string.IsNullOrWhiteSpace(Syndrome) &&
        !string.IsNullOrWhiteSpace(TreatmentPrinciple);

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText => $"{PatientName} - {ChiefComplaint} ({StatusText})";

    /// <summary>
    /// 问诊时长（分钟）
    /// </summary>
    public int? DurationMinutes
    {
        get
        {
            if (CompletedAt.HasValue)
            {
                return (int)(CompletedAt.Value - CreatedAt).TotalMinutes;
            }
            else if (IsInProgress)
            {
                return (int)(DateTime.Now - CreatedAt).TotalMinutes;
            }
            return null;
        }
    }
}
