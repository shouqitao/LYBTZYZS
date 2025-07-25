using AutoMapper;
using LYBT.Module.Queueing.Models;
using LYBT.Module.Queueing.Models.Dtos;

namespace LYBT.Module.Queueing.Mapping {

    /// <summary>
    /// 排队实体与DTO的AutoMapper映射配置
    /// </summary>
    public class QueueingMappingProfile : Profile {

        public QueueingMappingProfile() {
            CreateMap<QueueingModel, QueueingDto>().ReverseMap();
            CreateMap<QueueingModel, QueueingDetailDto>().ReverseMap();
            CreateMap<QueueingCreateDto, QueueingModel>();
            CreateMap<QueueingEditDto, QueueingModel>();
        }
    }
}