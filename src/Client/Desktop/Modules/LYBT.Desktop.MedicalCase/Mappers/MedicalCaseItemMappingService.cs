// -----------------------------------------------------------------------
// <copyright file="MedicalCaseItemMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 医案列表项数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装MedicalCaseItemMapper，提供DI友好的映射服务接口。
/// 注意：MedicalCaseItem主要用于列表显示，不支持ToInputDto（使用MedicalCaseDetailModel进行编辑）。
/// </remarks>
public class MedicalCaseItemMappingService
    : MappingServiceBase<MedicalCaseDetailDto, MedicalCaseInputDto, MedicalCaseItem>
{
    private readonly MedicalCaseItemMapper _mapper = new();

    /// <inheritdoc />
    public override MedicalCaseItem ToItem(MedicalCaseDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override MedicalCaseDetailDto ToDto(MedicalCaseItem item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    /// <remarks>
    /// MedicalCaseItem不支持直接转换为InputDto。
    /// 如需保存医案，请使用MedicalCaseDetailModel和MedicalCaseDetailModelMappingService。
    /// </remarks>
    public override MedicalCaseInputDto ToInputDto(MedicalCaseItem item)
        => throw new NotSupportedException(
            "MedicalCaseItem不支持ToInputDto转换。请使用MedicalCaseDetailModel进行编辑操作。");
}
