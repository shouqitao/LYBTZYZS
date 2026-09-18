namespace LYBT.Desktop.Contracts.Models.Navigation;

/// <summary>
/// 挂号列表导航参数工厂。
/// 消费方：<c>RegistrationListViewModel</c> / <c>RegistrationCreateDialogViewModel</c>。
/// Action 取值："Create"（字面量）。
/// </summary>
public static class RegistrationListNav
{
    /// <summary>动作："Create"</summary>
    public const string Action = "Action";

    /// <summary>患者 ID（Guid）— 预填创建对话框</summary>
    public const string PatientId = "PatientId";

    /// <summary>患者姓名 — 预填创建对话框</summary>
    public const string PatientName = "PatientName";

    /// <summary>打开新建挂号对话框（无预填）</summary>
    public static Dictionary<string, object> Create()
        => new() { [Action] = "Create" };

    /// <summary>打开新建挂号对话框并预填患者</summary>
    public static Dictionary<string, object> CreateForPatient(Guid patientId, string patientName)
        => new()
        {
            [Action] = "Create",
            [PatientId] = patientId,
            [PatientName] = patientName ?? string.Empty,
        };
}
