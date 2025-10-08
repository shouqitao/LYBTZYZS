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
        // MedicalCaseCreateDto -> MedicalCase
        CreateMap<MedicalCaseCreateDto, LYBT.Entities.MedicalCase.MedicalCase>()
            .ForMember(dest => dest.Consultation, opt => opt.Ignore())
            .ForMember(dest => dest.Prescription, opt => opt.Ignore())
            // BaseEntity 审计字段
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // MedicalCaseUpdateDto -> MedicalCase
        CreateMap<MedicalCaseUpdateDto, LYBT.Entities.MedicalCase.MedicalCase>()
            .ForMember(dest => dest.Consultation, opt => opt.Ignore())
            .ForMember(dest => dest.Prescription, opt => opt.Ignore())
            .ForSourceMember(src => src.DiagnosisSummary, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.ChiefComplaint, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.PresentIllness, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.DiagnosisResult, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.TreatmentPlan, opt => opt.DoNotValidate())
            // BaseEntity 审计字段
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // MedicalCase -> MedicalCaseDto
        CreateMap<LYBT.Entities.MedicalCase.MedicalCase, MedicalCaseDto>();

        // MedicalCase -> MedicalCaseDetailDto
        CreateMap<LYBT.Entities.MedicalCase.MedicalCase, MedicalCaseDetailDto>();
    }
    }
}
