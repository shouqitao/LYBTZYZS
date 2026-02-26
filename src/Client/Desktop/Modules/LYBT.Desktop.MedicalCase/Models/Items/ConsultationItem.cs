using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 诊断数据Item - 用于UI绑定的诊断数据模型
/// OpenSpec: consolidate-panel-viewmodels - 从Consultation模块迁移到MedicalCase聚合根模块
/// OpenSpec: adopt-mapperly-unified-mapping - 使用BindableBase确保Mapperly兼容
/// OpenSpec: simplify-workspace-architecture - 直接实现IDataProvider和IValidatable
///
/// 遵循Entity-DTO-Item模式：
/// - Entity: 服务端Consultation实体
/// - DTO: ConsultationDetailDto/ConsultationInputDto (Shared层)
/// - Item: ConsultationItem (Desktop层，用于XAML绑定)
///
/// 属性名与ConsultationDetailDto保持一致，确保XAML绑定兼容
/// </summary>
public class ConsultationItem : BindableBase, IDataProvider, IValidatable
{
    private static readonly ConsultationMapper s_mapper = new();

    #region 基础标识字段

    private Guid _id = Guid.Empty;
    /// <summary>
    /// 辨证记录ID（等于MedicalCaseId，共享主键）
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private Guid _medicalCaseId = Guid.Empty;
    /// <summary>
    /// 关联的医案ID
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

    #region 方法

    /// <summary>
    /// 重置可编辑字段（保留ID）
    /// OpenSpec: unify-medicalcase-item-editmodel - 从 ConsultationEditModel 合并
    /// </summary>
    public void Reset()
    {
        PresentIllness = null;
        TongueDiagnosis = null;
        PulseDiagnosis = null;
        TcmDiagnosis = null;
    }

    #endregion

    #region IDataProvider实现

    /// <inheritdoc />
    public ConsultationInputDto? GetConsultationData() => s_mapper.ToInputDto(this);

    /// <inheritdoc />
    public PrescriptionInputDto? GetPrescriptionData() => null;

    #endregion

    #region IValidatable实现

    private string _validationMessage = string.Empty;
    /// <inheritdoc />
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    /// <inheritdoc />
    public bool Validate()
    {
        if (!IsDiagnosisComplete)
        {
            ValidationMessage = "请填写中医诊断";
            return false;
        }
        ValidationMessage = string.Empty;
        return true;
    }

    #endregion
}
