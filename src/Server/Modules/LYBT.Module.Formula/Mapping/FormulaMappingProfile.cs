using AutoMapper;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formulas.Mapping
{

    /// <summary>
    /// 简化的验方管理AutoMapper映射配置
    /// OpenSpec: refactor-dto-simplification - 添加简化DTO映射
    /// </summary>
    public class FormulaMappingProfile : Profile
    {

        public FormulaMappingProfile()
        {
            // ============================================
            // 新简化DTO映射 (OpenSpec: refactor-dto-simplification)
            // ============================================

            // Formula -> FormulaListDto (新)
            CreateMap<Formula, FormulaListDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indication))
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => src.Herbs != null ? src.Herbs.Count : 0))
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()); // 由Service计算

            // Formula -> FormulaDetailDto (扁平化详情DTO)
            CreateMap<Formula, FormulaDetailDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indication))
                .ForMember(dest => dest.HerbCount, opt => opt.Ignore()) // 由Service计算
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()) // 由Service计算
                .ForMember(dest => dest.Herbs, opt => opt.Ignore()); // 单独映射子项

            // FormulaHerbItem -> FormulaHerbItemDto (新)
            CreateMap<FormulaHerbItem, FormulaHerbItemDto>();

            // ============================================
            // 旧DTO映射 (保持向后兼容，后续移除)
            // ============================================

            // Formula -> FormulaDetailDto
            CreateMap<Formula, FormulaDetailDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Indication)) // Issue #2014: Entity.Indication → DTO.Indications
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => src.Herbs != null ? src.Herbs.Count : 0))
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()); // FormulaHerbItem没有Herb导航属性，无法计算总价

            // FormulaHerbItem -> FormulaHerbItemDto
            CreateMap<FormulaHerbItem, FormulaHerbItemDto>();

            // FormulaInputDto -> Formula (用于更新场景，null值不覆盖)
            CreateMap<FormulaInputDto, Formula>()
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Property, opt => opt.Ignore())
                .ForMember(dest => dest.Herbs, opt => opt.Ignore())
                .ForSourceMember(src => src.Instructions, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Indications, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Contraindications, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Preparation, opt => opt.DoNotValidate())
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
