using System;
using MediatR;
using LYBT.Domain.Aggregates.PatientAggregate;
using LYBT.Domain.ValueObjects;

namespace LYBT.Domain.DomainEvents
{
    /// <summary>
    /// 患者创建领域事件
    /// </summary>
    public class PatientCreatedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public string PatientName { get; }
        public string Phone { get; }
        public DateTime OccurredOn { get; }

        public PatientCreatedDomainEvent(Patient patient)
        {
            PatientId = patient.Id;
            PatientName = patient.Name.ToString();
            Phone = patient.ContactPhone.ToString();
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 患者信息更新领域事件
    /// </summary>
    public class PatientInfoUpdatedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public object OriginalInfo { get; }
        public object UpdatedInfo { get; }
        public DateTime OccurredOn { get; }

        public PatientInfoUpdatedDomainEvent(Guid patientId, object originalInfo, object updatedInfo)
        {
            PatientId = patientId;
            OriginalInfo = originalInfo;
            UpdatedInfo = updatedInfo;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 就诊记录添加领域事件
    /// </summary>
    public class VisitRecordAddedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public VisitRecord VisitRecord { get; }
        public DateTime OccurredOn { get; }

        public VisitRecordAddedDomainEvent(Guid patientId, VisitRecord visitRecord)
        {
            PatientId = patientId;
            VisitRecord = visitRecord;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 过敏记录添加领域事件
    /// </summary>
    public class AllergyAddedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public AllergyRecord Allergy { get; }
        public DateTime OccurredOn { get; }

        public AllergyAddedDomainEvent(Guid patientId, AllergyRecord allergy)
        {
            PatientId = patientId;
            Allergy = allergy;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 过敏记录移除领域事件
    /// </summary>
    public class AllergyRemovedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public Guid AllergyId { get; }
        public DateTime OccurredOn { get; }

        public AllergyRemovedDomainEvent(Guid patientId, Guid allergyId)
        {
            PatientId = patientId;
            AllergyId = allergyId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 病史添加领域事件
    /// </summary>
    public class MedicalHistoryAddedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public MedicalHistory History { get; }
        public DateTime OccurredOn { get; }

        public MedicalHistoryAddedDomainEvent(Guid patientId, MedicalHistory history)
        {
            PatientId = patientId;
            History = history;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 患者激活领域事件
    /// </summary>
    public class PatientActivatedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public DateTime OccurredOn { get; }

        public PatientActivatedDomainEvent(Guid patientId)
        {
            PatientId = patientId;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 患者停用领域事件
    /// </summary>
    public class PatientDeactivatedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public string Reason { get; }
        public DateTime OccurredOn { get; }

        public PatientDeactivatedDomainEvent(Guid patientId, string reason)
        {
            PatientId = patientId;
            Reason = reason;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 患者归档领域事件
    /// </summary>
    public class PatientArchivedDomainEvent : INotification
    {
        public Guid PatientId { get; }
        public DateTime OccurredOn { get; }

        public PatientArchivedDomainEvent(Guid patientId)
        {
            PatientId = patientId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}