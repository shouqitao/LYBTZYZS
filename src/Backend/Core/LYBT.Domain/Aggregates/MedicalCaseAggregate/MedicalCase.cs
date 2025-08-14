using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using CommonVO = LYBT.Domain.ValueObjects;
using LYBT.Domain.ValueObjects;
using LYBT.Domain.Exceptions;
using LYBT.Domain.Aggregates.PatientAggregate.ValueObjects;

namespace LYBT.Domain.Aggregates.MedicalCaseAggregate
{
    /// <summary>
    /// 病案聚合根 - 诊疗流程核心领域模型
    /// 
    /// 职责：
    /// 1. 管理完整诊疗流程
    /// 2. 协调患者、医生、诊断、处方关系
    /// 3. 维护病历记录和治疗历史
    /// 4. 跟踪治疗效果和预后
    /// </summary>
    public class MedicalCase : AggregateRoot
    {
        #region 私有字段

        private string _caseNo;
        private Guid _patientId;
        private string _patientName;
        private int _patientAge;
        private Gender _patientGender;
        private Guid _doctorId;
        private string _doctorName;
        private DateTime _admissionDate;
        private DateTime? _dischargeDate;
        private CommonVO.CaseStatus _status;
        private CommonVO.CaseType _type;
        
        // Repository兼容性属性
        private CaseInfo _caseInfo;
        private FollowUpPlan _followUpPlan;
        
        // 主诉和病史
        private CommonVO.ChiefComplaint _chiefComplaint;
        private CommonVO.PresentIllness _presentIllness;
        private CommonVO.PastMedicalHistory _pastHistory;
        private CommonVO.PersonalHistory _personalHistory;
        private CommonVO.FamilyHistory _familyHistory;
        
        // 诊疗记录
        private readonly List<ConsultationRecord> _consultations;
        private readonly List<DiagnosisRecord> _diagnoses;
        private readonly List<PrescriptionRecord> _prescriptions;
        private readonly List<ExaminationRecord> _examinations;
        private readonly List<TreatmentRecord> _treatments;
        private readonly List<ProgressNote> _progressNotes;
        
        // 中医特色
        private CommonVO.TCMDiagnosis _tcmDiagnosis;
        private CommonVO.Constitution _constitution;
        private CommonVO.TCMSyndrome _primarySyndrome;
        private readonly List<CommonVO.TCMSyndrome> _secondarySyndromes;
        
        // 治疗结果
        private CommonVO.TreatmentOutcome _outcome;
        private string _prognosis;
        private readonly List<FollowUpRecord> _followUps;
        
        // 费用
        private CommonVO.Money _totalCost;
        private readonly List<BillingItem> _billingItems;
        
        // 其他
        private string _notes;
        private bool _isEmergency;
        private bool _isReferral;
        private string _referralReason;
        private Guid? _referredFromDoctorId;

        #endregion

        #region 属性

        public string CaseNo => _caseNo;
        public Guid PatientId => _patientId;
        public string PatientName => _patientName;
        public int PatientAge => _patientAge;
        public Gender PatientGender => _patientGender;
        public Guid DoctorId => _doctorId;
        public string DoctorName => _doctorName;
        public DateTime AdmissionDate => _admissionDate;
        public DateTime? DischargeDate => _dischargeDate;
        public CommonVO.CaseStatus Status => _status;
        public CommonVO.CaseType Type => _type;
        
        // Repository兼容性属性
        public CaseInfo CaseInfo => _caseInfo;
        public FollowUpPlan FollowUpPlan => _followUpPlan;
        
        public CommonVO.ChiefComplaint ChiefComplaint => _chiefComplaint;
        public CommonVO.PresentIllness PresentIllness => _presentIllness;
        public CommonVO.PastMedicalHistory PastHistory => _pastHistory;
        public CommonVO.PersonalHistory PersonalHistory => _personalHistory;
        public CommonVO.FamilyHistory FamilyHistory => _familyHistory;
        
        public IReadOnlyCollection<ConsultationRecord> Consultations => _consultations.AsReadOnly();
        public IReadOnlyCollection<DiagnosisRecord> Diagnoses => _diagnoses.AsReadOnly();
        public IReadOnlyCollection<PrescriptionRecord> Prescriptions => _prescriptions.AsReadOnly();
        public IReadOnlyCollection<ExaminationRecord> Examinations => _examinations.AsReadOnly();
        public IReadOnlyCollection<TreatmentRecord> Treatments => _treatments.AsReadOnly();
        public IReadOnlyCollection<ProgressNote> ProgressNotes => _progressNotes.AsReadOnly();
        
