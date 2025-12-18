using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Herbs;

/// <summary>
/// 药材详情DTO - 扁平化设计
/// OpenSpec: refactor-dto-simplification - 用于详情视图展示
/// </summary>
public class HerbDetailDtoNew
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

    /// <summary>性味</summary>
    [DisplayName("性味")]
    public string? Properties { get; set; }

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

    /// <summary>成本价</summary>
    [DisplayName("成本价")]
    public decimal? CostPrice { get; set; }

    /// <summary>功效说明</summary>
    [DisplayName("功效说明")]
    public string? Effect { get; set; }

    /// <summary>用法</summary>
    [DisplayName("用法")]
    public string? Usage { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    public string? Remark { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
}
