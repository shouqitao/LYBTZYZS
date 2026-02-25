// -----------------------------------------------------------------------
// <copyright file="FormulaMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping - Server端Mapperly映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Formulas.Mapping;

/// <summary>
/// 验方数据映射器 - Mapperly编译时生成
/// 替代原AutoMapper的FormulaMappingProfile
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FormulaMapper
{
    /// <summary>
    /// Formula实体转换为FormulaListDto（列表查询）
    /// </summary>
    /// <remarks>
    /// Indications映射自Indication字段
    /// HerbCount/TotalPrice由Service计算
    /// </remarks>
    [MapProperty(nameof(Formula.Indication), nameof(FormulaListDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaListDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaListDto.TotalPrice))]
    public partial FormulaListDto ToListDto(Formula entity);

    /// <summary>
    /// Formula实体列表转换为FormulaListDto列表
    /// </summary>
    public partial List<FormulaListDto> ToListDtos(List<Formula> entities);

    /// <summary>
    /// Formula实体转换为FormulaDetailDto（详情查询）
    /// </summary>
    /// <remarks>
    /// Indications映射自Indication字段
    /// HerbCount/TotalPrice由Service计算
    /// Herbs通过ToHerbItemDto/ToHerbItemDtos自动映射
    /// </remarks>
    [MapProperty(nameof(Formula.Indication), nameof(FormulaDetailDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Source))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Contraindications))]
    public partial FormulaDetailDto ToDetailDto(Formula entity);

    /// <summary>
    /// Formula实体列表转换为FormulaDetailDto列表
    /// </summary>
    public partial List<FormulaDetailDto> ToDetailDtos(List<Formula> entities);

    /// <summary>
    /// FormulaHerbItem实体转换为FormulaHerbItemDto
    /// </summary>
    /// <remarks>
    /// 以下字段由Service层填充：SpecialInstructions, SortOrder, Processing, Price, Preparation, Herb
    /// </remarks>
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.SpecialInstructions))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.SortOrder))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Processing))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Price))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Herb))]
    public partial FormulaHerbItemDto ToHerbItemDto(FormulaHerbItem entity);

    /// <summary>
    /// FormulaHerbItem实体列表转换为FormulaHerbItemDto列表
    /// </summary>
    public partial List<FormulaHerbItemDto> ToHerbItemDtos(List<FormulaHerbItem> entities);

    /// <summary>
    /// FormulaInputDto转换为Formula实体（创建）
    /// </summary>
    /// <remarks>
    /// 忽略Status、Property、Herbs等字段（由Service层管理）
    /// 忽略审计字段（由Service层自动设置）
    /// </remarks>
    [MapperIgnoreSource(nameof(FormulaInputDto.Id))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Indications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Contraindications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Preparation))]
    [MapperIgnoreTarget(nameof(Formula.Id))]
    [MapperIgnoreTarget(nameof(Formula.Status))]
    [MapperIgnoreTarget(nameof(Formula.Property))]
    [MapperIgnoreTarget(nameof(Formula.Herbs))]
    [MapperIgnoreTarget(nameof(Formula.CreatedAt))]
    [MapperIgnoreTarget(nameof(Formula.CreatedBy))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Formula.RowVersion))]
    [MapperIgnoreTarget(nameof(Formula.IsDeleted))]
    [MapperIgnoreTarget(nameof(Formula.ValidationStatus))]
    [MapperIgnoreTarget(nameof(Formula.UserId))]
    [MapperIgnoreTarget(nameof(Formula.Indication))]
    [MapperIgnoreTarget(nameof(Formula.FormulaType))]
    public partial Formula ToEntity(FormulaInputDto dto);

    /// <summary>
    /// FormulaInputDto更新到现有Formula实体
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaInputDto.Id))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Indications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Contraindications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Preparation))]
    [MapperIgnoreTarget(nameof(Formula.Id))]
    [MapperIgnoreTarget(nameof(Formula.Status))]
    [MapperIgnoreTarget(nameof(Formula.Property))]
    [MapperIgnoreTarget(nameof(Formula.Herbs))]
    [MapperIgnoreTarget(nameof(Formula.CreatedAt))]
    [MapperIgnoreTarget(nameof(Formula.CreatedBy))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Formula.RowVersion))]
    [MapperIgnoreTarget(nameof(Formula.IsDeleted))]
    [MapperIgnoreTarget(nameof(Formula.ValidationStatus))]
    [MapperIgnoreTarget(nameof(Formula.UserId))]
    [MapperIgnoreTarget(nameof(Formula.Indication))]
    [MapperIgnoreTarget(nameof(Formula.FormulaType))]
    public partial void UpdateEntity(FormulaInputDto dto, Formula entity);
}
