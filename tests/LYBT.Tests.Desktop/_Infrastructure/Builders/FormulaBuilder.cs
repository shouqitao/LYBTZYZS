using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Tests.Desktop._Infrastructure.Builders;

/// <summary>
/// 验方数据构建器
/// 使用 Fluent API 模式创建测试用的验方数据
/// </summary>
public class FormulaBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "测试验方";
    private string _effect = "测试功效";
    private string? _description;
    private string _usage = "水煎服，每日一剂";
    private string? _property;
    private string? _category;
    private bool _isShared = false;
    private string? _instructions;
    private string? _indications;
    private string? _contraindications;
    private string? _preparation;
    private string? _remark;
    private List<FormulaHerbItemInputDto> _herbs = new();

    public static FormulaBuilder Create() => new();

    public FormulaBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public FormulaBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public FormulaBuilder WithEffect(string effect)
    {
        _effect = effect;
        return this;
    }

    public FormulaBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public FormulaBuilder WithUsage(string usage)
    {
        _usage = usage;
        return this;
    }

    public FormulaBuilder WithProperty(string? property)
    {
        _property = property;
        return this;
    }

    public FormulaBuilder WithCategory(string? category)
    {
        _category = category;
        return this;
    }

    public FormulaBuilder WithIsShared(bool isShared)
    {
        _isShared = isShared;
        return this;
    }

    public FormulaBuilder WithInstructions(string? instructions)
    {
        _instructions = instructions;
        return this;
    }

    public FormulaBuilder WithIndications(string? indications)
    {
        _indications = indications;
        return this;
    }

    public FormulaBuilder WithContraindications(string? contraindications)
    {
        _contraindications = contraindications;
        return this;
    }

    public FormulaBuilder WithPreparation(string? preparation)
    {
        _preparation = preparation;
        return this;
    }

    public FormulaBuilder WithRemark(string? remark)
    {
        _remark = remark;
        return this;
    }

    public FormulaBuilder WithHerbs(List<FormulaHerbItemInputDto> herbs)
    {
        _herbs = herbs;
        return this;
    }

    public FormulaBuilder AddHerb(FormulaHerbItemInputDto herb)
    {
        _herbs.Add(herb);
        return this;
    }

    public FormulaBuilder AddHerb(Guid herbId, string herbName, int dosage, string unit)
    {
        _herbs.Add(new FormulaHerbItemInputDto
        {
            HerbId = herbId,
            HerbName = herbName,
            Dosage = dosage,
            Unit = unit
        });
        return this;
    }

    /// <summary>
    /// 构建 FormulaInputDto (用于创建/更新)
    /// </summary>
    public FormulaInputDto BuildInputDto() => new()
    {
        Id = _id,
        Name = _name,
        Effect = _effect,
        Description = _description,
        Usage = _usage,
        Property = _property,
        Category = _category,
        IsShared = _isShared,
        Instructions = _instructions,
        Indications = _indications,
        Contraindications = _contraindications,
        Preparation = _preparation,
        Remark = _remark,
        Herbs = _herbs
    };

    /// <summary>
    /// 预置：简单验方（最少字段）
    /// </summary>
    public static FormulaBuilder Simple() => Create()
        .WithName("简单验方")
        .WithEffect("测试")
        .WithUsage("水煎服")
        .WithHerbs(new List<FormulaHerbItemInputDto>());

    /// <summary>
    /// 预置：感冒方剂（常用示例）
    /// </summary>
    public static FormulaBuilder ColdRemedy() => Create()
        .WithName("感冒清热方")
        .WithEffect("清热解毒，疏风散寒")
        .WithDescription("用于风热感冒引起的发热、头痛、咳嗽等症状")
        .WithUsage("水煎服，每日一剂，早晚分服")
        .WithProperty("辛凉解表")
        .WithCategory("解表剂")
        .WithIndications("风热感冒，发热头痛")
        .WithContraindications("风寒感冒者慎用");

    /// <summary>
    /// 预置：共享验方
    /// </summary>
    public static FormulaBuilder Shared() => Create()
        .WithName("共享验方")
        .WithEffect("公共使用")
        .WithUsage("水煎服")
        .WithIsShared(true)
        .WithHerbs(new List<FormulaHerbItemInputDto>());
}
