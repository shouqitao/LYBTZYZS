using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Models;

/// <summary>
/// 中药材列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用HerbDto，实现Desktop层与Shared层的解耦
/// 保持属性名与HerbDto一致，确保XAML绑定兼容
/// </summary>
public partial class HerbItem : ObservableObject
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
    private string? nature; // 性味

    [ObservableProperty]
    private string? meridian; // 归经

    [ObservableProperty]
    private string? effect; // 功效

    [ObservableProperty]
    private string? indication; // 主治

    [ObservableProperty]
    private string? contraindication; // 禁忌

    [ObservableProperty]
    private decimal dosageMin;

    [ObservableProperty]
    private decimal dosageMax;

    [ObservableProperty]
    private string? dosageUnit;

    [ObservableProperty]
    private string? usage; // 用法

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private string? specification; // 规格

    [ObservableProperty]
    private string? manufacturer; // 生产厂家

    [ObservableProperty]
    private bool isActive = true;

    [ObservableProperty]
    private int stock;

    [ObservableProperty]
    private string? remark;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? updatedAt;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private bool isHighlighted;

    [ObservableProperty]
    private decimal currentDosage; // 当前处方剂量

    /// <summary>
    /// 从HerbDto创建HerbItem
    /// </summary>
    public static HerbItem FromDto(HerbDto dto)
    {
        return new HerbItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Pinyin = dto.Pinyin,
            Category = dto.Category,
            Nature = dto.Nature,
            Meridian = dto.Meridian,
            Effect = dto.Effect,
            Indication = dto.Indication,
            Contraindication = dto.Contraindication,
            DosageMin = dto.DosageMin,
            DosageMax = dto.DosageMax,
            DosageUnit = dto.DosageUnit,
            Usage = dto.Usage,
            UnitPrice = dto.UnitPrice,
            Specification = dto.Specification,
            Manufacturer = dto.Manufacturer,
            IsActive = dto.IsActive,
            Stock = dto.Stock,
            Remark = dto.Remark,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// 转换为HerbDto（用于API调用）
    /// </summary>
    public HerbDto ToDto()
    {
        return new HerbDto
        {
            Id = Id,
            Name = Name,
            Pinyin = Pinyin,
            Category = Category,
            Nature = Nature,
            Meridian = Meridian,
            Effect = Effect,
            Indication = Indication,
            Contraindication = Contraindication,
            DosageMin = DosageMin,
            DosageMax = DosageMax,
            DosageUnit = DosageUnit,
            Usage = Usage,
            UnitPrice = UnitPrice,
            Specification = Specification,
            Manufacturer = Manufacturer,
            IsActive = IsActive,
            Stock = Stock,
            Remark = Remark,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusText => IsActive ? "启用" : "停用";

    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => IsActive ? "#4CAF50" : "#F44336";

    /// <summary>
    /// 库存状态
    /// </summary>
    public string StockStatus
    {
        get
        {
            if (Stock <= 0) return "缺货";
            if (Stock < 50) return "库存不足";
            return "充足";
        }
    }

    /// <summary>
    /// 库存状态颜色
    /// </summary>
    public string StockColor
    {
        get
        {
            if (Stock <= 0) return "#F44336";
            if (Stock < 50) return "#FF9800";
            return "#4CAF50";
        }
    }

    /// <summary>
    /// 推荐剂量范围文本
    /// </summary>
    public string DosageRangeText => $"{DosageMin}-{DosageMax}{DosageUnit}";

    /// <summary>
    /// 显示文本（用于ComboBox等）
    /// </summary>
    public string DisplayText => $"{Name}({Pinyin}) - {Category}";

    /// <summary>
    /// 搜索文本（用于快速搜索）
    /// </summary>
    public string SearchText => $"{Name} {Pinyin} {Category} {Effect}";

    /// <summary>
    /// 价格显示文本
    /// </summary>
    public string PriceText => $"¥{UnitPrice:F2}/{DosageUnit}";

    /// <summary>
    /// 是否有库存
    /// </summary>
    public bool HasStock => Stock > 0;

    /// <summary>
    /// 是否可用（启用且有库存）
    /// </summary>
    public bool IsAvailable => IsActive && HasStock;

    /// <summary>
    /// 是否有禁忌
    /// </summary>
    public bool HasContraindication => !string.IsNullOrWhiteSpace(Contraindication);

    /// <summary>
    /// 验证当前剂量是否在范围内
    /// </summary>
    public bool IsCurrentDosageValid =>
        CurrentDosage >= DosageMin && CurrentDosage <= DosageMax;

    /// <summary>
    /// 计算小计金额
    /// </summary>
    public decimal CalculateSubtotal() => CurrentDosage * UnitPrice;
}