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
        // Issue #1562 Phase 2: 已删除ConsultationStatus/StartTime/EndTime字段映射
        CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDto>()
            .ForMember(dest => dest.PatientId, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientName, opt => opt.Ignore())
            .ForMember(dest => dest.DoctorName, opt => opt.Ignore());

        // Issue #1562 Phase 2: 已删除ConsultationDetailDto类型的映射配置

        // ConsultationCreateDto -> Consultation
        CreateMap<ConsultationCreateDto, LYBT.Entities.Consultation.Consultation>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            .ForSourceMember(src => src.PatientName, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.DoctorName, opt => opt.DoNotValidate())
            // Issue #1562 Phase 2: 已删除StartTime字段引用
            // BaseEntity 审计字段
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // ConsultationUpdateDto -> Consultation
        // Issue #1562 Phase 2: 已删除ConsultationStatus/EndTime字段映射
        CreateMap<ConsultationUpdateDto, LYBT.Entities.Consultation.Consultation>()
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
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
