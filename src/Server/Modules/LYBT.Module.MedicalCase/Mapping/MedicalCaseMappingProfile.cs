using AutoMapper;
using LYBT.Module.MedicalCase.Dtos;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

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
            // ========== Response映射（保持兼容性） ==========

            // MedicalCase -> MedicalCaseDto
            CreateMap<LYBT.Entities.MedicalCase.MedicalCase, MedicalCaseDto>()
                .ForMember(dest => dest.CaseStatus, opt => opt.MapFrom(src => src.CaseStatus))
                .ForMember(dest => dest.CaseNumber, opt => opt.Ignore())
                .ForMember(dest => dest.ChiefComplaint, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.Diagnosis, opt => opt.Ignore());

            // MedicalCase -> MedicalCaseDetailDto
            CreateMap<LYBT.Entities.MedicalCase.MedicalCase, MedicalCaseDetailDto>()
                .ForMember(dest => dest.CaseStatus, opt => opt.MapFrom(src => src.CaseStatus))
                .ForMember(dest => dest.CaseNumber, opt => opt.Ignore())
                .ForMember(dest => dest.ChiefComplaint, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.Diagnosis, opt => opt.Ignore())
                .ForMember(dest => dest.PresentIllness, opt => opt.Ignore())
                .ForMember(dest => dest.DiagnosisResult, opt => opt.Ignore())
                .ForMember(dest => dest.TreatmentPlan, opt => opt.Ignore())
                .ForMember(dest => dest.Prescription, opt => opt.Ignore());

            // ========== Epic #1961: FluentValidation统一设计 ==========

            // MedicalCaseInputDto -> MedicalCase (统一创建/更新)
            // 注意：MedicalCaseInputDto 是扁平化 DTO，部分字段应映射到 Consultation 实体
            // 此配置仅映射 MedicalCase 实体字段，Consultation 字段由 Service 层处理
            CreateMap<MedicalCaseInputDto, LYBT.Entities.MedicalCase.MedicalCase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Service层生成
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ConsultationDate, opt => opt.MapFrom(src => src.VisitDate))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                // 以下字段由 Service 层管理
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
                .ForMember(dest => dest.CaseStatus, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.NeedsPrescription, opt => opt.Ignore())
                // 导航属性
                .ForMember(dest => dest.Consultation, opt => opt.Ignore())
                .ForMember(dest => dest.Prescription, opt => opt.Ignore())
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // ========== Epic #1612 旧的Request映射（保持兼容性） ==========

            // Request映射: ConsultationInputDto -> Consultation (Shared层)
            // Issue #2231: Consultation使用共享主键，必须忽略Id相关字段以避免EF Core键修改错误
            CreateMap<ConsultationInputDto, LYBT.Entities.Consultation.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())  // 共享主键，不可修改
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())  // 导航属性，不可修改
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

            // Request映射: PrescriptionCreateDto -> Prescription (Shared层)
            CreateMap<PrescriptionCreateDto, LYBT.Entities.Prescriptions.Prescription>()
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
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore())
                .ForMember(dest => dest.ReferencedFormulas, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // Request映射: PrescriptionEditDto -> Prescription (Shared层)
            CreateMap<PrescriptionEditDto, LYBT.Entities.Prescriptions.Prescription>()
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
                .ForMember(dest => dest.PrescriptionNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.FormulaSource, opt => opt.Ignore())
                .ForMember(dest => dest.ReferencedFormulas, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
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

            // Response映射: Consultation -> ConsultationDto (Shared层)
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDto>()
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore());

            // Response映射: Prescription -> MedicalCasePrescriptionDto
            CreateMap<LYBT.Entities.Prescriptions.Prescription, MedicalCasePrescriptionDto>()
                .ForMember(dest => dest.Usage, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src =>
                    src.Items.Sum(i => i.Amount) * src.DosageCount * src.Discount));

            // Response映射: PrescriptionItem -> PrescriptionItemDto (嵌套对象)
            CreateMap<LYBT.Entities.Prescriptions.PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.Dosage, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore())
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => (decimal)src.Quantity));
        }
    }
}
