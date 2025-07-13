using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Module.Registration.Dtos;

namespace LYBT.UI.WPF.Views.Controls {
    public partial class PendingConsultationListControl : UserControl {
        public PendingConsultationListControl() {
            InitializeComponent();
        }

        public ObservableCollection<RegistrationDto> ItemsSource {
            get => (ObservableCollection<RegistrationDto>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<RegistrationDto>), typeof(PendingConsultationListControl));

        public RegistrationDto? SelectedItem {
            get => (RegistrationDto?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(RegistrationDto), typeof(PendingConsultationListControl));
    }
}
