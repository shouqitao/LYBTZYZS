using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Formula.Models.Items;

/// <summary>
/// 验方列表项UI模型 - 用于DataGrid/ListView显示
/// 替代直接使用FormulaDetailDto，实现Desktop层与Shared层的解耦
/// 保持属性名与FormulaDetailDto一致，确保XAML绑定兼容
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
/// </summary>
public partial class FormulaItem : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _pinyin;

    [ObservableProperty]
    private string? _category;

    [ObservableProperty]
    private string? _source; // 来源

    [ObservableProperty]
    private string? _composition; // 组成

    [ObservableProperty]
    private string? _effect; // 功效

    /// <summary>
    /// 主治 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Indications，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private string? _indications;

    [ObservableProperty]
    private string? _usage; // 用法用量

    [ObservableProperty]
    private string? _modification; // 加减

    /// <summary>
    /// 禁忌 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Contraindications，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private string? _contraindications;

    /// <summary>
    /// 注意事项/备注 - OpenSpec: unify-frontend-backend-types Phase 6
    /// 统一命名为Remark，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private string? _remark;

    /// <summary>
    /// 创建者ID - OpenSpec: unify-frontend-backend-types Phase 4
    /// 统一使用Guid?，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private Guid? _createdBy;

    [ObservableProperty]
    private bool _isClassic; // 是否经典方

    [ObservableProperty]
    private bool _isPersonal; // 是否个人验方

    /// <summary>
    /// 状态 - OpenSpec: unify-frontend-backend-types Phase 4
    /// 统一使用CommonStatus枚举，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(IsAvailable))]
    private CommonStatus _status = CommonStatus.Enabled;

    /// <summary>
    /// 是否启用（向后兼容计算属性）- OpenSpec: unify-frontend-backend-types Phase 4
    /// </summary>
    public bool IsActive => Status == CommonStatus.Enabled;

    [ObservableProperty]
    private int _usageCount; // 使用次数

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime? _updatedAt;

    [ObservableProperty]
    private List<FormulaHerbItem> _herbs = new();

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isFavorite;

    /// <summary>
    /// 从FormulaDetailDto创建FormulaItem
    /// OpenSpec: unify-frontend-backend-types Phase 4 - Status/CreatedBy直接使用
    /// </summary>
    /// <remarks>已废弃：请使用FormulaMappingService.ToItem()</remarks>
    [Obsolete("请使用FormulaMappingService.ToItem()替代。OpenSpec: adopt-mapperly-unified-mapping")]
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
#pragma warning disable CS0618
            item.Herbs = dto.Herbs.Select(h => FormulaHerbItem.FromDto(h)).ToList();
#pragma warning restore CS0618
        }

        return item;
    }

    /// <summary>
    /// 转换为FormulaDetailDto（用于API调用）
    /// OpenSpec: unify-frontend-backend-types Phase 4 - Status直接使用枚举
    /// </summary>
    /// <remarks>已废弃：请使用FormulaMappingService.ToDto()</remarks>
    [Obsolete("请使用FormulaMappingService.ToDto()替代。OpenSpec: adopt-mapperly-unified-mapping")]
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
#pragma warning disable CS0618
            Herbs = Herbs.Select(h => h.ToDto()).ToList()
#pragma warning restore CS0618
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
