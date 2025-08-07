using AutoMapper;
using LYBT.Models.Formula;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Formula.Mapping {

    /// <summary>
    /// 经验方模板实体与DTO的AutoMapper映射配置
    /// </summary>
    public class FormulaMappingProfile : Profile {

        public FormulaMappingProfile() {
            // FormulaModel -> FormulaDto 映射
            CreateMap<FormulaModel, FormulaDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Property)) // Property -> Category
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Effect)) // Effect -> Indications
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => src.Herbs != null ? src.Herbs.Count : 0))
                .ForMember(dest => dest.HerbNames, opt => opt.MapFrom(src => src.Herbs != null ? string.Join("，", src.Herbs.Select(h => h.HerbName)) : string.Empty));
                
            // FormulaDto -> FormulaModel 反向映射
            CreateMap<FormulaDto, FormulaModel>()
                .ForMember(dest => dest.Property, opt => opt.MapFrom(src => src.Category)) // Category -> Property
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Indications)) // Indications -> Effect
                .ForMember(dest => dest.Herbs, opt => opt.Ignore()); // 忽略Herbs，因为DTO中没有完整的药材信息
                
            // FormulaModel -> FormulaDetailDto 映射
            CreateMap<FormulaModel, FormulaDetailDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Property ?? "其他")) // Property -> Category
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Effect)) // Effect -> Indications  
                .ForMember(dest => dest.Efficacy, opt => opt.MapFrom(src => src.Effect)) // Effect -> Efficacy (相同内容)
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs ?? new List<FormulaHerbItem>()));
                
            // FormulaDetailDto -> FormulaModel 反向映射
            CreateMap<FormulaDetailDto, FormulaModel>()
                .ForMember(dest => dest.Property, opt => opt.MapFrom(src => src.Category)) // Category -> Property
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Indications ?? src.Efficacy)); // 优先使用Indications，其次使用Efficacy
                
            CreateMap<FormulaCreateDto, FormulaModel>();
            CreateMap<FormulaIngredientDto, FormulaHerbItem>().ReverseMap();
        }
    }
}