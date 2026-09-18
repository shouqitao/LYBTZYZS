using LYBT.Desktop.Contracts.Enums;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Contracts.Models.Navigation;

/// <summary>
/// 医案工作台导航参数工厂（唯一生产入口）。
/// 禁止无 patient / medicalCaseId 的裸导航——不提供无参 API。
/// 消费方：<c>MedicalCaseWorkspaceViewModel.OnNavigatedToAsync</c>。
/// </summary>
public static class MedicalCaseNav
{
    /// <summary>医案 ID（Guid）</summary>
    public const string MedicalCaseId = "MedicalCaseId";

    /// <summary>当前患者详情（PatientDetailDto）</summary>
    public const string CurrentPatient = "CurrentPatient";

    /// <summary>患者 ID（Guid）— 目标可据此回填 CurrentPatient</summary>
    public const string PatientId = "PatientId";

    /// <summary>工作区模式（WorkspaceMode）</summary>
    public const string WorkspaceMode = "WorkspaceMode";

    /// <summary>初始编辑状态（EditState）</summary>
    public const string InitialEditState = "InitialEditState";

    /// <summary>编辑模式标记（如 "HistoricalEdit"）</summary>
    public const string EditMode = "EditMode";

    /// <summary>返回目标视图名（ViewNames 常量）— 供 Workspace Back 导航</summary>
    public const string ReturnView = "ReturnView";

    /// <summary>完整：已有医案 + 患者详情</summary>
    public static Dictionary<string, object> ForExistingCase(
        Guid medicalCaseId,
        PatientDetailDto patient,
        WorkspaceMode mode = WorkspaceMode.Clinical,
        EditState edit = EditState.Editing,
        string? returnView = null)
    {
        ArgumentNullException.ThrowIfNull(patient);
        if (medicalCaseId == Guid.Empty)
            throw new ArgumentException("ForExistingCase 要求非空 medicalCaseId", nameof(medicalCaseId));

        var parameters = new Dictionary<string, object>
        {
            [MedicalCaseId] = medicalCaseId,
            [CurrentPatient] = patient,
            [PatientId] = patient.Id,
            [WorkspaceMode] = mode,
            [InitialEditState] = edit,
        };

        if (!string.IsNullOrEmpty(returnView))
            parameters[ReturnView] = returnView;

        return parameters;
    }

    /// <summary>新建：仅患者（目标负责 CreateMedicalCase）</summary>
    public static Dictionary<string, object> ForNewCase(
        PatientDetailDto patient,
        WorkspaceMode mode = WorkspaceMode.Clinical,
        EditState edit = EditState.Editing,
        string? returnView = null)
    {
        ArgumentNullException.ThrowIfNull(patient);

        var parameters = new Dictionary<string, object>
        {
            [CurrentPatient] = patient,
            [PatientId] = patient.Id,
            [WorkspaceMode] = mode,
            [InitialEditState] = edit,
        };

        if (!string.IsNullOrEmpty(returnView))
            parameters[ReturnView] = returnView;

        return parameters;
    }
}
