// -----------------------------------------------------------------------
// <copyright file="ConsultationMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Consultation.Models.Items;
using LYBT.Shared.Models.Contracts.Consultation;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Consultation.Mappers;

/// <summary>
/// 问诊数据映射器 - Mapperly实现。
/// </summary>
/// <remarks>
/// 映射关系：
/// - ConsultationDetailDto → ConsultationItem (从API加载)
/// - ConsultationItem → ConsultationDetailDto (保存到API)
/// - ConsultationItem → ConsultationInputDto (创建/更新API调用)
///
/// OpenSpec: resolve-mapperly-source-generator-conflict
/// Item类使用BindableBase+显式属性，确保Mapperly能正确生成映射代码。
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ConsultationMapper
{
    #region ConsultationDetailDto <-> ConsultationItem

    /// <summary>
    /// 将ConsultationDetailDto转换为ConsultationItem。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    [MapperIgnoreTarget(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreTarget(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreTarget(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreTarget(nameof(ConsultationItem.DisplayText))]
    [MapperIgnoreSource(nameof(ConsultationDetailDto.CreatedBy))]
    public partial ConsultationItem ToItem(ConsultationDetailDto dto);

    /// <summary>
    /// 将ConsultationItem转换为ConsultationDetailDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.CreatedBy))]
    public partial ConsultationDetailDto ToDto(ConsultationItem item);

    #endregion

    #region ConsultationItem -> ConsultationInputDto

    /// <summary>
    /// 将ConsultationItem转换为ConsultationInputDto。
    /// </summary>
    [MapperIgnoreSource(nameof(ConsultationItem.PatientName))]
    [MapperIgnoreSource(nameof(ConsultationItem.DoctorName))]
    [MapperIgnoreSource(nameof(ConsultationItem.CreatedAt))]
    [MapperIgnoreSource(nameof(ConsultationItem.UpdatedAt))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsSelected))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsExpanded))]
    [MapperIgnoreSource(nameof(ConsultationItem.IsDiagnosisComplete))]
    [MapperIgnoreSource(nameof(ConsultationItem.DisplayText))]
    private partial ConsultationInputDto ToInputDtoCore(ConsultationItem item);

    /// <summary>
    /// 将ConsultationItem转换为ConsultationInputDto（含特殊字段处理）。
    /// </summary>
    public ConsultationInputDto ToInputDto(ConsultationItem item)
    {
        var dto = ToInputDtoCore(item);
        // Id为Guid.Empty时设为null（表示新建）
        dto.Id = item.Id == Guid.Empty ? null : item.Id;
        dto.MedicalCaseId = item.MedicalCaseId == Guid.Empty ? null : item.MedicalCaseId;
        dto.PatientId = item.PatientId == Guid.Empty ? null : item.PatientId;
        dto.UserId = item.UserId == Guid.Empty ? null : item.UserId;
        return dto;
    }

    #endregion
}
