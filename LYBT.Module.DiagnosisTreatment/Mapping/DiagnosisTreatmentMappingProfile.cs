using AutoMapper;
using LYBT.Models.DiagnosisTreatment;
using LYBT.Models.Herbs;

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