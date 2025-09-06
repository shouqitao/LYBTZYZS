using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.WPF.Client.Controls.Prescriptions {

    /// <summary>
    /// PrescriptionListItemControl.xaml 的交互逻辑
    /// 处方列表项控件
    /// </summary>
    public partial class PrescriptionListItemControl : UserControl {

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(PrescriptionDto),
                typeof(PrescriptionListItemControl),
                new PropertyMetadata(null));

        public PrescriptionDto Data {
            get => (PrescriptionDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public PrescriptionListItemControl() {
            InitializeComponent();
        }
    }
}
