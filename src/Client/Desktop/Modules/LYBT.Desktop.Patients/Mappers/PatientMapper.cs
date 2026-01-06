// -----------------------------------------------------------------------
// <copyright file="PatientMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Patients.Models.Items;
using LYBT.Shared.Models.Contracts.Patients;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Patients.Mappers;

/// <summary>
/// 患者数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - PatientDetailDto → PatientItem (从API加载)
/// - PatientItem → PatientDetailDto (保存到API)
/// - PatientItem → PatientInputDto (创建/更新API调用)
///
/// 注意：PatientItem是简化版本，仅包含常用字段。
/// </remarks>
[Mapper]
public partial class PatientMapper
{
    /// <summary>
    /// 将PatientDetailDto转换为PatientItem。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Item对象。</returns>
    /// <remarks>
    /// DTO中的部分属性不映射到Item（Item是简化版本）：
    /// - MaritalStatus, IdType, BloodType
    /// - EmergencyContactName/Phone/Relation
    /// - DisableReason, PinYinCode, Status, UpdatedAt, CreatedBy
    /// </remarks>
    [MapperIgnoreSource(nameof(PatientDetailDto.Age))]
    [MapperIgnoreSource(nameof(PatientDetailDto.MaritalStatus))]
    [MapperIgnoreSource(nameof(PatientDetailDto.IdType))]
    [MapperIgnoreSource(nameof(PatientDetailDto.BloodType))]
    [MapperIgnoreSource(nameof(PatientDetailDto.EmergencyContactName))]
    [MapperIgnoreSource(nameof(PatientDetailDto.EmergencyContactPhone))]
    [MapperIgnoreSource(nameof(PatientDetailDto.EmergencyContactRelation))]
    [MapperIgnoreSource(nameof(PatientDetailDto.DisableReason))]
    [MapperIgnoreSource(nameof(PatientDetailDto.PinYinCode))]
    [MapperIgnoreSource(nameof(PatientDetailDto.Status))]
    [MapperIgnoreSource(nameof(PatientDetailDto.UpdatedAt))]
    [MapperIgnoreSource(nameof(PatientDetailDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(PatientItem.Age))]
    [MapperIgnoreTarget(nameof(PatientItem.GenderDisplay))]
    [MapperIgnoreTarget(nameof(PatientItem.IsSelected))]
    [MapperIgnoreTarget(nameof(PatientItem.IsHighlighted))]
    [MapperIgnoreTarget(nameof(PatientItem.DisplayText))]
    [MapperIgnoreTarget(nameof(PatientItem.IsNewPatient))]
    public partial PatientItem ToItem(PatientDetailDto dto);

    /// <summary>
    /// 将PatientItem转换为PatientDetailDto。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>DetailDTO对象。</returns>
    [MapperIgnoreSource(nameof(PatientItem.Age))]
    [MapperIgnoreSource(nameof(PatientItem.GenderDisplay))]
    [MapperIgnoreSource(nameof(PatientItem.IsSelected))]
    [MapperIgnoreSource(nameof(PatientItem.IsHighlighted))]
    [MapperIgnoreSource(nameof(PatientItem.DisplayText))]
    [MapperIgnoreSource(nameof(PatientItem.IsNewPatient))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.Age))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.MaritalStatus))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.IdType))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.BloodType))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactName))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactPhone))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.EmergencyContactRelation))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.DisableReason))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.PinYinCode))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.Status))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.UpdatedAt))]
    [MapperIgnoreTarget(nameof(PatientDetailDto.CreatedBy))]
    public partial PatientDetailDto ToDto(PatientItem item);

    /// <summary>
    /// 将PatientItem转换为PatientInputDto（核心映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    [MapperIgnoreSource(nameof(PatientItem.Id))]
    [MapperIgnoreSource(nameof(PatientItem.Age))]
    [MapperIgnoreSource(nameof(PatientItem.GenderDisplay))]
    [MapperIgnoreSource(nameof(PatientItem.IsSelected))]
    [MapperIgnoreSource(nameof(PatientItem.IsHighlighted))]
    [MapperIgnoreSource(nameof(PatientItem.DisplayText))]
    [MapperIgnoreSource(nameof(PatientItem.IsNewPatient))]
    [MapperIgnoreSource(nameof(PatientItem.CreatedAt))]
    [MapperIgnoreSource(nameof(PatientItem.LastVisitTime))]
    [MapperIgnoreSource(nameof(PatientItem.VisitCount))]
    [MapperIgnoreTarget(nameof(PatientInputDto.Id))]
    [MapperIgnoreTarget(nameof(PatientInputDto.PinYinCode))]
    [MapperIgnoreTarget(nameof(PatientInputDto.MaritalStatus))]
    [MapperIgnoreTarget(nameof(PatientInputDto.IdType))]
    [MapperIgnoreTarget(nameof(PatientInputDto.BloodType))]
    [MapperIgnoreTarget(nameof(PatientInputDto.EmergencyContactName))]
    [MapperIgnoreTarget(nameof(PatientInputDto.EmergencyContactPhone))]
    [MapperIgnoreTarget(nameof(PatientInputDto.EmergencyContactRelation))]
    public partial PatientInputDto ToInputDtoCore(PatientItem item);

    /// <summary>
    /// 将PatientItem转换为PatientInputDto（完整映射）。
    /// </summary>
    /// <param name="item">Item对象。</param>
    /// <returns>InputDTO对象。</returns>
    public PatientInputDto ToInputDto(PatientItem item)
    {
        var dto = ToInputDtoCore(item);

        // 设置Id（空Guid转为null表示创建）
        dto.Id = item.Id == Guid.Empty ? null : item.Id;

        return dto;
    }
}
