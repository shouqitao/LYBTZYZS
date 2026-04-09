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
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
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
    // T2-X8-09: PrintVersion/LastPrintedAt/PrintCount/IsPrinted/PrintLogs 已从 Prescription 移除
    [MapperIgnoreTarget(nameof(Prescription.Id))]
    [MapperIgnoreTarget(nameof(Prescription.MedicalCaseId))]
    [MapperIgnoreTarget(nameof(Prescription.PrescriptionNumber))]
    [MapperIgnoreTarget(nameof(Prescription.Items))]
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
    [MapperIgnoreTarget(nameof(Prescription.PrescriptionNumber))]
    [MapperIgnoreTarget(nameof(Prescription.Items))]
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
    /// 医案实体转换为MedicalCaseDetailDto（完整版）
    /// 基于Mapperly生成的ToDetailDto，再补充嵌套对象和计算字段
    /// Architecture Fix: 统一使用Mapperly + 手动丰富模式
    /// </summary>
    /// <param name="entity">医案实体（需包含导航属性）</param>
    /// <returns>医案完整详情DTO</returns>
    [UserMapping(Default = false)]
    public MedicalCaseDetailDto MapToMedicalCaseDetailDto(MedicalCase entity)
    {
        // 1. 使用Mapperly生成的基础映射
        var dto = ToDetailDto(entity);

        // 2. 补充Mapperly忽略的字段（依赖导航属性/计算逻辑）
        dto.CaseNumber = entity.CaseNumber;
        dto.Diagnosis = entity.Consultation?.TcmDiagnosis;
        dto.PresentIllness = entity.Consultation?.PresentIllness;
        dto.ConsultationId = entity.Consultation != null ? entity.Id : null;
        dto.PrescriptionId = entity.Prescription != null && !entity.Prescription.IsDeleted ? entity.Prescription.Id : null;

        // 3. 嵌套Consultation DTO（使用Mapperly映射 + 补充上下文字段）
        dto.Consultation = entity.Consultation != null
            ? EnrichConsultationDetailDto(entity)
            : null;

        // 4. 嵌套Prescription DTO（使用Mapperly映射 + 补充计算字段）
        dto.Prescription = entity.Prescription != null && !entity.Prescription.IsDeleted
            ? EnrichPrescriptionDetailDto(entity)
            : null;

        return dto;
    }

    /// <summary>
    /// 丰富Consultation DTO - 使用Mapperly映射后补充父级上下文字段
    /// </summary>
    private ConsultationDetailDto EnrichConsultationDetailDto(MedicalCase entity)
    {
        var consultationDto = ToConsultationDetailDto(entity.Consultation!);
        consultationDto.MedicalCaseId = entity.Id;
        consultationDto.PatientId = entity.PatientId;
        consultationDto.UserId = entity.UserId;
        consultationDto.PatientName = entity.PatientName;
        consultationDto.DoctorName = entity.DoctorName;
        return consultationDto;
    }

    /// <summary>
    /// 丰富Prescription DTO - 使用Mapperly映射后补充计算字段和Items
    /// </summary>
    private PrescriptionDetailDto EnrichPrescriptionDetailDto(MedicalCase entity)
    {
        var prescription = entity.Prescription!;
        var dto = ToPrescriptionDetailDto(prescription);
        dto.MedicalCaseId = entity.Id;

        // Items映射（使用Mapperly）
        dto.Items = prescription.Items?.Select(ToPrescriptionItemDto).ToList() ?? new List<PrescriptionItemDto>();

        // 计算字段（Service层关注点，非Mapper职责）
        dto.SingleDosePrice = prescription.Items?.Sum(x => x.Amount) ?? 0;
        dto.TotalPrice = dto.SingleDosePrice * prescription.DosageCount * prescription.Discount;
        dto.TotalWeight = prescription.Items?.Sum(x => x.Dosage) ?? 0;
        dto.Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled;

        return dto;
    }
}
