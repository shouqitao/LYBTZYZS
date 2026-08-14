// -----------------------------------------------------------------------
// <copyright file="FormulaDetailModelMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.Models.Items;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.ViewModels;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Catalog.Mappers;

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
    [MapperIgnoreTarget(nameof(FormulaDetailModel.Herbs))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.IsNew))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.HasErrors))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.Errors))]
    [MapperIgnoreTarget(nameof(FormulaDetailModel.HasErrorsDictionary))]
    private partial FormulaDetailModel ToItemCore(FormulaDetailDto dto);

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
            model.Herbs = new ObservableCollection<FormulaHerbItemModel>(dto.Herbs.Select(ToHerbItemModel));
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
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Indication))]
    private partial FormulaDetailDto ToDtoCore(FormulaDetailModel model);

    /// <summary>
    /// 将FormulaDetailModel转换为FormulaDetailDto（完整映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public FormulaDetailDto ToDto(FormulaDetailModel model)
    {
        var dto = ToDtoCore(model);

        // 手动映射Herbs集合
        dto.Herbs = model.Herbs?.Select(ToHerbItemDto).ToList() ?? new List<FormulaHerbItemDto>();

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
    [MapperIgnoreTarget(nameof(FormulaInputDto.Indication))]
    private partial FormulaInputDto ToInputDtoCore(FormulaDetailModel model);

    /// <summary>
    /// 将FormulaDetailDto转换为FormulaEditContext（编辑真源）。
    /// D1: 接线启用 Mapperly——替代 FormulaEditorViewModel 手写 DTO→EditContext 字段映射。
    /// </summary>
    public FormulaEditContext ToEditContext(FormulaDetailDto dto)
    {
        var context = new FormulaEditContext
        {
            Id = dto.Id,
            Name = dto.Name,
            Category = dto.Category,
            Property = dto.Property,
            Effect = dto.Effect,
            Usage = dto.Usage,
            Remark = dto.Remark,
            IsShared = dto.IsShared,
        };

        // 手动映射Herbs集合到ObservableCollection
        context.Herbs = new ObservableCollection<FormulaHerbItemModel>(dto.Herbs?.Select(ToHerbItemModel) ?? []);

        return context;
    }

    /// <summary>
    /// 将FormulaDetailModel转换为FormulaInputDto（完整映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>InputDTO对象。</returns>
    public FormulaInputDto ToInputDto(FormulaDetailModel model)
    {
        var dto = ToInputDtoCore(model);

        // 设置Id（空Guid转为null表示创建）
        dto.Id = model.Id.OrNullIfEmpty();

        // 手动映射Herbs集合
        dto.Herbs = model.Herbs?.Select(ToHerbItemInputDto).ToList() ?? new List<FormulaHerbItemInputDto>();

        return dto;
    }

    #region FormulaHerbItem 通用映射（mapper-chain-audit H2——原 3 处重复构造）

    /// <summary>
    /// 将 FormulaHerbItemDto 映射为 FormulaHerbItemModel。
    /// </summary>
    private static FormulaHerbItemModel ToHerbItemModel(FormulaHerbItemDto dto) => new()
    {
        HerbId = dto.HerbId,
        HerbName = dto.HerbName,
        Dosage = dto.Dosage,
        Unit = dto.Unit,
        ProcessingMethod = dto.ProcessingMethod,
        DecocteMethod = dto.DecocteMethod,
    };

    /// <summary>
    /// 将 FormulaHerbItemModel 映射为 FormulaHerbItemDto。
    /// </summary>
    private static FormulaHerbItemDto ToHerbItemDto(FormulaHerbItemModel model) => new()
    {
        HerbId = model.HerbId,
        HerbName = model.HerbName,
        Dosage = model.Dosage,
        Unit = model.Unit,
        ProcessingMethod = model.ProcessingMethod,
        DecocteMethod = model.DecocteMethod,
    };

    /// <summary>
    /// 将 FormulaHerbItemModel 映射为 FormulaHerbItemInputDto（保存路径）。
    /// </summary>
    private static FormulaHerbItemInputDto ToHerbItemInputDto(FormulaHerbItemModel model) => new()
    {
        HerbId = model.HerbId,
        Dosage = model.Dosage,
        Unit = model.Unit,
        ProcessingMethod = model.ProcessingMethod,
        DecocteMethod = model.DecocteMethod,
    };

    #endregion
}
