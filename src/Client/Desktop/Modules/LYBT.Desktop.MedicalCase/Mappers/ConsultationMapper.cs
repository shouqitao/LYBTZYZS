// -----------------------------------------------------------------------
// <copyright file="ConsultationMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Consultation;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 诊断数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - ConsultationDetailDto → ConsultationItem (从API加载)
/// - ConsultationItem → ConsultationDetailDto (仅供展示)
/// - ConsultationItem → ConsultationInputDto (保存到API)
///
/// OpenSpec: adopt-mapperly-unified-mapping - ConsultationItem使用BindableBase，支持Mapperly源生成
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ConsultationMapper
{
    #region DTO → Item

    /// <summary>
    /// 将ConsultationDetailDto转换为ConsultationItem（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    /// <remarks>
    /// 忽略UI状态字段（IsSelected, IsExpanded）。
    /// 忽略计算属性（IsDiagnosisComplete, DisplayText）。
    /// </remarks>
    [MapperIgnoreSource(nameof(ConsultationDetailDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreTarget(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreTarget(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreTarget(nameof(ConsultationItem.DisplayText))]
    private partial ConsultationItem ToItemCore(ConsultationDetailDto dto);

    /// <summary>
    /// 将ConsultationDetailDto转换为ConsultationItem（完整映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    public ConsultationItem ToItem(ConsultationDetailDto dto)
    {
        var item = ToItemCore(dto);

        // 处理可能为null的展示字段
        if (string.IsNullOrEmpty(item.PatientName))
        {
            item.PatientName = string.Empty;
        }
        if (string.IsNullOrEmpty(item.DoctorName))
        {
            item.DoctorName = string.Empty;
        }

        return item;
    }

    #endregion

    #region Item → DTO

    /// <summary>
    /// 将ConsultationItem转换为ConsultationDetailDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    /// <remarks>
    /// UI状态字段和计算属性不映射到DTO。
    /// </remarks>
    [MapperIgnoreSource(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.CreatedBy))]
    private partial ConsultationDetailDto ToDtoCore(ConsultationItem item);

    /// <summary>
    /// 将ConsultationItem转换为ConsultationDetailDto（完整映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public ConsultationDetailDto ToDto(ConsultationItem item)
    {
        var dto = ToDtoCore(item);
        dto.CreatedBy = null;
        return dto;
    }

    #endregion

    #region Item → InputDto

    /// <summary>
    /// 将ConsultationItem转换为ConsultationInputDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    /// <remarks>
    /// 仅映射可写字段，展示字段和审计字段不映射。
    /// </remarks>
    [MapperIgnoreSource(nameof(ConsultationItem.PatientName))]
    [MapperIgnoreSource(nameof(ConsultationItem.DoctorName))]
    [MapperIgnoreSource(nameof(ConsultationItem.CreatedAt))]
    [MapperIgnoreSource(nameof(ConsultationItem.UpdatedAt))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]
    public partial ConsultationInputDto ToInputDto(ConsultationItem item);

    #endregion
}
