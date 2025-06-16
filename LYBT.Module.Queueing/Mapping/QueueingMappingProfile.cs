using AutoMapper;
using LYBT.Models;
using LYBT.Models.Queueing;
using LYBT.Module.Queueing.Dtos;

namespace LYBT.Module.Queueing.Mapping {
    /// <summary>
    /// 排队实体与DTO的AutoMapper映射配置
    /// </summary>
    public class QueueingMappingProfile : Profile {
        public QueueingMappingProfile() {
            CreateMap<QueueingModel, QueueingDto>().ReverseMap();
            CreateMap<QueueingModel, QueueingDetailDto>().ReverseMap();
            CreateMap<QueueingCreateDto, QueueingModel>();
        }
    }
}
