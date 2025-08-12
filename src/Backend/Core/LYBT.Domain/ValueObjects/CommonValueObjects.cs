using LYBT.Domain.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Domain.ValueObjects
{
    /// <summary>
    /// 通用值对象定义 - 用于解决编译问题的临时实现
    /// </summary>

    // 中医相关值对象
    public class TCMSyndrome : ValueObject
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        public TCMSyndrome(string name, string description = "")
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return Description;
        }
    }

    public class TreatmentPrinciple : ValueObject
    {
        public string Principle { get; private set; }
        public string Methods { get; private set; }

        public TreatmentPrinciple(string principle, string methods = "")
        {
            Principle = principle ?? string.Empty;
            Methods = methods ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Principle;
            yield return Methods;
        }
    }

    // 金钱值对象
    public class Money : ValueObject
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        public static Money Zero => new Money(0, "CNY");

        public Money(decimal amount, string currency = "CNY")
        {
            Amount = amount;
            Currency = currency ?? "CNY";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    // 主诉值对象
    public class ChiefComplaint : ValueObject
    {
        public string Description { get; private set; }
        public string Complaint => Description; // 兼容属性
        public string Duration { get; private set; }
        public string Severity { get; private set; }

        public ChiefComplaint(string description, string duration = "", string severity = "")
        {
            Description = description ?? string.Empty;
            Duration = duration ?? string.Empty;
            Severity = severity ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Description;
            yield return Duration;
            yield return Severity;
        }
    }

    // 现病史值对象
    public class PresentIllness : ValueObject
    {
        public string Onset { get; private set; }
        public string Development { get; private set; }
        public string CurrentStatus { get; private set; }
        public string TreatmentHistory { get; private set; }
        public string Response { get; private set; }

        public PresentIllness(string onset = "", string development = "", string currentStatus = "", 
            string treatmentHistory = "", string response = "")
        {
            Onset = onset ?? string.Empty;
            Development = development ?? string.Empty;
            CurrentStatus = currentStatus ?? string.Empty;
            TreatmentHistory = treatmentHistory ?? string.Empty;
            Response = response ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Onset;
            yield return Development;
            yield return CurrentStatus;
            yield return TreatmentHistory;
            yield return Response;
        }
    }

    // 既往史值对象
    public class PastMedicalHistory : ValueObject
    {
        public string Diseases { get; private set; }
        public string Surgeries { get; private set; }
        public string Allergies { get; private set; }
        public string Medications { get; private set; }
        public string Immunizations { get; private set; }

        public PastMedicalHistory(string diseases = "", string surgeries = "", string allergies = "", 
            string medications = "", string immunizations = "")
        {
            Diseases = diseases ?? string.Empty;
            Surgeries = surgeries ?? string.Empty;
            Allergies = allergies ?? string.Empty;
            Medications = medications ?? string.Empty;
            Immunizations = immunizations ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Diseases;
            yield return Surgeries;
            yield return Allergies;
            yield return Medications;
            yield return Immunizations;
        }
    }

    // 个人史值对象
    public class PersonalHistory : ValueObject
    {
        public string Occupation { get; private set; }
        public string Lifestyle { get; private set; }
        public string DietaryHabits { get; private set; }
        public string SmokingHistory { get; private set; }
        public string DrinkingHistory { get; private set; }

        public PersonalHistory(string occupation = "", string lifestyle = "", string dietaryHabits = "", 
            string smokingHistory = "", string drinkingHistory = "")
        {
            Occupation = occupation ?? string.Empty;
            Lifestyle = lifestyle ?? string.Empty;
            DietaryHabits = dietaryHabits ?? string.Empty;
            SmokingHistory = smokingHistory ?? string.Empty;
            DrinkingHistory = drinkingHistory ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Occupation;
            yield return Lifestyle;
            yield return DietaryHabits;
            yield return SmokingHistory;
            yield return DrinkingHistory;
        }
    }

    // 家族史值对象
    public class FamilyHistory : ValueObject
    {
        public string Diseases { get; private set; }
        public string GeneticConditions { get; private set; }

        public FamilyHistory(string diseases = "", string geneticConditions = "")
        {
            Diseases = diseases ?? string.Empty;
            GeneticConditions = geneticConditions ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Diseases;
            yield return GeneticConditions;
        }
    }

    // 中医诊断值对象
    public class TCMDiagnosis : ValueObject
    {
        public string Syndrome { get; private set; }
        public string Pattern { get; private set; }
        public string Principle { get; private set; }
        public string Disease => Syndrome; // 兼容属性
        public string Name => Syndrome; // 兼容属性

        public TCMDiagnosis(string syndrome = "", string pattern = "", string principle = "")
        {
            Syndrome = syndrome ?? string.Empty;
            Pattern = pattern ?? string.Empty;
            Principle = principle ?? string.Empty;
        }

        // 四参数构造函数兼容
        public TCMDiagnosis(string syndrome, string pattern, string principle, string description)
            : this(syndrome, pattern, principle)
        {
            // description参数暂时忽略，保持兼容性
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Syndrome;
            yield return Pattern;
            yield return Principle;
        }
    }

    // 体质值对象
    public class Constitution : ValueObject
    {
        public string Type { get; private set; }
        public string Description { get; private set; }

        public Constitution(string type = "", string description = "")
        {
            Type = type ?? string.Empty;
            Description = description ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Type;
            yield return Description;
        }
    }

    // 治疗结果值对象
    public class TreatmentOutcome : ValueObject
    {
        public string Effect { get; private set; }
        public string Symptoms { get; private set; }
        public string Signs { get; private set; }
        public string LabResults { get; private set; }
        public string Complications { get; private set; }
        public string Prognosis { get; private set; }

        public TreatmentOutcome(string effect = "", string symptoms = "", string signs = "", 
            string labResults = "", string complications = "", string prognosis = "")
        {
            Effect = effect ?? string.Empty;
            Symptoms = symptoms ?? string.Empty;
            Signs = signs ?? string.Empty;
            LabResults = labResults ?? string.Empty;
            Complications = complications ?? string.Empty;
            Prognosis = prognosis ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Effect;
            yield return Symptoms;
            yield return Signs;
            yield return LabResults;
            yield return Complications;
            yield return Prognosis;
        }
    }

    // 就诊记录值对象
    public class VisitRecord : ValueObject
    {
        public DateTime VisitDate { get; private set; }
        public string Purpose { get; private set; }
        public string Diagnosis { get; private set; }
        public string Treatment { get; private set; }

        public VisitRecord(DateTime visitDate, string purpose = "", string diagnosis = "", string treatment = "")
        {
            VisitDate = visitDate;
            Purpose = purpose ?? string.Empty;
            Diagnosis = diagnosis ?? string.Empty;
            Treatment = treatment ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return VisitDate;
            yield return Purpose;
            yield return Diagnosis;
            yield return Treatment;
        }
    }

    // 过敏记录值对象
    public class AllergyRecord : ValueObject
    {
        public string Allergen { get; private set; }
        public string Reaction { get; private set; }
        public string Severity { get; private set; }
        public DateTime? FirstOccurrence { get; private set; }

        public AllergyRecord(string allergen, string reaction = "", string severity = "", DateTime? firstOccurrence = null)
        {
            Allergen = allergen ?? string.Empty;
            Reaction = reaction ?? string.Empty;
            Severity = severity ?? string.Empty;
            FirstOccurrence = firstOccurrence;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Allergen;
            yield return Reaction;
            yield return Severity;
            yield return FirstOccurrence;
        }
    }

    // 病史记录值对象
    public class MedicalHistory : ValueObject
    {
        public string Condition { get; private set; }
        public DateTime? DiagnosisDate { get; private set; }
        public string Status { get; private set; }
        public string Treatment { get; private set; }

        public MedicalHistory(string condition, DateTime? diagnosisDate = null, string status = "", string treatment = "")
        {
            Condition = condition ?? string.Empty;
            DiagnosisDate = diagnosisDate;
            Status = status ?? string.Empty;
            Treatment = treatment ?? string.Empty;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Condition;
            yield return DiagnosisDate;
            yield return Status;
            yield return Treatment;
        }
    }
}

// 枚举定义
namespace LYBT.Domain.ValueObjects
{
    public enum DiagnosisType
    {
        TCM = 0,
        Western = 1,
        Combined = 2
    }

    public enum TreatmentEffect
    {
        Excellent = 0,
        Good = 1,
        Fair = 2,
        Poor = 3
    }

    public enum CaseStatus
    {
        Draft = 0,
        Active = 1,
        Completed = 2,
        Cancelled = 3,
        Closed = 4
    }

    public enum CaseType
    {
        Outpatient = 0,
        Inpatient = 1,
        Emergency = 2,
        FollowUp = 3
    }

    public enum ConsultationStatus
    {
        Scheduled = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }

    public enum BillingCategory
    {
        Consultation = 0,
        Medicine = 1,
        Treatment = 2,
        Other = 3
    }
}