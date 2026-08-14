// -----------------------------------------------------------------------
// <copyright file="MedicalCaseMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.MedicalCases.Mappers;

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
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Items))]
    public partial PrescriptionDetailDto ToPrescriptionDetailDto(Prescription entity);

    // ========== PrescriptionItem映射 ==========

    /// <summary>
    /// PrescriptionItem实体转换为PrescriptionItemDto
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalWeight))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.Subtotal))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.Role))]
    public partial PrescriptionItemDto ToPrescriptionItemDto(PrescriptionItem entity);

    /// <summary>
    /// 医案实体转换为MedicalCaseDetailDto（完整版）
    /// 基于Mapperly生成的ToDetailDto，再补充嵌套对象和计算字段
    /// 架构修复：统一使用 Mapperly + 手动丰富模式
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
        return consultationDto;
    }

    /// <summary>
    /// 丰富Prescription DTO - 使用Mapperly映射后补充计算字段和Items
    /// 价格快照（A2 决策 + US-MC-002）：明细 Subtotal=UnitPrice×Dosage（单帖小计，文档 §721 Amount 定义）、
    /// TotalPrice=Subtotal×DosageCount（该味药帖剂总价——折扣为处方级 MC-D14 概念不摊明细）——
    /// 2026-08-13 修复：原 Mapperly 忽略 Subtotal/TotalPrice 致明细金额恒 0
    /// </summary>
    private PrescriptionDetailDto EnrichPrescriptionDetailDto(MedicalCase entity)
    {
        var prescription = entity.Prescription!;
        var dto = ToPrescriptionDetailDto(prescription);
        dto.MedicalCaseId = entity.Id;

        // Items映射（使用Mapperly）+ 明细金额快照补算（UnitPrice 已持久化——A2 快照语义）
        dto.Items = prescription.Items?.Select(item =>
        {
            var itemDto = ToPrescriptionItemDto(item);
            // 单帖小计 = 单价 × 剂量（实体 Amount 计算属性——明细金额快照）
            itemDto.Subtotal = item.Amount;
            // 该味药帖剂总价 = 单帖小计 × 剂数（折扣为处方级整体概念，不摊入明细）
            itemDto.TotalPrice = item.Amount * prescription.DosageCount;
            return itemDto;
        }).ToList() ?? new List<PrescriptionItemDto>();

        // 计算字段（Service层关注点，非Mapper职责）
        dto.SingleDosePrice = prescription.Items?.Sum(x => x.Amount) ?? 0;
        dto.TotalPrice = dto.SingleDosePrice * prescription.DosageCount * prescription.Discount;
        dto.TotalWeight = prescription.Items?.Sum(x => x.Dosage) ?? 0;

        return dto;
    }
}


