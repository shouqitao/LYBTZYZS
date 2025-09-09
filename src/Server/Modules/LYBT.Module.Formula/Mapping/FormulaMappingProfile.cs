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
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                .ForMember(dest => dest.Property, opt => opt.Ignore()) // 实体特有字段，不从DTO更新

                                                                       // 🎯 UltraThink修复：忽略实体中不存在的DTO字段
                .ForSourceMember(src => src.Instructions, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Indications, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Contraindications, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Preparation, opt => opt.DoNotValidate());

            // FormulaUpdateDto -> Formula - 🎯 UltraThink修复：处理字段不匹配问题
            CreateMap<FormulaUpdateDto, LYBT.Entities.Formula.Formula>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // 忽略ID
                .ForMember(dest => dest.Status, opt => opt.Ignore()) // 状态不通过UpdateDto更新
                .ForMember(dest => dest.Property, opt => opt.Ignore()) // 保持原有Property值
                .ForMember(dest => dest.Herbs, opt => opt.Ignore()) // Herbs需要特殊处理，不直接映射

                                                                    // 🎯 关键修复：忽略实体中不存在的DTO字段
                .ForSourceMember(src => src.Instructions, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Indications, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Contraindications, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Preparation, opt => opt.DoNotValidate())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // FormulaHerbItem -> FormulaHerbItemDto
            CreateMap<FormulaHerbItem, FormulaHerbItemDto>()
                .ForMember(dest => dest.HerbName, opt => opt.MapFrom(static src => string.Empty)); // 需要从Herb关联获取

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
