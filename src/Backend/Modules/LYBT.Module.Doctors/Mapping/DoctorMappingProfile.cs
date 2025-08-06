using AutoMapper;
using LYBT.Models.Doctors;
using LYBT.Shared.Models.Contracts.Doctors;

namespace LYBT.Module.Doctors.Mapping {

    /// <summary>
    /// 医生实体与DTO的AutoMapper映射配置（简化版）
    /// </summary>
    public class DoctorMappingProfile : Profile {

        public DoctorMappingProfile() {
            // DoctorModel -> DoctorDto 映射 (用于列表)
            CreateMap<DoctorModel, DoctorDto>();

            // DoctorModel -> DoctorDetailDto 映射 (用于详情)
            CreateMap<DoctorModel, DoctorDetailDto>()
                .ForMember(d => d.Username, opt => opt.MapFrom(s => s.User != null ? s.User.Username : null))
                .ForMember(d => d.TodayPatientCount, opt => opt.Ignore())
                .ForMember(d => d.TotalPatientCount, opt => opt.Ignore());

            // DoctorCreateDto -> DoctorModel 映射
            CreateMap<DoctorCreateDto, DoctorModel>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.PinYinCode, opt => opt.Ignore())
                .ForMember(d => d.Status, opt => opt.Ignore())
                .ForMember(d => d.CreateTime, opt => opt.Ignore())
                .ForMember(d => d.UpdateTime, opt => opt.Ignore());

            // DoctorUpdateDto -> DoctorModel 映射
            CreateMap<DoctorUpdateDto, DoctorModel>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.UserId, opt => opt.Ignore())
                .ForMember(d => d.User, opt => opt.Ignore())
                .ForMember(d => d.PinYinCode, opt => opt.Ignore())
                .ForMember(d => d.CreateTime, opt => opt.Ignore())
                .ForMember(d => d.UpdateTime, opt => opt.Ignore());
        }
    }
}