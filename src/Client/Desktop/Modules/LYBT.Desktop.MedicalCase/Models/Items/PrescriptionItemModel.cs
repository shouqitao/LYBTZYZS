using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 处方药材项 Model - Desktop UI 层可编辑数据模型
/// 替代直接使用 PrescriptionItemDto，遵循 DTO 不暴露到 UI 层原则（16-desktop-architecture-spec §4.6）
/// 实现 IHerbItemEditable 以支持共享控件 HerbListControl 的药材选择/剂量编辑
/// </summary>
public class PrescriptionItemModel : ObservableObject, IHerbItemEditable
{
    private Guid _id;
    private Guid? _prescriptionId;
    private Guid _herbId;
    private string _herbName = string.Empty;
    private string _unit = "g";
    private decimal _unitPrice;
    private int _dosage;
    private decimal _totalPrice;
    private decimal _totalWeight;
    private decimal _subtotal;
    private string? _usage;
    private DecocteMethod _decocteMethod = DecocteMethod.Default;
    private HerbRole _role = HerbRole.None;
    private string? _remark;

    private ObservableCollection<HerbListDto>? _allHerbs;
    private ObservableCollection<HerbListDto> _filteredHerbs = new();
    private HerbListDto? _selectedHerb;

    /// <summary>唯一标识符</summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>处方ID</summary>
    public Guid? PrescriptionId
    {
        get => _prescriptionId;
        set => SetProperty(ref _prescriptionId, value);
    }

    /// <summary>中药材ID</summary>
    public Guid HerbId
    {
        get => _herbId;
        set => SetProperty(ref _herbId, value);
    }

    /// <summary>中药材名称</summary>
    public string HerbName
    {
        get => _herbName;
        set
        {
            if (SetProperty(ref _herbName, value))
            {
                FilterHerbs();
            }
        }
    }

    /// <summary>单位</summary>
    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    /// <summary>单价</summary>
    public decimal UnitPrice
    {
        get => _unitPrice;
        set => SetProperty(ref _unitPrice, value);
    }

    /// <summary>剂量（整数克）</summary>
    public int Dosage
    {
        get => _dosage;
        set => SetProperty(ref _dosage, value);
    }

    /// <summary>总价</summary>
    public decimal TotalPrice
    {
        get => _totalPrice;
        set => SetProperty(ref _totalPrice, value);
    }

    /// <summary>总重量</summary>
    public decimal TotalWeight
    {
        get => _totalWeight;
        set => SetProperty(ref _totalWeight, value);
    }

    /// <summary>小计金额</summary>
    public decimal Subtotal
    {
        get => _subtotal;
        set => SetProperty(ref _subtotal, value);
    }

    /// <summary>用法说明</summary>
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    }

    /// <summary>煎法</summary>
    public DecocteMethod DecocteMethod
    {
        get => _decocteMethod;
        set => SetProperty(ref _decocteMethod, value);
    }

    /// <summary>药材角色（君臣佐使）</summary>
    public HerbRole Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    /// <summary>备注</summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    /// <summary>是否为空行（未选择药材）</summary>
    public bool IsEmpty => HerbId == Guid.Empty;

    #region IHerbItemEditable

    /// <inheritdoc />
    public ObservableCollection<HerbListDto>? AllHerbs
    {
        get => _allHerbs;
        set => SetProperty(ref _allHerbs, value);
    }

    /// <inheritdoc />
    public ObservableCollection<HerbListDto> FilteredHerbs => _filteredHerbs;

    /// <inheritdoc />
    public HerbListDto? SelectedHerb
    {
        get => _selectedHerb;
        set
        {
            if (SetProperty(ref _selectedHerb, value) && value != null)
            {
                // 选中药材后自动填充基础属性
                HerbId = value.Id;
                HerbName = value.Name ?? string.Empty;
                Unit = value.Unit;
                UnitPrice = value.Price;
            }
        }
    }

    #endregion

    #region 方法

    /// <summary>创建空模型</summary>
    public static PrescriptionItemModel CreateNew()
    {
        return new PrescriptionItemModel();
    }

    /// <summary>克隆模型（取消编辑恢复用）</summary>
    public PrescriptionItemModel Clone()
    {
        return new PrescriptionItemModel
        {
            Id = Id,
            PrescriptionId = PrescriptionId,
            HerbId = HerbId,
            HerbName = HerbName,
            Unit = Unit,
            UnitPrice = UnitPrice,
            Dosage = Dosage,
            TotalPrice = TotalPrice,
            TotalWeight = TotalWeight,
            Subtotal = Subtotal,
            Usage = Usage,
            DecocteMethod = DecocteMethod,
            Role = Role,
            Remark = Remark
        };
    }

    /// <summary>
    /// 拼音码/名称过滤 - 供药材选择建议列表使用
    /// 参考 HerbItemViewModelBase.FilterHerbs 的简化版
    /// </summary>
    private void FilterHerbs()
    {
        FilteredHerbs.Clear();

        if (AllHerbs == null || string.IsNullOrWhiteSpace(HerbName))
        {
            return;
        }

        var searchText = HerbName.Trim().ToLower();

        // 精确匹配说明是选择结果，不显示建议
        if (AllHerbs.Any(h => string.Equals(h.Name, searchText, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var herb in AllHerbs)
        {
            var name = herb.Name?.ToLower() ?? string.Empty;
            var pinyin = herb.PinYinCode?.ToLower() ?? string.Empty;

            if (name.Contains(searchText) || pinyin.Contains(searchText))
            {
                FilteredHerbs.Add(herb);
            }
        }
    }

    #endregion
}
