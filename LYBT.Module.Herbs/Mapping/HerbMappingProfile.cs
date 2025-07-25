using AutoMapper;
using LYBT.Models.Herbs;
using LYBT.Module.Herbs.Dtos;

namespace LYBT.Module.Herbs.Mapping {

    /// <summary>
    /// 药材实体与DTO的AutoMapper映射配置
    /// </summary>
    public class HerbMappingProfile : Profile {

        public HerbMappingProfile() {
            CreateMap<HerbModel, HerbDto>().ReverseMap();
            CreateMap<HerbModel, HerbDetailDto>().ReverseMap();
            CreateMap<HerbCreateDto, HerbModel>();
            CreateMap<HerbImportDto, HerbModel>();
        }
    }
}