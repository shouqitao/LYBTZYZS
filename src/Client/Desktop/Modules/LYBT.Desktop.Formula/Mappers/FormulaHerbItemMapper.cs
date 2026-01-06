// -----------------------------------------------------------------------
// <copyright file="FormulaHerbItemMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Formula.Models.Items;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Formula.Mappers;

/// <summary>
/// 验方药材项数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - FormulaHerbItemDto → FormulaHerbItem (从API加载)
/// - FormulaHerbItem → FormulaHerbItemDto (保存到API)
/// </remarks>
[Mapper]
public partial class FormulaHerbItemMapper
{
    /// <summary>
    /// 将FormulaHerbItemDto转换为FormulaHerbItem。
    /// </summary>
    /// <param name="dto">API返回的DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    /// <remarks>
    /// DTO中的Herb导航属性不映射到Item。
    /// </remarks>
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Id))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Herb))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.OriginalHerbName))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.IsValidated))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Preparation))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Processing))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Price))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.UnitPrice))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.ProcessingMethod))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.SpecialInstructions))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.DisplayText))]
    public partial FormulaHerbItem ToItem(FormulaHerbItemDto dto);

    /// <summary>
    /// 将FormulaHerbItem转换为FormulaHerbItemDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaHerbItem.DisplayText))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Id))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Herb))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.OriginalHerbName))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.IsValidated))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Processing))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Price))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.UnitPrice))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.ProcessingMethod))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.SpecialInstructions))]
    public partial FormulaHerbItemDto ToDto(FormulaHerbItem item);

    /// <summary>
    /// 将FormulaHerbItem转换为FormulaHerbItemInputDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaHerbItem.DisplayText))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemInputDto.Id))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemInputDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemInputDto.ProcessingMethod))]
    public partial FormulaHerbItemInputDto ToInputDto(FormulaHerbItem item);
}
