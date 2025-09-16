using Bogus;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formula.Tests.Base;

/// <summary>
/// 验方测试数据生成器
/// </summary>
public static class FormulaTestDataGenerator
{
    /// <summary>
    /// 验方数据生成器
    /// </summary>
    public static Faker<LYBT.Entities.Formula.Formula> FormulaGenerator => 
        new Faker<LYBT.Entities.Formula.Formula>("zh_CN")
            .RuleFor(f => f.Id, f => Guid.NewGuid())
            .RuleFor(f => f.Name, f => f.PickRandom(
                "桂枝汤", "麻黄汤", "小青龙汤", "大青龙汤", "葛根汤",
                "白虎汤", "承气汤", "桃核承气汤", "调胃承气汤", "大承气汤",
                "小承气汤", "四逆汤", "真武汤", "理中汤", "干姜附子汤",
                "六君子汤", "四君子汤", "补中益气汤", "当归补血汤", "十全大补汤",
                "逍遥散", "甘麦大枣汤", "安神定志丸", "天王补心丹", "朱砂安神丸"))
            .RuleFor(f => f.Description, f => f.Lorem.Paragraph(2, 4))
            .RuleFor(f => f.Classification, f => f.PickRandom(
                "解表剂", "清热剂", "泻下剂", "和解剂", "温里剂",
                "表里双解剂", "补益剂", "安神剂", "开窍剂", "固涩剂",
                "理气剂", "理血剂", "治风剂", "治燥剂", "祛湿剂",
                "祛痰剂", "消导剂", "驱虫剂", "涌吐剂", "外用剂"))
            .RuleFor(f => f.Composition, f => GenerateComposition(f))
            .RuleFor(f => f.Usage, f => f.PickRandom(
                "水煎服，日一剂，分二次温服",
                "研末，每服3g，日三次，温水送服",
                "蜜丸，每服9g，日二次，温开水送服",
                "散剂，每服6g，日二次，餐后温服",
                "汤剂，每日一剂，水煎分服"))
            .RuleFor(f => f.Functions, f => f.PickRandom(
                "发汗解肌，温经通阳", "辛凉解表，清热生津", "温中散寒，补气健脾",
                "补气养血，调和营卫", "清热泻火，凉血解毒", "理气健脾，燥湿化痰",
                "滋阴清热，养血安神", "温阳化气，利水消肿", "活血化瘀，行气止痛",
                "补肾固精，滋阴潜阳", "疏肝解郁，健脾和胃", "清热解毒，消肿散结"))
            .RuleFor(f => f.Indications, f => f.PickRandom(
                "外感风寒，头痛发热，汗出恶风", "外感风热，发热头痛，咽痛口渴",
                "脾胃虚寒，腹痛泄泻，四肢厥冷", "气血两虚，面色萎黄，倦怠乏力",
                "热病伤津，壮热烦渴，脉洪大", "湿痰内阻，胸脘痞闷，恶心呕吐",
                "阴虚火旺，心烦失眠，盗汗遗精", "肾阳虚衰，小便不利，肢体浮肿",
                "血瘀气滞，胸胁刺痛，痛有定处", "肝肾阴虚，头晕耳鸣，腰膝酸软",
                "肝郁脾虚，胸胁胀痛，食少便溏", "热毒内盛，痈疽疮疡，红肿热痛"))
            .RuleFor(f => f.Source, f => f.PickRandom(
                "《伤寒论》", "《金匮要略》", "《太平惠民和剂局方》", "《备急千金要方》",
                "《外台秘要》", "《圣济总录》", "《普济方》", "《医方考》",
                "《成方便读》", "《温病条辨》", "《医学心悟》", "《临证指南医案》"))
            .RuleFor(f => f.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(f => f.CreateTime, f => f.Date.Recent(365))
            .RuleFor(f => f.UpdateTime, f => f.Date.Recent(30))
            .FinishWith((f, formula) =>
            {
                // 确保更新时间不早于创建时间
                if (formula.UpdateTime < formula.CreateTime)
                {
                    formula.UpdateTime = formula.CreateTime.AddHours(1);
                }
            });

    /// <summary>
    /// 生成组成方剂
    /// </summary>
    private static string GenerateComposition(Faker faker)
    {
        var herbs = new[]
        {
            "桂枝9g", "白芍9g", "生姜9g", "大枣12枚", "甘草6g",
            "麻黄9g", "杏仁9g", "桂枝6g", "甘草3g",
            "人参9g", "白术9g", "茯苓9g", "甘草6g",
            "当归10g", "川芎8g", "白芍10g", "生地黄12g",
            "黄芪15g", "人参9g", "白术9g", "当归6g", "陈皮6g", "升麻3g", "柴胡3g", "甘草6g",
            "石膏30g", "知母9g", "甘草6g", "粳米15g",
            "大黄12g", "芒硝9g", "枳实12g", "厚朴15g",
            "附子9g", "干姜6g", "甘草6g",
            "茯苓12g", "白术9g", "生姜12g", "附子9g"
        };

        var selectedHerbs = faker.PickRandom(herbs, faker.Random.Int(5, 12));
        return string.Join("，", selectedHerbs);
    }

    /// <summary>
    /// 创建测试验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateTestFormula(
        string? name = null,
        string? classification = null,
        CommonStatus status = CommonStatus.Enabled)
    {
        var formula = FormulaGenerator.Generate();
        
        if (!string.IsNullOrEmpty(name))
            formula.Name = name;
            
        if (!string.IsNullOrEmpty(classification))
            formula.Classification = classification;
            
        formula.Status = status;
        
        return formula;
    }

