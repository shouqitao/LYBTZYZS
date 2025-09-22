using AutoMapper;

// UltraThink v2.0: HerbInfo模型已被移除，不再引用
// using LYBT.Desktop.Core.Models.Herbs;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Desktop.Herbs.Mappings
{

    /// <summary>
    /// Herbs模块 AutoMapper 映射配置
    /// UltraThink v2.0架构: 移除Info模型映射，直接使用DTO
    /// </summary>
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {
            ConfigureHerbMappings();
        }

        /// <summary>
        /// 配置中药材相关映射
        /// </summary>
        private void ConfigureHerbMappings()
        {
            // UltraThink v2.0: 直接使用DTO，无需映射
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
