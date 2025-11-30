using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Consultation.Models;

/// <summary>
/// 问诊列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用ConsultationDto，实现Desktop层与Shared层的解耦
/// 保持属性名与ConsultationDto一致，确保XAML绑定兼容
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

    // 中医四诊
    private string? _inspection;
    public string? Inspection
    {
        get => _inspection;
        set => SetProperty(ref _inspection, value);
    } // 望诊

    private string? _auscultation;
    public string? Auscultation
    {
        get => _auscultation;
        set => SetProperty(ref _auscultation, value);
    } // 闻诊

    private string? _inquiry;
    public string? Inquiry
    {
        get => _inquiry;
        set => SetProperty(ref _inquiry, value);
    } // 问诊

    private string? _palpation;
    public string? Palpation
    {
        get => _palpation;
        set => SetProperty(ref _palpation, value);
    } // 切诊

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
    /// 从ConsultationDto创建ConsultationItem
    /// </summary>
    public static ConsultationItem FromDto(ConsultationDto dto)
    {
        return new ConsultationItem
        {
            Id = dto.Id,
            MedicalCaseId = dto.MedicalCaseId,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            PatientGender = string.Empty, // ConsultationDto中没有此属性
            PatientAge = null, // ConsultationDto中没有此属性
            ChiefComplaint = dto.ChiefComplaint!,
            PresentIllness = dto.PresentIllness,
            PastHistory = null, // ConsultationDto中没有此属性
            PersonalHistory = null, // ConsultationDto中没有此属性
            FamilyHistory = null, // ConsultationDto中没有此属性
            AllergyHistory = null, // ConsultationDto中没有此属性
            Inspection = dto.Inspection,
            Auscultation = dto.AuscultationOlfaction, // ConsultationDto中是AuscultationOlfaction
            Inquiry = dto.Inquiry,
            Palpation = dto.Palpation,
            TcmDiagnosis = dto.TCMDiagnosis, // ConsultationDto中是TCMDiagnosis
            Syndrome = null, // ConsultationDto中没有此属性
            TreatmentPrinciple = dto.TreatmentPrinciple,
            // DD-002: ConsultationDto已删除Status字段，状态从聚合根MedicalCase派生
            // Step时间戳已移除，状态默认为InProgress，实际状态由MedicalCase聚合根决定
            Status = ConsultationStatus.InProgress,
            CreatedAt = dto.CreatedAt, // ConsultationDto继承的属性
            CompletedAt = null, // EndTime已删除
            PrescriptionId = null // ConsultationDto中没有此属性
        };
    }

    /// <summary>
    /// 转换为ConsultationDto（用于API调用）
    /// </summary>
    public ConsultationDto ToDto()
    {
        return new ConsultationDto
        {
            Id = Id,
            MedicalCaseId = MedicalCaseId,
            PatientId = PatientId,
            PatientName = PatientName,
            DoctorName = string.Empty, // 需要从其他地方获取
            UserId = Guid.Empty, // 需要从其他地方获取
            ChiefComplaint = ChiefComplaint,
            PresentIllness = PresentIllness,
            Inspection = Inspection,
            AuscultationOlfaction = Auscultation, // ConsultationDto中是AuscultationOlfaction
            Inquiry = Inquiry,
            Palpation = Palpation,
            TCMDiagnosis = TcmDiagnosis, // ConsultationDto中是TCMDiagnosis
            TreatmentPrinciple = TreatmentPrinciple,
            // DD-002: ConsultationDto已删除Status字段，状态从聚合根MedicalCase派生
            Remark = null, // ConsultationItem中没有Note属性
            // DD-002: 移除Status赋值
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
        !string.IsNullOrWhiteSpace(Inspection) &&
        !string.IsNullOrWhiteSpace(Auscultation) &&
        !string.IsNullOrWhiteSpace(Inquiry) &&
        !string.IsNullOrWhiteSpace(Palpation);

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
