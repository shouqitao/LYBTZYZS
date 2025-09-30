using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prism.Commands;

namespace LYBT.Desktop.Infrastructure.Controls
{

    /// <summary>
    /// 支持虚拟化和懒加载的数据网格控件 - Infrastructure简化版本
    /// </summary>
    public partial class VirtualizedDataGrid : UserControl
    {

        public VirtualizedDataGrid()
        {
            InitializeComponent();
            DataContext = new VirtualizedDataGridViewModel();
        }

        #region 依赖属性

        /// <summary>
        /// 数据源属性
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(System.Collections.IEnumerable),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// 选中项属性
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(VirtualizedDataGrid),
                new PropertyMetadata(null));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        #endregion 依赖属性

        #region 事件处理

        /// <summary>
        /// 数据源改变处理
        /// </summary>
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VirtualizedDataGrid control && control.DataContext is VirtualizedDataGridViewModel viewModel)
            {
                viewModel.SetItemsSource(e.NewValue as System.Collections.IEnumerable);
            }
        }

        #endregion 事件处理
    }

    /// <summary>
    /// 虚拟化数据网格的ViewModel - 简化版本
    /// </summary>
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

            // 初始化命令
            SearchCommand = new DelegateCommand(() => { /* 搜索逻辑 */ });
            RefreshCommand = new DelegateCommand(() => { /* 刷新逻辑 */ });
            FirstPageCommand = new DelegateCommand(() => { /* 首页逻辑 */ });
            PreviousPageCommand = new DelegateCommand(() => { /* 上一页逻辑 */ });
            NextPageCommand = new DelegateCommand(() => { /* 下一页逻辑 */ });
            LastPageCommand = new DelegateCommand(() => { /* 末页逻辑 */ });
        }

        #region 属性

        /// <summary>
        /// 数据项集合
        /// </summary>
        public ObservableCollection<object> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        /// <summary>
        /// 当前页
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        /// <summary>
        /// 总项目数
        /// </summary>
        public int TotalItems
        {
            get => _totalItems;
            set
            {
                if (SetProperty(ref _totalItems, value))
                {
                    TotalPages = (int)Math.Ceiling((double)value / PageSize);
                    OnPropertyChanged(nameof(TotalItemsText));
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        /// <summary>
        /// 选中项
        /// </summary>
        public object? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        /// <summary>
        /// 总项目数文本
        /// </summary>
        public string TotalItemsText => $"共 {TotalItems} 条记录";

        /// <summary>
        /// 是否可以转到上一页
        /// </summary>
        public bool CanGoToPreviousPage => CurrentPage > 1;

        /// <summary>
        /// 是否可以转到下一页
        /// </summary>
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        #endregion 属性

        #region 命令

        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        #endregion 命令

        #region 公共方法

        /// <summary>
        /// 设置数据源
        /// </summary>
        public void SetItemsSource(System.Collections.IEnumerable? itemsSource)
        {
            Items.Clear();
            if (itemsSource != null)
            {
                foreach (var item in itemsSource)
                {
                    Items.Add(item);
                }
            }
        }

        #endregion 公共方法

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}