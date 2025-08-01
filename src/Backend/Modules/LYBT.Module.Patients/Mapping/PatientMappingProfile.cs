using AutoMapper;
using LYBT.Models.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using SharedPatientDetailDto = LYBT.Shared.Models.Contracts.Patients.PatientDetailDto;

namespace LYBT.Module.Patients.Mapping {

    /// <summary>
    /// 患者实体与DTO之间的AutoMapper映射配置
    /// 更新以支持共享契约模型和基础模型继承
    /// </summary>
    public class PatientMappingProfile : Profile {

        public PatientMappingProfile() {
            // ==================== 共享契约映射 ====================
            
            // 患者实体转共享PatientDetailDto（API响应）
            CreateMap<PatientModel, SharedPatientDetailDto>()
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreateTime))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdateTime))
                .ForMember(dest => dest.IDType, opt => opt.MapFrom(src => src.IdType ?? "身份证"))
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

        }
    }
}