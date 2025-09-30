using AutoMapper;

namespace LYBT.Desktop.Auth.Mappings
{

    /// <summary>
    /// Auth模块 AutoMapper 映射配置
    /// UltraThink v2.0: 简化映射，直接使用DTO，无需Info模型
    /// </summary>
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {
            ConfigureAuthMappings();
        }

        /// <summary>
        /// 配置认证相关映射
        /// </summary>
        private void ConfigureAuthMappings()
        {
            // UltraThink v2.0: 简化映射配置
            // 如果将来需要其他Auth相关的映射可以在这里添加
            // 例如：UserDto的其他转换映射等

            // 目前大部分使用直接的DTO传递，减少不必要的映射
        }

        /// <summary>
        /// 创建AutoMapper实例 - 使用开源版本14.0.0
        /// </summary>
        public static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
                cfg.AddProfile(new MappingProfile()));

            return config.CreateMapper();
        }
    }
}
