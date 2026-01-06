// -----------------------------------------------------------------------
// <copyright file="MedicalCaseDetailModelMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 医案详情模型数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装MedicalCaseDetailModelMapper，提供DI友好的映射服务接口。
/// 用于MasterDetail视图中的医案详情编辑。
/// </remarks>
public class MedicalCaseDetailModelMappingService
    : MappingServiceBase<MedicalCaseDetailDto, MedicalCaseInputDto, MedicalCaseDetailModel>
{
    private readonly MedicalCaseDetailModelMapper _mapper = new();

    /// <inheritdoc />
    public override MedicalCaseDetailModel ToItem(MedicalCaseDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    /// <remarks>
    /// MedicalCaseDetailModel不直接转换为MedicalCaseDetailDto。
    /// 如需此功能，请使用MedicalCaseItemMapper。
    /// </remarks>
    public override MedicalCaseDetailDto ToDto(MedicalCaseDetailModel item)
        => throw new NotSupportedException(
            "MedicalCaseDetailModel不支持ToDto转换。请使用MedicalCaseItemMapper进行列表项转换。");

    /// <inheritdoc />
    public override MedicalCaseInputDto ToInputDto(MedicalCaseDetailModel item)
        => _mapper.ToInputDto(item);
}