    /// <summary>
    /// 批量创建测试验方
    /// </summary>
    public static List<LYBT.Entities.Formula.Formula> CreateTestFormulas(int count)
    {
        return FormulaGenerator.Generate(count);
    }

    /// <summary>
    /// 创建指定状态的验方
    /// </summary>
    public static List<LYBT.Entities.Formula.Formula> CreateTestFormulasWithStatus(
        CommonStatus status, 
        int count)
    {
        var generator = FormulaGenerator.RuleFor(f => f.Status, status);
        return generator.Generate(count);
    }

    /// <summary>
    /// 创建指定分类的验方
    /// </summary>
    public static List<LYBT.Entities.Formula.Formula> CreateTestFormulasWithClassification(
        string classification, 
        int count)
    {
        var generator = FormulaGenerator.RuleFor(f => f.Classification, classification);
        return generator.Generate(count);
    }

    /// <summary>
    /// 创建经典验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateClassicFormula()
    {
        var formula = FormulaGenerator.Generate();
        
        // 经典验方具体配置
        formula.Name = "桂枝汤";
        formula.Description = "调和营卫，解肌发表。主治太阳病，头痛发热，汗出恶风，鼻鸣干呕者。";
        formula.Classification = "解表剂";
        formula.Composition = "桂枝9g，白芍9g，生姜9g，大枣12枚，甘草6g";
        formula.Usage = "水煎温服，取微汗，不可令如水流漓";
        formula.Functions = "发汗解肌，温经通阳";
        formula.Indications = "外感风寒，营卫不和证";
        formula.Source = "《伤寒论》";
        formula.Status = CommonStatus.Enabled;
        
        return formula;
    }

    /// <summary>
    /// 创建复杂验方（包含完整信息）
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateComplexFormula()
    {
        var formula = FormulaGenerator.Generate();
        
        // 确保所有字段都有值
        formula.Name = "补中益气汤";
        formula.Description = "补中益气，升阳举陷。脾胃气虚，清阳不升，中气下陷之证。饮食减少，体倦肢软，少气懒言，面色㿠白，大便稀溏，脉虚缓等。气虚发热证。身热有汗，渴喜热饮，气短乏力，脉虚大无力等。";
        formula.Classification = "补益剂";
        formula.Composition = "黄芪15g，人参9g，白术9g，当归6g，陈皮6g，升麻3g，柴胡3g，甘草6g";
        formula.Usage = "水煎服，日一剂，分二次温服";
        formula.Functions = "补中益气，升阳举陷";
        formula.Indications = "脾胃气虚，清阳不升证；气虚发热证；脾虚下陷证";
        formula.Source = "《脾胃论》";
        formula.Status = CommonStatus.Enabled;
        
        return formula;
    }

    /// <summary>
    /// 创建温里剂验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateWarmingFormula()
    {
        var formula = CreateTestFormula(classification: "温里剂");
        formula.Name = "理中汤";
        formula.Composition = "人参9g，白术9g，干姜9g，甘草6g";
        formula.Functions = "温中散寒，补气健脾";
        formula.Indications = "中焦虚寒证";
        return formula;
    }

    /// <summary>
    /// 创建清热剂验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateClearingHeatFormula()
    {
        var formula = CreateTestFormula(classification: "清热剂");
        formula.Name = "白虎汤";
        formula.Composition = "石膏30g，知母9g，甘草6g，粳米15g";
        formula.Functions = "清热泻火，除烦止渴";
        formula.Indications = "气分热盛证";
        return formula;
    }

    /// <summary>
    /// 创建补益剂验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateTonifyingFormula()
    {
        var formula = CreateTestFormula(classification: "补益剂");
        formula.Name = "四君子汤";
        formula.Composition = "人参9g，白术9g，茯苓9g，甘草6g";
        formula.Functions = "益气健脾";
        formula.Indications = "脾胃气虚证";
        return formula;
    }

    /// <summary>
    /// 创建禁用验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateDisabledFormula()
    {
        return CreateTestFormula(status: CommonStatus.Disabled);
    }

    /// <summary>
    /// 创建启用验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateEnabledFormula()
    {
        return CreateTestFormula(status: CommonStatus.Enabled);
    }

    /// <summary>
    /// 创建不同来源的验方集合
    /// </summary>
    public static List<LYBT.Entities.Formula.Formula> CreateFormulasFromDifferentSources()
    {
        var sources = new[] { "《伤寒论》", "《金匮要略》", "《太平惠民和剂局方》", "《温病条辨》" };
        var formulas = new List<LYBT.Entities.Formula.Formula>();

        foreach (var source in sources)
        {
            var formula = CreateTestFormula();
            formula.Source = source;
            formulas.Add(formula);
        }

        return formulas;
    }

    /// <summary>
    /// 创建所有分类的验方
    /// </summary>
    public static List<LYBT.Entities.Formula.Formula> CreateFormulasOfAllClassifications()
    {
        var classifications = new[]
        {
            "解表剂", "清热剂", "泻下剂", "和解剂", "温里剂",
            "表里双解剂", "补益剂", "安神剂", "开窍剂", "固涩剂"
        };

        var formulas = new List<LYBT.Entities.Formula.Formula>();

        foreach (var classification in classifications)
        {
            var formula = CreateTestFormula(classification: classification);
            formulas.Add(formula);
        }

        return formulas;
    }
}