using AutoMapper;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services.Mapping
{
    /// <summary>
    /// 问诊模块 AutoMapper 配置
    /// </summary>
    public class ConsultationMappingProfile : Profile
    {
        public ConsultationMappingProfile()
        {
            // ConsultationCreateDto → ConsultationDto
            CreateMap<ConsultationCreateDto, ConsultationDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationStatus, opt => opt.MapFrom(src => ConsultationStatus.InProgress))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.EndTime, opt => opt.Ignore());

            // ConsultationUpdateDto → ConsultationDto (用于更新现有实体)
            CreateMap<ConsultationUpdateDto, ConsultationDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // ConsultationDto → ConsultationDto (用于克隆)
            CreateMap<ConsultationDto, ConsultationDto>();
        }
    }
}
