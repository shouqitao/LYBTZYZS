using System;
using LYBT.Domain.SeedWork;
using LYBT.Domain.ValueObjects;

namespace LYBT.Domain.Aggregates.MedicalCaseAggregate
{
    #region 看诊记录实体

    /// <summary>
    /// 看诊记录实体
    /// </summary>
    public class ConsultationRecord : Entity
    {
        public Guid ConsultationId { get; private set; }
        public DateTime ConsultationDate { get; private set; }
        public string FourDiagnosisInfo { get; private set; }
        public string Diagnosis { get; private set; }
        public string TreatmentPlan { get; private set; }
        public int VisitNumber { get; private set; }

        protected ConsultationRecord() { }

        public ConsultationRecord(
            Guid id,
            Guid consultationId,
            DateTime consultationDate,
            string fourDiagnosisInfo,
            string diagnosis,
            string treatmentPlan,
            int visitNumber)
        {
            Id = id;
            ConsultationId = consultationId;
            ConsultationDate = consultationDate;
            FourDiagnosisInfo = fourDiagnosisInfo;
            Diagnosis = diagnosis;
            TreatmentPlan = treatmentPlan;
            VisitNumber = visitNumber;
        }
    }

    #endregion

    #region 诊断记录实体

    /// <summary>
    /// 诊断记录实体
    /// </summary>
    public class DiagnosisRecord : Entity
    {
        public string DiseaseName { get; private set; }
        public string DiseaseCode { get; private set; }
        public TCMSyndrome Syndrome { get; private set; }
        public DiagnosisType Type { get; private set; }
        public bool IsPrimary { get; private set; }
        public DateTime DiagnosisDate { get; private set; }

        protected DiagnosisRecord() { }

        public DiagnosisRecord(
            Guid id,
            string diseaseName,
            string diseaseCode,
            TCMSyndrome syndrome,
            DiagnosisType type,
            bool isPrimary,
            DateTime diagnosisDate)
        {
            Id = id;
            DiseaseName = diseaseName;
            DiseaseCode = diseaseCode;
            Syndrome = syndrome;
            Type = type;
            IsPrimary = isPrimary;
            DiagnosisDate = diagnosisDate;
        }
    }

    #endregion

    #region 处方记录实体

    /// <summary>
    /// 处方记录实体
    /// </summary>
    public class PrescriptionRecord : Entity
    {
        public Guid PrescriptionId { get; private set; }
        public string PrescriptionNo { get; private set; }
        public DateTime PrescriptionDate { get; private set; }
        public Money Amount { get; private set; }

        protected PrescriptionRecord() { }

        public PrescriptionRecord(
            Guid id,
            Guid prescriptionId,
            string prescriptionNo,
            DateTime prescriptionDate,
            Money amount)
        {
            Id = id;
            PrescriptionId = prescriptionId;
            PrescriptionNo = prescriptionNo;
            PrescriptionDate = prescriptionDate;
            Amount = amount;
        }
    }

    #endregion

    #region 检查记录实体

    /// <summary>
    /// 检查记录实体
    /// </summary>
    public class ExaminationRecord : Entity
    {
        public string ExaminationType { get; private set; }
        public string ExaminationItem { get; private set; }
        public DateTime ExaminationDate { get; private set; }
        public string Result { get; private set; }
        public string Conclusion { get; private set; }

        protected ExaminationRecord() { }

        public ExaminationRecord(
            Guid id,
            string examinationType,
            string examinationItem,
            DateTime examinationDate,
            string result,
            string conclusion)
        {
            Id = id;
            ExaminationType = examinationType;
            ExaminationItem = examinationItem;
            ExaminationDate = examinationDate;
            Result = result;
            Conclusion = conclusion;
        }
    }

    #endregion

    #region 治疗记录实体

    /// <summary>
    /// 治疗记录实体
    /// </summary>
    public class TreatmentRecord : Entity
    {
        public string TreatmentType { get; private set; }
        public string TreatmentMethod { get; private set; }
        public DateTime TreatmentDate { get; private set; }
        public string TreatmentDetails { get; private set; }
        public string Effect { get; private set; }

        protected TreatmentRecord() { }

        public TreatmentRecord(
            Guid id,
            string treatmentType,
            string treatmentMethod,
            DateTime treatmentDate,
            string treatmentDetails,
            string effect)
        {
            Id = id;
            TreatmentType = treatmentType;
            TreatmentMethod = treatmentMethod;
            TreatmentDate = treatmentDate;
            TreatmentDetails = treatmentDetails;
            Effect = effect;
        }
    }

    #endregion

    #region 病程记录实体

    /// <summary>
    /// 病程记录实体
    /// </summary>
    public class ProgressNote : Entity
    {
        public DateTime RecordDate { get; private set; }
        public string Symptoms { get; private set; }
        public string Signs { get; private set; }
        public string Assessment { get; private set; }
        public string Plan { get; private set; }
        public Guid RecordedBy { get; private set; }
        public string RecorderName { get; private set; }

        protected ProgressNote() { }

        public ProgressNote(
            Guid id,
            DateTime recordDate,
            string symptoms,
            string signs,
            string assessment,
            string plan,
            Guid recordedBy,
            string recorderName)
        {
            Id = id;
            RecordDate = recordDate;
            Symptoms = symptoms;
            Signs = signs;
            Assessment = assessment;
            Plan = plan;
            RecordedBy = recordedBy;
            RecorderName = recorderName;
        }
    }

    #endregion

    #region 随访记录实体

    /// <summary>
    /// 随访记录实体
    /// </summary>
    public class FollowUpRecord : Entity
    {
        public DateTime FollowUpDate { get; private set; }
        public string Method { get; private set; }
        public string Status { get; private set; }
        public string Symptoms { get; private set; }
        public string Medication { get; private set; }
        public string Advice { get; private set; }
        public DateTime? NextFollowUpDate { get; private set; }

        protected FollowUpRecord() { }

        public FollowUpRecord(
            Guid id,
            DateTime followUpDate,
            string method,
            string status,
            string symptoms,
            string medication,
            string advice,
            DateTime? nextFollowUpDate)
        {
            Id = id;
            FollowUpDate = followUpDate;
            Method = method;
            Status = status;
            Symptoms = symptoms;
            Medication = medication;
            Advice = advice;
            NextFollowUpDate = nextFollowUpDate;
        }
    }

    #endregion

    #region 费用项目实体

    /// <summary>
    /// 费用项目实体
    /// </summary>
    public class BillingItem : Entity
    {
        public string ItemName { get; private set; }
        public Money Amount { get; private set; }
        public BillingCategory Category { get; private set; }
        public DateTime BillingDate { get; private set; }

        protected BillingItem() { }

        public BillingItem(
            Guid id,
            string itemName,
            Money amount,
            BillingCategory category,
            DateTime billingDate)
        {
            Id = id;
            ItemName = itemName;
            Amount = amount;
            Category = category;
            BillingDate = billingDate;
        }
    }

    #endregion
}