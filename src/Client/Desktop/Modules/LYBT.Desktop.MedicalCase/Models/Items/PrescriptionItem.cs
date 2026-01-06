using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 处方数据Item - 用于UI绑定的处方数据模型
/// OpenSpec: consolidate-panel-viewmodels - 遵循Entity-DTO-Item模式
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
///
/// 遵循Entity-DTO-Item模式：
/// - Entity: 服务端Prescription实体
/// - DTO: PrescriptionDetailDto/PrescriptionInputDto (Shared层)
/// - Item: PrescriptionItem (Desktop层，用于XAML绑定)
///
/// 属性名与PrescriptionDetailDto保持一致，确保XAML绑定兼容
/// </summary>
public partial class PrescriptionItem : ObservableObject
{
    #region 基础标识字段

    /// <summary>
    /// 处方ID
    /// </summary>
    [ObservableProperty]
    private Guid _id = Guid.Empty;

    /// <summary>
    /// 处方编号（格式：RX-YYYYMMDD-NNNN）
    /// </summary>
    [ObservableProperty]
    private string? _prescriptionNumber;

    /// <summary>
    /// 关联的病历ID
    /// </summary>
    [ObservableProperty]
    private Guid _medicalCaseId = Guid.Empty;

    #endregion

    #region 处方核心字段

    /// <summary>
    /// 剂数
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private int _dosageCount = 7;

    /// <summary>
    /// 用法
    /// </summary>
    [ObservableProperty]
    private string _usage = "水煎服，一日一剂，分早晚两次温服";

    /// <summary>
    /// 医嘱/用药建议
    /// </summary>
    [ObservableProperty]
    private string? _advice;

    /// <summary>
    /// 引用的验方名称列表，逗号分隔
    /// </summary>
    [ObservableProperty]
    private string? _referencedFormulas;

    /// <summary>
    /// 备注
    /// </summary>
    [ObservableProperty]
    private string? _remark;

    /// <summary>
    /// 折扣（0-1之间）
    /// </summary>
    [ObservableProperty]
    private decimal _discount = 1.0m;

    #endregion

    #region 价格字段

    /// <summary>
    /// 单帖价格
    /// </summary>
    [ObservableProperty]
    private decimal _singleDosePrice;

    /// <summary>
    /// 总重量
    /// </summary>
    [ObservableProperty]
    private decimal _totalWeight;

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
                OnPropertyChanged(nameof(ItemCount));
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(SingleDosePrice));
            }
        }
    }

    #endregion

    #region 状态字段

    /// <summary>
    /// 状态
    /// </summary>
    [ObservableProperty]
    private CommonStatus _status = CommonStatus.Enabled;

    #endregion

    #region 审计字段

    /// <summary>
    /// 创建时间
    /// </summary>
    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    [ObservableProperty]
    private DateTime? _updatedAt;

    #endregion

    #region 警告字段

    /// <summary>
    /// 重复用药警告
    /// </summary>
    [ObservableProperty]
    private string? _duplicateWarning;

    /// <summary>
    /// 缺药警告
    /// </summary>
    [ObservableProperty]
    private string? _missingDrugWarning;

    #endregion

    #region UI状态字段

    /// <summary>
    /// 是否选中（UI状态）
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 是否展开（UI状态）
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// 是否只读
    /// </summary>
    [ObservableProperty]
    private bool _isReadOnly;

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

    #region 转换方法

    /// <summary>
    /// 从PrescriptionDetailDto创建PrescriptionItem
    /// </summary>
    /// <remarks>已废弃：请使用PrescriptionMappingService.ToItem()</remarks>
    [Obsolete("请使用PrescriptionMappingService.ToItem()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public static PrescriptionItem FromDto(PrescriptionDetailDto dto)
    {
        var item = new PrescriptionItem
        {
            Id = dto.Id,
            PrescriptionNumber = dto.PrescriptionNumber,
            MedicalCaseId = dto.MedicalCaseId,
            DosageCount = dto.DosageCount,
            Usage = dto.Usage ?? "水煎服，一日一剂，分早晚两次温服",
            Advice = dto.Advice,
            ReferencedFormulas = dto.ReferencedFormulas,
            Remark = dto.Remark,
            Discount = dto.Discount,
            SingleDosePrice = dto.SingleDosePrice,
            TotalWeight = dto.TotalWeight,
            Status = dto.Status,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            DuplicateWarning = dto.DuplicateWarning,
            MissingDrugWarning = dto.MissingDrugWarning
        };

        // 转换药材列表
        if (dto.Items != null)
        {
            foreach (var herbDto in dto.Items)
            {
                item.Items.Add(HerbItemDto.FromPrescriptionItemDto(herbDto));
            }
        }

        return item;
    }

    /// <summary>
    /// 转换为PrescriptionDetailDto（用于展示）
    /// </summary>
    /// <remarks>已废弃：请使用PrescriptionMappingService.ToDto()</remarks>
    [Obsolete("请使用PrescriptionMappingService.ToDto()替代。OpenSpec: adopt-mapperly-unified-mapping")]
    public PrescriptionDetailDto ToDto()
    {
        return new PrescriptionDetailDto
        {
            Id = Id,
            PrescriptionNumber = PrescriptionNumber,
            MedicalCaseId = MedicalCaseId,
            DosageCount = DosageCount,
            Usage = Usage,
            Advice = Advice,
            ReferencedFormulas = ReferencedFormulas,
            Remark = Remark,
            Discount = Discount,
            SingleDosePrice = SingleDosePrice,
            TotalPrice = TotalPrice,
            TotalWeight = TotalWeight,
            Status = Status,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DuplicateWarning = DuplicateWarning,
            MissingDrugWarning = MissingDrugWarning,
            Items = Items?.Select(h => h.ToPrescriptionItemDto()).ToList() ?? new()
        };
    }

    /// <summary>
    /// 转换为PrescriptionInputDto（用于保存）
    /// </summary>
    public PrescriptionInputDto ToInputDto()
    {
        return new PrescriptionInputDto
        {
            Id = Id == Guid.Empty ? null : Id,
            MedicalCaseId = MedicalCaseId,
            NeedsPrescription = HasItems,
            DosageCount = DosageCount,
            Usage = Usage,
            Advice = Advice,
            ReferencedFormulas = ReferencedFormulas,
            Discount = Discount,
            TotalPrice = TotalPrice,
            Remark = Remark,
            Items = Items?.Select(h => h.ToPrescriptionItemInputDto()).ToList() ?? new()
        };
    }

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

        OnPropertyChanged(nameof(ItemCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(TotalPrice));
    }

    #endregion
}
