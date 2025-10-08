using AutoMapper;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Consultation.Mapping
{
    /// <summary>
    /// 诊疗模块 AutoMapper 映射配置
    /// </summary>
    public class ConsultationMappingProfile : Profile
    {
        public ConsultationMappingProfile()
    {
        // Consultation -> ConsultationDto
        CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDto>()
            .ForMember(dest => dest.ConsultationStatus, opt => opt.MapFrom(src =>
                src.Status == CommonStatus.Disabled ? ConsultationStatus.Completed : ConsultationStatus.InProgress))
            .ForMember(dest => dest.PatientId, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientName, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
            .ForMember(dest => dest.StartTime, opt => opt.Ignore())
            .ForMember(dest => dest.EndTime, opt => opt.Ignore());

        // Consultation -> ConsultationDetailDto
        CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDetailDto>()
            .ForMember(dest => dest.ConsultationStatus, opt => opt.MapFrom(src =>
                src.Status == CommonStatus.Disabled ? ConsultationStatus.Completed : ConsultationStatus.InProgress))
            .ForMember(dest => dest.PatientId, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientName, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
            .ForMember(dest => dest.StartTime, opt => opt.Ignore())
            .ForMember(dest => dest.EndTime, opt => opt.Ignore());

        // ConsultationDetailDto -> Consultation
        CreateMap<ConsultationDetailDto, LYBT.Entities.Consultation.Consultation>()
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                src.ConsultationStatus == ConsultationStatus.Completed
                    ? CommonStatus.Disabled
                    : CommonStatus.Enabled))
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForSourceMember(src => src.PatientName, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.DoctorName, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.StartTime, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.EndTime, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.ConsultationStatus, opt => opt.DoNotValidate())
            // BaseEntity 审计字段
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // ConsultationCreateDto -> Consultation
        CreateMap<ConsultationCreateDto, LYBT.Entities.Consultation.Consultation>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForSourceMember(src => src.PatientName, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.DoctorName, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.StartTime, opt => opt.DoNotValidate())
            // BaseEntity 审计字段
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // ConsultationUpdateDto -> Consultation
        CreateMap<ConsultationUpdateDto, LYBT.Entities.Consultation.Consultation>()
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                src.ConsultationStatus.HasValue && src.ConsultationStatus.Value == ConsultationStatus.Completed
                    ? CommonStatus.Disabled
                    : CommonStatus.Enabled))
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForSourceMember(src => src.ConsultationStatus, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.EndTime, opt => opt.DoNotValidate())
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
