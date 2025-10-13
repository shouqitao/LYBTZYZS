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
            // Issue #1262: 显式映射所有字段，确保序列化成功
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.LastLoginTime, opt => opt.MapFrom(src => src.LastLoginTime))
                .ForMember(dest => dest.FailedLoginCount, opt => opt.MapFrom(src => src.FailedLoginCount))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            // UserCreateDto转用户实体
            CreateMap<UserCreateDto, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName)) // 映射Username到UserName
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 密码由业务逻辑处理
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()) // 由业务逻辑自动生成
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.Remark, opt => opt.Ignore())
                // 忽略 BaseEntity 审计字段
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // UserUpdateDto转用户实体
            CreateMap<UserUpdateDto, User>()
                .ForMember(dest => dest.UserName, opt => opt.Ignore()) // 用户名不允许修改
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 密码由业务逻辑处理
                .ForMember(dest => dest.FailedLoginCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()) // 由业务逻辑自动生成
                .ForMember(dest => dest.LastLoginTime, opt => opt.Ignore())
                .ForMember(dest => dest.Remark, opt => opt.Ignore())
                // 忽略 BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
