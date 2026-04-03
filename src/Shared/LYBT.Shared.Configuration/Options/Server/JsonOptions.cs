namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// JSON 序列化配置
/// </summary>
public sealed class JsonOptions
{
    public const string SectionName = "Json";

    /// <summary>
    /// 是否使用 UnsafeRelaxedJsonEscaping
    /// </summary>
    public bool UnsafeRelaxedEscaping { get; set; } = false;

    /// <summary>
    /// 属性命名策略
    /// 可选值: CamelCase, SnakeCaseLower, SnakeCaseUpper, KebabCaseLower, KebabCaseUpper, null(PascalCase)
    /// </summary>
    public string PropertyNamingPolicy { get; set; } = "CamelCase";

    /// <summary>
    /// 是否忽略只读属性
    /// </summary>
    public bool IgnoreReadOnlyProperties { get; set; } = false;

    /// <summary>
    /// 是否允许尾随逗号
    /// </summary>
    public bool AllowTrailingCommas { get; set; } = false;
}
