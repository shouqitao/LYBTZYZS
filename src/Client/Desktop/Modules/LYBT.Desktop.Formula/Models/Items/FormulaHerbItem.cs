using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Formula.Models.Items;

/// <summary>
/// 验方中的药材项
/// 支持延迟绑定：HerbId可空
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
/// </summary>
public partial class FormulaHerbItem : ObservableObject
{
    [ObservableProperty]
    private Guid? _herbId;

    [ObservableProperty]
    private string _herbName = string.Empty;

    [ObservableProperty]
    private int _dosage;

    [ObservableProperty]
    private string _unit = string.Empty;

    [ObservableProperty]
    private string? _usage;

    /// <summary>
    /// 排序顺序 - OpenSpec: unify-frontend-backend-types Phase 7
    /// 统一命名为SortOrder，与DTO保持一致
    /// </summary>
    [ObservableProperty]
    private int _sortOrder;

    /// <summary>
    /// 煎法（先煎、后下等）
    /// OpenSpec: herb-editor-control-refactoring - 补充缺失字段
    /// </summary>
    [ObservableProperty]
    private DecocteMethod _decocteMethod = DecocteMethod.Default;

    /// <summary>
    /// 从FormulaHerbItemDto创建
    /// OpenSpec: unify-frontend-backend-types Phase 7 - SortOrder直接映射
    /// </summary>
    /// <remarks>已废弃：请使用FormulaHerbItemMapper.ToItem()</remarks>
    [Obsolete("请使用FormulaHerbItemMapper.ToItem()替代。OpenSpec: adopt-mapperly-unified-mapping")]
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
    /// <remarks>已废弃：请使用FormulaHerbItemMapper.ToDto()</remarks>
    [Obsolete("请使用FormulaHerbItemMapper.ToDto()替代。OpenSpec: adopt-mapperly-unified-mapping")]
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
