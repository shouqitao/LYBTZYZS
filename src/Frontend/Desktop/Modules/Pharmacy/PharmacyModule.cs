using LYBT.WPF.Client.Modules.Pharmacy.Views;
using LYBT.WPF.Client.Modules.Pharmacy.Dialogs;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.Pharmacy
{
    /// <summary>
    /// 药房模块
    /// </summary>
    public class PharmacyModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册药房管理视图
            containerRegistry.RegisterForNavigation<PharmacyManagementView>();
            
            // 注册对话框
            containerRegistry.RegisterDialog<DispensingDialog>();
            containerRegistry.RegisterDialog<StockInDialog>();
            containerRegistry.RegisterDialog<StockOutDialog>();
            containerRegistry.RegisterDialog<InventoryDialog>();
            containerRegistry.RegisterDialog<StockAlertDialog>();
            containerRegistry.RegisterDialog<PrescriptionDetailDialog>();
        }
    }
}