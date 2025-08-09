using System;
using LYBT.Models.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Builders
{
    /// <summary>
    /// 医疗案例测试数据构建器 - UltraThink设计
    /// 职责单一：专注于MedicalCase实体的测试数据生成
    /// 代码干净：流畅接口，清晰的医疗流程数据构建
    /// 性能出色：延迟构建，高效生成
    /// </summary>
    public class MedicalCaseTestDataBuilder : TestDataBuilder<MedicalCaseModel, MedicalCaseTestDataBuilder>
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

        public MedicalCaseTestDataBuilder WithUserId(Guid userId)
        {
            _buildActions.Add(m => m.UserId = userId);
            return this;
        }

        public MedicalCaseTestDataBuilder WithConsultationId(Guid? consultationId)
        {
            _buildActions.Add(m => m.ConsultationId = consultationId);
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

        public MedicalCaseTestDataBuilder WithCreateTime(DateTime createTime)
        {
            _buildActions.Add(m => m.CreateTime = createTime);
            return this;
        }

        public MedicalCaseTestDataBuilder WithUpdateTime(DateTime updateTime)
        {
            _buildActions.Add(m => m.UpdateTime = updateTime);
            return this;
        }

        public MedicalCaseTestDataBuilder WithCompleteTime(DateTime? completeTime)
        {
            _buildActions.Add(m => m.CompleteTime = completeTime);
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
            _buildActions.Add(m => m.IsActive = true);
            return this;
        }

        public MedicalCaseTestDataBuilder AsInactive()
        {
            _buildActions.Add(m => m.IsActive = false);
            return this;
        }

        public MedicalCaseTestDataBuilder AsRegistered()
        {
            return WithStatus(MedicalCaseStatus.Registered);
        }

        public MedicalCaseTestDataBuilder AsInConsultation()
        {
            return WithStatus(MedicalCaseStatus.InConsultation)
                .WithConsultationId(Guid.NewGuid());
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
                .WithUserId(Guid.NewGuid())
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
                .WithConsultationId(Guid.NewGuid())
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
                .WithUserId(Guid.NewGuid())
                .WithConsultationId(Guid.NewGuid())
                .WithPrescriptionId(Guid.NewGuid())
                .AsCompleted()
                .WithCreateTime(now.AddHours(-3))
                .WithUpdateTime(now)
                .WithCompleteTime(now)
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
                .WithUpdateTime(createTime.AddHours(2))
                .WithCompleteTime(createTime.AddHours(2))
                .WithRemark($"{daysAgo}天前的历史案例");
        }

        #endregion

        #region 批量生成辅助方法

        /// <summary>
        /// 生成一组不同状态的医疗案例
        /// </summary>
        public MedicalCaseModel[] BuildMixedStatusCases(int count)
        {
            var cases = new MedicalCaseModel[count];
            var statuses = Enum.GetValues<MedicalCaseStatus>();
            
            for (int i = 0; i < count; i++)
            {
                var status = statuses[i % statuses.Length];
                var builder = AsValidMedicalCase().WithStatus(status);
                
                if (status == MedicalCaseStatus.Completed)
                {
                    builder.WithCompleteTime(DateTime.Now.AddHours(-_random.Next(1, 24)));
                }
                else if (status == MedicalCaseStatus.InConsultation)
                {
                    builder.WithConsultationId(Guid.NewGuid());
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
        public MedicalCaseModel[] BuildPatientHistory(Guid patientId, int visitCount)
        {
            var cases = new MedicalCaseModel[visitCount];
            
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
                _entity.Status = MedicalCaseStatus.Registered;
            }

            // 默认激活状态
            if (!_buildActions.Any(a => a.Target?.ToString()?.Contains("IsActive") == true))
            {
                _entity.IsActive = true;
            }
        }
    }
}