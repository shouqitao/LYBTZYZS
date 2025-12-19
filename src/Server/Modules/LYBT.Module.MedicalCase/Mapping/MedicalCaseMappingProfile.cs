using AutoMapper;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.MedicalCases.Mapping
{

    /// <summary>
    /// 医疗案例映射配置
    /// Epic #1612 Task 1.4: 配置AutoMapper映射关系
    /// </summary>
    public class MedicalCaseMappingProfile : Profile
    {

        public MedicalCaseMappingProfile()
        {
            // ========== Response映射 ==========

            // OpenSpec: refactor-dto-simplification - MedicalCaseDto已删除，统一使用MedicalCaseDetailDto

            // MedicalCase -> MedicalCaseDetailDto (聚合DTO，包含嵌套的Consultation和Prescription)
            // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint, DiagnosisResult, TreatmentPlan
            // OpenSpec: refactor-dto-simplification - 独立定义，不再继承MedicalCaseDto
            CreateMap<LYBT.Entities.MedicalCases.MedicalCase, MedicalCaseDetailDto>()
                .ForMember(dest => dest.CaseStatus, opt => opt.MapFrom(src => src.CaseStatus))
                .ForMember(dest => dest.ConsultationId, opt => opt.MapFrom(src =>
                    src.Consultation != null && !src.Consultation.IsDeleted ? src.Consultation.Id : (Guid?)null))
                .ForMember(dest => dest.PrescriptionId, opt => opt.MapFrom(src =>
                    src.Prescription != null && !src.Prescription.IsDeleted ? src.Prescription.Id : (Guid?)null))
                .ForMember(dest => dest.CaseNumber, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.Diagnosis, opt => opt.Ignore())
                .ForMember(dest => dest.PresentIllness, opt => opt.Ignore())
                // 嵌套DTO属性在Service层单独填充
                .ForMember(dest => dest.Consultation, opt => opt.Ignore())
                .ForMember(dest => dest.Prescription, opt => opt.Ignore());

            // ========== OpenSpec: refactor-dto-simplification 新增简化DTO映射 ==========

            // MedicalCase -> MedicalCaseListDto (新-扁平化列表DTO)
            CreateMap<LYBT.Entities.MedicalCases.MedicalCase, MedicalCaseListDto>()
                .ForMember(dest => dest.CaseStatus, opt => opt.MapFrom(src => src.CaseStatus))
                .ForMember(dest => dest.HasConsultation, opt => opt.MapFrom(src =>
                    src.Consultation != null && !src.Consultation.IsDeleted))
                .ForMember(dest => dest.HasPrescription, opt => opt.MapFrom(src =>
                    src.Prescription != null && !src.Prescription.IsDeleted))
                // 以下字段需要Service层填充
                .ForMember(dest => dest.CaseNumber, opt => opt.Ignore())
                .ForMember(dest => dest.PatientGender, opt => opt.Ignore())
                .ForMember(dest => dest.PatientAge, opt => opt.Ignore())
                .ForMember(dest => dest.Diagnosis, opt => opt.Ignore());

            // ========== Epic #1961: FluentValidation统一设计 ==========

            // MedicalCaseInputDto -> MedicalCase (统一创建/更新)
            // 注意：MedicalCaseInputDto 是扁平化 DTO，部分字段应映射到 Consultation 实体
            // 此配置仅映射 MedicalCase 实体字段，Consultation 字段由 Service 层处理
            CreateMap<MedicalCaseInputDto, LYBT.Entities.MedicalCases.MedicalCase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Service层生成
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.DoctorId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.ConsultationDate, opt => opt.MapFrom(src => src.VisitDate))
                .ForMember(dest => dest.Remark, opt => opt.MapFrom(src => src.Remark))
                // 以下字段由 Service 层管理
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())
                .ForMember(dest => dest.CaseStatus, opt => opt.Ignore())
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
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            // OpenSpec: consultation-field-alignment - PrescriptionEnabled已移除
            CreateMap<ConsultationInputDto, LYBT.Entities.Consultations.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())  // 共享主键，不可修改
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())  // 导航属性，不可修改
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // Request映射: PrescriptionInputDto -> Prescription (Shared层)
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            CreateMap<PrescriptionInputDto, LYBT.Entities.Prescriptions.Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.PrintVersion, opt => opt.Ignore())
                .ForMember(dest => dest.LastPrintedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PrintCount, opt => opt.Ignore())
                .ForMember(dest => dest.IsPrinted, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.PrintLogs, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore())
                .ForMember(dest => dest.ReferencedFormulas, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                // BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // Request映射: PrescriptionInputDto -> Prescription (Shared层)
            // 注意：Items需要在Service层手动处理（删除旧项，添加新项），不能通过AutoMapper直接映射
            // OpenSpec: optimize-entity-data-flow - PatientId/UserId已移除
            CreateMap<PrescriptionInputDto, LYBT.Entities.Prescriptions.Prescription>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
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
                .ForMember(dest => dest.Items, opt => opt.Ignore()) // Items需在Service层手动处理
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

            // Request映射: PrescriptionItemInputDto -> PrescriptionItem (用于创建/更新处方)
            CreateMap<PrescriptionItemInputDto, LYBT.Entities.Prescriptions.PrescriptionItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PrescriptionId, opt => opt.Ignore());

            // Response映射: Consultation -> ConsultationDetailDto (Shared层)
            CreateMap<LYBT.Entities.Consultations.Consultation, ConsultationDetailDto>()
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore());

            // Response映射: PrescriptionItem -> PrescriptionItemDto (嵌套对象)
            CreateMap<LYBT.Entities.Prescriptions.PrescriptionItem, PrescriptionItemDto>()
                .ForMember(dest => dest.Dosage, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Subtotal, opt => opt.Ignore())
                .ForMember(dest => dest.Notes, opt => opt.Ignore())
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => (decimal)src.Dosage));
        }
    }
}
