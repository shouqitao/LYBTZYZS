using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 用于患者选择的 <see cref="IMedicalCaseWorkspaceContext"/> 适配器实现。
/// 在选中患者之前不存在活动医案，因此返回空/无效状态。
/// </summary>
public sealed class PatientSelectionWorkspaceContext : IMedicalCaseWorkspaceContext
{
    /// <inheritdoc />
    public WorkspaceState State { get; } = new(
        EditState: EditState.ReadOnly,
        EditType: EditType.Create,
        Mode: WorkspaceMode.Clinical,
        CanEdit: false,
        IsPrescriptionEnabled: false,
        NeedsPrescription: false,
        CanComplete: false,
        CanPrint: false,
        Remark: string.Empty,
        EditReason: string.Empty);

    /// <inheritdoc />
    public Guid MedicalCaseId => Guid.Empty;

    /// <inheritdoc />
    public PatientDetailDto? CurrentPatient => null;

    /// <inheritdoc />
    public ISessionManager? SessionManager => null;
}
