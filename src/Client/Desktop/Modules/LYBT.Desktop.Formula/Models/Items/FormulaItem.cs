using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Formula.Models.Items;

/// <summary>
/// 验方列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用FormulaDetailDto，实现Desktop层与Shared层的解耦
/// 保持属性名与FormulaDetailDto一致，确保XAML绑定兼容
/// </summary>
public class FormulaItem : BindableBase
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

    private string? _source;
    public string? Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    } // 来源

    private string? _composition;
    public string? Composition
    {
        get => _composition;
        set => SetProperty(ref _composition, value);
    } // 组成

    private string? _effect;
    public string? Effect
    {
        get => _effect;
        set => SetProperty(ref _effect, value);
    } // 功效

    /// <summary>
    /// 主治 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Indications，与DTO保持一致
    /// </summary>
    private string? _indications;
    public string? Indications
    {
        get => _indications;
        set => SetProperty(ref _indications, value);
    }

    private string? _usage;
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    } // 用法用量

    private string? _modification;
    public string? Modification
    {
        get => _modification;
        set => SetProperty(ref _modification, value);
    } // 加减

    /// <summary>
    /// 禁忌 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Contraindications，与DTO保持一致
    /// </summary>
    private string? _contraindications;
    public string? Contraindications
    {
        get => _contraindications;
        set => SetProperty(ref _contraindications, value);
    }

    /// <summary>
    /// 注意事项/备注 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Remark，与DTO保持一致
    /// </summary>
    private string? _remark;
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    /// <summary>
    /// 创建者ID - OpenSpec: unify-frontend-backend-types Phase 4
    /// 统一使用Guid?，与DTO保持一致
    /// </summary>
    private Guid? _createdBy;
    public Guid? CreatedBy
    {
        get => _createdBy;
        set => SetProperty(ref _createdBy, value);
    }

    private bool _isClassic;
    public bool IsClassic
    {
        get => _isClassic;
        set => SetProperty(ref _isClassic, value);
    } // 是否经典方

    private bool _isPersonal;
    public bool IsPersonal
    {
        get => _isPersonal;
        set => SetProperty(ref _isPersonal, value);
    } // 是否个人验方

    /// <summary>
    /// 状态 - OpenSpec: unify-frontend-backend-types Phase 4
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
    /// 是否启用（向后兼容计算属性）- OpenSpec: unify-frontend-backend-types Phase 4
    /// </summary>
    public bool IsActive => Status == CommonStatus.Enabled;

    private int _usageCount;
    public int UsageCount
    {
        get => _usageCount;
        set => SetProperty(ref _usageCount, value);
    } // 使用次数

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

    private List<FormulaHerbItem> _herbs = new();
    public List<FormulaHerbItem> Herbs
    {
        get => _herbs;
        set => SetProperty(ref _herbs, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    /// <summary>
    /// 从FormulaDetailDto创建FormulaItem
    /// OpenSpec: unify-frontend-backend-types Phase 4 - Status/CreatedBy直接使用
    /// </summary>
    public static FormulaItem FromDto(FormulaDetailDto dto)
    {
        var item = new FormulaItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Pinyin = null, // FormulaDetailDto中没有此属性
            Category = dto.Category,
            Source = null, // FormulaDetailDto中没有此属性
            Composition = null, // FormulaDetailDto中没有此属性
            Effect = dto.Effect,
            Indications = null, // FormulaDetailDto中没有此属性
            Usage = dto.Usage,
            Modification = null, // FormulaDetailDto中没有此属性
            Contraindications = null, // FormulaDetailDto中没有此属性
            Remark = dto.Remark, // OpenSpec: unify-frontend-backend-types - 直接映射
            CreatedBy = dto.CreatedBy, // OpenSpec: unify-frontend-backend-types - 直接使用Guid?
            IsClassic = false, // 默认值
            IsPersonal = !dto.IsShared, // 根据IsShared推断
            Status = dto.Status, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
            UsageCount = 0, // 默认值
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
    /// 转换为FormulaDetailDto（用于API调用）
    /// OpenSpec: unify-frontend-backend-types Phase 4 - Status直接使用枚举
    /// </summary>
    public FormulaDetailDto ToDto()
    {
        return new FormulaDetailDto
        {
            Id = Id,
            Name = Name,
            Effect = Effect,
            Usage = Usage,
            Property = null, // FormulaDetailDto中的属性
            IsShared = !IsPersonal,
            Remark = Remark, // OpenSpec: unify-frontend-backend-types - 直接映射
            Status = Status, // OpenSpec: unify-frontend-backend-types - 直接使用枚举
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
    /// 状态显示文本 - OpenSpec: unify-frontend-backend-types Phase 4
    /// </summary>
    public string StatusText => Status switch
    {
        CommonStatus.Enabled => "启用",
        CommonStatus.Disabled => "停用",
        _ => "未知"
    };

    /// <summary>
    /// 状态颜色 - OpenSpec: unify-frontend-backend-types Phase 4
    /// </summary>
    public string StatusColor => Status switch
    {
        CommonStatus.Enabled => "#4CAF50",
        CommonStatus.Disabled => "#F44336",
        _ => "#757575"
    };

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
    /// 搜索文本 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public string SearchText => $"{Name} {Pinyin} {Category} {Effect} {Indications}";

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable => IsActive;

    /// <summary>
    /// 是否有禁忌 - OpenSpec: unify-frontend-backend-types Phase 6
    /// </summary>
    public bool HasContraindication => !string.IsNullOrWhiteSpace(Contraindications);

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
