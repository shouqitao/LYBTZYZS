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
            CreateMap<FormulaTemplateModel, FormulaTemplateDto>().ReverseMap();
            CreateMap<FormulaTemplateModel, FormulaTemplateDetailDto>().ReverseMap();
            CreateMap<FormulaTemplateCreateDto, FormulaTemplateModel>();
            CreateMap<FormulaIngredientDto, FormulaTemplateHerbItem>().ReverseMap();
        }
    }
}