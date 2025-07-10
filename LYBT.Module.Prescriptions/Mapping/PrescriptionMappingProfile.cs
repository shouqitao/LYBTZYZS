using AutoMapper;
using LYBT.Models.Prescriptions;
using LYBT.Module.Prescriptions.Dtos;

namespace LYBT.Module.Prescriptions.Mapping {
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
