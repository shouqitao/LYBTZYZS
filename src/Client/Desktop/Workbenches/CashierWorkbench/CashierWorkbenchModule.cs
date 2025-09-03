using LYBT.Shared.Models.Contracts.Common;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using LYBT.Desktop.Workbench.Cashier.Views;
using LYBT.Desktop.Workbench.Cashier.ViewModels;

namespace LYBT.Desktop.Workbench.Cashier
{
    /// <summary>
    /// 收银员工作台模块
    /// </summary>
    public class CashierWorkbenchModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
            
            // 注册自定义的ViewModel映射
            ViewModelLocationProvider.Register<CashierMainView, CashierMainViewModel>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册收银员工作台主视图
            containerRegistry.RegisterForNavigation<CashierMainView>();
            
            // 注册占位视图
            containerRegistry.RegisterForNavigation<BillingManagementView>();
            // containerRegistry.RegisterForNavigation<PaymentManagementView>(); // 待实现
            // containerRegistry.RegisterForNavigation<FinancialReportsView>(); // 待实现
            
            // 预留：未来可注册收银相关的其他视图和服务
        }
    }
}