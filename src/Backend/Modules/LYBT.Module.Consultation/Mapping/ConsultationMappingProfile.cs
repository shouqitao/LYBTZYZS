using AutoMapper;
using LYBT.Models.Consultation;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Mapping
{
    /// <summary>
    /// 看诊映射配置（替代DiagnosisTreatmentMappingProfile）
    /// </summary>
    public class ConsultationMappingProfile : Profile
    {
        public ConsultationMappingProfile()
        {
            // Model -> DTO
            CreateMap<ConsultationModel, ConsultationDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Patient != null ? 
                    src.MedicalCase.Registration.Patient.Name : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Doctor != null ? 
                    src.MedicalCase.Registration.Doctor.Name : string.Empty));

            CreateMap<ConsultationModel, ConsultationDetailDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Patient != null ? 
                    src.MedicalCase.Registration.Patient.Name : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Doctor != null ? 
                    src.MedicalCase.Registration.Doctor.Name : string.Empty));

            // DTO -> Model
            CreateMap<ConsultationCreateDto, ConsultationModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationTime, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore());

            CreateMap<ConsultationUpdateDto, ConsultationModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationTime, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}