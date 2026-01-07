// -----------------------------------------------------------------------
// <copyright file="PrescriptionMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping - Server端Mapperly映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Prescriptions.Mapping;

/// <summary>
/// 处方数据映射器 - Mapperly编译时生成
/// 替代原AutoMapper的PrescriptionMappingProfile
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PrescriptionMapper
{
    /// <summary>
    /// Prescription实体转换为PrescriptionListDto（列表查询）
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionListDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionListDto.Status))]
    public partial PrescriptionListDto ToListDto(Prescription entity);

    /// <summary>
    /// Prescription实体列表转换为PrescriptionListDto列表
    /// </summary>
    public partial List<PrescriptionListDto> ToListDtos(List<Prescription> entities);

    /// <summary>
    /// Prescription实体转换为PrescriptionDetailDto（详情查询）
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.SingleDosePrice))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.TotalWeight))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.DuplicateWarning))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.MissingDrugWarning))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Status))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Items))]
    public partial PrescriptionDetailDto ToDetailDto(Prescription entity);

    /// <summary>
    /// Prescription实体列表转换为PrescriptionDetailDto列表
    /// </summary>
    public partial List<PrescriptionDetailDto> ToDetailDtos(List<Prescription> entities);

    /// <summary>
    /// PrescriptionItem实体转换为PrescriptionItemDto
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalWeight))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.Subtotal))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.Notes))]
    public partial PrescriptionItemDto ToItemDto(PrescriptionItem entity);

    /// <summary>
    /// PrescriptionItem实体列表转换为PrescriptionItemDto列表
    /// </summary>
    public partial List<PrescriptionItemDto> ToItemDtos(List<PrescriptionItem> entities);

    /// <summary>
    /// PrescriptionInputDto转换为Prescription实体（创建）
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
    public partial Prescription ToEntity(PrescriptionInputDto dto);

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
    public partial void UpdateEntity(PrescriptionInputDto dto, Prescription entity);

    /// <summary>
    /// PrescriptionItemInputDto转换为PrescriptionItem实体
    /// </summary>
    [MapperIgnoreTarget(nameof(PrescriptionItem.Id))]
    [MapperIgnoreTarget(nameof(PrescriptionItem.PrescriptionId))]
    public partial PrescriptionItem ToItemEntity(PrescriptionItemInputDto dto);

    /// <summary>
    /// PrescriptionItemInputDto列表转换为PrescriptionItem实体列表
    /// </summary>
    public partial List<PrescriptionItem> ToItemEntities(List<PrescriptionItemInputDto> dtos);
}
