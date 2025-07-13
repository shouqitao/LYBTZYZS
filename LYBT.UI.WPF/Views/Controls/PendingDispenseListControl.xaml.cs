using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Module.Pharmacy.Dtos;

namespace LYBT.UI.WPF.Views.Controls {
    public partial class PendingDispenseListControl : UserControl {
        public PendingDispenseListControl() {
            InitializeComponent();
        }

        public ObservableCollection<PharmacyDto> ItemsSource {
            get => (ObservableCollection<PharmacyDto>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<PharmacyDto>), typeof(PendingDispenseListControl));

        public PharmacyDto? SelectedItem {
            get => (PharmacyDto?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(PharmacyDto), typeof(PendingDispenseListControl));
    }
}
