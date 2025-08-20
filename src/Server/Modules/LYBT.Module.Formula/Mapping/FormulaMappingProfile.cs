using AutoMapper;
using LYBT.Entities.Formula;
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
            // Formula -> FormulaDto - UltraThink v2.0简化版
            CreateMap<LYBT.Entities.Formula.Formula, FormulaDto>()
                .ForMember(dest => dest.HerbCount, opt => opt.Ignore()) // 计算属性，由DTO自动计算
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()); // 计算属性，由DTO自动计算

            // Formula -> FormulaDetailDto  
            CreateMap<LYBT.Entities.Formula.Formula, FormulaDetailDto>()
                .ForMember(dest => dest.HerbCount, opt => opt.Ignore()) // 计算属性，由DTO自动计算
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore()) // 计算属性，由DTO自动计算
                .ForMember(dest => dest.Herbs, opt => opt.MapFrom(src => new List<FormulaHerbItemDto>())); // 需要单独处理

            // FormulaCreateDto -> Formula
            CreateMap<FormulaCreateDto, LYBT.Entities.Formula.Formula>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled));

            // FormulaUpdateDto -> Formula
            CreateMap<FormulaUpdateDto, LYBT.Entities.Formula.Formula>();

            // FormulaHerbItem -> FormulaHerbItemDto
            CreateMap<FormulaHerbItem, FormulaHerbItemDto>()
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(src => "")); // 需要从Herb关联获取

            // FormulaHerbItemCreateDto -> FormulaHerbItem
            CreateMap<FormulaHerbItemCreateDto, FormulaHerbItem>()
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));

            // FormulaHerbItemUpdateDto -> FormulaHerbItem
            CreateMap<FormulaHerbItemUpdateDto, FormulaHerbItem>()
                .ForMember(dest => dest.HerbId, opt => opt.MapFrom(src => src.HerbId))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));
        }
    }
}