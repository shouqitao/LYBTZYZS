using LYBT.WPF.Client.Modules.Payment.Views;
using LYBT.WPF.Client.Modules.Payment.Dialogs;
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.WPF.Client.Modules.Payment
{
    /// <summary>
    /// 付费模块
    /// </summary>
    public class PaymentModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化完成
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册付费管理视图
            containerRegistry.RegisterForNavigation<PaymentManagementView>();
            
            // 注册对话框
            containerRegistry.RegisterDialog<ChargeDialog>();
            containerRegistry.RegisterDialog<RefundDialog>();
            containerRegistry.RegisterDialog<PaymentDetailDialog>();
        }
    }
}