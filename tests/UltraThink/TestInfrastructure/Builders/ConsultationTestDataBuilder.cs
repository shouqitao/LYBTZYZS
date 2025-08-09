using System;
using LYBT.Models.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Builders
{
    /// <summary>
    /// 看诊测试数据构建器 - UltraThink设计
    /// 职责单一：专注于Consultation实体的测试数据生成
    /// 代码干净：流畅接口，清晰的中医四诊数据构建
    /// 性能出色：延迟构建，高效生成
    /// </summary>
    public class ConsultationTestDataBuilder : TestDataBuilder<ConsultationModel, ConsultationTestDataBuilder>
    {
        private static readonly string[] ChiefComplaints = 
        {
            "头痛3天，伴恶寒发热",
            "咳嗽2周，痰多色黄",
            "腹痛腹泻1天",
            "失眠多梦半月余",
            "腰膝酸软，畏寒肢冷",
            "心悸气短，活动后加重",
            "口干口苦，大便秘结"
        };

        private static readonly string[] TCMDiagnoses = 
        {
            "风寒感冒",
            "风热感冒",
            "脾胃虚寒",
            "肝郁气滞",
            "肾阳虚证",
            "心血不足",
            "阴虚火旺",
            "湿热蕴结",
            "血瘀证",
            "痰湿阻络"
        };

        private static readonly string[] TreatmentPrinciples = 
        {
            "疏风散寒，解表发汗",
            "清热解毒，宣肺止咳",
            "健脾和胃，温中散寒",
            "疏肝理气，活血化瘀",
            "温补肾阳，强筋健骨",
            "补益心血，安神定志",
            "滋阴清热，养血安神",
            "清热利湿，通腑泄浊"
        };

        private static readonly string[] TongueInspections = 
        {
            "舌淡红，苔薄白",
            "舌红，苔黄腻",
            "舌淡，苔白腻",
            "舌暗红，有瘀斑",
            "舌淡胖，边有齿痕",
            "舌红少苔",
            "舌绛，苔黄燥"
        };

        private static readonly string[] PulseConditions = 
        {
            "脉浮紧",
            "脉滑数",
            "脉沉细",
            "脉弦",
            "脉沉迟无力",
            "脉细数",
            "脉濡缓"
        };

        public ConsultationTestDataBuilder()
        {
            // 设置默认值
            WithCreateTime(DateTime.UtcNow)
                .WithUpdateTime(DateTime.UtcNow)
                .WithStatus(CommonStatus.Enabled);
        }

        #region 基本属性构建方法

        public ConsultationTestDataBuilder WithId(Guid id)
        {
            _buildActions.Add(c => c.Id = id);
            return this;
        }

        public ConsultationTestDataBuilder WithMedicalCaseId(Guid medicalCaseId)
        {
            _buildActions.Add(c => c.MedicalCaseId = medicalCaseId);
            return this;
        }

        public ConsultationTestDataBuilder WithPatientId(Guid patientId)
        {
            _buildActions.Add(c => c.PatientId = patientId);
            return this;
        }

        public ConsultationTestDataBuilder WithUserId(Guid userId)
        {
            _buildActions.Add(c => c.UserId = userId);
            return this;
        }

        public ConsultationTestDataBuilder WithConsultationTime(DateTime consultationTime)
        {
            _buildActions.Add(c => c.ConsultationTime = consultationTime);
            return this;
        }

        public ConsultationTestDataBuilder WithDiagnosis(string diagnosis)
        {
            _buildActions.Add(c => c.Diagnosis = diagnosis);
            return this;
        }

        public ConsultationTestDataBuilder WithRandomDiagnosis()
        {
            return WithDiagnosis(ChiefComplaints[_random.Next(ChiefComplaints.Length)]);
        }

        #endregion

        #region 中医四诊构建方法

        public ConsultationTestDataBuilder WithInspection(string inspection)
        {
            _buildActions.Add(c => c.Inspection = inspection);
            return this;
        }

        public ConsultationTestDataBuilder WithAuscultationOlfaction(string auscultationOlfaction)
        {
            _buildActions.Add(c => c.AuscultationOlfaction = auscultationOlfaction);
            return this;
        }

        public ConsultationTestDataBuilder WithInquiry(string inquiry)
        {
            _buildActions.Add(c => c.Inquiry = inquiry);
            return this;
        }

        public ConsultationTestDataBuilder WithPalpation(string palpation)
        {
            _buildActions.Add(c => c.Palpation = palpation);
            return this;
        }

        public ConsultationTestDataBuilder WithTongueInspection(string tongueInspection)
        {
            _buildActions.Add(c => c.TongueInspection = tongueInspection);
            return this;
        }

        public ConsultationTestDataBuilder WithRandomTongueInspection()
        {
            return WithTongueInspection(TongueInspections[_random.Next(TongueInspections.Length)]);
        }

        public ConsultationTestDataBuilder WithPulseCondition(string pulseCondition)
        {
            _buildActions.Add(c => c.PulseCondition = pulseCondition);
            return this;
        }

        public ConsultationTestDataBuilder WithRandomPulseCondition()
        {
            return WithPulseCondition(PulseConditions[_random.Next(PulseConditions.Length)]);
        }

        public ConsultationTestDataBuilder WithFullTCMExamination()
        {
            return WithInspection("面色萎黄，精神倦怠")
                .WithAuscultationOlfaction("语声低微，口气清淡")
                .WithInquiry("食欲不振，大便溏薄，小便清长")
                .WithPalpation("腹软无压痛，脉细弱")
                .WithRandomTongueInspection()
                .WithRandomPulseCondition();
        }

        #endregion

        #region 中医诊断构建方法

        public ConsultationTestDataBuilder WithTCMDiagnosis(string tcmDiagnosis)
        {
            _buildActions.Add(c => c.TCMDiagnosis = tcmDiagnosis);
            return this;
        }

        public ConsultationTestDataBuilder WithRandomTCMDiagnosis()
        {
            return WithTCMDiagnosis(TCMDiagnoses[_random.Next(TCMDiagnoses.Length)]);
        }

        public ConsultationTestDataBuilder WithTreatmentPrinciple(string treatmentPrinciple)
        {
            _buildActions.Add(c => c.TreatmentPrinciple = treatmentPrinciple);
            return this;
        }

        public ConsultationTestDataBuilder WithRandomTreatmentPrinciple()
        {
            return WithTreatmentPrinciple(TreatmentPrinciples[_random.Next(TreatmentPrinciples.Length)]);
        }

        public ConsultationTestDataBuilder WithMedicalAdvice(string medicalAdvice)
        {
            _buildActions.Add(c => c.MedicalAdvice = medicalAdvice);
            return this;
        }

        public ConsultationTestDataBuilder WithRemark(string remark)
        {
            _buildActions.Add(c => c.Remark = remark);
            return this;
        }

        #endregion

        #region 状态和审计字段构建方法

        public ConsultationTestDataBuilder WithStatus(CommonStatus status)
        {
            _buildActions.Add(c => c.Status = status);
            return this;
        }

        public ConsultationTestDataBuilder AsActive()
        {
            return WithStatus(CommonStatus.Enabled);
        }

        public ConsultationTestDataBuilder AsInactive()
        {
            return WithStatus(CommonStatus.Disabled);
        }

        public ConsultationTestDataBuilder WithCreateTime(DateTime createTime)
        {
            _buildActions.Add(c => c.CreateTime = createTime);
            return this;
        }

        public ConsultationTestDataBuilder WithUpdateTime(DateTime updateTime)
        {
            _buildActions.Add(c => c.UpdateTime = updateTime);
            return this;
        }

        #endregion

        #region 预设场景构建方法

        /// <summary>
        /// 构建一个有效的看诊记录
        /// </summary>
        public ConsultationTestDataBuilder AsValidConsultation()
        {
            return WithId(Guid.NewGuid())
                .WithMedicalCaseId(Guid.NewGuid())
                .WithPatientId(Guid.NewGuid())
                .WithUserId(Guid.NewGuid())
                .WithConsultationTime(DateTime.Now)
                .WithRandomDiagnosis()
                .AsActive();
        }

        /// <summary>
        /// 构建一个完整的中医看诊记录
        /// </summary>
        public ConsultationTestDataBuilder AsCompleteTCMConsultation()
        {
            return AsValidConsultation()
                .WithFullTCMExamination()
                .WithRandomTCMDiagnosis()
                .WithRandomTreatmentPrinciple()
                .WithMedicalAdvice("忌食生冷辛辣，注意保暖，按时服药")
                .WithRemark("患者症状典型，预后良好");
        }

        /// <summary>
        /// 构建一个刚开始的看诊记录
        /// </summary>
        public ConsultationTestDataBuilder AsNewConsultation()
        {
            return WithId(Guid.NewGuid())
                .WithMedicalCaseId(Guid.NewGuid())
                .WithPatientId(Guid.NewGuid())
                .WithUserId(Guid.NewGuid())
                .WithConsultationTime(DateTime.Now)
                .WithDiagnosis("")
                .AsActive();
        }

        /// <summary>
        /// 构建一个风寒感冒的看诊记录
        /// </summary>
        public ConsultationTestDataBuilder AsWindColdCase()
        {
            return AsValidConsultation()
                .WithDiagnosis("恶寒发热，头身疼痛，鼻塞流涕")
                .WithInspection("面色苍白，精神尚可")
                .WithAuscultationOlfaction("咳嗽声重，痰白稀薄")
                .WithInquiry("恶寒重发热轻，无汗，口不渴")
                .WithPalpation("脉浮紧")
                .WithTongueInspection("舌淡红，苔薄白")
                .WithPulseCondition("脉浮紧")
                .WithTCMDiagnosis("风寒感冒")
                .WithTreatmentPrinciple("疏风散寒，解表发汗")
                .WithMedicalAdvice("多饮温开水，注意保暖，避风寒");
        }

        /// <summary>
        /// 构建一个脾胃虚寒的看诊记录
        /// </summary>
        public ConsultationTestDataBuilder AsSpleenStomachColdCase()
        {
            return AsValidConsultation()
                .WithDiagnosis("腹痛隐隐，喜温喜按，食少纳呆")
                .WithInspection("面色萎黄，形体消瘦")
                .WithAuscultationOlfaction("语声低微，口淡无味")
                .WithInquiry("食欲不振，腹部冷痛，大便溏薄")
                .WithPalpation("腹软喜按，脉沉细")
                .WithTongueInspection("舌淡胖，边有齿痕，苔白滑")
                .WithPulseCondition("脉沉细无力")
                .WithTCMDiagnosis("脾胃虚寒证")
                .WithTreatmentPrinciple("健脾和胃，温中散寒")
                .WithMedicalAdvice("饮食宜温软易消化，忌生冷油腻");
        }

        /// <summary>
        /// 构建一个已完成的看诊记录
        /// </summary>
        public ConsultationTestDataBuilder AsCompletedConsultation()
        {
            return AsCompleteTCMConsultation()
                .WithCreateTime(DateTime.Now.AddHours(-2))
                .WithUpdateTime(DateTime.Now.AddMinutes(-30));
        }

        #endregion

        /// <summary>
        /// 应用默认值
        /// </summary>
        protected override void ApplyDefaults()
        {
            if (_entity.Id == Guid.Empty)
            {
                _entity.Id = Guid.NewGuid();
            }

            if (_entity.MedicalCaseId == Guid.Empty)
            {
                _entity.MedicalCaseId = Guid.NewGuid();
            }

            if (_entity.PatientId == Guid.Empty)
            {
                _entity.PatientId = Guid.NewGuid();
            }

            if (_entity.UserId == Guid.Empty)
            {
                _entity.UserId = Guid.NewGuid();
            }

            if (_entity.ConsultationTime == default)
            {
                _entity.ConsultationTime = DateTime.Now;
            }

            if (string.IsNullOrEmpty(_entity.Diagnosis))
            {
                _entity.Diagnosis = "待诊断";
            }

            if (_entity.CreateTime == default)
            {
                _entity.CreateTime = DateTime.UtcNow;
            }

            if (_entity.UpdateTime == default)
            {
                _entity.UpdateTime = DateTime.UtcNow;
            }

            if (_entity.Status == 0)
            {
                _entity.Status = CommonStatus.Enabled;
            }
        }
    }
}