using System;
using System.Collections.Generic;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.HerbAggregate.Events
{
    /// <summary>
    /// 中药材已创建事件 - UltraThink重构DDD架构
    /// 当新中药材被添加到系统时触发
    /// </summary>
    public record HerbCreatedEvent(
        Guid HerbId,
        string HerbName,
        string Category,
        string Nature,
        decimal Price,
        string Unit,
        Guid CreatedBy
    ) : DomainEvent;

    /// <summary>
    /// 中药材基本信息已更新事件
    /// 当中药材的基本信息被更新时触发
    /// </summary>
    public record HerbBasicInfoUpdatedEvent(
        Guid HerbId,
        string HerbName,
        string OldCategory,
        string NewCategory,
        string OldNature,
        string NewNature,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 中药材价格已更新事件
    /// 当中药材价格发生变化时触发
    /// </summary>
    public record HerbPriceUpdatedEvent(
        Guid HerbId,
        string HerbName,
        decimal OldPrice,
        decimal NewPrice,
        string Reason,
        Guid UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 中药材功效已更新事件
    /// 当中药材的功效描述被更新时触发
    /// </summary>
    public record HerbEfficacyUpdatedEvent(
        Guid HerbId,
        string HerbName,
        string NewEfficacy,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 中药材药性信息已更新事件
    /// 当中药材的味性归经信息被更新时触发
    /// </summary>
    public record HerbPropertiesUpdatedEvent(
        Guid HerbId,
        string HerbName,
        IReadOnlyList<string> Tastes,
        IReadOnlyList<string> Meridians,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 中药材禁忌信息已更新事件
    /// 当中药材的禁忌信息被更新时触发
    /// </summary>
    public record HerbContraindicationUpdatedEvent(
        Guid HerbId,
        string HerbName,
        IReadOnlyList<string> Contraindications,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 中药材已激活事件
    /// 当停用的中药材被重新激活时触发
    /// </summary>
    public record HerbActivatedEvent(
        Guid HerbId,
        string HerbName,
        Guid ActivatedBy,
        DateTime ActivatedAt
    ) : DomainEvent;

    /// <summary>
    /// 中药材已停用事件
    /// 当中药材被停用时触发
    /// </summary>
    public record HerbDeactivatedEvent(
        Guid HerbId,
        string HerbName,
        string Reason,
        Guid DeactivatedBy,
        DateTime DeactivatedAt
    ) : DomainEvent;

    /// <summary>
    /// 中药材批量价格更新事件
    /// 当多个中药材的价格被批量更新时触发
    /// </summary>
    public record HerbBatchPriceUpdatedEvent(
        IReadOnlyList<Guid> HerbIds,
        decimal AdjustmentPercentage,
        string Reason,
        Guid UpdatedBy,
        DateTime UpdatedAt,
        int AffectedCount
    ) : DomainEvent;

    /// <summary>
    /// 中药材批量状态更新事件
    /// 当多个中药材的状态被批量更新时触发
    /// </summary>
    public record HerbBatchStatusUpdatedEvent(
        IReadOnlyList<Guid> HerbIds,
        bool IsEnabled,
        string Reason,
        Guid UpdatedBy,
        DateTime UpdatedAt,
        int AffectedCount
    ) : DomainEvent;

    /// <summary>
    /// 中药材规格已更新事件
    /// 当中药材的规格信息被更新时触发
    /// </summary>
    public record HerbSpecificationUpdatedEvent(
        Guid HerbId,
        string HerbName,
        string OldSpecification,
        string NewSpecification,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 中药材单位已更新事件
    /// 当中药材的计量单位被更改时触发
    /// </summary>
    public record HerbUnitUpdatedEvent(
        Guid HerbId,
        string HerbName,
        string OldUnit,
        string NewUnit,
        Guid UpdatedBy
    ) : DomainEvent;
}