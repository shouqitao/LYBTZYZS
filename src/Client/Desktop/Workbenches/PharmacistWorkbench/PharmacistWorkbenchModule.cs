using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.Desktop.Workbench.Pharmacist.Views;
using LYBT.Desktop.Workbench.Pharmacist.ViewModels;
// UltraThink Phase 3.3: 集成Herbs模块功能
using LYBT.Desktop.Herbs.Views;

namespace LYBT.Desktop.Workbench.Pharmacist
{
    /// <summary>
    /// 药剂师工作台模块
    /// </summary>
    public class PharmacistWorkbenchModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
            
            // 注册自定义的ViewModel映射
            ViewModelLocationProvider.Register<PharmacistMainView, PharmacistMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册药剂师工作台主视图
            containerRegistry.RegisterForNavigation<PharmacistMainView>();
            
            // 注册占位视图 (暂时注释，待实现)
            // containerRegistry.RegisterForNavigation<DrugPreparationView>(); // 待实现
            // containerRegistry.RegisterForNavigation<InventoryManagementView>(); // 待实现
            // containerRegistry.RegisterForNavigation<MedicationGuidanceView>(); // 待实现
            
            // UltraThink Phase 3.3: 注册集成的中药材管理功能
            containerRegistry.RegisterForNavigation<HerbManagementView>();
            
            // TODO: 注册其他视图和服务
        }
    }
}