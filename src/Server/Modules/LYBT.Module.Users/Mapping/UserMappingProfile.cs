using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;

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
            // ==================== 现代化映射配置（UserMutationDto） ====================

            // 用户实体转UserDto（API响应和业务逻辑）
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username)); // 统一命名

            // UserCreateDto转用户实体
            CreateMap<UserCreateDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID由业务逻辑处理
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 密码由业务逻辑处理
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()); // 由业务逻辑自动生成

            // UserUpdateDto转用户实体
            CreateMap<UserUpdateDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID由业务逻辑处理
                .ForMember(dest => dest.Username, opt => opt.Ignore()) // 用户名不允许修改
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 密码由业务逻辑处理
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()); // 由业务逻辑自动生成
        }
    }
}
