using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula;

/// <summary>
/// 验方列表DTO - 扁平化设计
/// OpenSpec: refactor-dto-simplification - 用于列表视图展示
/// </summary>
public class FormulaListDto
{
    /// <summary>验方ID</summary>
    [DisplayName("验方ID")]
    public Guid Id { get; set; }

    /// <summary>验方名称</summary>
    [DisplayName("验方名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>功效</summary>
    [DisplayName("功效")]
    public string? Effect { get; set; }

    /// <summary>主治</summary>
    [DisplayName("主治")]
    public string? Indications { get; set; }

    /// <summary>分类</summary>
    [DisplayName("分类")]
    public string? Category { get; set; }

    /// <summary>是否共享</summary>
    [DisplayName("是否共享")]
    public bool IsShared { get; set; }

    /// <summary>验证状态</summary>
    [DisplayName("验证状态")]
    public FormulaValidationStatus ValidationStatus { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>药材数量（由Service计算）</summary>
    [DisplayName("药材数量")]
    public int HerbCount { get; set; }

    /// <summary>总价格（由Service计算）</summary>
    [DisplayName("总价格")]
    public decimal TotalPrice { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
}
