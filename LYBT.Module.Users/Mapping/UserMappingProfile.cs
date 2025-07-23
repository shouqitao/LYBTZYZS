using AutoMapper;
using LYBT.Common.Enums.Users;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users.Models;

namespace LYBT.Module.Users.Mapping {

    /// <summary>
    /// 用户实体与DTO映射配置（AutoMapper用）
    /// </summary>
    public class UserMappingProfile : Profile {

        public UserMappingProfile() {
            // 用户实体转DTO
            CreateMap<UserModel, UserDto>()
                .ForMember(dest => dest.Roles,
                    opt => opt.MapFrom(src => src.Roles ?? new List<UserRole>()));

            // 新增DTO转实体
            CreateMap<UserCreateDto, UserModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.PinyinCode, opt => opt.Ignore());

            // 详情DTO转实体
            CreateMap<UserDetailDto, UserModel>()
                .ForMember(dest => dest.UserName, opt => opt.Ignore()) // 用户名不允许修改
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.PinyinCode, opt => opt.Ignore());

            // 用户实体转详情DTO
            CreateMap<UserModel, UserDetailDto>();
        }
    }
}