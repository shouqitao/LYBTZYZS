using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.FormulaAggregate.ValueObjects
{
    /// <summary>
    /// 验方名称值对象 - UltraThink重构DDD架构
    /// </summary>
    public class FormulaName : SingleValueObject<string>
    {
        private static readonly Regex ValidFormulaNameRegex = new(@"^[\u4e00-\u9fa5\w\s\-\(\)（）]{1,100}$", RegexOptions.Compiled);

        private FormulaName(string value) : base(value) { }

        public static FormulaName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("验方名称不能为空", nameof(value));

            value = value.Trim();

            if (!ValidFormulaNameRegex.IsMatch(value))
                throw new ArgumentException($"验方名称格式不正确: '{value}'", nameof(value));

            return new FormulaName(value);
        }
    }

    /// <summary>
    /// 验方类型枚举值对象
    /// </summary>
    public class FormulaType : Enumeration<FormulaType>
    {
        public static readonly FormulaType Classical = new(1, nameof(Classical), "经典验方", "历代医家传承的经典方剂");
        public static readonly FormulaType Modern = new(2, nameof(Modern), "现代验方", "现代临床总结的有效方剂");
        public static readonly FormulaType Personal = new(3, nameof(Personal), "个人验方", "医生个人临床经验方");
        public static readonly FormulaType Hospital = new(4, nameof(Hospital), "院内制剂", "医院内部制剂方");
        public static readonly FormulaType Research = new(5, nameof(Research), "科研方剂", "科研项目验证方剂");

        public string DisplayName { get; }
        public string Description { get; }

        private FormulaType(int value, string name, string displayName, string description) : base(value, name)
        {
            DisplayName = displayName;
            Description = description;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 验方来源值对象
    /// </summary>
    public class FormulaSource : ValueObject
    {
        public string BookName { get; } // 出处书籍
        public string Author { get; } // 作者
        public string Dynasty { get; } // 朝代
        public string Edition { get; } // 版本

        private FormulaSource(string bookName, string author, string dynasty, string edition)
        {
            BookName = bookName;
            Author = author;
            Dynasty = dynasty;
            Edition = edition;
        }

        public static FormulaSource Create(
            string bookName,
            string author = null,
            string dynasty = null,
            string edition = null)
        {
            if (string.IsNullOrWhiteSpace(bookName))
                throw new ArgumentException("验方出处不能为空", nameof(bookName));

            return new FormulaSource(
                bookName.Trim(),
                author?.Trim(),
                dynasty?.Trim(),
                edition?.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return BookName;
            yield return Author ?? "";
            yield return Dynasty ?? "";
            yield return Edition ?? "";
        }

        public override string ToString()
        {
            var parts = new List<string> { BookName };
            if (!string.IsNullOrEmpty(Author)) parts.Add(Author);
            if (!string.IsNullOrEmpty(Dynasty)) parts.Add($"{Dynasty}代");
            if (!string.IsNullOrEmpty(Edition)) parts.Add(Edition);
            return string.Join("·", parts);
        }
    }

    /// <summary>
    /// 验方功效分类枚举值对象
    /// </summary>
    public class FormulaEfficacyCategory : Enumeration<FormulaEfficacyCategory>
    {
        public static readonly FormulaEfficacyCategory ReliefExterior = new(1, nameof(ReliefExterior), "解表剂", "发汗解肌，宣肺散邪");
        public static readonly FormulaEfficacyCategory Purgative = new(2, nameof(Purgative), "泻下剂", "通便泄热，逐水退肿");
        public static readonly FormulaEfficacyCategory HarmonizeInterior = new(3, nameof(HarmonizeInterior), "和解剂", "疏肝理气，调和脾胃");
        public static readonly FormulaEfficacyCategory ClearHeat = new(4, nameof(ClearHeat), "清热剂", "清热泻火，凉血解毒");
        public static readonly FormulaEfficacyCategory WarmInterior = new(5, nameof(WarmInterior), "温里剂", "温中散寒，回阳救逆");
        public static readonly FormulaEfficacyCategory Tonify = new(6, nameof(Tonify), "补益剂", "补气养血，滋阴助阳");
        public static readonly FormulaEfficacyCategory Calm = new(7, nameof(Calm), "安神剂", "养心安神，镇静定志");
        public static readonly FormulaEfficacyCategory RegulateQi = new(8, nameof(RegulateQi), "理气剂", "行气导滞，降逆平喘");
        public static readonly FormulaEfficacyCategory RegulateBlood = new(9, nameof(RegulateBlood), "理血剂", "活血化瘀，止血凉血");
        public static readonly FormulaEfficacyCategory ExpelWind = new(10, nameof(ExpelWind), "祛风剂", "祛风通络，化痰定惊");
        public static readonly FormulaEfficacyCategory DrainDamp = new(11, nameof(DrainDamp), "祛湿剂", "化湿利水，健脾渗湿");
        public static readonly FormulaEfficacyCategory TransformPhlegm = new(12, nameof(TransformPhlegm), "祛痰剂", "化痰止咳，宽胸散结");

        public string DisplayName { get; }
        public string Description { get; }

        private FormulaEfficacyCategory(int value, string name, string displayName, string description) : base(value, name)
        {
            DisplayName = displayName;
            Description = description;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 验方适用证候值对象
    /// </summary>
    public class FormulaSyndrome : ValueObject
    {
        public string MainSyndrome { get; } // 主要证候
        public IReadOnlyList<string> Symptoms { get; } // 症状表现
        public string TongueCondition { get; } // 舌象
        public string PulseCondition { get; } // 脉象

        private FormulaSyndrome(
            string mainSyndrome,
            List<string> symptoms,
            string tongueCondition,
            string pulseCondition)
        {
            MainSyndrome = mainSyndrome;
            Symptoms = symptoms?.Where(s => !string.IsNullOrWhiteSpace(s))
                              .Select(s => s.Trim())
                              .ToList() ?? new List<string>();
            TongueCondition = tongueCondition;
            PulseCondition = pulseCondition;
        }

        public static FormulaSyndrome Create(
            string mainSyndrome,
            List<string> symptoms = null,
            string tongueCondition = null,
            string pulseCondition = null)
        {
            if (string.IsNullOrWhiteSpace(mainSyndrome))
                throw new ArgumentException("主要证候不能为空", nameof(mainSyndrome));

            return new FormulaSyndrome(
                mainSyndrome.Trim(),
                symptoms,
                tongueCondition?.Trim(),
                pulseCondition?.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MainSyndrome;
            yield return TongueCondition ?? "";
            yield return PulseCondition ?? "";
            foreach (var symptom in Symptoms.OrderBy(s => s))
            {
                yield return symptom;
            }
        }

        public override string ToString()
        {
            var parts = new List<string> { MainSyndrome };
            if (Symptoms.Any()) parts.Add($"症见: {string.Join("，", Symptoms)}");
            if (!string.IsNullOrEmpty(TongueCondition)) parts.Add($"舌{TongueCondition}");
            if (!string.IsNullOrEmpty(PulseCondition)) parts.Add($"脉{PulseCondition}");
            return string.Join("；", parts);
        }
    }

    /// <summary>
    /// 验方组成药物值对象
    /// </summary>
    public class FormulaComposition : ValueObject
    {
        public Guid HerbId { get; } // 药材ID
        public string HerbName { get; } // 药材名称
        public decimal StandardDosage { get; } // 标准剂量
        public string DosageUnit { get; } // 剂量单位
        public string Role { get; } // 在方中的作用（君臣佐使）
        public string Usage { get; } // 特殊用法（如先煎、后下、包煎等）
        public bool IsOptional { get; } // 是否可选药味

        private FormulaComposition(
            Guid herbId,
            string herbName,
            decimal standardDosage,
            string dosageUnit,
            string role,
            string usage,
            bool isOptional)
        {
            HerbId = herbId;
            HerbName = herbName;
            StandardDosage = standardDosage;
            DosageUnit = dosageUnit;
            Role = role;
            Usage = usage;
            IsOptional = isOptional;
        }

        public static FormulaComposition Create(
            Guid herbId,
            string herbName,
            decimal standardDosage,
            string dosageUnit,
            string role = null,
            string usage = null,
            bool isOptional = false)
        {
            if (herbId == Guid.Empty)
                throw new ArgumentException("药材ID不能为空", nameof(herbId));

            if (string.IsNullOrWhiteSpace(herbName))
                throw new ArgumentException("药材名称不能为空", nameof(herbName));

            if (standardDosage <= 0)
                throw new ArgumentException("标准剂量必须大于0", nameof(standardDosage));

            if (string.IsNullOrWhiteSpace(dosageUnit))
                throw new ArgumentException("剂量单位不能为空", nameof(dosageUnit));

            // 验证角色
            if (!string.IsNullOrEmpty(role))
            {
                var validRoles = new[] { "君药", "臣药", "佐药", "使药" };
                if (!validRoles.Contains(role.Trim()))
                    throw new ArgumentException($"无效的药物角色: '{role}'. 有效角色: {string.Join("、", validRoles)}", nameof(role));
            }

            return new FormulaComposition(
                herbId,
                herbName.Trim(),
                standardDosage,
                dosageUnit.Trim(),
                role?.Trim(),
                usage?.Trim(),
                isOptional);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return HerbId;
            yield return HerbName;
            yield return StandardDosage;
            yield return DosageUnit;
            yield return Role ?? "";
            yield return Usage ?? "";
            yield return IsOptional;
        }

        public override string ToString()
        {
            var parts = new List<string> { $"{HerbName} {StandardDosage}{DosageUnit}" };
            if (!string.IsNullOrEmpty(Role)) parts.Add($"({Role})");
            if (!string.IsNullOrEmpty(Usage)) parts.Add($"[{Usage}]");
            if (IsOptional) parts.Add("(可选)");
            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// 验方用法用量值对象
    /// </summary>
    public class FormulaUsage : ValueObject
    {
        public string PreparationMethod { get; } // 制备方法
        public string AdministrationMethod { get; } // 服用方法
        public string Dosage { get; } // 用量
        public string Frequency { get; } // 频次
        public string Course { get; } // 疗程
        public string Precautions { get; } // 注意事项

        private FormulaUsage(
            string preparationMethod,
            string administrationMethod,
            string dosage,
            string frequency,
            string course,
            string precautions)
        {
            PreparationMethod = preparationMethod;
            AdministrationMethod = administrationMethod;
            Dosage = dosage;
            Frequency = frequency;
            Course = course;
            Precautions = precautions;
        }

        public static FormulaUsage Create(
            string preparationMethod,
            string administrationMethod,
            string dosage = null,
            string frequency = null,
            string course = null,
            string precautions = null)
        {
            if (string.IsNullOrWhiteSpace(preparationMethod))
                throw new ArgumentException("制备方法不能为空", nameof(preparationMethod));

            if (string.IsNullOrWhiteSpace(administrationMethod))
                throw new ArgumentException("服用方法不能为空", nameof(administrationMethod));

            return new FormulaUsage(
                preparationMethod.Trim(),
                administrationMethod.Trim(),
                dosage?.Trim(),
                frequency?.Trim(),
                course?.Trim(),
                precautions?.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return PreparationMethod;
            yield return AdministrationMethod;
            yield return Dosage ?? "";
            yield return Frequency ?? "";
            yield return Course ?? "";
            yield return Precautions ?? "";
        }

        public override string ToString()
        {
            var parts = new List<string> { PreparationMethod, AdministrationMethod };
            if (!string.IsNullOrEmpty(Dosage)) parts.Add($"用量: {Dosage}");
            if (!string.IsNullOrEmpty(Frequency)) parts.Add($"频次: {Frequency}");
            if (!string.IsNullOrEmpty(Course)) parts.Add($"疗程: {Course}");
            return string.Join("，", parts);
        }
    }

    /// <summary>
    /// 验方禁忌症值对象
    /// </summary>
    public class FormulaContraindication : ValueObject
    {
        public IReadOnlyList<string> Contraindications { get; } // 禁忌症列表
        public IReadOnlyList<string> Precautions { get; } // 使用注意事项

        private FormulaContraindication(List<string> contraindications, List<string> precautions)
        {
            Contraindications = contraindications?.Where(c => !string.IsNullOrWhiteSpace(c))
                                                 .Select(c => c.Trim())
                                                 .ToList() ?? new List<string>();
            Precautions = precautions?.Where(p => !string.IsNullOrWhiteSpace(p))
                                     .Select(p => p.Trim())
                                     .ToList() ?? new List<string>();
        }

        public static FormulaContraindication Create(
            List<string> contraindications = null,
            List<string> precautions = null)
        {
            return new FormulaContraindication(contraindications, precautions);
        }

        public bool HasContraindications() => Contraindications.Any();
        public bool HasPrecautions() => Precautions.Any();
        public bool HasAnyRestrictions() => HasContraindications() || HasPrecautions();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var contraindication in Contraindications.OrderBy(c => c))
            {
                yield return contraindication;
            }
            foreach (var precaution in Precautions.OrderBy(p => p))
            {
                yield return precaution;
            }
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (HasContraindications()) parts.Add($"禁忌: {string.Join("、", Contraindications)}");
            if (HasPrecautions()) parts.Add($"注意: {string.Join("、", Precautions)}");
            return parts.Any() ? string.Join("；", parts) : "无特殊禁忌";
        }
    }

    /// <summary>
    /// 验方信息值对象
    /// </summary>
    public class FormulaInfo : ValueObject
    {
        public string ChineseName { get; } // 中文名称
        public string EnglishName { get; } // 英文名称
        public string PinYinCode { get; } // 拼音码
        public string WuBiCode { get; } // 五笔码
        public string Classification { get; } // 分类

        private FormulaInfo(
            string chineseName,
            string englishName,
            string pinYinCode,
            string wuBiCode,
            string classification)
        {
            ChineseName = chineseName;
            EnglishName = englishName;
            PinYinCode = pinYinCode;
            WuBiCode = wuBiCode;
            Classification = classification;
        }

        public static FormulaInfo Create(
            string chineseName,
            string englishName = null,
            string pinYinCode = null,
            string wuBiCode = null,
            string classification = null)
        {
            if (string.IsNullOrWhiteSpace(chineseName))
                throw new ArgumentException("中文名称不能为空", nameof(chineseName));

            return new FormulaInfo(
                chineseName.Trim(),
                englishName?.Trim(),
                pinYinCode?.Trim().ToUpper(),
                wuBiCode?.Trim().ToUpper(),
                classification?.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ChineseName;
            yield return EnglishName ?? "";
            yield return PinYinCode ?? "";
            yield return WuBiCode ?? "";
            yield return Classification ?? "";
        }

        public override string ToString()
        {
            var parts = new List<string> { ChineseName };
            if (!string.IsNullOrEmpty(EnglishName)) parts.Add($"({EnglishName})");
            if (!string.IsNullOrEmpty(Classification)) parts.Add($"[{Classification}]");
            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// 验方功效值对象
    /// </summary>
    public class FormulaEfficacy : ValueObject
    {
        public string MainEffects { get; } // 主要功效
        public string Indications { get; } // 适应症
        public string Mechanism { get; } // 作用机理

        private FormulaEfficacy(
            string mainEffects,
            string indications,
            string mechanism)
        {
            MainEffects = mainEffects;
            Indications = indications;
            Mechanism = mechanism;
        }

        public static FormulaEfficacy Create(
            string mainEffects,
            string indications = null,
            string mechanism = null)
        {
            if (string.IsNullOrWhiteSpace(mainEffects))
                throw new ArgumentException("主要功效不能为空", nameof(mainEffects));

            return new FormulaEfficacy(
                mainEffects.Trim(),
                indications?.Trim(),
                mechanism?.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MainEffects;
            yield return Indications ?? "";
            yield return Mechanism ?? "";
        }

        public override string ToString()
        {
            var parts = new List<string> { $"功效: {MainEffects}" };
            if (!string.IsNullOrEmpty(Indications)) parts.Add($"适应症: {Indications}");
            if (!string.IsNullOrEmpty(Mechanism)) parts.Add($"机理: {Mechanism}");
            return string.Join("；", parts);
        }
    }

    /// <summary>
    /// 验方审批值对象
    /// </summary>
    public class FormulaApproval : ValueObject
    {
        public bool IsApproved { get; } // 是否已审批
        public string ApproverName { get; } // 审批人
        public DateTime? ApprovalDate { get; } // 审批日期
        public string ApprovalComments { get; } // 审批意见

        private FormulaApproval(
            bool isApproved,
            string approverName,
            DateTime? approvalDate,
            string approvalComments)
        {
            IsApproved = isApproved;
            ApproverName = approverName;
            ApprovalDate = approvalDate;
            ApprovalComments = approvalComments;
        }

        public static FormulaApproval CreatePending()
        {
            return new FormulaApproval(false, null, null, null);
        }

        public static FormulaApproval CreateApproved(
            string approverName,
            DateTime? approvalDate = null,
            string comments = null)
        {
            if (string.IsNullOrWhiteSpace(approverName))
                throw new ArgumentException("审批人不能为空", nameof(approverName));

            return new FormulaApproval(
                true,
                approverName.Trim(),
                approvalDate ?? DateTime.UtcNow,
                comments?.Trim());
        }

        public static FormulaApproval CreateRejected(
            string reviewerName,
            string comments,
            DateTime? reviewDate = null)
        {
            if (string.IsNullOrWhiteSpace(reviewerName))
                throw new ArgumentException("审核人不能为空", nameof(reviewerName));

            if (string.IsNullOrWhiteSpace(comments))
                throw new ArgumentException("拒绝意见不能为空", nameof(comments));

            return new FormulaApproval(
                false,
                reviewerName.Trim(),
                reviewDate ?? DateTime.UtcNow,
                comments.Trim());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return IsApproved;
            yield return ApproverName ?? "";
            yield return ApprovalDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
            yield return ApprovalComments ?? "";
        }

        // Repository兼容性属性
        /// <summary>
        /// 审核人ID - Repository兼容性属性
        /// </summary>
        public string ReviewerId => ApproverName ?? "";

        /// <summary>
        /// 审核时间 - Repository兼容性属性
        /// </summary>
        public DateTime? ReviewTime => ApprovalDate;

        public override string ToString()
        {
            if (!IsApproved)
            {
                if (string.IsNullOrEmpty(ApproverName))
                    return "待审批";
                else
                    return $"已拒绝 - {ApproverName} ({ApprovalDate:yyyy-MM-dd})";
            }

            return $"已审批 - {ApproverName} ({ApprovalDate:yyyy-MM-dd})";
        }
    }
}