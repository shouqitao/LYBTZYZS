using System;
using LYBT.Models.Herbs;
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
            _buildActions.Add(h => h.Pinyin = pinyin);
            return this;
        }

        public HerbTestDataBuilder WithCategory(string category)
        {
            _buildActions.Add(h => h.Category = category);
            return this;
        }

        public HerbTestDataBuilder WithRandomCategory()
        {
            return WithCategory(HerbCategories[_random.Next(HerbCategories.Length)]);
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

        public HerbTestDataBuilder WithStock(int stock)
        {
            _buildActions.Add(h => h.Stock = stock);
            return this;
        }

        public HerbTestDataBuilder WithRandomStock(int min = 0, int max = 10000)
        {
            return WithStock(_random.Next(min, max));
        }

        #endregion

        #region 功效和用法构建方法

        public HerbTestDataBuilder WithEfficacy(string efficacy)
        {
            _buildActions.Add(h => h.Efficacy = efficacy);
            return this;
        }

        public HerbTestDataBuilder WithUsage(string usage)
        {
            _buildActions.Add(h => h.Usage = usage);
            return this;
        }

        public HerbTestDataBuilder WithContraindication(string contraindication)
        {
            _buildActions.Add(h => h.Contraindication = contraindication);
            return this;
        }

        public HerbTestDataBuilder WithProcessingMethod(string processingMethod)
        {
            _buildActions.Add(h => h.ProcessingMethod = processingMethod);
            return this;
        }

        public HerbTestDataBuilder WithStorageMethod(string storageMethod)
        {
            _buildActions.Add(h => h.StorageMethod = storageMethod);
            return this;
        }

        public HerbTestDataBuilder WithRandomStorageMethod()
        {
            return WithStorageMethod(StorageConditions[_random.Next(StorageConditions.Length)]);
        }

        #endregion

        #region 状态和标识构建方法

        public HerbTestDataBuilder WithIsActive(bool isActive)
        {
            _buildActions.Add(h => h.IsActive = isActive);
            return this;
        }

        public HerbTestDataBuilder AsActive()
        {
            return WithIsActive(true);
        }

        public HerbTestDataBuilder AsInactive()
        {
            return WithIsActive(false);
        }

        public HerbTestDataBuilder WithCode(string code)
        {
            _buildActions.Add(h => h.Code = code);
            return this;
        }

        public HerbTestDataBuilder WithRandomCode()
        {
            return WithCode($"HC{_random.Next(10000, 99999)}");
        }

        public HerbTestDataBuilder WithSupplier(string supplier)
        {
            _buildActions.Add(h => h.Supplier = supplier);
            return this;
        }

        public HerbTestDataBuilder WithMinStock(int minStock)
        {
            _buildActions.Add(h => h.MinStock = minStock);
            return this;
        }

        public HerbTestDataBuilder WithMaxStock(int maxStock)
        {
            _buildActions.Add(h => h.MaxStock = maxStock);
            return this;
        }

        #endregion

        #region 审计字段构建方法

        public HerbTestDataBuilder WithCreatedBy(string createdBy)
        {
            _buildActions.Add(h => h.CreatedBy = createdBy);
            return this;
        }

        public HerbTestDataBuilder WithCreatedAt(DateTime createdAt)
        {
            _buildActions.Add(h => h.CreatedAt = createdAt);
            return this;
        }

        public HerbTestDataBuilder WithUpdatedBy(string updatedBy)
        {
            _buildActions.Add(h => h.UpdatedBy = updatedBy);
            return this;
        }

        public HerbTestDataBuilder WithUpdatedAt(DateTime updatedAt)
        {
            _buildActions.Add(h => h.UpdatedAt = updatedAt);
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
                .WithRandomCategory()
                .WithRandomPrice()
                .WithRandomUnit()
                .WithRandomStock()
                .WithRandomCode()
                .WithEfficacy("清热解毒，疏风散热")
                .WithUsage("每次3-9克，水煎服")
                .WithRandomStorageMethod()
                .AsActive();
        }

        /// <summary>
        /// 构建一个库存不足的中药材
        /// </summary>
        public HerbTestDataBuilder AsLowStockHerb()
        {
            return AsValidHerb()
                .WithStock(5)
                .WithMinStock(10)
                .WithMaxStock(100);
        }

        /// <summary>
        /// 构建一个高价值中药材
        /// </summary>
        public HerbTestDataBuilder AsExpensiveHerb()
        {
            return WithId(Guid.NewGuid())
                .WithName("野生人参")
                .WithCategory("补虚药")
                .WithPrice(8888.88m)
                .WithUnit("g")
                .WithStock(10)
                .WithCode("HC99999")
                .WithEfficacy("大补元气，复脉固脱，补脾益肺，生津养血，安神益智")
                .WithUsage("每次1-3克，研粉冲服或炖服")
                .WithStorageMethod("密封冷藏保存")
                .AsActive();
        }

        /// <summary>
        /// 构建一个停用的中药材
        /// </summary>
        public HerbTestDataBuilder AsDiscontinuedHerb()
        {
            return AsValidHerb()
                .AsInactive()
                .WithStock(0);
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

            if (string.IsNullOrEmpty(_entity.Code))
            {
                _entity.Code = $"HC{_random.Next(10000, 99999)}";
            }

            if (_entity.CreatedAt == default)
            {
                _entity.CreatedAt = DateTime.UtcNow;
            }

            if (_entity.UpdatedAt == default)
            {
                _entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}