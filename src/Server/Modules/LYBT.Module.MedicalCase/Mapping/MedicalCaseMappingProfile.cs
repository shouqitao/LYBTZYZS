using AutoMapper;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Mapping
{
    /// <summary>
    /// 医疗案例映射配置
    /// </summary>
    public class MedicalCaseMappingProfile : Profile
    {
        public MedicalCaseMappingProfile()
        {
            // Model -> DTO - 基础映射，Status映射到CaseStatus
            CreateMap<LYBT.Entities.MedicalCase.MedicalCase, MedicalCaseDto>()
                .ForMember(dest => dest.CaseStatus, opt => opt.MapFrom(src => src.Status));

            CreateMap<LYBT.Entities.MedicalCase.MedicalCase, MedicalCaseDetailDto>()
                .ForMember(dest => dest.CaseStatus, opt => opt.MapFrom(src => src.Status));

            // DTO -> Model - CaseStatus映射到Status，忽略计算属性和导航属性
            CreateMap<MedicalCaseCreateDto, LYBT.Entities.MedicalCase.MedicalCase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Consultations, opt => opt.Ignore()) // 修复：使用Consultations集合
                .ForMember(dest => dest.Prescription, opt => opt.Ignore());

            CreateMap<MedicalCaseUpdateDto, LYBT.Entities.MedicalCase.MedicalCase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Consultations, opt => opt.Ignore()) // 修复：使用Consultations集合
                .ForMember(dest => dest.Prescription, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<MedicalCaseDto, LYBT.Entities.MedicalCase.MedicalCase>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.CaseStatus))
                .ForMember(dest => dest.Consultations, opt => opt.Ignore()) // 修复：使用Consultations集合
                .ForMember(dest => dest.Prescription, opt => opt.Ignore());
        }
    }
}