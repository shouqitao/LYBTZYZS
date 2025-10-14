using AutoMapper;
using LYBT.Desktop.Infrastructure.Events;

namespace LYBT.Desktop.Presentation.Mapping
{
    /// <summary>
    /// PatientSelector组件映射配置
    /// </summary>
    public class PatientSelectorMappingProfile : Profile
    {
        /// <summary>
        /// 初始化PatientSelectorMappingProfile
        /// </summary>
        public PatientSelectorMappingProfile()
        {
            // 可以在这里添加患者选择器相关的映射配置
            // 例如：从患者实体到PatientSelectedPayload的映射
            
            // 示例映射（如果有患者实体的话）：
            // CreateMap<PatientEntity, PatientSelectedPayload>()
            //     .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.Id))
            //     .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.Name))
            //     .ForMember(dest => dest.SelectedAt, opt => opt.MapFrom(_ => DateTime.Now));
        }
    }
}