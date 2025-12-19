using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Models.Items.Herbs;

/// <summary>
/// 中药材列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用HerbDetailDto，实现Desktop层与Shared层的解耦
/// 保持属性名与HerbDetailDto一致，确保XAML绑定兼容
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

    /// <summary>
    /// 拼音码 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为PinYinCode，与DTO保持一致
    /// </summary>
    private string? _pinYinCode;
    public string? PinYinCode
    {
        get => _pinYinCode;
        set => SetProperty(ref _pinYinCode, value);
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

    /// <summary>
    /// 单位 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Unit，与DTO保持一致
    /// </summary>
    private string? _unit;
    public string? Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    private string? _usage; // 用法
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    }

    /// <summary>
    /// 单价 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Price，与DTO保持一致
    /// </summary>
    private decimal _price;
    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    /// <summary>
    /// 规格 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Spec，与DTO保持一致
    /// </summary>
    private string? _spec;
    public string? Spec
    {
        get => _spec;
        set => SetProperty(ref _spec, value);
    }

    private string? _manufacturer; // 生产厂家
    public string? Manufacturer
    {
        get => _manufacturer;
        set => SetProperty(ref _manufacturer, value);
    }

    /// <summary>
    /// 状态 - OpenSpec: unify-frontend-backend-types Phase 3
    /// 统一使用CommonStatus枚举，与DTO保持一致
    /// </summary>
    private CommonStatus _status = CommonStatus.Enabled;
    public CommonStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(StatusColor));
                RaisePropertyChanged(nameof(IsAvailable));
            }
        }
    }

    /// <summary>
    /// 是否启用（向后兼容计算属性）- OpenSpec: unify-frontend-backend-types Phase 3
    /// </summary>
    public bool IsActive => Status == CommonStatus.Enabled;

    // MVP阶段不实现库存管理，已移除Stock属性

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
    /// 从HerbDetailDto创建HerbItem
    /// OpenSpec: unify-frontend-backend-types Phase 3 - Status直接使用枚举
    /// </summary>
    public static HerbItem FromDto(HerbDetailDto dto)
    {
        return new HerbItem
        {
            Id = dto.Id,
            Name = dto.Name,
            PinYinCode = dto.PinYinCode, // OpenSpec: unify-frontend-backend-types - 直接映射
            Category = null, // HerbDetailDto中没有此属性
            Nature = null, // HerbDetailDto中没有此属性
            Meridian = null, // HerbDetailDto中没有此属性
            Effect = dto.Effect,
            Indication = null, // HerbDetailDto中没有此属性
            Contraindication = null, // HerbDetailDto中没有此属性
            DosageMin = 0, // HerbDetailDto中没有此属性
            DosageMax = 0, // HerbDetailDto中没有此属性
            Unit = dto.Unit, // OpenSpec: unify-frontend-backend-types - 直接映射
            Usage = dto.Usage,
            Price = dto.Price, // OpenSpec: unify-frontend-backend-types - 直接映射
            Spec = dto.Spec, // OpenSpec: unify-frontend-backend-types - 直接映射
            Manufacturer = null, // HerbDetailDto中没有此属性
            Status = dto.Status, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
            // MVP阶段移除Stock属性
            Remark = dto.Remark,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }

    /// <summary>
    /// 转换为HerbDetailDto（用于API调用）
    /// OpenSpec: unify-frontend-backend-types Phase 3 - Status直接使用枚举
    /// </summary>
    public HerbDetailDto ToDto()
    {
        return new HerbDetailDto
        {
            Id = Id,
            Name = Name,
            PinYinCode = PinYinCode, // OpenSpec: unify-frontend-backend-types - 直接映射
            Origin = null, // HerbItem中没有此属性
            Spec = Spec, // OpenSpec: unify-frontend-backend-types - 直接映射
            Unit = Unit ?? "克", // OpenSpec: unify-frontend-backend-types - 直接映射
            Price = Price, // OpenSpec: unify-frontend-backend-types - 直接映射
            CostPrice = null, // HerbItem中没有此属性
            Effect = Effect,
            Usage = Usage,
            Status = Status, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
            Remark = Remark,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// 状态显示文本 - OpenSpec: unify-frontend-backend-types Phase 3
    /// </summary>
    public string StatusText => Status switch
    {
        CommonStatus.Enabled => "启用",
        CommonStatus.Disabled => "停用",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色 - OpenSpec: unify-frontend-backend-types Phase 3
    /// </summary>
    public string StatusColor => Status switch
    {
        CommonStatus.Enabled => "#4CAF50",
        CommonStatus.Disabled => "#F44336",
        _ => "#757575"
    };

    // MVP阶段已移除库存相关属性：StockStatus, StockColor

    /// <summary>
    /// 推荐剂量范围文本 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public string DosageRangeText => $"{DosageMin}-{DosageMax}{Unit}";

    /// <summary>
    /// 显示文本（用于ComboBox等）- OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public string DisplayText => $"{Name}({PinYinCode}) - {Category}";

    /// <summary>
    /// 搜索文本（用于快速搜索）- OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public string SearchText => $"{Name} {PinYinCode} {Category} {Effect}";

    /// <summary>
    /// 价格显示文本 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public string PriceText => $"¥{Price:F2}/{Unit}";

    /// <summary>
    /// 是否可用（仅基于启用状态，MVP阶段不考虑库存）
    /// </summary>
    public bool IsAvailable => IsActive;

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
    /// 计算小计金额 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public decimal CalculateSubtotal() => CurrentDosage * Price;
}
