using AutoMapper;
using LYBT.Module.MedicalCase.Dtos;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Mapping
{

    /// <summary>
    /// 医疗案例映射配置
    /// Epic #1612 Task 1.4: 配置AutoMapper映射关系
    /// </summary>
    public class MedicalCaseMappingProfile : Profile
    {

        public MedicalCaseMappingProfile()
        {
            // ========== 旧的DTO映射（保持兼容性） ==========

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

            // ========== Epic #1612 新的DTO映射 ==========

            // Request映射: UpdateConsultationRequest -> Consultation
            CreateMap<UpdateConsultationRequest, LYBT.Entities.Consultation.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.Step1CompletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Step2CompletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Step3CompletedAt, opt => opt.Ignore())
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // Request映射: CreatePrescriptionRequest -> Prescription
            CreateMap<CreatePrescriptionRequest, LYBT.Entities.Prescriptions.Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // Request映射: UpdatePrescriptionRequest -> Prescription
            CreateMap<UpdatePrescriptionRequest, LYBT.Entities.Prescriptions.Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Request映射: PrescriptionItemDto -> PrescriptionItem (嵌套对象)
            CreateMap<PrescriptionItemDto, LYBT.Entities.Prescriptions.PrescriptionItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore())
                .ForMember(dest => dest.HerbName, opt => opt.Ignore())
                .ForMember(dest => dest.Unit, opt => opt.Ignore())
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore())
                .ForMember(dest => dest.Usage, opt => opt.Ignore())
                .ForMember(dest => dest.Remark, opt => opt.Ignore());

            // Response映射: MedicalCase -> MedicalCaseDetailResponse
            CreateMap<LYBT.Entities.MedicalCase.MedicalCase, MedicalCaseDetailResponse>()
                .ForMember(dest => dest.Consultation, opt => opt.MapFrom(src => src.Consultation))
                .ForMember(dest => dest.Prescription, opt => opt.MapFrom(src => src.Prescription));

            // Response映射: Consultation -> ConsultationDetailDto
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDetailDto>();

            // Response映射: Prescription -> MedicalCasePrescriptionDto
            CreateMap<LYBT.Entities.Prescriptions.Prescription, MedicalCasePrescriptionDto>()
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src =>
                    src.Items.Sum(i => i.Amount) * src.DosageCount * src.Discount));

            // Response映射: PrescriptionItem -> PrescriptionItemDto (嵌套对象)
            CreateMap<LYBT.Entities.Prescriptions.PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => (decimal)src.Quantity));
        }
    }
}
