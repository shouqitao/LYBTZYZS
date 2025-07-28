using AutoMapper;
using LYBT.Models.Patients;

namespace LYBT.Module.Patients.Mapping {

    /// <summary>
    /// 病人实体与DTO之间的AutoMapper映射配置
    /// </summary>
    public class PatientMappingProfile : Profile {

        public PatientMappingProfile() {
            // PatientModel → PatientDetailDto
            CreateMap<PatientModel, PatientDetailDto>();

            // PatientDetailDto → PatientModel（用于创建、修改、详情回显）
            CreateMap<PatientDetailDto, PatientModel>();
        }
    }
}