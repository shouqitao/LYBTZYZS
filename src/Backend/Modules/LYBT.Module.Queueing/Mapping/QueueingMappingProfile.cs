using AutoMapper;
using LYBT.Models.Queueing;
using LYBT.Shared.Models.Contracts.Queueing;

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