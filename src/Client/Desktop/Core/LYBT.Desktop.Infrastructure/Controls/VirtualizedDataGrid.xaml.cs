using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prism.Commands;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>支持虚拟化和懒加载的数据网格控件</summary>
    public partial class VirtualizedDataGrid : UserControl
    {
        public VirtualizedDataGrid() { InitializeComponent(); DataContext = new VirtualizedDataGridViewModel(); }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(VirtualizedDataGrid), new PropertyMetadata(null, OnItemsSourceChanged));
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(VirtualizedDataGrid), new PropertyMetadata(null));

        public System.Collections.IEnumerable ItemsSource { get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
        public object SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VirtualizedDataGrid control && control.DataContext is VirtualizedDataGridViewModel viewModel)
                viewModel.SetItemsSource(e.NewValue as System.Collections.IEnumerable);
        }
    }

    /// <summary>虚拟化数据网格的ViewModel</summary>
    public class VirtualizedDataGridViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<object> _items = new();
        private bool _isLoading;
        private string _searchKeyword = string.Empty;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalItems = 0;
        private int _totalPages = 0;
        private object? _selectedItem;

        public VirtualizedDataGridViewModel()
        {
            Items = new ObservableCollection<object>();
            SearchCommand = new DelegateCommand(() => { });
            RefreshCommand = new DelegateCommand(() => { });
            FirstPageCommand = new DelegateCommand(() => { });
            PreviousPageCommand = new DelegateCommand(() => { });
            NextPageCommand = new DelegateCommand(() => { });
            LastPageCommand = new DelegateCommand(() => { });
        }

        public ObservableCollection<object> Items { get => _items; set => SetProperty(ref _items, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public string SearchKeyword { get => _searchKeyword; set => SetProperty(ref _searchKeyword, value); }
        public int CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }
        public int PageSize { get => _pageSize; set => SetProperty(ref _pageSize, value); }
        public int TotalItems { get => _totalItems; set { if (SetProperty(ref _totalItems, value)) { TotalPages = (int)Math.Ceiling((double)value / PageSize); OnPropertyChanged(nameof(TotalItemsText)); } } }
        public int TotalPages { get => _totalPages; set => SetProperty(ref _totalPages, value); }
        public object? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        public string TotalItemsText => $"共 {TotalItems} 条记录";
        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        public void SetItemsSource(System.Collections.IEnumerable? itemsSource)
        {
            Items.Clear();
            if (itemsSource != null) foreach (var item in itemsSource) Items.Add(item);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) { if (Equals(storage, value)) return false; storage = value; OnPropertyChanged(propertyName); return true; }
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
