using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.SharedKernel.Primitives;

namespace LYBT.Module.Formulas.Domain;

/// <summary>
/// 验方明细 - 验方中的药材组成，包含药材名称和剂量。
/// </summary>
public class FormulaHerbItem : Entity
{
    /// <summary>所属验方ID</summary>
    public Guid FormulaId { get; private set; }

    /// <summary>关联的验方实体</summary>
    public Formula? Formula { get; private set; }

    /// <summary>药材ID（可空，支持延迟绑定）</summary>
    public Guid? HerbId { get; private set; }

    /// <summary>原始药材名称（从老系统导入时保存，用于延迟绑定）</summary>
    [StringLength(100)]
    public string? OriginalHerbName { get; private set; }

    /// <summary>是否已验证绑定</summary>
    public bool IsValidated { get; private set; }

    /// <summary>药材名称</summary>
    [Required]
    [StringLength(100)]
    public string HerbName { get; private set; } = string.Empty;

    /// <summary>剂量（整数）</summary>
    public int Dosage { get; private set; } = 1;

    /// <summary>单位</summary>
    [StringLength(16)]
    public string Unit { get; private set; } = "g";

    /// <summary>用法说明</summary>
    [StringLength(200)]
    public string? Usage { get; private set; }

    /// <summary>备注</summary>
    [StringLength(200)]
    public string? Remark { get; private set; }

    /// <summary>炮制方法</summary>
    [StringLength(100)]
    public string? ProcessingMethod { get; private set; }

    /// <summary>煎法（先煎、后下等）</summary>
    public DecocteMethod DecocteMethod { get; private set; } = DecocteMethod.Default;

    private FormulaHerbItem() { }

    /// <summary>
    /// 创建验方药材项。
    /// </summary>
    public static FormulaHerbItem Create(
        Guid formulaId,
        string herbName,
        int dosage = 1,
        string unit = "g",
        Guid? herbId = null,
        string? originalHerbName = null,
        string? usage = null,
        string? remark = null,
        string? processingMethod = null,
        DecocteMethod decocteMethod = DecocteMethod.Default)
    {
        if (string.IsNullOrWhiteSpace(herbName))
            throw new ArgumentException("药材名称不能为空", nameof(herbName));

        return new FormulaHerbItem
        {
            Id = Guid.NewGuid(),
            FormulaId = formulaId,
            HerbName = herbName.Trim(),
            Dosage = dosage,
            Unit = unit,
            HerbId = herbId,
            OriginalHerbName = originalHerbName?.Trim(),
            IsValidated = herbId.HasValue,
            Usage = usage?.Trim(),
            Remark = remark?.Trim(),
            ProcessingMethod = processingMethod?.Trim(),
            DecocteMethod = decocteMethod
        };
    }

    /// <summary>
    /// 绑定药材到系统药材库。
    /// </summary>
    public void BindHerb(Guid herbId, string herbName)
    {
        HerbId = herbId;
        HerbName = herbName;
        IsValidated = true;
    }
}


