using AutoMapper;
using LYBT.Models.Cashier;
using LYBT.Shared.Models.Contracts.Cashier;

namespace LYBT.Module.Cashier.Mapping
{
    /// <summary>
    /// 收银映射配置（替代BillingMappingProfile）
    /// </summary>
    public class CashierMappingProfile : Profile
    {
        public CashierMappingProfile()
        {
            // Model -> DTO
            CreateMap<CashierModel, CashierDto>()
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => 
                    src.MedicalCase != null && 
                    src.MedicalCase.Registration != null && 
                    src.MedicalCase.Registration.Patient != null ? 
                    src.MedicalCase.Registration.Patient.Name : string.Empty))
                .ForMember(dest => dest.PaymentMethodName, opt => opt.MapFrom(src => 
                    src.PaymentMethod.HasValue ? src.PaymentMethod.ToString() : string.Empty))
                .ForMember(dest => dest.PaymentStatusName, opt => opt.MapFrom(src => 
                    src.PaymentStatus.ToString()));

            CreateMap<CashierModel, CashierDetailDto>()
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

            // DTO -> Model
            CreateMap<CashierCreateDto, CashierModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentStatus, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentTime, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
                .ForMember(dest => dest.ActualAmount, opt => opt.MapFrom(src => src.TotalAmount - (src.DiscountAmount ?? 0)))
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForMember(dest => dest.RefundTime, opt => opt.Ignore())
                .ForMember(dest => dest.RefundAmount, opt => opt.Ignore())
                .ForMember(dest => dest.RefundReason, opt => opt.Ignore());

            CreateMap<CashierUpdateDto, CashierModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentStatus, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentTime, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Payment and Refund DTOs
            CreateMap<PaymentDto, CashierModel>()
                .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
                .ForAllMembers(opts => opts.Ignore());

            CreateMap<RefundDto, CashierModel>()
                .ForMember(dest => dest.RefundAmount, opt => opt.MapFrom(src => src.RefundAmount))
                .ForMember(dest => dest.RefundReason, opt => opt.MapFrom(src => src.RefundReason))
                .ForAllMembers(opts => opts.Ignore());
        }
    }
}