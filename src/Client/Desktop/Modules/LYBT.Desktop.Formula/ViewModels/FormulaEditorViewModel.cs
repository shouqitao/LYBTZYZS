using System.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Formula.Models.Items;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Formula.ViewModels;

/// <summary>
/// 子 VM - 验方编辑
/// OpenSpec: frontend-architecture-unification
///
/// 封装 FormulaEditContext，提供 DTO 初始化和数据提取
/// 管理药材编辑列表
/// </summary>
public partial class FormulaEditorViewModel : ObservableObject
{
    private FormulaEditContext _formula = FormulaEditContext.CreateNew();
    private readonly ObservableCollection<FormulaHerbItemViewModel> _editHerbItems = new();

    /// <summary>验方编辑上下文 (XAML 绑定目标)</summary>
    public FormulaEditContext Formula
    {
        get => _formula;
        set => SetProperty(ref _formula, value);
    }

    /// <summary>编辑模式下的药材列表</summary>
    public ObservableCollection<FormulaHerbItemViewModel> EditHerbItems => _editHerbItems;

    /// <summary>是否已修改 (脏数据标记)</summary>
    public bool IsDirty { get; private set; }

    /// <summary>药材数量</summary>
    public int HerbCount => _editHerbItems.Count(h => h.HerbId != Guid.Empty);

    /// <summary>
    /// 从 DTO 初始化 (查看/编辑已有验方)
    /// </summary>
    public void InitializeFromDto(FormulaDetailDto dto)
    {
        var context = new FormulaEditContext
        {
            Id = dto.Id,
            Name = dto.Name,
            Category = dto.Category,
            Property = dto.Property,
            Effect = dto.Effect,
            Usage = dto.Usage,
            Remark = dto.Remark,
            IsShared = dto.IsShared
        };

        Formula = context;
        IsDirty = false;

        // 初始化药材列表
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

        Formula.PropertyChanged += OnFormulaPropertyChanged;
        OnPropertyChanged(nameof(HerbCount));
    }

    /// <summary>
    /// 初始化为新验方 (新建场景)
    /// </summary>
    public void InitializeForNewCase()
    {
        Formula = FormulaEditContext.CreateNew();
        _editHerbItems.Clear();
        _editHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty });
        IsDirty = false;
        Formula.PropertyChanged += OnFormulaPropertyChanged;
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

    /// <summary>验证编辑内容</summary>
    public bool Validate()
    {
        return Formula.ValidateAll();
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

    /// <summary>重置编辑状态</summary>
    public void Reset()
    {
        Formula.PropertyChanged -= OnFormulaPropertyChanged;
        Formula = FormulaEditContext.CreateNew();
        _editHerbItems.Clear();
        _editHerbItems.Add(new FormulaHerbItemViewModel { Unit = string.Empty });
        IsDirty = false;
        OnPropertyChanged(nameof(HerbCount));
    }

    private void OnFormulaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsDirty = true;
    }
}
