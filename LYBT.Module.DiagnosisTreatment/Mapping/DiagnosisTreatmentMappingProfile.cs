using AutoMapper;
using LYBT.Module.DiagnosisTreatment.Models;
using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using LYBT.Module.Herbs.Models.Dtos;

namespace LYBT.Module.DiagnosisTreatment.Mapping {

    /// <summary>
    /// 诊疗相关实体与DTO的AutoMapper配置
    /// </summary>
    public class DiagnosisTreatmentMappingProfile : Profile {

        public DiagnosisTreatmentMappingProfile() {
            // 诊疗主表
            CreateMap<DiagnosisTreatmentModel, DiagnosisTreatmentDto>().ReverseMap();
            CreateMap<DiagnosisTreatmentModel, DiagnosisTreatmentDetailDto>().ReverseMap();
            CreateMap<DiagnosisTreatmentCreateDto, DiagnosisTreatmentModel>();
            // 治疗项目
            CreateMap<TreatmentItemModel, TreatmentItemDto>().ReverseMap();
            // 药方与药材
            CreateMap<FormulaModel, FormulaDto>().ReverseMap();
            CreateMap<HerbItemModel, HerbDto>().ReverseMap();
        }
    }
}