        public CommonVO.TCMDiagnosis TcmDiagnosis => _tcmDiagnosis;
        public CommonVO.Constitution Constitution => _constitution;
        public CommonVO.TCMSyndrome PrimarySyndrome => _primarySyndrome;
        public IReadOnlyCollection<CommonVO.TCMSyndrome> SecondarySyndromes => _secondarySyndromes.AsReadOnly();
        
        public CommonVO.TreatmentOutcome Outcome => _outcome;
        public string Prognosis => _prognosis;
        public IReadOnlyCollection<FollowUpRecord> FollowUps => _followUps.AsReadOnly();
        
        public CommonVO.Money TotalCost => _totalCost;
        public IReadOnlyCollection<BillingItem> BillingItems => _billingItems.AsReadOnly();
        
        public string Notes => _notes;
        public bool IsEmergency => _isEmergency;
        public bool IsReferral => _isReferral;
        public string ReferralReason => _referralReason;
        public Guid? ReferredFromDoctorId => _referredFromDoctorId;

        // 计算属性
        public bool IsActive => _status == CaseStatus.Active;
        public bool IsCompleted => _status == CaseStatus.Completed;
        public bool IsClosed => _status == CaseStatus.Closed;
        public int TreatmentDays => _dischargeDate.HasValue 
            ? (_dischargeDate.Value - _admissionDate).Days + 1 
            : (DateTime.Now - _admissionDate).Days + 1;
        public int ConsultationCount => _consultations.Count;
        public int PrescriptionCount => _prescriptions.Count;
        public bool HasDiagnosis => _diagnoses.Any();
        public bool HasTreatment => _treatments.Any() || _prescriptions.Any();

        #endregion

        #region 构造函数

        protected MedicalCase()
        {
            _consultations = new List<ConsultationRecord>();
            _diagnoses = new List<DiagnosisRecord>();
            _prescriptions = new List<PrescriptionRecord>();
            _examinations = new List<ExaminationRecord>();
            _treatments = new List<TreatmentRecord>();
            _progressNotes = new List<ProgressNote>();
            _secondarySyndromes = new List<CommonVO.TCMSyndrome>();
            _followUps = new List<FollowUpRecord>();
            _billingItems = new List<BillingItem>();
        }

        public MedicalCase(
            Guid patientId,
            string patientName,
            int patientAge,
            Gender patientGender,
            Guid doctorId,
            string doctorName,
            CommonVO.CaseType type,
            bool isEmergency = false) : this()
        {
            _caseNo = GenerateCaseNo();
            _patientId = patientId;
            _patientName = patientName ?? throw new MedicalCaseDomainException("患者姓名不能为空");
            _patientAge = patientAge;
            _patientGender = patientGender ?? throw new MedicalCaseDomainException("患者性别不能为空");
            _doctorId = doctorId;
            _doctorName = doctorName ?? throw new MedicalCaseDomainException("医生姓名不能为空");
            _type = type;
            _isEmergency = isEmergency;
            _admissionDate = DateTime.Now;
            _status = CommonVO.CaseStatus.Active;
            _totalCost = new CommonVO.Money(0, "CNY");
            
            // 初始化Repository兼容性属性
            _caseInfo = new CaseInfo(_caseNo, type, isEmergency);
            _followUpPlan = new FollowUpPlan(null, "未安排随访", null);
        }

        #endregion

        #region 病史管理

        /// <summary>
        /// 记录主诉
        /// </summary>
        public void RecordChiefComplaint(string complaint, int durationDays, string severity)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _chiefComplaint = new CommonVO.ChiefComplaint(complaint, durationDays.ToString(), severity);
        }

