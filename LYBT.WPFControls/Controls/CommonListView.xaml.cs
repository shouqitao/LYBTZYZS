using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace LYBT.WPFControls {
    /// <summary>
    /// 通用列表控件，包含查询、数据表格与分页条。
    /// </summary>
    [ContentProperty(nameof(Columns))]
    public partial class CommonListView : UserControl {
        public CommonListView() {
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

        public object? ActionContent {
            get => GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }

        public static readonly DependencyProperty ActionContentProperty =
            DependencyProperty.Register(nameof(ActionContent), typeof(object), typeof(CommonListView));

        public object? ItemsSource {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(CommonListView));

        public object? SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(CommonListView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string? SearchKeyword {
            get => (string?)GetValue(SearchKeywordProperty);
            set => SetValue(SearchKeywordProperty, value);
        }

        public static readonly DependencyProperty SearchKeywordProperty =
            DependencyProperty.Register(nameof(SearchKeyword), typeof(string), typeof(CommonListView));

        public System.Windows.Input.ICommand? SearchCommand {
            get => (System.Windows.Input.ICommand?)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(System.Windows.Input.ICommand), typeof(CommonListView));

        public bool IsBusy {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(CommonListView));

        public bool ShowPaging {
            get => (bool)GetValue(ShowPagingProperty);
            set => SetValue(ShowPagingProperty, value);
        }

        public static readonly DependencyProperty ShowPagingProperty =
            DependencyProperty.Register(nameof(ShowPaging), typeof(bool), typeof(CommonListView), new PropertyMetadata(true));
    }
}
