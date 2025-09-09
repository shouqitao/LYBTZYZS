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
                .ForMember(dest => dest.Consultation, opt => opt.Ignore()) // 修复：使用单个Consultation对象
                .ForMember(dest => dest.Prescription, opt => opt.Ignore());

            CreateMap<MedicalCaseUpdateDto, LYBT.Entities.MedicalCase.MedicalCase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Consultation, opt => opt.Ignore()) // 导航属性忽略
                .ForMember(dest => dest.Prescription, opt => opt.Ignore()) // 导航属性忽略

                // 🎯 UltraThink修复：明确忽略不属于MedicalCase实体的DTO字段
                .ForMember(dest => dest.PatientName, opt => opt.Ignore()) // 显示字段，不更新
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore()) // 显示字段，不更新

                // 以下字段属于Consultation模块，不映射到MedicalCase
                .ForSourceMember(src => src.RegistrationId, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.DiagnosisSummary, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.ChiefComplaint, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.PresentIllness, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.PastHistory, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.DiagnosisResult, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.TreatmentPlan, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.PhysicalExamination, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.AuxiliaryExamination, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.PrescriptionInfo, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.FollowUpPlan, opt => opt.DoNotValidate())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<MedicalCaseDto, LYBT.Entities.MedicalCase.MedicalCase>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.CaseStatus))
                .ForMember(dest => dest.Consultation, opt => opt.Ignore()) // 修复：使用单个Consultation对象
                .ForMember(dest => dest.Prescription, opt => opt.Ignore());
        }
    }
}
