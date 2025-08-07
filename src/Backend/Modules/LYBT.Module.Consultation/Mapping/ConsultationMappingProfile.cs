using AutoMapper;
using LYBT.Models.Consultation;
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
            // ConsultationModel -> ConsultationDto
            CreateMap<ConsultationModel, ConsultationDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore()) // 需要从关联数据获取
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())  // 需要从关联数据获取
                .ForMember(dest => dest.Status, opt => opt.Ignore());      // 需要根据业务逻辑计算

            // ConsultationModel -> ConsultationDetailDto
            CreateMap<ConsultationModel, ConsultationDetailDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore()) // 需要从关联数据获取
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore()); // 需要从关联数据获取

            // ConsultationCreateDto -> ConsultationModel
            CreateMap<ConsultationCreateDto, ConsultationModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.ConsultationTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Shared.Models.Enums.CommonStatus.Enabled));

            // ConsultationStartDto -> ConsultationModel
            CreateMap<ConsultationStartDto, ConsultationModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.ConsultationTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Shared.Models.Enums.CommonStatus.Enabled));

            // ConsultationUpdateDto -> ConsultationModel (仅更新非空字段)
            CreateMap<ConsultationUpdateDto, ConsultationModel>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}