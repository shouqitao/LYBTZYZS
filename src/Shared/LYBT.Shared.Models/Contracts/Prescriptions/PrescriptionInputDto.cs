using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Prescriptions;

/// <summary>
/// 处方输入DTO - 统一创建和编辑
/// OpenSpec: refactor-dto-simplification - 扁平化设计，遵循InputDto设计原则
/// </summary>
/// <remarks>
/// InputDto设计原则：
/// - 只含可写字段，排除系统字段（CreatedAt/UpdatedAt）
/// - 排除计算字段（SingleDosePrice/TotalWeight由Service计算）
/// - 排除状态字段（Status通过专用API修改）
/// - Id可空：null=创建，有值=更新
/// </remarks>
public class PrescriptionInputDto
{
    /// <summary>处方ID（更新时必填，创建时为null）</summary>
    [DisplayName("处方ID")]
    public Guid? Id { get; set; }

    /// <summary>医疗案例ID（必填）</summary>
    [Required(ErrorMessage = "医疗案例ID不能为空")]
    [DisplayName("医疗案例ID")]
    public Guid MedicalCaseId { get; set; }

    /// <summary>诊断</summary>
    [StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
    [DisplayName("诊断")]
    public string? Diagnosis { get; set; }

    /// <summary>主治</summary>
    [StringLength(500, ErrorMessage = "主治长度不能超过500个字符")]
    [DisplayName("主治")]
    public string? Indication { get; set; }

    /// <summary>剂数</summary>
    [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
    [DisplayName("剂数")]
    public int DosageCount { get; set; } = 7;

    /// <summary>用法</summary>
    [StringLength(200, ErrorMessage = "用法长度不能超过200个字符")]
    [DisplayName("用法")]
    public string? Usage { get; set; }

    /// <summary>医嘱/用药建议</summary>
    [StringLength(500, ErrorMessage = "用药建议不能超过500个字符")]
    [DisplayName("医嘱")]
    public string? Advice { get; set; }

    /// <summary>验方来源</summary>
    [StringLength(200, ErrorMessage = "验方来源长度不能超过200个字符")]
    [DisplayName("验方来源")]
    public string? FormulaSource { get; set; }

    /// <summary>折扣（0-1之间）</summary>
    [Range(0, 1, ErrorMessage = "折扣必须在0-1之间")]
    [DisplayName("折扣")]
    public decimal Discount { get; set; } = 1.0m;

    /// <summary>总价格</summary>
    [Range(0, double.MaxValue, ErrorMessage = "总价格必须大于等于0")]
    [DisplayName("总价格")]
    public decimal TotalPrice { get; set; }

    /// <summary>备注</summary>
    [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
    [DisplayName("备注")]
    public string? Remark { get; set; }

    /// <summary>处方项目列表</summary>
    [Required(ErrorMessage = "必须包含至少一个处方项目")]
    [DisplayName("处方项目")]
    public List<PrescriptionItemInputDto> Items { get; set; } = new();
}
