using AutoMapper;
using LYBT.Models.Cashier;
using LYBT.Shared.Models.Contracts.Cashier;

namespace LYBT.Module.Cashier.Mapping
{
    /// <summary>
    /// 收银AutoMapper映射配置
    /// </summary>
    public class CashierMappingProfile : Profile
    {
        public CashierMappingProfile()
        {
            // CashierRecord -> CashierRecordDto
            CreateMap<CashierRecord, CashierRecordDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.CashierName, opt => opt.Ignore());

            // CashierRecord -> CashierRecordDetailDto
            CreateMap<CashierRecord, CashierRecordDetailDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.CashierName, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments));

            // CashierItem -> CashierItemDto
            CreateMap<CashierItem, CashierItemDto>();

            // CashierPayment -> CashierPaymentDto
            CreateMap<CashierPayment, CashierPaymentDto>();

            // CashierRecordCreateDto -> CashierRecord
            CreateMap<CashierRecordCreateDto, CashierRecord>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.PaidAmount, opt => opt.Ignore())
                .ForMember(dest => dest.ChangeAmount, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.InvoiceNumber, opt => opt.Ignore())
                .ForMember(dest => dest.CashierId, opt => opt.Ignore())
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .ForMember(dest => dest.Payments, opt => opt.Ignore());

            // CashierItemCreateDto -> CashierItem
            CreateMap<CashierItemCreateDto, CashierItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CashierRecordId, opt => opt.Ignore())
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.UnitPrice * src.Quantity))
                .ForMember(dest => dest.CashierRecord, opt => opt.Ignore());

            // CashierPaymentCreateDto -> CashierPayment
            CreateMap<CashierPaymentCreateDto, CashierPayment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CashierRecordId, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CashierRecord, opt => opt.Ignore());

            // DailySettlement -> DailySettlementDto
            CreateMap<DailySettlement, DailySettlementDto>()
                .ForMember(dest => dest.CashierName, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentBreakdown, opt => opt.Ignore())
                .ForMember(dest => dest.ItemTypeBreakdown, opt => opt.Ignore());

            // Invoice -> InvoiceDto
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(dest => dest.Items, opt => opt.Ignore());
        }
    }
}