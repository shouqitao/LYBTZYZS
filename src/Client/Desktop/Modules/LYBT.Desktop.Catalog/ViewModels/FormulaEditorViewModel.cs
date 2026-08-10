using System.Collections.ObjectModel;
using LYBT.Desktop.Catalog.Mappers;
using LYBT.Desktop.Catalog.Models.Items;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Catalog.ViewModels;

/// <summary>
/// 子 VM - 验方编辑
///
/// 封装 FormulaEditContext，提供 DTO 初始化和数据提取
/// 管理药材编辑列表
/// </summary>
public partial class FormulaEditorViewModel : EditorViewModelBase<FormulaEditContext>
{
    private readonly FormulaDetailModelMapper _mapper;
    private FormulaEditContext _formula = FormulaEditContext.CreateNew();
    private readonly ObservableCollection<FormulaHerbItemViewModel> _editHerbItems = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public FormulaEditorViewModel(FormulaDetailModelMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>验方编辑上下文 (XAML 绑定目标)</summary>
    public FormulaEditContext Formula
    {
        get => _formula;
        set => SetProperty(ref _formula, value);
    }

    /// <summary>编辑模式下的药材列表</summary>
    public ObservableCollection<FormulaHerbItemViewModel> EditHerbItems => _editHerbItems;

    /// <summary>药材数量</summary>
    public int HerbCount => _editHerbItems.Count(h => h.HerbId != Guid.Empty);

    protected override FormulaEditContext Context
    {
        get => Formula;
        set => Formula = value;
    }

    protected override FormulaEditContext CreateNewContext() => FormulaEditContext.CreateNew();

    /// <summary>
    /// 从 DTO 初始化 (查看/编辑已有验方)
    /// D1: 改用 Mapperly FormulaDetailModelMapper.ToEditContext，消除手写字段映射
    /// </summary>
    public void InitializeFromDto(FormulaDetailDto dto)
    {
        Formula = _mapper.ToEditContext(dto);
        IsDirty = false;

        // 初始化药材列表（编辑行 ViewModel，保留原逻辑）
        _editHerbItems.Clear();
        foreach (var herb in dto.Herbs ?? Enumerable.Empty<FormulaHerbItemDto>())
        {
            _editHerbItems.Add(new FormulaHerbItemViewModel
            {
                HerbId = herb.HerbId ?? Guid.Empty,
                HerbName = herb.HerbName ?? string.Empty,
                Dosage = herb.Dosage,
                Unit = herb.Unit ?? string.Empty,
                Remark = herb.ProcessingMethod,
                DecocteMethod = herb.DecocteMethod
            });
        }
        if (_editHerbItems.Count == 0)
        {
            _editHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty });
        }

        SubscribeContext();
        OnPropertyChanged(nameof(HerbCount));
    }

    /// <inheritdoc/>
    public override void InitializeForNewCase()
    {
        base.InitializeForNewCase();
        _editHerbItems.Clear();
        _editHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty });
        OnPropertyChanged(nameof(HerbCount));
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _editHerbItems.Clear();
        _editHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty });
        OnPropertyChanged(nameof(HerbCount));
    }

    /// <summary>
    /// 设置所有可用药材列表 (用于药材选择器)
    /// </summary>
    public void SetAllHerbs(IEnumerable<HerbListDto> allHerbs)
    {
        var list = allHerbs as ObservableCollection<HerbListDto> ?? new ObservableCollection<HerbListDto>(allHerbs);
        foreach (var item in _editHerbItems)
        {
            item.AllHerbs = list;
        }
    }

    /// <summary>
    /// 提取编辑数据为药材输入DTO列表 (用于保存)
    /// </summary>
    public List<FormulaHerbItemInputDto> GetHerbInputDtos()
    {
        return _editHerbItems
            .Where(h => h.HerbId != Guid.Empty || !string.IsNullOrWhiteSpace(h.HerbName))
            .Select(h => new FormulaHerbItemInputDto
            {
                HerbId = h.HerbId == Guid.Empty ? null : h.HerbId,
                HerbName = h.HerbName,
                Dosage = h.Dosage,
                Unit = h.Unit,
                ProcessingMethod = h.Remark,
                DecocteMethod = h.DecocteMethod
            })
            .ToList();
    }

    /// <summary>添加药材行</summary>
    public void AddHerb(IEnumerable<HerbListDto> allHerbs)
    {
        var list = allHerbs as ObservableCollection<HerbListDto> ?? new ObservableCollection<HerbListDto>(allHerbs);
        _editHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty, AllHerbs = list });
        OnPropertyChanged(nameof(HerbCount));
    }

    /// <summary>删除药材行</summary>
    public void DeleteHerb(FormulaHerbItemViewModel herb)
    {
        _editHerbItems.Remove(herb);
        OnPropertyChanged(nameof(HerbCount));
    }
}
