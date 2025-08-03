using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Pharmacy;

namespace LYBT.WPF.Client.Controls.Pharmacy
{
    /// <summary>
    /// PharmacyListItemControl.xaml 的交互逻辑
    /// 药房列表项控件
    /// </summary>
    public partial class PharmacyListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(PharmacyDto),
                typeof(PharmacyListItemControl),
                new PropertyMetadata(null));

        public PharmacyDto Data
        {
            get => (PharmacyDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public PharmacyListItemControl()
        {
            InitializeComponent();
        }
    }
}