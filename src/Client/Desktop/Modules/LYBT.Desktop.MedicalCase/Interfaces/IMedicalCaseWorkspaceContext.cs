using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案子级 ViewModel 的只读上下文。
/// 由 MedicalCaseWorkspaceViewModel 实现。
/// 子级 ViewModel 用它读取当前工作区状态而无需耦合到父级。
/// </summary>
public interface IMedicalCaseWorkspaceContext
{
    WorkspaceState State { get; }
    Guid MedicalCaseId { get; }
    PatientDetailDto? CurrentPatient { get; }
    ISessionManager? SessionManager { get; }
}
