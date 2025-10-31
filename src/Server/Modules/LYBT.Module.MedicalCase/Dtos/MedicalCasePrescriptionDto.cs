using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// MedicalCase模块专用处方DTO（简化版本）
/// Epic #1736 Phase 5: 区分模块边界，避免与Shared.PrescriptionDetailDto冲突
/// </summary>
public class MedicalCasePrescriptionDto
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
