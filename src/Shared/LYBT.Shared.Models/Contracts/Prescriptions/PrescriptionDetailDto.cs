using System.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Prescriptions;

/// <summary>
/// 处方详情DTO - 用于详情展示
/// OpenSpec: refactor-dto-simplification - 扁平化设计，包含所有可读字段
/// </summary>
public class PrescriptionDetailDto
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

    /// <summary>剂数</summary>
    [DisplayName("剂数")]
    public int DosageCount { get; set; }

    /// <summary>处方用法（如"每日一剂，水煎服"）</summary>
    [DisplayName("用法")]
    public string? Usage { get; set; }

    /// <summary>医嘱/用药建议</summary>
    [DisplayName("医嘱")]
    public string? Advice { get; set; }

    /// <summary>引用的验方名称列表，逗号分隔</summary>
    [DisplayName("引用验方")]
    public string? ReferencedFormulas { get; set; }

    // Indication已删除，打印时从Consultation.TcmDiagnosis获取
    // FormulaSource已删除，与ReferencedFormulas功能重复
    // Diagnosis已删除，冗余字段

    /// <summary>备注</summary>
    [DisplayName("备注")]
    public string? Remark { get; set; }

    /// <summary>单帖价格（由Service层计算）</summary>
    [DisplayName("单帖价格")]
    public decimal SingleDosePrice { get; set; }

    /// <summary>总价格（由Service层计算）</summary>
    [DisplayName("总价格")]
    public decimal TotalPrice { get; set; }

    /// <summary>总重量（由Service层计算）</summary>
    [DisplayName("总重量")]
    public decimal TotalWeight { get; set; }

    /// <summary>折扣</summary>
    [DisplayName("折扣")]
    public decimal Discount { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>处方项目列表</summary>
    [DisplayName("处方项目")]
    public List<PrescriptionItemDto> Items { get; set; } = new();

    // 运行时计算的警告信息

    /// <summary>重复用药警告（运行时计算）</summary>
    [DisplayName("重复用药警告")]
    public string? DuplicateWarning { get; set; }

    /// <summary>缺药警告（运行时计算）</summary>
    [DisplayName("缺药警告")]
    public string? MissingDrugWarning { get; set; }
}
