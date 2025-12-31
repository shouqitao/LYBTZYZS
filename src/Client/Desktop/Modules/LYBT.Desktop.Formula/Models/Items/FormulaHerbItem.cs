using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Formula.Models.Items;

/// <summary>
/// 验方中的药材项
/// 支持延迟绑定：HerbId可空
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
        set => SetProperty(ref _herbName, value);
    }

    private int _dosage;
    public int Dosage
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

    /// <summary>
    /// 排序顺序 - OpenSpec: unify-frontend-backend-types Phase 7
    /// 统一命名为SortOrder，与DTO保持一致
    /// </summary>
    private int _sortOrder;
    public int SortOrder
    {
        get => _sortOrder;
        set => SetProperty(ref _sortOrder, value);
    }

    /// <summary>
    /// 煎法（先煎、后下等）
    /// OpenSpec: herb-editor-control-refactoring - 补充缺失字段
    /// </summary>
    private DecocteMethod _decocteMethod = DecocteMethod.Default;
    public DecocteMethod DecocteMethod
    {
        get => _decocteMethod;
        set => SetProperty(ref _decocteMethod, value);
    }

    /// <summary>
    /// 从FormulaHerbItemDto创建
    /// OpenSpec: unify-frontend-backend-types Phase 7 - SortOrder直接映射
    /// </summary>
    public static FormulaHerbItem FromDto(FormulaHerbItemDto dto)
    {
        return new FormulaHerbItem
        {
            HerbId = dto.HerbId,
            HerbName = dto.HerbName,
            Dosage = dto.Dosage,
            Unit = dto.Unit,
            Usage = dto.Usage,
            SortOrder = dto.SortOrder,
            DecocteMethod = dto.DecocteMethod // OpenSpec: herb-editor-control-refactoring
        };
    }

    /// <summary>
    /// 转换为DTO
    /// OpenSpec: unify-frontend-backend-types Phase 7 - SortOrder直接映射
    /// </summary>
    public FormulaHerbItemDto ToDto()
    {
        return new FormulaHerbItemDto
        {
            HerbId = HerbId,
            HerbName = HerbName,
            Dosage = Dosage,
            Unit = Unit,
            Usage = Usage,
            SortOrder = SortOrder,
            DecocteMethod = DecocteMethod // OpenSpec: herb-editor-control-refactoring
        };
    }

    /// <summary>
    /// 显示文本
    /// </summary>
    public string DisplayText => $"{HerbName} {Dosage}{Unit}";
}
