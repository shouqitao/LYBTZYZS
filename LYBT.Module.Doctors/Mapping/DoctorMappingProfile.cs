using AutoMapper;
using LYBT.Models;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;

namespace LYBT.Module.Doctors.Mapping {
    /// <summary>
    /// 医生实体与DTO的AutoMapper映射配置
    /// </summary>
    public class DoctorMappingProfile : Profile {
        public DoctorMappingProfile() {
            CreateMap<DoctorModel, DoctorDto>().ReverseMap();
            CreateMap<DoctorModel, DoctorDetailDto>().ReverseMap();
            CreateMap<DoctorCreateDto, DoctorModel>();
        }
    }
}
