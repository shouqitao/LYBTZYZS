using System.Collections.ObjectModel;
using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 处方数据Item - 用于UI绑定的处方数据模型
/// OpenSpec: consolidate-panel-viewmodels - 遵循Entity-DTO-Item模式
/// OpenSpec: adopt-mapperly-unified-mapping - 使用BindableBase确保Mapperly兼容
///
/// 遵循Entity-DTO-Item模式：
/// - Entity: 服务端Prescription实体
/// - DTO: PrescriptionDetailDto/PrescriptionInputDto (Shared层)
/// - Item: PrescriptionItem (Desktop层，用于XAML绑定)
///
/// 属性名与PrescriptionDetailDto保持一致，确保XAML绑定兼容
/// </summary>
public class PrescriptionItem : BindableBase
{
    #region 基础标识字段

    private Guid _id = Guid.Empty;
    /// <summary>
    /// 处方ID
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string? _prescriptionNumber;
    /// <summary>
    /// 处方编号（格式：RX-YYYYMMDD-NNNN）
    /// </summary>
    public string? PrescriptionNumber
    {
        get => _prescriptionNumber;
        set
        {
            if (SetProperty(ref _prescriptionNumber, value))
            {
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    private Guid _medicalCaseId = Guid.Empty;
    /// <summary>
    /// 关联的病历ID
    /// </summary>
    public Guid MedicalCaseId
    {
        get => _medicalCaseId;
        set => SetProperty(ref _medicalCaseId, value);
    }

    #endregion

    #region 处方核心字段

    private int _dosageCount = 7;
    /// <summary>
    /// 剂数
    /// </summary>
    public int DosageCount
    {
        get => _dosageCount;
        set
        {
            if (SetProperty(ref _dosageCount, value))
            {
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }
    }

    private string _usage = "水煎服，一日一剂，分早晚两次温服";
    /// <summary>
    /// 用法
    /// </summary>
    public string Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    }

    private string? _advice;
    /// <summary>
    /// 医嘱/用药建议
    /// </summary>
    public string? Advice
    {
        get => _advice;
        set => SetProperty(ref _advice, value);
    }

    private string? _referencedFormulas;
    /// <summary>
    /// 引用的验方名称列表，逗号分隔
    /// </summary>
    public string? ReferencedFormulas
    {
        get => _referencedFormulas;
        set => SetProperty(ref _referencedFormulas, value);
    }

    private string? _remark;
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    private decimal _discount = 1.0m;
    /// <summary>
    /// 折扣（0-1之间）
    /// </summary>
    public decimal Discount
    {
        get => _discount;
        set => SetProperty(ref _discount, value);
    }

    #endregion

    #region 价格字段

    private decimal _singleDosePrice;
    /// <summary>
    /// 单帖价格
    /// </summary>
    public decimal SingleDosePrice
    {
        get => _singleDosePrice;
        set
        {
            if (SetProperty(ref _singleDosePrice, value))
            {
                RaisePropertyChanged(nameof(TotalPrice));
            }
        }
    }

    private decimal _totalWeight;
    /// <summary>
    /// 总重量
    /// </summary>
    public decimal TotalWeight
    {
        get => _totalWeight;
        set => SetProperty(ref _totalWeight, value);
    }

    #endregion

    #region 药材列表

    private ObservableCollection<HerbItemDto> _items = new();
    /// <summary>
    /// 处方药材列表
    /// </summary>
    public ObservableCollection<HerbItemDto> Items
    {
        get => _items;
        set
        {
            if (SetProperty(ref _items, value))
            {
                RaisePropertyChanged(nameof(ItemCount));
                RaisePropertyChanged(nameof(HasItems));
                RaisePropertyChanged(nameof(IsValid));
                RaisePropertyChanged(nameof(TotalPrice));
                RaisePropertyChanged(nameof(SingleDosePrice));
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    #endregion

    #region 状态字段

    private CommonStatus _status = CommonStatus.Enabled;
    /// <summary>
    /// 状态
    /// </summary>
    public CommonStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    #endregion

    #region 审计字段

    private DateTime _createdAt = DateTime.Now;
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    private DateTime? _updatedAt;
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    #endregion

    #region 警告字段

    private string? _duplicateWarning;
    /// <summary>
    /// 重复用药警告
    /// </summary>
    public string? DuplicateWarning
    {
        get => _duplicateWarning;
        set => SetProperty(ref _duplicateWarning, value);
    }

    private string? _missingDrugWarning;
    /// <summary>
    /// 缺药警告
    /// </summary>
    public string? MissingDrugWarning
    {
        get => _missingDrugWarning;
        set => SetProperty(ref _missingDrugWarning, value);
    }

    #endregion

    #region UI状态字段

    private bool _isSelected;
    /// <summary>
    /// 是否选中（UI状态）
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _isExpanded;
    /// <summary>
    /// 是否展开（UI状态）
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isReadOnly;
    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set => SetProperty(ref _isReadOnly, value);
    }

    #endregion

    #region 计算属性

    /// <summary>
    /// 药材数量
    /// </summary>
    public int ItemCount => Items?.Count ?? 0;

    /// <summary>
    /// 是否有药材
    /// </summary>
    public bool HasItems => ItemCount > 0;

    /// <summary>
    /// 处方是否有效（至少有一种药材）
    /// </summary>
    public bool IsValid => HasItems;

    /// <summary>
    /// 总价格（单帖价格 * 剂数）
    /// </summary>
    public decimal TotalPrice => SingleDosePrice * DosageCount;

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText =>
        $"处方 {PrescriptionNumber ?? "新建"} - {ItemCount}味药材";

    #endregion

    #region 方法

    /// <summary>
    /// 清空处方数据
    /// </summary>
    public void Clear()
    {
        Id = Guid.Empty;
        PrescriptionNumber = null;
        DosageCount = 7;
        Usage = "水煎服，一日一剂，分早晚两次温服";
        Advice = null;
        ReferencedFormulas = null;
        Remark = null;
        Discount = 1.0m;
        SingleDosePrice = 0;
        TotalWeight = 0;
        DuplicateWarning = null;
        MissingDrugWarning = null;
        Items.Clear();

        RaisePropertyChanged(nameof(ItemCount));
        RaisePropertyChanged(nameof(HasItems));
        RaisePropertyChanged(nameof(IsValid));
        RaisePropertyChanged(nameof(TotalPrice));
        RaisePropertyChanged(nameof(DisplayText));
    }

    /// <summary>
    /// 通知药材列表相关属性更新
    /// </summary>
    public void NotifyItemsChanged()
    {
        RaisePropertyChanged(nameof(ItemCount));
        RaisePropertyChanged(nameof(HasItems));
        RaisePropertyChanged(nameof(IsValid));
        RaisePropertyChanged(nameof(TotalPrice));
        RaisePropertyChanged(nameof(DisplayText));
    }

    #endregion
}
