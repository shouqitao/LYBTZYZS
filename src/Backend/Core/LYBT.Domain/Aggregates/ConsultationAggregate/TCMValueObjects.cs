using System.Collections.Generic;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.ConsultationAggregate
{
    #region 望诊相关值对象

    /// <summary>
    /// 面色
    /// </summary>
    public class Complexion : Enumeration
    {
        public static Complexion Normal = new(1, "正常");
        public static Complexion Pale = new(2, "苍白");
        public static Complexion Flushed = new(3, "潮红");
        public static Complexion Yellow = new(4, "萎黄");
        public static Complexion Dark = new(5, "晦暗");
        public static Complexion Bluish = new(6, "青紫");

        public Complexion(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 神态
    /// </summary>
    public class Spirit : Enumeration
    {
        public static Spirit Normal = new(1, "神志清楚");
        public static Spirit Agitated = new(2, "烦躁不安");
        public static Spirit Depressed = new(3, "神情抑郁");
        public static Spirit Tired = new(4, "神疲乏力");
        public static Spirit Confused = new(5, "神志不清");

        public Spirit(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 体型
    /// </summary>
    public class BodyShape : Enumeration
    {
        public static BodyShape Normal = new(1, "正常");
        public static BodyShape Thin = new(2, "消瘦");
        public static BodyShape Obese = new(3, "肥胖");
        public static BodyShape Edema = new(4, "浮肿");
        public static BodyShape Strong = new(5, "强壮");

        public BodyShape(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 舌象
    /// </summary>
    public class TongueCondition : ValueObject
    {
        public TongueBody Body { get; private set; }
        public TongueCoating Coating { get; private set; }
        public string Details { get; private set; }

        protected TongueCondition() { }

        public TongueCondition(TongueBody body, TongueCoating coating, string details = null)
        {
            Body = body;
            Coating = coating;
            Details = details;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Body;
            yield return Coating;
            yield return Details;
        }
    }

    /// <summary>
    /// 舌质
    /// </summary>
    public class TongueBody : Enumeration
    {
        public static TongueBody Normal = new(1, "淡红");
        public static TongueBody Pale = new(2, "淡白");
        public static TongueBody Red = new(3, "红");
        public static TongueBody Crimson = new(4, "绛");
        public static TongueBody Purple = new(5, "紫暗");
        public static TongueBody Fat = new(6, "胖大");
        public static TongueBody Thin = new(7, "瘦薄");

        public TongueBody(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 舌苔
    /// </summary>
    public class TongueCoating : Enumeration
    {
        public static TongueCoating Thin = new(1, "薄白");
        public static TongueCoating ThickWhite = new(2, "厚白");
        public static TongueCoating Yellow = new(3, "黄");
        public static TongueCoating ThickYellow = new(4, "黄厚");
        public static TongueCoating Greasy = new(5, "腻");
        public static TongueCoating Dry = new(6, "燥");
        public static TongueCoating Peeled = new(7, "剥脱");
        public static TongueCoating None = new(8, "无苔");

        public TongueCoating(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 闻诊相关值对象

    /// <summary>
    /// 声音
    /// </summary>
    public class Voice : Enumeration
    {
        public static Voice Normal = new(1, "正常");
        public static Voice Weak = new(2, "声低微弱");
        public static Voice Loud = new(3, "声高亢");
        public static Voice Hoarse = new(4, "声音嘶哑");
        public static Voice Mute = new(5, "失音");

        public Voice(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 呼吸
    /// </summary>
    public class Breathing : Enumeration
    {
        public static Breathing Normal = new(1, "正常");
        public static Breathing Rapid = new(2, "呼吸急促");
        public static Breathing Slow = new(3, "呼吸缓慢");
        public static Breathing Difficult = new(4, "呼吸困难");
        public static Breathing Wheezing = new(5, "喘息");

        public Breathing(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 咳嗽
    /// </summary>
    public class Cough : Enumeration
    {
        public static Cough None = new(1, "无");
        public static Cough Dry = new(2, "干咳");
        public static Cough Productive = new(3, "有痰");
        public static Cough Frequent = new(4, "频繁");
        public static Cough Night = new(5, "夜间咳嗽");

        public Cough(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 气味
    /// </summary>
    public class Odor : Enumeration
    {
        public static Odor Normal = new(1, "正常");
        public static Odor Foul = new(2, "口臭");
        public static Odor Sour = new(3, "酸臭");
        public static Odor Fishy = new(4, "腥臭");
        public static Odor Sweet = new(5, "甜味");

        public Odor(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 问诊相关值对象

    /// <summary>
    /// 食欲状况
    /// </summary>
    public class AppetiteCondition : Enumeration
    {
        public static AppetiteCondition Normal = new(1, "正常");
        public static AppetiteCondition Poor = new(2, "食欲不振");
        public static AppetiteCondition Excessive = new(3, "食欲亢进");
        public static AppetiteCondition NoAppetite = new(4, "厌食");
        public static AppetiteCondition Nausea = new(5, "恶心");

        public AppetiteCondition(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 睡眠状况
    /// </summary>
    public class SleepCondition : Enumeration
    {
        public static SleepCondition Normal = new(1, "正常");
        public static SleepCondition Insomnia = new(2, "失眠");
        public static SleepCondition DreamDisturbed = new(3, "多梦");
        public static SleepCondition Drowsy = new(4, "嗜睡");
        public static SleepCondition LightSleep = new(5, "浅眠");

        public SleepCondition(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 大便状况
    /// </summary>
    public class BowelCondition : Enumeration
    {
        public static BowelCondition Normal = new(1, "正常");
        public static BowelCondition Constipation = new(2, "便秘");
        public static BowelCondition Diarrhea = new(3, "腹泻");
        public static BowelCondition Loose = new(4, "便溏");
        public static BowelCondition Dry = new(5, "便干");

        public BowelCondition(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 小便状况
    /// </summary>
    public class UrinationCondition : Enumeration
    {
        public static UrinationCondition Normal = new(1, "正常");
        public static UrinationCondition Frequent = new(2, "尿频");
        public static UrinationCondition Urgent = new(3, "尿急");
        public static UrinationCondition Painful = new(4, "尿痛");
        public static UrinationCondition Yellow = new(5, "小便黄");
        public static UrinationCondition Clear = new(6, "小便清长");

        public UrinationCondition(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 切诊相关值对象

    /// <summary>
    /// 脉象
    /// </summary>
    public class PulseCondition : ValueObject
    {
        public PulseRate Rate { get; private set; }
        public PulseStrength Strength { get; private set; }
        public PulseDepth Depth { get; private set; }
        public PulseRhythm Rhythm { get; private set; }
        public string Details { get; private set; }

        protected PulseCondition() { }

        public PulseCondition(
            PulseRate rate,
            PulseStrength strength,
            PulseDepth depth,
            PulseRhythm rhythm,
            string details = null)
        {
            Rate = rate;
            Strength = strength;
            Depth = depth;
            Rhythm = rhythm;
            Details = details;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Rate;
            yield return Strength;
            yield return Depth;
            yield return Rhythm;
            yield return Details;
        }

        public override string ToString()
        {
            var description = $"{Depth.Name}{Strength.Name}{Rate.Name}{Rhythm.Name}脉";
            if (!string.IsNullOrEmpty(Details))
                description += $"，{Details}";
            return description;
        }
    }

    /// <summary>
    /// 脉率
    /// </summary>
    public class PulseRate : Enumeration
    {
        public static PulseRate Normal = new(1, "平");
        public static PulseRate Rapid = new(2, "数");
        public static PulseRate Slow = new(3, "迟");
        public static PulseRate Moderate = new(4, "缓");

        public PulseRate(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 脉力
    /// </summary>
    public class PulseStrength : Enumeration
    {
        public static PulseStrength Normal = new(1, "有力");
        public static PulseStrength Weak = new(2, "无力");
        public static PulseStrength Strong = new(3, "洪");
        public static PulseStrength Thready = new(4, "细");
        public static PulseStrength Full = new(5, "实");
        public static PulseStrength Empty = new(6, "虚");

        public PulseStrength(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 脉位
    /// </summary>
    public class PulseDepth : Enumeration
    {
        public static PulseDepth Normal = new(1, "");
        public static PulseDepth Floating = new(2, "浮");
        public static PulseDepth Deep = new(3, "沉");
        public static PulseDepth Middle = new(4, "中");

        public PulseDepth(int id, string name) : base(id, name) { }
    }

    /// <summary>
    /// 脉律
    /// </summary>
    public class PulseRhythm : Enumeration
    {
        public static PulseRhythm Regular = new(1, "");
        public static PulseRhythm Irregular = new(2, "结代");
        public static PulseRhythm Intermittent = new(3, "促");
        public static PulseRhythm Knotted = new(4, "结");

        public PulseRhythm(int id, string name) : base(id, name) { }
    }

    #endregion

    #region 体质类型

    /// <summary>
    /// 中医体质
    /// </summary>
    public class Constitution : Enumeration
    {
        public static Constitution Balanced = new(1, "平和质");
        public static Constitution QiDeficiency = new(2, "气虚质");
        public static Constitution YangDeficiency = new(3, "阳虚质");
        public static Constitution YinDeficiency = new(4, "阴虚质");
        public static Constitution PhlegmDampness = new(5, "痰湿质");
        public static Constitution DampHeat = new(6, "湿热质");
        public static Constitution BloodStasis = new(7, "血瘀质");
        public static Constitution QiStagnation = new(8, "气郁质");
        public static Constitution Special = new(9, "特禀质");

        public Constitution(int id, string name) : base(id, name) { }
    }

    #endregion
}