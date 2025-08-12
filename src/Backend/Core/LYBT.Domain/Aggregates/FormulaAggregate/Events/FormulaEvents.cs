using System;
using System.Collections.Generic;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.FormulaAggregate.Events
{
    /// <summary>
    /// 验方已创建事件 - UltraThink重构DDD架构
    /// 当新验方被创建时触发
    /// </summary>
    public record FormulaCreatedEvent(
        Guid FormulaId,
        string FormulaName,
        string FormulaType,
        string EfficacyCategory,
        int CompositionCount,
        Guid CreatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方基本信息已更新事件
    /// 当验方的基本信息被更新时触发
    /// </summary>
    public record FormulaBasicInfoUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        string OldType,
        string NewType,
        string OldEfficacyCategory,
        string NewEfficacyCategory,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方药物组成已更新事件
    /// 当验方中的药物组成发生变化时触发
    /// </summary>
    public record FormulaCompositionUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        int OldCompositionCount,
        int NewCompositionCount,
        IReadOnlyList<string> AddedHerbs,
        IReadOnlyList<string> RemovedHerbs,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方药物已添加事件
    /// 当验方中添加新药物时触发
    /// </summary>
    public record FormulaHerbAddedEvent(
        Guid FormulaId,
        string FormulaName,
        Guid HerbId,
        string HerbName,
        decimal StandardDosage,
        string DosageUnit,
        string Role,
        bool IsOptional,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方药物已移除事件
    /// 当验方中移除药物时触发
    /// </summary>
    public record FormulaHerbRemovedEvent(
        Guid FormulaId,
        string FormulaName,
        Guid HerbId,
        string HerbName,
        string Role,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方药物剂量已更新事件
    /// 当验方中某药物的剂量被更新时触发
    /// </summary>
    public record FormulaHerbDosageUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        Guid HerbId,
        string HerbName,
        decimal OldDosage,
        decimal NewDosage,
        string DosageUnit,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方适应症已更新事件
    /// 当验方的适应症信息被更新时触发
    /// </summary>
    public record FormulaSyndromeUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        string MainSyndrome,
        IReadOnlyList<string> Symptoms,
        string TongueCondition,
        string PulseCondition,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方用法用量已更新事件
    /// 当验方的用法用量被更新时触发
    /// </summary>
    public record FormulaUsageUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        string PreparationMethod,
        string AdministrationMethod,
        string Dosage,
        string Frequency,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方禁忌症已更新事件
    /// 当验方的禁忌症信息被更新时触发
    /// </summary>
    public record FormulaContraindicationUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        IReadOnlyList<string> Contraindications,
        IReadOnlyList<string> Precautions,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方来源信息已更新事件
    /// 当验方的来源信息被更新时触发
    /// </summary>
    public record FormulaSourceUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        string BookName,
        string Author,
        string Dynasty,
        string Edition,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 验方已发布事件
    /// 当验方从草稿状态变为发布状态时触发
    /// </summary>
    public record FormulaPublishedEvent(
        Guid FormulaId,
        string FormulaName,
        string FormulaType,
        int CompositionCount,
        Guid PublishedBy,
        DateTime PublishedAt
    ) : DomainEvent;

    /// <summary>
    /// 验方已撤回事件
    /// 当已发布的验方被撤回时触发
    /// </summary>
    public record FormulaWithdrawnEvent(
        Guid FormulaId,
        string FormulaName,
        string Reason,
        Guid WithdrawnBy,
        DateTime WithdrawnAt
    ) : DomainEvent;

    /// <summary>
    /// 验方已归档事件
    /// 当验方被归档时触发
    /// </summary>
    public record FormulaArchivedEvent(
        Guid FormulaId,
        string FormulaName,
        string Reason,
        Guid ArchivedBy,
        DateTime ArchivedAt
    ) : DomainEvent;

    /// <summary>
    /// 验方使用统计事件
    /// 当验方被用于处方时触发
    /// </summary>
    public record FormulaUsedInPrescriptionEvent(
        Guid FormulaId,
        string FormulaName,
        Guid PrescriptionId,
        Guid PatientId,
        Guid DoctorId,
        DateTime UsedAt,
        int TotalUsageCount
    ) : DomainEvent;

    /// <summary>
    /// 验方复制事件
    /// 当基于现有验方创建新验方时触发
    /// </summary>
    public record FormulaCopiedEvent(
        Guid OriginalFormulaId,
        Guid NewFormulaId,
        string OriginalFormulaName,
        string NewFormulaName,
        Guid CopiedBy,
        DateTime CopiedAt
    ) : DomainEvent;

    /// <summary>
    /// 验方评分更新事件
    /// 当验方的临床评分被更新时触发
    /// </summary>
    public record FormulaRatingUpdatedEvent(
        Guid FormulaId,
        string FormulaName,
        decimal OldRating,
        decimal NewRating,
        int ReviewCount,
        Guid RatedBy
    ) : DomainEvent;
}