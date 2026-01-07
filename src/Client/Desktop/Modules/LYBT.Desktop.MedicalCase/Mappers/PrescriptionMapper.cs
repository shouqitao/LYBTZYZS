// -----------------------------------------------------------------------
// <copyright file="PrescriptionMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

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
/// OpenSpec: adopt-mapperly-unified-mapping - PrescriptionItem使用BindableBase，支持Mapperly源生成
/// 注意：Items集合需要手动映射，因为HerbItemDto与PrescriptionItemDto类型不同。
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
    [MapperIgnoreTarget(nameof(PrescriptionItem.Items))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.IsSelected))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.IsExpanded))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.IsReadOnly))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.ItemCount))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.HasItems))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.IsValid))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.DisplayText))]
    private partial PrescriptionItem ToItemCore(PrescriptionDetailDto dto);

    /// <summary>
    /// 将PrescriptionDetailDto转换为PrescriptionItem（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    public PrescriptionItem ToItem(PrescriptionDetailDto dto)
    {
        var item = ToItemCore(dto);

        // 处理Usage默认值
        if (string.IsNullOrEmpty(item.Usage))
        {
            item.Usage = "水煎服，一日一剂，分早晚两次温服";
        }

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

    #endregion

    #region Item → DTO

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionDetailDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(PrescriptionItem.Items))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsSelected))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsExpanded))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsReadOnly))]
    [MapperIgnoreSource(nameof(PrescriptionItem.ItemCount))]
    [MapperIgnoreSource(nameof(PrescriptionItem.HasItems))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsValid))]
    [MapperIgnoreSource(nameof(PrescriptionItem.DisplayText))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Items))]
    private partial PrescriptionDetailDto ToDtoCore(PrescriptionItem item);

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
    [MapperIgnoreSource(nameof(PrescriptionItem.Id))] // 手动映射：Id == Guid.Empty ? null : Id
    [MapperIgnoreSource(nameof(PrescriptionItem.TotalPrice))] // 手动映射
    [MapperIgnoreSource(nameof(PrescriptionItem.PrescriptionNumber))]
    [MapperIgnoreSource(nameof(PrescriptionItem.SingleDosePrice))]
    [MapperIgnoreSource(nameof(PrescriptionItem.TotalWeight))]
    [MapperIgnoreSource(nameof(PrescriptionItem.Status))]
    [MapperIgnoreSource(nameof(PrescriptionItem.CreatedAt))]
    [MapperIgnoreSource(nameof(PrescriptionItem.UpdatedAt))]
    [MapperIgnoreSource(nameof(PrescriptionItem.DuplicateWarning))]
    [MapperIgnoreSource(nameof(PrescriptionItem.MissingDrugWarning))]
    [MapperIgnoreSource(nameof(PrescriptionItem.Items))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsSelected))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsExpanded))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsReadOnly))]
    [MapperIgnoreSource(nameof(PrescriptionItem.ItemCount))]
    [MapperIgnoreSource(nameof(PrescriptionItem.HasItems))]
    [MapperIgnoreSource(nameof(PrescriptionItem.IsValid))]
    [MapperIgnoreSource(nameof(PrescriptionItem.DisplayText))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.Id))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.NeedsPrescription))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionInputDto.Items))]
    private partial PrescriptionInputDto ToInputDtoCore(PrescriptionItem item);

    /// <summary>
    /// 将PrescriptionItem转换为PrescriptionInputDto（完整映射，包含Items集合）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    public PrescriptionInputDto ToInputDto(PrescriptionItem item)
    {
        var dto = ToInputDtoCore(item);

        // 手动设置需要自定义逻辑的字段
        dto.Id = item.Id == Guid.Empty ? null : item.Id;
        dto.NeedsPrescription = item.HasItems;
        dto.TotalPrice = item.TotalPrice;

        // 手动映射Items集合：HerbItemDto → PrescriptionItemInputDto
        dto.Items = item.Items?.Select(h => h.ToPrescriptionItemInputDto()).ToList() ?? new();

        return dto;
    }

    #endregion
}
