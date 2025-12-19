using AutoMapper;
using LYBT.Entities.Consultations;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultations.Mapping
{
    /// <summary>
    /// 诊疗模块 AutoMapper 映射配置
    /// </summary>
    public class ConsultationMappingProfile : Profile
    {
        public ConsultationMappingProfile()
        {
            // Consultation -> ConsultationDetailDto
            // Issue #1562 Phase 2: 已删除ConsultationStatus/StartTime/EndTime字段映射
            CreateMap<Consultation, ConsultationDetailDto>()
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore());

            // Issue #1562 Phase 2: 已删除ConsultationDetailDto类型的映射配置

            // ConsultationInputDto -> Consultation
            // Issue #1562 Phase 2: 已删除ConsultationStatus/EndTime字段映射
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            // OpenSpec: refactor-dto-simplification - PatientName/DoctorName已从InputDto移除
            // OpenSpec: consultation-field-alignment - PrescriptionEnabled已移至MedicalCase
            CreateMap<ConsultationInputDto, Consultation>()
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                // BaseEntity 审计字段
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Consultation -> ConsultationListDto
            // OpenSpec: refactor-dto-simplification - 列表视图最小字段集
            CreateMap<Consultation, ConsultationListDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore());
        }
    }
}
