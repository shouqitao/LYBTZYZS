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
                .ForMember(d => d.Name, o => o.MapFrom(s => s.User != null ? s.User.RealName : string.Empty))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.User != null ? s.User.PhoneNumber : string.Empty))
                .ForMember(d => d.Title, o => o.MapFrom(s => s.Title))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.PinyinCode, o => o.MapFrom(s => s.PinyinCode))
                .ForMember(d => d.LicenseNumber, o => o.MapFrom(s => s.LicenseNumber))
                .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender))
                .ForMember(d => d.Birthday, o => o.MapFrom(s => s.Birthday))
                .ForMember(d => d.Remark, o => o.MapFrom(s => s.Remark));

            // DoctorModel -> DoctorDetailDto 映射 (用于详情)
            CreateMap<DoctorModel, DoctorDetailDto>()
                .ForMember(d => d.Name, o => o.MapFrom(s => s.User != null ? s.User.RealName : string.Empty))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.User != null ? s.User.PhoneNumber : string.Empty))
                .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender))
                .ForMember(d => d.Birthday, o => o.MapFrom(s => s.Birthday))
                .ForMember(d => d.PinyinCode, o => o.MapFrom(s => s.PinyinCode))
                .ForMember(d => d.LicenseNumber, o => o.MapFrom(s => s.LicenseNumber))
                .ForMember(d => d.Title, o => o.MapFrom(s => s.Title))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.Remark, o => o.MapFrom(s => s.Remark));

            // DoctorCreateDto -> DoctorModel 映射 (新增)
            CreateMap<DoctorCreateDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.CreatedTime, o => o.MapFrom(s => DateTime.Now));

            // DoctorEditDto -> DoctorModel 映射 (编辑)
            CreateMap<DoctorEditDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.CreatedTime, o => o.Ignore());
        }
    }
}