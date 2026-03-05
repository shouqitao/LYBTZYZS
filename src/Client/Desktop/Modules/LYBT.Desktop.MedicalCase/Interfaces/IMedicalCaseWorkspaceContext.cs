using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// Read-only context for MedicalCase child VMs.
/// Implemented by MedicalCaseWorkspaceViewModel.
/// Child VMs use this to read current workspace state without coupling to parent.
/// </summary>
public interface IMedicalCaseWorkspaceContext
{
    WorkspaceState State { get; }
    Guid MedicalCaseId { get; }
    PatientDetailDto? CurrentPatient { get; }
    ISessionManager? SessionManager { get; }
}
