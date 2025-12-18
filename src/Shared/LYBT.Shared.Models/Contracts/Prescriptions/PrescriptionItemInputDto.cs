using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Prescriptions;

/// <summary>
/// 处方项输入DTO - 统一创建和编辑
/// OpenSpec: refactor-dto-simplification - 扁平化设计
/// </summary>
public class PrescriptionItemInputDto
{
    /// <summary>项ID（更新时可填，创建时为null）</summary>
    [DisplayName("项ID")]
    public Guid? Id { get; set; }

    /// <summary>草药ID</summary>
    [Required(ErrorMessage = "草药ID不能为空")]
    [DisplayName("草药ID")]
    public Guid HerbId { get; set; }

    /// <summary>草药名称</summary>
    [StringLength(100, ErrorMessage = "草药名称长度不能超过100个字符")]
    [DisplayName("草药名称")]
    public string? HerbName { get; set; }

    /// <summary>单位</summary>
    [Required(ErrorMessage = "单位不能为空")]
    [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
    [DisplayName("单位")]
    public string Unit { get; set; } = string.Empty;

    /// <summary>剂量（整数克）</summary>
    [Range(1, 500, ErrorMessage = "剂量必须在1-500之间")]
    [DisplayName("剂量")]
    public int Dosage { get; set; }

    /// <summary>单价</summary>
    [Range(0, 10000, ErrorMessage = "单价必须在0-10000之间")]
    [DisplayName("单价")]
    public decimal UnitPrice { get; set; }

    /// <summary>小计金额</summary>
    [Range(0, double.MaxValue, ErrorMessage = "小计金额必须大于等于0")]
    [DisplayName("小计金额")]
    public decimal Subtotal { get; set; }

    /// <summary>用法说明</summary>
    [StringLength(200, ErrorMessage = "用法说明长度不能超过200个字符")]
    [DisplayName("用法说明")]
    public string? Usage { get; set; }

    /// <summary>煎法</summary>
    [DisplayName("煎法")]
    public DecocteMethod DecocteMethod { get; set; } = DecocteMethod.Default;

    /// <summary>备注</summary>
    [StringLength(200, ErrorMessage = "备注长度不能超过200个字符")]
    [DisplayName("备注")]
    public string? Remark { get; set; }
}
