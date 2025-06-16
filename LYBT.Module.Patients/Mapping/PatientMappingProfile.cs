using AutoMapper;
using LYBT.Models.Patient;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Models;

namespace LYBT.Module.Patients.Mapping {
    /// <summary>
    /// 病人实体与DTO之间的AutoMapper映射配置
    /// </summary>
    public class PatientMappingProfile : Profile {
        public PatientMappingProfile() {
            // PatientModel → PatientDetailDto
            CreateMap<PatientModel, PatientDetailDto>();

            // PatientModel → PatientDto（用于列表）
            CreateMap<PatientModel, PatientDto>();

            // PatientCreateDto → PatientModel
            CreateMap<PatientCreateDto, PatientModel>();

            // PatientEditDto → PatientModel
            CreateMap<PatientEditDto, PatientModel>();

            // PatientModel → PatientEditDto（便于回显/编辑）
            CreateMap<PatientModel, PatientEditDto>();

            // PatientDto → PatientModel（如列表选中行直接转为实体）
            CreateMap<PatientDto, PatientModel>();

            // PatientDetailDto → PatientModel（如详情页直接编辑）
            CreateMap<PatientDetailDto, PatientModel>();
        }
    }
}
