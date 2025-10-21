using AutoMapper;
using LYBT.Desktop.Infrastructure.Events;

namespace LYBT.Desktop.Presentation.Mapping
{
    /// <summary>
    /// PatientSelector组件映射配置
    /// 注意：由于架构原因,Presentation层不能引用Modules层
    /// PatientSelector组件使用反射进行手动映射,不需要AutoMapper配置
    /// 详见: PatientSelectorViewModel.CreatePatientSelectedPayload()
    /// </summary>
    public class PatientSelectorMappingProfile : Profile
    {
        /// <summary>
        /// 初始化PatientSelectorMappingProfile
        /// </summary>
        public PatientSelectorMappingProfile()
        {
            // 暂无映射配置
            // PatientSelector组件使用反射进行手动映射
        }
    }
}