using System;
using System.Collections.Generic;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.PrescriptionAggregate.Events
{
    /// <summary>
    /// 处方已创建事件 - UltraThink重构DDD架构
    /// 当新处方被创建时触发
    /// </summary>
    public record PrescriptionCreatedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid PatientId,
        string PatientName,
        Guid DoctorId,
        string PrescriptionType,
        decimal TotalAmount,
        Guid CreatedBy
    ) : DomainEvent;

    /// <summary>
    /// 处方明细已添加事件
    /// 当处方中添加药品明细时触发
    /// </summary>
    public record PrescriptionItemAddedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid HerbId,
        string HerbName,
        decimal Quantity,
        decimal UnitPrice,
        decimal ItemAmount
    ) : DomainEvent;

    /// <summary>
    /// 处方明细已更新事件
    /// 当处方中的药品明细被更新时触发
    /// </summary>
    public record PrescriptionItemUpdatedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid HerbId,
        string HerbName,
        decimal OldQuantity,
        decimal NewQuantity,
        decimal NewItemAmount
    ) : DomainEvent;

    /// <summary>
    /// 处方明细已移除事件
    /// 当处方中的药品明细被移除时触发
    /// </summary>
    public record PrescriptionItemRemovedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid HerbId,
        string HerbName,
        decimal RemovedAmount
    ) : DomainEvent;

    /// <summary>
    /// 处方状态已更改事件
    /// 当处方状态发生变化时触发
    /// </summary>
    public record PrescriptionStatusChangedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        string OldStatus,
        string NewStatus,
        Guid? ChangedBy,
        string Reason
    ) : DomainEvent;

    /// <summary>
    /// 处方已确认事件
    /// 当处方从草稿状态变为确认状态时触发
    /// </summary>
    public record PrescriptionConfirmedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid PatientId,
        string PatientName,
        Guid DoctorId,
        decimal TotalAmount,
        DateTime ConfirmedAt,
        Guid ConfirmedBy
    ) : DomainEvent;

    /// <summary>
    /// 处方开始配药事件
    /// 当处方开始配药时触发
    /// </summary>
    public record PrescriptionDispensingStartedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid PharmacistId,
        DateTime StartedAt
    ) : DomainEvent;

    /// <summary>
    /// 处方配药完成事件
    /// 当处方配药完成时触发
    /// </summary>
    public record PrescriptionDispensedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid PatientId,
        string PatientName,
        Guid PharmacistId,
        decimal TotalAmount,
        DateTime DispensedAt
    ) : DomainEvent;

    /// <summary>
    /// 处方已取消事件
    /// 当处方被取消时触发
    /// </summary>
    public record PrescriptionCancelledEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        string Reason,
        Guid CancelledBy,
        DateTime CancelledAt
    ) : DomainEvent;

    /// <summary>
    /// 处方用法用量已更新事件
    /// 当处方的服用方法和用量被更新时触发
    /// </summary>
    public record PrescriptionUsageUpdatedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        string Method,
        string Frequency,
        string Duration,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 处方诊断信息已更新事件
    /// 当处方的诊断信息被更新时触发
    /// </summary>
    public record PrescriptionDiagnosisUpdatedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        string ChiefComplaint,
        string TcmDiagnosis,
        string TcmSyndrome,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 处方总金额已重新计算事件
    /// 当处方明细变更导致总金额重新计算时触发
    /// </summary>
    public record PrescriptionAmountRecalculatedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        decimal OldAmount,
        decimal NewAmount,
        int TotalItemCount
    ) : DomainEvent;

    /// <summary>
    /// 处方打印事件
    /// 当处方被打印时触发
    /// </summary>
    public record PrescriptionPrintedEvent(
        Guid PrescriptionId,
        string PrescriptionNumber,
        Guid PatientId,
        Guid PrintedBy,
        DateTime PrintedAt,
        int PrintCount
    ) : DomainEvent;

    /// <summary>
    /// 处方复制事件
    /// 当基于现有处方创建新处方时触发
    /// </summary>
    public record PrescriptionCopiedEvent(
        Guid OriginalPrescriptionId,
        Guid NewPrescriptionId,
        string OriginalPrescriptionNumber,
        string NewPrescriptionNumber,
        Guid CopiedBy,
        DateTime CopiedAt
    ) : DomainEvent;
}