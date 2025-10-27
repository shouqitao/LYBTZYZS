using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.MedicalCase.Dtos;

/// <summary>
/// 标记是否开处方请求DTO
/// Epic #1612 Task 1.3
/// 支持动态流程控制,用户可选择是否开处方
/// </summary>
public class SetPrescriptionFlagRequest
{
    /// <summary>
    /// 是否需要开处方
    /// </summary>
    [Required]
    public bool NeedsPrescription { get; set; }
}
