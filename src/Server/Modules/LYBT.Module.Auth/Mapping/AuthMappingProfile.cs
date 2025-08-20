using AutoMapper;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
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
            // 用户到BaseUser的映射 - UltraThink v2.0简化版
            CreateMap<User, BaseUser>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RealName, opt => opt.MapFrom(src => src.RealName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));
                // CreateTime/LastLoginTime/Remark字段已删除（UltraThink v2.0简化）
                // .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
                // .ForMember(dest => dest.LastLoginTime, opt => opt.MapFrom(src => src.LastLoginTime))
                // .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark));

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
            
            // 认证会话映射：BaseAuthSession ↔ AuthSession
            CreateMap<BaseAuthSession, AuthSession>()
                .ReverseMap();

            // 注意：登录尝试和安全日志映射已在UltraThink v2.0简化中移除
            // LoginAttemptModel 和 SecurityLogModel 已删除
        }
    }
}