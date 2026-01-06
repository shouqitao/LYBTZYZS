// -----------------------------------------------------------------------
// <copyright file="FormulaMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Formula.Models.Items;
using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Mappers;

/// <summary>
/// 验方数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装FormulaMapper，提供DI友好的映射服务接口。
/// 处理嵌套的FormulaHerbItem集合映射。
/// </remarks>
public class FormulaMappingService
    : MappingServiceBase<FormulaDetailDto, FormulaInputDto, FormulaItem>
{
    private readonly FormulaMapper _mapper = new();

    /// <inheritdoc />
    public override FormulaItem ToItem(FormulaDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override FormulaDetailDto ToDto(FormulaItem item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    public override FormulaInputDto ToInputDto(FormulaItem item)
        => _mapper.ToInputDto(item);
}
