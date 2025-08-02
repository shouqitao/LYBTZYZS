using AutoMapper;
using LYBT.Models.Records;
using LYBT.Shared.Models.Contracts.Records;

namespace LYBT.Module.Records.Mapping {

    /// <summary>
    /// 病历实体与DTO的AutoMapper映射配置
    /// </summary>
    public class RecordMappingProfile : Profile {

        public RecordMappingProfile() {
            // 实体 <=> 列表DTO
            CreateMap<RecordModel, RecordDto>().ReverseMap();
            // 实体 <=> 详情DTO
            CreateMap<RecordModel, RecordDetailDto>().ReverseMap();
            // 新增DTO => 实体
            CreateMap<RecordCreateDto, RecordModel>();
        }
    }
}