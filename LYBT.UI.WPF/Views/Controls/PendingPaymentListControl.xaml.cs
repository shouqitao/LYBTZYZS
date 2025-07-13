using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Module.Billing.Dtos;

namespace LYBT.UI.WPF.Views.Controls {
    public partial class PendingPaymentListControl : UserControl {
        public PendingPaymentListControl() {
            InitializeComponent();
        }

        public ObservableCollection<BillingDto> ItemsSource {
            get => (ObservableCollection<BillingDto>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<BillingDto>), typeof(PendingPaymentListControl));

        public BillingDto? SelectedItem {
            get => (BillingDto?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(BillingDto), typeof(PendingPaymentListControl));
    }
}
