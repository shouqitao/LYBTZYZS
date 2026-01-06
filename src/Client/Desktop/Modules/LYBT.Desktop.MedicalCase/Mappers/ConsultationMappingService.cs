// -----------------------------------------------------------------------
// <copyright file="ConsultationMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 诊断数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装ConsultationMapper，提供DI友好的映射服务接口。
/// </remarks>
public class ConsultationMappingService
    : MappingServiceBase<ConsultationDetailDto, ConsultationInputDto, ConsultationItem>
{
    private readonly ConsultationMapper _mapper = new();

    /// <inheritdoc />
    public override ConsultationItem ToItem(ConsultationDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override ConsultationDetailDto ToDto(ConsultationItem item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    public override ConsultationInputDto ToInputDto(ConsultationItem item)
        => _mapper.ToInputDto(item);
}
