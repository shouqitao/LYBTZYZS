using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 病历列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用MedicalCaseDto，实现Desktop层与Shared层的解耦
/// 保持属性名与MedicalCaseDto一致，确保XAML绑定兼容
/// OpenSpec: resolve-mapperly-source-generator-conflict - 使用BindableBase确保Mapperly兼容
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

    private Gender _patientGender;
    /// <summary>
    /// 患者性别 - OpenSpec: unify-frontend-backend-types Phase 2
    /// 统一使用Gender枚举，与DTO保持一致
    /// </summary>
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

    private MedicalCaseStatus _caseStatus;
    /// <summary>
    /// 医案状态 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为CaseStatus，与DTO保持一致
    /// </summary>
    public MedicalCaseStatus CaseStatus
    {
        get => _caseStatus;
        set
        {
            if (SetProperty(ref _caseStatus, value))
            {
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(StatusColor));
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(IsCompleted));
                RaisePropertyChanged(nameof(CanEdit));
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    private Guid? _consultationId;
    public Guid? ConsultationId
    {
        get => _consultationId;
        set
        {
            SetProperty(ref _consultationId, value);
        }
    }

    private Guid? _prescriptionId;
    public Guid? PrescriptionId
    {
        get => _prescriptionId;
        set
        {
            SetProperty(ref _prescriptionId, value);
        }
    }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (SetProperty(ref _createdAt, value))
            {
                RaisePropertyChanged(nameof(DurationMinutes));
            }
        }
    }

    private DateTime? _completedAt;
    public DateTime? CompletedAt
    {
        get => _completedAt;
        set
        {
            if (SetProperty(ref _completedAt, value))
            {
                RaisePropertyChanged(nameof(DurationMinutes));
            }
        }
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

    #region 计算属性

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

    // OpenSpec: simplify-desktop-data-layer - Phase 2
    // CanStartConsultation和CanCreatePrescription属性已删除（无XAML绑定使用）

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

    #endregion
}
