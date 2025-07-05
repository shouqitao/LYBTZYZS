using AutoMapper;
using LYBT.Common.Enums.Users;
using System.Collections.Generic;
using System.Linq;
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
                .ForMember(dest => dest.Role,
                    opt => opt.MapFrom(src => src.Roles != null && src.Roles.Count > 0 ? src.Roles[0] : UserRole.Admin)) // 默认用Admin
                .ForMember(dest => dest.Roles,
                    opt => opt.MapFrom(src => src.Roles ?? new List<UserRole>()))
                .ForMember(dest => dest.RolesText, opt => opt.Ignore());

            // 新增DTO转实体
            CreateMap<UserCreateDto, UserModel>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

            // 详情DTO转实体
            CreateMap<UserDetailDto, UserModel>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        }
    }
}