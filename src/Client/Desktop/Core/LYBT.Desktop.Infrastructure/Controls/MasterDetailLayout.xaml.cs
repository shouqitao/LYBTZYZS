using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// Master-Detail布局容器控件
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 功能：
    /// - 左右分割布局，支持GridSplitter调节
    /// - 左侧Master区域显示列表
    /// - 右侧Detail区域显示详情/编辑
    /// - 支持空状态提示
    /// </summary>
    public partial class MasterDetailLayout : UserControl
    {
        public MasterDetailLayout() => InitializeComponent();

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
