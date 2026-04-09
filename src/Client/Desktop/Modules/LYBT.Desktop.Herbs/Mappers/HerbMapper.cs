// -----------------------------------------------------------------------
// <copyright file="HerbMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Herbs.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Herbs.Mappers;

/// <summary>
/// 药材数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - HerbDetailDto → HerbDetailModel (从API加载)
/// - HerbDetailModel → HerbDetailDto (保存到API)
/// - HerbDetailModel → HerbInputDto (创建/更新API调用)
///
/// 注意：HerbDetailModel使用ValidatableModelBase，无FromDto/ToDto方法。
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class HerbMapper
{
    /// <summary>
    /// 将HerbDetailDto转换为HerbDetailModel。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    /// <remarks>
    /// DTO中的CreatedBy不映射到Model。
    /// Model中的IsNew是计算属性。
    /// </remarks>
    [MapperIgnoreSource(nameof(HerbDetailDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.IsNew))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.HasErrors))]
    public partial HerbDetailModel ToItem(HerbDetailDto dto);

    /// <summary>
    /// 将HerbDetailModel转换为HerbDetailDto。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(HerbDetailModel.IsNew))]
    [MapperIgnoreSource(nameof(HerbDetailModel.HasErrors))]
    [MapperIgnoreSource(nameof(HerbDetailModel.Errors))]
    [MapperIgnoreSource(nameof(HerbDetailModel.HasErrorsDictionary))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.CreatedBy))]
    public partial HerbDetailDto ToDto(HerbDetailModel model);

    /// <summary>
    /// 将HerbDetailModel转换为HerbInputDto（核心映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>InputDTO对象。</returns>
    [MapperIgnoreSource(nameof(HerbDetailModel.Id))]
    [MapperIgnoreSource(nameof(HerbDetailModel.IsNew))]
    [MapperIgnoreSource(nameof(HerbDetailModel.HasErrors))]
    [MapperIgnoreSource(nameof(HerbDetailModel.Errors))]
    [MapperIgnoreSource(nameof(HerbDetailModel.HasErrorsDictionary))]
    [MapperIgnoreSource(nameof(HerbDetailModel.Status))]
    [MapperIgnoreSource(nameof(HerbDetailModel.CreatedAt))]
    [MapperIgnoreSource(nameof(HerbDetailModel.UpdatedAt))]
    [MapperIgnoreTarget(nameof(HerbInputDto.Id))]
    public partial HerbInputDto ToInputDtoCore(HerbDetailModel model);

    /// <summary>
    /// 将HerbDetailModel转换为HerbInputDto（完整映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>InputDTO对象。</returns>
    public HerbInputDto ToInputDto(HerbDetailModel model)
    {
        var dto = ToInputDtoCore(model);

        // 设置Id（空Guid转为null表示创建）
        dto.Id = model.Id == Guid.Empty ? null : model.Id;

        return dto;
    }
}
