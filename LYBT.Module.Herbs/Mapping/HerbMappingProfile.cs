using AutoMapper;
using LYBT.Common.Extensions;
using LYBT.Module.Herbs.Models;
using LYBT.Module.Herbs.Models.Dtos;

namespace LYBT.Module.Herbs.Mapping {

    /// <summary>
    /// 药材实体与DTO的AutoMapper映射配置
    /// </summary>
    public class HerbMappingProfile : Profile {

        public HerbMappingProfile() {
            // 基础映射
            CreateMap<HerbModel, HerbDto>()
                .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => src.Status.GetDescription()))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => (int)(src.Price * src.Stock)));

            CreateMap<HerbModel, HerbDetailDto>()
                .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => src.Status.GetDescription()));

            // 创建映射
            CreateMap<HerbCreateDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());

            // 编辑映射
            CreateMap<HerbEditDto, HerbModel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());

            // 导入映射
            CreateMap<HerbImportDto, HerbModel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PinyinCode, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());

            // 反向映射
            CreateMap<HerbDto, HerbModel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());

            CreateMap<HerbDetailDto, HerbModel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorId, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperatorName, opt => opt.Ignore());
        }
    }
}