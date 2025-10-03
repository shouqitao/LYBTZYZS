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
                .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IdNumber))
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdatedAt));

            // PatientCreateDto转患者实体
            CreateMap<PatientCreateDto, Patient>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore())  // 拼音码由系统生成
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                // 忽略BaseEntity的审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore());

            // PatientUpdateDto转患者实体
            CreateMap<PatientUpdateDto, Patient>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore())  // 拼音码由系统生成
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                // 忽略BaseEntity的审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // PatientDto转患者实体（用于新增/更新）
            CreateMap<PatientDto, Patient>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IdNumber))
                // 忽略BaseEntity的审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdateTime))
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // UltraThink修复：添加缺失的DTO间映射配置
            // PatientCreateDto -> PatientDto（用于验证服务）
            CreateMap<PatientCreateDto, PatientDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore())  // 拼音码由系统生成
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())  // 审计字段
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore());  // 审计字段

            // PatientUpdateDto -> PatientDto（用于验证服务）
            CreateMap<PatientUpdateDto, PatientDto>()
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore())  // 拼音码由系统生成
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.CreateTime, opt => opt.Ignore())  // 审计字段
                .ForMember(dest => dest.UpdateTime, opt => opt.Ignore());  // 审计字段
        }
    }
}
