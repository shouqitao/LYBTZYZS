// -----------------------------------------------------------------------
// <copyright file="PrescriptionMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 处方数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - PrescriptionDetailDto → PrescriptionItem (从API加载)
/// - PrescriptionItem → PrescriptionDetailDto (仅供展示)
/// - PrescriptionItem → PrescriptionInputDto (保存到API)
///
/// 注意：Items集合需要自定义映射，因为HerbItemDto与PrescriptionItemDto类型不同。
/// 注意：PrescriptionItem的属性由[ObservableProperty]源生成器生成，需使用字符串字面量。
/// OpenSpec: standardize-viewmodel-framework - CommunityToolkit.Mvvm源生成器兼容
/// </remarks>
[Mapper]
public partial class PrescriptionMapper
{
    /// <summary>
    /// 将PrescriptionDetailDto转换为PrescriptionItem。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    [MapperIgnoreTarget("IsSelected")]
    [MapperIgnoreTarget("IsExpanded")]
    [MapperIgnoreTarget("IsReadOnly")]
    [MapperIgnoreTarget("Items")]
    [MapperIgnoreSource(nameof(PrescriptionDetailDto.Items))]
    public partial PrescriptionItem ToItemCore(PrescriptionDetailDto dto);

    /// <summary>
    /// 将PrescriptionDetailDto转换为PrescriptionItem（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    public PrescriptionItem ToItem(PrescriptionDetailDto dto)
    {
        var item = ToItemCore(dto);

        // 手动映射Items集合：PrescriptionItemDto → HerbItemDto
        if (dto.Items != null)
        {
            foreach (var herbDto in dto.Items)
            {
                item.Items.Add(HerbItemDto.FromPrescriptionItemDto(herbDto));
            }
        }

        return item;
    }

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionDetailDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource("IsSelected")]
    [MapperIgnoreSource("IsExpanded")]
    [MapperIgnoreSource("IsReadOnly")]
    [MapperIgnoreSource(nameof(PrescriptionItem.ItemCount))]
    [MapperIgnoreSource(nameof(PrescriptionItem.HasItems))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsValid))]
    [MapperIgnoreSource(nameof(PrescriptionItem.DisplayText))]
    [MapperIgnoreSource("Items")]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Items))]
    public partial PrescriptionDetailDto ToDtoCore(PrescriptionItem item);

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionDetailDto（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public PrescriptionDetailDto ToDto(PrescriptionItem item)
    {
        var dto = ToDtoCore(item);

        // 手动映射Items集合：HerbItemDto → PrescriptionItemDto
        dto.Items = item.Items?.Select(h => h.ToPrescriptionItemDto()).ToList() ?? new();
        dto.TotalPrice = item.TotalPrice;

        return dto;
    }

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionInputDto（用于创建/更新API调用）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    [MapperIgnoreSource("IsSelected")]
    [MapperIgnoreSource("IsExpanded")]
    [MapperIgnoreSource("IsReadOnly")]
    [MapperIgnoreSource(nameof(PrescriptionItem.ItemCount))]
    [MapperIgnoreSource(nameof(PrescriptionItem.HasItems))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsValid))]
    [MapperIgnoreSource(nameof(PrescriptionItem.DisplayText))]
    [MapperIgnoreSource("PrescriptionNumber")]
    [MapperIgnoreSource("SingleDosePrice")]
    [MapperIgnoreSource("TotalWeight")]
    [MapperIgnoreSource("Status")]
    [MapperIgnoreSource("CreatedAt")]
    [MapperIgnoreSource("UpdatedAt")]
    [MapperIgnoreSource("DuplicateWarning")]
    [MapperIgnoreSource("MissingDrugWarning")]
    [MapperIgnoreSource("Items")]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.NeedsPrescription))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.Items))]
    public partial PrescriptionInputDto ToInputDtoCore(PrescriptionItem item);

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionInputDto（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    public PrescriptionInputDto ToInputDto(PrescriptionItem item)
    {
        var dto = ToInputDtoCore(item);

        // 设置NeedsPrescription标志
        dto.NeedsPrescription = item.HasItems;

        // 处理Id：空Guid转为null表示创建
        dto.Id = item.Id == Guid.Empty ? null : item.Id;

        // 计算TotalPrice
        dto.TotalPrice = item.TotalPrice;

        // 手动映射Items集合：HerbItemDto → PrescriptionItemInputDto
        dto.Items = item.Items?.Select(h => h.ToPrescriptionItemInputDto()).ToList() ?? new();

        return dto;
    }
}
