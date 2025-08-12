using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.Common;
using LYBT.Domain.ValueObjects;
using LYBT.Domain.Exceptions;
using LYBT.Domain.Aggregates.FormulaAggregate.ValueObjects;

namespace LYBT.Domain.Aggregates.FormulaAggregate
{
    /// <summary>
    /// 验方聚合根 - 经典验方模板管理核心领域模型
    /// 
    /// 职责：
    /// 1. 管理经典验方配方
    /// 2. 维护方剂组成和配伍
    /// 3. 记录临床应用经验
    /// 4. 支持验方修改和版本管理
    /// </summary>
    public class Formula : AggregateRoot
    {
        #region 私有字段

        private string _code;
        private string _name;
        private string _pinyin;
        private string _source;
        private FormulaCategory _category;
        private readonly List<FormulaHerb> _herbs;
        private TCMSyndrome _targetSyndrome;
        private TreatmentPrinciple _treatmentPrinciple;
        private string _indication;
        private string _contraindication;
        private string _preparation;
        private string _usage;
        private string _modification;
        private readonly List<ClinicalCase> _clinicalCases;
        private readonly List<FormulaVariation> _variations;
        private FormulaStatus _status;
        private FormulaType _type;
        private Guid? _creatorId;
        private string _creatorName;
        private bool _isClassic;
        private bool _isPublic;
        private int _usageCount;
        private decimal _successRate;
        private DateTime _lastUsedDate;
        private string _notes;
        private int _version;
        private bool _isActive;
        private FormulaInfo _formulaInfo;
        private FormulaEfficacy _efficacy;
        private FormulaApproval _approval;

        #endregion

        #region 属性

        public string Code => _code;
        public string Name => _name;
        public string Pinyin => _pinyin;
        public string Source => _source;
        public FormulaCategory Category => _category;
        public IReadOnlyCollection<FormulaHerb> Herbs => _herbs.AsReadOnly();
        public TCMSyndrome TargetSyndrome => _targetSyndrome;
        public TreatmentPrinciple TreatmentPrinciple => _treatmentPrinciple;
        public string Indication => _indication;
        public string Contraindication => _contraindication;
        public string Preparation => _preparation;
        public string Usage => _usage;
        public string Modification => _modification;
        public IReadOnlyCollection<ClinicalCase> ClinicalCases => _clinicalCases.AsReadOnly();
        public IReadOnlyCollection<FormulaVariation> Variations => _variations.AsReadOnly();
        public FormulaStatus Status => _status;
        public FormulaType Type => _type;
        public Guid? CreatorId => _creatorId;
        public string CreatorName => _creatorName;
        public bool IsClassic => _isClassic;
        public bool IsPublic => _isPublic;
        public int UsageCount => _usageCount;
        public decimal SuccessRate => _successRate;
        public DateTime LastUsedDate => _lastUsedDate;
        public string Notes => _notes;
        public int Version => _version;
        public bool IsActive => _isActive;
        public FormulaInfo FormulaInfo => _formulaInfo;
        public FormulaEfficacy Efficacy => _efficacy;
        public FormulaApproval Approval => _approval;

        // 计算属性
        public int HerbCount => _herbs.Count;
        public decimal TotalDosage => _herbs.Sum(h => h.Dosage);
        public bool HasMonarchHerb => _herbs.Any(h => h.Role == HerbRole.Monarch);
        public bool IsComplete => HasMonarchHerb && !string.IsNullOrWhiteSpace(_indication);
        public bool CanBeUsed => _status == FormulaStatus.Approved && IsComplete;
        public string FullName => $"{_name}（{_source}）";

        #endregion

        #region 构造函数

        protected Formula()
        {
            _herbs = new List<FormulaHerb>();
            _clinicalCases = new List<ClinicalCase>();
            _variations = new List<FormulaVariation>();
        }

