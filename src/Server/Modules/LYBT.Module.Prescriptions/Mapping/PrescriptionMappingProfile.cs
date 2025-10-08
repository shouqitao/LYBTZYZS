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
            // Prescription -> PrescriptionDto - UltraThink v2.0简化版
            CreateMap<Prescription, PrescriptionDto>()
                .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore()) // 计算属性，由DTO自动计算
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()) // 计算属性，由DTO自动计算
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore()); // 计算属性，由DTO自动计算

            // Prescription -> PrescriptionDetailDto
            CreateMap<Prescription, PrescriptionDetailDto>()
                .ForMember(dest => dest.SingleDosePrice, opt => opt.Ignore()) // 计算属性，由DTO自动计算
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()) // 计算属性，由DTO自动计算
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore()); // 计算属性，由DTO自动计算

            // PrescriptionItemModel -> PrescriptionItemDto
            CreateMap<PrescriptionItem, PrescriptionItemDto>();

            // 创建映射 - 忽略自动字段
            CreateMap<PrescriptionCreateDto, Prescription>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                // 忽略外键和业务字段
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore())
                // 忽略打印相关字段
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                // 忽略导航属性
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

            // CreateTime字段已删除（UltraThink v2.0简化）
            // .ForMember(dest => dest.CreateTime, opt => opt.Ignore());
            CreateMap<PrescriptionItemCreateDto, PrescriptionItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());

            // 编辑映射 - 忽略不可修改字段
            CreateMap<PrescriptionUpdateDto, Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                // 忽略业务字段
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.FormulaSource, opt => opt.Ignore())
                // 忽略导航属性
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                // 忽略审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // 配伍记录映射 - 移除：HerbCompatibilityNote实体已删除
            // CreateMap<HerbCompatibilityNote, CompatibilityNoteDto>();
            // ... (所有相关映射已移除)
        }
    }
}
