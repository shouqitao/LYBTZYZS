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
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName)) // 统一命名
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreatedAt)) // 映射审计字段
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdatedAt)); // 映射审计字段

            // UserCreateDto转用户实体
            CreateMap<UserCreateDto, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username)) // 映射Username到UserName
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID由业务逻辑处理
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 密码由业务逻辑处理
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()) // 由业务逻辑自动生成
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.Remark, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // UserUpdateDto转用户实体
            CreateMap<UserUpdateDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID由业务逻辑处理
                .ForMember(dest => dest.UserName, opt => opt.Ignore()) // 用户名不允许修改
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 密码由业务逻辑处理
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()) // 由业务逻辑自动生成
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.Remark, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
        }
    }
}
