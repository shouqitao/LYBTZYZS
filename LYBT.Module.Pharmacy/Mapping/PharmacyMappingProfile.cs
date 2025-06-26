using AutoMapper;
using LYBT.Models.Pharmacy;
using LYBT.Module.Pharmacy.Dtos;

namespace LYBT.Module.Pharmacy.Mapping {

    /// <summary>
    /// 药房实体与DTO的AutoMapper映射配置
    /// </summary>
    public class PharmacyMappingProfile : Profile {

        public PharmacyMappingProfile() {
            // 实体 <=> 列表DTO
            CreateMap<PharmacyModel, PharmacyDto>().ReverseMap();
            // 实体 <=> 详情DTO
            CreateMap<PharmacyModel, PharmacyDetailDto>().ReverseMap();
            // 新增DTO => 实体
            CreateMap<PharmacyCreateDto, PharmacyModel>();
        }
    }
}