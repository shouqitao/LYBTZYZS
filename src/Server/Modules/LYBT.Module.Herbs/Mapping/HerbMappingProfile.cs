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
            CreateMap<Herb, HerbDetailDto>()
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdatedAt));

            // HerbCreateDto转药材实体
            CreateMap<HerbCreateDto, Herb>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.Empty))
                // 忽略BaseEntity的审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // HerbUpdateDto转药材实体
            CreateMap<HerbUpdateDto, Herb>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.Empty))
                // 忽略BaseEntity的审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // 药材实体转HerbDto（列表显示）
            CreateMap<Herb, HerbDto>()
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Properties, opt => opt.Ignore());

            // HerbImportDto转药材实体
            CreateMap<HerbImportDto, Herb>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.Empty))
                .ForMember(dest => dest.Usage, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore())
                .ForMember(dest => dest.CostPrice, opt => opt.Ignore())
                // 忽略BaseEntity的审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // ==================== UltraThink v2.0简化映射 ====================
            // 不再使用基础模型继承，直接映射
        }
    }
}
