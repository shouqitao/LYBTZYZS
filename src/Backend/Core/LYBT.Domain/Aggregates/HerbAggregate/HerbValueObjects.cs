using System.Collections.Generic;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.ValueObjects;

namespace LYBT.Domain.Aggregates.HerbAggregate
{
    #region 药材分类

    /// <summary>
    /// 药材类别
    /// </summary>
    public class HerbCategory : Enumeration
    {
        public static HerbCategory JiebiaoYao = new(1, "解表药");
        public static HerbCategory QingReYao = new(2, "清热药");
        public static HerbCategory XieXiaYao = new(3, "泻下药");
        public static HerbCategory QuFengShiYao = new(4, "祛风湿药");
        public static HerbCategory HuaShiYao = new(5, "化湿药");
        public static HerbCategory LiShuiShenShiYao = new(6, "利水渗湿药");
        public static HerbCategory WenLiYao = new(7, "温里药");
        public static HerbCategory LiQiYao = new(8, "理气药");
        public static HerbCategory XiaoShiYao = new(9, "消食药");
        public static HerbCategory QvChongYao = new(10, "驱虫药");
        public static HerbCategory ZhiXueYao = new(11, "止血药");
        public static HerbCategory HuoXueHuaYuYao = new(12, "活血化瘀药");
        public static HerbCategory HuaTanZhiKeYao = new(13, "化痰止咳药");
        public static HerbCategory AnShenYao = new(14, "安神药");
        public static HerbCategory PingGanXiFengYao = new(15, "平肝息风药");
        public static HerbCategory KaiQiaoYao = new(16, "开窍药");
        public static HerbCategory BuYiYao = new(17, "补益药");
        public static HerbCategory ShouSeYao = new(18, "收涩药");
        public static HerbCategory YongTuCuiYao = new(19, "涌吐催药");
        public static HerbCategory WaiYongYao = new(20, "外用药");

        public HerbCategory(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 药性药味

    /// <summary>
    /// 药性（四气）
    /// </summary>
    public class HerbNature : Enumeration
    {
        public static HerbNature Cold = new(1, "寒");
        public static HerbNature Cool = new(2, "凉");
        public static HerbNature Neutral = new(3, "平");
        public static HerbNature Warm = new(4, "温");
        public static HerbNature Hot = new(5, "热");

        public HerbNature(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 药味（五味）
    /// </summary>
    public class HerbFlavor : Enumeration
    {
        public static HerbFlavor Sour = new(1, "酸");
        public static HerbFlavor Bitter = new(2, "苦");
        public static HerbFlavor Sweet = new(3, "甘");
        public static HerbFlavor Pungent = new(4, "辛");
        public static HerbFlavor Salty = new(5, "咸");
        public static HerbFlavor Bland = new(6, "淡");
        public static HerbFlavor Astringent = new(7, "涩");

        public HerbFlavor(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 归经
    /// </summary>
    public class Meridian : Enumeration
    {
        public static Meridian Liver = new(1, "肝经");
        public static Meridian Heart = new(2, "心经");
        public static Meridian Spleen = new(3, "脾经");
        public static Meridian Lung = new(4, "肺经");
        public static Meridian Kidney = new(5, "肾经");
        public static Meridian Pericardium = new(6, "心包经");
        public static Meridian TripleEnergizer = new(7, "三焦经");
        public static Meridian GallBladder = new(8, "胆经");
        public static Meridian SmallIntestine = new(9, "小肠经");
        public static Meridian Stomach = new(10, "胃经");
        public static Meridian LargeIntestine = new(11, "大肠经");
        public static Meridian Bladder = new(12, "膀胱经");

        public Meridian(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 功效分类

    /// <summary>
    /// 功效类别
    /// </summary>
    public class EffectCategory : Enumeration
    {
        public static EffectCategory Primary = new(1, "主要功效");
        public static EffectCategory Secondary = new(2, "次要功效");
        public static EffectCategory Special = new(3, "特殊功效");

        public EffectCategory(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 配伍禁忌

    /// <summary>
    /// 配伍禁忌类型
    /// </summary>
    public class IncompatibilityType : Enumeration
    {
        public static IncompatibilityType Eighteen = new(1, "十八反");
        public static IncompatibilityType Other = new(2, "其他禁忌");

        public IncompatibilityType(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 配伍慎用类型
    /// </summary>
    public class CautionType : Enumeration
    {
        public static CautionType Nineteen = new(1, "十九畏");
        public static CautionType Other = new(2, "其他慎用");

        public CautionType(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 质量等级

    /// <summary>
    /// 药材质量等级
    /// </summary>
    public class HerbQualityGrade : Enumeration
    {
        public static HerbQualityGrade Standard = new(1, "普通");
        public static HerbQualityGrade Premium = new(2, "优质");
        public static HerbQualityGrade Superior = new(3, "特级");
        public static HerbQualityGrade Authentic = new(4, "道地");

        public HerbQualityGrade(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 炮制方法

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
        public static ProcessingMethod Calcined = new(9, "煅");
        public static ProcessingMethod Processed = new(10, "制");

        public ProcessingMethod(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 剂量范围

    /// <summary>
    /// 剂量范围值对象
    /// </summary>
    public class DosageRange : ValueObject
    {
        public decimal MinDosage { get; private set; }
        public decimal MaxDosage { get; private set; }
        public decimal CommonDosage { get; private set; }
        public string Unit { get; private set; }

        protected DosageRange() { }

        public DosageRange(decimal minDosage, decimal maxDosage, decimal commonDosage, string unit = "g")
        {
            if (minDosage <= 0)
                throw new HerbDomainException("最小剂量必须大于0");

            if (maxDosage <= minDosage)
                throw new HerbDomainException("最大剂量必须大于最小剂量");

            if (commonDosage < minDosage || commonDosage > maxDosage)
                throw new HerbDomainException("常用剂量必须在最小和最大剂量之间");

            MinDosage = minDosage;
            MaxDosage = maxDosage;
            CommonDosage = commonDosage;
            Unit = unit;
        }

        public bool IsInRange(decimal dosage)
        {
            return dosage >= MinDosage && dosage <= MaxDosage;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return MinDosage;
            yield return MaxDosage;
            yield return CommonDosage;
            yield return Unit;
        }

        public override string ToString()
        {
            return $"{MinDosage}-{MaxDosage}{Unit}（常用{CommonDosage}{Unit}）";
        }
    }

    #endregion

    #region 毒性分级

    /// <summary>
    /// 中药材毒性等级
    /// </summary>
    public class HerbToxicity : Enumeration
    {
        public static HerbToxicity NonToxic = new(1, "无毒");
        public static HerbToxicity LowToxic = new(2, "小毒");
        public static HerbToxicity Toxic = new(3, "有毒");
        public static HerbToxicity HighToxic = new(4, "大毒");

        public HerbToxicity(int id, string name) : base(id, name) { }
    }

    #endregion
}