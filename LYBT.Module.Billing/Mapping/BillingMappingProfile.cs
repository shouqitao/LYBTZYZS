using AutoMapper;
using LYBT.Models.Billing;
using LYBT.Module.Billing.Dtos;

namespace LYBT.Module.Billing.Mapping {

    /// <summary>
    /// 费用结算 AutoMapper 配置
    /// </summary>
    public class BillingMappingProfile : Profile {

        public BillingMappingProfile() {
            // 实体 <=> 列表DTO
            CreateMap<BillingModel, BillingDto>().ReverseMap();
            // 实体 <=> 详情DTO
            CreateMap<BillingModel, BillingDetailDto>().ReverseMap();
            // 新增DTO => 实体
            CreateMap<BillingCreateDto, BillingModel>();
        }
    }
}