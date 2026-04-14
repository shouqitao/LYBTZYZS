// -----------------------------------------------------------------------
// <copyright file="HerbMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping - Server端Mapperly映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Herbs.Mapping;

/// <summary>
/// 药材数据映射器 - Mapperly编译时生成
/// 替代原AutoMapper的HerbMappingProfile
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class HerbMapper
{
    /// <summary>
    /// Herb实体转换为HerbListDto（列表查询）
    /// </summary>
    public partial HerbListDto ToListDto(Herb entity);

    /// <summary>
    /// Herb实体列表转换为HerbListDto列表
    /// </summary>
    public partial List<HerbListDto> ToListDtos(List<Herb> entities);

    /// <summary>
    /// Herb实体转换为HerbDetailDto（详情查询）
    /// </summary>
    public partial HerbDetailDto ToDetailDto(Herb entity);

    /// <summary>
    /// Herb实体列表转换为HerbDetailDto列表
    /// </summary>
    /// <remarks>
    /// 注：集合映射自动继承元素映射器ToDetailDto的忽略配置，无需重复声明
    /// </remarks>
    public partial List<HerbDetailDto> ToDetailDtos(List<Herb> entities);

    /// <summary>
    /// HerbInputDto转换为Herb实体（创建）
    /// </summary>
    /// <remarks>
    /// 忽略审计字段（由Service层自动设置）
    /// 忽略Status字段（通过专用API修改）
    /// </remarks>
    [MapperIgnoreSource(nameof(HerbInputDto.Id))]
    [MapperIgnoreTarget(nameof(Herb.Id))]
    [MapperIgnoreTarget(nameof(Herb.Status))]
    [MapperIgnoreTarget(nameof(Herb.CreatedAt))]
    [MapperIgnoreTarget(nameof(Herb.CreatedBy))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Herb.RowVersion))]
    [MapperIgnoreTarget(nameof(Herb.IsDeleted))]
    public partial Herb ToEntity(HerbInputDto dto);

    /// <summary>
    /// HerbInputDto更新到现有Herb实体
    /// </summary>
    [MapperIgnoreSource(nameof(HerbInputDto.Id))]
    [MapperIgnoreTarget(nameof(Herb.Id))]
    [MapperIgnoreTarget(nameof(Herb.Status))]
    [MapperIgnoreTarget(nameof(Herb.CreatedAt))]
    [MapperIgnoreTarget(nameof(Herb.CreatedBy))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Herb.RowVersion))]
    [MapperIgnoreTarget(nameof(Herb.IsDeleted))]
    public partial void UpdateEntity(HerbInputDto dto, Herb entity);

    /// <summary>
    /// HerbImportItemDto转换为Herb实体（批量导入）
    /// </summary>
    [MapperIgnoreTarget(nameof(Herb.Id))]
    [MapperIgnoreTarget(nameof(Herb.Status))]
    [MapperIgnoreTarget(nameof(Herb.Properties))]
    [MapperIgnoreTarget(nameof(Herb.Usage))]
    [MapperIgnoreTarget(nameof(Herb.PinYinCode))]
    [MapperIgnoreTarget(nameof(Herb.CostPrice))]
    [MapperIgnoreTarget(nameof(Herb.Category))]
    [MapperIgnoreTarget(nameof(Herb.CreatedAt))]
    [MapperIgnoreTarget(nameof(Herb.CreatedBy))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Herb.RowVersion))]
    [MapperIgnoreTarget(nameof(Herb.IsDeleted))]
    public partial Herb ToEntityFromImport(HerbImportItemDto dto);
}
