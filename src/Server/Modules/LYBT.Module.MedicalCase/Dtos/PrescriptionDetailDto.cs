namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// 处方信息响应DTO（完整字段）
/// Epic #1612 Task 1.3
/// </summary>
public class PrescriptionDetailDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// 处方编号
    /// </summary>
    public string? PrescriptionNumber { get; set; }

    /// <summary>
    /// 主治
    /// </summary>
    public string? Indication { get; set; }

    /// <summary>
    /// 剂数
    /// </summary>
    public int DosageCount { get; set; }

    /// <summary>
    /// 用法
    /// </summary>
    public string? Usage { get; set; }

    /// <summary>
    /// 折扣
    /// </summary>
    public decimal Discount { get; set; }

    /// <summary>
    /// 医嘱
    /// </summary>
    public string? Advice { get; set; }

    /// <summary>
    /// 验方来源
    /// </summary>
    public string? FormulaSource { get; set; }

    /// <summary>
    /// 引用验方
    /// </summary>
    public string? ReferencedFormulas { get; set; }

    /// <summary>
    /// 处方药品列表
    /// </summary>
    public List<PrescriptionItemDto> Items { get; set; } = new();

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 总价格（计算字段）
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
