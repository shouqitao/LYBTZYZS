using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Consultation;

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
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
/// </summary>
public partial class ConsultationItem : ObservableObject
{
    #region 基础标识字段

    /// <summary>
    /// 问诊记录ID（等于MedicalCaseId，共享主键）
    /// </summary>
    [ObservableProperty]
    private Guid _id = Guid.Empty;

    /// <summary>
    /// 关联的病历ID
    /// </summary>
    [ObservableProperty]
    private Guid _medicalCaseId = Guid.Empty;

    /// <summary>
    /// 患者ID
    /// </summary>
    [ObservableProperty]
    private Guid _patientId = Guid.Empty;

    /// <summary>
    /// 医生用户ID
    /// </summary>
    [ObservableProperty]
    private Guid _userId = Guid.Empty;

    #endregion

    #region 展示字段

    /// <summary>
    /// 患者姓名（展示用）
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string _patientName = string.Empty;

    /// <summary>
    /// 医生姓名（展示用）
    /// </summary>
    [ObservableProperty]
    private string _doctorName = string.Empty;

    #endregion

    #region 诊断核心字段

    /// <summary>
    /// 现病史
    /// </summary>
    [ObservableProperty]
    private string? _presentIllness;

    /// <summary>
    /// 舌诊
    /// </summary>
    [ObservableProperty]
    private string? _tongueDiagnosis;

    /// <summary>
    /// 脉诊
    /// </summary>
    [ObservableProperty]
    private string? _pulseDiagnosis;

    /// <summary>
    /// 中医诊断（必填）
    /// OpenSpec: consolidate-panel-viewmodels - 属性名统一为TcmDiagnosis，与DTO和XAML绑定一致
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDiagnosisComplete))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string? _tcmDiagnosis;

    #endregion

    #region 审计字段

    /// <summary>
    /// 创建时间
    /// </summary>
    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    [ObservableProperty]
    private DateTime? _updatedAt;

    #endregion

    #region UI状态字段

    /// <summary>
    /// 是否选中（UI状态）
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 是否展开（UI状态）
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

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

    #region 转换方法

    /// <summary>
    /// 从ConsultationDetailDto创建ConsultationItem
    /// </summary>
    /// <remarks>已废弃：请使用ConsultationMappingService.ToItem()</remarks>
    [Obsolete("请使用ConsultationMappingService.ToItem()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public static ConsultationItem FromDto(ConsultationDetailDto dto)
    {
        return new ConsultationItem
        {
            Id = dto.Id,
            MedicalCaseId = dto.MedicalCaseId,
            PatientId = dto.PatientId,
            UserId = dto.UserId,
            PatientName = dto.PatientName ?? string.Empty,
            DoctorName = dto.DoctorName ?? string.Empty,
            PresentIllness = dto.PresentIllness,
            TongueDiagnosis = dto.TongueDiagnosis,
            PulseDiagnosis = dto.PulseDiagnosis,
            TcmDiagnosis = dto.TcmDiagnosis,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// 转换为ConsultationDetailDto（用于API调用）
    /// </summary>
    /// <remarks>已废弃：请使用ConsultationMappingService.ToDto()替代</remarks>
    [Obsolete("请使用ConsultationMappingService.ToDto()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public ConsultationDetailDto ToDto()
    {
        return new ConsultationDetailDto
        {
            Id = Id,
            MedicalCaseId = MedicalCaseId,
            PatientId = PatientId,
            UserId = UserId,
            PatientName = PatientName,
            DoctorName = DoctorName,
            PresentIllness = PresentIllness,
            TongueDiagnosis = TongueDiagnosis,
            PulseDiagnosis = PulseDiagnosis,
            TcmDiagnosis = TcmDiagnosis,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// 转换为ConsultationInputDto（用于保存）
    /// </summary>
    public ConsultationInputDto ToInputDto()
    {
        return new ConsultationInputDto
        {
            Id = Id,
            MedicalCaseId = MedicalCaseId,
            PatientId = PatientId,
            UserId = UserId,
            PresentIllness = PresentIllness,
            TongueDiagnosis = TongueDiagnosis,
            PulseDiagnosis = PulseDiagnosis,
            TcmDiagnosis = TcmDiagnosis
        };
    }

    #endregion
}