        public Formula(
            string code,
            string name,
            string pinyin,
            string source,
            FormulaCategory category,
            TCMSyndrome targetSyndrome,
            TreatmentPrinciple treatmentPrinciple,
            FormulaType type,
            bool isClassic = false) : this()
        {
            SetBasicInfo(code, name, pinyin, source);
            SetCategory(category);
            SetSyndromeAndPrinciple(targetSyndrome, treatmentPrinciple);
            
            _type = type ?? throw new FormulaDomainException("验方类型不能为空");
            _isClassic = isClassic;
            _isPublic = isClassic;  // 经典方默认公开
            _status = FormulaStatus.Draft;
            _version = 1;
            _usageCount = 0;
            _successRate = 0;
            _isActive = true; // 新创建的验方默认为活跃状态
            
            // 初始化新的值对象属性
            _formulaInfo = FormulaInfo.Create(name, null, pinyin?.ToUpper(), null, category?.Name);
            _efficacy = FormulaEfficacy.Create("待完善");
            _approval = FormulaApproval.CreatePending();
        }

        #endregion

        #region 方剂组成管理

        /// <summary>
        /// 添加药材
        /// </summary>
        public void AddHerb(
            Guid herbId,
            string herbName,
            decimal dosage,
            HerbRole role,
            ProcessingMethod processingMethod = null,
            string specialInstructions = null)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            if (dosage <= 0)
                throw new FormulaDomainException("药材剂量必须大于0");

            // 检查是否已存在
            if (_herbs.Any(h => h.HerbId == herbId))
                throw new FormulaDomainException($"{herbName}已存在于验方中");

            // 验证君药唯一性
            if (role == HerbRole.Monarch && _herbs.Any(h => h.Role == HerbRole.Monarch))
                throw new FormulaDomainException("验方只能有一味君药");

            var formulaHerb = new FormulaHerb(
                Guid.NewGuid(),
                herbId,
                herbName,
                dosage,
                role,
                processingMethod ?? ProcessingMethod.Raw,
                specialInstructions,
                _herbs.Count + 1);

            _herbs.Add(formulaHerb);
            IncrementVersion();
        }

        /// <summary>
        /// 移除药材
        /// </summary>
        public void RemoveHerb(Guid herbId)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            var herb = _herbs.FirstOrDefault(h => h.HerbId == herbId);
            if (herb == null)
                throw new FormulaDomainException("药材不存在于验方中");

            _herbs.Remove(herb);
            
            // 重新排序
            for (int i = 0; i < _herbs.Count; i++)
            {
                _herbs[i].UpdateSequence(i + 1);
            }

