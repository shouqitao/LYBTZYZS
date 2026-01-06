// -----------------------------------------------------------------------
// <copyright file="FormulaDetailModelMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Mappers;

/// <summary>
/// 验方详情模型映射服务实现。
/// </summary>
/// <remarks>
/// 封装FormulaDetailModelMapper，提供DI友好的映射服务接口。
/// 用于FormulaMasterDetailViewModel的Detail编辑。
/// </remarks>
public class FormulaDetailModelMappingService
    : MappingServiceBase<FormulaDetailDto, FormulaInputDto, FormulaDetailModel>
{
    private readonly FormulaDetailModelMapper _mapper = new();

    /// <inheritdoc />
    public override FormulaDetailModel ToItem(FormulaDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override FormulaDetailDto ToDto(FormulaDetailModel item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    public override FormulaInputDto ToInputDto(FormulaDetailModel item)
        => _mapper.ToInputDto(item);
}
