using AutoMapper;
using LYBT.Module.FormulaTemplates.Models;
using LYBT.Module.FormulaTemplates.Models.Dtos;
using LYBT.Module.Herbs.Models;
using LYBT.Module.Herbs.Models.Dtos;

namespace LYBT.Module.FormulaTemplates.Mapping {

    /// <summary>
    /// 经验方模板实体与DTO的AutoMapper映射配置
    /// </summary>
    public class FormulaTemplateMappingProfile : Profile {

        public FormulaTemplateMappingProfile() {
            CreateMap<FormulaTemplateModel, FormulaTemplateDto>().ReverseMap();
            CreateMap<FormulaTemplateModel, FormulaTemplateDetailDto>().ReverseMap();
            CreateMap<FormulaTemplateCreateDto, FormulaTemplateModel>();
            CreateMap<HerbDto, FormulaTemplateHerbItem>().ReverseMap();
        }
    }
}