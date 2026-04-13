using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// Adapter implementation of <see cref="IMedicalCaseWorkspaceContext"/> for patient selection.
/// Returns empty/null state because no active medical case exists until a patient is selected.
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