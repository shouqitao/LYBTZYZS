// -----------------------------------------------------------------------
// <copyright file="HerbMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Herbs.Models;
using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Mappers;

/// <summary>
/// 药材数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装HerbMapper，提供DI友好的映射服务接口。
/// </remarks>
public class HerbMappingService
    : MappingServiceBase<HerbDetailDto, HerbInputDto, HerbDetailModel>
{
    private readonly HerbMapper _mapper = new();

    /// <inheritdoc />
    public override HerbDetailModel ToItem(HerbDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override HerbDetailDto ToDto(HerbDetailModel item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    public override HerbInputDto ToInputDto(HerbDetailModel item)
        => _mapper.ToInputDto(item);
}
