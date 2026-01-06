using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 诊断数据Item - 用于UI绑定的诊断数据模型
/// OpenSpec: consolidate-panel-viewmodels - 从Consultation模块迁移到MedicalCase聚合根模块
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
///
/// 遵循Entity-DTO-Item模式：
/// - Entity: 服务端Consultation实体
/// - DTO: ConsultationDetailDto/ConsultationInputDto (Shared层)
/// - Item: ConsultationItem (Desktop层，用于XAML绑定)
///
/// 属性名与ConsultationDetailDto保持一致，确保XAML绑定兼容
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
    /// <remarks>已废弃：请使用ConsultationMappingService.ToDto()</remarks>
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
