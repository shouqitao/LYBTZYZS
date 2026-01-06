namespace LYBT.Desktop.MedicalCase.Events;

/// <summary>
/// 医案工作区事件相关类型
/// OpenSpec: simplify-workspace-event-architecture (Phase 4)
/// </summary>
/// <remarks>
/// 所有Event类已移除，改用回调模式：
/// - PrescriptionSavedEvent -> SetOnPrescriptionSavedCallback
/// - PrescriptionDataChangedEvent -> 已删除（无订阅者）
/// - SaveAllRequestedEvent -> 已删除（无发布者）
/// 
/// 跨模块事件使用 LYBT.Desktop.Infrastructure.Events.CaseEvents
/// </remarks>

/// <summary>
/// 处方已保存负载（供回调使用）
/// OpenSpec: simplify-workspace-event-architecture (Phase 4)
/// </summary>
public class PrescriptionSavedPayload
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 处方ID
    /// </summary>
    public Guid PrescriptionId { get; init; }

    /// <summary>
    /// 更新后的RowVersion
    /// </summary>
    public byte[]? UpdatedRowVersion { get; init; }

    /// <summary>
    /// 是否为自动保存
    /// </summary>
    public bool IsAutoSave { get; init; }
}
