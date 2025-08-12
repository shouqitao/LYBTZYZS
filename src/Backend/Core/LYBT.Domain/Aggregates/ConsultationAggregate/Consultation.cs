using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.Common;
using LYBT.Domain.ValueObjects;
using LYBT.Domain.Exceptions;

namespace LYBT.Domain.Aggregates.ConsultationAggregate
{
    /// <summary>
    /// 看诊聚合根 - 中医诊疗核心领域模型
    /// 
    /// 职责：
    /// 1. 管理四诊信息（望闻问切）
    /// 2. 执行辨证论治
    /// 3. 生成诊断结果
    /// 4. 制定治疗方案
    /// </summary>
    public class Consultation : AggregateRoot
    {
        #region 私有字段

        private Guid _patientId;
        private Guid _doctorId;
        private string _doctorName;
        private string _consultationNo;
        private DateTime _consultationDate;
        private ConsultationStatus _status;
        
        // 四诊信息
        private InspectionInfo _inspection;  // 望诊
        private AuscultationOlfactionInfo _auscultationOlfaction;  // 闻诊
        private InquiryInfo _inquiry;  // 问诊
        private PalpationInfo _palpation;  // 切诊
        
        // 诊断信息
        private readonly List<TCMDiagnosis> _diagnoses;
        private TCMSyndrome _primarySyndrome;
        private readonly List<TCMSyndrome> _secondarySyndromes;
        private TreatmentPrinciple _treatmentPrinciple;
        
        // 症状和体征
        private readonly List<Symptom> _symptoms;
        private readonly List<Sign> _signs;
        
        // 治疗方案
        private TreatmentPlan _treatmentPlan;
        private string _medicalAdvice;
        private DateTime? _nextVisitDate;
        
        // 时间戳
        private DateTime? _startTime;
        private DateTime? _endTime;
        private int _duration;  // 分钟

        #endregion

        #region 属性

        public Guid PatientId => _patientId;
        public Guid DoctorId => _doctorId;
        public string DoctorName => _doctorName;
        public string ConsultationNo => _consultationNo;
        public DateTime ConsultationDate => _consultationDate;
        public DateTime ConsultationTime => _consultationDate; // 兼容Repository中的属性名
        public ConsultationStatus Status => _status;
        
        public InspectionInfo Inspection => _inspection;
        public AuscultationOlfactionInfo AuscultationOlfaction => _auscultationOlfaction;
        public InquiryInfo Inquiry => _inquiry;
        public PalpationInfo Palpation => _palpation;
        
        public IReadOnlyCollection<TCMDiagnosis> Diagnoses => _diagnoses.AsReadOnly();
        public TCMSyndrome PrimarySyndrome => _primarySyndrome;
        public IReadOnlyCollection<TCMSyndrome> SecondarySyndromes => _secondarySyndromes.AsReadOnly();
        public TreatmentPrinciple TreatmentPrinciple => _treatmentPrinciple;
        
        // 兼容Repository中的Diagnosis属性访问
        public DiagnosisInfo Diagnosis => new DiagnosisInfo
        {
            TCMDiagnosis = _diagnoses.Any() ? string.Join("；", _diagnoses.Select(d => d.DiseaseName)) : "",
            WesternDiagnosis = _diagnoses.Any() ? string.Join("；", _diagnoses.Where(d => !string.IsNullOrEmpty(d.DiseaseCode)).Select(d => d.DiseaseCode)) : ""
        };
        
        public IReadOnlyCollection<Symptom> Symptoms => _symptoms.AsReadOnly();
        public IReadOnlyCollection<Sign> Signs => _signs.AsReadOnly();
        
        public TreatmentPlan TreatmentPlan => _treatmentPlan;
        public string MedicalAdvice => _medicalAdvice;
        public DateTime? NextVisitDate => _nextVisitDate;
        
        public DateTime? StartTime => _startTime;
        public DateTime? EndTime => _endTime;
        public int Duration => _duration;

