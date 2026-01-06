// -----------------------------------------------------------------------
// <copyright file="MedicalCaseMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping - Server端Mapperly映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.MedicalCases.Mapping;

/// <summary>
/// 医案数据映射器 - Mapperly编译时生成
/// 替代原AutoMapper的MedicalCaseMappingProfile
/// </summary>
[Mapper]
public partial class MedicalCaseMapper
{
    // ========== MedicalCase映射 ==========

    /// <summary>
    /// MedicalCase实体转换为MedicalCaseListDto（列表查询）
    /// </summary>
    [MapperIgnoreTarget(nameof(MedicalCaseListDto.CaseNumber))]
    [MapperIgnoreTarget(nameof(MedicalCaseListDto.PatientGender))]
    [MapperIgnoreTarget(nameof(MedicalCaseListDto.PatientAge))]
    [MapperIgnoreTarget(nameof(MedicalCaseListDto.Diagnosis))]
    [MapperIgnoreTarget(nameof(MedicalCaseListDto.HasConsultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseListDto.HasPrescription))]
    public partial MedicalCaseListDto ToListDto(MedicalCase entity);

    /// <summary>
    /// MedicalCase实体列表转换为MedicalCaseListDto列表
    /// </summary>
    public partial List<MedicalCaseListDto> ToListDtos(List<MedicalCase> entities);

