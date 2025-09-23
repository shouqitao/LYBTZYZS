using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Models;

/// <summary>
/// 验方列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用FormulaDto，实现Desktop层与Shared层的解耦
/// 保持属性名与FormulaDto一致，确保XAML绑定兼容
/// </summary>
public partial class FormulaItem : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string? pinyin;

    [ObservableProperty]
    private string? category;

    [ObservableProperty]
    private string? source; // 来源

    [ObservableProperty]
    private string? composition; // 组成

    [ObservableProperty]
    private string? effect; // 功效

    [ObservableProperty]
    private string? indication; // 主治

    [ObservableProperty]
    private string? usage; // 用法用量

    [ObservableProperty]
    private string? modification; // 加减

    [ObservableProperty]
    private string? contraindication; // 禁忌

    [ObservableProperty]
    private string? note; // 注意事项

    [ObservableProperty]
    private string? createdBy;

    [ObservableProperty]
    private bool isClassic; // 是否经典方

    [ObservableProperty]
    private bool isPersonal; // 是否个人验方

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private int usageCount; // 使用次数

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? updatedAt;

    [ObservableProperty]
    private List<FormulaHerbItem> herbs = new();

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isExpanded;

    [ObservableProperty]
    private bool isFavorite;

    /// <summary>
    /// 从FormulaDto创建FormulaItem
    /// </summary>
    public static FormulaItem FromDto(FormulaDto dto)
    {
        var item = new FormulaItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Pinyin = dto.Pinyin,
            Category = dto.Category,
            Source = dto.Source,
            Composition = dto.Composition,
            Effect = dto.Effect,
            Indication = dto.Indication,
            Usage = dto.Usage,
            Modification = dto.Modification,
            Contraindication = dto.Contraindication,
            Note = dto.Note,
            CreatedBy = dto.CreatedBy,
            IsClassic = dto.IsClassic,
            IsPersonal = dto.IsPersonal,
            IsActive = dto.IsActive,
            UsageCount = dto.UsageCount,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };

        // 转换药材列表
        if (dto.Herbs != null)
        {
            item.Herbs = dto.Herbs.Select(h => FormulaHerbItem.FromDto(h)).ToList();
        }

        return item;
    }

    /// <summary>
    /// 转换为FormulaDto（用于API调用）
    /// </summary>
    public FormulaDto ToDto()
    {
        return new FormulaDto
        {
            Id = Id,
            Name = Name,
            Pinyin = Pinyin,
            Category = Category,
            Source = Source,
            Composition = Composition,
            Effect = Effect,
            Indication = Indication,
            Usage = Usage,
            Modification = Modification,
            Contraindication = Contraindication,
            Note = Note,
            CreatedBy = CreatedBy,
            IsClassic = IsClassic,
            IsPersonal = IsPersonal,
            IsActive = IsActive,
            UsageCount = UsageCount,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Herbs = Herbs.Select(h => h.ToDto()).ToList()
        };
    }

    /// <summary>
    /// 类型文本
    /// </summary>
    public string TypeText
    {
        get
        {
            if (IsClassic) return "经典方";
            if (IsPersonal) return "个人验方";
            return "通用方";
        }
    }

    /// <summary>
    /// 类型颜色
    /// </summary>
    public string TypeColor
    {
        get
        {
            if (IsClassic) return "#9C27B0";
            if (IsPersonal) return "#FF9800";
            return "#2196F3";
        }
    }

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText => IsActive ? "启用" : "停用";

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => IsActive ? "#4CAF50" : "#F44336";

    /// <summary>
    /// 药材数量
    /// </summary>
    public int HerbCount => Herbs?.Count ?? 0;

    /// <summary>
    /// 药材组成文本
    /// </summary>
    public string HerbCompositionText
    {
        get
        {
            if (Herbs == null || Herbs.Count == 0)
                return "暂无组成";

            var herbNames = Herbs.Select(h => $"{h.HerbName}{h.Dosage}{h.Unit}");
            return string.Join("、", herbNames);
        }
    }

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{Name}({TypeText}) - {Category}";

    /// <summary>
    /// 搜索文本
    /// </summary>
    public string SearchText => $"{Name} {Pinyin} {Category} {Effect} {Indication}";

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable => IsActive;

    /// <summary>
    /// 是否有禁忌
    /// </summary>
    public bool HasContraindication => !string.IsNullOrWhiteSpace(Contraindication);

    /// <summary>
    /// 是否有加减
    /// </summary>
    public bool HasModification => !string.IsNullOrWhiteSpace(Modification);

    /// <summary>
    /// 受欢迎程度（基于使用次数）
    /// </summary>
    public string PopularityLevel
    {
        get
        {
            if (UsageCount >= 100) return "热门";
            if (UsageCount >= 50) return "常用";
            if (UsageCount >= 10) return "偶用";
            return "少用";
        }
    }

    /// <summary>
    /// 受欢迎程度颜色
    /// </summary>
    public string PopularityColor
    {
        get
        {
            if (UsageCount >= 100) return "#F44336";
            if (UsageCount >= 50) return "#FF9800";
            if (UsageCount >= 10) return "#2196F3";
            return "#9E9E9E";
        }
    }
}

/// <summary>
/// 验方中的药材项
/// </summary>
public partial class FormulaHerbItem : ObservableObject
{
    [ObservableProperty]
    private int herbId;

    [ObservableProperty]
    private string herbName = string.Empty;

    [ObservableProperty]
    private decimal dosage;

    [ObservableProperty]
    private string unit = string.Empty;

    [ObservableProperty]
    private string? usage;

    [ObservableProperty]
    private int sequence;

    /// <summary>
    /// 从FormulaHerbDto创建
    /// </summary>
    public static FormulaHerbItem FromDto(FormulaHerbDto dto)
    {
        return new FormulaHerbItem
        {
            HerbId = dto.HerbId,
            HerbName = dto.HerbName,
            Dosage = dto.Dosage,
            Unit = dto.Unit,
            Usage = dto.Usage,
            Sequence = dto.Sequence
        };
    }

    /// <summary>
    /// 转换为DTO
    /// </summary>
    public FormulaHerbDto ToDto()
    {
        return new FormulaHerbDto
        {
            HerbId = HerbId,
            HerbName = HerbName,
            Dosage = Dosage,
            Unit = Unit,
            Usage = Usage,
            Sequence = Sequence
        };
    }

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText => $"{HerbName} {Dosage}{Unit}";
}