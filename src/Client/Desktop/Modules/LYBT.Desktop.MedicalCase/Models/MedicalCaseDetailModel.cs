using System.Collections.ObjectModel;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Modules.MedicalCase.Models;

/// <summary>
/// 医案详情模型 - Master-Detail模式使用
/// OpenSpec: refactor-medicalcase-management
///
/// 可编辑字段：诊断信息（现病史、舌诊、脉诊、中医诊断）、备注
/// 只读字段：患者信息、处方信息、系统信息
/// </summary>
public class MedicalCaseDetailModel : BindableBase
{
    private Guid _id;
    private Guid _patientId;
    private string _patientName = string.Empty;
    // ConsultationDate已删除，使用CreatedAt代替
    private MedicalCaseStatus _status = MedicalCaseStatus.Draft;
    private string? _remark;

    // 诊断摘要（只读）
    private string? _presentIllness;
    private string? _tongueDiagnosis;
    private string? _pulseDiagnosis;
    private string? _tcmDiagnosis;

    // 处方摘要（只读）
    private int? _herbCount;
    private int? _doseCount;
    private string? _referencedFormulas;
    private ObservableCollection<PrescriptionItemDto> _prescriptionItems = new();

    // 审计信息
    private DateTime _createdAt;
    private DateTime? _updatedAt;
    private string? _doctorName;

    /// <summary>医案ID</summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>患者ID</summary>
    public Guid PatientId
    {
        get => _patientId;
        set => SetProperty(ref _patientId, value);
    }

    /// <summary>患者姓名（只读）</summary>
    public string PatientName
    {
        get => _patientName;
        set => SetProperty(ref _patientName, value);
    }

    // ConsultationDate属性已删除，使用CreatedAt代替
    /// <summary>就诊日期（使用CreatedAt）</summary>
    public DateTime ConsultationDate => CreatedAt;

    /// <summary>状态</summary>
    public MedicalCaseStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>备注（可编辑）</summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    #region 诊断摘要（只读）

    /// <summary>现病史</summary>
    public string? PresentIllness
    {
        get => _presentIllness;
        set => SetProperty(ref _presentIllness, value);
    }

    /// <summary>舌诊</summary>
    public string? TongueDiagnosis
    {
        get => _tongueDiagnosis;
        set => SetProperty(ref _tongueDiagnosis, value);
    }

    /// <summary>脉诊</summary>
    public string? PulseDiagnosis
    {
        get => _pulseDiagnosis;
        set => SetProperty(ref _pulseDiagnosis, value);
    }

    /// <summary>中医诊断</summary>
    public string? TCMDiagnosis
    {
        get => _tcmDiagnosis;
        set => SetProperty(ref _tcmDiagnosis, value);
    }

