using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// 处方药品项DTO（嵌套对象）
/// Epic #1612 Task 1.3
/// </summary>
public class PrescriptionItemDto
{
    /// <summary>
    /// 药品ID
    /// </summary>
    [Required(ErrorMessage = "药品ID不能为空")]
    public Guid HerbId { get; set; }

    /// <summary>
    /// 数量（克）
    /// </summary>
    [Required(ErrorMessage = "数量不能为空")]
    [Range(0.1, 1000, ErrorMessage = "数量必须在0.1-1000克之间")]
    public decimal Quantity { get; set; }
}
