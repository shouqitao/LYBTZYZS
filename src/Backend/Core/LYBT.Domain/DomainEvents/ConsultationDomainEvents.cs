using System;
using MediatR;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.ValueObjects;

namespace LYBT.Domain.DomainEvents
{
    /// <summary>
    /// 看诊开始领域事件
    /// </summary>
    public class ConsultationStartedDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public Guid PatientId { get; }
        public Guid DoctorId { get; }
        public DateTime StartTime { get; }
        public DateTime OccurredOn { get; }

        public ConsultationStartedDomainEvent(Guid consultationId, Guid patientId, Guid doctorId, DateTime startTime)
        {
            ConsultationId = consultationId;
            PatientId = patientId;
            DoctorId = doctorId;
            StartTime = startTime;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 四诊信息记录领域事件
    /// </summary>
    public class FourDiagnosisRecordedDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public string DiagnosisType { get; } // 望、闻、问、切
        public object DiagnosisInfo { get; }
        public DateTime OccurredOn { get; }

        public FourDiagnosisRecordedDomainEvent(Guid consultationId, string diagnosisType, object diagnosisInfo)
        {
            ConsultationId = consultationId;
            DiagnosisType = diagnosisType;
            DiagnosisInfo = diagnosisInfo;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 诊断完成领域事件
    /// </summary>
    public class DiagnosisCompletedDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public TCMSyndrome PrimarySyndrome { get; }
        public TreatmentPrinciple TreatmentPrinciple { get; }
        public DateTime OccurredOn { get; }

        public DiagnosisCompletedDomainEvent(
            Guid consultationId,
            TCMSyndrome primarySyndrome,
            TreatmentPrinciple treatmentPrinciple)
        {
            ConsultationId = consultationId;
            PrimarySyndrome = primarySyndrome;
            TreatmentPrinciple = treatmentPrinciple;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 治疗方案创建领域事件
    /// </summary>
    public class TreatmentPlanCreatedDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public TreatmentPlan TreatmentPlan { get; }
        public DateTime OccurredOn { get; }

        public TreatmentPlanCreatedDomainEvent(Guid consultationId, TreatmentPlan treatmentPlan)
        {
            ConsultationId = consultationId;
            TreatmentPlan = treatmentPlan;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 看诊完成领域事件
    /// </summary>
    public class ConsultationCompletedDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public Guid PatientId { get; }
        public Guid DoctorId { get; }
        public int Duration { get; }
        public DateTime? NextVisitDate { get; }
        public DateTime OccurredOn { get; }

        public ConsultationCompletedDomainEvent(
            Guid consultationId,
            Guid patientId,
            Guid doctorId,
            int duration,
            DateTime? nextVisitDate)
        {
            ConsultationId = consultationId;
            PatientId = patientId;
            DoctorId = doctorId;
            Duration = duration;
            NextVisitDate = nextVisitDate;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 看诊取消领域事件
    /// </summary>
    public class ConsultationCancelledDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public string Reason { get; }
        public DateTime OccurredOn { get; }

        public ConsultationCancelledDomainEvent(Guid consultationId, string reason)
        {
            ConsultationId = consultationId;
            Reason = reason;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 症状记录领域事件
    /// </summary>
    public class SymptomRecordedDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public string SymptomName { get; }
        public string Severity { get; }
        public DateTime OccurredOn { get; }

        public SymptomRecordedDomainEvent(Guid consultationId, string symptomName, string severity)
        {
            ConsultationId = consultationId;
            SymptomName = symptomName;
            Severity = severity;
            OccurredOn = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 复诊预约领域事件
    /// </summary>
    public class FollowUpScheduledDomainEvent : INotification
    {
        public Guid ConsultationId { get; }
        public Guid PatientId { get; }
        public DateTime FollowUpDate { get; }
        public DateTime OccurredOn { get; }

        public FollowUpScheduledDomainEvent(Guid consultationId, Guid patientId, DateTime followUpDate)
        {
            ConsultationId = consultationId;
            PatientId = patientId;
            FollowUpDate = followUpDate;
            OccurredOn = DateTime.UtcNow;
        }
    }
}