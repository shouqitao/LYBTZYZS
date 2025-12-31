using Prism.Events;

namespace LYBT.Desktop.MedicalCase.Events;

/// <summary>
/// 医案工作区事件定义
/// OpenSpec: controlify-workspace - Phase 1.4
/// OpenSpec: optimize-medicalcase-api - Phase 5.2 技术债务清理
/// </summary>
/// <remarks>
/// 已删除未使用的事件类（ConsultationSavedEvent, WorkspaceModeChangedEvent等）
/// 跨模块事件使用 LYBT.Desktop.Infrastructure.Events.CaseEvents
/// </remarks>

#region 处方相关事件

/// <summary>
/// 处方已保存事件
/// </summary>
public class PrescriptionSavedEvent : PubSubEvent<PrescriptionSavedPayload> { }

/// <summary>
/// 处方已保存负载
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

/// <summary>
/// 处方数据变更事件（用于脏数据追踪）
/// </summary>
public class PrescriptionDataChangedEvent : PubSubEvent<Guid> { }

#endregion

#region 工作区协调事件

/// <summary>
/// 请求保存所有修改事件
/// </summary>
public class SaveAllRequestedEvent : PubSubEvent<Guid> { }

#endregion
