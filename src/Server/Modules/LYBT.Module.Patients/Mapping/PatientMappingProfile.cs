using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Mapping
{

    /// <summary>
    /// 患者实体与DTO之间的AutoMapper映射配置
    /// 更新以支持共享契约模型和基础模型继承
    /// </summary>
    public class PatientMappingProfile : Profile
    {

        public PatientMappingProfile()
        {
            // ==================== 共享契约映射 ====================

            // 患者实体转共享PatientDetailDto（API响应）
            CreateMap<PatientModel, PatientDetailDto>()
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdateTime))
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.IDNumber, opt => opt.MapFrom(src => src.IdNumber));

            // 共享PatientCreateDto转患者实体
            CreateMap<PatientCreateDto, PatientModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            // 共享PatientUpdateDto转患者实体
            CreateMap<PatientUpdateDto, PatientModel>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            // 共享PatientDetailDto转患者实体（用于新增/更新）
            CreateMap<PatientDetailDto, PatientModel>()
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IDNumber));

            // 患者实体转共享PatientDto（列表显示）
            CreateMap<PatientModel, PatientDto>();
        }
    }
}