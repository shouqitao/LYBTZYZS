// -----------------------------------------------------------------------
// <copyright file="PrescriptionMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.ViewModels.Items;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 处方数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - PrescriptionDetailDto → PrescriptionItemViewModel (从API加载)
/// - PrescriptionItemViewModel → PrescriptionDetailDto (仅供展示)
/// - PrescriptionItemViewModel → PrescriptionInputDto (保存到API)
///
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PrescriptionMapper
{
    #region DTO → Item

    /// <summary>
    /// 将PrescriptionDetailDto转换为PrescriptionItem（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    /// <remarks>
    /// 忽略UI状态字段、计算属性、Items集合（手动映射）。
    /// </remarks>
    [MapperIgnoreSource(nameof(PrescriptionDetailDto.Items))]
    [MapperIgnoreSource(nameof(PrescriptionDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.Items))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.IsSelected))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.IsExpanded))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.IsReadOnly))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.ItemCount))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.HasItems))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.IsValid))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.DisplayText))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.ValidationEnabled))]
    [MapperIgnoreTarget(nameof(PrescriptionItemViewModel.ValidationMessage))]
    private partial PrescriptionItemViewModel ToItemCore(PrescriptionDetailDto dto);

    /// <summary>
    /// 将PrescriptionDetailDto转换为PrescriptionItem（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    public PrescriptionItemViewModel ToItem(PrescriptionDetailDto dto)
    {
        var item = ToItemCore(dto);

        // 处理Usage默认值
        if (string.IsNullOrEmpty(item.Usage))
        {
            item.Usage = "水煎服，一日一剂，分早晚两次温服";
        }
        if (dto.Items != null)
        {
            foreach (var prescriptionItem in dto.Items)
            {
                item.Items.Add(prescriptionItem);
            }
        }

        return item;
    }

    #endregion

    #region Item → DTO

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionDetailDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.Items))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsSelected))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsExpanded))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsReadOnly))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.ItemCount))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.HasItems))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsValid))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.DisplayText))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.ValidationEnabled))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.ValidationMessage))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Items))]
    private partial PrescriptionDetailDto ToDtoCore(PrescriptionItemViewModel item);

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionDetailDto（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public PrescriptionDetailDto ToDto(PrescriptionItemViewModel item)
    {
        var dto = ToDtoCore(item);
        dto.Items = item.Items?.ToList() ?? new();

        return dto;
    }

    #endregion

    #region Item → InputDto

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionInputDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    /// <remarks>
    /// 仅映射可写字段。
    /// </remarks>
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.Id))] // 手动映射：Id == Guid.Empty ? null : Id
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.TotalPrice))] // 手动映射
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.PrescriptionNumber))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.SingleDosePrice))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.TotalWeight))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.Status))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.CreatedAt))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.UpdatedAt))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.DuplicateWarning))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.MissingDrugWarning))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.Items))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsSelected))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsExpanded))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsReadOnly))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.ItemCount))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.HasItems))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.IsValid))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.DisplayText))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.ValidationEnabled))]
    [MapperIgnoreSource(nameof(PrescriptionItemViewModel.ValidationMessage))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.Id))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.NeedsPrescription))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.Items))]
    private partial PrescriptionInputDto ToInputDtoCore(PrescriptionItemViewModel item);

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionInputDto（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    public PrescriptionInputDto ToInputDto(PrescriptionItemViewModel item)
    {
        var dto = ToInputDtoCore(item);

        // 手动设置需要自定义逻辑的字段
        dto.Id = item.Id == Guid.Empty ? null : item.Id;
        dto.NeedsPrescription = item.HasItems;
        dto.TotalPrice = item.TotalPrice;
        dto.Items = item.Items?.Select(h => new PrescriptionItemInputDto
        {
            HerbId = h.HerbId,
            HerbName = h.HerbName ?? string.Empty,
            Dosage = h.Dosage,
            Unit = h.Unit ?? "g",
            UnitPrice = h.UnitPrice,
            Subtotal = h.Dosage * h.UnitPrice,
            DecocteMethod = h.DecocteMethod
        }).ToList() ?? new();

        return dto;
    }

    #endregion
}
