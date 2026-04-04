using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Tests.Desktop._Infrastructure.Builders;

/// <summary>
/// 药材数据构建器
/// 使用 Fluent API 模式创建测试用的药材数据
/// </summary>
public class HerbBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "测试药材";
    private string? _pinYinCode;
    private string? _category;
    private string? _origin;
    private string? _spec;
    private string _unit = "克";
    private decimal _price = 1.00m;
    private decimal? _costPrice;
    private string? _effect;
    private string? _usage;
    private string? _remark;

    public static HerbBuilder Create() => new();

    public HerbBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public HerbBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public HerbBuilder WithPinYinCode(string? pinYinCode)
    {
        _pinYinCode = pinYinCode;
        return this;
    }

    public HerbBuilder WithCategory(string? category)
    {
        _category = category;
        return this;
    }

    public HerbBuilder WithOrigin(string? origin)
    {
        _origin = origin;
        return this;
    }

    public HerbBuilder WithSpec(string? spec)
    {
        _spec = spec;
        return this;
    }

    public HerbBuilder WithUnit(string unit)
    {
        _unit = unit;
        return this;
    }

    public HerbBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public HerbBuilder WithCostPrice(decimal? costPrice)
    {
        _costPrice = costPrice;
        return this;
    }

    public HerbBuilder WithEffect(string? effect)
    {
        _effect = effect;
        return this;
    }

    public HerbBuilder WithUsage(string? usage)
    {
        _usage = usage;
        return this;
    }

    public HerbBuilder WithRemark(string? remark)
    {
        _remark = remark;
        return this;
    }

    /// <summary>
    /// 构建 HerbInputDto (用于创建/更新)
    /// </summary>
    public HerbInputDto BuildInputDto() => new()
    {
        Id = _id,
        Name = _name,
        PinYinCode = _pinYinCode,
        Category = _category,
        Origin = _origin,
        Spec = _spec,
        Unit = _unit,
        Price = _price,
        CostPrice = _costPrice,
        Effect = _effect,
        Usage = _usage,
        Remark = _remark
    };

    /// <summary>
    /// 预置：常用药材 - 甘草
    /// </summary>
    public static HerbBuilder GanCao() => Create()
        .WithName("甘草")
        .WithPinYinCode("GanCao")
        .WithCategory("补益药")
        .WithOrigin("内蒙古")
        .WithSpec("片")
        .WithUnit("克")
        .WithPrice(0.15m)
        .WithEffect("补脾益气，清热解毒，祛痰止咳，缓急止痛，调和诸药")
        .WithUsage("煎服，2-10g");

    /// <summary>
    /// 预置：常用药材 - 人参
    /// </summary>
    public static HerbBuilder RenShen() => Create()
        .WithName("人参")
        .WithPinYinCode("RenShen")
        .WithCategory("补益药")
        .WithOrigin("吉林")
        .WithSpec("生晒参")
        .WithUnit("克")
        .WithPrice(5.00m)
        .WithEffect("大补元气，复脉固脱，补脾益肺，生津养血，安神益智")
        .WithUsage("煎服，3-9g");

    /// <summary>
    /// 预置：常用药材 - 当归
    /// </summary>
    public static HerbBuilder DangGui() => Create()
        .WithName("当归")
        .WithPinYinCode("DangGui")
        .WithCategory("补益药")
        .WithOrigin("甘肃")
        .WithSpec("片")
        .WithUnit("克")
        .WithPrice(0.30m)
        .WithEffect("补血活血，调经止痛，润肠通便")
        .WithUsage("煎服，6-12g");

    /// <summary>
    /// 预置：贵重药材 - 麝香
    /// </summary>
    public static HerbBuilder SheXiang() => Create()
        .WithName("麝香")
        .WithPinYinCode("SheXiang")
        .WithCategory("开窍药")
        .WithOrigin("西藏")
        .WithSpec("净香")
        .WithUnit("克")
        .WithPrice(500.00m)
        .WithEffect("开窍醒神，活血通经，消肿止痛")
        .WithUsage("入丸散，0.03-0.1g");

    /// <summary>
    /// 预置：简单药材（最少字段）
    /// </summary>
    public static HerbBuilder Simple() => Create()
        .WithName("简单药材")
        .WithUnit("克")
        .WithPrice(1.00m);
}
