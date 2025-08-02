using AutoMapper;
using LYBT.Models.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Mapping {

    /// <summary>
    /// 药材实体与DTO的AutoMapper映射配置
    /// 使用共享契约模型和基础模型继承
    /// </summary>
    public class HerbMappingProfile : Profile {

        public HerbMappingProfile() {
            // ==================== 共享契约映射 ====================

            // 药材实体转HerbDetailDto（API响应）
            CreateMap<HerbModel, HerbDetailDto>()
                .IncludeBase<LYBT.Shared.Models.Core.BaseHerbModel, HerbDetailDto>();

            // HerbCreateDto转药材实体
            CreateMap<HerbCreateDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.Specification, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.Usage, opt => opt.Ignore());

            // HerbUpdateDto转药材实体
            CreateMap<HerbUpdateDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());

            // 药材实体转HerbDto（列表显示）
            CreateMap<HerbModel, HerbDto>()
                .IncludeBase<LYBT.Shared.Models.Core.BaseHerbModel, HerbDto>();

            // HerbImportDto转药材实体
            CreateMap<HerbImportDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.Specification, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.Usage, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => LYBT.Shared.Models.Enums.HerbStatus.Active));

            // ==================== 基础模型映射 ====================

            // BaseHerbModel转HerbDetailDto
            CreateMap<LYBT.Shared.Models.Core.BaseHerbModel, HerbDetailDto>();

            // BaseHerbModel转HerbDto（列表显示）
            CreateMap<LYBT.Shared.Models.Core.BaseHerbModel, HerbDto>();
        }
    }
}