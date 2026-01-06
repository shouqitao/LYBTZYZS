// -----------------------------------------------------------------------
// <copyright file="PrescriptionMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 处方数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装PrescriptionMapper，提供DI友好的映射服务接口。
/// 处理嵌套的HerbItemDto集合映射。
/// </remarks>
public class PrescriptionMappingService
    : MappingServiceBase<PrescriptionDetailDto, PrescriptionInputDto, PrescriptionItem>
{
    private readonly PrescriptionMapper _mapper = new();

    /// <inheritdoc />
    public override PrescriptionItem ToItem(PrescriptionDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override PrescriptionDetailDto ToDto(PrescriptionItem item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    public override PrescriptionInputDto ToInputDto(PrescriptionItem item)
        => _mapper.ToInputDto(item);
}
