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
    // ==================== UltraThink v2.0简化映射 ====================

    // 患者实体转PatientDto（API响应）
    CreateMap<Patient, PatientDto>()
        .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
        .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IdNumber));

    // PatientCreateDto转患者实体
    CreateMap<PatientCreateDto, Patient>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
        .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
        .ForMember(dest => dest.DisableReason, opt => opt.Ignore());

    // PatientUpdateDto转患者实体
    CreateMap<PatientUpdateDto, Patient>()
        .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
        .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
        .ForMember(dest => dest.DisableReason, opt => opt.Ignore());

    // PatientDto转患者实体（用于新增/更新）
    CreateMap<PatientDto, Patient>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
        .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
        .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
        .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
        .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IdNumber));

    // 患者实体转PatientDto（列表显示）- Age字段在DTO中为计算属性
    CreateMap<Patient, PatientDto>();

}
    }
}