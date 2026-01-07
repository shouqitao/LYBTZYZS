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
/// 验方药材项数据映射器 - Mapperly实现。
/// </summary>
/// <remarks>
/// 映射关系：
/// - FormulaHerbItemDto → FormulaHerbItem (从API加载)
/// - FormulaHerbItem → FormulaHerbItemDto (保存到API)
/// - FormulaHerbItem → FormulaHerbItemInputDto (创建/更新API调用)
///
/// OpenSpec: resolve-mapperly-source-generator-conflict
/// Item类使用BindableBase+显式属性，确保Mapperly能正确生成映射代码。
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FormulaHerbItemMapper
{
    #region FormulaHerbItemDto -> FormulaHerbItem

    /// <summary>
    /// 将FormulaHerbItemDto转换为FormulaHerbItem。
    /// </summary>
    /// <param name="dto">API返回的DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    /// <remarks>
    /// DTO中的Herb导航属性不映射到Item。
    /// </remarks>
    [MapperIgnoreTarget(nameof(FormulaHerbItem.DisplayText))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Id))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.OriginalHerbName))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.IsValidated))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Preparation))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Processing))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.ProcessingMethod))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.SpecialInstructions))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Price))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.UnitPrice))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Herb))]
    public partial FormulaHerbItem ToItem(FormulaHerbItemDto dto);

    #endregion

    #region FormulaHerbItem -> FormulaHerbItemDto

    /// <summary>
    /// 将FormulaHerbItem转换为FormulaHerbItemDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaHerbItem.DisplayText))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Id))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.OriginalHerbName))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.IsValidated))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Processing))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.ProcessingMethod))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.SpecialInstructions))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Price))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Herb))]
    public partial FormulaHerbItemDto ToDto(FormulaHerbItem item);

    #endregion

    #region FormulaHerbItem -> FormulaHerbItemInputDto

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

    #endregion
}
