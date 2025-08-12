using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.Common;
using LYBT.Domain.ValueObjects;
using LYBT.Domain.Exceptions;

namespace LYBT.Domain.Aggregates.HerbAggregate
{
    /// <summary>
    /// 中药材聚合根 - 药材管理核心领域模型
    /// 
    /// 职责：
    /// 1. 管理药材基本信息
    /// 2. 维护药材属性和功效
    /// 3. 控制价格和库存预警
    /// 4. 管理配伍禁忌规则
    /// </summary>
    public class Herb : AggregateRoot
    {
        #region 私有字段

        private string _code;
        private string _name;
        private string _pinyin;
        private string _englishName;
        private HerbCategory _category;
        private string _origin;
        private HerbNature _nature;
        private HerbFlavor _flavor;
        private readonly List<Meridian> _meridians;
        private readonly List<MedicinalEffect> _effects;
        private readonly List<ClinicalApplication> _applications;
        private DosageRange _dosageRange;
        private Money _unitPrice;
        private string _unit;
        private ProcessingMethod _defaultProcessing;
        private readonly List<ProcessingMethod> _availableProcessings;
        private readonly List<HerbIncompatibility> _incompatibilities;
        private readonly List<HerbCaution> _cautions;
        private readonly List<string> _contraindications;
        private HerbQualityGrade _qualityGrade;
        private string _storageRequirements;
        private int _shelfLifeDays;
        private bool _isPrescriptionOnly;
        private bool _isActive;
        private DateTime _lastPriceUpdate;
        private string _notes;
        
        // Repository兼容性属性
        private string _commonName;
        private string _latinName;
        private HerbBasicInfo _basicInfo;
        private HerbProperties _properties;
        private HerbEfficacy _efficacy;
        private HerbPriceInfo _priceInfo;

        #endregion

        #region 属性

        public string Code => _code;
        public string Name => _name;
        public string Pinyin => _pinyin;
        public string EnglishName => _englishName;
        public HerbCategory Category => _category;
        public string Origin => _origin;
        public HerbNature Nature => _nature;
        public HerbFlavor Flavor => _flavor;
        public IReadOnlyCollection<Meridian> Meridians => _meridians.AsReadOnly();
        public IReadOnlyCollection<MedicinalEffect> Effects => _effects.AsReadOnly();
        public IReadOnlyCollection<ClinicalApplication> Applications => _applications.AsReadOnly();
        public DosageRange DosageRange => _dosageRange;
        public Money UnitPrice => _unitPrice;
        public string Unit => _unit;
        public ProcessingMethod DefaultProcessing => _defaultProcessing;
        public IReadOnlyCollection<ProcessingMethod> AvailableProcessings => _availableProcessings.AsReadOnly();
        public IReadOnlyCollection<HerbIncompatibility> Incompatibilities => _incompatibilities.AsReadOnly();
        public IReadOnlyCollection<HerbCaution> Cautions => _cautions.AsReadOnly();
        public IReadOnlyCollection<string> Contraindications => _contraindications.AsReadOnly();
        public HerbQualityGrade QualityGrade => _qualityGrade;
        public string StorageRequirements => _storageRequirements;
        public int ShelfLifeDays => _shelfLifeDays;
        public bool IsPrescriptionOnly => _isPrescriptionOnly;
        public bool IsActive => _isActive;
        public DateTime LastPriceUpdate => _lastPriceUpdate;
        public string Notes => _notes;
        
        // Repository兼容性属性
        public string CommonName => _commonName;
        public string LatinName => _latinName;
        public HerbBasicInfo BasicInfo => _basicInfo;
        public HerbProperties Properties => _properties;
        public HerbEfficacy Efficacy => _efficacy;
        public HerbPriceInfo PriceInfo => _priceInfo;

        // 计算属性
        public bool HasIncompatibilities => _incompatibilities.Any();
        public bool HasCautions => _cautions.Any();
        public bool HasContraindications => _contraindications.Any();
        public bool RequiresSpecialStorage => !string.IsNullOrWhiteSpace(_storageRequirements);
        public string FullName => $"{_name}({_pinyin})";

