using LYBT.WPF.Client.Modules.MedicalCase.ViewModels;
using LYBT.WPF.Client.Modules.MedicalCase.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.MedicalCase
{
    /// <summary>
    /// 医疗案例模块
    /// </summary>
    public class MedicalCaseModule : IModule
    {

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化后的操作
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册视图和视图模型（使用简化版）
            containerRegistry.RegisterForNavigation<MedicalCaseListView, MedicalCaseListViewModelSimple>();
            // 暂时注释掉其他视图，待修复后再启用
            // containerRegistry.RegisterForNavigation<MedicalCaseDetailView, MedicalCaseDetailViewModel>();
            // containerRegistry.RegisterDialog<CreateMedicalCaseDialog, CreateMedicalCaseViewModel>();
        }
    }
}