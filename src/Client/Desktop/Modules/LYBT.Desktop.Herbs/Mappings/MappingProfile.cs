using AutoMapper;

// UltraThink v2.0: HerbInfo妯″瀷宸茶绉婚櫎锛屼笉鍐嶅紩鐢?
// using LYBT.Desktop.Models.Herbs;

namespace LYBT.Desktop.Herbs.Mappings
{

    /// <summary>
    /// Herbs妯″潡 AutoMapper 鏄犲皠閰嶇疆
    /// UltraThink v2.0鏋舵瀯: 绉婚櫎Info妯″瀷鏄犲皠锛岀洿鎺ヤ娇鐢―TO
    /// </summary>
    public class MappingProfile : Profile
    {

        public MappingProfile()
        {
            ConfigureHerbMappings();
        }

        /// <summary>
        /// 閰嶇疆涓嵂鏉愮浉鍏虫槧灏?
        /// </summary>
        private void ConfigureHerbMappings()
        {
            // UltraThink v2.0: 鐩存帴浣跨敤DTO锛屾棤闇€鏄犲皠
        }

        /// <summary>
        /// 鍒涘缓AutoMapper瀹炰緥 - 浣跨敤寮€婧愮増鏈?4.0.0
        /// </summary>
        public static IMapper CreateMapper()
        {
            var config = new MapperConfiguration(cfg =>
                cfg.AddProfile(new MappingProfile()));

            return config.CreateMapper();
        }
    }
}
