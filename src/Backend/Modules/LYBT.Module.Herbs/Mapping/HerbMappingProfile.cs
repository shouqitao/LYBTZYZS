using AutoMapper;
using LYBT.Shared.Models.Extensions;
using LYBT.Models.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using SharedHerbDetailDto = LYBT.Shared.Models.Contracts.Herbs.HerbDetailDto;
using SharedHerbCreateDto = LYBT.Shared.Models.Contracts.Herbs.HerbCreateDto;
using SharedHerbUpdateDto = LYBT.Shared.Models.Contracts.Herbs.HerbUpdateDto;

namespace LYBT.Module.Herbs.Mapping {
    /// <summary>
    /// 药材实体与DTO的AutoMapper映射配置
    /// 更新以支持共享契约模型和基础模型继承
    /// </summary>
    public class HerbMappingProfile : Profile {
        public HerbMappingProfile() {
            // ==================== 共享契约映射 ====================
            
            // 药材实体转共享HerbDetailDto（API响应）
            CreateMap<HerbModel, SharedHerbDetailDto>()
                .IncludeBase<LYBT.Shared.Models.Core.BaseHerbModel, SharedHerbDetailDto>();

            // 共享HerbCreateDto转药材实体
            CreateMap<SharedHerbCreateDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore())
                .ForMember(dest => dest.Specification, opt => opt.MapFrom(src => 1))
                .ForMember(dest => dest.Usage, opt => opt.Ignore());

            // 共享HerbUpdateDto转药材实体
            CreateMap<SharedHerbUpdateDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());

            // ==================== 本地模型映射 ====================

            // 药材实体转本地HerbDto（内部使用）
            CreateMap<HerbModel, HerbDto>()
                .IncludeBase<LYBT.Shared.Models.Core.BaseHerbModel, HerbDto>();

            // ==================== 基础模型映射 ====================

            // BaseHerbModel转共享HerbDetailDto
            CreateMap<LYBT.Shared.Models.Core.BaseHerbModel, SharedHerbDetailDto>();

            // BaseHerbModel转本地HerbDto
            CreateMap<LYBT.Shared.Models.Core.BaseHerbModel, HerbDto>();
        }
    }
}