            IncrementVersion();
        }

        /// <summary>
        /// 更新药材剂量
        /// </summary>
        public void UpdateHerbDosage(Guid herbId, decimal newDosage)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            if (newDosage <= 0)
                throw new FormulaDomainException("药材剂量必须大于0");

            var herb = _herbs.FirstOrDefault(h => h.HerbId == herbId);
            if (herb == null)
                throw new FormulaDomainException("药材不存在于验方中");

            herb.UpdateDosage(newDosage);
            IncrementVersion();
        }

        /// <summary>
        /// 调整药材角色
        /// </summary>
        public void UpdateHerbRole(Guid herbId, HerbRole newRole)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            var herb = _herbs.FirstOrDefault(h => h.HerbId == herbId);
            if (herb == null)
                throw new FormulaDomainException("药材不存在于验方中");

            // 验证君药唯一性
            if (newRole == HerbRole.Monarch && _herbs.Any(h => h.Role == HerbRole.Monarch && h.HerbId != herbId))
                throw new FormulaDomainException("验方只能有一味君药");

            herb.UpdateRole(newRole);
            IncrementVersion();
        }

        #endregion

        #region 适应症管理

        /// <summary>
        /// 设置适应症
        /// </summary>
        public void SetIndication(string indication)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            if (string.IsNullOrWhiteSpace(indication))
                throw new FormulaDomainException("适应症不能为空");

            _indication = indication;
            IncrementVersion();
        }

        /// <summary>
        /// 设置禁忌症
        /// </summary>
        public void SetContraindication(string contraindication)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            _contraindication = contraindication;
            IncrementVersion();
        }

        /// <summary>
        /// 设置制法
        /// </summary>
        public void SetPreparation(string preparation)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            if (string.IsNullOrWhiteSpace(preparation))
                preparation = "水煎服";

            _preparation = preparation;
            IncrementVersion();
        }

        /// <summary>
        /// 设置用法用量
        /// </summary>
        public void SetUsage(string usage)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            if (string.IsNullOrWhiteSpace(usage))
                usage = "每日一剂，分两次温服";

            _usage = usage;
            IncrementVersion();
        }

        /// <summary>
        /// 设置加减法
        /// </summary>
        public void SetModification(string modification)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            _modification = modification;
            IncrementVersion();
        }

        #endregion

        #region 临床案例管理

        /// <summary>
        /// 添加临床案例
        /// </summary>
        public void AddClinicalCase(
            string patientInfo,
            string diagnosis,
            string treatment,
            string outcome,
            DateTime treatmentDate,
            string doctorName)
        {
            if (string.IsNullOrWhiteSpace(patientInfo))
                throw new FormulaDomainException("患者信息不能为空");

            if (string.IsNullOrWhiteSpace(outcome))
                throw new FormulaDomainException("治疗效果不能为空");

            var clinicalCase = new ClinicalCase(
                Guid.NewGuid(),
                patientInfo,
                diagnosis,
                treatment,
                outcome,
                treatmentDate,
                doctorName);

            _clinicalCases.Add(clinicalCase);
            
            // 更新使用统计
            _usageCount++;
            _lastUsedDate = treatmentDate;
            
            // 更新成功率（简化计算）
            if (outcome.Contains("治愈") || outcome.Contains("显效"))
            {
                _successRate = (_successRate * (_usageCount - 1) + 100) / _usageCount;
            }
            else if (outcome.Contains("有效"))
            {
                _successRate = (_successRate * (_usageCount - 1) + 70) / _usageCount;
            }
            else
            {
                _successRate = (_successRate * (_usageCount - 1) + 30) / _usageCount;
            }
        }

        #endregion

        #region 变方管理

        /// <summary>
        /// 添加变方
        /// </summary>
        public void AddVariation(
            string name,
            string condition,
            string modification,
            string indication)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new FormulaDomainException("变方名称不能为空");

            if (string.IsNullOrWhiteSpace(modification))
                throw new FormulaDomainException("变方修改内容不能为空");

            var variation = new FormulaVariation(
                Guid.NewGuid(),
                name,
                condition,
                modification,
                indication);

            _variations.Add(variation);
            IncrementVersion();
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 提交审核
        /// </summary>
        public void SubmitForApproval()
        {
            if (_status != FormulaStatus.Draft)
                throw new FormulaDomainException($"只有草稿状态的验方才能提交审核，当前状态：{_status}");

            ValidateCompleteness();
            _status = FormulaStatus.UnderReview;
        }

        /// <summary>
        /// 批准验方
        /// </summary>
        public void Approve(string approverName, string comments = null)
        {
            if (_status != FormulaStatus.UnderReview)
                throw new FormulaDomainException($"只有审核中的验方才能批准，当前状态：{_status}");

            if (string.IsNullOrWhiteSpace(approverName))
                throw new FormulaDomainException("审批人不能为空");

            _status = FormulaStatus.Approved;
            _approval = FormulaApproval.CreateApproved(approverName, DateTime.UtcNow, comments);
            AddNotes($"由{approverName}于{DateTime.Now:yyyy-MM-dd HH:mm}批准");
        }

        /// <summary>
        /// 拒绝验方
        /// </summary>
        public void Reject(string reason, string reviewerName)
        {
            if (_status != FormulaStatus.UnderReview)
                throw new FormulaDomainException($"只有审核中的验方才能拒绝，当前状态：{_status}");

            if (string.IsNullOrWhiteSpace(reason))
                throw new FormulaDomainException("拒绝原因不能为空");

            _status = FormulaStatus.Rejected;
            _approval = FormulaApproval.CreateRejected(reviewerName, reason, DateTime.UtcNow);
            AddNotes($"由{reviewerName}于{DateTime.Now:yyyy-MM-dd HH:mm}拒绝：{reason}");
        }

        /// <summary>
        /// 归档验方
        /// </summary>
        public void Archive()
        {
            if (_status == FormulaStatus.Draft || _status == FormulaStatus.UnderReview)
                throw new FormulaDomainException("草稿或审核中的验方不能归档");

            _status = FormulaStatus.Archived;
        }

        /// <summary>
        /// 设为公开
        /// </summary>
        public void MakePublic()
        {
            if (!_isClassic && _status != FormulaStatus.Approved)
                throw new FormulaDomainException("只有已批准的验方才能设为公开");

            _isPublic = true;
        }

        /// <summary>
        /// 设为私有
        /// </summary>
        public void MakePrivate()
        {
            if (_isClassic)
                throw new FormulaDomainException("经典方不能设为私有");

            _isPublic = false;
        }

        #endregion

        #region 新增属性管理

        /// <summary>
        /// 更新验方信息
        /// </summary>
        public void UpdateFormulaInfo(
            string chineseName = null,
            string englishName = null,
            string pinYinCode = null,
            string wuBiCode = null,
            string classification = null)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            _formulaInfo = FormulaInfo.Create(
                chineseName ?? _formulaInfo.ChineseName,
                englishName ?? _formulaInfo.EnglishName,
                pinYinCode ?? _formulaInfo.PinYinCode,
                wuBiCode ?? _formulaInfo.WuBiCode,
                classification ?? _formulaInfo.Classification);
            
            IncrementVersion();
        }

        /// <summary>
        /// 更新验方功效
        /// </summary>
        public void UpdateEfficacy(string mainEffects, string indications = null, string mechanism = null)
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能修改");

            _efficacy = FormulaEfficacy.Create(mainEffects, indications, mechanism);
            IncrementVersion();
        }

        /// <summary>
        /// 停用验方
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            IncrementVersion();
        }

        /// <summary>
        /// 激活验方
        /// </summary>
        public void Activate()
        {
            if (_status == FormulaStatus.Archived)
                throw new FormulaDomainException("已归档的验方不能激活");

            _isActive = true;
            IncrementVersion();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 创建处方
        /// </summary>
        public Dictionary<Guid, decimal> CreatePrescriptionItems(decimal dosageMultiplier = 1.0m)
        {
            if (!CanBeUsed)
                throw new FormulaDomainException("验方状态不允许使用");

            var items = new Dictionary<Guid, decimal>();
            foreach (var herb in _herbs)
            {
                items[herb.HerbId] = herb.Dosage * dosageMultiplier;
            }

            return items;
        }

        /// <summary>
        /// 检查药材配伍
        /// </summary>
        public bool HasIncompatibleHerbs(List<Guid> otherHerbIds)
        {
            // 这里可以实现十八反、十九畏的检查逻辑
            // 需要与Herb聚合根协作
            return false;
        }

        /// <summary>
        /// 获取君臣佐使分析
        /// </summary>
        public string GetHerbRoleAnalysis()
        {
            var monarch = _herbs.Where(h => h.Role == HerbRole.Monarch).Select(h => h.HerbName);
            var minister = _herbs.Where(h => h.Role == HerbRole.Minister).Select(h => h.HerbName);
            var assistant = _herbs.Where(h => h.Role == HerbRole.Assistant).Select(h => h.HerbName);
            var guide = _herbs.Where(h => h.Role == HerbRole.Guide).Select(h => h.HerbName);

            var analysis = new List<string>();
            if (monarch.Any()) analysis.Add($"君药：{string.Join("、", monarch)}");
            if (minister.Any()) analysis.Add($"臣药：{string.Join("、", minister)}");
            if (assistant.Any()) analysis.Add($"佐药：{string.Join("、", assistant)}");
            if (guide.Any()) analysis.Add($"使药：{string.Join("、", guide)}");

            return string.Join("；", analysis);
        }

        /// <summary>
        /// 克隆验方（用于创建个人验方）
        /// </summary>
        public Formula Clone(Guid creatorId, string creatorName, string newName = null)
        {
            var clonedFormula = new Formula(
                GenerateCode(),
                newName ?? $"{_name}_个人",
                _pinyin,
                $"基于{_source}",
                _category,
                _targetSyndrome,
                _treatmentPrinciple,
                _type,
                false);

            clonedFormula._creatorId = creatorId;
            clonedFormula._creatorName = creatorName;
            clonedFormula._isPublic = false;

            // 复制药材组成
            foreach (var herb in _herbs)
            {
                clonedFormula.AddHerb(
                    herb.HerbId,
                    herb.HerbName,
                    herb.Dosage,
                    herb.Role,
                    herb.ProcessingMethod,
                    herb.SpecialInstructions);
            }

            // 复制其他信息
            clonedFormula._indication = _indication;
            clonedFormula._contraindication = _contraindication;
            clonedFormula._preparation = _preparation;
            clonedFormula._usage = _usage;
            clonedFormula._modification = _modification;

            return clonedFormula;
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证验方完整性
        /// </summary>
        private void ValidateCompleteness()
        {
            if (!_herbs.Any())
                throw new FormulaDomainException("验方至少需要包含一味药材");

            if (!HasMonarchHerb)
                throw new FormulaDomainException("验方必须有君药");

            if (string.IsNullOrWhiteSpace(_indication))
                throw new FormulaDomainException("验方必须有适应症");

            if (string.IsNullOrWhiteSpace(_usage))
                throw new FormulaDomainException("验方必须有用法用量");

            if (_herbs.Count < 2)
                throw new FormulaDomainException("验方药味数过少（至少2味）");

            if (_herbs.Count > 50)
                throw new FormulaDomainException("验方药味数过多（超过50味）");
        }

        #endregion

        #region 私有方法

        private void SetBasicInfo(string code, string name, string pinyin, string source)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new FormulaDomainException("验方编码不能为空");

            if (string.IsNullOrWhiteSpace(name))
                throw new FormulaDomainException("验方名称不能为空");

            if (string.IsNullOrWhiteSpace(pinyin))
                throw new FormulaDomainException("拼音不能为空");

            if (string.IsNullOrWhiteSpace(source))
                throw new FormulaDomainException("出处不能为空");

            _code = code;
            _name = name;
            _pinyin = pinyin;
            _source = source;
        }

        private void SetCategory(FormulaCategory category)
        {
            _category = category ?? throw new FormulaDomainException("验方类别不能为空");
        }

        private void SetSyndromeAndPrinciple(TCMSyndrome syndrome, TreatmentPrinciple principle)
        {
            _targetSyndrome = syndrome ?? throw new FormulaDomainException("目标证型不能为空");
            _treatmentPrinciple = principle ?? throw new FormulaDomainException("治法不能为空");
        }

        private void IncrementVersion()
        {
            _version++;
        }

        private string GenerateCode()
        {
            return $"FM{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";
        }

        private void AddNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return;

            _notes = string.IsNullOrWhiteSpace(_notes)
                ? notes
                : $"{_notes}\n{notes}";
        }

        #endregion
    }

    #region 实体

    /// <summary>
    /// 验方药材实体
    /// </summary>
    public class FormulaHerb : Entity
    {
        public Guid HerbId { get; private set; }
        public string HerbName { get; private set; }
        public decimal Dosage { get; private set; }
        public HerbRole Role { get; private set; }
        public ProcessingMethod ProcessingMethod { get; private set; }
        public string SpecialInstructions { get; private set; }
        public int Sequence { get; private set; }

        protected FormulaHerb() { }

        public FormulaHerb(
            Guid id,
            Guid herbId,
            string herbName,
            decimal dosage,
            HerbRole role,
            ProcessingMethod processingMethod,
            string specialInstructions,
            int sequence)
        {
            Id = id;
            HerbId = herbId;
            HerbName = herbName;
            Dosage = dosage;
            Role = role;
            ProcessingMethod = processingMethod;
            SpecialInstructions = specialInstructions;
            Sequence = sequence;
        }

        public void UpdateDosage(decimal newDosage)
        {
            if (newDosage <= 0)
                throw new FormulaDomainException("剂量必须大于0");
            Dosage = newDosage;
        }

        public void UpdateRole(HerbRole newRole)
        {
            Role = newRole ?? throw new FormulaDomainException("药材角色不能为空");
        }

        public void UpdateSequence(int newSequence)
        {
            Sequence = newSequence;
        }
    }

    /// <summary>
    /// 临床案例实体
    /// </summary>
    public class ClinicalCase : Entity
    {
        public string PatientInfo { get; private set; }
        public string Diagnosis { get; private set; }
        public string Treatment { get; private set; }
        public string Outcome { get; private set; }
        public DateTime TreatmentDate { get; private set; }
        public string DoctorName { get; private set; }

        protected ClinicalCase() { }

        public ClinicalCase(
            Guid id,
            string patientInfo,
            string diagnosis,
            string treatment,
            string outcome,
            DateTime treatmentDate,
            string doctorName)
        {
            Id = id;
            PatientInfo = patientInfo;
            Diagnosis = diagnosis;
            Treatment = treatment;
            Outcome = outcome;
            TreatmentDate = treatmentDate;
            DoctorName = doctorName;
        }
    }

    /// <summary>
    /// 验方变化实体
    /// </summary>
    public class FormulaVariation : Entity
    {
        public string Name { get; private set; }
        public string Condition { get; private set; }
        public string Modification { get; private set; }
        public string Indication { get; private set; }

        protected FormulaVariation() { }

        public FormulaVariation(
            Guid id,
            string name,
            string condition,
            string modification,
            string indication)
        {
            Id = id;
            Name = name;
            Condition = condition;
            Modification = modification;
            Indication = indication;
        }
    }

    #endregion

    #region 值对象

    /// <summary>
    /// 验方类别
    /// </summary>
    public class FormulaCategory : Enumeration
    {
        public static FormulaCategory JieBiao = new(1, "解表剂");
        public static FormulaCategory XieXia = new(2, "泻下剂");
        public static FormulaCategory HeJie = new(3, "和解剂");
        public static FormulaCategory QingRe = new(4, "清热剂");
        public static FormulaCategory WenLi = new(5, "温里剂");
        public static FormulaCategory BuYi = new(6, "补益剂");
        public static FormulaCategory AnShen = new(7, "安神剂");
        public static FormulaCategory KaiQiao = new(8, "开窍剂");
        public static FormulaCategory GuSe = new(9, "固涩剂");
        public static FormulaCategory LiQi = new(10, "理气剂");
        public static FormulaCategory LiXue = new(11, "理血剂");
        public static FormulaCategory ZhiFeng = new(12, "治风剂");
        public static FormulaCategory ZhiZao = new(13, "治燥剂");
        public static FormulaCategory QuShi = new(14, "祛湿剂");
        public static FormulaCategory QuTan = new(15, "祛痰剂");
        public static FormulaCategory XiaoDao = new(16, "消导剂");
        public static FormulaCategory QuChong = new(17, "驱虫剂");

        public FormulaCategory(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 验方类型
    /// </summary>
    public class FormulaType : Enumeration
    {
        public static FormulaType Classic = new(1, "经典方");
        public static FormulaType Experience = new(2, "经验方");
        public static FormulaType Hospital = new(3, "院内制剂");
        public static FormulaType Personal = new(4, "个人验方");
        public static FormulaType Modified = new(5, "加减方");

        public FormulaType(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 验方状态
    /// </summary>
    public class FormulaStatus : Enumeration
    {
        public static FormulaStatus Draft = new(1, "草稿");
        public static FormulaStatus UnderReview = new(2, "审核中");
        public static FormulaStatus Approved = new(3, "已批准");
        public static FormulaStatus Rejected = new(4, "已拒绝");
        public static FormulaStatus Archived = new(5, "已归档");

        public FormulaStatus(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 药材角色（君臣佐使）
    /// </summary>
    public class HerbRole : Enumeration
    {
        public static HerbRole Monarch = new(1, "君药");
        public static HerbRole Minister = new(2, "臣药");
        public static HerbRole Assistant = new(3, "佐药");
        public static HerbRole Guide = new(4, "使药");

        public HerbRole(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 炮制方法
    /// </summary>
    public class ProcessingMethod : Enumeration
    {
        public static ProcessingMethod Raw = new(1, "生");
        public static ProcessingMethod Fried = new(2, "炒");
        public static ProcessingMethod HoneyFried = new(3, "蜜炙");
        public static ProcessingMethod WineFried = new(4, "酒炙");
        public static ProcessingMethod SaltFried = new(5, "盐炙");
        public static ProcessingMethod Charred = new(6, "炭");
        public static ProcessingMethod Steamed = new(7, "蒸");
        public static ProcessingMethod Boiled = new(8, "煮");

        public ProcessingMethod(int id, string name) : base(id, name) { }
    }

    #endregion
}