using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案编辑上下文 - 跨服务共享的可变状态
/// 由 CommandService 和 LifecycleService 共同持有，保证状态一致性
/// </summary>
public sealed class MedicalCaseEditContext
{
    private readonly MedicalCaseCloneMapper _cloneMapper = new();

    public MedicalCaseDetailDto? CurrentDetail { get; set; }
    public MedicalCaseDetailDto? OriginalDetail { get; set; }

    // 缓存字段 (原 Coordinator 职责)
    public MedicalCaseDetailDto? CachedMedicalCase { get; set; }
    public ConsultationDetailDto? CachedConsultation { get; set; }
    public PrescriptionDetailDto? CachedPrescription { get; set; }

    public void SetCurrent(MedicalCaseDetailDto detail)
    {
        CurrentDetail = detail;
        OriginalDetail = _cloneMapper.Clone(detail);
    }

    public void UpdateOriginal()
    {
        if (CurrentDetail != null)
            OriginalDetail = _cloneMapper.Clone(CurrentDetail);
    }

    public void Clear()
    {
        CurrentDetail = null;
        OriginalDetail = null;
    }

    public void ClearCache()
    {
        CachedMedicalCase = null;
        CachedConsultation = null;
        CachedPrescription = null;
    }
}
