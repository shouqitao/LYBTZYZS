using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Prescriptions;

/// <summary>
/// 处方列表DTO - 用于列表展示
/// OpenSpec: refactor-dto-simplification - 扁平化设计
/// </summary>
public class PrescriptionListDto
{
    /// <summary>处方ID</summary>
    [DisplayName("处方ID")]
    public Guid Id { get; set; }

    /// <summary>处方编号（格式：RX-YYYYMMDD-NNNN）</summary>
    [DisplayName("处方编号")]
    public string? PrescriptionNumber { get; set; }

    /// <summary>医疗案例ID</summary>
    [DisplayName("医疗案例ID")]
    public Guid MedicalCaseId { get; set; }

    // OpenSpec: simplify-medicalcase-dataflow - Indication已从Prescription移除，打印时从Consultation.TCMDiagnosis获取

    /// <summary>剂数</summary>
    [DisplayName("剂数")]
    public int DosageCount { get; set; }

    /// <summary>总价格</summary>
    [DisplayName("总价格")]
    public decimal TotalPrice { get; set; }

    /// <summary>折扣</summary>
    [DisplayName("折扣")]
    public decimal Discount { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }
}
