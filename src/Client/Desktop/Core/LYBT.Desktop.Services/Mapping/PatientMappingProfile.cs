using AutoMapper;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services.Mapping
{
    /// <summary>
    /// 患者模块 AutoMapper 配置
    /// </summary>
    public class PatientMappingProfile : Profile
    {
        public PatientMappingProfile()
        {
            // PatientCreateDto → PatientDto
            CreateMap<PatientCreateDto, PatientDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore());

            // PatientUpdateDto → PatientDto (用于更新现有实体)
            CreateMap<PatientUpdateDto, PatientDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore());

            // PatientDto → PatientDto (用于克隆)
            CreateMap<PatientDto, PatientDto>();
        }
    }
}
