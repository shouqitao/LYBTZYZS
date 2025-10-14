using LYBT.Desktop.Presentation.Components.PatientSelector;
using Prism.Ioc;

namespace LYBT.Desktop.Presentation.DependencyInjection
{
    /// <summary>
    /// ViewModel依赖注入扩展方法
    /// Project Standardization 3.0 - PatientSelector组件依赖注入配置
    /// </summary>
    public static class ViewModelContainerRegistryExtensions
    {
        /// <summary>
        /// 注册所有ViewModel
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <returns>容器注册器</returns>
        public static IContainerRegistry RegisterViewModels(this IContainerRegistry containerRegistry)
        {
            // 注册PatientSelectorViewModel
            containerRegistry.Register<PatientSelectorViewModel>();
            
            return containerRegistry;
        }

        /// <summary>
        /// 注册PatientSelector相关服务
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <returns>容器注册器</returns>
        public static IContainerRegistry RegisterPatientSelectorServices(this IContainerRegistry containerRegistry)
        {
            // 注册ViewModel
            containerRegistry.Register<PatientSelectorViewModel>();
            
            return containerRegistry;
        }
    }
}