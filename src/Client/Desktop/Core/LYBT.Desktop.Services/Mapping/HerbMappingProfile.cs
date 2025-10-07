using AutoMapper;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services.Mapping
{
    /// <summary>
    /// 药材模块 AutoMapper 配置
    /// </summary>
    public class HerbMappingProfile : Profile
    {
        public HerbMappingProfile()
        {
            // HerbCreateDto → HerbDto
            CreateMap<HerbCreateDto, HerbDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                // 修复字段映射: Origin→Category, Spec→Properties
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Properties, opt => opt.MapFrom(src => src.Spec));

            // HerbUpdateDto → HerbDto (用于更新现有实体)
            CreateMap<HerbUpdateDto, HerbDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Remark, opt => opt.Ignore())
                // 修复字段映射: Origin→Category, Spec→Properties
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Properties, opt => opt.MapFrom(src => src.Spec));

            // HerbDto → HerbDto (用于克隆)
            CreateMap<HerbDto, HerbDto>();
        }
    }
}
