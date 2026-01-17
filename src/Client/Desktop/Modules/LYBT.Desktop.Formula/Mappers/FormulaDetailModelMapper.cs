// -----------------------------------------------------------------------
// <copyright file="FormulaDetailModelMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.Models;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Formula.Mappers;

/// <summary>
/// 验方详情模型映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - FormulaDetailDto → FormulaDetailModel (从API加载)
/// - FormulaDetailModel → FormulaDetailDto (保存到API)
/// - FormulaDetailModel → FormulaInputDto (创建/更新API调用)
///
/// 注意：Herbs集合需要手动映射（ObservableCollection）。
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class FormulaDetailModelMapper
{
    /// <summary>
    /// 将FormulaDetailDto转换为FormulaDetailModel（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    [MapperIgnoreSource(nameof(FormulaDetailDto.Herbs))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbNames))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.IsEnabled))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.ValidationStatus))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Indications))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Contraindications))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.IsNew))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.HasErrors))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.Errors))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.HasErrorsDictionary))]
    public partial FormulaDetailModel ToItemCore(FormulaDetailDto dto);

    /// <summary>
    /// 将FormulaDetailDto转换为FormulaDetailModel（完整映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    public FormulaDetailModel ToItem(FormulaDetailDto dto)
    {
        var model = ToItemCore(dto);

        // 手动映射Herbs集合到ObservableCollection
        if (dto.Herbs != null)
        {
            model.Herbs = new ObservableCollection<FormulaHerbItemDto>(
                dto.Herbs.Select(h => new FormulaHerbItemDto
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = h.Dosage,
                    Unit = h.Unit,
                    ProcessingMethod = h.ProcessingMethod,
                    DecocteMethod = h.DecocteMethod
                }));
        }

        return model;
    }

    /// <summary>
    /// 将FormulaDetailModel转换为FormulaDetailDto（核心映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaDetailModel.Herbs))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.IsNew))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.HasErrors))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.Errors))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.HasErrorsDictionary))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbNames))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.IsEnabled))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.ValidationStatus))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Contraindications))]
    public partial FormulaDetailDto ToDtoCore(FormulaDetailModel model);

    /// <summary>
    /// 将FormulaDetailModel转换为FormulaDetailDto（完整映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public FormulaDetailDto ToDto(FormulaDetailModel model)
    {
        var dto = ToDtoCore(model);

        // 手动映射Herbs集合
        dto.Herbs = model.Herbs?.ToList() ?? new List<FormulaHerbItemDto>();

        return dto;
    }

    /// <summary>
    /// 将FormulaDetailModel转换为FormulaInputDto（核心映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>InputDTO对象。</returns>
    [MapperIgnoreSource(nameof(FormulaDetailModel.Id))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.Herbs))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.IsNew))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.Status))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.CreatedAt))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.UpdatedAt))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.CreatedBy))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.HasErrors))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.Errors))]
    [MapperIgnoreSource(nameof(FormulaDetailModel.HasErrorsDictionary))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Id))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Contraindications))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Preparation))]
    public partial FormulaInputDto ToInputDtoCore(FormulaDetailModel model);

    /// <summary>
    /// 将FormulaDetailModel转换为FormulaInputDto（完整映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>InputDTO对象。</returns>
    public FormulaInputDto ToInputDto(FormulaDetailModel model)
    {
        var dto = ToInputDtoCore(model);

        // 设置Id（空Guid转为null表示创建）
        dto.Id = model.Id == Guid.Empty ? null : model.Id;

        // 手动映射Herbs集合
        dto.Herbs = model.Herbs?.Select(h => new FormulaHerbItemInputDto
        {
            HerbId = h.HerbId,
            Dosage = h.Dosage,
            Unit = h.Unit,
            ProcessingMethod = h.ProcessingMethod,
            DecocteMethod = h.DecocteMethod
        }).ToList() ?? new List<FormulaHerbItemInputDto>();

        return dto;
    }
}
