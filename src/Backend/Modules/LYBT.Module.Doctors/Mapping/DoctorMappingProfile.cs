using AutoMapper;
using LYBT.Models.Doctors;
using LYBT.Shared.Models.Contracts.Doctors;

namespace LYBT.Module.Doctors.Mapping {

    /// <summary>
    /// 医生实体与DTO的AutoMapper映射配置
    /// </summary>
    public class DoctorMappingProfile : Profile {

        public DoctorMappingProfile() {
            // DoctorModel -> DoctorDto 映射 (用于列表)
            CreateMap<DoctorModel, DoctorDto>()
                .ForMember(d => d.Username, opt => opt.MapFrom(s => s.User.Username))
                .ForMember(d => d.RealName, opt => opt.MapFrom(s => s.User.RealName))
                .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.User.PhoneNumber))
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.Age, opt => opt.Ignore()); // 通过属性计算

            // DoctorModel -> DoctorDetailDto 映射 (用于详情)
            CreateMap<DoctorModel, DoctorDetailDto>()
                .ForMember(d => d.Username, opt => opt.MapFrom(s => s.User.Username))
                .ForMember(d => d.RealName, opt => opt.MapFrom(s => s.User.RealName))
                .ForMember(d => d.PhoneNumber, opt => opt.MapFrom(s => s.User.PhoneNumber))
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.Age, opt => opt.Ignore()); // 通过属性计算

            // DoctorDetailDto -> DoctorModel 映射 (新增/编辑)
            CreateMap<DoctorDetailDto, DoctorModel>()
                .ForMember(d => d.User, opt => opt.Ignore()) // 不自动映射User对象
                .ForMember(d => d.CreateTime, opt => opt.Ignore()) // 在Service中设置
                .ForMember(d => d.Age, opt => opt.Ignore()); // 计算属性不映射
        }
    }
}