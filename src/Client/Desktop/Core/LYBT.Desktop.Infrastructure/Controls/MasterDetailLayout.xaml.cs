using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Infrastructure.Helpers;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// Master-Detail布局容器控件
    /// OpenSpec: refactor-master-detail-layout
    /// OpenSpec: responsive-layout-optimization
    ///
    /// 功能：
    /// - 左右分割布局，支持GridSplitter调节
    /// - 左侧Master区域显示列表
    /// - 右侧Detail区域显示详情/编辑
    /// - 支持空状态提示
    /// - 响应式布局适配不同屏幕尺寸
    /// </summary>
    public partial class MasterDetailLayout : UserControl
    {
        public MasterDetailLayout()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyResponsiveLayout();
            // 监听窗口大小变化
            if (Window.GetWindow(this) is Window window)
            {
                window.SizeChanged += OnWindowSizeChanged;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is Window window)
            {
                window.SizeChanged -= OnWindowSizeChanged;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        /// <summary>
        /// 应用响应式布局
        /// </summary>
        private void ApplyResponsiveLayout()
        {
            if (Window.GetWindow(this) is not Window window) return;

            var category = ResponsiveLayoutHelper.GetScreenCategory(window.ActualWidth);
            var masterMinWidth = ResponsiveLayoutHelper.GetRecommendedMasterWidth(category);
            var detailMinWidth = ResponsiveLayoutHelper.GetRecommendedDetailWidth(category);

            // 更新MinWidth
            if (MasterWidth.IsAbsolute)
            {
                MasterWidth = new GridLength(masterMinWidth, GridUnitType.Pixel);
            }
            if (DetailWidth.IsAbsolute)
            {
                DetailWidth = new GridLength(detailMinWidth, GridUnitType.Pixel);
            }
        }

        #region MasterContent - 主列表内容

        public object MasterContent
        {
            get => GetValue(MasterContentProperty);
            set => SetValue(MasterContentProperty, value);
        }

        public static readonly DependencyProperty MasterContentProperty =
            DependencyProperty.Register(nameof(MasterContent), typeof(object), typeof(MasterDetailLayout),
                new PropertyMetadata(null));

        #endregion

        #region DetailContent - 详情内容

        public object DetailContent
        {
            get => GetValue(DetailContentProperty);
            set => SetValue(DetailContentProperty, value);
        }

        public static readonly DependencyProperty DetailContentProperty =
            DependencyProperty.Register(nameof(DetailContent), typeof(object), typeof(MasterDetailLayout),
                new PropertyMetadata(null));

        #endregion

        #region EmptyContent - 空状态内容

        public object EmptyContent
        {
            get => GetValue(EmptyContentProperty);
            set => SetValue(EmptyContentProperty, value);
        }

        public static readonly DependencyProperty EmptyContentProperty =
            DependencyProperty.Register(nameof(EmptyContent), typeof(object), typeof(MasterDetailLayout),
                new PropertyMetadata(null));

        #endregion

        #region HeaderContent - 头部内容（面包屑等导航）

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(MasterDetailLayout),
                new PropertyMetadata(null));

        #endregion

        #region HasSelection - 是否有选中项

        public bool HasSelection
        {
            get => (bool)GetValue(HasSelectionProperty);
            set => SetValue(HasSelectionProperty, value);
        }

        public static readonly DependencyProperty HasSelectionProperty =
            DependencyProperty.Register(nameof(HasSelection), typeof(bool), typeof(MasterDetailLayout),
                new PropertyMetadata(false));

        #endregion

        #region MasterWidth - 主列表宽度

        public GridLength MasterWidth
        {
            get => (GridLength)GetValue(MasterWidthProperty);
            set => SetValue(MasterWidthProperty, value);
        }

        public static readonly DependencyProperty MasterWidthProperty =
            DependencyProperty.Register(nameof(MasterWidth), typeof(GridLength), typeof(MasterDetailLayout),
                new PropertyMetadata(new GridLength(2, GridUnitType.Star)));

        #endregion

        #region DetailWidth - 详情区域宽度

        public GridLength DetailWidth
        {
            get => (GridLength)GetValue(DetailWidthProperty);
            set => SetValue(DetailWidthProperty, value);
        }

        public static readonly DependencyProperty DetailWidthProperty =
            DependencyProperty.Register(nameof(DetailWidth), typeof(GridLength), typeof(MasterDetailLayout),
                new PropertyMetadata(new GridLength(3, GridUnitType.Star)));

        #endregion
    }
}
