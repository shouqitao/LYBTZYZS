using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Module.TreatmentRoom.Dtos;

namespace LYBT.UI.WPF.Views.Controls {
    public partial class PendingTreatmentListControl : UserControl {
        public PendingTreatmentListControl() {
            InitializeComponent();
        }

        public ObservableCollection<TreatmentRoomDto> ItemsSource {
            get => (ObservableCollection<TreatmentRoomDto>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<TreatmentRoomDto>), typeof(PendingTreatmentListControl));

        public TreatmentRoomDto? SelectedItem {
            get => (TreatmentRoomDto?)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(TreatmentRoomDto), typeof(PendingTreatmentListControl));
    }
}
