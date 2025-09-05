using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Mapping
{

    /// <summary>
    /// 药材实体与DTO的AutoMapper映射配置
    /// 使用共享契约模型和基础模型继承
    /// </summary>
    public class HerbMappingProfile : Profile
    {

        public HerbMappingProfile()
        {
            // ==================== 共享契约映射 ====================

            // 药材实体转HerbDetailDto（API响应）
            CreateMap<Herb, HerbDetailDto>();

            // HerbCreateDto转药材实体
            CreateMap<HerbCreateDto, Herb>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore());

            // HerbUpdateDto转药材实体
            CreateMap<HerbUpdateDto, Herb>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // 药材实体转HerbDto（列表显示）
            CreateMap<Herb, HerbDto>();

            // HerbImportDto转药材实体
            CreateMap<HerbImportDto, Herb>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore());

            // ==================== UltraThink v2.0简化映射 ====================
            // 不再使用基础模型继承，直接映射
        }
    }
}
