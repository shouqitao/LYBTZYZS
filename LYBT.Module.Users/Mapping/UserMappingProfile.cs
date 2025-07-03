using AutoMapper;
using LYBT.Common.Enums.Users;
using System.Collections.Generic;
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
                    opt => opt.MapFrom(src => src.Roles.FirstOrDefault()));
            // 新增DTO转实体
            CreateMap<UserCreateDto, UserModel>();
            // 编辑DTO转实体（密码字段需单独处理）
            CreateMap<UserEditDto, UserModel>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
        }
    }
}