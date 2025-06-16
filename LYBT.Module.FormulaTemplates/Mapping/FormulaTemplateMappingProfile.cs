using AutoMapper;
using LYBT.Models;
using LYBT.Models.FormulaTemplates;
using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Module.Herbs.Dtos;

namespace LYBT.Module.FormulaTemplates.Mapping {
    /// <summary>
    /// 经验方模板实体与DTO的AutoMapper映射配置
    /// </summary>
    public class FormulaTemplateMappingProfile : Profile {
        public FormulaTemplateMappingProfile() {
            CreateMap<FormulaTemplateModel, FormulaTemplateDto>().ReverseMap();
            CreateMap<FormulaTemplateModel, FormulaTemplateDetailDto>().ReverseMap();
            CreateMap<FormulaTemplateCreateDto, FormulaTemplateModel>();
            CreateMap<HerbDto, HerbItemModel>().ReverseMap();
        }
    }
}
