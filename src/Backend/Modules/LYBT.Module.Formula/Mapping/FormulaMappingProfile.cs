using AutoMapper;
using LYBT.Models.Formula;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formula.Mapping
{
    /// <summary>
    /// 简化的验方管理AutoMapper映射配置
    /// </summary>
    public class FormulaMappingProfile : Profile
    {
        public FormulaMappingProfile()
        {
            // FormulaModel -> FormulaDto
            CreateMap<FormulaModel, FormulaDto>()
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore())
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => 0));

            // FormulaModel -> FormulaDetailDto
            CreateMap<FormulaModel, FormulaDetailDto>()
                .ForMember(dest => dest.CreatedByName, opt => opt.Ignore())
                .ForMember(dest => dest.HerbCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => new List<FormulaHerbItemDto>()));

            // FormulaCreateDto -> FormulaModel
            CreateMap<FormulaCreateDto, FormulaModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedById, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled));

            // FormulaHerbItem -> FormulaHerbItemDto
            CreateMap<FormulaHerbItem, FormulaHerbItemDto>()
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => ""));

            // FormulaHerbItemCreateDto -> FormulaHerbItem
            CreateMap<FormulaHerbItemCreateDto, FormulaHerbItem>()
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.Unit, opt => opt.Ignore());

        }
    }
}