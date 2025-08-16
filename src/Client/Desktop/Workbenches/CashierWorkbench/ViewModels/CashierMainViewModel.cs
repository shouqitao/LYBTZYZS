using LYBT.Shared.Models.Contracts.Common;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;

namespace LYBT.Desktop.Workbench.Cashier.ViewModels
{
    /// <summary>
    /// 收银员工作台主视图模型
    /// </summary>
    public class CashierMainViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;

        public DelegateCommand NavigateToBillingManagementCommand { get; }
        public DelegateCommand NavigateToPaymentManagementCommand { get; }
        public DelegateCommand NavigateToFinancialReportsCommand { get; }
        public DelegateCommand NavigateToRefundManagementCommand { get; }

        public CashierMainViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;

            // 初始化导航命令
            NavigateToBillingManagementCommand = new DelegateCommand(() => NavigateTo("BillingManagementView"));
            NavigateToPaymentManagementCommand = new DelegateCommand(() => NavigateTo("PaymentManagementView"));
            NavigateToFinancialReportsCommand = new DelegateCommand(() => NavigateTo("FinancialReportsView"));
            NavigateToRefundManagementCommand = new DelegateCommand(() => NavigateTo("RefundManagementView"));

            // 默认导航到费用结算
            NavigateTo("BillingManagementView");
        }

        private void NavigateTo(string viewName)
        {
            try
            {
                _regionManager.RequestNavigate("CashierContentRegion", viewName);
            }
            catch (Exception)
            {
                // 如果视图不存在，显示占位界面
                // 暂时静默处理，后续可添加日志
            }
        }
    }
}