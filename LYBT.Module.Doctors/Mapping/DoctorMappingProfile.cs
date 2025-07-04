using AutoMapper;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;

namespace LYBT.Module.Doctors.Mapping {

    /// <summary>
    /// 医生实体与DTO的AutoMapper映射配置
    /// </summary>
    public class DoctorMappingProfile : Profile {

        public DoctorMappingProfile() {
            CreateMap<DoctorModel, DoctorDto>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.User != null ? s.User.RealName : string.Empty))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.User != null ? s.User.PhoneNumber : string.Empty));

            CreateMap<DoctorModel, DoctorDetailDto>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.User != null ? s.User.RealName : string.Empty))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.User != null ? s.User.PhoneNumber : string.Empty));

            CreateMap<DoctorCreateDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore());
        }
    }
}