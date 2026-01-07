// -----------------------------------------------------------------------
// <copyright file="PatientMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping - Server端Mapperly映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Patients.Mapping;

/// <summary>
/// 患者数据映射器 - Mapperly编译时生成
/// 替代原AutoMapper的PatientMappingProfile
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PatientMapper
{
    /// <summary>
    /// Patient实体转换为PatientListDto（列表查询）
    /// </summary>
    public partial PatientListDto ToListDto(Patient entity);

    /// <summary>
    /// Patient实体列表转换为PatientListDto列表
    /// </summary>
    public partial List<PatientListDto> ToListDtos(List<Patient> entities);

    /// <summary>
    /// Patient实体转换为PatientDetailDto（详情查询）
    /// </summary>
    public partial PatientDetailDto ToDetailDto(Patient entity);

    /// <summary>
    /// Patient实体列表转换为PatientDetailDto列表
    /// </summary>
    public partial List<PatientDetailDto> ToDetailDtos(List<Patient> entities);

    /// <summary>
    /// PatientInputDto转换为Patient实体（创建）
    /// </summary>
    /// <remarks>
    /// Age是只读计算属性，PinYinCode由系统生成
    /// 忽略审计字段（由Service层自动设置）
    /// </remarks>
    [MapperIgnoreSource(nameof(PatientInputDto.Id))]
    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.Age))]
    [MapperIgnoreTarget(nameof(Patient.PinYinCode))]
    [MapperIgnoreTarget(nameof(Patient.LastVisitTime))]
    [MapperIgnoreTarget(nameof(Patient.VisitCount))]
    [MapperIgnoreTarget(nameof(Patient.DisableReason))]
    [MapperIgnoreTarget(nameof(Patient.Status))]
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.CreatedBy))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Patient.RowVersion))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    public partial Patient ToEntity(PatientInputDto dto);

    /// <summary>
    /// PatientInputDto更新到现有Patient实体
    /// </summary>
    [MapperIgnoreSource(nameof(PatientInputDto.Id))]
    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.Age))]
    [MapperIgnoreTarget(nameof(Patient.PinYinCode))]
    [MapperIgnoreTarget(nameof(Patient.LastVisitTime))]
    [MapperIgnoreTarget(nameof(Patient.VisitCount))]
    [MapperIgnoreTarget(nameof(Patient.DisableReason))]
    [MapperIgnoreTarget(nameof(Patient.Status))]
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.CreatedBy))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Patient.RowVersion))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    public partial void UpdateEntity(PatientInputDto dto, Patient entity);

    /// <summary>
    /// PatientDetailDto更新到现有Patient实体
    /// </summary>
    [MapperIgnoreTarget(nameof(Patient.Age))]
    [MapperIgnoreTarget(nameof(Patient.LastVisitTime))]
    [MapperIgnoreTarget(nameof(Patient.VisitCount))]
    [MapperIgnoreTarget(nameof(Patient.DisableReason))]
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.CreatedBy))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Patient.RowVersion))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    public partial void UpdateEntityFromDetail(PatientDetailDto dto, Patient entity);
}