        /// <summary>
        /// 记录现病史
        /// </summary>
        public void RecordPresentIllness(
            string onset,
            string development,
            string currentStatus,
            string treatmentHistory)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _presentIllness = new CommonVO.PresentIllness(onset, development, currentStatus, treatmentHistory);
        }

        /// <summary>
        /// 记录既往史
        /// </summary>
        public void RecordPastHistory(
            List<string> diseases,
            List<string> surgeries,
            List<string> allergies,
            List<string> medications)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _pastHistory = new CommonVO.PastMedicalHistory(
                string.Join("; ", diseases ?? new List<string>()), 
                string.Join("; ", surgeries ?? new List<string>()), 
                string.Join("; ", allergies ?? new List<string>()), 
                string.Join("; ", medications ?? new List<string>()));
        }

        /// <summary>
        /// 记录个人史
        /// </summary>
        public void RecordPersonalHistory(
            string occupation,
            string lifestyle,
            string dietaryHabits,
            string smokingHistory,
            string drinkingHistory)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _personalHistory = new CommonVO.PersonalHistory(
                occupation,
                lifestyle,
                dietaryHabits,
                smokingHistory,
                drinkingHistory);
        }

        /// <summary>
        /// 记录家族史
        /// </summary>
        public void RecordFamilyHistory(List<string> familyDiseases)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _familyHistory = new CommonVO.FamilyHistory(string.Join(", ", familyDiseases));
        }

        #endregion

        #region 诊疗记录管理

        /// <summary>
        /// 添加看诊记录
        /// </summary>
        public void AddConsultation(
            Guid consultationId,
            DateTime consultationDate,
            string fourDiagnosisInfo,
            string diagnosis,
            string treatmentPlan)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能添加看诊记录");

            var consultation = new ConsultationRecord(
                Guid.NewGuid(),
                consultationId,
                consultationDate,
                fourDiagnosisInfo,
                diagnosis,
                treatmentPlan,
                _consultations.Count + 1);

            _consultations.Add(consultation);
        }

        /// <summary>
        /// 添加诊断记录
        /// </summary>
        public void AddDiagnosis(
            string diseaseName,
            string diseaseCode,
            CommonVO.TCMSyndrome syndrome,
            CommonVO.DiagnosisType type,
            bool isPrimary = false)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能添加诊断");

            // 主诊断唯一性验证
            if (isPrimary && _diagnoses.Any(d => d.IsPrimary))
                throw new MedicalCaseDomainException("已存在主诊断");

            var diagnosis = new DiagnosisRecord(
                Guid.NewGuid(),
                diseaseName,
                diseaseCode,
                syndrome,
                type,
                isPrimary,
                DateTime.Now);

            _diagnoses.Add(diagnosis);

            // 更新中医证型
            if (syndrome != null)
            {
                if (isPrimary)
                {
                    _primarySyndrome = syndrome;
                }
                else if (!_secondarySyndromes.Contains(syndrome))
                {
                    _secondarySyndromes.Add(syndrome);
                }
            }
        }

        /// <summary>
        /// 添加处方记录
        /// </summary>
        public void AddPrescription(
            Guid prescriptionId,
            string prescriptionNo,
            DateTime prescriptionDate,
            CommonVO.Money amount)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能添加处方");

            var prescription = new PrescriptionRecord(
                Guid.NewGuid(),
                prescriptionId,
                prescriptionNo,
                prescriptionDate,
                amount);

            _prescriptions.Add(prescription);
            
            // 更新费用
            AddBillingItem("处方费", amount, CommonVO.BillingCategory.Medicine);
        }

        /// <summary>
        /// 添加检查记录
        /// </summary>
        public void AddExamination(
            string examinationType,
            string examinationItem,
            DateTime examinationDate,
            string result,
            string conclusion)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能添加检查");

            var examination = new ExaminationRecord(
                Guid.NewGuid(),
                examinationType,
                examinationItem,
                examinationDate,
                result,
                conclusion);

            _examinations.Add(examination);
        }

        /// <summary>
        /// 添加治疗记录
        /// </summary>
        public void AddTreatment(
            string treatmentType,
            string treatmentMethod,
            DateTime treatmentDate,
            string treatmentDetails,
            string effect)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能添加治疗");

            var treatment = new TreatmentRecord(
                Guid.NewGuid(),
                treatmentType,
                treatmentMethod,
                treatmentDate,
                treatmentDetails,
                effect);

            _treatments.Add(treatment);
        }

        /// <summary>
        /// 添加病程记录
        /// </summary>
        public void AddProgressNote(
            DateTime recordDate,
            string symptoms,
            string signs,
            string assessment,
            string plan,
            Guid recordedBy,
            string recorderName)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能添加病程记录");

            var progressNote = new ProgressNote(
                Guid.NewGuid(),
                recordDate,
                symptoms,
                signs,
                assessment,
                plan,
                recordedBy,
                recorderName);

            _progressNotes.Add(progressNote);
        }

        #endregion

        #region 中医诊断管理

        /// <summary>
        /// 设置中医诊断
        /// </summary>
        public void SetTCMDiagnosis(
            string disease,
            CommonVO.TCMSyndrome syndrome,
            string syndromeAnalysis,
            CommonVO.TreatmentPrinciple treatmentPrinciple)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _tcmDiagnosis = new TCMDiagnosis(disease, syndrome.ToString(), syndromeAnalysis, treatmentPrinciple.ToString());
            _primarySyndrome = syndrome;
        }

        /// <summary>
        /// 设置体质辨识
        /// </summary>
        public void SetConstitution(CommonVO.Constitution constitution)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _constitution = constitution ?? throw new MedicalCaseDomainException("体质类型不能为空");
        }

        #endregion

        #region 治疗结果管理

        /// <summary>
        /// 记录治疗结果
        /// </summary>
        public void RecordOutcome(
            CommonVO.TreatmentEffect effect,
            string symptoms,
            string signs,
            string labResults,
            string complications)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            _outcome = new TreatmentOutcome(effect.ToString(), symptoms, signs, labResults, complications);
        }

        /// <summary>
        /// 设置预后
        /// </summary>
        public void SetPrognosis(string prognosis)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"病案状态为{_status}，不能修改");

            if (string.IsNullOrWhiteSpace(prognosis))
                throw new MedicalCaseDomainException("预后评估不能为空");

            _prognosis = prognosis;
        }

        /// <summary>
        /// 添加随访记录
        /// </summary>
        public void AddFollowUp(
            DateTime followUpDate,
            string method,
            string status,
            string symptoms,
            string medication,
            string advice,
            DateTime? nextFollowUpDate)
        {
            var followUp = new FollowUpRecord(
                Guid.NewGuid(),
                followUpDate,
                method,
                status,
                symptoms,
                medication,
                advice,
                nextFollowUpDate);

            _followUps.Add(followUp);
            
            // 更新随访计划
            _followUpPlan = new FollowUpPlan(nextFollowUpDate, method, advice);
        }

        #endregion

        #region 费用管理

        /// <summary>
        /// 添加收费项目
        /// </summary>
        public void AddBillingItem(string itemName, CommonVO.Money amount, CommonVO.BillingCategory category)
        {
            if (amount == null || amount.Amount <= 0)
                throw new MedicalCaseDomainException("费用金额必须大于0");

            var billingItem = new BillingItem(
                Guid.NewGuid(),
                itemName,
                amount,
                category,
                DateTime.Now);

            _billingItems.Add(billingItem);
            RecalculateTotalCost();
        }

        /// <summary>
        /// 重新计算总费用
        /// </summary>
        private void RecalculateTotalCost()
        {
            if (!_billingItems.Any())
            {
                _totalCost = new CommonVO.Money(0, "CNY");
                return;
            }

            var total = _billingItems.Sum(item => item.Amount.Amount);
            _totalCost = new CommonVO.Money(total, "CNY");
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 完成病案
        /// </summary>
        public void Complete(string summary)
        {
            if (_status != CaseStatus.Active)
                throw new MedicalCaseDomainException($"只有活动状态的病案才能完成，当前状态：{_status}");

            ValidateCompleteness();
            
            _status = CaseStatus.Completed;
            _dischargeDate = DateTime.Now;
            
            if (!string.IsNullOrWhiteSpace(summary))
            {
                AddNotes($"出院小结：{summary}");
            }
        }

        /// <summary>
        /// 关闭病案
        /// </summary>
        public void Close(string reason)
        {
            if (_status == CaseStatus.Closed)
                return;

            if (string.IsNullOrWhiteSpace(reason))
                throw new MedicalCaseDomainException("关闭病案必须提供原因");

            _status = CaseStatus.Closed;
            if (!_dischargeDate.HasValue)
            {
                _dischargeDate = DateTime.Now;
            }
            
            AddNotes($"病案关闭原因：{reason}");
        }

        /// <summary>
        /// 重新激活病案
        /// </summary>
        public void Reactivate(string reason)
        {
            if (_status != CaseStatus.Closed)
                throw new MedicalCaseDomainException("只有已关闭的病案才能重新激活");

            if (string.IsNullOrWhiteSpace(reason))
                throw new MedicalCaseDomainException("重新激活病案必须提供原因");

            _status = CaseStatus.Active;
            _dischargeDate = null;
            
            AddNotes($"病案重新激活原因：{reason}");
        }

        #endregion

        #region 转诊管理

        /// <summary>
        /// 标记为转诊病案
        /// </summary>
        public void MarkAsReferral(Guid referredFromDoctorId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new MedicalCaseDomainException("转诊原因不能为空");

            _isReferral = true;
            _referredFromDoctorId = referredFromDoctorId;
            _referralReason = reason;
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 生成病案摘要
        /// </summary>
        public string GenerateSummary()
        {
            var summary = new List<string>
            {
                $"病案号：{_caseNo}",
                $"患者：{_patientName}，{_patientGender.Name}，{_patientAge}岁",
                $"入院日期：{_admissionDate:yyyy-MM-dd}",
                $"主诊医生：{_doctorName}"
            };

            if (_chiefComplaint != null)
            {
                summary.Add($"主诉：{_chiefComplaint.Description}");
            }

            if (_diagnoses.Any())
            {
                var primaryDiagnosis = _diagnoses.FirstOrDefault(d => d.IsPrimary);
                if (primaryDiagnosis != null)
                {
                    summary.Add($"主要诊断：{primaryDiagnosis.DiseaseName}");
                }
            }

            if (_tcmDiagnosis != null)
            {
                summary.Add($"中医诊断：{_tcmDiagnosis.Syndrome}，治法：{_tcmDiagnosis.Pattern}");
            }

            summary.Add($"诊疗次数：{_consultations.Count}次");
            summary.Add($"处方数量：{_prescriptions.Count}个");

            if (_outcome != null)
            {
                summary.Add($"治疗效果：{_outcome.Effect}");
            }

            if (_dischargeDate.HasValue)
            {
                summary.Add($"出院日期：{_dischargeDate:yyyy-MM-dd}");
                summary.Add($"住院天数：{TreatmentDays}天");
            }

            summary.Add($"总费用：{_totalCost}");

            return string.Join("\n", summary);
        }

        /// <summary>
        /// 获取最近的诊疗记录
        /// </summary>
        public ConsultationRecord GetLatestConsultation()
        {
            return _consultations.OrderByDescending(c => c.ConsultationDate).FirstOrDefault();
        }

        /// <summary>
        /// 获取主要诊断
        /// </summary>
        public DiagnosisRecord GetPrimaryDiagnosis()
        {
            return _diagnoses.FirstOrDefault(d => d.IsPrimary);
        }

        /// <summary>
        /// 检查是否需要随访
        /// </summary>
        public bool NeedsFollowUp()
        {
            if (!_followUps.Any())
                return _status == CaseStatus.Completed;

            var lastFollowUp = _followUps.OrderByDescending(f => f.FollowUpDate).First();
            return lastFollowUp.NextFollowUpDate.HasValue && 
                   lastFollowUp.NextFollowUpDate.Value <= DateTime.Now.AddDays(7);
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证病案完整性
        /// </summary>
        private void ValidateCompleteness()
        {
            if (_chiefComplaint == null)
                throw new MedicalCaseDomainException("病案必须有主诉");

            if (!_diagnoses.Any())
                throw new MedicalCaseDomainException("病案必须有诊断");

            if (!_consultations.Any())
                throw new MedicalCaseDomainException("病案必须有看诊记录");

            if (_outcome == null)
                throw new MedicalCaseDomainException("病案必须有治疗结果评估");
        }

        #endregion

        #region 私有方法

        private string GenerateCaseNo()
        {
            return $"MC{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
        }

        private void AddNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return;

            _notes = string.IsNullOrWhiteSpace(_notes)
                ? notes
                : $"{_notes}\n{DateTime.Now:yyyy-MM-dd HH:mm} - {notes}";
        }

        #endregion
    }

    #region Repository兼容性值对象

    /// <summary>
    /// 病案信息值对象 - Repository兼容性
    /// </summary>
    public class CaseInfo : ValueObject
    {
        public string CaseNumber { get; }
        public CommonVO.CaseType CaseType { get; }
        public bool IsEmergency { get; }

        public CaseInfo(string caseNumber, CommonVO.CaseType caseType, bool isEmergency)
        {
            CaseNumber = caseNumber ?? throw new ArgumentNullException(nameof(caseNumber));
            CaseType = caseType;
            IsEmergency = isEmergency;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return CaseNumber;
            yield return CaseType;
            yield return IsEmergency;
        }
    }

    /// <summary>
    /// 随访计划值对象 - Repository兼容性
    /// </summary>
    public class FollowUpPlan : ValueObject
    {
        public DateTime? NextFollowUpDate { get; }
        public string FollowUpMethod { get; }
        public string Notes { get; }

        public FollowUpPlan(DateTime? nextFollowUpDate, string followUpMethod, string notes)
        {
            NextFollowUpDate = nextFollowUpDate;
            FollowUpMethod = followUpMethod ?? "未定";
            Notes = notes;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return NextFollowUpDate?.ToString("yyyy-MM-dd") ?? "";
            yield return FollowUpMethod;
            yield return Notes ?? "";
        }
    }

    #endregion
}