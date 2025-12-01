using Prism.Events;

namespace LYBT.Desktop.MedicalCase.Events;

/// <summary>
/// 医案工作区事件定义
/// OpenSpec: controlify-workspace - Phase 1.4
/// </summary>

#region 诊断相关事件

/// <summary>
/// 诊断已保存事件
/// </summary>
public class ConsultationSavedEvent : PubSubEvent<ConsultationSavedPayload> { }

/// <summary>
/// 诊断已保存负载
/// </summary>
public class ConsultationSavedPayload
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 更新后的RowVersion
    /// </summary>
    public byte[]? UpdatedRowVersion { get; init; }

    /// <summary>
    /// 是否为自动保存
    /// </summary>
    public bool IsAutoSave { get; init; }
}

// 注意: ConsultationCompletedEvent 定义在 LYBT.Desktop.Infrastructure.Events 中
// 使用现有的 ConsultationCompletedEvent 和 ConsultationCompletedPayload

/// <summary>
/// 诊断数据变更事件（用于脏数据追踪）
/// </summary>
public class ConsultationDataChangedEvent : PubSubEvent<Guid> { }

#endregion

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
/// 处方打印请求事件
/// </summary>
public class PrescriptionPrintRequestedEvent : PubSubEvent<Guid> { }

/// <summary>
/// 处方数据变更事件（用于脏数据追踪）
/// </summary>
public class PrescriptionDataChangedEvent : PubSubEvent<Guid> { }

/// <summary>
/// 处方价格计算完成事件
/// </summary>
public class PrescriptionPriceCalculatedEvent : PubSubEvent<PrescriptionPricePayload> { }

/// <summary>
/// 处方价格负载
/// </summary>
public class PrescriptionPricePayload
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 总价
    /// </summary>
    public decimal TotalPrice { get; init; }

    /// <summary>
    /// 药材数量
    /// </summary>
    public int HerbCount { get; init; }
}

#endregion

#region 工作区协调事件

/// <summary>
/// 工作区模式变更事件
/// </summary>
public class WorkspaceModeChangedEvent : PubSubEvent<WorkspaceModeChangedPayload> { }

/// <summary>
/// 工作区模式变更负载
/// </summary>
public class WorkspaceModeChangedPayload
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 新编辑类型
    /// </summary>
    public Models.EditType NewEditType { get; init; }

    /// <summary>
    /// 旧编辑类型
    /// </summary>
    public Models.EditType OldEditType { get; init; }
}

/// <summary>
/// 检测到未保存修改事件
/// </summary>
public class UnsavedChangesDetectedEvent : PubSubEvent<UnsavedChangesPayload> { }

/// <summary>
/// 未保存修改负载
/// </summary>
public class UnsavedChangesPayload
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 诊断有修改
    /// </summary>
    public bool HasConsultationChanges { get; init; }

    /// <summary>
    /// 处方有修改
    /// </summary>
    public bool HasPrescriptionChanges { get; init; }
}

/// <summary>
/// 请求保存所有修改事件
/// </summary>
public class SaveAllRequestedEvent : PubSubEvent<Guid> { }

/// <summary>
/// 所有保存完成事件
/// </summary>
public class AllSavedEvent : PubSubEvent<AllSavedPayload> { }

/// <summary>
/// 所有保存完成负载
/// </summary>
public class AllSavedPayload
{
    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 错误消息（失败时）
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// 完成看诊请求事件
/// </summary>
public class CompleteVisitRequestedEvent : PubSubEvent<Guid> { }

/// <summary>
/// 看诊已完成事件
/// </summary>
public class VisitCompletedEvent : PubSubEvent<Guid> { }

#endregion
