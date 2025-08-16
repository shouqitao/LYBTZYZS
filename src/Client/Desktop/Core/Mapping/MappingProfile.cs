using LYBT.Shared.Models.Contracts.Common;
using AutoMapper;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Desktop.Core.Models.Patients;

namespace LYBT.Desktop.Core.Mapping
{
    /// <summary>
    /// AutoMapper 映射配置文件
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 患者映射
            CreateMap<PatientDetailDto, PatientInfo>()
                .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IDNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

            CreateMap<PatientInfo, PatientDetailDto>()
                .ForMember(dest => dest.IDNumber, opt => opt.MapFrom(src => src.IdNumber));

            // 可以在这里添加更多映射配置
        }
    }
}