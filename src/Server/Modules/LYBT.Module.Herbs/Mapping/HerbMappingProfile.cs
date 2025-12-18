using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Mapping
{

    /// <summary>
    /// 药材实体与DTO的AutoMapper映射配置
    /// OpenSpec: refactor-dto-simplification - 添加简化DTO映射
    /// </summary>
    public class HerbMappingProfile : Profile
    {

        public HerbMappingProfile()
        {
            // ============================================
            // 新简化DTO映射 (OpenSpec: refactor-dto-simplification)
            // ============================================

            // Herb -> HerbListDto (新)
            CreateMap<Herb, HerbListDto>();

            // Herb -> HerbDetailDto (扁平化详情DTO)
            // OpenSpec: dto-architecture-specification - HerbDto已删除，统一使用HerbDetailDto
            // 注：HerbDetailDto.Properties字段由Herb实体中不存在，需要忽略
            CreateMap<Herb, HerbDetailDto>()
                .ForMember(dest => dest.Properties, opt => opt.Ignore());

            // HerbInputDto -> Herb (Epic #1962: 统一创建/更新映射)
            // OpenSpec: refactor-dto-simplification - Status字段已从InputDto移除，由Service层管理
            CreateMap<HerbInputDto, Herb>()
                .ForMember(dest => dest.Status, opt => opt.Ignore()) // Status通过专用API修改
                // BaseEntity 审计字段（Service层自动设置）
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // HerbImportDto -> Herb (保留用于向后兼容)
            CreateMap<HerbImportDto, Herb>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore())
                .ForMember(dest => dest.CostPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore()) // HerbImportDto没有Category字段
                // BaseEntity 审计字段
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
        }
    }
}
