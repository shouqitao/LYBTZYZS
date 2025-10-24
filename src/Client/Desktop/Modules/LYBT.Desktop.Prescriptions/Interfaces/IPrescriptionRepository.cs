using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces
{
    /// <summary>
    /// ⚠️ 临时接口桩 - Issue #1606 Phase 3
    /// 此接口已被删除，仅作为编译过渡使用
    /// 请勿使用此接口，所有Write操作应通过IMedicalCaseRepository聚合根
    /// 待Issue #1608重构以下ViewModel后删除：
    /// - PrescriptionCommandHandler
    /// - PrescriptionDataManager
    /// - PrescriptionEditorDialogViewModel
    /// - PrescriptionManagementViewModel
    /// - PrescriptionsMainViewModel
    /// - PrescriptionViewModel
    /// </summary>
    [Obsolete("此接口已被删除，请使用IMedicalCaseRepository聚合根 (Issue #1606)")]
    public interface IPrescriptionRepository
    {
        // 空接口桩
    }
}
