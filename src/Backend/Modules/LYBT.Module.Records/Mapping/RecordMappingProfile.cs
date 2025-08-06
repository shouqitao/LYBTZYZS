using AutoMapper;
using LYBT.Models.Records;
using LYBT.Shared.Models.Contracts.Records;

namespace LYBT.Module.Records.Mapping {

    /// <summary>
    /// 病历实体与DTO的AutoMapper映射配置
    /// </summary>
    public class RecordMappingProfile : Profile {

        public RecordMappingProfile() {
            // 实体 => 列表DTO (忽略PatientName，需要从Patient表获取)
            CreateMap<RecordModel, RecordDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore());
            
            // 列表DTO => 实体
            CreateMap<RecordDto, RecordModel>()
                .ForMember(dest => dest.DiagnosisResults, opt => opt.Ignore())
                .ForMember(dest => dest.HerbalFormula, opt => opt.Ignore())
                .ForMember(dest => dest.TreatmentPlans, opt => opt.Ignore())
                .ForMember(dest => dest.FormulaTemplateId, opt => opt.Ignore())
                .ForMember(dest => dest.TreatmentRoomIds, opt => opt.Ignore())
                .ForMember(dest => dest.SharedToDoctorIds, opt => opt.Ignore());
            
            // 实体 => 详情DTO
            CreateMap<RecordModel, RecordDetailDto>()
                .ForMember(dest => dest.PatientName, opt => opt.Ignore())
                .ForMember(dest => dest.RegistrationId, opt => opt.Ignore());
            
            // 详情DTO => 实体
            CreateMap<RecordDetailDto, RecordModel>();
            // 新增DTO => 实体
            CreateMap<RecordCreateDto, RecordModel>();
        }
    }
}