using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Formula.Models.Items;

/// <summary>
/// 验方中的药材项
/// 支持延迟绑定：HerbId可空
/// OpenSpec: resolve-mapperly-source-generator-conflict - 使用BindableBase确保Mapperly兼容
/// </summary>
public class FormulaHerbItem : BindableBase
{
    private Guid? _herbId;
    public Guid? HerbId
    {
        get => _herbId;
        set => SetProperty(ref _herbId, value);
    }

    private string _herbName = string.Empty;
    public string HerbName
    {
        get => _herbName;
        set
        {
            if (SetProperty(ref _herbName, value))
            {
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    private int _dosage;
    public int Dosage
    {
        get => _dosage;
        set
        {
            if (SetProperty(ref _dosage, value))
            {
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    private string _unit = string.Empty;
    public string Unit
    {
        get => _unit;
        set
        {
            if (SetProperty(ref _unit, value))
            {
                RaisePropertyChanged(nameof(DisplayText));
            }
        }
    }

    private string? _usage;
    public string? Usage
    {
        get => _usage;
        set => SetProperty(ref _usage, value);
    }

    /// <summary>
    /// 排序顺序
    /// </summary>
    private int _sortOrder;
    public int SortOrder
    {
        get => _sortOrder;
        set => SetProperty(ref _sortOrder, value);
    }

    /// <summary>
    /// 煎法（先煎、后下等）
    /// </summary>
    private DecocteMethod _decocteMethod = DecocteMethod.Default;
    public DecocteMethod DecocteMethod
    {
        get => _decocteMethod;
        set => SetProperty(ref _decocteMethod, value);
    }

    #region 计算属性

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText => $"{HerbName} {Dosage}{Unit}";

    #endregion
}
