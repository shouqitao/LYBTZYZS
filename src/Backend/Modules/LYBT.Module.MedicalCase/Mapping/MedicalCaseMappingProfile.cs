using AutoMapper;
using LYBT.Models.MedicalCase;
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
            // Model -> DTO
            CreateMap<MedicalCaseModel, MedicalCaseDto>()
                .ForMember(dest => dest.DiagnosisSummary, opt => opt.MapFrom(src => src.Consultation != null ? src.Consultation.Diagnosis : string.Empty));

            CreateMap<MedicalCaseModel, MedicalCaseDetailDto>();

            // DTO -> Model
            CreateMap<MedicalCaseCreateDto, MedicalCaseModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Consultation, opt => opt.Ignore());

            CreateMap<MedicalCaseUpdateDto, MedicalCaseModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Consultation, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}