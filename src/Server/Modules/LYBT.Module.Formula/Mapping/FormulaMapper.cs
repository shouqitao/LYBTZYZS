// -----------------------------------------------------------------------
// <copyright file="FormulaMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;
using FormulaEntity = LYBT.Entities.Formulas.Formula;

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
    [MapProperty(nameof(FormulaEntity.Indication), nameof(FormulaListDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaListDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaListDto.TotalPrice))]
    public partial FormulaListDto ToListDto(FormulaEntity entity);

    /// <summary>
    /// Formula实体列表转换为FormulaListDto列表
    /// </summary>
    public partial List<FormulaListDto> ToListDtos(List<FormulaEntity> entities);

    /// <summary>
    /// Formula实体转换为FormulaDetailDto（详情查询）
    /// </summary>
    /// <remarks>
    /// Indications映射自Indication字段
    /// HerbCount/TotalPrice由Service计算
    /// Herbs通过ToHerbItemDto/ToHerbItemDtos自动映射
    /// </remarks>
    [MapProperty(nameof(FormulaEntity.Indication), nameof(FormulaDetailDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Source))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Contraindications))]
    public partial FormulaDetailDto ToDetailDto(FormulaEntity entity);

    /// <summary>
    /// Formula实体列表转换为FormulaDetailDto列表
    /// </summary>
    public partial List<FormulaDetailDto> ToDetailDtos(List<FormulaEntity> entities);

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
    [MapperIgnoreTarget(nameof(FormulaEntity.Id))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Status))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Property))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(FormulaEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(FormulaEntity.UpdatedAt))]
    [MapperIgnoreTarget(nameof(FormulaEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(FormulaEntity.RowVersion))]
    [MapperIgnoreTarget(nameof(FormulaEntity.IsDeleted))]
    [MapperIgnoreTarget(nameof(FormulaEntity.ValidationStatus))]
    [MapperIgnoreTarget(nameof(FormulaEntity.UserId))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Indication))]
    [MapperIgnoreTarget(nameof(FormulaEntity.FormulaType))]
    public partial FormulaEntity ToEntity(FormulaInputDto dto);

    /// <summary>
    /// FormulaInputDto更新到现有Formula实体
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaInputDto.Id))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Indications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Contraindications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Id))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Status))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Property))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaEntity.CreatedAt))]
    [MapperIgnoreTarget(nameof(FormulaEntity.CreatedBy))]
    [MapperIgnoreTarget(nameof(FormulaEntity.UpdatedAt))]
    [MapperIgnoreTarget(nameof(FormulaEntity.UpdatedBy))]
    [MapperIgnoreTarget(nameof(FormulaEntity.RowVersion))]
    [MapperIgnoreTarget(nameof(FormulaEntity.IsDeleted))]
    [MapperIgnoreTarget(nameof(FormulaEntity.ValidationStatus))]
    [MapperIgnoreTarget(nameof(FormulaEntity.UserId))]
    [MapperIgnoreTarget(nameof(FormulaEntity.Indication))]
    [MapperIgnoreTarget(nameof(FormulaEntity.FormulaType))]
    public partial void UpdateEntity(FormulaInputDto dto, FormulaEntity entity);
}


