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
        // Formula -> FormulaDto
        CreateMap<LYBT.Entities.Formula.Formula, FormulaDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        // Formula -> FormulaDetailDto
        CreateMap<LYBT.Entities.Formula.Formula, FormulaDetailDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        // FormulaCreateDto -> Formula
        CreateMap<FormulaCreateDto, LYBT.Entities.Formula.Formula>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
            .ForMember(dest => dest.Property, opt => opt.Ignore())
            .ForMember(dest => dest.Herbs, opt => opt.Ignore())
            .ForSourceMember(src => src.Instructions, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.Indications, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.Contraindications, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.Preparation, opt => opt.DoNotValidate())
            // BaseEntity 审计字段
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // FormulaUpdateDto -> Formula
        CreateMap<FormulaUpdateDto, LYBT.Entities.Formula.Formula>()
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
