using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Views
{
    /// <summary>
    /// 基础数据管理视图模板
    /// Issue #1998 - Task 2.5: 提供统一的管理界面布局模板
    ///
    /// 功能特性：
    /// - 统一三行布局：工具栏（Row 0）+ 数据表格（Row 1）+ 分页控件（Row 2）
    /// - 支持自定义：DataGrid列、操作按钮、筛选区域
    /// - 集成忙碌指示器
    ///
    /// 使用说明：
    /// 1. 子类View通过暴露的依赖属性绑定ViewModel数据
    /// 2. 通过 Columns 属性定义 DataGrid 列（类似 UnifiedManagementTable）
    /// 3. 通过 ActionButtons 属性提供操作按钮
    /// </summary>
    public partial class BaseMasterDataListView : UserControl
    {
        public BaseMasterDataListView()
        {
            InitializeComponent();
        }

        #region 数据相关依赖属性

        /// <summary>
        /// 数据源
        /// </summary>
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        /// <summary>
        /// 选中项
        /// </summary>
        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(BaseMasterDataListView),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 空状态提示文本
        /// </summary>
        public string EmptyStateText
        {
            get => (string)GetValue(EmptyStateTextProperty);
            set => SetValue(EmptyStateTextProperty, value);
        }

        public static readonly DependencyProperty EmptyStateTextProperty =
            DependencyProperty.Register(
                nameof(EmptyStateText),
                typeof(string),
                typeof(BaseMasterDataListView),
                new PropertyMetadata("暂无数据"));

        /// <summary>
        /// 选中项集合（批量操作）
        /// </summary>
        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(
                nameof(SelectedItems),
                typeof(IList),
                typeof(BaseMasterDataListView),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 是否显示CheckBox选择列
        /// </summary>
        public bool ShowCheckBoxColumn
        {
            get => (bool)GetValue(ShowCheckBoxColumnProperty);
            set => SetValue(ShowCheckBoxColumnProperty, value);
        }

        public static readonly DependencyProperty ShowCheckBoxColumnProperty =
            DependencyProperty.Register(
                nameof(ShowCheckBoxColumn),
                typeof(bool),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(false));

        #endregion

        #region 搜索相关依赖属性

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(BaseMasterDataListView),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(
                nameof(SearchCommand),
                typeof(ICommand),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        #endregion

        #region 分页相关依赖属性

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => (int)GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(
                nameof(CurrentPage),
                typeof(int),
                typeof(BaseMasterDataListView),
                new FrameworkPropertyMetadata(
                    1,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => (int)GetValue(TotalPagesProperty);
            set => SetValue(TotalPagesProperty, value);
        }

        public static readonly DependencyProperty TotalPagesProperty =
            DependencyProperty.Register(
                nameof(TotalPages),
                typeof(int),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(0));

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize
        {
            get => (int)GetValue(PageSizeProperty);
            set => SetValue(PageSizeProperty, value);
        }

        public static readonly DependencyProperty PageSizeProperty =
            DependencyProperty.Register(
                nameof(PageSize),
                typeof(int),
                typeof(BaseMasterDataListView),
                new FrameworkPropertyMetadata(
                    20,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => (int)GetValue(TotalCountProperty);
            set => SetValue(TotalCountProperty, value);
        }

        public static readonly DependencyProperty TotalCountProperty =
            DependencyProperty.Register(
                nameof(TotalCount),
                typeof(int),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(0));

        #endregion

        #region 分页命令依赖属性

        /// <summary>
        /// 第一页命令
        /// </summary>
        public ICommand FirstPageCommand
        {
            get => (ICommand)GetValue(FirstPageCommandProperty);
            set => SetValue(FirstPageCommandProperty, value);
        }

        public static readonly DependencyProperty FirstPageCommandProperty =
            DependencyProperty.Register(
                nameof(FirstPageCommand),
                typeof(ICommand),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        /// <summary>
        /// 上一页命令
        /// </summary>
        public ICommand PreviousPageCommand
        {
            get => (ICommand)GetValue(PreviousPageCommandProperty);
            set => SetValue(PreviousPageCommandProperty, value);
        }

        public static readonly DependencyProperty PreviousPageCommandProperty =
            DependencyProperty.Register(
                nameof(PreviousPageCommand),
                typeof(ICommand),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        /// <summary>
        /// 下一页命令
        /// </summary>
        public ICommand NextPageCommand
        {
            get => (ICommand)GetValue(NextPageCommandProperty);
            set => SetValue(NextPageCommandProperty, value);
        }

        public static readonly DependencyProperty NextPageCommandProperty =
            DependencyProperty.Register(
                nameof(NextPageCommand),
                typeof(ICommand),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        /// <summary>
        /// 最后一页命令
        /// </summary>
        public ICommand LastPageCommand
        {
            get => (ICommand)GetValue(LastPageCommandProperty);
            set => SetValue(LastPageCommandProperty, value);
        }

        public static readonly DependencyProperty LastPageCommandProperty =
            DependencyProperty.Register(
                nameof(LastPageCommand),
                typeof(ICommand),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        #endregion

        #region 自定义内容插槽依赖属性

        /// <summary>
        /// 筛选内容区域
        /// 用于放置筛选控件（如ComboBox、DatePicker等）
        /// </summary>
        public object FilterContent
        {
            get => GetValue(FilterContentProperty);
            set => SetValue(FilterContentProperty, value);
        }

        public static readonly DependencyProperty FilterContentProperty =
            DependencyProperty.Register(
                nameof(FilterContent),
                typeof(object),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        /// <summary>
        /// 操作按钮区域
        /// 用于放置操作按钮（如"新建"、"导入"、"导出"、"刷新"等）
        /// </summary>
        public object ActionButtons
        {
            get => GetValue(ActionButtonsProperty);
            set => SetValue(ActionButtonsProperty, value);
        }

        public static readonly DependencyProperty ActionButtonsProperty =
            DependencyProperty.Register(
                nameof(ActionButtons),
                typeof(object),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(null));

        #endregion

        #region 忙碌状态依赖属性

        /// <summary>
        /// 是否处于忙碌状态
        /// </summary>
        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(
                nameof(IsBusy),
                typeof(bool),
                typeof(BaseMasterDataListView),
                new PropertyMetadata(false));

        /// <summary>
        /// 忙碌提示消息
        /// </summary>
        public string BusyMessage
        {
            get => (string)GetValue(BusyMessageProperty);
            set => SetValue(BusyMessageProperty, value);
        }

        public static readonly DependencyProperty BusyMessageProperty =
            DependencyProperty.Register(
                nameof(BusyMessage),
                typeof(string),
                typeof(BaseMasterDataListView),
                new PropertyMetadata("正在加载..."));

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取DataGrid的列集合
        /// 允许在XAML中通过 &lt;BaseMasterDataListView.Columns&gt; 定义DataGrid列
        ///
        /// 使用示例：
        /// &lt;views:BaseMasterDataListView&gt;
        ///     &lt;views:BaseMasterDataListView.Columns&gt;
        ///         &lt;DataGridTextColumn Header="列名" Binding="{Binding PropertyName}" /&gt;
        ///     &lt;/views:BaseMasterDataListView.Columns&gt;
        /// &lt;/views:BaseMasterDataListView&gt;
        ///
        /// Issue #2011: 添加 null 检查，防止在视觉树构建期间访问未初始化的 DataTable
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<DataGridColumn> Columns
        {
            get
            {
                // 确保 DataTable 已经在视觉树中初始化
                if (DataTable == null)
                {
                    return new System.Collections.ObjectModel.ObservableCollection<DataGridColumn>();
                }
                return DataTable.Columns;
            }
        }

        #endregion
    }
}
