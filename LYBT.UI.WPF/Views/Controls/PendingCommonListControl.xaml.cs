using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace LYBT.UI.WPF.Views.Controls {
    [ContentProperty(nameof(Columns))]
    public partial class PendingCommonListControl : UserControl {
        public PendingCommonListControl() {
            Columns = new ObservableCollection<DataGridColumn>();
            InitializeComponent();
            Loaded += (_, __) => ApplyColumns();
        }

        private void ApplyColumns() {
            PART_DataGrid.Columns.Clear();
            foreach (var c in Columns)
                PART_DataGrid.Columns.Add(c);
        }

        public ObservableCollection<DataGridColumn> Columns { get; }

        public object? ItemsSource {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(PendingCommonListControl));

        public object? SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(PendingCommonListControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}
