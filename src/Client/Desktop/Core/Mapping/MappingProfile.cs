using AutoMapper;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Core.Mapping
{

    /// <summary>
    /// AutoMapper 映射配置文件 - UltraThink v2.0 简化版
    /// 只包含Client层必要的DTO工具映射，无Info层转换
    /// </summary>
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {
            // UltraThink v2.0: Client层极简映射配置
            // 移除所有DTO→Info映射，Client直接使用DTO

            // 仅保留必要的DTO之间的工具映射
            // 例如：DetailDto → UpdateDto 用于编辑功能
            CreateMap<UserDto, UserUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

            CreateMap<PatientDto, PatientUpdateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.Age))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address));

            // 注意：v2.0架构中，大部分映射应该在Server层完成
            // Client层主要直接使用DTO进行UI绑定
        }
    }
}
