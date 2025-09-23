using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Formula.Models;

/// <summary>
/// 验方列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用FormulaDto，实现Desktop层与Shared层的解耦
/// 保持属性名与FormulaDto一致，确保XAML绑定兼容
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

        private string? _indication;
    public string? Indication
    {
        get => _indication;
        set => SetProperty(ref _indication, value);
    } // 主治

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

        private string? _contraindication;
    public string? Contraindication
    {
        get => _contraindication;
        set => SetProperty(ref _contraindication, value);
    } // 禁忌

        private string? _note;
    public string? Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    } // 注意事项

        private string? _createdBy;
    public string? CreatedBy
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

        private bool _isActive = true;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

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
    /// 从FormulaDto创建FormulaItem
    /// </summary>
    public static FormulaItem FromDto(FormulaDto dto)
    {
        var item = new FormulaItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Pinyin = null, // FormulaDto中没有此属性
            Category = dto.Category,
            Source = null, // FormulaDto中没有此属性
            Composition = null, // FormulaDto中没有此属性  
            Effect = dto.Effect,
            Indication = null, // FormulaDto中没有此属性
            Usage = dto.Usage,
            Modification = null, // FormulaDto中没有此属性
            Contraindication = null, // FormulaDto中没有此属性
            Note = dto.Remark, // FormulaDto中是Remark
            CreatedBy = null, // FormulaDto中没有此属性
            IsClassic = false, // 默认值
            IsPersonal = !dto.IsShared, // 根据IsShared推断
            IsActive = dto.Status == CommonStatus.Enabled,
            UsageCount = 0, // 默认值
            CreatedAt = dto.CreateTime,
            UpdatedAt = dto.UpdateTime
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
            Effect = Effect,
            Usage = Usage,
            Property = null, // FormulaDto中的属性
            IsShared = !IsPersonal,
            Remark = Note,
            Status = IsActive ? CommonStatus.Enabled : CommonStatus.Disabled,
            CreateTime = CreatedAt,
            UpdateTime = UpdatedAt,
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
public class FormulaHerbItem : BindableBase
{
        private Guid _herbId;
    public Guid HerbId
    {
        get => _herbId;
        set => SetProperty(ref _herbId, value);
    }

        private string _herbName = string.Empty;
    public string HerbName
    {
        get => _herbName;
        set => SetProperty(ref _herbName, value);
    }

        private decimal _dosage;
    public decimal Dosage
    {
        get => _dosage;
        set => SetProperty(ref _dosage, value);
    }

        private string _unit = string.Empty;
    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

        private string? _usage;
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    }

        private int _sequence;
    public int Sequence
    {
        get => _sequence;
        set => SetProperty(ref _sequence, value);
    }

    /// <summary>
    /// 从FormulaHerbItemDto创建
    /// </summary>
    public static FormulaHerbItem FromDto(FormulaHerbItemDto dto)
    {
        return new FormulaHerbItem
        {
            HerbId = dto.HerbId,
            HerbName = dto.HerbName,
            Dosage = dto.Quantity, // FormulaHerbItemDto 中是 Quantity
            Unit = dto.Unit,
            Usage = dto.Usage,
            Sequence = dto.SortOrder // FormulaHerbItemDto 中是 SortOrder
        };
    }

    /// <summary>
    /// 转换为DTO
    /// </summary>
    public FormulaHerbItemDto ToDto()
    {
        return new FormulaHerbItemDto
        {
            HerbId = HerbId,
            HerbName = HerbName,
            Quantity = Dosage, // FormulaHerbItemDto 中是 Quantity
            Unit = Unit,
            Usage = Usage,
            SortOrder = Sequence // FormulaHerbItemDto 中是 SortOrder
        };
    }

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText => $"{HerbName} {Dosage}{Unit}";
}
