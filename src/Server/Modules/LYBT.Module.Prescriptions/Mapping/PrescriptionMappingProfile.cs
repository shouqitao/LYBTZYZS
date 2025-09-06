using AutoMapper;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Mapping {

    /// <summary>
    /// 表示PrescriptionMappingProfile。
    /// </summary>
    public class PrescriptionMappingProfile : Profile {

        public PrescriptionMappingProfile() {
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
            CreateMap<PrescriptionItemModel, PrescriptionItemDto>();

            // 创建映射 - 忽略自动字段
            CreateMap<PrescriptionCreateDto, Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            // CreateTime字段已删除（UltraThink v2.0简化）
            // .ForMember(dest => dest.CreateTime, opt => opt.Ignore());

            CreateMap<PrescriptionItemCreateDto, PrescriptionItemModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // 编辑映射 - 忽略不可修改字段
            CreateMap<PrescriptionEditDto, Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // UltraThink v2.0简化：CreateTime字段已删除
        }
    }
}
