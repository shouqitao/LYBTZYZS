using AutoMapper;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Auth.Mapping
{

    /// <summary>
    /// 认证模块AutoMapper配置 - 模块标准化重构
    /// </summary>
    public class AuthMappingProfile : Profile
    {

        public AuthMappingProfile()
        {
            // 用户到UserDto的映射 - UltraThink v2.0简化版（替换BaseUser）
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UsernName))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdatedAt));

            // 密码修改相关映射
            CreateMap<ChangePasswordRequest, ChangePasswordRequest>()
                .ReverseMap();

            CreateMap<ChangeSysAdminPassword, ChangeSysAdminPassword>()
                .ReverseMap();

            // 系统管理员密钥映射
            CreateMap<AdminSecretModel, AdminSecretModel>()
                .ReverseMap();

            // === UltraThink Auth模块映射配置 ===
            // 仅配置后端Shared基础模型与Backend数据模型的映射
            // Frontend显示模型映射将在Frontend项目中单独配置

            // 认证会话映射：BaseAuthSession → AuthSession
            CreateMap<BaseAuthSession, AuthSession>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? Guid.Empty))
                .ForMember(dest => dest.TokenHash, opt => opt.Ignore()) // 由业务层设置
                .ForMember(dest => dest.LoginTime, opt => opt.MapFrom(src => src.LoginTime))
                .ForMember(dest => dest.LogoutTime, opt => opt.MapFrom(src => src.LogoutTime))
                .ForMember(dest => dest.ExpiryTime, opt => opt.Ignore()) // 由业务层设置
                .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.ClientIp ?? string.Empty))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.IsRevoked, opt => opt.MapFrom(src => src.Status == AuthSessionStatus.Revoked))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
                    src.Status == AuthSessionStatus.Active ? CommonStatus.Enabled : CommonStatus.Disabled));

            // 认证会话映射：AuthSession → BaseAuthSession
            CreateMap<AuthSession, BaseAuthSession>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.Ignore()) // 需要通过关联查询获取
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.LoginType, opt => opt.MapFrom(src => LoginType.Password)) // 默认密码登录
                .ForMember(dest => dest.LoginTime, opt => opt.MapFrom(src => src.LoginTime))
                .ForMember(dest => dest.LogoutTime, opt => opt.MapFrom(src => src.LogoutTime))
                .ForMember(dest => dest.ClientIp, opt => opt.MapFrom(src => src.IpAddress))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
                    src.IsRevoked ? AuthSessionStatus.Revoked : 
                    src.Status == CommonStatus.Enabled ? AuthSessionStatus.Active : AuthSessionStatus.Expired))
                .ForMember(dest => dest.LastActivityTime, opt => opt.Ignore()) // 需要业务层计算
                .ForMember(dest => dest.DurationSeconds, opt => opt.Ignore()) // 需要业务层计算
                .ForMember(dest => dest.RememberMe, opt => opt.MapFrom(src => false)) // 默认false
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.LoginTime))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.LogoutTime));

            // 注意：登录尝试和安全日志映射已在UltraThink v2.0简化中移除
            // LoginAttemptModel 和 SecurityLogModel 已删除
        }
    }
}
