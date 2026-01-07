using Prism.Mvvm;

namespace LYBT.Desktop.Consultation.Models.Items;

/// <summary>
/// 问诊列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用ConsultationDetailDto，实现Desktop层与Shared层的解耦
/// 保持属性名与ConsultationDetailDto一致，确保XAML绑定兼容
///
/// OpenSpec: consultation-field-alignment - 清理技术债务
/// 移除不属于Consultation实体的字段：
/// - Patient相关字段(Gender/Age/History等) → 应从Patient实体获取
/// - Syndrome → 已废弃
/// - Status/CompletedAt/PrescriptionId → 无后端对应
/// OpenSpec: resolve-mapperly-source-generator-conflict - 使用BindableBase确保Mapperly兼容
/// </summary>
public class ConsultationItem : BindableBase
{
    #region 基础标识字段

    private Guid _id = Guid.Empty;
    /// <summary>
    /// 问诊记录ID（等于MedicalCaseId，共享主键）
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
    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId
    {
        get => _patientId;
        set => SetProperty(ref _patientId, value);
    }

    private Guid _userId = Guid.Empty;
    /// <summary>
    /// 医生用户ID
    /// </summary>
    public Guid UserId
    {
        get => _userId;
        set => SetProperty(ref _userId, value);
    }

    #endregion

    #region 展示字段

    private string _patientName = string.Empty;
    /// <summary>
    /// 患者姓名（展示用）
    /// </summary>
    public string PatientName
    {
        get => _patientName;
        set
        {
            if (SetProperty(ref _patientName, value))
            {
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    private string _doctorName = string.Empty;
    /// <summary>
    /// 医生姓名（展示用）
    /// </summary>
    public string DoctorName
    {
        get => _doctorName;
        set => SetProperty(ref _doctorName, value);
    }

    #endregion

    #region 诊断核心字段

    private string? _presentIllness;
    /// <summary>
    /// 现病史
    /// </summary>
    public string? PresentIllness
    {
        get => _presentIllness;
        set => SetProperty(ref _presentIllness, value);
    }

    private string? _tongueDiagnosis;
    /// <summary>
    /// 舌诊
    /// </summary>
    public string? TongueDiagnosis
    {
        get => _tongueDiagnosis;
        set => SetProperty(ref _tongueDiagnosis, value);
    }

    private string? _pulseDiagnosis;
    /// <summary>
    /// 脉诊
    /// </summary>
    public string? PulseDiagnosis
    {
        get => _pulseDiagnosis;
        set => SetProperty(ref _pulseDiagnosis, value);
    }

    private string? _tcmDiagnosis;
    /// <summary>
    /// 中医诊断（必填）
    /// OpenSpec: consolidate-panel-viewmodels - 属性名统一为TcmDiagnosis，与DTO和XAML绑定一致
    /// </summary>
    public string? TcmDiagnosis
    {
        get => _tcmDiagnosis;
        set
        {
            if (SetProperty(ref _tcmDiagnosis, value))
            {
                RaisePropertyChanged(nameof(IsDiagnosisComplete));
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    #endregion

    #region 审计字段

    private DateTime _createdAt = DateTime.Now;
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    private DateTime? _updatedAt;
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    #endregion

    #region UI状态字段

    private bool _isSelected;
    /// <summary>
    /// 是否选中（UI状态）
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isExpanded;
    /// <summary>
    /// 是否展开（UI状态）
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    #endregion

    #region 计算属性

    /// <summary>
    /// 诊断是否完整（仅检查中医诊断必填）
    /// </summary>
    public bool IsDiagnosisComplete =>
        !string.IsNullOrWhiteSpace(TcmDiagnosis);

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText =>
        $"{PatientName} - {TcmDiagnosis ?? "未诊断"}";

    #endregion
}
