// -----------------------------------------------------------------------
// <copyright file="PatientMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Desktop.Patients.Models.Items;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Mappers;

/// <summary>
/// 患者数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装PatientMapper，提供DI友好的映射服务接口。
/// </remarks>
public class PatientMappingService
    : MappingServiceBase<PatientDetailDto, PatientInputDto, PatientItem>
{
    private readonly PatientMapper _mapper = new();

    /// <inheritdoc />
    public override PatientItem ToItem(PatientDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override PatientDetailDto ToDto(PatientItem item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    public override PatientInputDto ToInputDto(PatientItem item)
        => _mapper.ToInputDto(item);
}
