namespace LYBT.Desktop.MedicalCase.Enums;

/// <summary>
/// 面板状态枚举
/// OpenSpec: simplify-workspace-event-architecture
/// </summary>
public enum PanelStatus
{
    /// <summary>
    /// 未开始 - 初始状态
    /// </summary>
    NotStarted,

    /// <summary>
    /// 进行中 - 正在编辑
    /// </summary>
    InProgress,

    /// <summary>
    /// 已完成 - 保存成功
    /// </summary>
    Completed
}
