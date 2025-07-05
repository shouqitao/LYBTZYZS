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
            CreateMap<DoctorModel, DoctorDto>();

            // DoctorModel -> DoctorDetailDto 映射 (用于详情)
            CreateMap<DoctorModel, DoctorDetailDto>();

            // DoctorCreateDto -> DoctorModel 映射 (新增)
            CreateMap<DoctorCreateDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore());

            // DoctorEditDto -> DoctorModel 映射 (编辑)
            CreateMap<DoctorEditDto, DoctorModel>()
                .ForMember(d => d.User, o => o.Ignore());
        }
    }
}