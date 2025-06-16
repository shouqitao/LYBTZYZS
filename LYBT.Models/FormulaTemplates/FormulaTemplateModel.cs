using LYBT.Models;

/// <summary>
/// 经验方模板主表实体
/// </summary>
public class FormulaTemplateModel {
    /// <summary>
    /// 模板ID（主键）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 药材组成（结构化列表）
    /// </summary>
    public List<HerbItemModel> Herbs { get; set; } = new();

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}