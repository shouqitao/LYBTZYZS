namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds HerbInputDto payloads for API calls.
/// </summary>
public sealed class HerbBuilder
{
    private string _name = $"测试药材_{Guid.NewGuid():N}"[..10];
    private string? _pinYinCode = "CSYC";
    private string? _category = "清热解毒";
    private string _unit = "克";
    private decimal _price = 10.0m;
    private decimal? _costPrice = 5.0m;
    private string? _effect;

    public static HerbBuilder Default() => new();

    public HerbBuilder WithName(string name) { _name = name; return this; }
    public HerbBuilder WithPinYinCode(string code) { _pinYinCode = code; return this; }
    public HerbBuilder WithCategory(string cat) { _category = cat; return this; }
    public HerbBuilder WithUnit(string unit) { _unit = unit; return this; }
    public HerbBuilder WithPrice(decimal price) { _price = price; return this; }
    public HerbBuilder WithCostPrice(decimal cost) { _costPrice = cost; return this; }
    public HerbBuilder WithEffect(string effect) { _effect = effect; return this; }

    public object Build() => new
    {
        Name = _name,
        PinYinCode = _pinYinCode,
        Category = _category,
        Unit = _unit,
        Price = _price,
        CostPrice = _costPrice,
        Effect = _effect
    };
}
