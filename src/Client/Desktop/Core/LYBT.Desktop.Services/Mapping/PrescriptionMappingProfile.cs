using AutoMapper;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Services.Mapping
{
    /// <summary>
    /// 处方模块 AutoMapper 配置
    /// </summary>
    public class PrescriptionMappingProfile : Profile
    {
        public PrescriptionMappingProfile()
        {
            // PrescriptionCreateDto → PrescriptionDto
            CreateMap<PrescriptionCreateDto, PrescriptionDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.PatientId))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.DosageCount, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                // Items 集合需要在 Service 层单独处理
                .ForMember(dest => dest.Items, opt => opt.Ignore());

            // PrescriptionEditDto → PrescriptionDto (用于更新现有实体)
            CreateMap<PrescriptionEditDto, PrescriptionDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.MedicalCaseId, opt => opt.Ignore())
                .ForMember(dest => dest.Indication, opt => opt.Ignore())
                // Items 集合需要在 Service 层单独处理
                .ForMember(dest => dest.Items, opt => opt.Ignore());

            // PrescriptionDto → PrescriptionDto (用于克隆)
            CreateMap<PrescriptionDto, PrescriptionDto>();

            // PrescriptionItemCreateDto → PrescriptionItemDto
            CreateMap<PrescriptionItemCreateDto, PrescriptionItemDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.UnitPrice * src.Quantity))
                .ForMember(dest => dest.TotalWeight, opt => opt.Ignore())
                .ForMember(dest => dest.Dosage, opt => opt.MapFrom(src => src.Quantity));
        }
    }
}
