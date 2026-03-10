namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds FormulaInputDto payloads for API calls.
/// </summary>
public sealed class FormulaBuilder
{
    private string _name = $"测试验方_{Guid.NewGuid():N}"[..10];
    private string _effect = "清热解毒";
    private string? _description = "测试验方描述";
    private string _usage = "水煎服";
    private readonly List<object> _herbs = [];

    public static FormulaBuilder Default() => new();

    public FormulaBuilder WithName(string name) { _name = name; return this; }
    public FormulaBuilder WithEffect(string effect) { _effect = effect; return this; }
    public FormulaBuilder WithDescription(string desc) { _description = desc; return this; }
    public FormulaBuilder WithUsage(string usage) { _usage = usage; return this; }

    public FormulaBuilder AddHerb(Guid? herbId, string herbName, int dosage,
        string unit = "克")
    {
        _herbs.Add(new
        {
            HerbId = herbId,
            HerbName = herbName,
            Dosage = dosage,
            Unit = unit
        });
        return this;
    }

    public object Build() => new
    {
        Name = _name,
        Effect = _effect,
        Description = _description,
        Usage = _usage,
        Herbs = _herbs
    };
}
