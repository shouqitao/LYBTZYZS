using System;
using System.Collections.Generic;
using Bogus;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formula.Tests.Base;

/// <summary>
/// 验方测试数据生成器
/// </summary>
public static class FormulaTestDataGenerator
{
    private static readonly string[] FormulaNames = {
        "桂枝汤", "麻黄汤", "小青龙汤", "大青龙汤", "葛根汤",
        "白虎汤", "承气汤", "桃核承气汤", "调胃承气汤", "大承气汤",
        "小承气汤", "四逆汤", "真武汤", "理中汤", "干姜附子汤",
        "六君子汤", "四君子汤", "补中益气汤", "当归补血汤", "十全大补汤"
    };

    /// <summary>
    /// 验方数据生成器
    /// </summary>
    public static Faker<LYBT.Entities.Formula.Formula> FormulaGenerator => 
        new Faker<LYBT.Entities.Formula.Formula>("zh_CN")
            .RuleFor(f => f.Id, f => Guid.NewGuid())
            .RuleFor(f => f.Name, f => f.PickRandom(FormulaNames))
            .RuleFor(f => f.Effect, f => f.Lorem.Sentence())
            .RuleFor(f => f.Usage, f => f.Lorem.Sentence())
            .RuleFor(f => f.Remark, f => f.Lorem.Sentence())
            .RuleFor(f => f.Property, f => f.PickRandom("温性", "寒性", "平性", "热性", "凉性"))
            .RuleFor(f => f.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(f => f.IsShared, f => f.Random.Bool());

    /// <summary>
    /// 创建测试验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateTestFormula(
        string? name = null,
        string? effect = null,
        CommonStatus status = CommonStatus.Enabled)
    {
        var formula = FormulaGenerator.Generate();
        
        if (!string.IsNullOrEmpty(name))
            formula.Name = name;
            
        if (!string.IsNullOrEmpty(effect))
            formula.Effect = effect;
            
        formula.Status = status;
        
        return formula;
    }

    /// <summary>
    /// 批量创建测试验方
    /// </summary>
    public static List<LYBT.Entities.Formula.Formula> CreateTestFormulas(int count, CommonStatus? status = null)
    {
        var generator = FormulaGenerator;

        if (status.HasValue)
            generator = generator.RuleFor(f => f.Status, status.Value);

        // 确保名称唯一性
        var formulas = generator.Generate(count);
        for (int i = 0; i < formulas.Count; i++)
        {
            formulas[i].Name = $"{formulas[i].Name}_{i + 1}";
        }

        return formulas;
    }

    /// <summary>
    /// 创建启用的测试验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateEnabledFormula()
    {
        return CreateTestFormula(status: CommonStatus.Enabled);
    }

    /// <summary>
    /// 创建禁用的测试验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateDisabledFormula()
    {
        return CreateTestFormula(status: CommonStatus.Disabled);
    }

    /// <summary>
    /// 创建具有特定名称的验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateFormulaWithName(string name)
    {
        return CreateTestFormula(name: name);
    }

    /// <summary>
    /// 创建共享验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreateSharedFormula()
    {
        var formula = CreateTestFormula();
        formula.IsShared = true;
        return formula;
    }

    /// <summary>
    /// 创建私有验方
    /// </summary>
    public static LYBT.Entities.Formula.Formula CreatePrivateFormula()
    {
        var formula = CreateTestFormula();
        formula.IsShared = false;
        return formula;
    }
}