    /// <summary>诊断摘要（格式化显示）</summary>
    public string DiagnosisSummary
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(PresentIllness))
                parts.Add($"现病史: {PresentIllness}");
            if (!string.IsNullOrWhiteSpace(TongueDiagnosis))
                parts.Add($"舌诊: {TongueDiagnosis}");
            if (!string.IsNullOrWhiteSpace(PulseDiagnosis))
                parts.Add($"脉诊: {PulseDiagnosis}");
            if (!string.IsNullOrWhiteSpace(TCMDiagnosis))
                parts.Add($"中医诊断: {TCMDiagnosis}");
            return parts.Count > 0 ? string.Join("\n", parts) : "暂无诊断信息";
        }
    }

    #endregion

    #region 处方摘要（只读）

    /// <summary>药材数量</summary>
    public int? HerbCount
    {
        get => _herbCount;
        set => SetProperty(ref _herbCount, value);
    }

    /// <summary>剂数</summary>
    public int? DoseCount
    {
        get => _doseCount;
        set => SetProperty(ref _doseCount, value);
    }

    /// <summary>引用验方（验方名称列表，逗号分隔）</summary>
    /// <remarks>OpenSpec: simplify-medicalcase-dataflow - FormulaSource重命名为ReferencedFormulas</remarks>
    public string? ReferencedFormulas
    {
        get => _referencedFormulas;
        set => SetProperty(ref _referencedFormulas, value);
    }

    /// <summary>来源显示（兼容旧代码）</summary>
    public string? FormulaSource => ReferencedFormulas;

    /// <summary>处方摘要（格式化显示）</summary>
    public string PrescriptionSummary
    {
        get
        {
            if (HerbCount == null || HerbCount == 0)
                return "暂无处方";

            var parts = new List<string>();
            parts.Add($"药材: {HerbCount}味");
            if (DoseCount.HasValue && DoseCount > 0)
                parts.Add($"剂数: {DoseCount}剂");
            if (!string.IsNullOrWhiteSpace(FormulaSource))
                parts.Add($"来源: {FormulaSource}");
            return string.Join("   ", parts);
        }
    }

    /// <summary>处方药材列表（只读）</summary>
    public ObservableCollection<PrescriptionItemDto> PrescriptionItems
    {
        get => _prescriptionItems;
        set => SetProperty(ref _prescriptionItems, value);
    }

    /// <summary>是否有处方药材</summary>
    public bool HasPrescriptionItems => PrescriptionItems?.Count > 0;

    #endregion

    #region 审计信息

    /// <summary>创建时间</summary>
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    /// <summary>医生姓名</summary>
    public string? DoctorName
    {
        get => _doctorName;
        set => SetProperty(ref _doctorName, value);
    }

    #endregion

    #region 状态显示

    /// <summary>状态文本</summary>
    public string StatusText => Status switch
    {
        MedicalCaseStatus.Draft => "暂存",
        MedicalCaseStatus.Active => "进行中",
        MedicalCaseStatus.Completed => "已完成",
        _ => "未知"
    };

    #endregion

    #region 工厂方法

    /// <summary>从MedicalCaseDetailDto创建模型</summary>
    /// <remarks>OpenSpec: simplify-medicalcase-dataflow - ConsultationDate删除，使用CreatedAt</remarks>
    public static MedicalCaseDetailModel FromDto(MedicalCaseDetailDto dto)
    {
        var model = new MedicalCaseDetailModel
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            PatientName = dto.PatientName ?? string.Empty,
            // ConsultationDate已删除，使用CreatedAt代替
            Status = dto.CaseStatus,
            Remark = dto.Remark,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            DoctorName = dto.DoctorName
        };

        // 诊断信息（从Consultation获取）
        if (dto.Consultation != null)
        {
            model.PresentIllness = dto.Consultation.PresentIllness;
            model.TongueDiagnosis = dto.Consultation.TongueDiagnosis;
            model.PulseDiagnosis = dto.Consultation.PulseDiagnosis;
            model.TCMDiagnosis = dto.Consultation.TCMDiagnosis;
        }

        // 处方信息
        if (dto.Prescription != null)
        {
            model.HerbCount = dto.Prescription.Items?.Count ?? 0;
            model.DoseCount = dto.Prescription.DosageCount;
            model.ReferencedFormulas = dto.Prescription.ReferencedFormulas ?? "自拟方";

            // 填充处方药材列表
            if (dto.Prescription.Items != null)
            {
                model.PrescriptionItems = new ObservableCollection<PrescriptionItemDto>(dto.Prescription.Items);
            }
        }

        return model;
    }

    /// <summary>转换为医案更新DTO（包含Remark）</summary>
    public MedicalCaseInputDto ToPrescriptionInputDto()
    {
        return new MedicalCaseInputDto
        {
            Id = Id,
            PatientId = PatientId,
            Remark = Remark
        };
    }

    /// <summary>
    /// 转换为诊断更新DTO
    /// OpenSpec: refactor-medicalcase-management - 支持诊断字段编辑
    /// </summary>
    public ConsultationInputDto ToConsultationInputDto()
    {
        return new ConsultationInputDto
        {
            Id = Id,  // 共享主键
            MedicalCaseId = Id,
            PresentIllness = PresentIllness,
            TongueDiagnosis = TongueDiagnosis,
            PulseDiagnosis = PulseDiagnosis,
            TCMDiagnosis = TCMDiagnosis
        };
    }

    /// <summary>克隆模型</summary>
    /// <remarks>OpenSpec: simplify-medicalcase-dataflow - ConsultationDate删除</remarks>
    public MedicalCaseDetailModel Clone()
    {
        var clone = new MedicalCaseDetailModel
        {
            Id = Id,
            PatientId = PatientId,
            PatientName = PatientName,
            // ConsultationDate已删除，使用CreatedAt代替
            Status = Status,
            Remark = Remark,
            PresentIllness = PresentIllness,
            TongueDiagnosis = TongueDiagnosis,
            PulseDiagnosis = PulseDiagnosis,
            TCMDiagnosis = TCMDiagnosis,
            HerbCount = HerbCount,
            DoseCount = DoseCount,
            ReferencedFormulas = ReferencedFormulas,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DoctorName = DoctorName,
            PrescriptionItems = new ObservableCollection<PrescriptionItemDto>(PrescriptionItems)
        };
        return clone;
    }

    #endregion
}
