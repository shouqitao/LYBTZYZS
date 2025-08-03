using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Billing;

namespace LYBT.WPF.Client.Controls.Billing
{
    /// <summary>
    /// BillingListItemControl.xaml 的交互逻辑
    /// 账单列表项控件
    /// </summary>
    public partial class BillingListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(BillingDto),
                typeof(BillingListItemControl),
                new PropertyMetadata(null));

        public BillingDto Data
        {
            get => (BillingDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public BillingListItemControl()
        {
            InitializeComponent();
        }
    }
}