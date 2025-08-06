using AutoMapper;
using LYBT.Models.TreatmentPlan;
using LYBT.Models.Prescriptions;
using LYBT.Shared.Models.Contracts.TreatmentPlan;

namespace LYBT.Module.TreatmentPlan.Mapping
{
    /// <summary>
    /// 治疗方案映射配置
    /// </summary>
    public class TreatmentPlanMappingProfile : Profile
    {
        public TreatmentPlanMappingProfile()
        {
            // Model -> DTO
            CreateMap<TreatmentPlanModel, TreatmentPlanDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Patient != null ? 
                    src.MedicalCase.Registration.Patient.Name : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Doctor != null ? 
                    src.MedicalCase.Registration.Doctor.Name : string.Empty))
                .ForMember(dest => dest.HasPrescription, opt => opt.MapFrom(src => src.Prescription != null))
                .ForMember(dest => dest.HasPhysiotherapy, opt => opt.MapFrom(src => src.PhysiotherapyItems != null && src.PhysiotherapyItems.Count > 0));

            CreateMap<TreatmentPlanModel, TreatmentPlanDetailDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Patient != null ? 
                    src.MedicalCase.Registration.Patient.Name : string.Empty))
                .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Doctor != null ? 
                    src.MedicalCase.Registration.Doctor.Name : string.Empty));

            // Prescription mappings
            CreateMap<PrescriptionModel, PrescriptionDto>();
            CreateMap<PrescriptionDto, PrescriptionModel>();
            CreateMap<PrescriptionItemModel, PrescriptionItemDto>();
            CreateMap<PrescriptionItemDto, PrescriptionItemModel>();

            // Physiotherapy mappings
            CreateMap<PhysiotherapyItemModel, PhysiotherapyItemDto>();
            CreateMap<PhysiotherapyItemDto, PhysiotherapyItemModel>();

            // DTO -> Model
            CreateMap<TreatmentPlanCreateDto, TreatmentPlanModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore());

            CreateMap<TreatmentPlanUpdateDto, TreatmentPlanModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.Prescription, opt => opt.Ignore())
                .ForMember(dest => dest.PhysiotherapyItems, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}