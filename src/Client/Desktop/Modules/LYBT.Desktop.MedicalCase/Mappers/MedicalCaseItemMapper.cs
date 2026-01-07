// -----------------------------------------------------------------------
// <copyright file="MedicalCaseItemMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 医案列表项数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - MedicalCaseDetailDto → MedicalCaseItem (从API加载)
/// - MedicalCaseItem → MedicalCaseDetailDto (仅供展示)
///
/// OpenSpec: adopt-mapperly-unified-mapping - MedicalCaseItem使用BindableBase，支持Mapperly源生成
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class MedicalCaseItemMapper
{
    #region DTO → Item

    /// <summary>
    /// 将MedicalCaseDetailDto转换为MedicalCaseItem（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于列表显示的Item对象。</returns>
    /// <remarks>
    /// 忽略UI状态字段、计算属性、需要自定义逻辑的字段。
    /// </remarks>
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.UserId))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.DoctorName))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Remark))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.UpdatedAt))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CreatedBy))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CompletedAt))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.HasConsultation))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.HasPrescription))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Prescription))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CaseNumber))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Diagnosis))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CaseStatus))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.CaseNumber))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.Diagnosis))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.CompletedAt))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.CompletionReason))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.IsSelected))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.IsHighlighted))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.IsExpanded))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.CaseStatus))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.PatientGenderDisplay))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.StatusText))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.StatusColor))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.IsActive))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.IsCompleted))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.CanEdit))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.CanStartConsultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.CanCreatePrescription))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.DisplayText))]
    [MapperIgnoreTarget(nameof(MedicalCaseItem.DurationMinutes))]
    private partial MedicalCaseItem ToItemCore(MedicalCaseDetailDto dto);

    /// <summary>
    /// 将MedicalCaseDetailDto转换为MedicalCaseItem（完整映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于列表显示的Item对象。</returns>
    public MedicalCaseItem ToItem(MedicalCaseDetailDto dto)
    {
        var item = ToItemCore(dto);

        // 手动映射CaseStatus
        item.CaseStatus = dto.CaseStatus;

        // 自定义逻辑：CaseNumber默认值
        item.CaseNumber = dto.CaseNumber ?? dto.Id.ToString().Substring(0, 8).ToUpper();

        // 自定义逻辑：Diagnosis优先使用DTO字段，否则从嵌套Consultation获取
        item.Diagnosis = dto.Diagnosis ?? dto.Consultation?.TcmDiagnosis;

        // 自定义逻辑：完成状态相关字段
        if (dto.CaseStatus == MedicalCaseStatus.Completed)
        {
            item.CompletedAt = dto.UpdatedAt;
            item.CompletionReason = "已完成";
        }

        return item;
    }

    #endregion

    #region Item → DTO

    /// <summary>
    /// 将MedicalCaseItem转换为MedicalCaseDetailDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(MedicalCaseItem.IsSelected))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.IsHighlighted))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.IsExpanded))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.CompletionReason))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.CompletedAt))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.CaseStatus))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.PatientGenderDisplay))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.StatusText))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.StatusColor))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.IsActive))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.IsCompleted))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.CanEdit))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.CanStartConsultation))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.CanCreatePrescription))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.DisplayText))]
    [MapperIgnoreSource(nameof(MedicalCaseItem.DurationMinutes))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.UserId))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.DoctorName))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Remark))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.HasConsultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.HasPrescription))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.CaseStatus))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.CompletedAt))] // 手动映射或不需要映射
    private partial MedicalCaseDetailDto ToDtoCore(MedicalCaseItem item);

    /// <summary>
    /// 将MedicalCaseItem转换为MedicalCaseDetailDto（完整映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public MedicalCaseDetailDto ToDto(MedicalCaseItem item)
    {
        var dto = ToDtoCore(item);

        // 手动映射
        dto.CaseStatus = item.CaseStatus;
        dto.UpdatedAt = item.CompletedAt;

        // 设置默认值
        dto.UserId = Guid.Empty;
        dto.DoctorName = string.Empty;

        return dto;
    }

    #endregion
}
