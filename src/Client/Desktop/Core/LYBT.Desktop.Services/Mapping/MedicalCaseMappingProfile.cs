using AutoMapper;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services.Mapping
{
    /// <summary>
    /// 医疗案例模块 AutoMapper 配置
    /// </summary>
    public class MedicalCaseMappingProfile : Profile
    {
        public MedicalCaseMappingProfile()
        {
            // MedicalCaseCreateDto → MedicalCaseDto
            CreateMap<MedicalCaseCreateDto, MedicalCaseDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                .ForMember(dest => dest.CaseStatus, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationDate, opt => opt.MapFrom(src => DateTime.Now))
                // 患者/医生信息需要在 Service 层填充
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationId, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());

            // MedicalCaseUpdateDto → MedicalCaseDto (用于更新现有实体)
            CreateMap<MedicalCaseUpdateDto, MedicalCaseDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CaseStatus, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationDate, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
                .ForMember(dest => dest.ConsultationId, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());

            // MedicalCaseDto → MedicalCaseDto (用于克隆)
            CreateMap<MedicalCaseDto, MedicalCaseDto>();
        }
    }
}
