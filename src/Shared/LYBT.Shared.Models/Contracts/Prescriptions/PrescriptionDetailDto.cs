using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

    /// <summary>主治</summary>
    [DisplayName("主治")]
    public string? Indication { get; set; }

    /// <summary>诊断</summary>
    [DisplayName("诊断")]
    public string? Diagnosis { get; set; }

    /// <summary>剂数</summary>
    [DisplayName("剂数")]
    public int DosageCount { get; set; }

    /// <summary>用法</summary>
    [DisplayName("用法")]
    public string? Usage { get; set; }

    /// <summary>医嘱/用药建议</summary>
    [DisplayName("医嘱")]
    public string? Advice { get; set; }

    /// <summary>验方来源</summary>
    [DisplayName("验方来源")]
    public string? FormulaSource { get; set; }

    /// <summary>引用的验方名称列表，逗号分隔</summary>
    [DisplayName("引用验方")]
    public string? ReferencedFormulas { get; set; }

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
    public List<PrescriptionItemDetailDto> Items { get; set; } = new();

    // 运行时计算的警告信息

    /// <summary>重复用药警告（运行时计算）</summary>
    [DisplayName("重复用药警告")]
    public string? DuplicateWarning { get; set; }

    /// <summary>缺药警告（运行时计算）</summary>
    [DisplayName("缺药警告")]
    public string? MissingDrugWarning { get; set; }
}

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
