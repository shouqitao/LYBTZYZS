using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Herbs;

/// <summary>
/// 药材列表DTO - 扁平化设计
/// OpenSpec: refactor-dto-simplification - 用于列表视图展示
/// </summary>
public class HerbListDto
{
    /// <summary>药材ID</summary>
    [DisplayName("药材ID")]
    public Guid Id { get; set; }

    /// <summary>药材名称</summary>
    [DisplayName("药材名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>拼音码</summary>
    [DisplayName("拼音码")]
    public string? PinYinCode { get; set; }

    /// <summary>分类</summary>
    [DisplayName("分类")]
    public string? Category { get; set; }

    /// <summary>产地</summary>
    [DisplayName("产地")]
    public string? Origin { get; set; }

    /// <summary>规格</summary>
    [DisplayName("规格")]
    public string? Spec { get; set; }

    /// <summary>单位</summary>
    [DisplayName("单位")]
    public string Unit { get; set; } = "克";

    /// <summary>单价</summary>
    [DisplayName("单价")]
    public decimal Price { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
}
