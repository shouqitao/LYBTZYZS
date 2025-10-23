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
        // Issue #1570b: 添加所有诊断字段的显式映射（与UpdateDto保持一致）
        CreateMap<ConsultationCreateDto, LYBT.Entities.Consultation.Consultation>()
            // Status字段
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
            // 10个诊断字段 - 显式映射
            .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
            .ForMember(dest => dest.PresentIllness, opt => opt.MapFrom(src => src.PresentIllness))
            .ForMember(dest => dest.Inspection, opt => opt.MapFrom(src => src.Inspection))
            .ForMember(dest => dest.AuscultationOlfaction, opt => opt.MapFrom(src => src.AuscultationOlfaction))
            .ForMember(dest => dest.Inquiry, opt => opt.MapFrom(src => src.Inquiry))
            .ForMember(dest => dest.Palpation, opt => opt.MapFrom(src => src.Palpation))
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            .ForMember(dest => dest.TreatmentPrinciple, opt => opt.MapFrom(src => src.TreatmentPrinciple))
            .ForMember(dest => dest.MedicalAdvice, opt => opt.MapFrom(src => src.MedicalAdvice))
            .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
            // 忽略导航属性和源字段
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
        // Issue #1570b: 修复缺失的字段映射 - 添加所有10个诊断字段的显式映射
        CreateMap<ConsultationUpdateDto, LYBT.Entities.Consultation.Consultation>()
            // 10个诊断字段 - 显式映射确保数据正确传递
            .ForMember(dest => dest.ChiefComplaint, opt => opt.MapFrom(src => src.ChiefComplaint))
            .ForMember(dest => dest.PresentIllness, opt => opt.MapFrom(src => src.PresentIllness))
            .ForMember(dest => dest.Inspection, opt => opt.MapFrom(src => src.Inspection))
            .ForMember(dest => dest.AuscultationOlfaction, opt => opt.MapFrom(src => src.AuscultationOlfaction))
            .ForMember(dest => dest.Inquiry, opt => opt.MapFrom(src => src.Inquiry))
            .ForMember(dest => dest.Palpation, opt => opt.MapFrom(src => src.Palpation))
            .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
            .ForMember(dest => dest.TreatmentPrinciple, opt => opt.MapFrom(src => src.TreatmentPrinciple))
            .ForMember(dest => dest.MedicalAdvice, opt => opt.MapFrom(src => src.MedicalAdvice))
            .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
            // 忽略导航属性
            .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
            // BaseEntity 审计字段
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            // 条件：只映射非null的值（保留原有逻辑）
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
    }
}
