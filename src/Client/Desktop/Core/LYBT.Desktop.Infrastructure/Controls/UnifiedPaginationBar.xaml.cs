using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 统一分页工具栏组件
    /// 提供统一的分页功能
    /// Issue #1840 - Desktop端管理界面UI统一化
    /// </summary>
    public partial class UnifiedPaginationBar : UserControl
    {
        public UnifiedPaginationBar()
        {
            InitializeComponent();
        }

        #region 依赖属性

        /// <summary>
        /// 当前页码(从1开始)
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
                typeof(UnifiedPaginationBar),
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
                typeof(UnifiedPaginationBar),
                new PropertyMetadata(1));

        /// <summary>
        /// 每页显示数量
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
                typeof(UnifiedPaginationBar),
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
                typeof(UnifiedPaginationBar),
                new PropertyMetadata(0));

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
                typeof(UnifiedPaginationBar),
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
                typeof(UnifiedPaginationBar),
                new PropertyMetadata(null));

        /// <summary>
        /// 页大小改变命令
        /// </summary>
        public ICommand PageSizeChangedCommand
        {
            get => (ICommand)GetValue(PageSizeChangedCommandProperty);
            set => SetValue(PageSizeChangedCommandProperty, value);
        }

        public static readonly DependencyProperty PageSizeChangedCommandProperty =
            DependencyProperty.Register(
                nameof(PageSizeChangedCommand),
                typeof(ICommand),
                typeof(UnifiedPaginationBar),
                new PropertyMetadata(null));

        /// <summary>
        /// 首页命令
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
                typeof(UnifiedPaginationBar),
                new PropertyMetadata(null));

        /// <summary>
        /// 末页命令
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
                typeof(UnifiedPaginationBar),
                new PropertyMetadata(null));

        #endregion
    }
}
