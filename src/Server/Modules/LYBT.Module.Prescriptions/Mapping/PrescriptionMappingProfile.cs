using AutoMapper;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Mapping
{
    /// <summary>
    /// 处方模块AutoMapper映射配置
    /// OpenSpec: refactor-dto-simplification - 统一DTO映射（移除重复配置）
    /// </summary>
    public class PrescriptionMappingProfile : Profile
    {
        public PrescriptionMappingProfile()
        {
            // ============================================
            // Entity -> DTO 映射 (响应)
            // ============================================

            // Prescription -> PrescriptionListDto
            CreateMap<Prescription, PrescriptionListDto>()
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()) // 由Service计算
                .ForMember(dest => dest.Status, opt => opt.Ignore()); // 实体无此字段

            // Prescription -> PrescriptionDetailDto
            // OpenSpec: simplify-medicalcase-dataflow - Indication/FormulaSource/Diagnosis已从实体和DTO删除
            CreateMap<Prescription, PrescriptionDetailDto>()
                .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.DuplicateWarning, opt => opt.Ignore())
                .ForMember(dest => dest.MissingDrugWarning, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore()); // 单独映射子项

            // PrescriptionItem -> PrescriptionItemDto
            CreateMap<PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage))
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore());

            // ============================================
            // DTO -> Entity 映射 (输入)
            // ============================================

            // PrescriptionInputDto -> Prescription (统一输入)
            // OpenSpec: simplify-medicalcase-dataflow - Indication/FormulaSource已从实体删除
            CreateMap<PrescriptionInputDto, Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Id由系统生成或更新时保留
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore()) // 由Service设置
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore()) // Items单独处理
                // OpenSpec: refactor-server-ddd-aggregates - MedicalCase导航属性已移除
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // PrescriptionItemInputDto -> PrescriptionItem
            CreateMap<PrescriptionItemInputDto, PrescriptionItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Id由系统生成或更新时保留
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore()) // 由Service设置
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Dosage));
        }
    }
}
