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
/// 注意：ConsultationItem的属性由[ObservableProperty]源生成器生成，需使用字符串字面量。
/// OpenSpec: standardize-viewmodel-framework - CommunityToolkit.Mvvm源生成器兼容
/// </remarks>
[Mapper]
public partial class ConsultationMapper
{
    /// <summary>
    /// 将ConsultationDetailDto转换为ConsultationItem。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    /// <remarks>
    /// 忽略UI状态字段（IsSelected, IsExpanded），这些字段保持默认值。
    /// ConsultationItem无CreatedBy属性，忽略源字段。
    /// </remarks>
    [MapperIgnoreTarget("IsSelected")]
    [MapperIgnoreTarget("IsExpanded")]
    [MapperIgnoreSource(nameof(ConsultationDetailDto.CreatedBy))]
    public partial ConsultationItem ToItem(ConsultationDetailDto dto);

    /// <summary>
    /// 将ConsultationItem转换为ConsultationDetailDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    /// <remarks>
    /// 忽略UI状态字段和计算属性。
    /// </remarks>
    [MapperIgnoreSource("IsSelected")]
    [MapperIgnoreSource("IsExpanded")]
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.CreatedBy))]
    public partial ConsultationDetailDto ToDto(ConsultationItem item);

    /// <summary>
    /// 将ConsultationItem转换为ConsultationInputDto（用于创建/更新API调用）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    /// <remarks>
    /// 忽略UI状态字段、计算属性和审计字段。
    /// </remarks>
    [MapperIgnoreSource("IsSelected")]
    [MapperIgnoreSource("IsExpanded")]
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]
    [MapperIgnoreSource("CreatedAt")]
    [MapperIgnoreSource("UpdatedAt")]
    [MapperIgnoreSource("PatientName")]
    [MapperIgnoreSource("DoctorName")]
    public partial ConsultationInputDto ToInputDto(ConsultationItem item);
}
