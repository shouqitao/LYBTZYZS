using AutoMapper;
using LYBT.Entities.Prescriptions;

// using LYBT.Entities.Compatibility; // 移除：已删除的Compatibility实体
// using LYBT.Shared.Models.Contracts.Compatibility; // 移除：已删除的Compatibility契约
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Mapping
{

    /// <summary>
    /// 表示PrescriptionMappingProfile。
    /// OpenSpec: refactor-dto-simplification - 添加简化DTO映射
    /// </summary>
    public class PrescriptionMappingProfile : Profile
    {

        public PrescriptionMappingProfile()
        {
            // ============================================
            // 新简化DTO映射 (OpenSpec: refactor-dto-simplification)
            // ============================================

            // Prescription -> PrescriptionListDto (新)
            CreateMap<Prescription, PrescriptionListDto>()
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()) // 由Service计算
                .ForMember(dest => dest.Status, opt => opt.Ignore()); // 实体无此字段

            // Prescription -> PrescriptionDetailDto (新-简化版)
            CreateMap<Prescription, PrescriptionDetailDto>()
                .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.DuplicateWarning, opt => opt.Ignore())
                .ForMember(dest => dest.MissingDrugWarning, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore()); // 单独映射子项

            // PrescriptionItem -> PrescriptionItemDetailDto (新)
            CreateMap<PrescriptionItem, PrescriptionItemDetailDto>()
                .ForMember(dest => dest.Dosage, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotal, opt => opt.Ignore());

            // PrescriptionInputDto -> Prescription (新-统一输入)
            CreateMap<PrescriptionInputDto, Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Condition(src => src.Id.HasValue))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? Guid.Empty))
                .ForMember(dest => dest.ReferencedFormulas, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // PrescriptionItemInputDto -> PrescriptionItem (新-简化版)
            CreateMap<PrescriptionItemInputDto, PrescriptionItem>()
                .ForMember(dest => dest.Id, opt => opt.Condition(src => src.Id.HasValue))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? Guid.Empty))
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());

            // ============================================
            // 旧DTO映射 (保持向后兼容，后续移除)
            // ============================================

            // Prescription -> PrescriptionDto
            CreateMap<Prescription, PrescriptionDto>()
                .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore()) // 计算属性
                .ForMember(dest => dest.Status, opt => opt.Ignore()); // StatusDto继承属性，实体无此字段

            // PrescriptionItem -> PrescriptionItemDto
            CreateMap<PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.Dosage, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore());

            // PrescriptionCreateDto -> Prescription
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            CreateMap<PrescriptionCreateDto, Prescription>()
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore())
                .ForMember(dest => dest.ReferencedFormulas, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                // 忽略 BaseEntity 审计字段
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // PrescriptionItemInputDto -> PrescriptionItem (旧版映射，所有旧DTO共用新的ItemInputDto)
            // 注：新DTO映射在上方，此映射保留给PrescriptionCreateDto等旧DTO使用
            // CreateMap<PrescriptionItemInputDto, PrescriptionItem>() 已在上方新映射中定义

            // PrescriptionUpdateDto -> Prescription
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            CreateMap<PrescriptionUpdateDto, Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.ReferencedFormulas, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.FormulaSource, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // PrescriptionEditDto -> Prescription
            CreateMap<PrescriptionEditDto, Prescription>()
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.ReferencedFormulas, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.FormulaSource, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.Ignore()) // EditDto无此字段
                // 忽略 BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
