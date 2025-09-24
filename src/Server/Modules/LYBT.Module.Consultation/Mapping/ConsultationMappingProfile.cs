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
            // UltraThink修复：启用AutoMapper配置，解决字段更新不完整问题

            // ConsultationDetailDto -> Consultation - 核心更新映射
            CreateMap<ConsultationDetailDto, LYBT.Entities.Consultation.Consultation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // 忽略ID
                .ForMember(dest => dest.TCMDiagnosis, opt => opt.MapFrom(src => src.TCMDiagnosis)) // TCMDiagnosis -> TCMDiagnosis
                .ForMember(dest => dest.Patient, opt => opt.Ignore()) // 导航属性忽略
                .ForMember(dest => dest.User, opt => opt.Ignore()) // 导航属性忽略
                .ForMember(dest => dest.MedicalCase, opt => opt.Ignore()) // 导航属性忽略

                // 忽略DTO中的显示字段
                .ForSourceMember(src => src.PatientName, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.DoctorName, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.StartTime, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.EndTime, opt => opt.DoNotValidate())
                .ForSourceMember(src => src.ConsultationStatus, opt => opt.DoNotValidate()) // 状态类型不同
                .ForSourceMember(src => src.IsCompleted, opt => opt.DoNotValidate()) // 计算属性
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Consultation -> ConsultationDto - 基础列表映射
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDto>()
                // 移除Diagnosis字段映射，使用TCMDiagnosis
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore()); // 需要从关联数据获取

            // Consultation -> ConsultationDetailDto - 详细信息映射
            CreateMap<LYBT.Entities.Consultation.Consultation, ConsultationDetailDto>()
                // 移除Diagnosis字段映射，使用TCMDiagnosis
                .ForMember(dest => dest.PatientName, opt => opt.Ignore()) // 需要从关联数据获取
                .ForMember(dest => dest.DoctorName, opt => opt.Ignore()) // 需要从关联数据获取
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => DateTime.Now)) // 简单时间映射
                .ForMember(dest => dest.EndTime, opt => opt.Ignore()) // 暂不处理结束时间
                .ForMember(dest => dest.ConsultationStatus, opt => opt.MapFrom(src => src.Status == LYBT.Shared.Models.Enums.CommonStatus.Disabled ? ConsultationStatus.Completed : ConsultationStatus.InProgress)); // 状态映射
        }
    }
}
