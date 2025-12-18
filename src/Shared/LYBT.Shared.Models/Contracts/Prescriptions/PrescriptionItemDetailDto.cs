using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Prescriptions;

/// <summary>
/// 处方项目详情DTO - 用于详情展示
/// OpenSpec: refactor-dto-simplification - 扁平化设计
/// </summary>
public class PrescriptionItemDetailDto
{
    /// <summary>项目ID</summary>
    [DisplayName("项目ID")]
    public Guid Id { get; set; }

    /// <summary>中药材ID</summary>
    [DisplayName("中药材ID")]
    public Guid HerbId { get; set; }

    /// <summary>中药材名称</summary>
    [DisplayName("中药材名称")]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>单位</summary>
    [DisplayName("单位")]
    public string Unit { get; set; } = string.Empty;

    /// <summary>单价</summary>
    [DisplayName("单价")]
    public decimal UnitPrice { get; set; }

    /// <summary>剂量</summary>
    [DisplayName("剂量")]
    public int Dosage { get; set; }

    /// <summary>总价</summary>
    [DisplayName("总价")]
    public decimal TotalPrice { get; set; }

    /// <summary>总重量</summary>
    [DisplayName("总重量")]
    public decimal TotalWeight { get; set; }

    /// <summary>小计金额</summary>
    [DisplayName("小计金额")]
    public decimal Subtotal { get; set; }

    /// <summary>用法说明</summary>
    [DisplayName("用法说明")]
    public string? Usage { get; set; }

    /// <summary>煎法</summary>
    [DisplayName("煎法")]
    public DecocteMethod DecocteMethod { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    public string? Remark { get; set; }
}
