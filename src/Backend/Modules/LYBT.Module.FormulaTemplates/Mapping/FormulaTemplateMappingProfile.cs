using AutoMapper;
using LYBT.Models.FormulaTemplates;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.FormulaTemplates.Mapping {

    /// <summary>
    /// 经验方模板实体与DTO的AutoMapper映射配置
    /// </summary>
    public class FormulaTemplateMappingProfile : Profile {

        public FormulaTemplateMappingProfile() {
            // FormulaTemplateModel -> FormulaTemplateDto 映射
            CreateMap<FormulaTemplateModel, FormulaTemplateDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Property)) // Property -> Category
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Effect)) // Effect -> Indications
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => src.Herbs != null ? src.Herbs.Count : 0))
                .ForMember(dest => dest.HerbNames, opt => opt.MapFrom(src => src.Herbs != null ? string.Join("，", src.Herbs.Select(h => h.HerbName)) : string.Empty));
                
            // FormulaTemplateDto -> FormulaTemplateModel 反向映射
            CreateMap<FormulaTemplateDto, FormulaTemplateModel>()
                .ForMember(dest => dest.Property, opt => opt.MapFrom(src => src.Category)) // Category -> Property
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Indications)) // Indications -> Effect
                .ForMember(dest => dest.Herbs, opt => opt.Ignore()); // 忽略Herbs，因为DTO中没有完整的药材信息
                
            // FormulaTemplateModel -> FormulaTemplateDetailDto 映射
            CreateMap<FormulaTemplateModel, FormulaTemplateDetailDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Property ?? "其他")) // Property -> Category
                .ForMember(dest => dest.Indications, opt => opt.MapFrom(src => src.Effect)) // Effect -> Indications  
                .ForMember(dest => dest.Efficacy, opt => opt.MapFrom(src => src.Effect)) // Effect -> Efficacy (相同内容)
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs ?? new List<FormulaTemplateHerbItem>()));
                
            // FormulaTemplateDetailDto -> FormulaTemplateModel 反向映射
            CreateMap<FormulaTemplateDetailDto, FormulaTemplateModel>()
                .ForMember(dest => dest.Property, opt => opt.MapFrom(src => src.Category)) // Category -> Property
                .ForMember(dest => dest.Effect, opt => opt.MapFrom(src => src.Indications ?? src.Efficacy)); // 优先使用Indications，其次使用Efficacy
                
            CreateMap<FormulaTemplateCreateDto, FormulaTemplateModel>();
            CreateMap<FormulaIngredientDto, FormulaTemplateHerbItem>().ReverseMap();
        }
    }
}