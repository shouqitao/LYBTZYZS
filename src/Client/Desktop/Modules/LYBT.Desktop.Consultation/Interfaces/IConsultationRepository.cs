using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// ⚠️ 临时接口桩 - Issue #1606 Phase 3
    /// 此接口已被删除，仅作为编译过渡使用
    /// 请勿使用此接口，所有Write操作应通过IMedicalCaseRepository聚合根
    /// 待Issue #1607重构ConsultationFormViewModel和ConsultationManagementViewModel后删除
    /// </summary>
    [Obsolete("此接口已被删除，请使用IMedicalCaseRepository聚合根 (Issue #1606)")]
    public interface IConsultationRepository
    {
        // 空接口桩
    }

    /// <summary>
    /// ⚠️ 临时接口桩 - Issue #1606 Phase 3
    /// 此接口已被删除，仅作为编译过渡使用
    /// 请勿使用此接口，所有Write操作应通过IMedicalCaseRepository聚合根
    /// 待Issue #1607重构ConsultationFormViewModel后删除
    /// </summary>
    [Obsolete("此接口已被删除，请使用IMedicalCaseRepository聚合根 (Issue #1606)")]
    public interface IConsultationApiClient
    {
        // 空接口桩
    }
}
