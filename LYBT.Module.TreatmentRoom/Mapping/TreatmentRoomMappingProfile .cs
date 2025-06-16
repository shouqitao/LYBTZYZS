using AutoMapper;
using LYBT.Models;
using LYBT.Models.TreatmentRoom;
using LYBT.Module.TreatmentRoom.Dtos;

namespace LYBT.Module.TreatmentRoom.Mapping {
    /// <summary>
    /// 治疗室实体与DTO的AutoMapper映射配置
    /// </summary>
    public class TreatmentRoomMappingProfile : Profile {
        public TreatmentRoomMappingProfile() {
            // 实体 <=> 列表DTO
            CreateMap<TreatmentRoomModel, TreatmentRoomDto>().ReverseMap();
            // 实体 <=> 详情DTO
            CreateMap<TreatmentRoomModel, TreatmentRoomDetailDto>().ReverseMap();
            // 新增DTO => 实体
            CreateMap<TreatmentRoomCreateDto, TreatmentRoomModel>();
        }
    }
}
