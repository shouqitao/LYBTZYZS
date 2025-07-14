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
            CreateMap<DoctorModel, DoctorDetailDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName))
                .ForMember(d => d.RealName, o => o.MapFrom(s => s.User.RealName))
                .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.User.PhoneNumber));

            // DoctorDetailDto -> DoctorModel 映射 (新增/编辑)
            CreateMap<DoctorDetailDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId))
                .ForMember(d => d.ContactNumber, o => o.MapFrom(s => s.ContactNumber))
                .ForMember(d => d.Gender, o => o.MapFrom(s => s.Gender))
                .ForMember(d => d.Birthday, o => o.MapFrom(s => s.Birthday))
                .ForMember(d => d.Title, o => o.MapFrom(s => s.Title))
                .ForMember(d => d.LicenseNumber, o => o.MapFrom(s => s.LicenseNumber))
                .ForMember(d => d.Specialty, o => o.MapFrom(s => s.Specialty))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.WorkStatus, o => o.MapFrom(s => s.WorkStatus))
                .ForMember(d => d.PinyinCode, o => o.MapFrom(s => s.PinyinCode))
                .ForMember(d => d.Remark, o => o.MapFrom(s => s.Remark));
        }
    }
}