        // 计算属性
        public bool IsInProgress => _status == ConsultationStatus.InProgress;
        public bool IsCompleted => _status == ConsultationStatus.Completed;
        public bool CanModify => _status == ConsultationStatus.Scheduled || _status == ConsultationStatus.InProgress;
        public bool HasDiagnosis => _diagnoses.Any();
        public bool HasTreatmentPlan => _treatmentPlan != null;

        #endregion

        #region 构造函数

        protected Consultation()
        {
            _diagnoses = new List<TCMDiagnosis>();
            _secondarySyndromes = new List<TCMSyndrome>();
            _symptoms = new List<Symptom>();
            _signs = new List<Sign>();
        }

        public Consultation(
            Guid patientId,
            Guid doctorId,
            string doctorName,
            DateTime consultationDate) : this()
        {
            _patientId = patientId;
            _doctorId = doctorId;
            _doctorName = doctorName ?? throw new ArgumentNullException(nameof(doctorName));
            _consultationDate = consultationDate;
            _consultationNo = GenerateConsultationNo();
            _status = ConsultationStatus.Scheduled;
        }

        #endregion

        #region 看诊流程管理

        /// <summary>
        /// 开始看诊
        /// </summary>
        public void StartConsultation()
        {
            if (_status != ConsultationStatus.Scheduled)
                throw new ConsultationDomainException($"只有已预约的看诊才能开始，当前状态：{_status}");

            _status = ConsultationStatus.InProgress;
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// 记录望诊信息
        /// </summary>
        public void RecordInspection(
            Complexion complexion,
            Spirit spirit,
            BodyShape bodyShape,
            TongueCondition tongueCondition,
            string observations)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            _inspection = new InspectionInfo(
                complexion,
                spirit,
                bodyShape,
                tongueCondition,
                observations);
        }

        /// <summary>
        /// 记录闻诊信息
        /// </summary>
        public void RecordAuscultationOlfaction(
            Voice voice,
            Breathing breathing,
            Cough cough,
            Odor odor,
            string observations)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            _auscultationOlfaction = new AuscultationOlfactionInfo(
                voice,
                breathing,
                cough,
                odor,
                observations);
        }

