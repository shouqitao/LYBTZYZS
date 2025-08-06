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
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Registration != null && src.Registration.Patient != null ? src.Registration.Patient.Name : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Registration != null && src.Registration.Doctor != null ? src.Registration.Doctor.Name : string.Empty))
                .ForMember(dest => dest.DiagnosisSummary, opt => opt.MapFrom(src => src.Consultation != null ? src.Consultation.Diagnosis : string.Empty))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Cashier != null ? src.Cashier.TotalAmount : 0))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.Cashier != null ? src.Cashier.PaymentStatus.ToString() : "未付费"));

            CreateMap<MedicalCaseModel, MedicalCaseDetailDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Registration != null && src.Registration.Patient != null ? src.Registration.Patient.Name : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.Registration != null && src.Registration.Doctor != null ? src.Registration.Doctor.Name : string.Empty));

            // DTO -> Model
            CreateMap<MedicalCaseCreateDto, MedicalCaseModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Registration, opt => opt.Ignore())
                .ForMember(dest => dest.Consultation, opt => opt.Ignore())
                .ForMember(dest => dest.TreatmentPlan, opt => opt.Ignore())
                .ForMember(dest => dest.Cashier, opt => opt.Ignore())
                .ForMember(dest => dest.Pharmacy, opt => opt.Ignore())
                .ForMember(dest => dest.TreatmentRoom, opt => opt.Ignore());

            CreateMap<MedicalCaseUpdateDto, MedicalCaseModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Registration, opt => opt.Ignore())
                .ForMember(dest => dest.Consultation, opt => opt.Ignore())
                .ForMember(dest => dest.TreatmentPlan, opt => opt.Ignore())
                .ForMember(dest => dest.Cashier, opt => opt.Ignore())
                .ForMember(dest => dest.Pharmacy, opt => opt.Ignore())
                .ForMember(dest => dest.TreatmentRoom, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}