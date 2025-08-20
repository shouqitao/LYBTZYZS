using AutoMapper;
using LYBT.Entities.Consultation;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Mapping
{
    /// <summary>
    /// 看诊模块 AutoMapper 映射配置
    /// </summary>
    public class ConsultationMappingProfile : Profile
    {
        public ConsultationMappingProfile()
        {
#if false
            // TODO: UltraThink v2.0 Refactor - 映射中使用了不存在的属性
            // Consultation -> ConsultationDto - UltraThink v2.0简化版
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore()) // 需要从关联数据获取
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore()); // 需要从关联数据获取
#endif

#if false
            // TODO: UltraThink v2.0 Refactor - 映射中使用了不存在的属性
            // Consultation -> ConsultationDetailDto
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDetailDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore()) // 需要从关联数据获取
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore()) // 需要从关联数据获取
                .ForMember(dest => dest.ConsultationTime, opt => opt.MapFrom(src => src.CreateTime)); // 使用CreateTime作为ConsultationTime
#endif

#if false
            // TODO: UltraThink v2.0 Refactor - 映射中使用了不存在的属性
            // ConsultationCreateDto -> Consultation - 基础映射
            CreateMap<ConsultationCreateDto, LYBT.Entities.Consultation.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Shared.Models.Enums.CommonStatus.Enabled));
#endif

#if false
            // TODO: UltraThink v2.0 Refactor - 映射中使用了不存在的属性
            // ConsultationUpdateDto -> Consultation - 仅更新非空字段
            CreateMap<ConsultationUpdateDto, LYBT.Entities.Consultation.Consultation>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
#endif
        }
    }
}