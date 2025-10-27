using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// 创建处方请求DTO（完整字段）
/// Epic #1612 Task 1.3
/// </summary>
public class CreatePrescriptionRequest
{
    /// <summary>
    /// 处方编号（系统自动生成，但支持手动指定）
    /// </summary>
    [MaxLength(50)]
    public string? PrescriptionNumber { get; set; }

    /// <summary>
    /// 主治
    /// </summary>
    [MaxLength(500)]
    public string? Indication { get; set; }

    /// <summary>
    /// 剂数
    /// </summary>
    [Range(1, 100, ErrorMessage = "剂数必须在1-100之间")]
    public int DosageCount { get; set; } = 7;

    /// <summary>
    /// 用法
    /// </summary>
    [MaxLength(500)]
    public string? Usage { get; set; }

    /// <summary>
    /// 折扣（0.0-1.0）
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "折扣必须在0-1之间")]
    public decimal Discount { get; set; } = 1.0m;

    /// <summary>
    /// 医嘱
    /// </summary>
    [MaxLength(500)]
    public string? Advice { get; set; }

    /// <summary>
    /// 验方来源
    /// </summary>
    [MaxLength(200)]
    public string? FormulaSource { get; set; }

    /// <summary>
    /// 引用验方
    /// </summary>
    [MaxLength(500)]
    public string? ReferencedFormulas { get; set; }

    /// <summary>
    /// 处方药品列表
    /// </summary>
    [Required(ErrorMessage = "处方药品列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要一味药")]
    public List<PrescriptionItemDto> Items { get; set; } = new();

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }
}