        #endregion

        #region 构造函数

        protected Herb()
        {
            _meridians = new List<Meridian>();
            _effects = new List<MedicinalEffect>();
            _applications = new List<ClinicalApplication>();
            _availableProcessings = new List<ProcessingMethod>();
            _incompatibilities = new List<HerbIncompatibility>();
            _cautions = new List<HerbCaution>();
            _contraindications = new List<string>();
        }

        public Herb(
            string code,
            string name,
            string pinyin,
            HerbCategory category,
            HerbNature nature,
            HerbFlavor flavor,
            DosageRange dosageRange,
            Money unitPrice,
            string unit = "g") : this()
        {
            SetBasicInfo(code, name, pinyin, category);
            SetProperties(nature, flavor);
            SetDosageRange(dosageRange);
            SetPrice(unitPrice);
            
            _unit = unit;
            _isActive = true;
            _defaultProcessing = ProcessingMethod.Raw;
            _qualityGrade = HerbQualityGrade.Standard;
            _shelfLifeDays = 365;  // 默认保质期1年
            
            // 初始化Repository兼容性属性
            _commonName = _name;
            _latinName = _englishName ?? "";
            _basicInfo = new HerbBasicInfo(_pinyin?.ToUpper(), null, _category?.Name);
            _properties = new HerbProperties(_nature, _flavor, 
                string.Join("，", _meridians.Select(m => m.Name)), HerbToxicity.NonToxic);
            _efficacy = new HerbEfficacy(
                _effects.Any() ? string.Join("，", _effects.Select(e => e.Effect)) : "待完善",
                _applications.Any() ? string.Join("，", _applications.Select(a => a.Disease)) : "");
            _priceInfo = new HerbPriceInfo(_unitPrice?.Amount ?? 0, _unit);
        }

        #endregion

        #region 基本信息管理

        /// <summary>
        /// 更新基本信息
        /// </summary>
        public void UpdateBasicInfo(
            string name,
            string pinyin,
            string englishName,
            string origin)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new HerbDomainException("药材名称不能为空");

            if (string.IsNullOrWhiteSpace(pinyin))
                throw new HerbDomainException("拼音不能为空");

            _name = name;
            _pinyin = pinyin;
            _englishName = englishName;
            _origin = origin;
        }

        /// <summary>
        /// 更新药性
        /// </summary>
        public void UpdateProperties(HerbNature nature, HerbFlavor flavor)
        {
            _nature = nature ?? throw new HerbDomainException("药性不能为空");
            _flavor = flavor ?? throw new HerbDomainException("药味不能为空");
        }

        #endregion

        #region 归经管理

        /// <summary>
        /// 添加归经
        /// </summary>
        public void AddMeridian(Meridian meridian)
        {
            if (meridian == null)
                throw new HerbDomainException("归经不能为空");

            if (_meridians.Contains(meridian))
                return;

            _meridians.Add(meridian);
        }

        /// <summary>
        /// 移除归经
        /// </summary>
        public void RemoveMeridian(Meridian meridian)
        {
            _meridians.Remove(meridian);
        }

        /// <summary>
        /// 设置归经列表
        /// </summary>
        public void SetMeridians(IEnumerable<Meridian> meridians)
        {
            _meridians.Clear();
            if (meridians != null)
            {
                _meridians.AddRange(meridians.Distinct());
            }
        }

        #endregion

        #region 功效管理

        /// <summary>
        /// 添加功效
        /// </summary>
        public void AddEffect(string effect, EffectCategory category, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(effect))
                throw new HerbDomainException("功效描述不能为空");

            var medicinalEffect = new MedicinalEffect(
                Guid.NewGuid(),
                effect,
                category,
                priority);

