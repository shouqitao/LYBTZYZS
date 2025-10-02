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

                // 忽略BaseEntity的审计字段（这些不应该从DTO设置）
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())

                // 忽略DTO中不存在于实体的字段
                .ForSourceMember(src => src.DiagnosisSummary, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.ChiefComplaint, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.PresentIllness, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.DiagnosisResult, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.TreatmentPlan, opt => opt.DoNotValidate())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<MedicalCaseDto, LYBT.Entities.MedicalCase.MedicalCase>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.CaseStatus))
                .ForMember(dest => dest.Consultation, opt => opt.Ignore()) // 修复：使用单个Consultation对象
                .ForMember(dest => dest.Prescription, opt => opt.Ignore());
        }
    }
}