    /// <summary>
    /// MedicalCase实体转换为MedicalCaseDetailDto（详情查询）
    /// </summary>
    /// <remarks>
    /// ConsultationId/PrescriptionId由Service根据导航属性填充
    /// 嵌套DTO（Consultation/Prescription）在Service层单独填充
    /// </remarks>
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.CaseNumber))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PatientGender))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PatientAge))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Diagnosis))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PresentIllness))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.ConsultationId))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PrescriptionId))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Prescription))]
    public partial MedicalCaseDetailDto ToDetailDto(MedicalCase entity);

    /// <summary>
    /// MedicalCase实体列表转换为MedicalCaseDetailDto列表
    /// </summary>
    public partial List<MedicalCaseDetailDto> ToDetailDtos(List<MedicalCase> entities);

    /// <summary>
    /// MedicalCaseInputDto转换为MedicalCase实体（创建）
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.Id))]
    [MapperIgnoreTarget(nameof(MedicalCase.Id))]
    [MapperIgnoreTarget(nameof(MedicalCase.PatientName))]
    [MapperIgnoreTarget(nameof(MedicalCase.DoctorName))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseStatus))]
    [MapperIgnoreTarget(nameof(MedicalCase.NeedsPrescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseNumber))]
    [MapperIgnoreTarget(nameof(MedicalCase.CompletedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCase.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreTarget(nameof(MedicalCase.IsDeleted))]
    public partial MedicalCase ToEntity(MedicalCaseInputDto dto);

    /// <summary>
    /// MedicalCaseInputDto更新到现有MedicalCase实体
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.Id))]
    [MapperIgnoreTarget(nameof(MedicalCase.Id))]
    [MapperIgnoreTarget(nameof(MedicalCase.PatientName))]
    [MapperIgnoreTarget(nameof(MedicalCase.DoctorName))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseStatus))]
    [MapperIgnoreTarget(nameof(MedicalCase.NeedsPrescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseNumber))]
    [MapperIgnoreTarget(nameof(MedicalCase.CompletedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCase.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreTarget(nameof(MedicalCase.IsDeleted))]
    public partial void UpdateEntity(MedicalCaseInputDto dto, MedicalCase entity);

    // ========== Consultation映射（聚合内使用） ==========

    /// <summary>
    /// Consultation实体转换为ConsultationDetailDto
    /// </summary>
    [MapProperty(nameof(Consultation.Id), nameof(ConsultationDetailDto.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.PatientId))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.UserId))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.PatientName))]
    [MapperIgnoreTarget(nameof(ConsultationDetailDto.DoctorName))]
    public partial ConsultationDetailDto ToConsultationDetailDto(Consultation entity);

    /// <summary>
    /// ConsultationInputDto转换为Consultation实体
    /// </summary>
    [MapperIgnoreTarget(nameof(Consultation.Id))]
    [MapperIgnoreTarget(nameof(Consultation.CreatedAt))]
    [MapperIgnoreTarget(nameof(Consultation.CreatedBy))]
    [MapperIgnoreTarget(nameof(Consultation.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Consultation.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Consultation.RowVersion))]
    [MapperIgnoreTarget(nameof(Consultation.IsDeleted))]
    public partial Consultation ToConsultationEntity(ConsultationInputDto dto);

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
    public partial void UpdateConsultationEntity(ConsultationInputDto dto, Consultation entity);

    // ========== Prescription映射（聚合内使用） ==========

    /// <summary>
    /// Prescription实体转换为PrescriptionDetailDto
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.SingleDosePrice))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.TotalWeight))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.DuplicateWarning))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.MissingDrugWarning))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Status))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Items))]
    public partial PrescriptionDetailDto ToPrescriptionDetailDto(Prescription entity);

    /// <summary>
    /// PrescriptionInputDto转换为Prescription实体
    /// </summary>
    [MapperIgnoreTarget(nameof(Prescription.Id))]
    [MapperIgnoreTarget(nameof(Prescription.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(Prescription.PrintVersion))]
    [MapperIgnoreTarget(nameof(Prescription.LastPrintedAt))]
    [MapperIgnoreTarget(nameof(Prescription.PrintCount))]
    [MapperIgnoreTarget(nameof(Prescription.IsPrinted))]
    [MapperIgnoreTarget(nameof(Prescription.PrescriptionNumber))]
    [MapperIgnoreTarget(nameof(Prescription.Items))]
    [MapperIgnoreTarget(nameof(Prescription.PrintLogs))]
    [MapperIgnoreTarget(nameof(Prescription.CreatedAt))]
    [MapperIgnoreTarget(nameof(Prescription.CreatedBy))]
    [MapperIgnoreTarget(nameof(Prescription.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Prescription.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Prescription.RowVersion))]
    [MapperIgnoreTarget(nameof(Prescription.IsDeleted))]
    public partial Prescription ToPrescriptionEntity(PrescriptionInputDto dto);

    /// <summary>
    /// PrescriptionInputDto更新到现有Prescription实体
    /// </summary>
    [MapperIgnoreTarget(nameof(Prescription.Id))]
    [MapperIgnoreTarget(nameof(Prescription.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(Prescription.PrintVersion))]
    [MapperIgnoreTarget(nameof(Prescription.LastPrintedAt))]
    [MapperIgnoreTarget(nameof(Prescription.PrintCount))]
    [MapperIgnoreTarget(nameof(Prescription.IsPrinted))]
    [MapperIgnoreTarget(nameof(Prescription.PrescriptionNumber))]
    [MapperIgnoreTarget(nameof(Prescription.Items))]
    [MapperIgnoreTarget(nameof(Prescription.PrintLogs))]
    [MapperIgnoreTarget(nameof(Prescription.CreatedAt))]
    [MapperIgnoreTarget(nameof(Prescription.CreatedBy))]
    [MapperIgnoreTarget(nameof(Prescription.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Prescription.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Prescription.RowVersion))]
    [MapperIgnoreTarget(nameof(Prescription.IsDeleted))]
    public partial void UpdatePrescriptionEntity(PrescriptionInputDto dto, Prescription entity);

    // ========== PrescriptionItem映射 ==========

    /// <summary>
    /// PrescriptionItem实体转换为PrescriptionItemDto
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalWeight))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.Subtotal))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.Notes))]
    public partial PrescriptionItemDto ToPrescriptionItemDto(PrescriptionItem entity);

    /// <summary>
    /// PrescriptionItem实体列表转换为PrescriptionItemDto列表
    /// </summary>
    public partial List<PrescriptionItemDto> ToPrescriptionItemDtos(List<PrescriptionItem> entities);

    /// <summary>
    /// PrescriptionItemInputDto转换为PrescriptionItem实体
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionItem.Id))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.PrescriptionId))]
    public partial PrescriptionItem ToPrescriptionItemEntity(PrescriptionItemInputDto dto);

    /// <summary>
    /// PrescriptionItemInputDto列表转换为PrescriptionItem实体列表
    /// </summary>
    public partial List<PrescriptionItem> ToPrescriptionItemEntities(List<PrescriptionItemInputDto> dtos);
}
