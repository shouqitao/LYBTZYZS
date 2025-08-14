using AutoMapper;
using LYBT.Entities.Users;
using SharedUserCreateDto = LYBT.Shared.Models.Contracts.Users.UserCreateDto;
using SharedUserDto = LYBT.Shared.Models.Contracts.Users.UserDto;
using SharedUserUpdateDto = LYBT.Shared.Models.Contracts.Users.UserUpdateDto;

namespace LYBT.Module.Users.Mapping
{

    /// <summary>
    /// 用户实体与DTO映射配置（AutoMapper用）
    /// 更新以支持共享契约模型和基础模型继承
    /// </summary>
    public class UserMappingProfile : Profile
    {

        public UserMappingProfile()
        {
            // ==================== 共享契约映射 ====================

            // 用户实体转共享UserDto（API响应）
            CreateMap<UserModel, SharedUserDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username)) // 统一命名
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime)) // 统一命名
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore()); // 忽略UpdateTime字段

            // 共享UserCreateDto转用户实体
            CreateMap<SharedUserCreateDto, UserModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore());

            // 共享UserUpdateDto转用户实体
            CreateMap<SharedUserUpdateDto, UserModel>()
                .ForMember(dest => dest.Username, opt => opt.Ignore()) // 用户名不允许修改
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore()); // 忽略UpdateTime字段
        }
    }
}