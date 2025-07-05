using AutoMapper;
using LYBT.Models.Doctors;
using LYBT.Module.Doctors.Dtos;

namespace LYBT.Module.Doctors.Mapping {
    /// <summary>
    /// 医生实体与DTO的AutoMapper映射配置
    /// </summary>
    public class DoctorMappingProfile : Profile {
        public DoctorMappingProfile() {
            // DoctorModel -> DoctorDto 映射 (用于列表)
            CreateMap<DoctorModel, DoctorDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName))
                .ForMember(d => d.RealName, o => o.MapFrom(s => s.User.RealName))
                .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.User.PhoneNumber))
                .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender));

            // DoctorModel -> DoctorDetailDto 映射 (用于详情)
            CreateMap<DoctorModel, DoctorDetailDto>();

            // DoctorCreateDto -> DoctorModel 映射 (新增)
            CreateMap<DoctorCreateDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.ContactNumber, o => o.MapFrom(s => s.ContactNumber))
                .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender));

            // DoctorEditDto -> DoctorModel 映射 (编辑)
            CreateMap<DoctorEditDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.ContactNumber, o => o.MapFrom(s => s.ContactNumber))
                .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender));
        }
    }
}