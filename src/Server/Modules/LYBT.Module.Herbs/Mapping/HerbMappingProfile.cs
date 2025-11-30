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
            // Herb -> HerbDto
            // Epic #1962: Category字段现已支持，移除Ignore
            CreateMap<Herb, HerbDto>()
                .ForMember(dest => dest.Properties, opt => opt.Ignore()); // Properties暂不支持

            // Herb -> HerbDetailDto
            CreateMap<Herb, HerbDetailDto>();

            // HerbInputDto -> Herb (Epic #1962: 统一创建/更新映射)
            // 注意：Category字段已添加，PinYinCode由Service层自动生成
            CreateMap<HerbInputDto, Herb>()
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
