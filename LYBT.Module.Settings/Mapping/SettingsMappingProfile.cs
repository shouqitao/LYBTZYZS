using AutoMapper;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Dtos;

namespace LYBT.Module.Settings.Mapping {

    /// <summary>
    /// 设置项实体与DTO的AutoMapper映射配置
    /// </summary>
    public class SettingsMappingProfile : Profile {

        public SettingsMappingProfile() {
            // 实体 <=> 列表DTO
            CreateMap<SettingsModel, SettingsDto>().ReverseMap();
            // 实体 <=> 详情DTO
            CreateMap<SettingsModel, SettingsDetailDto>().ReverseMap();
            // 新增DTO => 实体
            CreateMap<SettingsCreateDto, SettingsModel>();

            CreateMap<SettingsCreateDto, SettingsModel>();
            CreateMap<SettingsEditDto, SettingsModel>();

            CreateMap<DiagnosisCatalogModel, DiagnosisCatalogDto>().ReverseMap();
            CreateMap<DiagnosisCatalogCreateDto, DiagnosisCatalogModel>();
            CreateMap<DiagnosisCatalogEditDto, DiagnosisCatalogModel>();

            CreateMap<TreatmentCatalogModel, TreatmentCatalogDto>().ReverseMap();
            CreateMap<TreatmentCatalogCreateDto, TreatmentCatalogModel>();
            CreateMap<TreatmentCatalogEditDto, TreatmentCatalogModel>();

            CreateMap<GlobalSettingsModel, GlobalSettingsDto>().ReverseMap();
        }
    }
}