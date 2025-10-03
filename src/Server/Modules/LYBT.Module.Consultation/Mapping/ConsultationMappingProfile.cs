using AutoMapper;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Consultation.Mapping
{
    /// <summary>
    /// 诊疗模块 AutoMapper 映射配置
    /// </summary>
    public class ConsultationMappingProfile : Profile
    {
        public ConsultationMappingProfile()
        {
            // ConsultationDetailDto -> Consultation - 核心更新映射
            CreateMap<ConsultationDetailDto, LYBT.Entities.Consultation.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Empty)) // 测试映射时确保ID为空值
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))

                // 映射审计字段（从DTO的CreateTime/UpdateTime到实体的CreatedAt/UpdatedAt）
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdateTime))

                // 忽略BaseEntity的其他审计字段（这些不应该从DTO设置）
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())

                // 状态映射：ConsultationStatus -> CommonStatus
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    src.ConsultationStatus == ConsultationStatus.Completed
                        ? CommonStatus.Disabled
                        : CommonStatus.Enabled))

                // 忽略导航属性
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())

                // 忽略DTO中的显示字段（这些不存在于实体中）
                .ForSourceMember(src => src.PatientName, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.DoctorName, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.StartTime, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.EndTime, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.Duration, opt => opt.DoNotValidate()) // 计算属性
                .ForSourceMember(src => src.ConsultationStatus, opt => opt.DoNotValidate()) // 已映射到Status
                .ForSourceMember(src => src.IsCompleted, opt => opt.DoNotValidate()) // 计算属性

                // 条件映射：只映射非null值
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Consultation -> ConsultationDto - 基础列表映射
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDto>()
                // 映射时间字段
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdatedAt))

                // 映射关联ID（通过 MedicalCase 导航属性获取，服务层填充）
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())

                // 映射显示字段（保持null值，服务层会填充实际值）
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())

                // 映射诊疗时间字段（这些在实体中不存在，使用默认值）
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.UpdatedAt))

                // 映射诊疗状态（从CommonStatus转换）
                .ForMember(dest => dest.ConsultationStatus, opt => opt.MapFrom(src =>
                    src.Status == CommonStatus.Disabled
                        ? ConsultationStatus.Completed
                        : ConsultationStatus.InProgress));

            // Consultation -> ConsultationDetailDto - 详细信息映射
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDetailDto>()
                // 映射时间字段
                .ForMember(dest => dest.CreateTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdateTime, opt => opt.MapFrom(src => src.UpdatedAt))

                // 映射关联ID（通过 MedicalCase 导航属性获取，服务层填充）
                .ForMember(dest => dest.PatientId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())

                // 映射显示字段（保持null值，服务层会填充实际值）
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore())

                // 映射诊疗时间字段
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.UpdatedAt))

                // 映射诊疗状态
                .ForMember(dest => dest.ConsultationStatus, opt => opt.MapFrom(src =>
                    src.Status == CommonStatus.Disabled
                        ? ConsultationStatus.Completed
                        : ConsultationStatus.InProgress));

            // ConsultationCreateDto -> Consultation - 创建映射
            CreateMap<ConsultationCreateDto, LYBT.Entities.Consultation.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID由系统生成
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled)) // 新建默认启用
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))

                // 忽略导航属性
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())

                // 忽略DTO中的显示字段
                .ForSourceMember(src => src.PatientName, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.DoctorName, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.StartTime, opt => opt.DoNotValidate());

            // ConsultationUpdateDto -> Consultation - 更新映射
            CreateMap<ConsultationUpdateDto, LYBT.Entities.Consultation.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID不允许更新
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis))

                // 状态映射
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                    src.ConsultationStatus.HasValue && src.ConsultationStatus.Value == ConsultationStatus.Completed
                        ? CommonStatus.Disabled
                        : CommonStatus.Enabled))

                // 更新时间
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now))

                // 忽略审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())

                // 忽略导航属性
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore())

                // 忽略DTO中的其他字段
                .ForSourceMember(src => src.ConsultationStatus, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.EndTime, opt => opt.DoNotValidate())

                // 条件映射：只映射非null值
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}
