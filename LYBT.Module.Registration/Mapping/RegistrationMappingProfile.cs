using AutoMapper;
using LYBT.Models.Registration;
using LYBT.Module.Registration.Dtos;

namespace LYBT.Module.Registration.Mapping {

    /// <summary>
    /// 挂号实体与DTO的AutoMapper映射配置
    /// </summary>
    public class RegistrationMappingProfile : Profile {

        public RegistrationMappingProfile() {
            // 挂号实体 <=> 列表DTO
            CreateMap<RegistrationModel, RegistrationDto>().ReverseMap();
            // 挂号实体 <=> 详情DTO
            CreateMap<RegistrationModel, RegistrationDetailDto>().ReverseMap();
            // 新增DTO => 实体
            CreateMap<RegistrationCreateDto, RegistrationModel>();
            // 编辑DTO => 实体
            CreateMap<RegistrationEditDto, RegistrationModel>();
        }
    }
}