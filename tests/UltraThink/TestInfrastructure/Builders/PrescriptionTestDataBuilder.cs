using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Builders
{
    /// <summary>
    /// 处方测试数据构建器 - UltraThink设计
    /// 职责单一：专注于Prescription实体的测试数据生成
    /// 代码干净：流畅接口，清晰的处方数据构建
    /// 性能出色：延迟构建，高效生成
    /// </summary>
    public class PrescriptionTestDataBuilder : TestDataBuilder<Prescription, PrescriptionTestDataBuilder>
    {
        private static readonly string[] Diagnoses = 
        {
            "风寒感冒，恶寒发热",
            "风热感冒，咽喉肿痛",
            "脾胃虚寒，食欲不振",
            "肝郁气滞，胸胁胀痛",
            "肾阳虚证，腰膝酸软",
            "心血不足，心悸失眠",
            "阴虚火旺，潮热盗汗",
            "湿热蕴结，口苦口臭",
            "血瘀证，肌肤甲错",
            "痰湿阻络，肢体麻木"
        };

        private static readonly string[] Advices = 
        {
            "忌食生冷辛辣，注意保暖",
            "多饮温开水，清淡饮食",
            "饮食规律，忌暴饮暴食",
            "保持心情舒畅，避免生气",
            "注意休息，避免劳累",
            "按时服药，饭后温服",
            "适当运动，增强体质",
            "戒烟戒酒，规律作息",
            "定期复诊，观察疗效",
            "遵医嘱服药，不可随意停药"
        };

        private readonly List<PrescriptionItem> _items = new List<PrescriptionItem>();

        public PrescriptionTestDataBuilder()
        {
            // 设置默认值
            WithCreateTime(DateTime.UtcNow)
                .WithUpdateTime(DateTime.UtcNow)
                .WithStatus(PrescriptionStatus.Draft);
        }

        #region 基本属性构建方法

        public PrescriptionTestDataBuilder WithId(Guid id)
        {
            _buildActions.Add(p => p.Id = id);
            return this;
        }

        public PrescriptionTestDataBuilder WithPatientId(Guid patientId)
        {
            _buildActions.Add(p => p.PatientId = patientId);
            return this;
        }

        public PrescriptionTestDataBuilder WithUserId(Guid userId)
        {
            _buildActions.Add(p => p.UserId = userId);
            return this;
        }

        public PrescriptionTestDataBuilder WithIndication(string indication)
        {
            _buildActions.Add(p => p.Indication = indication);
            return this;
        }

        public PrescriptionTestDataBuilder WithMedicalCaseId(Guid medicalCaseId)
        {
            _buildActions.Add(p => p.MedicalCaseId = medicalCaseId);
            return this;
        }

        public PrescriptionTestDataBuilder WithRandomIndication()
        {
            return WithIndication(Diagnoses[_random.Next(Diagnoses.Length)]);
        }

        public PrescriptionTestDataBuilder WithAdvice(string advice)
        {
            _buildActions.Add(p => p.Advice = advice);
            return this;
        }

        public PrescriptionTestDataBuilder WithRandomAdvice()
        {
            return WithAdvice(Advices[_random.Next(Advices.Length)]);
        }

        public PrescriptionTestDataBuilder WithDosageCount(int dosageCount)
        {
            _buildActions.Add(p => p.DosageCount = dosageCount);
            return this;
        }

        public PrescriptionTestDataBuilder WithDiscount(decimal discount)
        {
            _buildActions.Add(p => p.Discount = discount);
            return this;
        }

        public PrescriptionTestDataBuilder WithFormulaSource(string formulaSource)
        {
            _buildActions.Add(p => p.FormulaSource = formulaSource);
            return this;
        }

        public PrescriptionTestDataBuilder WithRemark(string remark)
        {
            _buildActions.Add(p => p.Remark = remark);
            return this;
        }

        #endregion

        #region 处方项构建方法

        public PrescriptionTestDataBuilder AddItem(PrescriptionItem item)
        {
            _items.Add(item);
            return this;
        }

        public PrescriptionTestDataBuilder AddItem(Guid herbId, string herbName, decimal quantity, string unit, decimal unitPrice)
        {
            var item = new PrescriptionItem
            {
                Id = Guid.NewGuid(),
                HerbId = herbId,
                HerbName = herbName,
                Quantity = quantity,
                Unit = unit,
                UnitPrice = unitPrice
                // Amount是计算属性，不需要设置
            };
            _items.Add(item);
            return this;
        }

        public PrescriptionTestDataBuilder AddRandomItems(int count)
        {
            var herbNames = new[] { "麻黄", "桂枝", "甘草", "大枣", "生姜", "白芍", "细辛", "附子", "黄芪", "当归" };
            var units = new[] { "g", "克", "片", "枚" };

            for (int i = 0; i < count; i++)
            {
                AddItem(
                    Guid.NewGuid(),
                    herbNames[_random.Next(herbNames.Length)],
                    _random.Next(3, 30),
                    units[_random.Next(units.Length)],
                    _random.Next(5, 50) * 0.1m
                );
            }
            return this;
        }

        public PrescriptionTestDataBuilder WithClassicFormula()
        {
            // 麻黄汤
            AddItem(Guid.NewGuid(), "麻黄", 9, "g", 2.5m);
            AddItem(Guid.NewGuid(), "桂枝", 6, "g", 3.0m);
            AddItem(Guid.NewGuid(), "杏仁", 9, "g", 4.5m);
            AddItem(Guid.NewGuid(), "甘草", 3, "g", 2.0m);
            return this;
        }

        #endregion

        #region 状态相关构建方法

        public PrescriptionTestDataBuilder WithStatus(PrescriptionStatus status)
        {
            _buildActions.Add(p => p.Status = status);
            return this;
        }

        public PrescriptionTestDataBuilder AsDraft()
        {
            return WithStatus(PrescriptionStatus.Draft);
        }

        public PrescriptionTestDataBuilder AsCompleted()
        {
            return WithStatus(PrescriptionStatus.Completed);
        }

        #endregion

        #region 时间相关构建方法

        // 注意：Prescription实体没有审计字段，已简化架构
        public PrescriptionTestDataBuilder WithCreateTime(DateTime createTime)
        {
            // 审计字段已移除，跳过此操作
            return this;
        }

        public PrescriptionTestDataBuilder WithUpdateTime(DateTime updateTime)
        {
            // 审计字段已移除，跳过此操作
            return this;
        }

        public PrescriptionTestDataBuilder CreatedToday()
        {
            return WithCreateTime(DateTime.Today.AddHours(_random.Next(8, 18)));
        }

        public PrescriptionTestDataBuilder CreatedDaysAgo(int days)
        {
            return WithCreateTime(DateTime.Now.AddDays(-days));
        }

        #endregion

        #region 预设场景构建方法

        /// <summary>
        /// 构建一个有效的处方
        /// </summary>
        public PrescriptionTestDataBuilder AsValidPrescription()
        {
            return WithId(Guid.NewGuid())
                .WithPatientId(Guid.NewGuid())
                .WithUserId(Guid.NewGuid())
                .WithRandomIndication()
                .WithMedicalCaseId(Guid.NewGuid())
                .WithRandomAdvice()
                .WithDosageCount(7)
                .AddRandomItems(5)
                .AsDraft();
        }

        /// <summary>
        /// 构建一个今天的处方
        /// </summary>
        public PrescriptionTestDataBuilder AsTodayPrescription()
        {
            return AsValidPrescription()
                .CreatedToday();
        }

        /// <summary>
        /// 构建一个已完成的处方
        /// </summary>
        public PrescriptionTestDataBuilder AsCompletedPrescription()
        {
            return AsValidPrescription()
                .AsCompleted()
                .WithDiscount(0.9m);
        }

        /// <summary>
        /// 构建一个已完成的处方（配药完毕）
        /// </summary>
        public PrescriptionTestDataBuilder AsDispensedPrescription()
        {
            return AsCompletedPrescription()
                .AsCompleted()
                .WithUpdateTime(DateTime.Now);
        }

        /// <summary>
        /// 构建一个经典方剂处方
        /// </summary>
        public PrescriptionTestDataBuilder AsClassicPrescription()
        {
            return WithId(Guid.NewGuid())
                .WithPatientId(Guid.NewGuid())
                .WithUserId(Guid.NewGuid())
                .WithIndication("风寒感冒，恶寒发热，无汗")
                .WithMedicalCaseId(Guid.NewGuid())
                .WithAdvice("温服，服后覆被取微汗")
                .WithDosageCount(3)
                .WithClassicFormula()
                .AsDraft();
        }

        /// <summary>
        /// 构建一个空处方（仅诊断）
        /// </summary>
        public PrescriptionTestDataBuilder AsEmptyPrescription()
        {
            return WithId(Guid.NewGuid())
                .WithPatientId(Guid.NewGuid())
                .WithUserId(Guid.NewGuid())
                .WithIndication("待补充")
                .WithMedicalCaseId(Guid.NewGuid())
                .WithDosageCount(0)
                .AsDraft();
        }

        /// <summary>
        /// 构建患者的历史处方
        /// </summary>
        public Prescription[] BuildPatientHistory(Guid patientId, int count)
        {
            var prescriptions = new Prescription[count];
            for (int i = 0; i < count; i++)
            {
                prescriptions[i] = AsValidPrescription()
                    .WithPatientId(patientId)
                    .CreatedDaysAgo(i * 7) // 每周一次
                    .AsCompleted()
                    .Build();
            }
            return prescriptions;
        }

        /// <summary>
        /// 构建医生今日的处方
        /// </summary>
        public Prescription[] BuildDoctorTodayPrescriptions(Guid doctorId, int count)
        {
            var prescriptions = new Prescription[count];
            var today = DateTime.Today;
            for (int i = 0; i < count; i++)
            {
                prescriptions[i] = AsValidPrescription()
                    .WithUserId(doctorId)
                    .WithCreateTime(today.AddHours(8 + i))
                    .Build();
            }
            return prescriptions;
        }

        #endregion

        /// <summary>
        /// 构建处方
        /// </summary>
        public override Prescription Build()
        {
            var prescription = base.Build();
            
            // 添加处方项
            if (_items.Any())
            {
                prescription.Items = _items;
            }
            
            // 计算总价（如果有处方项）
            if (prescription.Items != null && prescription.Items.Any())
            {
                // 价格计算在DTO层处理，这里不做计算
            }
            
            return prescription;
        }

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

            if (_entity.UserId == Guid.Empty)
            {
                _entity.UserId = Guid.NewGuid();
            }

            if (string.IsNullOrEmpty(_entity.Indication))
            {
                _entity.Indication = "待诊断";
            }

            if (_entity.MedicalCaseId == Guid.Empty)
            {
                _entity.MedicalCaseId = Guid.NewGuid();
            }

            // 审计字段已移除

            if (_entity.Status == 0)
            {
                _entity.Status = PrescriptionStatus.Draft;
            }

            if (_entity.DosageCount == 0)
            {
                _entity.DosageCount = 7; // 默认7剂
            }

            // 确保Items不为null
            if (_entity.Items == null)
            {
                _entity.Items = new List<PrescriptionItem>();
            }
        }
    }
}