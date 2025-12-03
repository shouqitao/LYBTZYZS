using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Views
{
    /// <summary>基础数据管理视图模板 - 提供统一的三行布局（工具栏+数据表格+分页控件）</summary>
    public partial class BaseMasterDataListView : UserControl
    {
        public BaseMasterDataListView() => InitializeComponent();

        public IEnumerable ItemsSource { get => (IEnumerable)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public object SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(BaseMasterDataListView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string EmptyStateText { get => (string)GetValue(EmptyStateTextProperty); set => SetValue(EmptyStateTextProperty, value); }
        public static readonly DependencyProperty EmptyStateTextProperty = DependencyProperty.Register(nameof(EmptyStateText), typeof(string), typeof(BaseMasterDataListView), new PropertyMetadata("暂无数据"));

        public IList SelectedItems { get => (IList)GetValue(SelectedItemsProperty); set => SetValue(SelectedItemsProperty, value); }
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(BaseMasterDataListView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool ShowCheckBoxColumn { get => (bool)GetValue(ShowCheckBoxColumnProperty); set => SetValue(ShowCheckBoxColumnProperty, value); }
        public static readonly DependencyProperty ShowCheckBoxColumnProperty = DependencyProperty.Register(nameof(ShowCheckBoxColumn), typeof(bool), typeof(BaseMasterDataListView), new PropertyMetadata(false));

        public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(BaseMasterDataListView), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ICommand SearchCommand { get => (ICommand)GetValue(SearchCommandProperty); set => SetValue(SearchCommandProperty, value); }
        public static readonly DependencyProperty SearchCommandProperty = DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public int CurrentPage { get => (int)GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value); }
        public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(BaseMasterDataListView), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public int TotalPages { get => (int)GetValue(TotalPagesProperty); set => SetValue(TotalPagesProperty, value); }
        public static readonly DependencyProperty TotalPagesProperty = DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(BaseMasterDataListView), new PropertyMetadata(0));

        public int PageSize { get => (int)GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }
        public static readonly DependencyProperty PageSizeProperty = DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(BaseMasterDataListView), new FrameworkPropertyMetadata(20, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public int TotalCount { get => (int)GetValue(TotalCountProperty); set => SetValue(TotalCountProperty, value); }
        public static readonly DependencyProperty TotalCountProperty = DependencyProperty.Register(nameof(TotalCount), typeof(int), typeof(BaseMasterDataListView), new PropertyMetadata(0));

        public ICommand FirstPageCommand { get => (ICommand)GetValue(FirstPageCommandProperty); set => SetValue(FirstPageCommandProperty, value); }
        public static readonly DependencyProperty FirstPageCommandProperty = DependencyProperty.Register(nameof(FirstPageCommand), typeof(ICommand), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public ICommand PreviousPageCommand { get => (ICommand)GetValue(PreviousPageCommandProperty); set => SetValue(PreviousPageCommandProperty, value); }
        public static readonly DependencyProperty PreviousPageCommandProperty = DependencyProperty.Register(nameof(PreviousPageCommand), typeof(ICommand), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public ICommand NextPageCommand { get => (ICommand)GetValue(NextPageCommandProperty); set => SetValue(NextPageCommandProperty, value); }
        public static readonly DependencyProperty NextPageCommandProperty = DependencyProperty.Register(nameof(NextPageCommand), typeof(ICommand), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public ICommand LastPageCommand { get => (ICommand)GetValue(LastPageCommandProperty); set => SetValue(LastPageCommandProperty, value); }
        public static readonly DependencyProperty LastPageCommandProperty = DependencyProperty.Register(nameof(LastPageCommand), typeof(ICommand), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public object FilterContent { get => GetValue(FilterContentProperty); set => SetValue(FilterContentProperty, value); }
        public static readonly DependencyProperty FilterContentProperty = DependencyProperty.Register(nameof(FilterContent), typeof(object), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public object ActionButtons { get => GetValue(ActionButtonsProperty); set => SetValue(ActionButtonsProperty, value); }
        public static readonly DependencyProperty ActionButtonsProperty = DependencyProperty.Register(nameof(ActionButtons), typeof(object), typeof(BaseMasterDataListView), new PropertyMetadata(null));

        public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(BaseMasterDataListView), new PropertyMetadata(false));

        public string BusyMessage { get => (string)GetValue(BusyMessageProperty); set => SetValue(BusyMessageProperty, value); }
        public static readonly DependencyProperty BusyMessageProperty = DependencyProperty.Register(nameof(BusyMessage), typeof(string), typeof(BaseMasterDataListView), new PropertyMetadata("正在加载..."));

        public System.Collections.ObjectModel.ObservableCollection<DataGridColumn> Columns => DataTable == null ? new System.Collections.ObjectModel.ObservableCollection<DataGridColumn>() : DataTable.Columns;
    }
}
