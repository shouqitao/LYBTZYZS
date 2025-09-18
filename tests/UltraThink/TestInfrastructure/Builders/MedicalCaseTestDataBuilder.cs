using System;
using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Builders
{
    /// <summary>
    /// 医疗案例测试数据构建器 - UltraThink设计
    /// 职责单一：专注于MedicalCase实体的测试数据生成
    /// 代码干净：流畅接口，清晰的医疗流程数据构建
    /// 性能出色：延迟构建，高效生成
    /// </summary>
    public class MedicalCaseTestDataBuilder : TestDataBuilder<MedicalCase, MedicalCaseTestDataBuilder>
    {
        private static readonly string[] Remarks = 
        {
            "患者首次就诊，症状明显",
            "复诊患者，病情有所好转",
            "慢性病管理，需长期调理",
            "急性症状，需要紧急处理",
            "体检发现问题，建议进一步检查",
            "康复期患者，需要定期复查",
            "疑难杂症，需要多科会诊"
        };

        private static readonly string[] CancellationReasons = 
        {
            "患者临时有事取消",
            "医生临时调班",
            "患者已在其他医院就诊",
            "预约时间冲突",
            "患者病情好转，暂不需要就诊"
        };

        public MedicalCaseTestDataBuilder()
        {
            // 设置默认值
            WithCreateTime(DateTime.UtcNow)
                .WithUpdateTime(DateTime.UtcNow)
                .AsActive();
        }

        #region 基本属性构建方法

        public MedicalCaseTestDataBuilder WithId(Guid id)
        {
            _buildActions.Add(m => m.Id = id);
            return this;
        }

        public MedicalCaseTestDataBuilder WithPatientId(Guid patientId)
        {
            _buildActions.Add(m => m.PatientId = patientId);
            return this;
        }

        public MedicalCaseTestDataBuilder WithDoctorId(Guid doctorId)
        {
            _buildActions.Add(m => m.DoctorId = doctorId);
            return this;
        }

        public MedicalCaseTestDataBuilder WithPatientName(string patientName)
        {
            _buildActions.Add(m => m.PatientName = patientName);
            return this;
        }

        public MedicalCaseTestDataBuilder WithDoctorName(string doctorName)
        {
            _buildActions.Add(m => m.DoctorName = doctorName);
            return this;
        }

        public MedicalCaseTestDataBuilder WithConsultationDate(DateTime consultationDate)
        {
            _buildActions.Add(m => m.ConsultationDate = consultationDate);
            return this;
        }

        public MedicalCaseTestDataBuilder WithPrescriptionId(Guid? prescriptionId)
        {
            _buildActions.Add(m => m.PrescriptionId = prescriptionId);
            return this;
        }

        public MedicalCaseTestDataBuilder WithStatus(MedicalCaseStatus status)
        {
            _buildActions.Add(m => m.Status = status);
            return this;
        }

        public MedicalCaseTestDataBuilder WithRemark(string remark)
        {
            _buildActions.Add(m => m.Remark = remark);
            return this;
        }

        public MedicalCaseTestDataBuilder WithRandomRemark()
        {
            return WithRemark(Remarks[_random.Next(Remarks.Length)]);
        }

        #endregion

        #region 时间相关构建方法

        // 注意：MedicalCase实体没有审计字段，已简化架构
        public MedicalCaseTestDataBuilder WithCreateTime(DateTime createTime)
        {
            // 使用ConsultationDate代替CreateTime
            _buildActions.Add(m => m.ConsultationDate = createTime);
            return this;
        }

        public MedicalCaseTestDataBuilder WithUpdateTime(DateTime updateTime)
        {
            // 审计字段已移除，跳过此操作
            return this;
        }

        public MedicalCaseTestDataBuilder WithCompleteTime(DateTime? completeTime)
        {
            // MedicalCase实体没有CompleteTime属性，跳过此操作
            return this;
        }

        public MedicalCaseTestDataBuilder CreatedToday()
        {
            return WithCreateTime(DateTime.Today.AddHours(_random.Next(8, 18)));
        }

        public MedicalCaseTestDataBuilder CreatedDaysAgo(int days)
        {
            return WithCreateTime(DateTime.Now.AddDays(-days));
        }

        #endregion

        #region 状态相关构建方法

        public MedicalCaseTestDataBuilder AsActive()
        {
            return WithStatus(MedicalCaseStatus.Active);
        }

        public MedicalCaseTestDataBuilder AsInactive()
        {
            return WithStatus(MedicalCaseStatus.Cancelled);
        }

        public MedicalCaseTestDataBuilder AsRegistered()
        {
            return WithStatus(MedicalCaseStatus.Active);
        }

        public MedicalCaseTestDataBuilder AsInConsultation()
        {
            return WithStatus(MedicalCaseStatus.Active);
        }

        public MedicalCaseTestDataBuilder AsCompleted()
        {
            return WithStatus(MedicalCaseStatus.Completed)
                .WithCompleteTime(DateTime.Now);
        }

        public MedicalCaseTestDataBuilder AsCancelled(string reason = null)
        {
            return WithStatus(MedicalCaseStatus.Cancelled)
                .WithRemark(reason ?? CancellationReasons[_random.Next(CancellationReasons.Length)]);
        }

        #endregion

        #region 预设场景构建方法

        /// <summary>
        /// 构建一个有效的新医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsValidMedicalCase()
        {
            return WithId(Guid.NewGuid())
                .WithPatientId(Guid.NewGuid())
                .WithDoctorId(Guid.NewGuid())
                .WithPatientName("测试患者")
                .WithDoctorName("测试医生")
                .AsRegistered()
                .AsActive()
                .WithRandomRemark();
        }

        /// <summary>
        /// 构建一个今天创建的医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsTodayCase()
        {
            return AsValidMedicalCase()
                .CreatedToday();
        }

        /// <summary>
        /// 构建一个正在看诊的医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsConsultingCase()
        {
            return AsValidMedicalCase()
                .AsInConsultation()
                .WithRemark("正在进行中医四诊");
        }

        /// <summary>
        /// 构建一个已完成的医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsCompletedCase()
        {
            return AsValidMedicalCase()
                .AsCompleted()
                .WithPrescriptionId(Guid.NewGuid())
                .WithRemark("诊疗完成，已开具处方");
        }

        /// <summary>
        /// 构建一个被取消的医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsCancelledCase()
        {
            return AsValidMedicalCase()
                .AsCancelled()
                .WithUpdateTime(DateTime.Now);
        }

        /// <summary>
        /// 构建一个待处理的医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsPendingCase()
        {
            return AsValidMedicalCase()
                .AsRegistered()
                .WithRemark("等待医生接诊");
        }

        /// <summary>
        /// 构建一个完整流程的医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsFullWorkflowCase()
        {
            var now = DateTime.Now;
            return WithId(Guid.NewGuid())
                .WithPatientId(Guid.NewGuid())
                .WithDoctorId(Guid.NewGuid())
                .WithPatientName("测试患者")
                .WithDoctorName("测试医生")
                .WithPrescriptionId(Guid.NewGuid())
                .AsCompleted()
                .WithCreateTime(now.AddHours(-3))
                .WithRemark("完整诊疗流程，包含看诊和处方");
        }

        /// <summary>
        /// 构建一个历史医疗案例
        /// </summary>
        public MedicalCaseTestDataBuilder AsHistoricalCase(int daysAgo)
        {
            var createTime = DateTime.Now.AddDays(-daysAgo);
            return AsValidMedicalCase()
                .AsCompleted()
                .WithCreateTime(createTime)
                .WithCreateTime(createTime)
                .WithRemark($"{daysAgo}天前的历史案例");
        }

        #endregion

        #region 批量生成辅助方法

        /// <summary>
        /// 生成一组不同状态的医疗案例
        /// </summary>
        public MedicalCase[] BuildMixedStatusCases(int count)
        {
            var cases = new MedicalCase[count];
            var statuses = Enum.GetValues<MedicalCaseStatus>();
            
            for (int i = 0; i < count; i++)
            {
                var status = statuses[i % statuses.Length];
                var builder = AsValidMedicalCase().WithStatus(status);
                
                if (status == MedicalCaseStatus.Completed)
                {
                    // 完成状态无需额外操作
                }
                else if (status == MedicalCaseStatus.Cancelled)
                {
                    builder.WithRemark(CancellationReasons[_random.Next(CancellationReasons.Length)]);
                }
                
                cases[i] = builder.Build();
            }
            
            return cases;
        }

        /// <summary>
        /// 生成一个患者的多次就诊记录
        /// </summary>
        public MedicalCase[] BuildPatientHistory(Guid patientId, int visitCount)
        {
            var cases = new MedicalCase[visitCount];
            
            for (int i = 0; i < visitCount; i++)
            {
                cases[i] = AsValidMedicalCase()
                    .WithPatientId(patientId)
                    .CreatedDaysAgo(i * 30) // 每月一次
                    .AsCompleted()
                    .WithRemark($"第{visitCount - i}次就诊")
                    .Build();
            }
            
            return cases;
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

            if (_entity.PatientId == Guid.Empty)
            {
                _entity.PatientId = Guid.NewGuid();
            }

            if (_entity.ConsultationDate == default)
            {
                _entity.ConsultationDate = DateTime.Now;
            }

            if (string.IsNullOrEmpty(_entity.PatientName))
            {
                _entity.PatientName = "默认患者";
            }

            if (string.IsNullOrEmpty(_entity.DoctorName))
            {
                _entity.DoctorName = "默认医生";
            }

            if (_entity.DoctorId == Guid.Empty)
            {
                _entity.DoctorId = Guid.NewGuid();
            }

            if (_entity.Status == 0)
            {
                _entity.Status = MedicalCaseStatus.Active;
            }
        }
    }
}