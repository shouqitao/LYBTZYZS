using System;
using System.Collections.Generic;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.PatientAggregate.Events
{
    /// <summary>
    /// 患者已创建事件 - UltraThink重构DDD架构
    /// 当新患者档案被创建时触发
    /// </summary>
    public record PatientCreatedEvent(
        Guid PatientId,
        string PatientNumber,
        string PatientName,
        string Gender,
        string ContactPhone,
        Guid CreatedBy
    ) : DomainEvent;

    /// <summary>
    /// 患者基本信息已更新事件
    /// 当患者基本信息被更新时触发
    /// </summary>
    public record PatientBasicInfoUpdatedEvent(
        Guid PatientId,
        string PatientNumber,
        string PatientName,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 患者紧急联系人已更新事件
    /// 当患者紧急联系人信息被更新时触发
    /// </summary>
    public record PatientEmergencyContactUpdatedEvent(
        Guid PatientId,
        string PatientNumber,
        string ContactName,
        string ContactPhone,
        string Relationship,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 患者过敏史已更新事件
    /// 当患者过敏史被更新时触发
    /// </summary>
    public record PatientAllergyHistoryUpdatedEvent(
        Guid PatientId,
        string PatientNumber,
        IReadOnlyList<string> Allergies,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 患者既往病史已更新事件
    /// 当患者既往病史被更新时触发
    /// </summary>
    public record PatientMedicalHistoryUpdatedEvent(
        Guid PatientId,
        string PatientNumber,
        IReadOnlyList<string> MedicalHistory,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 患者家族病史已更新事件
    /// 当患者家族病史被更新时触发
    /// </summary>
    public record PatientFamilyHistoryUpdatedEvent(
        Guid PatientId,
        string PatientNumber,
        IReadOnlyDictionary<string, List<string>> FamilyHistory,
        Guid? UpdatedBy
    ) : DomainEvent;

    /// <summary>
    /// 患者首次就诊记录事件
    /// 当患者第一次就诊时触发
    /// </summary>
    public record PatientFirstVisitRecordedEvent(
        Guid PatientId,
        string PatientNumber,
        DateTime VisitTime,
        Guid DoctorId
    ) : DomainEvent;

    /// <summary>
    /// 患者就诊记录事件
    /// 当患者每次就诊时触发
    /// </summary>
    public record PatientVisitRecordedEvent(
        Guid PatientId,
        string PatientNumber,
        DateTime VisitTime,
        Guid DoctorId,
        int TotalVisits
    ) : DomainEvent;

    /// <summary>
    /// 患者档案已停用事件
    /// 当患者档案被停用时触发
    /// </summary>
    public record PatientDeactivatedEvent(
        Guid PatientId,
        string PatientNumber,
        string PatientName,
        string Reason,
        Guid DeactivatedBy
    ) : DomainEvent;

    /// <summary>
    /// 患者档案已激活事件
    /// 当停用的患者档案被重新激活时触发
    /// </summary>
    public record PatientActivatedEvent(
        Guid PatientId,
        string PatientNumber,
        string PatientName,
        Guid ActivatedBy
    ) : DomainEvent;
}