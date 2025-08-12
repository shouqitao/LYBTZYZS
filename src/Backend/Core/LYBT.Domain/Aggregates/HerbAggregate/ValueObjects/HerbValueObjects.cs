using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.HerbAggregate.ValueObjects
{
    /// <summary>
    /// 中药材名称值对象 - UltraThink重构DDD架构
    /// </summary>
    public class HerbName : SingleValueObject<string>
    {
        private static readonly Regex ValidHerbNameRegex = new(@"^[\u4e00-\u9fa5\w\s\-\(\)（）]{1,50}$", RegexOptions.Compiled);

        private HerbName(string value) : base(value) { }

        public static HerbName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("中药材名称不能为空", nameof(value));

            value = value.Trim();

            if (!ValidHerbNameRegex.IsMatch(value))
                throw new ArgumentException($"中药材名称格式不正确: '{value}'", nameof(value));

            return new HerbName(value);
        }
    }

    /// <summary>
    /// 中药材分类枚举值对象
    /// </summary>
    public class HerbCategory : Enumeration<HerbCategory>
    {
        public static readonly HerbCategory QingRe = new(1, nameof(QingRe), "清热药", "清热泻火、凉血解毒");
        public static readonly HerbCategory JiebiaoYao = new(2, nameof(JiebiaoYao), "解表药", "发汗解表、祛风散寒");
        public static readonly HerbCategory LiShuiShenShi = new(3, nameof(LiShuiShenShi), "利水渗湿药", "利水消肿、渗湿利尿");
        public static readonly HerbCategory FenglishiYao = new(4, nameof(FenglishiYao), "祛风湿药", "祛风除湿、通络止痛");
        public static readonly HerbCategory HuoxueHuayu = new(5, nameof(HuoxueHuayu), "活血化瘀药", "活血散瘀、通经止痛");
        public static readonly HerbCategory HuatanZhike = new(6, nameof(HuatanZhike), "化痰止咳平喘药", "化痰止咳、平喘降气");
        public static readonly HerbCategory AnShen = new(7, nameof(AnShen), "安神药", "宁心安神、镇静催眠");
        public static readonly HerbCategory PingGanXiFeng = new(8, nameof(PingGanXiFeng), "平肝息风药", "平肝潜阳、息风止痉");
        public static readonly HerbCategory WenliYao = new(9, nameof(WenliYao), "温里药", "温中散寒、回阳救逆");
        public static readonly HerbCategory BuYiYao = new(10, nameof(BuYiYao), "补益药", "补气养血、滋阴助阳");
        public static readonly HerbCategory ShouseYao = new(11, nameof(ShouseYao), "收涩药", "固表止汗、涩精止遗");
        public static readonly HerbCategory LiQi = new(12, nameof(LiQi), "理气药", "行气导滞、降逆平喘");
        public static readonly HerbCategory XiaoShiYao = new(13, nameof(XiaoShiYao), "消食药", "消食化积、健脾和胃");
        public static readonly HerbCategory QuchongYao = new(14, nameof(QuchongYao), "驱虫药", "杀虫消积、涤痰逐水");
        public static readonly HerbCategory ZhixueYao = new(15, nameof(ZhixueYao), "止血药", "收敛止血、凉血止血");
        public static readonly HerbCategory KaiQiao = new(16, nameof(KaiQiao), "开窍药", "开窍醒神、化痰定惊");
        public static readonly HerbCategory Other = new(99, nameof(Other), "其他", "其他类中药材");

        public string DisplayName { get; }
        public string Description { get; }

        private HerbCategory(int value, string name, string displayName, string description) : base(value, name)
        {
            DisplayName = displayName;
            Description = description;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 药性枚举值对象
    /// </summary>
    public class HerbNature : Enumeration<HerbNature>
    {
        public static readonly HerbNature Cold = new(1, nameof(Cold), "寒", "性寒，能清热泻火");
        public static readonly HerbNature Cool = new(2, nameof(Cool), "凉", "性凉，能清热解毒");
        public static readonly HerbNature Neutral = new(3, nameof(Neutral), "平", "性平，无寒热偏性");
        public static readonly HerbNature Warm = new(4, nameof(Warm), "温", "性温，能温中散寒");
        public static readonly HerbNature Hot = new(5, nameof(Hot), "热", "性热，能温阳散寒");

        public string DisplayName { get; }
        public string Description { get; }

        private HerbNature(int value, string name, string displayName, string description) : base(value, name)
        {
            DisplayName = displayName;
            Description = description;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 药味枚举值对象
    /// </summary>
    public class HerbTaste : ValueObject
    {
        public IReadOnlyList<string> Tastes { get; }

        private static readonly string[] ValidTastes = { "酸", "苦", "甘", "辛", "咸", "淡" };

        private HerbTaste(List<string> tastes)
        {
            Tastes = tastes ?? new List<string>();
        }

        public static HerbTaste Create(params string[] tastes)
        {
            if (tastes == null || tastes.Length == 0)
                throw new ArgumentException("药味不能为空", nameof(tastes));

            var processedTastes = new List<string>();
            foreach (var taste in tastes)
            {
                if (string.IsNullOrWhiteSpace(taste))
                    continue;

                var trimmedTaste = taste.Trim();
                if (!ValidTastes.Contains(trimmedTaste))
                    throw new ArgumentException($"无效的药味: '{trimmedTaste}'，有效药味为: {string.Join("、", ValidTastes)}", nameof(tastes));

                if (!processedTastes.Contains(trimmedTaste))
                    processedTastes.Add(trimmedTaste);
            }

            if (!processedTastes.Any())
                throw new ArgumentException("必须提供至少一种有效药味", nameof(tastes));

            return new HerbTaste(processedTastes);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var taste in Tastes.OrderBy(t => t))
            {
                yield return taste;
            }
        }

        public override string ToString() => string.Join("、", Tastes);
    }

    /// <summary>
    /// 归经值对象
    /// </summary>
    public class HerbMeridian : ValueObject
    {
        public IReadOnlyList<string> Meridians { get; }

        private static readonly string[] ValidMeridians = { 
            "肺经", "大肠经", "胃经", "脾经", "心经", "小肠经",
            "膀胱经", "肾经", "心包经", "三焦经", "胆经", "肝经" 
        };

        private HerbMeridian(List<string> meridians)
        {
            Meridians = meridians ?? new List<string>();
        }

        public static HerbMeridian Create(params string[] meridians)
        {
            if (meridians == null || meridians.Length == 0)
                throw new ArgumentException("归经不能为空", nameof(meridians));

            var processedMeridians = new List<string>();
            foreach (var meridian in meridians)
            {
                if (string.IsNullOrWhiteSpace(meridian))
                    continue;

                var trimmedMeridian = meridian.Trim();
                if (!ValidMeridians.Contains(trimmedMeridian))
                    throw new ArgumentException($"无效的归经: '{trimmedMeridian}'，有效归经为: {string.Join("、", ValidMeridians)}", nameof(meridians));

                if (!processedMeridians.Contains(trimmedMeridian))
                    processedMeridians.Add(trimmedMeridian);
            }

            if (!processedMeridians.Any())
                throw new ArgumentException("必须提供至少一条有效归经", nameof(meridians));

            return new HerbMeridian(processedMeridians);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var meridian in Meridians.OrderBy(m => m))
            {
                yield return meridian;
            }
        }

        public override string ToString() => string.Join("、", Meridians);
    }

    /// <summary>
    /// 药材功效值对象
    /// </summary>
    public class HerbEfficacy : SingleValueObject<string>
    {
        private HerbEfficacy(string value) : base(value) { }

        public static HerbEfficacy Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("药材功效不能为空", nameof(value));

            value = value.Trim();

            if (value.Length > 500)
                throw new ArgumentException("药材功效描述不能超过500个字符", nameof(value));

            return new HerbEfficacy(value);
        }
    }

    /// <summary>
    /// 单价值对象
    /// </summary>
    public class HerbPrice : SingleValueObject<decimal>
    {
        private HerbPrice(decimal value) : base(value) { }

        public static HerbPrice Create(decimal value)
        {
            if (value < 0)
                throw new ArgumentException("药材单价不能为负数", nameof(value));

            if (value > 9999.99m)
                throw new ArgumentException("药材单价不能超过9999.99元", nameof(value));

            return new HerbPrice(Math.Round(value, 2)); // 保留两位小数
        }

        public static HerbPrice Zero => new(0);

        public override string ToString() => $"¥{Value:F2}";
    }

    /// <summary>
    /// 药材规格值对象
    /// </summary>
    public class HerbSpecification : SingleValueObject<string>
    {
        private HerbSpecification(string value) : base(value) { }

        public static HerbSpecification Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null; // 规格可以为空

            value = value.Trim();

            if (value.Length > 100)
                throw new ArgumentException("药材规格描述不能超过100个字符", nameof(value));

            return new HerbSpecification(value);
        }
    }

    /// <summary>
    /// 用药禁忌值对象
    /// </summary>
    public class HerbContraindication : ValueObject
    {
        public IReadOnlyList<string> Contraindications { get; }

        private HerbContraindication(List<string> contraindications)
        {
            Contraindications = contraindications?.Where(c => !string.IsNullOrWhiteSpace(c))
                                               .Select(c => c.Trim())
                                               .ToList() ?? new List<string>();
        }

        public static HerbContraindication Create(params string[] contraindications)
        {
            return new HerbContraindication(contraindications?.ToList());
        }

        public bool HasContraindications() => Contraindications.Any();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            foreach (var contraindication in Contraindications.OrderBy(c => c))
            {
                yield return contraindication;
            }
        }

        public override string ToString()
        {
            return HasContraindications() ? string.Join("; ", Contraindications) : "无禁忌";
        }
    }

    /// <summary>
    /// 药材单位枚举值对象
    /// </summary>
    public class HerbUnit : Enumeration<HerbUnit>
    {
        public static readonly HerbUnit Gram = new(1, nameof(Gram), "g", "克");
        public static readonly HerbUnit Kilogram = new(2, nameof(Kilogram), "kg", "千克");
        public static readonly HerbUnit Piece = new(3, nameof(Piece), "片", "片");
        public static readonly HerbUnit Pack = new(4, nameof(Pack), "包", "包");
        public static readonly HerbUnit Bottle = new(5, nameof(Bottle), "瓶", "瓶");
        public static readonly HerbUnit Box = new(6, nameof(Box), "盒", "盒");

        public string Symbol { get; }
        public string DisplayName { get; }

        private HerbUnit(int value, string name, string symbol, string displayName) : base(value, name)
        {
            Symbol = symbol;
            DisplayName = displayName;
        }

        public override string ToString() => DisplayName;
    }
}