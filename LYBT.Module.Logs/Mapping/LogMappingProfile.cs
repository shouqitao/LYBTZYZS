using AutoMapper;
using LYBT.Module.Logs.Models;
using LYBT.Module.Logs.Models.Dtos;

namespace LYBT.Module.Logs.Mapping {

    /// <summary>
    /// 日志实体与DTO的映射配置（AutoMapper）
    /// </summary>
    public class LogMappingProfile : Profile {

        public LogMappingProfile() {
            CreateMap<LogModel, LogDto>().ReverseMap();
            CreateMap<LogCreateDto, LogModel>();
        }
    }
}