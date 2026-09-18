using LYBT.Desktop.Contracts.Models.Navigation;

namespace LYBT.Desktop.Contracts.Models;

/// <summary>
/// 医案工作台导航参数键（薄封装）。
/// 权威键定义与参数工厂见 <see cref="MedicalCaseNav"/>；
/// 禁止兼容层：生产方一律使用 <c>MedicalCaseNav.ForNewCase</c> / <c>ForExistingCase</c>。
/// </summary>
public static class MedicalCaseNavigationParameters
{
    /// <summary>参数键名: 医案ID</summary>
    public const string MedicalCaseIdKey = MedicalCaseNav.MedicalCaseId;

    /// <summary>参数键名: 患者ID</summary>
    public const string PatientIdKey = MedicalCaseNav.PatientId;

    /// <summary>参数键名: 工作区模式</summary>
    public const string WorkspaceModeKey = MedicalCaseNav.WorkspaceMode;

    /// <summary>参数键名: 初始编辑状态</summary>
    public const string InitialEditStateKey = MedicalCaseNav.InitialEditState;

    /// <summary>参数键名: 返回目标视图</summary>
    public const string ReturnViewKey = MedicalCaseNav.ReturnView;
}
