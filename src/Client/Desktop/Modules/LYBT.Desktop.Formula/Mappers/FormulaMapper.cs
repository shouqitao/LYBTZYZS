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
/// 验方数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - FormulaDetailDto → FormulaItem (从API加载)
/// - FormulaItem → FormulaDetailDto (保存到API)
/// - FormulaItem → FormulaInputDto (创建/更新API调用)
///
/// 注意：Herbs集合需要自定义映射。
/// </remarks>
[Mapper]
public partial class FormulaMapper
{
    private readonly FormulaHerbItemMapper _herbMapper = new();

    /// <summary>
    /// 将FormulaDetailDto转换为FormulaItem（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    /// <remarks>
    /// 忽略Item中不存在于DTO的属性（Pinyin、Source、Composition等来自其他数据源）。
    /// 忽略UI状态字段（IsSelected、IsExpanded、IsFavorite）。
    /// </remarks>
    [MapperIgnoreSource(nameof(FormulaDetailDto.Herbs))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbNames))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.IsEnabled))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.ValidationStatus))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Property))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.IsShared))]
    [MapperIgnoreTarget(nameof(FormulaItem.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaItem.Pinyin))]
    [MapperIgnoreTarget(nameof(FormulaItem.Composition))]
    [MapperIgnoreTarget(nameof(FormulaItem.Modification))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsClassic))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsPersonal))]
    [MapperIgnoreTarget(nameof(FormulaItem.UsageCount))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsSelected))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsExpanded))]
    [MapperIgnoreTarget(nameof(FormulaItem.IsFavorite))]
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
    [MapperIgnoreTarget(nameof(FormulaItem.IsActive))]
    public partial FormulaItem ToItemCore(FormulaDetailDto dto);

    /// <summary>
    /// 将FormulaDetailDto转换为FormulaItem（完整映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    public FormulaItem ToItem(FormulaDetailDto dto)
    {
        var item = ToItemCore(dto);

        // 设置派生属性
        item.IsPersonal = !dto.IsShared;

        // 手动映射Herbs集合
        if (dto.Herbs != null)
        {
            item.Herbs = dto.Herbs.Select(h => _herbMapper.ToItem(h)).ToList();
        }

        return item;
    }

    /// <summary>
    /// 将FormulaItem转换为FormulaDetailDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaItem.Herbs))]
    [MapperIgnoreSource(nameof(FormulaItem.Pinyin))]
    [MapperIgnoreSource(nameof(FormulaItem.Composition))]
    [MapperIgnoreSource(nameof(FormulaItem.Modification))]
    [MapperIgnoreSource(nameof(FormulaItem.IsClassic))]
    [MapperIgnoreSource(nameof(FormulaItem.IsPersonal))]
    [MapperIgnoreSource(nameof(FormulaItem.UsageCount))]
    [MapperIgnoreSource(nameof(FormulaItem.IsSelected))]
    [MapperIgnoreSource(nameof(FormulaItem.IsExpanded))]
    [MapperIgnoreSource(nameof(FormulaItem.IsFavorite))]
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
    [MapperIgnoreSource(nameof(FormulaItem.IsActive))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbNames))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.IsEnabled))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.ValidationStatus))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Property))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.IsShared))]
    public partial FormulaDetailDto ToDtoCore(FormulaItem item);

    /// <summary>
    /// 将FormulaItem转换为FormulaDetailDto（完整映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public FormulaDetailDto ToDto(FormulaItem item)
    {
        var dto = ToDtoCore(item);

        // 设置派生属性
        dto.IsShared = !item.IsPersonal;

        // 手动映射Herbs集合
        dto.Herbs = item.Herbs?.Select(h => _herbMapper.ToDto(h)).ToList() ?? new();

        return dto;
    }

    /// <summary>
    /// 将FormulaItem转换为FormulaInputDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaItem.Herbs))]
    [MapperIgnoreSource(nameof(FormulaItem.Id))]
    [MapperIgnoreSource(nameof(FormulaItem.Pinyin))]
    [MapperIgnoreSource(nameof(FormulaItem.Composition))]
    [MapperIgnoreSource(nameof(FormulaItem.Modification))]
    [MapperIgnoreSource(nameof(FormulaItem.IsClassic))]
    [MapperIgnoreSource(nameof(FormulaItem.IsPersonal))]
    [MapperIgnoreSource(nameof(FormulaItem.UsageCount))]
    [MapperIgnoreSource(nameof(FormulaItem.IsSelected))]
    [MapperIgnoreSource(nameof(FormulaItem.IsExpanded))]
    [MapperIgnoreSource(nameof(FormulaItem.IsFavorite))]
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
    [MapperIgnoreSource(nameof(FormulaItem.IsActive))]
    [MapperIgnoreSource(nameof(FormulaItem.Status))]
    [MapperIgnoreSource(nameof(FormulaItem.CreatedBy))]
    [MapperIgnoreSource(nameof(FormulaItem.CreatedAt))]
    [MapperIgnoreSource(nameof(FormulaItem.UpdatedAt))]
    [MapperIgnoreSource(nameof(FormulaItem.Source))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Id))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.IsShared))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Property))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Preparation))]
    public partial FormulaInputDto ToInputDtoCore(FormulaItem item);

    /// <summary>
    /// 将FormulaItem转换为FormulaInputDto（完整映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    public FormulaInputDto ToInputDto(FormulaItem item)
    {
        var dto = ToInputDtoCore(item);

        // 设置Id（空Guid转为null表示创建）
        dto.Id = item.Id == Guid.Empty ? null : item.Id;

        // 设置派生属性
        dto.IsShared = !item.IsPersonal;

        // 手动映射Herbs集合
        dto.Herbs = item.Herbs?.Select(h => _herbMapper.ToInputDto(h)).ToList() ?? new();

        return dto;
    }
}
