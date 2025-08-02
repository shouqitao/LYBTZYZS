using AutoMapper;
using LYBT.Models.Prescriptions;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Mapping {

    /// <summary>
    /// 表示PrescriptionMappingProfile。
    /// </summary>
    public class PrescriptionMappingProfile : Profile {

        public PrescriptionMappingProfile() {
            CreateMap<PrescriptionModel, PrescriptionDto>();
            CreateMap<PrescriptionModel, PrescriptionDetailDto>();
            CreateMap<PrescriptionItemModel, PrescriptionItemDto>();
            CreateMap<PrescriptionCreateDto, PrescriptionModel>();
            CreateMap<PrescriptionItemCreateDto, PrescriptionItemModel>();
            CreateMap<PrescriptionEditDto, PrescriptionModel>();
        }
    }
}