            _effects.Add(medicinalEffect);
        }

        /// <summary>
        /// 添加临床应用
        /// </summary>
        public void AddClinicalApplication(
            string disease,
            string syndrome,
            string usage,
            string combination)
        {
            if (string.IsNullOrWhiteSpace(disease))
                throw new HerbDomainException("疾病名称不能为空");

            var application = new ClinicalApplication(
                Guid.NewGuid(),
                disease,
                syndrome,
                usage,
                combination);

            _applications.Add(application);
        }

        #endregion

        #region 价格管理

        /// <summary>
        /// 更新价格
        /// </summary>
        public void UpdatePrice(Money newPrice)
        {
            if (newPrice == null || newPrice.Amount <= 0)
                throw new HerbDomainException("价格必须大于0");

            var oldPrice = _unitPrice;
            _unitPrice = newPrice;
            _lastPriceUpdate = DateTime.Now;

            // 价格变化超过20%需要记录
            if (oldPrice != null)
            {
                var changeRate = Math.Abs((newPrice.Amount - oldPrice.Amount) / oldPrice.Amount);
                if (changeRate > 0.2m)
                {
                    // 可以触发价格大幅变动事件
                }
            }
        }

        /// <summary>
        /// 设置质量等级
        /// </summary>
        public void SetQualityGrade(HerbQualityGrade grade)
        {
            _qualityGrade = grade ?? throw new HerbDomainException("质量等级不能为空");
            
            // 根据质量等级调整价格系数
            var priceMultiplier = grade.Id switch
            {
                1 => 1.0m,    // 普通
                2 => 1.2m,    // 优质
                3 => 1.5m,    // 特级
                4 => 2.0m,    // 道地
                _ => 1.0m
            };

            if (priceMultiplier != 1.0m && _unitPrice != null)
            {
                var adjustedPrice = new Money(_unitPrice.Amount * priceMultiplier, _unitPrice.Currency);
                UpdatePrice(adjustedPrice);
            }
        }

        #endregion

        #region 炮制方法管理

        /// <summary>
        /// 添加可用炮制方法
        /// </summary>
        public void AddProcessingMethod(ProcessingMethod method)
        {
            if (method == null)
                throw new HerbDomainException("炮制方法不能为空");

            if (!_availableProcessings.Contains(method))
            {
                _availableProcessings.Add(method);
            }
        }

        /// <summary>
        /// 设置默认炮制方法
        /// </summary>
        public void SetDefaultProcessingMethod(ProcessingMethod method)
        {
            if (method == null)
                throw new HerbDomainException("炮制方法不能为空");

            if (!_availableProcessings.Contains(method))
            {
                _availableProcessings.Add(method);
            }

            _defaultProcessing = method;
        }

        #endregion

        #region 配伍禁忌管理

        /// <summary>
        /// 添加配伍禁忌（十八反）
        /// </summary>
        public void AddIncompatibility(Guid incompatibleHerbId, string herbName, string reason)
        {
            if (incompatibleHerbId == Id)
                throw new HerbDomainException("不能添加自身为配伍禁忌");

            if (_incompatibilities.Any(i => i.IncompatibleHerbId == incompatibleHerbId))
                return;

            var incompatibility = new HerbIncompatibility(
                Guid.NewGuid(),
                incompatibleHerbId,
                herbName,
                IncompatibilityType.Eighteen,
                reason);

            _incompatibilities.Add(incompatibility);
        }

        /// <summary>
        /// 添加配伍慎用（十九畏）
        /// </summary>
        public void AddCaution(Guid cautionHerbId, string herbName, string reason)
        {
            if (cautionHerbId == Id)
                throw new HerbDomainException("不能添加自身为配伍慎用");

            if (_cautions.Any(c => c.CautionHerbId == cautionHerbId))
                return;

            var caution = new HerbCaution(
                Guid.NewGuid(),
                cautionHerbId,
                herbName,
                CautionType.Nineteen,
                reason);

            _cautions.Add(caution);
        }

        /// <summary>
        /// 添加禁忌症
        /// </summary>
        public void AddContraindication(string contraindication)
        {
            if (string.IsNullOrWhiteSpace(contraindication))
                throw new HerbDomainException("禁忌症描述不能为空");

            if (!_contraindications.Contains(contraindication))
            {
                _contraindications.Add(contraindication);
            }
        }

        #endregion

        #region 存储管理

        /// <summary>
        /// 设置存储要求
        /// </summary>
        public void SetStorageRequirements(string requirements, int shelfLifeDays)
        {
            if (shelfLifeDays <= 0)
                throw new HerbDomainException("保质期必须大于0天");

            _storageRequirements = requirements;
            _shelfLifeDays = shelfLifeDays;
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 激活药材
        /// </summary>
        public void Activate()
        {
            if (_isActive)
                return;

            _isActive = true;
        }

        /// <summary>
        /// 停用药材
        /// </summary>
        public void Deactivate(string reason)
        {
            if (!_isActive)
                return;

            if (string.IsNullOrWhiteSpace(reason))
                throw new HerbDomainException("停用原因不能为空");

            _isActive = false;
            AddNotes($"停用原因：{reason}");
        }

        /// <summary>
        /// 标记为处方药
        /// </summary>
        public void MarkAsPrescriptionOnly()
        {
            _isPrescriptionOnly = true;
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 检查与其他药材的配伍
        /// </summary>
        public bool IsCompatibleWith(Guid herbId)
        {
            return !_incompatibilities.Any(i => i.IncompatibleHerbId == herbId);
        }

        /// <summary>
        /// 检查是否需要慎用
        /// </summary>
        public bool RequiresCautionWith(Guid herbId)
        {
            return _cautions.Any(c => c.CautionHerbId == herbId);
        }

        /// <summary>
        /// 验证剂量是否合理
        /// </summary>
        public bool IsValidDosage(decimal dosage)
        {
            if (_dosageRange == null)
                return true;

            return dosage >= _dosageRange.MinDosage && dosage <= _dosageRange.MaxDosage;
        }

        /// <summary>
        /// 添加备注
        /// </summary>
        public void AddNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return;

            _notes = string.IsNullOrWhiteSpace(_notes)
                ? notes
                : $"{_notes}\n{notes}";
        }

        #endregion

        #region 私有方法

        private void SetBasicInfo(string code, string name, string pinyin, HerbCategory category)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new HerbDomainException("药材编码不能为空");

            if (string.IsNullOrWhiteSpace(name))
                throw new HerbDomainException("药材名称不能为空");

            if (string.IsNullOrWhiteSpace(pinyin))
                throw new HerbDomainException("拼音不能为空");

            _code = code;
            _name = name;
            _pinyin = pinyin;
            _category = category ?? throw new HerbDomainException("药材类别不能为空");
        }

        private void SetProperties(HerbNature nature, HerbFlavor flavor)
        {
            _nature = nature ?? throw new HerbDomainException("药性不能为空");
            _flavor = flavor ?? throw new HerbDomainException("药味不能为空");
        }

        private void SetDosageRange(DosageRange dosageRange)
        {
            _dosageRange = dosageRange ?? throw new HerbDomainException("剂量范围不能为空");
        }

        private void SetPrice(Money price)
        {
            if (price == null || price.Amount <= 0)
                throw new HerbDomainException("价格必须大于0");

            _unitPrice = price;
            _lastPriceUpdate = DateTime.Now;
        }

        #endregion
    }

    #region 实体

    /// <summary>
    /// 药效实体
    /// </summary>
    public class MedicinalEffect : Entity
    {
        public string Effect { get; private set; }
        public EffectCategory Category { get; private set; }
        public int Priority { get; private set; }

        protected MedicinalEffect() { }

        public MedicinalEffect(Guid id, string effect, EffectCategory category, int priority = 0)
        {
            Id = id;
            Effect = effect;
            Category = category;
            Priority = priority;
        }
    }

    /// <summary>
    /// 临床应用实体
    /// </summary>
    public class ClinicalApplication : Entity
    {
        public string Disease { get; private set; }
        public string Syndrome { get; private set; }
        public string Usage { get; private set; }
        public string Combination { get; private set; }

        protected ClinicalApplication() { }

        public ClinicalApplication(
            Guid id,
            string disease,
            string syndrome,
            string usage,
            string combination)
        {
            Id = id;
            Disease = disease;
            Syndrome = syndrome;
            Usage = usage;
            Combination = combination;
        }
    }

    /// <summary>
    /// 配伍禁忌实体
    /// </summary>
    public class HerbIncompatibility : Entity
    {
        public Guid IncompatibleHerbId { get; private set; }
        public string HerbName { get; private set; }
        public IncompatibilityType Type { get; private set; }
        public string Reason { get; private set; }

        protected HerbIncompatibility() { }

        public HerbIncompatibility(
            Guid id,
            Guid incompatibleHerbId,
            string herbName,
            IncompatibilityType type,
            string reason)
        {
            Id = id;
            IncompatibleHerbId = incompatibleHerbId;
            HerbName = herbName;
            Type = type;
            Reason = reason;
        }
    }

    /// <summary>
    /// 配伍慎用实体
    /// </summary>
    public class HerbCaution : Entity
    {
        public Guid CautionHerbId { get; private set; }
        public string HerbName { get; private set; }
        public CautionType Type { get; private set; }
        public string Reason { get; private set; }

        protected HerbCaution() { }

        public HerbCaution(
            Guid id,
            Guid cautionHerbId,
            string herbName,
            CautionType type,
            string reason)
        {
            Id = id;
            CautionHerbId = cautionHerbId;
            HerbName = herbName;
            Type = type;
            Reason = reason;
        }
    }

    #endregion

    #region Repository兼容性值对象

    /// <summary>
    /// 药材基础信息值对象 - Repository兼容性
    /// </summary>
    public class HerbBasicInfo : ValueObject
    {
        public string PinYinCode { get; }
        public string WuBiCode { get; }
        public string CategoryName { get; }

        public HerbBasicInfo(string pinYinCode, string wuBiCode, string categoryName)
        {
            PinYinCode = pinYinCode ?? "";
            WuBiCode = wuBiCode ?? "";
            CategoryName = categoryName ?? "";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return PinYinCode;
            yield return WuBiCode;
            yield return CategoryName;
        }
    }

    /// <summary>
    /// 药材属性值对象 - Repository兼容性
    /// </summary>
    public class HerbProperties : ValueObject
    {
        public HerbNature Nature { get; }
        public HerbFlavor Flavor { get; }
        public string Meridians { get; }
        public HerbToxicity Toxicity { get; }

        public HerbProperties(HerbNature nature, HerbFlavor flavor, string meridians, HerbToxicity toxicity)
        {
            Nature = nature;
            Flavor = flavor;
            Meridians = meridians ?? "";
            Toxicity = toxicity;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Nature;
            yield return Flavor;
            yield return Meridians;
            yield return Toxicity;
        }
    }

    /// <summary>
    /// 药材功效值对象 - Repository兼容性
    /// </summary>
    public class HerbEfficacy : ValueObject
    {
        public string MainEffects { get; }
        public string Indications { get; }

        public HerbEfficacy(string mainEffects, string indications)
        {
            MainEffects = mainEffects ?? "";
            Indications = indications ?? "";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MainEffects;
            yield return Indications;
        }
    }

    /// <summary>
    /// 药材价格信息值对象 - Repository兼容性
    /// </summary>
    public class HerbPriceInfo : ValueObject
    {
        public decimal UnitPrice { get; }
        public string Unit { get; }

        public HerbPriceInfo(decimal unitPrice, string unit)
        {
            UnitPrice = unitPrice;
            Unit = unit ?? "g";
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return UnitPrice;
            yield return Unit;
        }
    }

    #endregion
}