        /// <summary>
        /// 记录问诊信息
        /// </summary>
        public void RecordInquiry(
            string chiefComplaint,
            string presentIllness,
            string pastHistory,
            AppetiteCondition appetite,
            SleepCondition sleep,
            BowelCondition bowel,
            UrinationCondition urination,
            string menstruation,
            string observations)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            _inquiry = new InquiryInfo(
                chiefComplaint,
                presentIllness,
                pastHistory,
                appetite,
                sleep,
                bowel,
                urination,
                menstruation,
                observations);
        }

        /// <summary>
        /// 记录切诊信息
        /// </summary>
        public void RecordPalpation(
            PulseCondition leftPulse,
            PulseCondition rightPulse,
            string pulseDetails,
            string abdominalPalpation,
            string meridianPalpation,
            string observations)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            _palpation = new PalpationInfo(
                leftPulse,
                rightPulse,
                pulseDetails,
                abdominalPalpation,
                meridianPalpation,
                observations);
        }

        #endregion

        #region 症状体征管理

        /// <summary>
        /// 添加症状
        /// </summary>
        public void AddSymptom(
            string name,
            string description,
            SeverityLevel severity,
            int durationDays)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            var symptom = new Symptom(
                Guid.NewGuid(),
                name,
                description,
                severity,
                durationDays);

            _symptoms.Add(symptom);
        }

        /// <summary>
        /// 添加体征
        /// </summary>
        public void AddSign(
            string name,
            string value,
            string unit,
            bool isAbnormal)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            var sign = new Sign(
                Guid.NewGuid(),
                name,
                value,
                unit,
                isAbnormal);

            _signs.Add(sign);
        }

        #endregion

        #region 诊断管理

        /// <summary>
        /// 添加中医诊断
        /// </summary>
        public void AddDiagnosis(
            string diseaseName,
            string diseaseCode,
            TCMSyndrome syndrome,
            string syndromeAnalysis,
            bool isPrimary = false)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            var diagnosis = new TCMDiagnosis(
                Guid.NewGuid(),
                diseaseName,
                diseaseCode,
                syndrome,
                syndromeAnalysis,
                isPrimary);

            _diagnoses.Add(diagnosis);

            if (isPrimary)
            {
                _primarySyndrome = syndrome;
            }
            else if (!_secondarySyndromes.Contains(syndrome))
            {
                _secondarySyndromes.Add(syndrome);
            }
        }

        /// <summary>
        /// 设置治法
        /// </summary>
        public void SetTreatmentPrinciple(TreatmentPrinciple principle)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            if (principle == null)
                throw new ConsultationDomainException("治法不能为空");

            _treatmentPrinciple = principle;
        }

        #endregion

        #region 治疗方案管理

        /// <summary>
        /// 制定治疗方案
        /// </summary>
        public void CreateTreatmentPlan(
            TreatmentType treatmentType,
            Guid? prescriptionId,
            string acupuncturePoints,
            string massageTechnique,
            string otherTherapy,
            string dietaryAdvice,
            string lifestyleAdvice,
            string precautions)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            if (!HasDiagnosis)
                throw new ConsultationDomainException("必须先完成诊断才能制定治疗方案");

            _treatmentPlan = new TreatmentPlan(
                treatmentType,
                prescriptionId,
                acupuncturePoints,
                massageTechnique,
                otherTherapy,
                dietaryAdvice,
                lifestyleAdvice,
                precautions);
        }

        /// <summary>
        /// 设置医嘱
        /// </summary>
        public void SetMedicalAdvice(string advice)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            if (string.IsNullOrWhiteSpace(advice))
                throw new ConsultationDomainException("医嘱不能为空");

            _medicalAdvice = advice;
        }

        /// <summary>
        /// 设置复诊时间
        /// </summary>
        public void SetNextVisitDate(DateTime nextVisitDate)
        {
            if (!CanModify)
                throw new ConsultationDomainException($"看诊状态为{_status}，不能修改");

            if (nextVisitDate <= _consultationDate)
                throw new ConsultationDomainException("复诊时间必须晚于本次看诊时间");

            _nextVisitDate = nextVisitDate;
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 完成看诊
        /// </summary>
        public void CompleteConsultation()
        {
            if (_status != ConsultationStatus.InProgress)
                throw new ConsultationDomainException($"只有进行中的看诊才能完成，当前状态：{_status}");

            ValidateConsultationCompleteness();

            _status = ConsultationStatus.Completed;
            _endTime = DateTime.Now;
            
            if (_startTime.HasValue)
            {
                _duration = (int)(_endTime.Value - _startTime.Value).TotalMinutes;
            }
        }

        /// <summary>
        /// 取消看诊
        /// </summary>
        public void CancelConsultation(string reason)
        {
            if (_status == ConsultationStatus.Completed)
                throw new ConsultationDomainException("已完成的看诊不能取消");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ConsultationDomainException("取消原因不能为空");

            _status = ConsultationStatus.Cancelled;
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证看诊完整性
        /// </summary>
        private void ValidateConsultationCompleteness()
        {
            // 必须有四诊信息
            if (_inspection == null && _auscultationOlfaction == null && 
                _inquiry == null && _palpation == null)
                throw new ConsultationDomainException("至少需要记录一项四诊信息");

            // 必须有诊断
            if (!HasDiagnosis)
                throw new ConsultationDomainException("必须完成诊断");

            // 必须有治法
            if (_treatmentPrinciple == null)
                throw new ConsultationDomainException("必须确定治法");

            // 必须有治疗方案
            if (!HasTreatmentPlan)
                throw new ConsultationDomainException("必须制定治疗方案");
        }

        #endregion

        #region 私有方法

        private string GenerateConsultationNo()
        {
            return $"CN{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
        }

        #endregion
    }

    #region 实体

    /// <summary>
    /// 中医诊断实体
    /// </summary>
    public class TCMDiagnosis : Entity
    {
        public string DiseaseName { get; private set; }
        public string DiseaseCode { get; private set; }
        public TCMSyndrome Syndrome { get; private set; }
        public string SyndromeAnalysis { get; private set; }
        public bool IsPrimary { get; private set; }

        protected TCMDiagnosis() { }

        public TCMDiagnosis(
            Guid id,
            string diseaseName,
            string diseaseCode,
            TCMSyndrome syndrome,
            string syndromeAnalysis,
            bool isPrimary)
        {
            Id = id;
            DiseaseName = diseaseName;
            DiseaseCode = diseaseCode;
            Syndrome = syndrome;
            SyndromeAnalysis = syndromeAnalysis;
            IsPrimary = isPrimary;
        }
    }

    /// <summary>
    /// 症状实体
    /// </summary>
    public class Symptom : Entity
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public SeverityLevel Severity { get; private set; }
        public int DurationDays { get; private set; }

        protected Symptom() { }

        public Symptom(
            Guid id,
            string name,
            string description,
            SeverityLevel severity,
            int durationDays)
        {
            Id = id;
            Name = name;
            Description = description;
            Severity = severity;
            DurationDays = durationDays;
        }
    }

    /// <summary>
    /// 体征实体
    /// </summary>
    public class Sign : Entity
    {
        public string Name { get; private set; }
        public string Value { get; private set; }
        public string Unit { get; private set; }
        public bool IsAbnormal { get; private set; }

        protected Sign() { }

        public Sign(
            Guid id,
            string name,
            string value,
            string unit,
            bool isAbnormal)
        {
            Id = id;
            Name = name;
            Value = value;
            Unit = unit;
            IsAbnormal = isAbnormal;
        }
    }

    /// <summary>
    /// 治疗方案实体
    /// </summary>
    public class TreatmentPlan : Entity
    {
        public TreatmentType TreatmentType { get; private set; }
        public Guid? PrescriptionId { get; private set; }
        public string AcupuncturePoints { get; private set; }
        public string MassageTechnique { get; private set; }
        public string OtherTherapy { get; private set; }
        public string DietaryAdvice { get; private set; }
        public string LifestyleAdvice { get; private set; }
        public string Precautions { get; private set; }

        protected TreatmentPlan() { }

        public TreatmentPlan(
            TreatmentType treatmentType,
            Guid? prescriptionId,
            string acupuncturePoints,
            string massageTechnique,
            string otherTherapy,
            string dietaryAdvice,
            string lifestyleAdvice,
            string precautions)
        {
            Id = Guid.NewGuid();
            TreatmentType = treatmentType;
            PrescriptionId = prescriptionId;
            AcupuncturePoints = acupuncturePoints;
            MassageTechnique = massageTechnique;
            OtherTherapy = otherTherapy;
            DietaryAdvice = dietaryAdvice;
            LifestyleAdvice = lifestyleAdvice;
            Precautions = precautions;
        }
    }

    #endregion

    #region 值对象

    /// <summary>
    /// 望诊信息
    /// </summary>
    public class InspectionInfo : ValueObject
    {
        public Complexion Complexion { get; private set; }
        public Spirit Spirit { get; private set; }
        public BodyShape BodyShape { get; private set; }
        public TongueCondition TongueCondition { get; private set; }
        public string Observations { get; private set; }

        protected InspectionInfo() { }

        public InspectionInfo(
            Complexion complexion,
            Spirit spirit,
            BodyShape bodyShape,
            TongueCondition tongueCondition,
            string observations)
        {
            Complexion = complexion;
            Spirit = spirit;
            BodyShape = bodyShape;
            TongueCondition = tongueCondition;
            Observations = observations;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Complexion;
            yield return Spirit;
            yield return BodyShape;
            yield return TongueCondition;
            yield return Observations;
        }
    }

    /// <summary>
    /// 闻诊信息
    /// </summary>
    public class AuscultationOlfactionInfo : ValueObject
    {
        public Voice Voice { get; private set; }
        public Breathing Breathing { get; private set; }
        public Cough Cough { get; private set; }
        public Odor Odor { get; private set; }
        public string Observations { get; private set; }

        protected AuscultationOlfactionInfo() { }

        public AuscultationOlfactionInfo(
            Voice voice,
            Breathing breathing,
            Cough cough,
            Odor odor,
            string observations)
        {
            Voice = voice;
            Breathing = breathing;
            Cough = cough;
            Odor = odor;
            Observations = observations;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Voice;
            yield return Breathing;
            yield return Cough;
            yield return Odor;
            yield return Observations;
        }
    }

    /// <summary>
    /// 问诊信息
    /// </summary>
    public class InquiryInfo : ValueObject
    {
        public string ChiefComplaint { get; private set; }
        public string PresentIllness { get; private set; }
        public string PastHistory { get; private set; }
        public AppetiteCondition Appetite { get; private set; }
        public SleepCondition Sleep { get; private set; }
        public BowelCondition Bowel { get; private set; }
        public UrinationCondition Urination { get; private set; }
        public string Menstruation { get; private set; }
        public string Observations { get; private set; }

        protected InquiryInfo() { }

        public InquiryInfo(
            string chiefComplaint,
            string presentIllness,
            string pastHistory,
            AppetiteCondition appetite,
            SleepCondition sleep,
            BowelCondition bowel,
            UrinationCondition urination,
            string menstruation,
            string observations)
        {
            ChiefComplaint = chiefComplaint;
            PresentIllness = presentIllness;
            PastHistory = pastHistory;
            Appetite = appetite;
            Sleep = sleep;
            Bowel = bowel;
            Urination = urination;
            Menstruation = menstruation;
            Observations = observations;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ChiefComplaint;
            yield return PresentIllness;
            yield return PastHistory;
            yield return Appetite;
            yield return Sleep;
            yield return Bowel;
            yield return Urination;
            yield return Menstruation;
            yield return Observations;
        }
    }

    /// <summary>
    /// 切诊信息
    /// </summary>
    public class PalpationInfo : ValueObject
    {
        public PulseCondition LeftPulse { get; private set; }
        public PulseCondition RightPulse { get; private set; }
        public string PulseDetails { get; private set; }
        public string AbdominalPalpation { get; private set; }
        public string MeridianPalpation { get; private set; }
        public string Observations { get; private set; }

        protected PalpationInfo() { }

        public PalpationInfo(
            PulseCondition leftPulse,
            PulseCondition rightPulse,
            string pulseDetails,
            string abdominalPalpation,
            string meridianPalpation,
            string observations)
        {
            LeftPulse = leftPulse;
            RightPulse = rightPulse;
            PulseDetails = pulseDetails;
            AbdominalPalpation = abdominalPalpation;
            MeridianPalpation = meridianPalpation;
            Observations = observations;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return LeftPulse;
            yield return RightPulse;
            yield return PulseDetails;
            yield return AbdominalPalpation;
            yield return MeridianPalpation;
            yield return Observations;
        }
    }

    #endregion

    #region 枚举

    /// <summary>
    /// 看诊状态
    /// </summary>
    public class ConsultationStatus : Enumeration
    {
        public static ConsultationStatus Scheduled = new(1, "已预约");
        public static ConsultationStatus InProgress = new(2, "进行中");
        public static ConsultationStatus Completed = new(3, "已完成");
        public static ConsultationStatus Cancelled = new(4, "已取消");

        public ConsultationStatus(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 治疗类型
    /// </summary>
    public class TreatmentType : Enumeration
    {
        public static TreatmentType HerbalMedicine = new(1, "中药");
        public static TreatmentType Acupuncture = new(2, "针灸");
        public static TreatmentType Massage = new(3, "推拿");
        public static TreatmentType Combined = new(4, "综合治疗");

        public TreatmentType(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 严重程度
    /// </summary>
    public class SeverityLevel : Enumeration
    {
        public static SeverityLevel Mild = new(1, "轻度");
        public static SeverityLevel Moderate = new(2, "中度");
        public static SeverityLevel Severe = new(3, "重度");

        public SeverityLevel(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// 诊断信息 - 用于Repository兼容性
    /// </summary>
    public class DiagnosisInfo
    {
        public string TCMDiagnosis { get; set; } = "";
        public string WesternDiagnosis { get; set; } = "";
    }

    #endregion
}