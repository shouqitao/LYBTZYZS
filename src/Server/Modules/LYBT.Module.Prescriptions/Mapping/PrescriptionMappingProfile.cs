using AutoMapper;
using LYBT.Entities.Prescriptions;

// using LYBT.Entities.Compatibility; // 移除：已删除的Compatibility实体
// using LYBT.Shared.Models.Contracts.Compatibility; // 移除：已删除的Compatibility契约
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Mapping
{

    /// <summary>
    /// 表示PrescriptionMappingProfile。
    /// </summary>
    public class PrescriptionMappingProfile : Profile
    {

        public PrescriptionMappingProfile()
        {
            // Prescription -> PrescriptionDto
            CreateMap<Prescription, PrescriptionDto>()
                .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore()); // 计算属性

            // Prescription -> PrescriptionDetailDto
            CreateMap<Prescription, PrescriptionDetailDto>()
                .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.DuplicateWarning, opt => opt.Ignore())
                .ForMember(dest => dest.MissingDrugWarning, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionNo, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalAdvice, opt => opt.Ignore());

            // PrescriptionItem -> PrescriptionItemDto
            CreateMap<PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.Dosage, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore());

            // PrescriptionCreateDto -> Prescription
            CreateMap<PrescriptionCreateDto, Prescription>()
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
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

            // PrescriptionItemInputDto -> PrescriptionItem
            CreateMap<PrescriptionItemInputDto, PrescriptionItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());

            // PrescriptionUpdateDto -> Prescription
            CreateMap<PrescriptionUpdateDto, Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
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
