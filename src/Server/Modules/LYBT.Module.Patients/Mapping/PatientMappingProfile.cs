using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Mapping
{

    /// <summary>
    /// 患者实体与DTO之间的AutoMapper映射配置
    /// OpenSpec: refactor-dto-simplification - 添加简化DTO映射
    /// </summary>
    public class PatientMappingProfile : Profile
    {

        public PatientMappingProfile()
        {
            // ============================================
            // 新简化DTO映射 (OpenSpec: refactor-dto-simplification)
            // ============================================

            // Patient -> PatientListDto (新)
            CreateMap<Patient, PatientListDto>();

            // Patient -> PatientDetailDto (扁平化详情DTO)
            CreateMap<Patient, PatientDetailDto>();

            // ============================================
            // 旧DTO映射 (保持向后兼容，后续移除)
            // ============================================

            // 患者实体 → PatientDetailDto（API响应）
            CreateMap<Patient, PatientDetailDto>()
                // Issue #2240: Patient.Age是从BirthDate计算的只读属性，AutoMapper会自动复制其计算值到PatientDto.Age
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IdNumber))
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory)); // Epic #1934新增

            // PatientInputDto → Patient（创建和批量导入）
            CreateMap<PatientInputDto, Patient>()
                .ForMember(dest => dest.Age, opt => opt.Ignore()) // Age是只读计算属性
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()) // 拼音码由系统生成
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                // 忽略 BaseEntity 审计字段
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                // Epic #1934: MedicalHistory字段映射
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory));

            // PatientDetailDto → Patient（用于更新）
            CreateMap<PatientDetailDto, Patient>()
                .ForMember(dest => dest.Age, opt => opt.Ignore()) // Age是只读计算属性
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                .ForMember(dest => dest.PinYinCode, opt => opt.MapFrom(src => src.PinYinCode))
                .ForMember(dest => dest.IdNumber, opt => opt.MapFrom(src => src.IdNumber))
                // 忽略 BaseEntity 审计字段
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // PatientInputDto → PatientDetailDto（用于验证服务）
            CreateMap<PatientInputDto, PatientDetailDto>()
                .ForMember(dest => dest.Age, opt => opt.Ignore()) // Issue #2240: Age是只读计算属性，从BirthDate计算
                .ForMember(dest => dest.PinYinCode, opt => opt.Ignore()) // 拼音码由系统生成
                .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                // 忽略时间戳字段（从 TimestampDto 继承）
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                // Epic #1934: MedicalHistory字段映射
                .ForMember(dest => dest.MedicalHistory, opt => opt.MapFrom(src => src.MedicalHistory));
        }
    }
}
