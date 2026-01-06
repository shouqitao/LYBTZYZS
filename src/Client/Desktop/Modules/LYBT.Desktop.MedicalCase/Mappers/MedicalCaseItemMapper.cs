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
/// 注意：部分字段需要自定义映射逻辑（CaseNumber默认值、Diagnosis从嵌套DTO获取）。
/// </remarks>
[Mapper]
public partial class MedicalCaseItemMapper
{
    /// <summary>
    /// 将MedicalCaseDetailDto转换为MedicalCaseItem（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于列表显示的Item对象。</returns>
    /// <remarks>
    /// 忽略UI状态字段（IsSelected, IsExpanded, IsHighlighted）。
    /// 忽略计算属性（StatusText, StatusColor, DisplayText等）。
    /// 忽略需要自定义逻辑的字段（CaseNumber, Diagnosis, CompletedAt, CompletionReason）。
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
    // 注意：以下属性由[ObservableProperty]源生成器生成，需使用字符串字面量
    // OpenSpec: standardize-viewmodel-framework - CommunityToolkit.Mvvm源生成器兼容
    [MapperIgnoreTarget("CaseNumber")]
    [MapperIgnoreTarget("Diagnosis")]
    [MapperIgnoreTarget("CompletedAt")]
    [MapperIgnoreTarget("CompletionReason")]
    [MapperIgnoreTarget("IsSelected")]
    [MapperIgnoreTarget("IsHighlighted")]
    [MapperIgnoreTarget("IsExpanded")]
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
    [MapperIgnoreTarget("CaseStatus")]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CaseStatus))]
    public partial MedicalCaseItem ToItemCore(MedicalCaseDetailDto dto);

    /// <summary>
    /// 将MedicalCaseDetailDto转换为MedicalCaseItem（完整映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于列表显示的Item对象。</returns>
    public MedicalCaseItem ToItem(MedicalCaseDetailDto dto)
    {
        var item = ToItemCore(dto);

        // 手动映射CaseStatus（源生成属性，无法使用[MapProperty]）
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

    /// <summary>
    /// 将MedicalCaseItem转换为MedicalCaseDetailDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    // 注意：以下属性由[ObservableProperty]源生成器生成，需使用字符串字面量
    // OpenSpec: standardize-viewmodel-framework - CommunityToolkit.Mvvm源生成器兼容
    [MapperIgnoreSource("IsSelected")]
    [MapperIgnoreSource("IsHighlighted")]
    [MapperIgnoreSource("IsExpanded")]
    [MapperIgnoreSource("CompletionReason")]
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
    [MapperIgnoreSource("CaseStatus")]
    [MapperIgnoreSource("CompletedAt")]
    public partial MedicalCaseDetailDto ToDtoCore(MedicalCaseItem item);

    /// <summary>
    /// 将MedicalCaseItem转换为MedicalCaseDetailDto（完整映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    public MedicalCaseDetailDto ToDto(MedicalCaseItem item)
    {
        var dto = ToDtoCore(item);

        // 手动映射源生成属性（无法使用[MapProperty]）
        dto.CaseStatus = item.CaseStatus;
        dto.UpdatedAt = item.CompletedAt;

        // 设置默认值
        dto.UserId = Guid.Empty;
        dto.DoctorName = string.Empty;

        return dto;
    }
}
