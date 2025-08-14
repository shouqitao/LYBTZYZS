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
            CreateMap<HerbModel, HerbDetailDto>()
                .IncludeBase<LYBT.Shared.Models.Core.BaseHerb, HerbDetailDto>();

            // HerbCreateDto转药材实体
            CreateMap<HerbCreateDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                                                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore())
                // Specification字段已删除
                .ForMember(dest => dest.Usage, opt => opt.Ignore());

            // HerbUpdateDto转药材实体
            CreateMap<HerbUpdateDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                                                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());

            // 药材实体转HerbDto（列表显示）
            CreateMap<HerbModel, HerbDto>()
                .IncludeBase<LYBT.Shared.Models.Core.BaseHerb, HerbDto>();

            // HerbImportDto转药材实体
            CreateMap<HerbImportDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                                                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore())
                // Specification字段已删除
                .ForMember(dest => dest.Usage, opt => opt.Ignore());

            // ==================== 基础模型映射 ====================

            // BaseHerb转HerbDetailDto
            CreateMap<LYBT.Shared.Models.Core.BaseHerb, HerbDetailDto>();

            // BaseHerb转HerbDto（列表显示）
            CreateMap<LYBT.Shared.Models.Core.BaseHerb, HerbDto>();
        }
    }
}