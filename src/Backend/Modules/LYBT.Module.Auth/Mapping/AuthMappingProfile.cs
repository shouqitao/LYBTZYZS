using AutoMapper;
using LYBT.Models.Auth;
using LYBT.Models.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Core;

namespace LYBT.Module.Auth.Mapping
{
    /// <summary>
    /// 认证模块AutoMapper配置 - 模块标准化重构
    /// </summary>
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            // 用户到BaseUser的映射
            CreateMap<UserModel, BaseUser>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
                .ForMember(dest => dest.LastLoginTime, opt => opt.MapFrom(src => src.LastLoginTime))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdateTime))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

            // 密码修改相关映射
            CreateMap<ChangePasswordRequestDto, ChangePasswordRequestDto>()
                .ReverseMap();

            CreateMap<ChangeSysAdminPasswordDto, ChangeSysAdminPasswordDto>()
                .ReverseMap();

            // 系统管理员密钥映射
            CreateMap<AdminSecretModel, AdminSecretModel>()
                .ReverseMap();

            // === UltraThink Auth模块映射配置 ===
            // 仅配置后端Shared基础模型与Backend数据模型的映射
            // Frontend显示模型映射将在Frontend项目中单独配置
            
            // 认证会话映射：BaseAuthSession ↔ AuthSessionModel
            CreateMap<BaseAuthSession, AuthSessionModel>()
                .ReverseMap();

            // 登录尝试映射：BaseLoginAttempt ↔ LoginAttemptModel
            CreateMap<BaseLoginAttempt, LoginAttemptModel>()
                .ReverseMap();

            // 安全日志映射：BaseSecurityLog ↔ SecurityLogModel
            CreateMap<BaseSecurityLog, SecurityLogModel>()
                .ReverseMap();
        }
    }
}