using AutoMapper;
using LYBT.Desktop.Core.Models.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Desktop.Herbs.Mappings
{
    /// <summary>
    /// Herbs模块 AutoMapper 映射配置
    /// UltraThink架构: DTO ↔ Info 模型映射，确保层级分离
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
            // DTO → Info 映射：API响应到前端模型
            CreateMap<HerbDto, HerbInfo>()
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.StatusDescription, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperationTime, opt => opt.Ignore())
                .ForMember(dest => dest.OperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == LYBT.Shared.Models.Enums.CommonStatus.Active))
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            CreateMap<HerbDetailDto, HerbInfo>()
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.StatusDescription, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperationTime, opt => opt.Ignore())
                .ForMember(dest => dest.OperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Stock, opt => opt.Ignore()) // HerbDetailDto没有Stock字段
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == LYBT.Shared.Models.Enums.CommonStatus.Active))
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());

            // Info → DTO 映射：前端模型到API请求
            CreateMap<HerbInfo, HerbCreateDto>()
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => (int)src.Stock))
                .ForMember(dest => dest.BatchNo, opt => opt.Ignore())
                .ForMember(dest => dest.ExpireDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
                    src.IsActive ? LYBT.Shared.Models.Enums.CommonStatus.Active : LYBT.Shared.Models.Enums.CommonStatus.Inactive));

            CreateMap<HerbInfo, HerbUpdateDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => 
                    src.IsActive ? LYBT.Shared.Models.Enums.CommonStatus.Active : LYBT.Shared.Models.Enums.CommonStatus.Inactive));

            // Info ↔ DTO 双向映射：为对话框兼容性支持
            CreateMap<HerbInfo, HerbDto>()
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => (int)src.Stock))
                .ForMember(dest => dest.WuBiCode, opt => opt.Ignore()) // HerbInfo没有WuBiCode属性
                .ReverseMap()
                .ForMember(dest => dest.Stock, opt => opt.MapFrom(src => (decimal)src.Stock))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Status == LYBT.Shared.Models.Enums.CommonStatus.Active))
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.StatusDescription, opt => opt.Ignore())
                .ForMember(dest => dest.Supplier, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperationTime, opt => opt.Ignore())
                .ForMember(dest => dest.OperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore())
                .ForMember(dest => dest.IsExpanded, opt => opt.Ignore())
                .ForMember(dest => dest.IsEditing, opt => opt.Ignore())
                .ForMember(dest => dest.IsLoading, opt => opt.Ignore());
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