namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 药材基本信息DTO - 用于跨模块查询
/// 供Formula模块验证和匹配药材使用
/// </summary>
public class HerbBasicDto
{
    /// <summary>
    /// 药材ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 药材名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 拼音
    /// </summary>
    public string? Pinyin { get; set; }

    /// <summary>
    /// 药材类别
    /// </summary>
    public string? Category { get; set; }
}
