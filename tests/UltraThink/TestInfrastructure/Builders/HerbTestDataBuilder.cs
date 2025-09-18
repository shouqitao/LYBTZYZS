using System;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Builders
{
    /// <summary>
    /// 中药材测试数据构建器
    /// UltraThink设计：专注于Herb实体的测试数据生成
    /// </summary>
    public class HerbTestDataBuilder : TestDataBuilder<Herb, HerbTestDataBuilder>
    {
        private static readonly string[] HerbNames = 
        {
            "麻黄", "桂枝", "白芍", "甘草", "生姜", "大枣", "杏仁",
            "石膏", "知母", "粳米", "黄芩", "黄连", "黄柏", "栀子",
            "金银花", "连翘", "薄荷", "荆芥", "防风", "羌活", "独活",
            "当归", "川芎", "白芷", "细辛", "人参", "党参", "黄芪",
            "白术", "茯苓", "山药", "薏苡仁", "陈皮", "半夏", "枳实"
        };

        private static readonly string[] HerbCategories = 
        {
            "解表药", "清热药", "泻下药", "祛风湿药", "化湿药",
            "利水渗湿药", "温里药", "理气药", "消食药", "驱虫药",
            "止血药", "活血化瘀药", "化痰止咳平喘药", "安神药",
            "平肝息风药", "开窍药", "补虚药", "收涩药", "涌吐药"
        };

        private static readonly string[] HerbUnits = 
        {
            "g", "克", "kg", "千克", "两", "钱", "分", "枚", "个", "片", "粒", "包"
        };

        private static readonly string[] StorageConditions = 
        {
            "阴凉干燥处", "冷藏保存", "密封保存", "避光保存", 
            "常温保存", "低温保存", "通风干燥处"
        };

        public HerbTestDataBuilder()
        {
            // 设置默认的创建和更新信息
            WithCreatedBy("TestUser")
                .WithCreatedAt(DateTime.UtcNow)
                .WithUpdatedBy("TestUser")
                .WithUpdatedAt(DateTime.UtcNow);
        }

        #region 基本属性构建方法

        public HerbTestDataBuilder WithId(Guid id)
        {
            _buildActions.Add(h => h.Id = id);
            return this;
        }

        public HerbTestDataBuilder WithName(string name)
        {
            _buildActions.Add(h => h.Name = name);
            return this;
        }

        public HerbTestDataBuilder WithRandomName()
        {
            return WithName(HerbNames[_random.Next(HerbNames.Length)]);
        }

        public HerbTestDataBuilder WithPinyin(string pinyin)
        {
            _buildActions.Add(h => h.PinYinCode = pinyin);
            return this;
        }

        public HerbTestDataBuilder WithOrigin(string origin)
        {
            _buildActions.Add(h => h.Origin = origin);
            return this;
        }

        public HerbTestDataBuilder WithRandomOrigin()
        {
            string[] origins = {"安徽", "四川", "河南", "山东", "云南", "广西", "甘肃", "东北"};
            return WithOrigin(origins[_random.Next(origins.Length)]);
        }

        public HerbTestDataBuilder WithPrice(decimal price)
        {
            _buildActions.Add(h => h.Price = price);
            return this;
        }

        public HerbTestDataBuilder WithRandomPrice(decimal min = 10, decimal max = 500)
        {
            return WithPrice(GenerateRandomPrice(min, max));
        }

        public HerbTestDataBuilder WithUnit(string unit)
        {
            _buildActions.Add(h => h.Unit = unit);
            return this;
        }

        public HerbTestDataBuilder WithRandomUnit()
        {
            return WithUnit(HerbUnits[_random.Next(HerbUnits.Length)]);
        }

        public HerbTestDataBuilder WithSpec(string spec)
        {
            _buildActions.Add(h => h.Spec = spec);
            return this;
        }

        public HerbTestDataBuilder WithRandomSpec()
        {
            string[] specs = {"特级", "一等", "二等", "三等", "统货", "选货"};
            return WithSpec(specs[_random.Next(specs.Length)]);
        }

        #endregion

        #region 功效和用法构建方法

        public HerbTestDataBuilder WithEffect(string effect)
        {
            _buildActions.Add(h => h.Effect = effect);
            return this;
        }

        public HerbTestDataBuilder WithUsage(string usage)
        {
            _buildActions.Add(h => h.Usage = usage);
            return this;
        }

        public HerbTestDataBuilder WithRemark(string remark)
        {
            _buildActions.Add(h => h.Remark = remark);
            return this;
        }

        public HerbTestDataBuilder WithCostPrice(decimal costPrice)
        {
            _buildActions.Add(h => h.CostPrice = costPrice);
            return this;
        }

        public HerbTestDataBuilder WithStatus(CommonStatus status)
        {
            _buildActions.Add(h => h.Status = status);
            return this;
        }

        public HerbTestDataBuilder WithRandomStatus()
        {
            var statuses = new[] { CommonStatus.Enabled, CommonStatus.Disabled };
            return WithStatus(statuses[_random.Next(statuses.Length)]);
        }

        #endregion

        #region 状态和标识构建方法

        public HerbTestDataBuilder WithEnabled(bool enabled)
        {
            _buildActions.Add(h => h.Status = enabled ? CommonStatus.Enabled : CommonStatus.Disabled);
            return this;
        }

        public HerbTestDataBuilder AsEnabled()
        {
            return WithEnabled(true);
        }

        public HerbTestDataBuilder AsDisabled()
        {
            return WithEnabled(false);
        }

        // 注意：Herb实体没有Code属性，使用PinYinCode代替
        public HerbTestDataBuilder WithCode(string code)
        {
            _buildActions.Add(h => h.PinYinCode = code);
            return this;
        }

        public HerbTestDataBuilder WithRandomCode()
        {
            var name = HerbNames[_random.Next(HerbNames.Length)];
            return WithCode(GetPinYinCode(name));
        }

        // 注意：Herb实体没有Supplier属性，用Remark代替
        public HerbTestDataBuilder WithSupplier(string supplier)
        {
            _buildActions.Add(h => h.Remark = $"供应商：{supplier}");
            return this;
        }

        // 注意：Herb实体没有MinStock属性，已移除库存管理功能
        public HerbTestDataBuilder WithMinStock(int minStock)
        {
            // 库存功能已移除，跳过此操作
            return this;
        }

        // 注意：Herb实体没有MaxStock属性，已移除库存管理功能
        public HerbTestDataBuilder WithMaxStock(int maxStock)
        {
            // 库存功能已移除，跳过此操作
            return this;
        }

        #endregion

        #region 审计字段构建方法

        // 注意：Herb实体没有审计字段，已简化架构
        public HerbTestDataBuilder WithCreatedBy(string createdBy)
        {
            // 审计字段已移除，跳过此操作
            return this;
        }

        public HerbTestDataBuilder WithCreatedAt(DateTime createdAt)
        {
            // 审计字段已移除，跳过此操作
            return this;
        }

        public HerbTestDataBuilder WithUpdatedBy(string updatedBy)
        {
            // 审计字段已移除，跳过此操作
            return this;
        }

        public HerbTestDataBuilder WithUpdatedAt(DateTime updatedAt)
        {
            // 审计字段已移除，跳过此操作
            return this;
        }

        #endregion

        #region 预设场景构建方法

        /// <summary>
        /// 构建一个完整有效的中药材
        /// </summary>
        public HerbTestDataBuilder AsValidHerb()
        {
            return WithId(Guid.NewGuid())
                .WithRandomName()
                .WithRandomOrigin()
                .WithRandomPrice()
                .WithRandomUnit()
                .WithRandomSpec()
                .WithRandomCode()
                .WithEffect("清热解毒，疏风散热")
                .WithUsage("每次3-9克，水煎服")
                .WithRandomStatus()
                .AsEnabled();
        }

        /// <summary>
        /// 构建一个低价的中药材
        /// </summary>
        public HerbTestDataBuilder AsLowPriceHerb()
        {
            return AsValidHerb()
                .WithPrice(5.0m)
                .WithCostPrice(3.0m);
        }

        /// <summary>
        /// 构建一个高价值中药材
        /// </summary>
        public HerbTestDataBuilder AsExpensiveHerb()
        {
            return WithId(Guid.NewGuid())
                .WithName("野生人参")
                .WithOrigin("东北")
                .WithPrice(8888.88m)
                .WithUnit("g")
                .WithSpec("特级")
                .WithCode("YSRS")
                .WithEffect("大补元气，复脉固脱，补脾益肺，生津养血，安神益智")
                .WithUsage("每次1-3克，研粉冲服或炖服")
                .WithRemark("密封冷藏保存")
                .AsEnabled();
        }

        /// <summary>
        /// 构建一个停用的中药材
        /// </summary>
        public HerbTestDataBuilder AsDiscontinuedHerb()
        {
            return AsValidHerb()
                .AsDisabled()
                .WithRemark("已停用");
        }

        #endregion

        #region 私有辅助方法
        
        /// <summary>
        /// 获取拼音码（简化版）
        /// </summary>
        private string GetPinYinCode(string chinese)
        {
            if (string.IsNullOrEmpty(chinese)) return string.Empty;
            // 简化的拼音转换，实际项目可能使用专门的拼音库
            return chinese.Substring(0, Math.Min(chinese.Length, 2)).ToUpper();
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

            if (string.IsNullOrEmpty(_entity.Name))
            {
                _entity.Name = HerbNames[_random.Next(HerbNames.Length)];
            }

            if (string.IsNullOrEmpty(_entity.Unit))
            {
                _entity.Unit = "g";
            }

            if (_entity.Price <= 0)
            {
                _entity.Price = GenerateRandomPrice(10, 200);
            }

            if (string.IsNullOrEmpty(_entity.PinYinCode))
            {
                _entity.PinYinCode = GetPinYinCode(_entity.Name);
            }

            if (_entity.Status == default)
            {
                _entity.Status = CommonStatus.Enabled;
            }
        }
    }
}