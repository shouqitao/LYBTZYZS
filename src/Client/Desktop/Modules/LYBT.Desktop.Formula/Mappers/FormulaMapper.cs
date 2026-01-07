// -----------------------------------------------------------------------
// <copyright file="FormulaMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Formula.Models.Items;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Formula.Mappers;

/// <summary>
/// 验方数据映射器 - Mapperly实现。
/// </summary>
/// <remarks>
/// 映射关系：
/// - FormulaDetailDto → FormulaItem (从API加载)
/// - FormulaListDto → FormulaItem (从列表API加载)
/// - FormulaItem → FormulaDetailDto (保存到API)
/// - FormulaItem → FormulaInputDto (创建/更新API调用)
///
/// OpenSpec: resolve-mapperly-source-generator-conflict
/// Item类使用BindableBase+显式属性，确保Mapperly能正确生成映射代码。
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FormulaMapper
{
    private readonly FormulaHerbItemMapper _herbMapper = new();

    #region FormulaDetailDto <-> FormulaItem

    /// <summary>
    /// 将FormulaDetailDto转换为FormulaItem。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    [MapperIgnoreTarget(nameof(FormulaItem.IsSelected))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsExpanded))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsFavorite))]
    [MapperIgnoreTarget(nameof(FormulaItem.Pinyin))]
    [MapperIgnoreTarget(nameof(FormulaItem.Composition))]
    [MapperIgnoreTarget(nameof(FormulaItem.Modification))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsClassic))]
    [MapperIgnoreTarget(nameof(FormulaItem.UsageCount))]
    [MapperIgnoreTarget(nameof(FormulaItem.Herbs))] // 手动映射集合
    [MapperIgnoreTarget(nameof(FormulaItem.IsActive))]
    [MapperIgnoreTarget(nameof(FormulaItem.TypeText))]
    [MapperIgnoreTarget(nameof(FormulaItem.TypeColor))]
    [MapperIgnoreTarget(nameof(FormulaItem.StatusText))]
    [MapperIgnoreTarget(nameof(FormulaItem.StatusColor))]
    [MapperIgnoreTarget(nameof(FormulaItem.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaItem.HerbCompositionText))]
    [MapperIgnoreTarget(nameof(FormulaItem.DisplayText))]
    [MapperIgnoreTarget(nameof(FormulaItem.SearchText))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsAvailable))]
    [MapperIgnoreTarget(nameof(FormulaItem.HasContraindication))]
    [MapperIgnoreTarget(nameof(FormulaItem.HasModification))]
    [MapperIgnoreTarget(nameof(FormulaItem.PopularityLevel))]
    [MapperIgnoreTarget(nameof(FormulaItem.PopularityColor))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Property))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.ValidationStatus))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbNames))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Herbs))] // 手动映射集合
    [MapperIgnoreSource(nameof(FormulaDetailDto.IsEnabled))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.IsShared))] // 手动映射为IsPersonal
    [MapperIgnoreTarget(nameof(FormulaItem.IsPersonal))] // 手动映射
    private partial FormulaItem ToItemCore(FormulaDetailDto dto);

    /// <summary>
    /// 将FormulaDetailDto转换为FormulaItem（含Herbs集合映射）。
    /// </summary>
    public FormulaItem ToItem(FormulaDetailDto dto)
    {
        var item = ToItemCore(dto);
        item.IsPersonal = !dto.IsShared;

        // 手动映射Herbs集合
        if (dto.Herbs != null)
        {
            item.Herbs = dto.Herbs.Select(h => _herbMapper.ToItem(h)).ToList();
        }

        return item;
    }

    /// <summary>
    /// 将FormulaItem转换为FormulaDetailDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaItem.IsSelected))]
    [MapperIgnoreSource(nameof(FormulaItem.IsExpanded))]
    [MapperIgnoreSource(nameof(FormulaItem.IsFavorite))]
    [MapperIgnoreSource(nameof(FormulaItem.Pinyin))]
    [MapperIgnoreSource(nameof(FormulaItem.Composition))]
    [MapperIgnoreSource(nameof(FormulaItem.Modification))]
    [MapperIgnoreSource(nameof(FormulaItem.IsClassic))]
    [MapperIgnoreSource(nameof(FormulaItem.IsPersonal))] // 手动映射为IsShared
    [MapperIgnoreSource(nameof(FormulaItem.UsageCount))]
    [MapperIgnoreSource(nameof(FormulaItem.Herbs))] // 手动映射集合
    [MapperIgnoreSource(nameof(FormulaItem.IsActive))]
    [MapperIgnoreSource(nameof(FormulaItem.TypeText))]
    [MapperIgnoreSource(nameof(FormulaItem.TypeColor))]
    [MapperIgnoreSource(nameof(FormulaItem.StatusText))]
    [MapperIgnoreSource(nameof(FormulaItem.StatusColor))]
    [MapperIgnoreSource(nameof(FormulaItem.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaItem.HerbCompositionText))]
    [MapperIgnoreSource(nameof(FormulaItem.DisplayText))]
    [MapperIgnoreSource(nameof(FormulaItem.SearchText))]
    [MapperIgnoreSource(nameof(FormulaItem.IsAvailable))]
    [MapperIgnoreSource(nameof(FormulaItem.HasContraindication))]
    [MapperIgnoreSource(nameof(FormulaItem.HasModification))]
    [MapperIgnoreSource(nameof(FormulaItem.PopularityLevel))]
    [MapperIgnoreSource(nameof(FormulaItem.PopularityColor))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Property))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.ValidationStatus))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.IsShared))] // 手动映射
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Herbs))] // 手动映射集合
    private partial FormulaDetailDto ToDtoCore(FormulaItem item);

    /// <summary>
    /// 将FormulaItem转换为FormulaDetailDto（含Herbs集合映射）。
    /// </summary>
    public FormulaDetailDto ToDto(FormulaItem item)
    {
        var dto = ToDtoCore(item);
        dto.IsShared = !item.IsPersonal;

        // 手动映射Herbs集合
        dto.Herbs = item.Herbs?.Select(h => _herbMapper.ToDto(h)).ToList() ?? new();

        return dto;
    }

    #endregion

    #region FormulaListDto -> FormulaItem

    /// <summary>
    /// 将FormulaListDto转换为FormulaItem。
    /// </summary>
    [MapperIgnoreTarget(nameof(FormulaItem.IsSelected))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsExpanded))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsFavorite))]
    [MapperIgnoreTarget(nameof(FormulaItem.Pinyin))]
    [MapperIgnoreTarget(nameof(FormulaItem.Source))]
    [MapperIgnoreTarget(nameof(FormulaItem.Composition))]
    [MapperIgnoreTarget(nameof(FormulaItem.Usage))]
    [MapperIgnoreTarget(nameof(FormulaItem.Modification))]
    [MapperIgnoreTarget(nameof(FormulaItem.Contraindications))]
    [MapperIgnoreTarget(nameof(FormulaItem.Remark))]
    [MapperIgnoreTarget(nameof(FormulaItem.CreatedBy))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsClassic))]
    [MapperIgnoreTarget(nameof(FormulaItem.UsageCount))]
    [MapperIgnoreTarget(nameof(FormulaItem.UpdatedAt))]
    [MapperIgnoreTarget(nameof(FormulaItem.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsActive))]
    [MapperIgnoreTarget(nameof(FormulaItem.TypeText))]
    [MapperIgnoreTarget(nameof(FormulaItem.TypeColor))]
    [MapperIgnoreTarget(nameof(FormulaItem.StatusText))]
    [MapperIgnoreTarget(nameof(FormulaItem.StatusColor))]
    [MapperIgnoreTarget(nameof(FormulaItem.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaItem.HerbCompositionText))]
    [MapperIgnoreTarget(nameof(FormulaItem.DisplayText))]
    [MapperIgnoreTarget(nameof(FormulaItem.SearchText))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsAvailable))]
    [MapperIgnoreTarget(nameof(FormulaItem.HasContraindication))]
    [MapperIgnoreTarget(nameof(FormulaItem.HasModification))]
    [MapperIgnoreTarget(nameof(FormulaItem.PopularityLevel))]
    [MapperIgnoreTarget(nameof(FormulaItem.PopularityColor))]
    [MapperIgnoreSource(nameof(FormulaListDto.ValidationStatus))]
    [MapperIgnoreSource(nameof(FormulaListDto.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaListDto.TotalPrice))]
    [MapperIgnoreSource(nameof(FormulaListDto.IsShared))] // 手动映射为IsPersonal
    [MapperIgnoreTarget(nameof(FormulaItem.IsPersonal))] // 手动映射
    private partial FormulaItem ToItemFromListCore(FormulaListDto dto);

    /// <summary>
    /// 将FormulaListDto转换为FormulaItem。
    /// </summary>
    public FormulaItem ToItem(FormulaListDto dto)
    {
        var item = ToItemFromListCore(dto);
        item.IsPersonal = !dto.IsShared;
        return item;
    }

    #endregion

    #region FormulaItem -> FormulaInputDto

    /// <summary>
    /// 将FormulaItem转换为FormulaInputDto。
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaItem.Id))] // 手动处理
    [MapperIgnoreSource(nameof(FormulaItem.IsSelected))]
    [MapperIgnoreSource(nameof(FormulaItem.IsExpanded))]
    [MapperIgnoreSource(nameof(FormulaItem.IsFavorite))]
    [MapperIgnoreSource(nameof(FormulaItem.Pinyin))]
    [MapperIgnoreSource(nameof(FormulaItem.Source))]
    [MapperIgnoreSource(nameof(FormulaItem.Composition))]
    [MapperIgnoreSource(nameof(FormulaItem.Modification))]
    [MapperIgnoreSource(nameof(FormulaItem.CreatedBy))]
    [MapperIgnoreSource(nameof(FormulaItem.IsClassic))]
    [MapperIgnoreSource(nameof(FormulaItem.IsPersonal))] // 手动映射为IsShared
    [MapperIgnoreSource(nameof(FormulaItem.Status))]
    [MapperIgnoreSource(nameof(FormulaItem.UsageCount))]
    [MapperIgnoreSource(nameof(FormulaItem.CreatedAt))]
    [MapperIgnoreSource(nameof(FormulaItem.UpdatedAt))]
    [MapperIgnoreSource(nameof(FormulaItem.Herbs))] // 手动映射集合
    [MapperIgnoreSource(nameof(FormulaItem.IsActive))]
    [MapperIgnoreSource(nameof(FormulaItem.TypeText))]
    [MapperIgnoreSource(nameof(FormulaItem.TypeColor))]
    [MapperIgnoreSource(nameof(FormulaItem.StatusText))]
    [MapperIgnoreSource(nameof(FormulaItem.StatusColor))]
    [MapperIgnoreSource(nameof(FormulaItem.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaItem.HerbCompositionText))]
    [MapperIgnoreSource(nameof(FormulaItem.DisplayText))]
    [MapperIgnoreSource(nameof(FormulaItem.SearchText))]
    [MapperIgnoreSource(nameof(FormulaItem.IsAvailable))]
    [MapperIgnoreSource(nameof(FormulaItem.HasContraindication))]
    [MapperIgnoreSource(nameof(FormulaItem.HasModification))]
    [MapperIgnoreSource(nameof(FormulaItem.PopularityLevel))]
    [MapperIgnoreSource(nameof(FormulaItem.PopularityColor))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Id))] // 手动处理
    [MapperIgnoreTarget(nameof(FormulaInputDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Property))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.IsShared))] // 手动映射
    [MapperIgnoreTarget(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Herbs))] // 手动映射集合
    private partial FormulaInputDto ToInputDtoCore(FormulaItem item);

    /// <summary>
    /// 将FormulaItem转换为FormulaInputDto（含特殊字段处理）。
    /// </summary>
    public FormulaInputDto ToInputDto(FormulaItem item)
    {
        var dto = ToInputDtoCore(item);
        dto.Id = item.Id == Guid.Empty ? null : item.Id;
        dto.IsShared = !item.IsPersonal;
        dto.Effect ??= string.Empty;
        dto.Usage ??= string.Empty;

        // 手动映射Herbs集合
        dto.Herbs = item.Herbs?.Select(h => _herbMapper.ToInputDto(h)).ToList() ?? new();

        return dto;
    }

    #endregion
}
