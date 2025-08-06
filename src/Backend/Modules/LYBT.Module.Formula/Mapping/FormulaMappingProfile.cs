using AutoMapper;
using LYBT.Models.Formula;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Mapping
{
    /// <summary>
    /// 验方管理AutoMapper映射配置
    /// </summary>
    public class FormulaMappingProfile : Profile
    {
        public FormulaMappingProfile()
        {
            // FormulaModel -> FormulaDto
            CreateMap<FormulaModel, FormulaDto>()
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore())
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => src.Herbs.Count));

            // FormulaModel -> FormulaDetailDto
            CreateMap<FormulaModel, FormulaDetailDto>()
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore())
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => src.Herbs.Count))
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => src.Herbs.OrderBy(h => h.SortOrder)));

            // FormulaCreateDto -> FormulaModel
            CreateMap<FormulaCreateDto, FormulaModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Herbs, opt => opt.Ignore());

            // FormulaHerbItem -> FormulaHerbItemDto
            CreateMap<FormulaHerbItem, FormulaHerbItemDto>()
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => src.Herb != null ? src.Herb.Name : ""));

            // FormulaHerbItemCreateDto -> FormulaHerbItem (used in service)
            CreateMap<FormulaHerbItemCreateDto, FormulaHerbItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FormulaId, opt => opt.Ignore())
                .ForMember(dest => dest.Unit, opt => opt.Ignore())
                .ForMember(dest => dest.Price, opt => opt.Ignore())
                .ForMember(dest => dest.Herb, opt => opt.Ignore());
        }
    }
}