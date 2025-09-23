using System;
using Prism.Mvvm;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Models;

/// <summary>
/// 中药材列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用HerbDto，实现Desktop层与Shared层的解耦
/// 保持属性名与HerbDto一致，确保XAML绑定兼容
/// </summary>
public class HerbItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string? _pinyin;
    public string? Pinyin
    {
        get => _pinyin;
        set => SetProperty(ref _pinyin, value);
    }

    private string? _category;
    public string? Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    private string? _nature; // 性味
    public string? Nature
    {
        get => _nature;
        set => SetProperty(ref _nature, value);
    }

    private string? _meridian; // 归经
    public string? Meridian
    {
        get => _meridian;
        set => SetProperty(ref _meridian, value);
    }

    private string? _effect; // 功效
    public string? Effect
    {
        get => _effect;
        set => SetProperty(ref _effect, value);
    }

    private string? _indication; // 主治
    public string? Indication
    {
        get => _indication;
        set => SetProperty(ref _indication, value);
    }

    private string? _contraindication; // 禁忌
    public string? Contraindication
    {
        get => _contraindication;
        set => SetProperty(ref _contraindication, value);
    }

    private decimal _dosageMin;
    public decimal DosageMin
    {
        get => _dosageMin;
        set => SetProperty(ref _dosageMin, value);
    }

    private decimal _dosageMax;
    public decimal DosageMax
    {
        get => _dosageMax;
        set => SetProperty(ref _dosageMax, value);
    }

    private string? _dosageUnit;
    public string? DosageUnit
    {
        get => _dosageUnit;
        set => SetProperty(ref _dosageUnit, value);
    }

    private string? _usage; // 用法
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    }

    private decimal _unitPrice;
    public decimal UnitPrice
    {
        get => _unitPrice;
        set => SetProperty(ref _unitPrice, value);
    }

    private string? _specification; // 规格
    public string? Specification
    {
        get => _specification;
        set => SetProperty(ref _specification, value);
    }

    private string? _manufacturer; // 生产厂家
    public string? Manufacturer
    {
        get => _manufacturer;
        set => SetProperty(ref _manufacturer, value);
    }

    private bool _isActive = true;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    private int _stock;
    public int Stock
    {
        get => _stock;
        set => SetProperty(ref _stock, value);
    }

    private string? _remark;
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    private DateTime? _updatedAt;
    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isHighlighted;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }

    private decimal _currentDosage; // 当前处方剂量
    public decimal CurrentDosage
    {
        get => _currentDosage;
        set => SetProperty(ref _currentDosage, value);
    }

    /// <summary>
    /// 从HerbDto创建HerbItem
    /// </summary>
    public static HerbItem FromDto(HerbDto dto)
    {
        return new HerbItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Pinyin = dto.PinYinCode,
            Category = null, // HerbDto中没有此属性
            Nature = null, // HerbDto中没有此属性
            Meridian = null, // HerbDto中没有此属性
            Effect = dto.Effect,
            Indication = null, // HerbDto中没有此属性
            Contraindication = null, // HerbDto中没有此属性
            DosageMin = 0, // HerbDto中没有此属性
            DosageMax = 0, // HerbDto中没有此属性
            DosageUnit = dto.Unit,
            Usage = dto.Usage,
            UnitPrice = dto.Price,
            Specification = dto.Spec,
            Manufacturer = null, // HerbDto中没有此属性
            IsActive = dto.Status == CommonStatus.Enabled,
            Stock = 0, // HerbDto中没有此属性
            Remark = dto.Remark,
            CreatedAt = dto.CreateTime,
            UpdatedAt = dto.UpdateTime
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
            PinYinCode = Pinyin,
            Origin = null, // HerbItem中没有此属性
            Spec = Specification,
            Unit = DosageUnit ?? "克",
            Price = UnitPrice,
            CostPrice = null, // HerbItem中没有此属性
            Effect = Effect,
            Usage = Usage,
            Status = IsActive ? CommonStatus.Enabled : CommonStatus.Disabled,
            Remark = Remark,
            CreateTime = CreatedAt,
            UpdateTime = UpdatedAt
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