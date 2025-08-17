using AutoMapper;
using LYBT.Desktop.Core.Models.Auth;
using LYBT.Desktop.Core.Models.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Desktop.Auth.Mappings
{
    /// <summary>
    /// Auth模块 AutoMapper 映射配置
    /// UltraThink架构: DTO ↔ Info 模型映射，确保层级分离
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            ConfigureLoginMappings();
            ConfigureSessionMappings();
        }

        /// <summary>
        /// 配置登录相关映射
        /// </summary>
        private void ConfigureLoginMappings()
        {
            // LoginInfo → LoginRequest 映射：前端模型到API请求
            CreateMap<LoginInfo, LoginRequest>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username.Trim()))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.ClientIp, opt => opt.MapFrom(src => src.ClientIp))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.LoginType, opt => opt.MapFrom(src => src.LoginType))
                .ForMember(dest => dest.RememberMe, opt => opt.MapFrom(src => src.RememberMe));

            // LoginResponse → LoginInfo 映射：API响应到前端模型
            CreateMap<LoginResponse, LoginInfo>()
                .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src.Token))
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.IsLoggedIn, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.Token)))
                .ForMember(dest => dest.IsLoggingIn, opt => opt.Ignore())
                .ForMember(dest => dest.HasSavedPassword, opt => opt.Ignore())
                .ForMember(dest => dest.IsApiOnline, opt => opt.Ignore())
                .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore())
                .ForMember(dest => dest.StatusMessage, opt => opt.MapFrom(src => "登录成功"))
                // 以下为请求相关属性，从响应映射时忽略
                .ForMember(dest => dest.Username, opt => opt.Ignore())
                .ForMember(dest => dest.Password, opt => opt.Ignore())
                .ForMember(dest => dest.ClientIp, opt => opt.Ignore())
                .ForMember(dest => dest.UserAgent, opt => opt.Ignore())
                .ForMember(dest => dest.LoginType, opt => opt.Ignore())
                .ForMember(dest => dest.RememberMe, opt => opt.Ignore());

            // 复合映射：将LoginResponse合并到现有LoginInfo
            CreateMap<(LoginInfo loginInfo, LoginResponse response), LoginInfo>()
                .ConvertUsing((src, dest, context) =>
                {
                    var result = context.Mapper.Map<LoginInfo>(src.loginInfo);
                    result.Token = src.response.Token;
                    result.User = src.response.User;
                    result.IsLoggedIn = !string.IsNullOrEmpty(src.response.Token);
                    result.IsLoggingIn = false;
                    result.ErrorMessage = null;
                    result.StatusMessage = "登录成功";
                    return result;
                });
        }

        /// <summary>
        /// 配置会话相关映射
        /// </summary>
        private void ConfigureSessionMappings()
        {
            // UserInfo → BaseUser 映射：前端用户模型到共享基础模型
            CreateMap<UserInfo, BaseUser>()
                .IncludeAllDerived();

            // BaseUser → UserInfo 映射：共享基础模型到前端用户模型
            CreateMap<BaseUser, UserInfo>()
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            // 预留：如果将来需要其他Auth相关的映射可以在这里添加
            // 例如：ChangePasswordRequest, LogoutRequest等的映射
        }

        /// <summary>
        /// 创建AutoMapper实例 - 包含ILoggerFactory参数
        /// </summary>
        public static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
                cfg.AddProfile(new MappingProfile()), 
                NullLoggerFactory.Instance);
                
            return config.CreateMapper();
        }
    }
}