using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula;

/// <summary>
/// 验方详情DTO - 扁平化设计
/// OpenSpec: refactor-dto-simplification - 用于详情视图展示
/// </summary>
public class FormulaDetailDtoNew
{
    /// <summary>验方ID</summary>
    [DisplayName("验方ID")]
    public Guid Id { get; set; }

    /// <summary>验方名称</summary>
    [DisplayName("验方名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>功效</summary>
    [DisplayName("功效")]
    public string? Effect { get; set; }

    /// <summary>主治</summary>
    [DisplayName("主治")]
    public string? Indications { get; set; }

    /// <summary>验方描述</summary>
    [DisplayName("验方描述")]
    public string? Description { get; set; }

    /// <summary>用法</summary>
    [DisplayName("用法")]
    public string? Usage { get; set; }

    /// <summary>性味归经</summary>
    [DisplayName("性味归经")]
    public string? Property { get; set; }

    /// <summary>分类</summary>
    [DisplayName("分类")]
    public string? Category { get; set; }

    /// <summary>是否共享</summary>
    [DisplayName("是否共享")]
    public bool IsShared { get; set; }

    /// <summary>验证状态</summary>
    [DisplayName("验证状态")]
    public FormulaValidationStatus ValidationStatus { get; set; }

    /// <summary>来源</summary>
    [DisplayName("来源")]
    public string? Source { get; set; }

    /// <summary>禁忌症</summary>
    [DisplayName("禁忌症")]
    public string? Contraindications { get; set; }

    /// <summary>备注</summary>
    [DisplayName("备注")]
    public string? Remark { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; }

    /// <summary>药材数量（由Service计算）</summary>
    [DisplayName("药材数量")]
    public int HerbCount { get; set; }

    /// <summary>总价格（由Service计算）</summary>
    [DisplayName("总价格")]
    public decimal TotalPrice { get; set; }

    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>药材组成</summary>
    [DisplayName("药材组成")]
    public List<FormulaHerbItemDetailDto> Herbs { get; set; } = new();
}

/// <summary>
/// 验方药材项详情DTO - 扁平化设计
/// OpenSpec: refactor-dto-simplification
/// </summary>
public class FormulaHerbItemDetailDto
{
    /// <summary>项ID</summary>
    [DisplayName("项ID")]
    public Guid Id { get; set; }

    /// <summary>药材ID（可空，支持延迟绑定）</summary>
    [DisplayName("药材ID")]
    public Guid? HerbId { get; set; }

    /// <summary>药材名称</summary>
    [DisplayName("药材名称")]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>原始药材名称（从老系统导入）</summary>
    [DisplayName("原始药材名称")]
    public string? OriginalHerbName { get; set; }

    /// <summary>是否已验证绑定</summary>
    [DisplayName("已验证")]
    public bool IsValidated { get; set; }

    /// <summary>用量</summary>
    [DisplayName("用量")]
    public int Dosage { get; set; }

    /// <summary>单位</summary>
    [DisplayName("单位")]
    public string Unit { get; set; } = string.Empty;

    /// <summary>炮制方法</summary>
    [DisplayName("炮制方法")]
    public string? Preparation { get; set; }

    /// <summary>加工方法</summary>
    [DisplayName("加工方法")]
    public string? ProcessingMethod { get; set; }

    /// <summary>用法</summary>
    [DisplayName("用法")]
    public string? Usage { get; set; }

    /// <summary>价格</summary>
    [DisplayName("价格")]
    public decimal Price { get; set; }

    /// <summary>煎法</summary>
    [DisplayName("煎法")]
    public DecocteMethod DecocteMethod { get; set; }

    /// <summary>排序</summary>
    [DisplayName("排序")]
    public int SortOrder { get; set; }
}
