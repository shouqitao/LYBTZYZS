using AutoMapper;
using LYBT.Models;
using LYBT.Module.Sync.Dtos;

namespace LYBT.Module.Sync.Mapping {

    /// <summary>
    /// 同步任务与日志实体与DTO的AutoMapper配置
    /// </summary>
    public class SyncMappingProfile : Profile {

        public SyncMappingProfile() {
            // 同步日志
            CreateMap<SyncLogModel, SyncLogDto>().ReverseMap();
            CreateMap<SyncLogCreateDto, SyncLogModel>();
            // 同步任务
            CreateMap<SyncTaskModel, SyncTaskDto>().ReverseMap();
            CreateMap<SyncTaskModel, SyncTaskDetailDto>().ReverseMap();
            CreateMap<SyncTaskCreateDto, SyncTaskModel>();
        }
    }
}