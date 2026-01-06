// -----------------------------------------------------------------------
// <copyright file="ConsultationMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping - Server端Mapperly映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Consultations;
using LYBT.Shared.Models.Contracts.Consultation;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Consultations.Mapping;

/// <summary>
/// 诊疗数据映射器 - Mapperly编译时生成
/// 替代原AutoMapper的ConsultationMappingProfile
/// </summary>
[Mapper]
public partial class ConsultationMapper
{
    /// <summary>
    /// Consultation实体转换为ConsultationListDto（列表查询）
    /// </summary>
    /// <remarks>
    /// MedicalCaseId使用共享主键，等于Id
    /// PatientName/DoctorName由Service填充
    /// </remarks>
    [MapProperty(nameof(Consultation.Id), nameof(ConsultationListDto.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(ConsultationListDto.PatientName))]
    [MapperIgnoreTarget(nameof(ConsultationListDto.DoctorName))]
    public partial ConsultationListDto ToListDto(Consultation entity);

    /// <summary>
    /// Consultation实体列表转换为ConsultationListDto列表
    /// </summary>
    public partial List<ConsultationListDto> ToListDtos(List<Consultation> entities);

    /// <summary>
    /// Consultation实体转换为ConsultationDetailDto（详情查询）
    /// </summary>
    /// <remarks>
    /// MedicalCaseId使用共享主键，等于Id
    /// PatientId/UserId/PatientName/DoctorName由Service填充
    /// </remarks>
    [MapProperty(nameof(Consultation.Id), nameof(ConsultationDetailDto.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.PatientId))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.UserId))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.PatientName))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.DoctorName))]
    public partial ConsultationDetailDto ToDetailDto(Consultation entity);

    /// <summary>
    /// Consultation实体列表转换为ConsultationDetailDto列表
    /// </summary>
    public partial List<ConsultationDetailDto> ToDetailDtos(List<Consultation> entities);

    /// <summary>
    /// ConsultationInputDto转换为Consultation实体（创建）
    /// </summary>
    /// <remarks>
    /// Id使用共享主键，由Service层设置
    /// 忽略审计字段（由Service层自动设置）
    /// </remarks>
    [MapperIgnoreTarget(nameof(Consultation.Id))]
    [MapperIgnoreTarget(nameof(Consultation.CreatedAt))]
    [MapperIgnoreTarget(nameof(Consultation.CreatedBy))]
    [MapperIgnoreTarget(nameof(Consultation.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Consultation.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Consultation.RowVersion))]
    [MapperIgnoreTarget(nameof(Consultation.IsDeleted))]
    public partial Consultation ToEntity(ConsultationInputDto dto);

    /// <summary>
    /// ConsultationInputDto更新到现有Consultation实体
    /// </summary>
    [MapperIgnoreTarget(nameof(Consultation.Id))]
    [MapperIgnoreTarget(nameof(Consultation.CreatedAt))]
    [MapperIgnoreTarget(nameof(Consultation.CreatedBy))]
    [MapperIgnoreTarget(nameof(Consultation.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Consultation.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Consultation.RowVersion))]
    [MapperIgnoreTarget(nameof(Consultation.IsDeleted))]
    public partial void UpdateEntity(ConsultationInputDto dto, Consultation entity);
}
