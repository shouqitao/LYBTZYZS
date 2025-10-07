using AutoMapper;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services.Mapping
{
    /// <summary>
    /// 配方模块 AutoMapper 配置
    /// </summary>
    public class FormulaMappingProfile : Profile
    {
        public FormulaMappingProfile()
        {
            // FormulaCreateDto → FormulaDto
            CreateMap<FormulaCreateDto, FormulaDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                // Herbs 集合需要在 Service 层单独处理
                .ForMember(dest => dest.Herbs, opt => opt.Ignore())
                // Source 字段在 CreateDto 中不存在
                .ForMember(dest => dest.Source, opt => opt.Ignore());

            // FormulaUpdateDto → FormulaDto (用于更新现有实体)
            CreateMap<FormulaUpdateDto, FormulaDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                // Herbs 集合需要在 Service 层单独处理
                .ForMember(dest => dest.Herbs, opt => opt.Ignore())
                .ForMember(dest => dest.Source, opt => opt.Ignore());

            // FormulaDto → FormulaDto (用于克隆)
            CreateMap<FormulaDto, FormulaDto>();
        }
    }
}
